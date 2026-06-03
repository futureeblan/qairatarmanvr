using UnityEngine;

/// <summary>
/// Батарейка-подбирашка для фонарика.
/// Поставь Trigger-коллайдер на этот объект.
/// FlashlightController найдётся автоматически среди дочерних объектов игрока.
/// </summary>
public class BatteryPickup : MonoBehaviour
{
    [Tooltip("Сколько заряда восстанавливать (те же единицы что maxBattery у FlashlightController)")]
    public float rechargeAmount = 40f;

    [Tooltip("Назначь вручную или оставь пустым — найдётся в Start()")]
    public FlashlightController flashlight;

    [Header("Визуал/аудио при подборе")]
    public AudioClip pickupSound;
    public GameObject pickupVFX;     // опционально: партикл или анимация

    AudioSource _audio;

    void Awake()
    {
        _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake  = false;
        _audio.spatialBlend = 1f;
    }

    void Start()
    {
        if (flashlight != null) return;

        var playerGo = GameObject.FindWithTag("Player")
                    ?? GameObject.Find("XR Origin (XR Rig)");
        if (playerGo != null)
            flashlight = playerGo.GetComponentInChildren<FlashlightController>(true);
    }

    void OnTriggerEnter(Collider other)
    {
        if (flashlight == null) return;
        if (!IsPlayer(other)) return;

        flashlight.Recharge(rechargeAmount);
        PlayPickupFX();
        gameObject.SetActive(false);
    }

    bool IsPlayer(Collider col)
    {
        return col.CompareTag("Player") ||
               col.name.Contains("XR Origin") ||
               col.name.Contains("Player");
    }

    void PlayPickupFX()
    {
        if (pickupSound != null)
            _audio.PlayOneShot(pickupSound);

        if (pickupVFX != null)
        {
            var vfx = Instantiate(pickupVFX, transform.position, Quaternion.identity);
            Destroy(vfx, 3f);
        }
    }
}
