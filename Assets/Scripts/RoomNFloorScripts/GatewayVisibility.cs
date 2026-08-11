using System.Collections;
using UnityEngine;

public class GatewayVisibility : MonoBehaviour
{
    [Header("Visibility")]
    [SerializeField] private bool visibleWhenUnlocked = true;

    [Header("Fade")]
    [SerializeField] private bool smoothFade = true;
    [SerializeField] private float fadeDuration = 0.2f;

    [Header("Components")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D gatewayCollider;

    private bool currentState;
    private Color originalColor;
    private Coroutine fadeCoroutine;

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
    }

    public void SetVisible(bool visible)
    {
        if (visible == currentState)
            return;

        currentState = visible;

        float targetAlpha = visible
            ? originalColor.a
            : 0f;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        if (smoothFade)
        {
            fadeCoroutine = StartCoroutine(
                FadeTo(targetAlpha)
            );
        }
        else
        {
            SetAlpha(targetAlpha);
        }

        if (gatewayCollider != null)
            gatewayCollider.enabled = visible;
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        if (spriteRenderer == null)
            yield break;

        Color color = spriteRenderer.color;

        float startAlpha = color.a;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;

            float t = time / fadeDuration;

            color.a = Mathf.Lerp(
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