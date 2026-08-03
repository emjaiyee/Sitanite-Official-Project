using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class TilemapElevationVisibility : MonoBehaviour
{
    [Header("Elevation")]
    [SerializeField] private int tilemapLevel = 0;

    [Header("Visibility")]
    [SerializeField] private bool disableAtThreshold = true;
    [SerializeField] [Min(0)] private int disableThreshold = 3;

    [Header("Fade")]
    [SerializeField] private bool smoothFade = true;
    [SerializeField] [Min(0.01f)] private float fadeDuration = 0.2f;

    [Header("Components")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TilemapCollider2D tilemapCollider;
    [SerializeField] private CompositeCollider2D compositeCollider;

    private bool currentState = true;
    private Color originalColor;

    private Coroutine fadeCoroutine;

    private void Reset()
    {
        tilemap = GetComponent<Tilemap>();
        tilemapCollider = GetComponent<TilemapCollider2D>();
        compositeCollider = GetComponent<CompositeCollider2D>();
    }

    private void Awake()
    {
        if (tilemap == null)
            tilemap = GetComponent<Tilemap>();

        originalColor = tilemap.color;
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
            int difference = Mathf.Abs(playerLevel - tilemapLevel);

            if (difference >= disableThreshold)
                enabled = false;
        }

        if (enabled == currentState)
            return;

        currentState = enabled;

        float targetAlpha = enabled ? originalColor.a : 0f;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        if (smoothFade)
        {
            fadeCoroutine = StartCoroutine(FadeTo(targetAlpha));
        }
        else
        {
            Color c = tilemap.color;
            c.a = targetAlpha;
            tilemap.color = c;
        }

        if (tilemapCollider != null)
            tilemapCollider.enabled = enabled;

        if (compositeCollider != null)
            compositeCollider.enabled = enabled;
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        Color color = tilemap.color;

        float startAlpha = color.a;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;

            color.a = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            tilemap.color = color;

            yield return null;
        }

        color.a = targetAlpha;
        tilemap.color = color;

        fadeCoroutine = null;
    }
}