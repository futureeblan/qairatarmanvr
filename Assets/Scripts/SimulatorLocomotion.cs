using UnityEngine;

/// <summary>
/// Routes WASD / arrow-key input through CharacterController so the player
/// cannot walk through walls when testing inside the XR Device Simulator.
/// Disable this component (or the whole GameObject) on device builds.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class SimulatorLocomotion : MonoBehaviour
{
    [Tooltip("Movement speed in m/s")]
    public float speed = 3f;

    CharacterController _cc;
    Transform _cam;

    void Awake()
    {
        _cc  = GetComponent<CharacterController>();
        _cam = Camera.main != null ? Camera.main.transform : null;
    }

    void Update()
    {
        if (_cam == null) return;

        float h = Input.GetAxis("Horizontal"); // A / D
        float v = Input.GetAxis("Vertical");   // W / S

        if (Mathf.Abs(h) < 0.001f && Mathf.Abs(v) < 0.001f) return;

        // Project camera directions onto the horizontal plane
        Vector3 forward = Vector3.Scale(_cam.forward, new Vector3(1, 0, 1)).normalized;
        Vector3 right   = Vector3.Scale(_cam.right,   new Vector3(1, 0, 1)).normalized;

        Vector3 move = (forward * v + right * h) * speed;
        // Apply movement + gravity through CharacterController (respects wall colliders)
        _cc.Move((move + Vector3.down * 9.81f) * Time.deltaTime);
    }
}
