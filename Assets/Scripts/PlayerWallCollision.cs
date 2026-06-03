using UnityEngine;

/// <summary>
/// Prevents the player from tunneling through walls in VR.
/// Handles both joystick movement (CharacterController depenetration)
/// and room-scale head movement (head sphere overlap push-back).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerWallCollision : MonoBehaviour
{
    [Tooltip("Radius of the sphere used for room-scale head collision detection")]
    public float headRadius = 0.13f;

    [Tooltip("Layers considered solid walls (Default + StaticEnvironment by default)")]
    public LayerMask wallLayers = 1;

    CharacterController _cc;
    Transform _cam;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _cam = Camera.main != null ? Camera.main.transform : null;
    }

    void LateUpdate()
    {
        Depenetrate();

        if (_cam != null)
            PushBackRoomScale();
    }

    // Push the CharacterController out of any wall it overlaps (fixes tunneling)
    void Depenetrate()
    {
        Vector3 center = transform.TransformPoint(_cc.center);
        float halfH    = _cc.height * 0.5f - _cc.radius;
        Vector3 top    = center + Vector3.up * halfH;
        Vector3 bottom = center - Vector3.up * halfH;
        float   radius = _cc.radius + _cc.skinWidth;

        Collider[] overlaps = Physics.OverlapCapsule(
            bottom, top, radius, wallLayers, QueryTriggerInteraction.Ignore);

        foreach (Collider col in overlaps)
        {
            if (col.transform.IsChildOf(transform)) continue;

            if (Physics.ComputePenetration(
                    _cc,  transform.position,       transform.rotation,
                    col,  col.transform.position,   col.transform.rotation,
                    out Vector3 dir, out float depth))
            {
                transform.position += dir * (depth + 0.001f);
            }
        }
    }

    // Room-scale: if the head physically enters a wall, push the whole rig back
    void PushBackRoomScale()
    {
        Collider[] hits = Physics.OverlapSphere(
            _cam.position, headRadius, wallLayers, QueryTriggerInteraction.Ignore);

        foreach (Collider col in hits)
        {
            if (col.transform.IsChildOf(transform)) continue;

            if (Physics.ComputePenetration(
                    _cc,  transform.position,       transform.rotation,
                    col,  col.transform.position,   col.transform.rotation,
                    out Vector3 dir, out float depth))
            {
                transform.position += dir * (depth + 0.001f);
            }
        }
    }
}
