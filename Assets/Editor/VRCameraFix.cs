using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEditor;

/// <summary>
/// Editor-утилита. Запусти через меню: VR Tools → Fix VR Camera Head Tracking
/// Проверяет и исправляет TrackedPoseDriver на Main Camera.
/// </summary>
public static class VRCameraFix
{
    [MenuItem("VR Tools/Fix VR Camera Head Tracking")]
    static void Fix()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            EditorUtility.DisplayDialog("VR Camera Fix",
                "Main Camera не найдена!\n\nУбедись что на камере стоит тег 'MainCamera'.", "OK");
            return;
        }

        bool changed = false;
        string report = $"Камера: '{cam.gameObject.name}'\n\n";

        // ── 1. Проверяем TrackedPoseDriver ─────────────────────────────────
        var tpd = cam.GetComponent<TrackedPoseDriver>();
        if (tpd == null)
        {
            tpd = cam.gameObject.AddComponent<TrackedPoseDriver>();

            // Привязка к HMD через прямые device bindings
            var posAction = new InputAction("HMD Position",
                binding: "<XRHMD>/centerEyePosition");
            var rotAction = new InputAction("HMD Rotation",
                binding: "<XRHMD>/centerEyeRotation");

            tpd.positionInput = new InputActionProperty(posAction);
            tpd.rotationInput = new InputActionProperty(rotAction);
            tpd.trackingType  = TrackedPoseDriver.TrackingType.RotationAndPosition;
            tpd.updateType    = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;

            report  += "✅ TrackedPoseDriver — ДОБАВЛЕН (HMD bindings)\n";
            changed  = true;
        }
        else
        {
            report += "✅ TrackedPoseDriver — уже есть\n";

            // Проверяем тип трекинга
            if (tpd.trackingType != TrackedPoseDriver.TrackingType.RotationAndPosition)
            {
                tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
                report  += "   ⚠️ Tracking Type исправлен → RotationAndPosition\n";
                changed  = true;
            }
        }

        // ── 2. CameraFollow — перебивает TrackedPoseDriver ─────────────────
        // CameraFollow может отсутствовать в проекте; используем строковый GetComponent
        var cf = cam.GetComponent("CameraFollow") as Behaviour;
        if (cf != null && cf.enabled)
        {
            cf.enabled = false;
            report    += "⚠️ CameraFollow — ОТКЛЮЧЁН (он перебивал поворот камеры!)\n";
            changed    = true;
        }

        // ── 3. Проверяем иерархию XR Origin ───────────────────────────────
        bool hasXROrigin = false;
        Transform t = cam.transform.parent;
        while (t != null)
        {
            if (t.GetComponent<Unity.XR.CoreUtils.XROrigin>() != null)
            {
                hasXROrigin = true;
                break;
            }
            t = t.parent;
        }

        if (!hasXROrigin)
            report += "\n⚠️ ВНИМАНИЕ: Main Camera не является дочерней к XR Origin!\n" +
                      "   Иерархия должна быть:\n" +
                      "   XR Origin → Camera Offset → Main Camera\n";
        else
            report += "✅ Main Camera находится внутри XR Origin\n";

        // ── Итог ───────────────────────────────────────────────────────────
        if (changed)
        {
            EditorUtility.SetDirty(cam.gameObject);
            report += "\n✅ Изменения применены. Сохрани сцену (Ctrl+S).";
        }
        else
        {
            report += "\nВсё выглядит правильно. Если трекинг всё равно не работает — " +
                      "проверь Input Action References в TrackedPoseDriver вручную.";
        }

        Debug.Log("[VRCameraFix]\n" + report);
        EditorUtility.DisplayDialog("VR Camera Fix", report, "OK");
    }

    [MenuItem("VR Tools/Diagnose VR Camera (только отчёт)")]
    static void Diagnose()
    {
        var cam = Camera.main;
        if (cam == null) { Debug.LogError("Main Camera не найдена!"); return; }

        string r = $"=== VR Camera Диагностика: '{cam.gameObject.name}' ===\n";

        var tpd = cam.GetComponent<TrackedPoseDriver>();
        r += tpd != null
            ? $"[OK] TrackedPoseDriver: есть. Tracking={tpd.trackingType}, Update={tpd.updateType}\n"
            : "[!!] TrackedPoseDriver: ОТСУТСТВУЕТ — главная причина что камера не вращается!\n";

        var cf = cam.GetComponent("CameraFollow") as Behaviour;
        r += cf != null
            ? $"[!!] CameraFollow: {'е' + (cf.enabled ? "сть и ВКЛЮЧЁН — перебивает поворот!" : "сть но выключен")}\n"
            : "[OK] CameraFollow: нет\n";

        Transform p = cam.transform.parent;
        bool foundOrigin = false;
        while (p != null)
        {
            if (p.GetComponent<Unity.XR.CoreUtils.XROrigin>() != null) { foundOrigin = true; break; }
            p = p.parent;
        }
        r += foundOrigin ? "[OK] XR Origin: найден в иерархии\n" : "[!!] XR Origin: камера НЕ внутри XR Origin!\n";

        Debug.Log(r);
        EditorUtility.DisplayDialog("VR Camera Диагностика", r, "OK");
    }
}
