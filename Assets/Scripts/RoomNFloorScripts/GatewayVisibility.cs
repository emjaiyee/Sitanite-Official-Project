using System.Collections;
using UnityEngine;

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
    [SerializeField] private Collider2D gatewayCollider;

    private bool currentState;
    private Color originalColor;
    private Coroutine fadeCoroutine;


    public bool IsVisible => currentState;


    private void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        gatewayCollider = GetComponent<Collider2D>();
    }


    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (gatewayCollider == null)
            gatewayCollider = GetComponent<Collider2D>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

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
    public void SetVisible(bool visible)
    {
        if (visible == currentState)
        {
            // Even if the visual state is already correct,
            // make sure the collider matches it.
            if (gatewayCollider != null)
                gatewayCollider.enabled = visible;

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
            gatewayCollider.enabled = visible;
        }
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


    private IEnumerator FadeTo(float targetAlpha)
    {
        if (spriteRenderer == null)
        {
            fadeCoroutine = null;
            yield break;
        }


        float duration =
            Mathf.Max(0.01f, fadeDuration);

        Color color = spriteRenderer.color;

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

            spriteRenderer.color = color;

            yield return null;
        }


        color.a = targetAlpha;
        spriteRenderer.color = color;

        fadeCoroutine = null;
    }


    private void SetAlpha(float alpha)
    {
        if (spriteRenderer == null)
            return;

        Color color = spriteRenderer.color;
        color.a = alpha;
        spriteRenderer.color = color;
    }
}