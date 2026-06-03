using UnityEngine;

public class VRBodyFollowHead : MonoBehaviour
{
    [Header("Ссылки")]
    public Transform vrCamera;        // Main Camera
    public Transform bodyRoot;        // Корень тела (например, hips/spine)

    [Header("Настройки")]
    public float followSpeed = 10f;
    public float rotationSpeed = 5f;
    public float heightOffset = -1.7f; // Разница высоты камеры и тела

    void Update()
    {
        if (vrCamera == null || bodyRoot == null) return;

        // Позиция тела под камерой
        Vector3 targetPos = vrCamera.position;
        targetPos.y += heightOffset; // Опускаем тело ниже камеры

        // Плавно двигаем тело
        bodyRoot.position = Vector3.Lerp(bodyRoot.position, targetPos, Time.deltaTime * followSpeed);

        // Поворот тела по камере (только горизонтально)
        Vector3 camForward = vrCamera.forward;
        camForward.y = 0;

        if (camForward.magnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(camForward);
            bodyRoot.rotation = Quaternion.Slerp(bodyRoot.rotation, targetRot, Time.deltaTime * rotationSpeed);
        }
    }
}