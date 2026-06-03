using UnityEngine;

public class HandFollowVR : MonoBehaviour
{
    [Header("VR Controller")]
    public Transform targetController;  // Left Controller или Right Controller

    [Header("Настройки")]
    public float positionSpeed = 15f;
    public float rotationSpeed = 15f;

    void Update()
    {
        if (targetController == null) return;

        // Плавно следуем за контроллером
        transform.position = Vector3.Lerp(transform.position, targetController.position, Time.deltaTime * positionSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetController.rotation, Time.deltaTime * rotationSpeed);
    }
}
