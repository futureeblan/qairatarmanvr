using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// Робот-враг: патрулирует точки → замечает игрока → преследует → бьёт.
///
/// Настройка:
///   1. Добавь NavMeshAgent на GameObject робота.
///   2. Добавь Animator (нужны параметры: "Speed" float, "Attack" trigger, "Die" trigger).
///   3. Заполни patrolPoints точками патруля.
///   4. Назначь playerTransform (или оставь пустым — найдёт по тегу "Player").
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class RobotEnemy : MonoBehaviour
{
    public enum State { Patrol, Chase, Attack, Dead }

    [Header("Дальности")]
    [Tooltip("С этого расстояния замечает игрока")]
    public float detectionRange = 12f;
    [Tooltip("С этого расстояния атакует")]
    public float attackRange    = 1.8f;
    [Tooltip("Теряет игрока если отошёл дальше")]
    public float loseRange      = 18f;

    [Header("Обнаружение (опционально)")]
    [Tooltip("Если включено — враг увидит игрока только при прямой видимости (Raycast).")]
    public bool requireLineOfSight = false;
    [Tooltip("Точка откуда 'смотрит' робот. Если пусто — используется transform.")]
    public Transform eye;
    [Tooltip("Если eye пусто — высота луча над pivot'ом.")]
    public float eyeHeight = 1.4f;
    [Tooltip("Слои, которые блокируют видимость. Обычно: Default + Environment, без Player.")]
    public LayerMask lineOfSightMask = ~0;

    [Header("Атака")]
    public int   damagePerHit   = 1;
    [Tooltip("Секунд между ударами")]
    public float attackCooldown = 2f;
    [Tooltip("Если true — урон наносится только через AnimationEvent (DealDamageFromAnimation).")]
    public bool useAnimationEventForDamage = false;

    [Header("Движение")]
    public float chaseSpeed   = 3.5f;
    public float patrolSpeed  = 1.8f;
    [Tooltip("Точки патруля. Если пусто — стоит на месте")]
    public Transform[] patrolPoints;

    [Header("Атмосфера (звук/эффекты)")]
    [Tooltip("Звук, когда робот впервые замечает игрока (вход в Chase).")]
    public AudioClip detectSound;
    [Tooltip("Звук, когда робот теряет игрока (переход обратно в Patrol).")]
    public AudioClip loseSound;
    public AudioClip attackSound;
    [Tooltip("Опционально: глитч-оверлей на камере игрока (как в PlayerHealth/JumpScare).")]
    public GlitchOverlay glitchOverlay;
    [Range(0f, 1f)]
    public float detectGlitchIntensity = 0.25f;
    [Range(0f, 1f)]
    public float chaseGlitchIntensity = 0.35f;
    [Range(0f, 1f)]
    public float attackGlitchIntensity = 0.65f;

    [Header("Ссылки")]
    [Tooltip("Оставь пустым — найдёт по тегу Player")]
    public Transform playerTransform;

    [Header("Events")]
    public UnityEvent OnPlayerDetected;
    public UnityEvent OnPlayerLost;
    public UnityEvent OnAttack;
    public UnityEvent OnDied;

    State        _state          = State.Patrol;
    NavMeshAgent _agent;
    Animator     _anim;
    AudioSource  _audio;
    PlayerHealth _playerHealth;

    int   _patrolIndex;
    float _attackTimer;
    float _patrolWaitTimer;
    const float PatrolWait = 1.5f;
    bool  _hasDetected;

    static readonly int AnimSpeed  = Animator.StringToHash("Speed");
    static readonly int AnimAttack = Animator.StringToHash("Attack");
    static readonly int AnimDie    = Animator.StringToHash("Die");

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _anim  = GetComponent<Animator>();

        _audio              = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake  = false;
        _audio.spatialBlend = 1f;
        _audio.maxDistance  = 20f;
    }

    void OnValidate()
    {
        detectionRange = Mathf.Max(0.1f, detectionRange);
        attackRange    = Mathf.Max(0.1f, attackRange);
        loseRange      = Mathf.Max(detectionRange, loseRange);
        chaseSpeed     = Mathf.Max(0f, chaseSpeed);
        patrolSpeed    = Mathf.Max(0f, patrolSpeed);
        attackCooldown = Mathf.Max(0.05f, attackCooldown);
        eyeHeight      = Mathf.Max(0f, eyeHeight);
        // checkEveryFrames-подобных делений тут нет, но оставляем значения безопасными
    }

    void Start()
    {
        if (playerTransform == null)
        {
            var go = GameObject.FindWithTag("Player")
                  ?? GameObject.Find("XR Origin (XR Rig)");
            if (go != null) playerTransform = go.transform;
        }

        if (playerTransform != null)
            _playerHealth = playerTransform.GetComponentInChildren<PlayerHealth>()
                         ?? playerTransform.GetComponent<PlayerHealth>();

        if (glitchOverlay == null && _playerHealth != null)
            glitchOverlay = _playerHealth.glitchOverlay;

        GoPatrol();
    }

    void Update()
    {
        if (_state == State.Dead) return;

        _attackTimer -= Time.deltaTime;

        switch (_state)
        {
            case State.Patrol: UpdatePatrol(); break;
            case State.Chase:  UpdateChase();  break;
            case State.Attack: UpdateAttack(); break;
        }

        _anim.SetFloat(AnimSpeed, _agent.velocity.magnitude);
    }

    // ── состояния ────────────────────────────────────────────────────────────

    void UpdatePatrol()
    {
        if (playerTransform != null && IsPlayerDetected())
        {
            EnterChase();
            return;
        }

        if (patrolPoints == null || patrolPoints.Length == 0) return;

        if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
        {
            _patrolWaitTimer -= Time.deltaTime;
            if (_patrolWaitTimer <= 0f)
                MoveToNextPatrolPoint();
        }
    }

    void UpdateChase()
    {
        if (playerTransform == null) { GoPatrol(); return; }

        float dist = DistToPlayer();

        if (dist > loseRange)          { GoPatrol(); return; }
        if (dist <= attackRange)       { EnterAttack(); return; }

        // Мягкая "атмосфера" пока преследует (не спамим событиями — только меняем интенсивность)
        if (_hasDetected) glitchOverlay?.SetGlitchIntensity(chaseGlitchIntensity);
        _agent.SetDestination(playerTransform.position);
    }

    void UpdateAttack()
    {
        if (playerTransform == null)   { GoPatrol(); return; }

        float dist = DistToPlayer();

        // Повернуться к игроку
        Vector3 dir = playerTransform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(dir),
                360f * Time.deltaTime);

        if (dist > attackRange * 1.3f) // небольшой гистерезис
        {
            EnterChase();
            return;
        }

        if (_attackTimer <= 0f)
        {
            DoAttack();
        }
    }

    // ── атака ────────────────────────────────────────────────────────────────

    void DoAttack()
    {
        _attackTimer = attackCooldown;
        _anim.SetTrigger(AnimAttack);
        if (attackSound != null) _audio.PlayOneShot(attackSound);
        glitchOverlay?.SetGlitchIntensity(attackGlitchIntensity);
        OnAttack.Invoke();

        if (!useAnimationEventForDamage)
        {
            // Если хочешь попадание строго по кадру удара — включи useAnimationEventForDamage
            // и добавь AnimationEvent → DealDamageFromAnimation() в клип атаки.
            if (_playerHealth != null && !_playerHealth.IsDead)
                _playerHealth.TakeDamage(damagePerHit, transform);
        }
    }

    /// <summary>
    /// Вызывается из AnimationEvent на кадре удара (опционально).
    /// Если используешь AnimationEvent — убери DealDamage из DoAttack.
    /// </summary>
    public void DealDamageFromAnimation()
    {
        if (_state == State.Dead) return;
        if (_playerHealth != null && !_playerHealth.IsDead)
            _playerHealth.TakeDamage(damagePerHit, transform);
    }

    // ── переходы ─────────────────────────────────────────────────────────────

    void GoPatrol()
    {
        _state = State.Patrol;
        _agent.speed = patrolSpeed;
        _agent.isStopped = false;
        _agent.stoppingDistance = 0f;
        if (_hasDetected)
        {
            _hasDetected = false;
            if (loseSound != null) _audio.PlayOneShot(loseSound);
            glitchOverlay?.SetGlitchIntensity(0f);
            OnPlayerLost.Invoke();
        }
        MoveToNextPatrolPoint();
    }

    void EnterChase()
    {
        _state = State.Chase;
        _agent.speed     = chaseSpeed;
        _agent.isStopped = false;
        _agent.stoppingDistance = Mathf.Max(0.05f, attackRange * 0.85f);
        if (!_hasDetected)
        {
            _hasDetected = true;
            if (detectSound != null) _audio.PlayOneShot(detectSound);
            glitchOverlay?.SetGlitchIntensity(detectGlitchIntensity);
            OnPlayerDetected.Invoke();
        }
    }

    void EnterAttack()
    {
        _state = State.Attack;
        _agent.isStopped = true;
        _agent.ResetPath();
        if (_attackTimer <= 0f) DoAttack();
    }

    public void Die()
    {
        if (_state == State.Dead) return;
        _state = State.Dead;
        _agent.isStopped = true;
        _anim.SetTrigger(AnimDie);
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        glitchOverlay?.SetGlitchIntensity(0f);
        OnDied.Invoke();
        Destroy(gameObject, 4f);
    }

    // ── вспомогательное ──────────────────────────────────────────────────────

    void MoveToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        _agent.SetDestination(patrolPoints[_patrolIndex].position);
        _patrolIndex    = (_patrolIndex + 1) % patrolPoints.Length;
        _patrolWaitTimer = PatrolWait;
    }

    float DistToPlayer() =>
        playerTransform == null ? float.MaxValue :
        Vector3.Distance(transform.position, playerTransform.position);

    bool IsPlayerDetected()
    {
        if (playerTransform == null) return false;

        float sqrDist = (playerTransform.position - transform.position).sqrMagnitude;
        float sqrRange = detectionRange * detectionRange;
        if (sqrDist > sqrRange) return false;

        if (!requireLineOfSight) return true;

        Vector3 from = eye != null
            ? eye.position
            : transform.position + Vector3.up * Mathf.Max(0f, eyeHeight);

        Vector3 to = playerTransform.position;
        Vector3 dir = to - from;
        float   dist = dir.magnitude;
        if (dist < 0.001f) return true;
        dir /= dist;

        // Если луч упёрся в препятствие раньше игрока — не видим.
        // Важно: lineOfSightMask настрой так, чтобы Player был исключён из маски.
        return !Physics.Raycast(from, dir, dist, lineOfSightMask, QueryTriggerInteraction.Ignore);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, loseRange);
    }
}
