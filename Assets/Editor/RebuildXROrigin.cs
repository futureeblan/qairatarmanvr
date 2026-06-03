using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Tools → VR Setup → Rebuild XR Origin
/// Deletes the old broken XR Origin and creates a clean one with correct Floor tracking.
/// </summary>
public static class RebuildXROrigin
{
    [MenuItem("Tools/VR Setup/Rebuild XR Origin (Clean)")]
    static void Rebuild()
    {
        // ── 1. Удалить старый XR Origin ──────────────────────────────────
        string[] oldNames = { "XR Origin (XR Rig)", "XR Origin", "XR Rig" };
        foreach (var n in oldNames)
        {
            var go = GameObject.Find(n);
            if (go != null)
            {
                Undo.DestroyObjectImmediate(go);
                Debug.Log($"[RebuildXROrigin] Deleted old '{n}'");
            }
        }

        // ── 2. Корень: XR Origin ──────────────────────────────────────────
        var root = new GameObject("XR Origin (XR Rig)");
        Undo.RegisterCreatedObjectUndo(root, "Rebuild XR Origin");
        root.tag = "Player";
        root.transform.position = new Vector3(58.813f, 0.13f, -5.76f);

        // CharacterController для движения без прохождения сквозь стены
        var cc = Undo.AddComponent<CharacterController>(root);
        cc.height   = 1.8f;
        cc.radius   = 0.16f;
        cc.center   = new Vector3(0f, 0.9f, 0f);
        cc.skinWidth  = 0.08f;
        cc.stepOffset = 0.3f;
        cc.slopeLimit = 45f;

        // ── 3. Camera Offset (Y=0 для Floor-режима) ───────────────────────
        var offsetGO = new GameObject("Camera Offset");
        Undo.RegisterCreatedObjectUndo(offsetGO, "Rebuild XR Origin");
        offsetGO.transform.SetParent(root.transform, false);
        offsetGO.transform.localPosition = Vector3.zero;

        // ── 4. Main Camera ────────────────────────────────────────────────
        var camGO = new GameObject("Main Camera");
        Undo.RegisterCreatedObjectUndo(camGO, "Rebuild XR Origin");
        camGO.tag = "MainCamera";
        camGO.transform.SetParent(offsetGO.transform, false);

        var cam = Undo.AddComponent<Camera>(camGO);
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane  = 1000f;
        cam.clearFlags    = CameraClearFlags.Skybox;

        Undo.AddComponent<UniversalAdditionalCameraData>(camGO);
        Undo.AddComponent<AudioListener>(camGO);

        // TrackedPoseDriver с правильными OpenXR-привязками
        var tpd = Undo.AddComponent<TrackedPoseDriver>(camGO);
        tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        tpd.updateType   = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;

        // Привязки: <XRHMD>/centerEyePosition и centerEyeRotation
        var posAction = new InputAction("Position", expectedControlType: "Vector3");
        posAction.AddBinding("<XRHMD>/centerEyePosition");
        posAction.AddBinding("<HandheldARInputDevice>/devicePosition");

        var rotAction = new InputAction("Rotation", expectedControlType: "Quaternion");
        rotAction.AddBinding("<XRHMD>/centerEyeRotation");
        rotAction.AddBinding("<HandheldARInputDevice>/deviceRotation");

        var stateAction = new InputAction("Tracking State", expectedControlType: "Integer");
        stateAction.AddBinding("<XRHMD>/trackingState");

        tpd.positionInput     = new InputActionProperty(posAction);
        tpd.rotationInput     = new InputActionProperty(rotAction);
        tpd.trackingStateInput = new InputActionProperty(stateAction);

        // ── 5. XROrigin — Floor режим ─────────────────────────────────────
        var xrOrigin = Undo.AddComponent<XROrigin>(root);
        xrOrigin.Camera                   = cam;
        xrOrigin.CameraFloorOffsetObject  = offsetGO;
        xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
        xrOrigin.CameraYOffset            = 0f;

        // ── 6. Left Controller ────────────────────────────────────────────
        var leftGO = new GameObject("Left Controller");
        Undo.RegisterCreatedObjectUndo(leftGO, "Rebuild XR Origin");
        leftGO.transform.SetParent(offsetGO.transform, false);
        var leftCtrl = Undo.AddComponent<ActionBasedController>(leftGO);
        leftCtrl.enableInputTracking = true;

        // ── 7. Right Controller ───────────────────────────────────────────
        var rightGO = new GameObject("Right Controller");
        Undo.RegisterCreatedObjectUndo(rightGO, "Rebuild XR Origin");
        rightGO.transform.SetParent(offsetGO.transform, false);
        var rightCtrl = Undo.AddComponent<ActionBasedController>(rightGO);
        rightCtrl.enableInputTracking = true;

        // ── 8. Locomotion ─────────────────────────────────────────────────
        var locoSys = Undo.AddComponent<LocomotionSystem>(root);
        locoSys.xrOrigin = xrOrigin;

        var move = Undo.AddComponent<ActionBasedContinuousMoveProvider>(root);
        move.system       = locoSys;
        move.moveSpeed    = 3f;
        move.forwardSource = camGO.transform;

        var turn = Undo.AddComponent<ActionBasedSnapTurnProvider>(root);
        turn.system     = locoSys;
        turn.turnAmount = 45f;

        var driver = Undo.AddComponent<CharacterControllerDriver>(root);
        driver.locomotionProvider = move;

        // ── 9. VRFloorSetup страховка ─────────────────────────────────────
        root.AddComponent<VRFloorSetup>();

        // ── 10. Финал ─────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = root;

        Debug.Log("[RebuildXROrigin] ✓ Done! Press Ctrl+S to save the scene, then rebuild APK.");
    }
}
