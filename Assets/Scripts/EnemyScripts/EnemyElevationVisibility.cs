using System.Collections;
using UnityEngine;

public class EnemyElevationVisibility : MonoBehaviour
{
    [Header("Visibility")]
    [Tooltip(
        "If enabled, the enemy becomes invisible " +
        "when the elevation difference reaches the threshold."
    )]
    [SerializeField] private bool disableAtThreshold = true;


    [Tooltip(
        "Maximum elevation difference before the enemy " +
        "becomes invisible."
    )]
    [SerializeField] [Min(0)]
    private int disableThreshold = 3;


    [Header("Fade")]
    [Tooltip(
        "Smoothly fade the enemy instead of instantly " +
        "changing visibility."
    )]
    [SerializeField] private bool smoothFade = true;


    [Tooltip(
        "How long the visibility fade takes."
    )]
    [SerializeField] [Min(0.01f)]
    private float fadeDuration = 0.2f;


    [Header("Components")]
    [SerializeField]
    private SpriteRenderer spriteRenderer;


    [SerializeField]
    private Collider2D enemyCollider;


    private EnemyElevationLevel enemyElevation;

    private bool currentState = true;

    private Color originalColor;

    private Coroutine fadeCoroutine;


    // =========================================================
    // UNITY
    // =========================================================

    private void Reset()
    {
        spriteRenderer =
            GetComponentInChildren<SpriteRenderer>();


        enemyCollider =
            GetComponent<Collider2D>();
    }


    private void Awake()
    {
        enemyElevation =
            GetComponent<EnemyElevationLevel>();


        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponentInChildren<SpriteRenderer>();
        }


        if (enemyCollider == null)
        {
            enemyCollider =
                GetComponent<Collider2D>();
        }


        if (spriteRenderer != null)
        {
            originalColor =
                spriteRenderer.color;
        }
    }


    private void OnEnable()
    {
        PlayerElevationLevel.OnElevationChanged +=
            HandlePlayerElevationChanged;


        if (enemyElevation != null)
        {
            enemyElevation.OnElevationChanged +=
                HandleEnemyElevationChanged;
        }
    }


    private void OnDisable()
    {
        PlayerElevationLevel.OnElevationChanged -=
            HandlePlayerElevationChanged;


        if (enemyElevation != null)
        {
            enemyElevation.OnElevationChanged -=
                HandleEnemyElevationChanged;
        }
    }


    private void Start()
    {
        Refresh();
    }


    // =========================================================
    // ELEVATION EVENTS
    // =========================================================

    private void HandlePlayerElevationChanged(
        int playerLevel)
    {
        Refresh();
    }


    private void HandleEnemyElevationChanged(
        int enemyLevel)
    {
        Refresh();
    }


    // =========================================================
    // REFRESH
    // =========================================================

    private void Refresh()
    {
        if (enemyElevation == null)
            return;


        if (PlayerElevationLevel.Instance == null)
            return;


        int playerLevel =
            PlayerElevationLevel.Instance.CurrentLevel;


        int enemyLevel =
            enemyElevation.CurrentLevel;


        bool shouldBeVisible = true;


        if (disableAtThreshold)
        {
            int difference =
                Mathf.Abs(
                    playerLevel -
                    enemyLevel
                );


            if (difference >= disableThreshold)
            {
                shouldBeVisible = false;
            }
        }


        ApplyVisibility(
            shouldBeVisible
        );
    }


    // =========================================================
    // APPLY VISIBILITY
    // =========================================================

    private void ApplyVisibility(
        bool visible)
    {
        if (visible == currentState)
            return;


        currentState = visible;


        float targetAlpha =
            visible
                ? originalColor.a
                : 0f;


        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }


        if (smoothFade)
        {
            fadeCoroutine =
                StartCoroutine(
                    FadeTo(targetAlpha)
                );
        }
        else
        {
            SetAlpha(targetAlpha);
        }


        if (enemyCollider != null)
        {
            enemyCollider.enabled =
                visible;
        }
    }


    // =========================================================
    // FADE
    // =========================================================

    private IEnumerator FadeTo(
        float targetAlpha)
    {
        if (spriteRenderer == null)
            yield break;


        Color color =
            spriteRenderer.color;


        float startAlpha =
            color.a;


        float time = 0f;


        while (time < fadeDuration)
        {
            time += Time.deltaTime;


            float progress =
                Mathf.Clamp01(
                    time /
                    fadeDuration
                );


            color.a =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    progress
                );


            spriteRenderer.color =
                color;


            yield return null;
        }


        color.a =
            targetAlpha;


        spriteRenderer.color =
            color;


        fadeCoroutine = null;
    }


    // =========================================================
    // ALPHA
    // =========================================================

    private void SetAlpha(
        float alpha)
    {
        if (spriteRenderer == null)
            return;


        Color color =
            spriteRenderer.color;


        color.a =
            alpha;


        spriteRenderer.color =
            color;
    }
}