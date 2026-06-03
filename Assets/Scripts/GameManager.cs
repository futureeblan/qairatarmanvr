using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Синглтон. Управляет перезапуском/чекпоинтами.
/// 1. Повесь на пустой GameObject "GameManager" в сцене.
/// 2. Подпиши PlayerHealth.OnDeathFadeComplete → GameManager.OnPlayerDied
/// 3. (опц.) Разбросай CheckpointTrigger-зоны по уровню.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("XR Rig")]
    [Tooltip("Оставь пустым — найдётся автоматически")]
    public Transform xrOrigin;

    [Header("Смерть")]
    [Tooltip("true = перезагрузить сцену (проще). false = телепортировать к чекпоинту.")]
    public bool reloadSceneOnDeath = true;
    [Tooltip("Пауза перед перезапуском (секунды)")]
    public float restartDelay = 2f;

    Vector3    _spawnPos;
    Quaternion _spawnRot;
    bool       _checkpointSet;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start() => CacheXROrigin();

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CacheXROrigin();
    }

    void CacheXROrigin()
    {
        if (xrOrigin != null) return;

        var go = GameObject.FindWithTag("Player")
              ?? GameObject.Find("XR Origin (XR Rig)");
        if (go == null) return;

        xrOrigin = go.transform;

        // Первый старт — запомнить стартовую точку как дефолтный чекпоинт
        if (!_checkpointSet)
        {
            _spawnPos      = xrOrigin.position;
            _spawnRot      = xrOrigin.rotation;
            _checkpointSet = true;
        }
    }

    // ── вызывается из PlayerHealth.OnDeathFadeComplete ─────────────────────

    public void OnPlayerDied()
    {
        StartCoroutine(RestartRoutine());
    }

    IEnumerator RestartRoutine()
    {
        yield return new WaitForSeconds(restartDelay);

        if (reloadSceneOnDeath)
        {
            _checkpointSet = false; // после перезагрузки сброс к старту
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            RespawnAtCheckpoint();
        }
    }

    void RespawnAtCheckpoint()
    {
        CacheXROrigin();
        if (xrOrigin == null) return;

        xrOrigin.position = _spawnPos;
        xrOrigin.rotation = _spawnRot;

        // Сбросить здоровье через пересоздание компонента невозможно — перезагружай сцену
        // или добавь метод PlayerHealth.Reset() вручную
        var health = xrOrigin.GetComponent<PlayerHealth>();
        if (health != null)
            Debug.LogWarning("[GameManager] Respawn in-place: PlayerHealth не имеет Reset(). " +
                             "Включи reloadSceneOnDeath или добавь PlayerHealth.Reset().");
    }

    // ── чекпоинты ──────────────────────────────────────────────────────────

    /// <summary>Сохраняет текущую позицию XR Origin как точку возрождения.</summary>
    public void SaveCheckpoint()
    {
        CacheXROrigin();
        if (xrOrigin == null) return;
        _spawnPos      = xrOrigin.position;
        _spawnRot      = xrOrigin.rotation;
        _checkpointSet = true;
        Debug.Log($"[GameManager] Checkpoint saved at {_spawnPos}");
    }

    /// <summary>Установить точку возрождения вручную (из триггер-зоны).</summary>
    public void SetCheckpoint(Vector3 position, Quaternion rotation)
    {
        _spawnPos      = position;
        _spawnRot      = rotation;
        _checkpointSet = true;
    }
}
