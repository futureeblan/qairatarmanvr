using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Drone AI with a three-state FSM: Patrol → Searching → Alert.
/// Integrates with PlayerHealth (catch trigger) and GlitchOverlay (per-state intensity).
/// Zero GC allocations in Update.
/// </summary>
public class DroneLogic : MonoBehaviour
{
    public enum DroneState { Patrol, Searching, Alert }

    // ── Patrol ────────────────────────────────────────────────────────────
    [Header("Patrol")]
    [Tooltip("Visited in sequence, loops. Two points = simple A↔B patrol.")]
    public Transform[] waypoints;
    public float patrolSpeed       = 2f;
    public float patrolRotateSpeed = 90f;  // degrees/sec

    // ── Detection ─────────────────────────────────────────────────────────
    [Header("Detection")]
    public Light droneLight;
    public float viewDistance  = 10f;
    public float beamWidth     = 1.5f;
    [Tooltip("Seconds player must be visible in Patrol before switching to Searching")]
    public float patrolSpotTime     = 0.15f;
    [Tooltip("Seconds player must be visible in Searching before switching to Alert")]
    public float suspicionBuildTime = 0.80f;

    // ── Searching ─────────────────────────────────────────────────────────
    [Header("Searching")]
    [Tooltip("Seconds of scanning before giving up and returning to Patrol")]
    public float searchDuration    = 5f;
    public float searchRotateSpeed = 60f;  // degrees/sec toward last known position

    // ── Alert ─────────────────────────────────────────────────────────────
    [Header("Alert")]
    public float alertSpeed       = 5f;
    public float alertRotateSpeed = 180f;
    [Tooltip("World distance at which the drone catches the player")]
    public float catchDistance    = 1.5f;
    [Tooltip("Seconds without LOS before dropping back to Searching")]
    public float lostPlayerTimeout = 2f;

    // ── Light Colors ──────────────────────────────────────────────────────
    [Header("Light Colors")]
    public Color patrolColor = Color.white;
    public Color searchColor = Color.yellow;
    public Color alertColor  = Color.red;

    // ── References ────────────────────────────────────────────────────────
    [Header("References")]
    public Transform     player;
    public PlayerHealth  playerHealth;
    public GlitchOverlay glitchOverlay;

    // ── Events ────────────────────────────────────────────────────────────
    [Header("Events")]
    public UnityEvent OnEnterPatrol;
    public UnityEvent OnEnterSearching;
    public UnityEvent OnEnterAlert;
    public UnityEvent OnPlayerCaught;

    // ── Private state ─────────────────────────────────────────────────────
    DroneState _state = DroneState.Patrol;
    int        _waypointIndex;
    float      _suspicionTimer;
    float      _searchTimer;
    float      _lostTimer;
    Vector3    _lastKnownPos;
    bool       _caught;
    float      _catchDistSq;    // squared — avoids sqrt each frame

    RaycastHit _hit;            // reused struct — zero allocation per frame

    // ── Public getters ────────────────────────────────────────────────────
    public DroneState State             => _state;
    public bool       IsAlerted         => _state == DroneState.Alert;
    public Vector3    LastKnownPosition => _lastKnownPos;

    // ── Lifecycle ─────────────────────────────────────────────────────────
    void Start()
    {
        _catchDistSq = catchDistance * catchDistance;
        SetState(DroneState.Patrol);
    }

    void Update()
    {
        if (_caught) return;

        switch (_state)
        {
            case DroneState.Patrol:    TickPatrol();    break;
            case DroneState.Searching: TickSearching(); break;
            case DroneState.Alert:     TickAlert();     break;
        }
    }

    // ── Patrol ────────────────────────────────────────────────────────────
    void TickPatrol()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Transform wp = waypoints[_waypointIndex];
        Move(wp.position, patrolSpeed);
        RotateToward(wp.position, patrolRotateSpeed);

        // Advance waypoint (0.3 m threshold, sqrMagnitude avoids sqrt)
        if ((transform.position - wp.position).sqrMagnitude < 0.09f)
            _waypointIndex = (_waypointIndex + 1) % waypoints.Length;

        if (ScanForPlayer())
        {
            _lastKnownPos   = player.position;
            _suspicionTimer += Time.deltaTime;
            if (_suspicionTimer >= patrolSpotTime)
                SetState(DroneState.Searching);
        }
        else
        {
            _suspicionTimer = 0f;
        }
    }

    // ── Searching ─────────────────────────────────────────────────────────
    void TickSearching()
    {
        _searchTimer -= Time.deltaTime;

        // Hover in place, rotate spotlight toward last known position
        RotateToward(_lastKnownPos, searchRotateSpeed);

        if (ScanForPlayer())
        {
            _lastKnownPos    = player.position;
            _suspicionTimer += Time.deltaTime;
            if (_suspicionTimer >= suspicionBuildTime)
                SetState(DroneState.Alert);
        }
        else
        {
            // Decay slower than build — player must hide consistently to cool down
            _suspicionTimer = Mathf.Max(0f, _suspicionTimer - Time.deltaTime * 0.5f);
        }

        if (_searchTimer <= 0f)
        {
            _suspicionTimer = 0f;
            SetState(DroneState.Patrol);
        }
    }

    // ── Alert ─────────────────────────────────────────────────────────────
    void TickAlert()
    {
        if (ScanForPlayer())
        {
            _lastKnownPos = player.position;
            _lostTimer    = 0f;
        }
        else
        {
            _lostTimer += Time.deltaTime;
            if (_lostTimer >= lostPlayerTimeout)
            {
                SetState(DroneState.Searching);
                return;
            }
        }

        // Chase: move toward last known position (current if visible, cached if not)
        Move(_lastKnownPos, alertSpeed);
        RotateToward(_lastKnownPos, alertRotateSpeed);

        // Catch check — sqrMagnitude avoids sqrt
        if ((transform.position - player.position).sqrMagnitude <= _catchDistSq)
            CatchPlayer();
    }

    // ── State machine ─────────────────────────────────────────────────────
    void SetState(DroneState next)
    {
        _state = next;
        switch (next)
        {
            case DroneState.Patrol:
                if (droneLight) droneLight.color = patrolColor;
                glitchOverlay?.SetGlitch(false);
                OnEnterPatrol.Invoke();
                break;

            case DroneState.Searching:
                _searchTimer    = searchDuration;
                _suspicionTimer = 0f;
                if (droneLight) droneLight.color = searchColor;
                glitchOverlay?.SetGlitchIntensity(0.35f); // subtle unease
                OnEnterSearching.Invoke();
                break;

            case DroneState.Alert:
                _lostTimer = 0f;
                if (droneLight) droneLight.color = alertColor;
                glitchOverlay?.SetGlitch(true);
                OnEnterAlert.Invoke();
                break;
        }
    }

    void CatchPlayer()
    {
        if (_caught) return;
        _caught = true;
        glitchOverlay?.SetGlitchIntensity(1f);
        OnPlayerCaught.Invoke();
        playerHealth?.TriggerDeath(transform);
    }

    // ── Movement helpers — no allocation ─────────────────────────────────
    void Move(Vector3 target, float speed)
    {
        transform.position = Vector3.MoveTowards(
            transform.position, target, speed * Time.deltaTime);
    }

    void RotateToward(Vector3 target, float degreesPerSec)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f; // keep drone level — horizontal rotation only
        if (dir.sqrMagnitude < 0.001f) return;
        Quaternion goal = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, goal, degreesPerSec * Time.deltaTime);
    }

    // SphereCast with cached RaycastHit struct — zero allocation per frame
    bool ScanForPlayer()
    {
        if (droneLight == null || player == null) return false;
        return Physics.SphereCast(
                   droneLight.transform.position,
                   beamWidth,
                   droneLight.transform.forward,
                   out _hit,
                   viewDistance)
               && _hit.collider.CompareTag("Player");
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Forces Searching state toward a sound source.
    /// Call from footstep triggers, breaking glass events, etc.
    /// </summary>
    public void HearSound(Vector3 worldPosition)
    {
        if (_caught || _state == DroneState.Alert) return;
        _lastKnownPos = worldPosition;
        SetState(DroneState.Searching);
    }

    // ── Editor gizmos ─────────────────────────────────────────────────────
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, catchDistance);

        if (droneLight != null)
        {
            Gizmos.color = (Application.isPlaying && _state == DroneState.Alert)
                ? Color.red : Color.yellow;
            Gizmos.DrawRay(droneLight.transform.position,
                           droneLight.transform.forward * viewDistance);
        }

        if (Application.isPlaying && _state != DroneState.Patrol)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(_lastKnownPos, 0.25f);
        }
    }
#endif
}
