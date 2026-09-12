using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[AddComponentMenu("Dimla/Gateway Floor Visibility")]
public class GatewayFloorVisibility : GatewayVisibility
{
    [Serializable]
    private class RelatedPathTilemap
    {
        [SerializeField] private Tilemap tilemap;
        [SerializeField] private bool visibleWhenUnlocked = true;

        public Tilemap Tilemap => tilemap;
        public bool VisibleWhenUnlocked => visibleWhenUnlocked;
    }

    [Serializable]
    private class RelatedPathSprite
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool visibleWhenUnlocked = true;

        public SpriteRenderer SpriteRenderer => spriteRenderer;
        public bool VisibleWhenUnlocked => visibleWhenUnlocked;
    }

    [Serializable]
    private class RelatedPathCollider
    {
        [SerializeField] private Collider2D collider;
        [SerializeField] private bool enabledWhenUnlocked = true;

        public Collider2D Collider => collider;
        public bool EnabledWhenUnlocked => enabledWhenUnlocked;
    }

    [Header("Floor Gateway Path")]
    [Tooltip("Each tilemap has its own visibility state while floor progression is unlocked.")]
    [SerializeField] private List<RelatedPathTilemap> relatedTilemaps =
        new List<RelatedPathTilemap>();

    [Tooltip("Each sprite has its own visibility state while floor progression is unlocked.")]
    [SerializeField] private List<RelatedPathSprite> relatedSprites =
        new List<RelatedPathSprite>();

    [Tooltip("Each collider has its own enabled state while floor progression is unlocked.")]
    [SerializeField] private List<RelatedPathCollider> relatedColliders =
        new List<RelatedPathCollider>();

    private Color[] originalTilemapColors;
    private Color[] originalSpriteColors;
    private Coroutine relatedFadeCoroutine;
    private float relatedAlpha = 1f;

    protected override void Awake()
    {
        CacheOriginalColors();
        base.Awake();
        SetVisibleImmediately(false);
        ApplyRelatedState(false, false);
    }

    public override void SetVisible(bool visible)
    {
        base.SetVisible(visible);
        ApplyRelatedState(visible, true);
    }

    public void SetInvisible()
    {
        SetVisible(false);
    }

    public void UnlockFloorProgression()
    {
        SetVisible(true);
    }

    private void CacheOriginalColors()
    {
        originalTilemapColors = new Color[relatedTilemaps.Count];
        for (int index = 0; index < relatedTilemaps.Count; index++)
        {
            Tilemap tilemap = relatedTilemaps[index].Tilemap;
            if (tilemap != null)
                originalTilemapColors[index] = tilemap.color;
        }

        originalSpriteColors = new Color[relatedSprites.Count];
        for (int index = 0; index < relatedSprites.Count; index++)
        {
            SpriteRenderer sprite = relatedSprites[index].SpriteRenderer;
            if (sprite != null)
                originalSpriteColors[index] = sprite.color;
        }
    }

    private void ApplyRelatedState(bool unlocked, bool animate)
    {
        float targetAlpha = unlocked ? 1f : 0f;

        if (relatedFadeCoroutine != null)
        {
            StopCoroutine(relatedFadeCoroutine);
            relatedFadeCoroutine = null;
        }

        if (animate && UsesSmoothFade)
            relatedFadeCoroutine = StartCoroutine(FadeRelatedTo(targetAlpha));
        else
            SetRelatedAlpha(targetAlpha);

        SetRelatedColliders(unlocked);
    }

    private IEnumerator FadeRelatedTo(float targetAlpha)
    {
        float duration = Mathf.Max(0.01f, VisibilityFadeDuration);
        float time = 0f;
        float startAlpha = relatedAlpha;

        while (time < duration)
        {
            time += Time.deltaTime;
            float progress = Mathf.Clamp01(time / duration);
            SetRelatedAlpha(Mathf.Lerp(startAlpha, targetAlpha, progress));
            yield return null;
        }

        SetRelatedAlpha(targetAlpha);
        relatedFadeCoroutine = null;
    }

    private void SetRelatedAlpha(float alpha)
    {
        relatedAlpha = alpha;

        for (int index = 0; index < relatedSprites.Count; index++)
        {
            SpriteRenderer sprite = relatedSprites[index].SpriteRenderer;
            if (sprite == null)
                continue;

            Color color = originalSpriteColors[index];
            color.a *= relatedSprites[index].VisibleWhenUnlocked ? alpha : 1f - alpha;
            sprite.color = color;
        }

        for (int index = 0; index < relatedTilemaps.Count; index++)
        {
            Tilemap tilemap = relatedTilemaps[index].Tilemap;
            if (tilemap == null)
                continue;

            Color color = originalTilemapColors[index];
            color.a *= relatedTilemaps[index].VisibleWhenUnlocked ? alpha : 1f - alpha;
            tilemap.color = color;
        }
    }

    private void SetRelatedColliders(bool unlocked)
    {
        foreach (RelatedPathCollider relatedCollider in relatedColliders)
        {
            if (relatedCollider.Collider != null)
            {
                relatedCollider.Collider.enabled =
                    relatedCollider.EnabledWhenUnlocked == unlocked;
            }
        }
    }
}