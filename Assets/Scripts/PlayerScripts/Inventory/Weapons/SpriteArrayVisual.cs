using UnityEngine;

/// <summary>Plays a sprite array on every child SpriteRenderer, then removes the effect.</summary>
public class SpriteArrayVisual : MonoBehaviour
{
    [SerializeField] private Sprite[] sprites;
    [Min(1f)] [SerializeField] private float framesPerSecond = 12f;
    [SerializeField] private bool destroyWhenFinished = true;
    [SerializeField] private bool fadeWhenFinished;
    [Min(0.01f)] [SerializeField] private float fadeDuration = 0.25f;

    private SpriteRenderer[] spriteRenderers;
    private Color[] initialColors;
    private float startTime;
    private float fadeStartTime;
    private bool isFading;

    private void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        initialColors = new Color[spriteRenderers.Length];

        for (int index = 0; index < spriteRenderers.Length; index++)
            initialColors[index] = spriteRenderers[index].color;
    }

    private void OnEnable()
    {
        startTime = Time.time;
        isFading = false;
        RestoreInitialColors();
        ApplyFrame(0);

        if (destroyWhenFinished && (sprites == null || sprites.Length == 0))
            Destroy(gameObject);
    }

    private void Update()
    {
        if (sprites == null || sprites.Length == 0)
            return;

        if (isFading)
        {
            UpdateFade();
            return;
        }

        int frameIndex = Mathf.FloorToInt(
            (Time.time - startTime) * Mathf.Max(1f, framesPerSecond));

        if (frameIndex >= sprites.Length)
        {
            ApplyFrame(sprites.Length - 1);

            if (fadeWhenFinished)
            {
                isFading = true;
                fadeStartTime = Time.time;
                return;
            }

            if (destroyWhenFinished)
                Destroy(gameObject);

            return;
        }

        ApplyFrame(frameIndex);
    }

    private void ApplyFrame(int frameIndex)
    {
        if (sprites == null || sprites.Length == 0 || spriteRenderers == null)
            return;

        Sprite sprite = sprites[Mathf.Clamp(frameIndex, 0, sprites.Length - 1)];
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            if (spriteRenderer != null)
                spriteRenderer.sprite = sprite;
        }
    }

    private void UpdateFade()
    {
        float fadeProgress = Mathf.Clamp01((Time.time - fadeStartTime) / fadeDuration);

        for (int index = 0; index < spriteRenderers.Length; index++)
        {
            SpriteRenderer spriteRenderer = spriteRenderers[index];
            if (spriteRenderer == null)
                continue;

            Color color = initialColors[index];
            color.a *= 1f - fadeProgress;
            spriteRenderer.color = color;
        }

        if (fadeProgress >= 1f && destroyWhenFinished)
            Destroy(gameObject);
    }

    private void RestoreInitialColors()
    {
        if (spriteRenderers == null || initialColors == null)
            return;

        for (int index = 0; index < spriteRenderers.Length; index++)
        {
            if (spriteRenderers[index] != null)
                spriteRenderers[index].color = initialColors[index];
        }
    }
}