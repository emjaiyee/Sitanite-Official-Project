using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GatewayVisibility : MonoBehaviour
{
    [Header("Visibility")]
    [Tooltip("Initial state of the gateway when the room is loaded.")]
    [SerializeField] private bool visibleWhenUnlocked = true;
    [Tooltip("Keeps the gateway visual hidden while its collider remains usable when unlocked.")]
    [SerializeField] private bool invisibleWhenUnlocked;

    [Header("Fade")]
    [SerializeField] private bool smoothFade = true;
    [SerializeField] private float fadeDuration = 0.2f;

    [Header("Components")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TilemapRenderer tilemapRenderer;
    [SerializeField] private Collider2D gatewayCollider;

    private bool currentState;
    private Color originalColor;
    private Coroutine fadeCoroutine;


    public bool IsVisible => currentState;


    protected virtual void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        tilemap = GetComponent<Tilemap>();
        tilemapRenderer = GetComponent<TilemapRenderer>();
        gatewayCollider = GetComponent<Collider2D>();
    }


    protected virtual void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (tilemap == null)
            tilemap = GetComponent<Tilemap>();

        if (tilemapRenderer == null)
            tilemapRenderer = GetComponent<TilemapRenderer>();

        if (gatewayCollider == null)
            gatewayCollider = GetComponent<Collider2D>();

        if (gatewayCollider == null)
            gatewayCollider = GetComponentInParent<Collider2D>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
        else if (tilemap != null)
            originalColor = tilemap.color;

        // Establish the prefab's default state first.
        // RoomManager will override this afterward
        // based on whether the room has enemies.
        currentState = !visibleWhenUnlocked;

        SetVisible(visibleWhenUnlocked);
    }


    /// <summary>
    /// Makes the gateway visible and usable,
    /// or invisible and unusable.
    /// </summary>
    public virtual void SetVisible(bool visible)
    {
        if (visible == currentState)
        {
            float currentTargetAlpha = visible && !invisibleWhenUnlocked
                ? originalColor.a
                : 0f;

            SetAlpha(currentTargetAlpha);

            // Even if the visual state is already correct,
            // make sure the collider matches it.
            if (gatewayCollider != null)
                gatewayCollider.enabled = visible && !invisibleWhenUnlocked;

            if (visible && tilemapRenderer != null)
                tilemapRenderer.enabled = true;

            return;
        }

        currentState = visible;


        // ---------------------------------------------
        // VISUAL
        // ---------------------------------------------

        float targetAlpha = visible && !invisibleWhenUnlocked
            ? originalColor.a
            : 0f;


        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }


        if (smoothFade && fadeDuration > 0f)
        {
            fadeCoroutine = StartCoroutine(
                FadeTo(targetAlpha)
            );
        }
        else
        {
            SetAlpha(targetAlpha);
        }


        // ---------------------------------------------
        // COLLIDER
        // ---------------------------------------------

        // This is what actually prevents the player
        // from entering the gateway.
        if (gatewayCollider != null)
        {
            gatewayCollider.enabled = visible && !invisibleWhenUnlocked;
        }

        if (visible && tilemapRenderer != null)
            tilemapRenderer.enabled = true;
    }


    /// <summary>
    /// Convenience method for locking the gateway.
    /// </summary>
    public void Lock()
    {
        SetVisible(false);
    }


    /// <summary>
    /// Convenience method for unlocking the gateway.
    /// </summary>
    public void Unlock()
    {
        SetVisible(true);
    }

    protected void SetVisibleImmediately(bool visible)
    {
        currentState = visible;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        SetAlpha(visible && !invisibleWhenUnlocked
            ? originalColor.a
            : 0f);

        if (gatewayCollider != null)
            gatewayCollider.enabled = visible && !invisibleWhenUnlocked;
    }

    protected bool ShouldShowVisual(bool visible)
    {
        return visible && !invisibleWhenUnlocked;
    }

    protected bool UsesSmoothFade => smoothFade && fadeDuration > 0f;
    protected float VisibilityFadeDuration => fadeDuration;


    private IEnumerator FadeTo(float targetAlpha)
    {
        if (spriteRenderer == null && tilemap == null)
        {
            fadeCoroutine = null;
            yield break;
        }


        float duration =
            Mathf.Max(0.01f, fadeDuration);

        Color color = spriteRenderer != null
            ? spriteRenderer.color
            : tilemap.color;

        float startAlpha = color.a;
        float time = 0f;


        while (time < duration)
        {
            time += Time.deltaTime;

            float t =
                Mathf.Clamp01(time / duration);

            color.a =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    t
                );

            SetAlpha(color.a);

            yield return null;
        }


        color.a = targetAlpha;
        SetAlpha(color.a);

        fadeCoroutine = null;
    }


    private void SetAlpha(float alpha)
    {
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }

        if (tilemap != null)
        {
            Color color = tilemap.color;
            color.a = alpha;
            tilemap.color = color;
        }
    }
}