using System.Collections;
using UnityEngine;

/// <summary>
/// Locked door that can be unlocked via TerminalHack.
/// Attach to any door mesh object. Rotation happens around the object's local Y axis.
/// </summary>
public class DoorController : MonoBehaviour
{
    [Header("Door Settings")]
    public bool isLocked = true;
    [Tooltip("How many degrees to rotate when opening (negative = opens other direction)")]
    public float openAngle = 90f;
    [Tooltip("Seconds the open animation takes")]
    public float openDuration = 1.2f;

    bool _isOpen      = false;
    bool _isAnimating = false;
    Quaternion _closedRot;
    Quaternion _openRot;

    void Start()
    {
        _closedRot = transform.localRotation;
        _openRot   = _closedRot * Quaternion.Euler(0f, openAngle, 0f);

        // Non-convex MeshColliders cannot move — make them convex so they rotate with the door
        foreach (var mc in GetComponentsInChildren<MeshCollider>())
            mc.convex = true;
    }

    /// <summary>Called by TerminalHack after successful hack.</summary>
    public void Unlock()
    {
        isLocked = false;
    }

    /// <summary>Starts the open animation if door is unlocked and not already open.</summary>
    public void OpenDoor()
    {
        if (isLocked || _isOpen || _isAnimating) return;
        StartCoroutine(AnimateDoor());
    }

    /// <summary>Unlock + open in one call.</summary>
    public void UnlockAndOpen()
    {
        Unlock();
        OpenDoor();
    }

    IEnumerator AnimateDoor()
    {
        _isAnimating = true;
        Quaternion start = transform.localRotation;
        float elapsed = 0f;

        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / openDuration);
            transform.localRotation = Quaternion.Slerp(start, _openRot, t);
            yield return null;
        }

        transform.localRotation = _openRot;
        _isOpen      = true;
        _isAnimating = false;
    }
}
