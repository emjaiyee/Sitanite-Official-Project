using UnityEngine;

/// <summary>Plays a sprite array on every child SpriteRenderer, then removes the effect.</summary>
public class SpriteArrayVisual : MonoBehaviour
{
    [SerializeField] private Sprite[] sprites;
    [Min(1f)] [SerializeField] private float framesPerSecond = 12f;
    [SerializeField] private bool destroyWhenFinished = true;

    private SpriteRenderer[] spriteRenderers;
    private float startTime;

    private void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        startTime = Time.time;
        ApplyFrame(0);

        if (destroyWhenFinished && (sprites == null || sprites.Length == 0))
            Destroy(gameObject);
    }

    private void Update()
    {
        if (sprites == null || sprites.Length == 0)
            return;

        int frameIndex = Mathf.FloorToInt(
            (Time.time - startTime) * Mathf.Max(1f, framesPerSecond));

        if (frameIndex >= sprites.Length)
        {
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
}