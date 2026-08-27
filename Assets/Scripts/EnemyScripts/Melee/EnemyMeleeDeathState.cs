using UnityEngine;

public class EnemyMeleeDeathState : EnemyMeleeState
{
    private float deathTimer;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;


    public EnemyMeleeDeathState(
        EnemyMelee enemy)
        : base(enemy)
    {
    }


    // =========================================================
    // ENTER
    // =========================================================

    public override void Enter()
    {
        deathTimer = 0f;

        Enemy.StopMoving();


        // -----------------------------------------------------
        // FIND SPRITE RENDERER
        // -----------------------------------------------------

        spriteRenderer =
            Enemy.GetComponentInChildren<SpriteRenderer>();


        if (spriteRenderer != null)
        {
            originalColor =
                spriteRenderer.color;
        }
        else
        {
            Debug.LogWarning(
                $"[Death] {Enemy.name}: " +
                "No SpriteRenderer found."
            );
        }


        Debug.Log(
            $"[Death] {Enemy.name}: " +
            $"Entered Death state. " +
            $"Animation delay = " +
            $"{Enemy.DeathAnimationDelay}s, " +
            $"Fade duration = " +
            $"{Enemy.DeathFadeDuration}s"
        );
    }


    // =========================================================
    // TICK
    // =========================================================

    public override void Tick()
    {
        deathTimer += Time.deltaTime;


        // =====================================================
        // PHASE 1 — DEATH ANIMATION DELAY
        // =====================================================

        if (deathTimer <
            Enemy.DeathAnimationDelay)
        {
            return;
        }


        // =====================================================
        // PHASE 2 — FADE
        // =====================================================

        float fadeTimer =
            deathTimer -
            Enemy.DeathAnimationDelay;


        float fadeDuration =
            Enemy.DeathFadeDuration;


        float fadeProgress;


        if (fadeDuration <= 0f)
        {
            fadeProgress = 1f;
        }
        else
        {
            fadeProgress =
                Mathf.Clamp01(
                    fadeTimer /
                    fadeDuration
                );
        }


        if (spriteRenderer != null)
        {
            Color color =
                originalColor;

            color.a =
                Mathf.Lerp(
                    originalColor.a,
                    0f,
                    fadeProgress
                );

            spriteRenderer.color =
                color;
        }


        // =====================================================
        // PHASE 3 — DESTROY
        // =====================================================

        if (fadeProgress >= 1f)
        {
            DestroyEnemy();
        }
    }


    // =========================================================
    // DESTROY
    // =========================================================

    private void DestroyEnemy()
    {
        Debug.Log(
            $"[Death] {Enemy.name}: " +
            "Death sequence finished. " +
            "Destroying enemy."
        );


        Object.Destroy(
            Enemy.gameObject
        );
    }


    // =========================================================
    // EXIT
    // =========================================================

    public override void Exit()
    {
    }
}