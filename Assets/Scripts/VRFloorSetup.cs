using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Unity.XR.CoreUtils;

// Attach to XR Origin. Forces floor-level tracking at runtime and logs diagnostics via ADB logcat.
[DefaultExecutionOrder(-100)]
public class VRFloorSetup : MonoBehaviour
{
    void Start()
    {
        ForceFloorTracking();
        LogDiagnostics();
    }

    void ForceFloorTracking()
    {
        var subsystems = new List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);

        if (subsystems.Count == 0)
        {
            Debug.LogWarning("[VRFloorSetup] No XRInputSubsystem found. XR may not be initialized yet.");
            Invoke(nameof(ForceFloorTracking), 1f);
            return;
        }

        foreach (var subsystem in subsystems)
        {
            bool success = subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
            Debug.Log($"[VRFloorSetup] TrySetTrackingOriginMode(Floor) = {success} on {subsystem.GetType().Name}");

            if (success)
                subsystem.TryRecenter();
        }

        // Also set via XROrigin component
        var origin = GetComponent<XROrigin>();
        if (origin != null)
        {
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            origin.CameraYOffset = 0f;
            Debug.Log("[VRFloorSetup] XROrigin.RequestedTrackingOriginMode set to Floor");
        }
    }

    void LogDiagnostics()
    {
        // Log which XR loader is running
        var xrSettings = UnityEngine.XR.Management.XRGeneralSettings.Instance;
        if (xrSettings != null && xrSettings.Manager != null && xrSettings.Manager.activeLoader != null)
            Debug.Log($"[VRFloorSetup] Active XR Loader: {xrSettings.Manager.activeLoader.GetType().Name}");
        else
            Debug.LogWarning("[VRFloorSetup] XR Manager or active loader is null.");

        // Log TrackedPoseDriver state
        var cam = Camera.main;
        if (cam != null)
        {
            var tpd = cam.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
            Debug.Log(tpd != null
                ? $"[VRFloorSetup] TrackedPoseDriver found on '{cam.name}', enabled={tpd.enabled}"
                : $"[VRFloorSetup] WARNING: No TrackedPoseDriver on Main Camera '{cam.name}'");
        }

        // Log HMD presence
        var hmdDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, hmdDevices);
        Debug.Log($"[VRFloorSetup] HMD devices found: {hmdDevices.Count}");
        foreach (var d in hmdDevices)
            Debug.Log($"[VRFloorSetup]   HMD: {d.name}, isValid={d.isValid}");
    }
}
