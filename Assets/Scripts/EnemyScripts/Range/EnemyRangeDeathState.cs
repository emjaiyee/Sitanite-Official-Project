using UnityEngine;

public class EnemyRangeDeathState : EnemyRangeState
{
    private float deathTimer;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    public EnemyRangeDeathState(EnemyRange enemy) : base(enemy)
    {
    }

    public override void Enter()
    {
        deathTimer = 0f;
        Enemy.StopMoving();

        spriteRenderer = Enemy.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    public override void Tick()
    {
        deathTimer += Time.deltaTime;

        if (deathTimer < Enemy.DeathAnimationDelay)
            return;

        float fadeTimer = deathTimer - Enemy.DeathAnimationDelay;
        float fadeDuration = Enemy.DeathFadeDuration;
        float fadeProgress = fadeDuration <= 0f ? 1f : Mathf.Clamp01(fadeTimer / fadeDuration);

        if (spriteRenderer != null)
        {
            Color color = originalColor;
            color.a = Mathf.Lerp(originalColor.a, 0f, fadeProgress);
            spriteRenderer.color = color;
        }

        if (fadeProgress >= 1f)
            Object.Destroy(Enemy.gameObject);
    }
}