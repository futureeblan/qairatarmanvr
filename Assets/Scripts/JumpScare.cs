using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Скример-триггер. Срабатывает один раз когда игрок входит в зону.
/// Настройка:
///   1. Добавь Collider (Is Trigger = true) на этот GameObject.
///   2. screamerImage  — полноэкранный Image на Canvas-оверлее камеры.
///   3. screamerClip   — звук скримера (AudioClip).
///   4. (опц.) screamerObject — 3D-объект/анимация который появляется и исчезает.
/// </summary>
public class JumpScare : MonoBehaviour
{
    [Header("Скример")]
    public Image      screamerImage;   // 2D полноэкранный
    public GameObject screamerObject;  // 3D-объект (опционально)
    public AudioClip  screamerClip;
    public float      displayDuration = 0.65f;

    [Header("Глитч")]
    [Tooltip("Можно подключить тот же GlitchOverlay что у PlayerHealth")]
    public GlitchOverlay glitchOverlay;
    public float glitchDuration = 1.2f;

    [Header("Настройки")]
    public bool destroyAfterFire = true;

    AudioSource _audio;
    Transform   _xrOrigin;
    bool        _fired;

    void Awake()
    {
        _audio              = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake  = false;
        _audio.spatialBlend = 0f;   // 2D — скример слышен без затухания по дистанции
        _audio.priority     = 0;    // максимальный приоритет
    }

    void Start()
    {
        var go = GameObject.FindWithTag("Player") ?? GameObject.Find("XR Origin (XR Rig)");
        if (go != null) _xrOrigin = go.transform;

        if (screamerImage  != null) screamerImage.enabled  = false;
        if (screamerObject != null) screamerObject.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (_fired) return;
        if (!IsPlayer(other)) return;
        Fire();
    }

    /// <summary>Можно вызвать вручную из Timeline или другого скрипта.</summary>
    public void Fire()
    {
        if (_fired) return;
        _fired = true;

        if (screamerClip != null)
        {
            _audio.clip = screamerClip;
            _audio.Play();
        }

        if (screamerImage != null)
            screamerImage.enabled = true;

        if (screamerObject != null)
            screamerObject.SetActive(true);

        glitchOverlay?.SetGlitchIntensity(0.8f);

        Invoke(nameof(HideScreamer), displayDuration);
        Invoke(nameof(StopGlitch),   glitchDuration);
    }

    void HideScreamer()
    {
        if (screamerImage  != null) screamerImage.enabled  = false;
        if (screamerObject != null) screamerObject.SetActive(false);

        if (destroyAfterFire)
            Destroy(gameObject, 0.1f);
    }

    void StopGlitch() => glitchOverlay?.SetGlitchIntensity(0f);

    bool IsPlayer(Collider col)
    {
        return col.CompareTag("Player") ||
               (_xrOrigin != null && (col.transform == _xrOrigin ||
                                      col.transform.IsChildOf(_xrOrigin)));
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.3f);
        var col = GetComponent<Collider>();
        if (col != null) Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
}
