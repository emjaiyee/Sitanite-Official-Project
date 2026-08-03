using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class LightElevationVisibility : MonoBehaviour
{
    [Header("Elevation")]
    [SerializeField] private int lightLevel = 0;

    [Header("Visibility")]
    [SerializeField] private bool disableAtThreshold = true;
    [SerializeField] [Min(0)] private int disableThreshold = 3;

    [Header("Fade")]
    [SerializeField] private bool smoothFade = true;
    [SerializeField] [Min(0.01f)] private float fadeDuration = 0.2f;

    [Header("Components")]
    [SerializeField] private Light2D light2D;

    private bool currentState = true;
    private float originalIntensity;

    private Coroutine fadeCoroutine;

    private void Reset()
    {
        light2D = GetComponent<Light2D>();
    }

    private void Awake()
    {
        if (light2D == null)
            light2D = GetComponent<Light2D>();

        originalIntensity = light2D.intensity;
    }

    private void OnEnable()
    {
        PlayerElevationLevel.OnElevationChanged += Refresh;
    }

    private void OnDisable()
    {
        PlayerElevationLevel.OnElevationChanged -= Refresh;
    }

    public void Refresh(int playerLevel)
    {
        bool enabled = true;

        if (disableAtThreshold)
        {
            int difference = Mathf.Abs(playerLevel - lightLevel);

            if (difference >= disableThreshold)
                enabled = false;
        }

        if (enabled == currentState)
            return;

        currentState = enabled;

        float targetIntensity = enabled ? originalIntensity : 0f;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        if (smoothFade)
        {
            fadeCoroutine = StartCoroutine(FadeTo(targetIntensity));
        }
        else
        {
            light2D.intensity = targetIntensity;
        }
    }

    private IEnumerator FadeTo(float targetIntensity)
    {
        float startIntensity = light2D.intensity;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;

            light2D.intensity = Mathf.Lerp(
                startIntensity,
                targetIntensity,
                time / fadeDuration);

            yield return null;
        }

        light2D.intensity = targetIntensity;
        fadeCoroutine = null;
    }
}