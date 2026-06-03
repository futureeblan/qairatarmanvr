using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Attach to a World-Space Canvas parented to the VR Camera.
// Add a child Image (stretch to fill) and assign it to overlayImage.
[RequireComponent(typeof(Canvas))]
public class GlitchOverlay : MonoBehaviour
{
    [Header("Visual")]
    public Image overlayImage;
    public Color baseGlitchColor = new Color(1f, 0.05f, 0.05f, 0f);

    float _intensity;
    Coroutine _flicker;

    public void SetGlitch(bool on) => SetGlitchIntensity(on ? 1f : 0f);

    public void SetGlitchIntensity(float intensity)
    {
        _intensity = Mathf.Clamp01(intensity);

        if (_intensity < 0.01f)
        {
            if (_flicker != null) { StopCoroutine(_flicker); _flicker = null; }
            if (overlayImage) overlayImage.enabled = false;
            return;
        }

        if (overlayImage) overlayImage.enabled = true;
        if (_flicker == null) _flicker = StartCoroutine(FlickerLoop());
    }

    IEnumerator FlickerLoop()
    {
        while (_intensity > 0.01f)
        {
            if (overlayImage)
            {
                float alpha = Random.value < _intensity
                    ? Random.Range(0f, _intensity * 0.55f)
                    : 0f;

                overlayImage.color = new Color(
                    baseGlitchColor.r, baseGlitchColor.g, baseGlitchColor.b, alpha);

                // Scan-line shift
                overlayImage.rectTransform.anchoredPosition = new Vector2(
                    (Random.value - 0.5f) * 10f * _intensity,
                    (Random.value - 0.5f) * 28f * _intensity);
            }

            float interval = Random.Range(0.016f, 0.11f / Mathf.Max(_intensity, 0.05f));
            yield return new WaitForSeconds(interval);
        }

        if (overlayImage) overlayImage.enabled = false;
        _flicker = null;
    }
}
