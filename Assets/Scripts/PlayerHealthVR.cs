using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Система здоровья игрока с VR-спецификой: вибрация контроллера при получении урона.
/// Использует XR Interaction Toolkit для тактильной отдачи (haptic feedback).
/// </summary>
public class PlayerHealthVR : MonoBehaviour
{
    [Header("Настройки здоровья")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    [Header("VR Haptic Feedback")]
    [SerializeField] private float hapticAmplitude = 0.5f;      // Сила вибрации (0-1)
    [SerializeField] private float hapticDuration = 0.2f;       // Длительность вибрации (секунды)
    [SerializeField] private ActionBasedController leftController;  // Левый контроллер
    [SerializeField] private ActionBasedController rightController; // Правый контроллер

    [Header("События")]
    [SerializeField] private UnityEngine.Events.UnityEvent onDeath;
    [SerializeField] private UnityEngine.Events.UnityEvent<float> onDamageTaken;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0;

    void Start()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Нанесение урона игроку с VR-вибрацией контроллеров
    /// </summary>
    /// <param name="amount">Количество урона</param>
    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);

        // Логирование в консоль
        Debug.Log($"[PlayerHealthVR] Damage taken: {amount}, Current health: {currentHealth}/{maxHealth}");

        // Вызов вибрации контроллеров
        TriggerHapticFeedback();

        // Вызов события
        onDamageTaken?.Invoke(currentHealth);

        // Проверка смерти
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Вибрация обоих контроллеров через XR Interaction Toolkit
    /// </summary>
    private void TriggerHapticFeedback()
    {
        // Вибрация левого контроллера
        if (leftController != null)
        {
            leftController.SendHapticImpulse(hapticAmplitude, hapticDuration);
        }

        // Вибрация правого контроллера
        if (rightController != null)
        {
            rightController.SendHapticImpulse(hapticAmplitude, hapticDuration);
        }

        // Fallback: попытка найти контроллеры автоматически если ссылки не заданы
        if (leftController == null && rightController == null)
        {
            TryFindControllersAndVibrate();
        }
    }

    /// <summary>
    /// Автоматический поиск контроллеров и вибрация (fallback)
    /// </summary>
    private void TryFindControllersAndVibrate()
    {
        // Ищем все ActionBasedController в сцене
        ActionBasedController[] controllers = FindObjectsOfType<ActionBasedController>();

        foreach (var controller in controllers)
        {
            controller.SendHapticImpulse(hapticAmplitude, hapticDuration);
        }

        if (controllers.Length > 0)
        {
            Debug.Log($"[PlayerHealthVR] Haptic feedback sent to {controllers.Length} controller(s) (auto-found)");
        }
        else
        {
            Debug.LogWarning("[PlayerHealthVR] No controllers found for haptic feedback!");
        }
    }

    /// <summary>
    /// Смерть игрока
    /// </summary>
    private void Die()
    {
        Debug.Log("[PlayerHealthVR] Player died!");
        onDeath?.Invoke();
    }

    /// <summary>
    /// Исцеление игрока (опционально)
    /// </summary>
    public void Heal(float amount)
    {
        if (IsDead) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        Debug.Log($"[PlayerHealthVR] Healed: {amount}, Current health: {currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// Полный респаун (восстановление здоровья)
    /// </summary>
    public void Respawn()
    {
        currentHealth = maxHealth;
        Debug.Log("[PlayerHealthVR] Player respawned with full health");
    }
}
