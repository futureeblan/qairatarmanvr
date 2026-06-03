using UnityEngine;
using UnityEngine.AI;

public class MechAI : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform player;

    [Header("Distance Mechanics")]
    public float maxDistance = 10f; // ��������� ��� ��������, 1 ���� � ��� ������� ����!
    public float spawnInFrontDistance = 4f;

    private NavMeshAgent agent;
    private Animator animator;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // ��������: ����� �� ����� �� NavMesh ��� ������
        if (agent != null && !agent.isOnNavMesh)
        {
            Debug.LogError($"������ {gameObject.name} ����� ��� NavMesh! ��������� ��� � ���������.");
        }
    }

    void Update()
    {
        // 1. �������� �� ������� ������
        if (player == null || agent == null || !agent.isOnNavMesh) return;

        // 2. ���������� ������ (������ ���� ����� �������)
        agent.SetDestination(player.position);

        // 3. �������� (����������: ���������, ��� � Animator �������� ���������� "Speed")
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }

        // 4. �������� ��������� ��� ������������
        float currentDistance = Vector3.Distance(transform.position, player.position);

        if (currentDistance > maxDistance)
        {
            TeleportInFront();
        }
    }

    void TeleportInFront()
    {
        // ������������ ����� ����� ����� ������
        Vector3 targetPos = player.position + (player.forward * spawnInFrontDistance);

        NavMeshHit hit;
        // 5f � ������ ������ ���������� ����� �� �����
        if (NavMesh.SamplePosition(targetPos, out hit, 5f, NavMesh.AllAreas))
        {
            // Warp � ���������� ������ ����������� ����������� ������
            agent.Warp(hit.position);

            // ������� � ������
            Vector3 lookAtPos = new Vector3(player.position.x, transform.position.y, player.position.z);
            transform.LookAt(lookAtPos);

            Debug.Log("<color=red>�������������:</color> ���� ��� �� ��������?");
        }
    }
}