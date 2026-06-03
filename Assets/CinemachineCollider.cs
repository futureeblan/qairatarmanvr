using UnityEngine;

public class VRBodyPushback : MonoBehaviour
{
    [Header("���������")]
    public Transform cameraTransform;      // ������� ������ (VR)
    public CharacterController body;      // ���� ���������
    public float minHeadDistance = 0.3f;  // ����������� ���������� �� �����
    public float pushbackSpeed = 5f;      // �������� ������������

    void Update()
    {
        if (cameraTransform == null || body == null) return;

        if (Physics.CheckSphere(cameraTransform.position, minHeadDistance))
        {
            // ����������� �� ������ � ����
            Vector3 pushDir = (body.transform.position - cameraTransform.position).normalized;
            pushDir.y = 0; // ������ �������������

            // ������� ����, �� ������!
            body.Move(pushDir * pushbackSpeed * Time.deltaTime);
        }
    }
}