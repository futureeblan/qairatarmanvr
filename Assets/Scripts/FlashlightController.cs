using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// VR flashlight with battery drain, panic flicker below threshold, and Recharge().
/// Attach to the Light GameObject.
/// Wire Toggle() to an InputAction.performed event in the Inspector.
/// Zero GC allocations in Update.
/// </summary>
[RequireComponent(typeof(Light))]
public class FlashlightController : MonoBehaviour
{
    [System.Serializable]
    public class FloatEvent : UnityEvent<float> { }

    [Header("Battery")]
    public float maxBattery = 100f;
    [Tooltip("Battery units drained per second while on")]
    public float drainRate  = 4f;

    [Header("Panic Flicker")]
    [Range(0f, 0.5f), Tooltip("Normalized charge (0–1) below which flickering begins")]
    public float panicThreshold     = 0.20f;
    public float flickerMinInterval = 0.04f;
    public float flickerMaxInterval = 0.35f;

    [Header("Events")]
    [Tooltip("Arg = normalized charge 0..1. Wire to a battery bar UI slider.")]
    public FloatEvent OnBatteryChanged;
    public UnityEvent OnBatteryEmpty;
    public UnityEvent OnPanicStart;
    public UnityEvent OnPanicEnd;

    Light _light;
    float _baseIntensity;
    float _currentBattery;
    bool  _isOn    = true;
    bool  _inPanic;
    bool  _isEmpty;
    float _nextFlickerTime;

    public float BatteryNormalized => maxBattery > 0f ? _currentBattery / maxBattery : 0f;
    public bool  IsOn              => _isOn && !_isEmpty;

    void Awake()
    {
        _light          = GetComponent<Light>();
        _baseIntensity  = _light.intensity;
        _currentBattery = maxBattery;
    }

    void Update()
    {
        if (!_isOn || _isEmpty) return;

        _currentBattery -= drainRate * Time.deltaTime;

        if (_currentBattery <= 0f)
        {
            _currentBattery = 0f;
            _isEmpty        = true;
            _light.enabled  = false;
            if (_inPanic) ExitPanic();
            OnBatteryEmpty.Invoke();
            return;
        }

        OnBatteryChanged.Invoke(_currentBattery / maxBattery);

        bool shouldPanic = (_currentBattery / maxBattery) <= panicThreshold;
        if ( shouldPanic && !_inPanic) EnterPanic();
        if (!shouldPanic &&  _inPanic) ExitPanic();

        // Timer-based flicker — no coroutine, no allocation
        if (_inPanic && Time.time >= _nextFlickerTime)
        {
            _light.enabled   = !_light.enabled;
            _nextFlickerTime = Time.time + Random.Range(flickerMinInterval, flickerMaxInterval);
        }
    }

    void EnterPanic()
    {
        _inPanic         = true;
        _nextFlickerTime = Time.time;
        OnPanicStart.Invoke();
    }

    void ExitPanic()
    {
        _inPanic         = false;
        _light.enabled   = true;
        _light.intensity = _baseIntensity;
        OnPanicEnd.Invoke();
    }

    /// <summary>Wire to InputAction.performed in the Inspector.</summary>
    public void Toggle()
    {
        if (_isEmpty) return;
        _isOn          = !_isOn;
        _light.enabled = _isOn;
        if (!_isOn && _inPanic) ExitPanic();
    }

    /// <summary>Direct on/off for scripted sequences.</summary>
    public void SetOn(bool on)
    {
        if (_isEmpty) return;
        _isOn          = on;
        _light.enabled = on;
        if (!on && _inPanic) ExitPanic();
    }

    /// <summary>Call from a battery pickup collectible.</summary>
    public void Recharge(float amount)
    {
        _currentBattery = Mathf.Min(_currentBattery + amount, maxBattery);
        if (_isEmpty && _currentBattery > 0f)
        {
            _isEmpty       = false;
            _light.enabled = _isOn;
        }
    }
}
