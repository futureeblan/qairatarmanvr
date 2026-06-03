using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// AI робота с конечным автоматом (FSM): Idle → Chase → Attack
/// Использует NavMeshAgent для навигации и преследования игрока.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class RobotAI : MonoBehaviour
{
    [Header("Настройки дистанций")]
    [SerializeField] private float idleDistance = 10f;      // Дистанция для состояния Idle
    [SerializeField] private float attackDistance = 2f;    // Дистанция для состояния Attack
    [SerializeField] private float rotationSpeed = 5f;     // Скорость поворота к игроку

    [Header("Настройки атаки")]
    [SerializeField] private float attackCooldown = 1.5f;  // Задержка между атаками
    [SerializeField] private int damageAmount = 10;          // Урон за одну атаку

    [Header("Ссылки")]
    [SerializeField] private Transform playerTransform;   // Ссылка на игрока (автопоиск если пусто)

    private NavMeshAgent _navMeshAgent;
    private Animator _animator;
    private enum State { Idle, Chase, Attack }
    private State _currentState = State.Idle;
    private float _attackTimer;
    private PlayerHealth _playerHealth;

    void Awake()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
    }

    void Start()
    {
        // Автоматический поиск игрока по тегу если ссылка не задана
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                // Попытка найти XR Origin для VR
                GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
                if (xrOrigin != null)
                {
                    playerTransform = xrOrigin.transform;
                }
            }
        }

        // Получаем компонент здоровья игрока
        if (playerTransform != null)
        {
            _playerHealth = playerTransform.GetComponent<PlayerHealth>();
        }

        _attackTimer = attackCooldown;
    }

    void Update()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Обновление таймера атаки
        if (_attackTimer > 0)
        {
            _attackTimer -= Time.deltaTime;
        }

        // Конечный автомат (FSM)
        switch (_currentState)
        {
            case State.Idle:
                UpdateIdleState(distanceToPlayer);
                break;
            case State.Chase:
                UpdateChaseState(distanceToPlayer);
                break;
            case State.Attack:
                UpdateAttackState(distanceToPlayer);
                break;
        }

        // Плавный поворот к игроку во всех состояниях
        RotateTowardsPlayer();
    }

    /// <summary>
    /// Состояние Idle: робот стоит на месте, игрок далеко (> 10м)
    /// </summary>
    private void UpdateIdleState(float distance)
    {
        _navMeshAgent.isStopped = true;

        // Переход в Chase если игрок приблизился
        if (distance <= idleDistance && distance > attackDistance)
        {
            ChangeState(State.Chase);
        }
        // Переход в Attack если игрок очень близко
        else if (distance <= attackDistance)
        {
            ChangeState(State.Attack);
        }
    }

    /// <summary>
    /// Состояние Chase: робот преследует игрока (2м < дистанция <= 10м)
    /// </summary>
    private void UpdateChaseState(float distance)
    {
        _navMeshAgent.isStopped = false;
        _navMeshAgent.SetDestination(playerTransform.position);

        // Переход в Idle если игрок отдалился
        if (distance > idleDistance)
        {
            ChangeState(State.Idle);
        }
        // Переход в Attack если игрок приблизился
        else if (distance <= attackDistance)
        {
            ChangeState(State.Attack);
        }
    }

    /// <summary>
    /// Состояние Attack: робот атакует игрока (дистанция <= 2м)
    /// </summary>
    private void UpdateAttackState(float distance)
    {
        _navMeshAgent.isStopped = true;

        // Атака с кулдауном
        if (_attackTimer <= 0f)
        {
            AttackPlayer();
            _attackTimer = attackCooldown;
        }

        // Переход в Chase если игрок отошел
        if (distance > attackDistance && distance <= idleDistance)
        {
            ChangeState(State.Chase);
        }
        // Переход в Idle если игрок далеко
        else if (distance > idleDistance)
        {
            ChangeState(State.Idle);
        }
    }

    /// <summary>
    /// Нанесение урона игроку
    /// </summary>
    private void AttackPlayer()
    {
        if (_playerHealth != null)
        {
            _playerHealth.TakeDamage(damageAmount);
            Debug.Log($"[RobotAI] Атаковано игрока! Урон: {damageAmount}");
        }
        else
        {
            Debug.LogWarning("[RobotAI] PlayerHealth не найден на игроке!");
        }

        // Запуск анимации атаки если есть Animator
        if (_animator != null)
        {
            _animator.SetTrigger("Attack");
        }
    }

    /// <summary>
    /// Плавный поворот робота к игроку
    /// </summary>
    private void RotateTowardsPlayer()
    {
        if (playerTransform == null) return;

        Vector3 directionToPlayer = playerTransform.position - transform.position;
        directionToPlayer.y = 0; // Игнорируем разницу по высоте

        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Смена состояния с логированием
    /// </summary>
    private void ChangeState(State newState)
    {
        if (_currentState != newState)
        {
            Debug.Log($"[RobotAI] Состояние изменено: {_currentState} → {newState}");
            _currentState = newState;
        }
    }

    /// <summary>
    /// Отрисовка гизмо в редакторе для визуализации дистанций
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Зона атаки (красная)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);

        // Зона преследования (желтая)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, idleDistance);

        // Линия к игроку
        if (playerTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }
    }
}
