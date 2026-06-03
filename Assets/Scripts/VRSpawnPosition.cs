using System.Collections;
using UnityEngine;
using Unity.XR.CoreUtils;

/// <summary>
/// Решает проблему неправильного появления в VR.
///
/// Суть: в шлеме камера = XR Origin + физическое положение головы в комнате.
/// Этот скрипт после инициализации трекинга сдвигает XR Origin так,
/// чтобы камера оказалась точно в нужной точке карты.
///
/// Привяжи к XR Origin (XR Rig). SpawnPoint — пустой Transform в нужной точке сцены.
/// </summary>
[RequireComponent(typeof(XROrigin))]
public class VRSpawnPosition : MonoBehaviour
{
    [Header("Точка появления")]
    [Tooltip("Пустой GameObject на нужной позиции. Если не задан — берёт текущую позицию XR Origin")]
    public Transform spawnPoint;

    [Header("Настройки")]
    [Tooltip("Секунд ждать после старта перед коррекцией (нужно чтобы трекинг успел инициализироваться)")]
    public float initDelay = 0.5f;
    [Tooltip("Исправлять только XZ, оставить Y как есть (рекомендуется для Floor tracking)")]
    public bool correctXZOnly = true;

    XROrigin  _xrOrigin;
    Transform _camera;

    void Awake()
    {
        _xrOrigin = GetComponent<XROrigin>();
        _camera   = _xrOrigin.Camera.transform;
    }

    void Start()
    {
        StartCoroutine(CorrectAfterDelay());
    }

    IEnumerator CorrectAfterDelay()
    {
        yield return new WaitForSeconds(initDelay);

        Vector3 target = spawnPoint != null ? spawnPoint.position : transform.position;

        // XROrigin.MoveCameraToWorldLocation сдвигает Origin так чтобы
        // камера (голова) оказалась ровно в target точке
        if (correctXZOnly)
        {
            // Только XZ — Y оставляем для Floor tracking (высота берётся из реального роста)
            Vector3 currentCamWorld = _camera.position;
            Vector3 offset = new Vector3(
                target.x - currentCamWorld.x,
                0f,
                target.z - currentCamWorld.z
            );
            transform.position += offset;
        }
        else
        {
            _xrOrigin.MoveCameraToWorldLocation(target);
        }

        Debug.Log($"[VRSpawnPosition] Игрок скорректирован → камера в {_camera.position}");
    }
}
