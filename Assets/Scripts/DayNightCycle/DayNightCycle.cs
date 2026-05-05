using UnityEngine;
using UnityEngine.UI;

public class DayNightCycle : MonoBehaviour
{
    [Header("Assign your fullscreen Image here")]
    public Image fullscreenImage;

    [Header("Colors for different times")]
    public Color nightColor = new Color(0f, 0f, 0.1f, 1f);
    public Color sunriseColor = new Color(1f, 0.45f, 0f, 1f);
    public Color dayColor = new Color(1f, 0.9f, 0.7f, 1f);
    public Color sunsetColor = new Color(1f, 0.35f, 0f, 1f);

    [Header("Opacity per phase (0 to 1)")]
    [Range(0f, 1f)] public float nightOpacity = 0.85f;
    [Range(0f, 1f)] public float sunriseOpacity = 0.2f;
    [Range(0f, 1f)] public float dayOpacity = 0.1f;
    [Range(0f, 1f)] public float sunsetOpacity = 0.2f;

    [Header("Cycle duration in seconds")]
    public float cycleDuration = 120f;

    private float timer = 0f;

    private void Update()
    {
        if (fullscreenImage == null) return;

        timer += Time.deltaTime;
        float t = (timer % cycleDuration) / cycleDuration;

        Color targetColor;
        float targetAlpha;

        if (t < 0.25f) // Night -> Sunrise
        {
            float phaseT = t / 0.25f;
            targetColor = Color.Lerp(nightColor, sunriseColor, phaseT);
            targetAlpha = Mathf.Lerp(nightOpacity, sunriseOpacity, phaseT);
        }
        else if (t < 0.5f) // Sunrise -> Day
        {
            float phaseT = (t - 0.25f) / 0.25f;
            targetColor = Color.Lerp(sunriseColor, dayColor, phaseT);
            targetAlpha = Mathf.Lerp(sunriseOpacity, dayOpacity, phaseT);
        }
        else if (t < 0.75f) // Day -> Sunset
        {
            float phaseT = (t - 0.5f) / 0.25f;
            targetColor = Color.Lerp(dayColor, sunsetColor, phaseT);
            targetAlpha = Mathf.Lerp(dayOpacity, sunsetOpacity, phaseT);
        }
        else // Sunset -> Night
        {
            float phaseT = (t - 0.75f) / 0.25f;
            targetColor = Color.Lerp(sunsetColor, nightColor, phaseT);
            targetAlpha = Mathf.Lerp(sunsetOpacity, nightOpacity, phaseT);
        }

        targetColor.a = targetAlpha;
        fullscreenImage.color = targetColor;
    }
}