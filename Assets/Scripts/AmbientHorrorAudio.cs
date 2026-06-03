using UnityEngine;

/// <summary>
/// Случайные атмосферные звуки для хоррора (шорохи, шаги вдали, металлический скрип).
/// Привяжи к пустому GameObject в комнате/коридоре.
/// AudioSource настроится автоматически (3D, без loop).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AmbientHorrorAudio : MonoBehaviour
{
    [Header("Клипы")]
    [Tooltip("Заполни несколько клипов — будут играть случайно")]
    public AudioClip[] ambientClips;

    [Header("Интервал между звуками")]
    public float minInterval = 10f;
    public float maxInterval = 35f;

    [Header("Громкость")]
    public float minVolume = 0.08f;
    public float maxVolume = 0.45f;

    [Header("Слышимость")]
    [Tooltip("Расстояние до которого слышен звук (AudioSource.maxDistance)")]
    public float maxDistance = 25f;
    [Tooltip("0 = 2D, 1 = полностью 3D (пространственный)")]
    [Range(0f, 1f)]
    public float spatialBlend = 1f;

    AudioSource _source;
    float       _nextPlayTime;

    void Awake()
    {
        _source                 = GetComponent<AudioSource>();
        _source.playOnAwake     = false;
        _source.loop            = false;
        _source.spatialBlend    = spatialBlend;
        _source.maxDistance     = maxDistance;
        _source.rolloffMode     = AudioRolloffMode.Logarithmic;
    }

    void Start() => ScheduleNext();

    void Update()
    {
        if (ambientClips == null || ambientClips.Length == 0) return;
        if (Time.time < _nextPlayTime) return;

        PlayRandom();
        ScheduleNext();
    }

    void PlayRandom()
    {
        AudioClip clip = ambientClips[Random.Range(0, ambientClips.Length)];
        if (clip == null) return;

        _source.volume = Random.Range(minVolume, maxVolume);
        _source.PlayOneShot(clip);
    }

    void ScheduleNext()
    {
        _nextPlayTime = Time.time + Random.Range(minInterval, maxInterval);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 0f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }
}
