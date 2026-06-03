using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CharacterController))]
public class PlayerHealth : MonoBehaviour
{
    [System.Serializable] public class IntEvent : UnityEvent<int> { }

    [Header("Здоровье")]
    public int   maxHealth      = 3;
    [Tooltip("Секунд неуязвимости после каждого удара")]
    public float damageCooldown = 1.5f;

    [Header("Удар — визуал")]
    [Tooltip("CanvasGroup красного оверлея на камере (отдельный от fadeCanvas)")]
    public CanvasGroup hitFlashCanvas;
    public float       hitFlashDuration = 0.35f;

    [Header("Смерть — тайминги")]
    public float lookDuration        = 0.4f;
    public float screamerHoldSeconds = 0.8f;
    public float fadeDuration        = 2.0f;

    [Header("VR References")]
    public Transform     xrRig;
    [Tooltip("CanvasGroup чёрного фейда при смерти")]
    public CanvasGroup   fadeCanvas;
    public GlitchOverlay glitchOverlay;

    [Header("Локомоция")]
    public MonoBehaviour[] locomotionProviders;

    [Header("Звуки")]
    public AudioClip hitSound;
    public AudioClip deathSound;

    [Header("События")]
    public IntEvent    OnTakeDamage;        // arg = текущее здоровье после удара
    public UnityEvent  OnPlayerDeath;
    public UnityEvent  OnDeathFadeComplete;

    int              _currentHealth;
    bool             _isDead;
    bool             _invincible;
    CharacterController _cc;
    AudioSource      _audio;

    public int  CurrentHealth => _currentHealth;
    public bool IsDead        => _isDead;

    void Awake()
    {
        _cc             = GetComponent<CharacterController>();
        _currentHealth  = maxHealth;

        _audio              = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake  = false;
        _audio.spatialBlend = 0f;
        _audio.priority     = 0;
    }

    void Start()
    {
        if (fadeCanvas != null)
        {
            fadeCanvas.alpha = 0f;
            fadeCanvas.gameObject.SetActive(false);
        }
        if (hitFlashCanvas != null)
        {
            hitFlashCanvas.alpha = 0f;
            hitFlashCanvas.gameObject.SetActive(false);
        }
    }

    // ── урон ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Нанести урон игроку. attacker — Transform врага (передаётся в TriggerDeath).
    /// </summary>
    public void TakeDamage(int amount, Transform attacker = null)
    {
        if (_isDead || _invincible) return;

        _currentHealth = Mathf.Max(0, _currentHealth - amount);
        OnTakeDamage.Invoke(_currentHealth);

        if (hitSound != null) _audio.PlayOneShot(hitSound);

        if (_currentHealth == 0)
        {
            TriggerDeath(attacker);
        }
        else
        {
            StartCoroutine(HitFlash());
            StartCoroutine(InvincibilityWindow());
        }
    }

    // Версия без аттакера — для совместимости с UnityEvent в инспекторе
    public void TakeDamage(int amount) => TakeDamage(amount, null);

    // ── смерть ──────────────────────────────────────────────────────────────

    public void TriggerDeath(Transform killer)
    {
        if (_isDead) return;
        _isDead        = true;
        _currentHealth = 0;
        if (deathSound != null) _audio.PlayOneShot(deathSound);
        StartCoroutine(DeathSequence(killer));
    }

    // ── сброс (для GameManager.RespawnAtCheckpoint) ─────────────────────────

    public void Reset()
    {
        StopAllCoroutines();
        _isDead        = false;
        _invincible    = false;
        _currentHealth = maxHealth;

        _cc.enabled    = true;

        if (fadeCanvas != null)
        {
            fadeCanvas.alpha = 0f;
            fadeCanvas.gameObject.SetActive(false);
        }
        if (hitFlashCanvas != null)
        {
            hitFlashCanvas.alpha = 0f;
            hitFlashCanvas.gameObject.SetActive(false);
        }
        glitchOverlay?.SetGlitchIntensity(0f);
    }

    // ── корутины ────────────────────────────────────────────────────────────

    IEnumerator HitFlash()
    {
        if (hitFlashCanvas == null) yield break;

        hitFlashCanvas.gameObject.SetActive(true);
        hitFlashCanvas.alpha = 1f;

        float elapsed = 0f;
        while (elapsed < hitFlashDuration)
        {
            elapsed             += Time.deltaTime;
            hitFlashCanvas.alpha = Mathf.Lerp(1f, 0f, elapsed / hitFlashDuration);
            yield return null;
        }

        hitFlashCanvas.alpha = 0f;
        hitFlashCanvas.gameObject.SetActive(false);
    }

    IEnumerator InvincibilityWindow()
    {
        _invincible = true;
        yield return new WaitForSeconds(damageCooldown);
        _invincible = false;
    }

    IEnumerator DeathSequence(Transform killer)
    {
        DisableLocomotion();
        OnPlayerDeath.Invoke();
        glitchOverlay?.SetGlitchIntensity(1f);

        if (killer != null && xrRig != null)
        {
            Vector3 dir = killer.position - xrRig.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion from    = xrRig.rotation;
                Quaternion to      = Quaternion.LookRotation(dir.normalized);
                float      elapsed = 0f;
                while (elapsed < lookDuration)
                {
                    elapsed       += Time.deltaTime;
                    xrRig.rotation = Quaternion.Slerp(from, to, elapsed / lookDuration);
                    yield return null;
                }
                xrRig.rotation = to;
            }
        }

        yield return new WaitForSeconds(screamerHoldSeconds);

        if (fadeCanvas != null)
        {
            fadeCanvas.gameObject.SetActive(true);
            fadeCanvas.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed          += Time.deltaTime;
                fadeCanvas.alpha  = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            fadeCanvas.alpha = 1f;
        }
        else
        {
            yield return new WaitForSeconds(fadeDuration);
        }

        OnDeathFadeComplete.Invoke();
    }

    void DisableLocomotion()
    {
        _cc.enabled = false;

        if (locomotionProviders != null && locomotionProviders.Length > 0)
        {
            foreach (MonoBehaviour mb in locomotionProviders)
                if (mb != null) mb.enabled = false;
            return;
        }

        Transform root = xrRig != null ? xrRig : transform;
        foreach (MonoBehaviour mb in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null) continue;
            string t = mb.GetType().Name;
            if (t.Contains("MoveProvider")      ||
                t.Contains("TurnProvider")       ||
                t.Contains("LocomotionProvider") ||
                t.Contains("SimulatorLocomotion"))
            {
                mb.enabled = false;
            }
        }
    }
}
