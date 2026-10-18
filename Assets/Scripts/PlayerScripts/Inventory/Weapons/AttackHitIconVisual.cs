using UnityEngine;

public sealed class AttackHitIconVisual : MonoBehaviour
{
    private const float Lifetime = 0.65f;
    private const float RiseDistance = 0.35f;

    private static bool warnedAboutMissingIcon;

    private SpriteRenderer iconRenderer;
    private Vector3 startLocalPosition;
    private Vector3 baseLocalScale;
    private float elapsed;
    private bool isImpactBurst;
    private Sprite[] impactSprites;
    private float impactFramesPerSecond;

    public static void Play(
        IDamageable target,
        Sprite icon,
        Vector3? source,
        float iconWorldHeight)
    {
        if (target is not Component targetComponent)
            return;

        if (icon == null)
        {
            if (!warnedAboutMissingIcon)
            {
                Debug.LogWarning(
                    "Attack hit feedback needs an icon assigned to the weapon.",
                    targetComponent
                );
                warnedAboutMissingIcon = true;
            }

            return;
        }

        Transform targetTransform = targetComponent.transform;
        SpriteRenderer targetRenderer =
            targetTransform.GetComponentInChildren<SpriteRenderer>();
        Vector3 targetPosition = targetRenderer != null
            ? targetRenderer.bounds.center
            : targetTransform.position;
        Vector2 direction = source.HasValue
            ? (Vector2)(targetPosition - source.Value)
            : Vector2.up;

        float side = direction.x < 0f ? -1f : 1f;
        float horizontalOffset = targetRenderer != null
            ? targetRenderer.bounds.extents.x + 0.2f
            : 0.45f;
        Vector3 worldPosition = targetPosition +
            new Vector3(side * horizontalOffset, 0.2f, 0f);

        GameObject visualObject = new GameObject("Attack Hit Icon");
        visualObject.transform.SetParent(targetTransform, false);
        visualObject.transform.position = worldPosition;

        SpriteRenderer visualRenderer =
            visualObject.AddComponent<SpriteRenderer>();
        visualRenderer.sprite = icon;

        if (targetRenderer != null)
        {
            visualRenderer.sortingLayerID = targetRenderer.sortingLayerID;
            visualRenderer.sortingOrder = targetRenderer.sortingOrder + 1;
        }

        float iconHeight = Mathf.Max(0.0001f, icon.bounds.size.y);
        float parentScaleY = Mathf.Max(
            0.0001f,
            targetTransform.lossyScale.y
        );
        visualObject.transform.localScale = Vector3.one *
            (Mathf.Max(0.01f, iconWorldHeight) / (iconHeight * parentScaleY));

        AttackHitIconVisual visual =
            visualObject.AddComponent<AttackHitIconVisual>();
        visual.Initialize(visualRenderer);
    }

    public static void PlayImpact(
        IDamageable target,
        Sprite icon,
        float iconWorldHeight)
    {
        if (icon == null)
            return;

        CreateImpact(target, icon, iconWorldHeight, null, 0f);
    }

    public static void PlayImpact(
        IDamageable target,
        Sprite[] sprites,
        float framesPerSecond,
        float iconWorldHeight)
    {
        if (sprites == null || sprites.Length == 0 || sprites[0] == null)
            return;

        CreateImpact(
            target,
            sprites[0],
            iconWorldHeight,
            sprites,
            Mathf.Max(1f, framesPerSecond)
        );
    }

    private static void CreateImpact(
        IDamageable target,
        Sprite icon,
        float iconWorldHeight,
        Sprite[] sprites,
        float framesPerSecond)
    {
        if (target is not Component targetComponent)
            return;

        Transform targetTransform = targetComponent.transform;
        SpriteRenderer targetRenderer =
            targetTransform.GetComponentInChildren<SpriteRenderer>();
        Vector3 worldPosition = targetRenderer != null
            ? targetRenderer.bounds.center
            : targetTransform.position;

        GameObject visualObject = new GameObject("Attack Hit Impact");
        visualObject.transform.SetParent(targetTransform, false);
        visualObject.transform.position = worldPosition;

        SpriteRenderer visualRenderer =
            visualObject.AddComponent<SpriteRenderer>();
        visualRenderer.sprite = icon;

        if (targetRenderer != null)
        {
            visualRenderer.sortingLayerID = targetRenderer.sortingLayerID;
            visualRenderer.sortingOrder = targetRenderer.sortingOrder + 1;
        }

        float iconHeight = Mathf.Max(0.0001f, icon.bounds.size.y);
        float parentScaleY = Mathf.Max(
            0.0001f,
            targetTransform.lossyScale.y
        );
        visualObject.transform.localScale = Vector3.one *
            (Mathf.Max(0.01f, iconWorldHeight) / (iconHeight * parentScaleY));

        AttackHitIconVisual visual =
            visualObject.AddComponent<AttackHitIconVisual>();
        visual.Initialize(visualRenderer, true, sprites, framesPerSecond);
    }

    private void Initialize(
        SpriteRenderer renderer,
        bool impactBurst = false,
        Sprite[] sprites = null,
        float framesPerSecond = 0f)
    {
        iconRenderer = renderer;
        startLocalPosition = transform.localPosition;
        baseLocalScale = transform.localScale;
        isImpactBurst = impactBurst;
        impactSprites = sprites;
        impactFramesPerSecond = framesPerSecond;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float impactLifetime = impactSprites != null
            ? impactSprites.Length / impactFramesPerSecond
            : Lifetime;
        float progress = Mathf.Clamp01(elapsed / impactLifetime);

        if (isImpactBurst)
        {
            float scale = progress < 0.25f
                ? Mathf.Lerp(0.55f, 1.2f, progress / 0.25f)
                : Mathf.Lerp(1.2f, 0.8f, (progress - 0.25f) / 0.75f);
            transform.localScale = baseLocalScale * scale;
        }
        else
        {
            transform.localPosition = startLocalPosition +
                Vector3.up * (RiseDistance * progress);
        }

        if (impactSprites != null)
        {
            int frameIndex = Mathf.FloorToInt(
                elapsed * impactFramesPerSecond);
            if (frameIndex >= impactSprites.Length)
            {
                Destroy(gameObject);
                return;
            }

            if (impactSprites[frameIndex] != null)
                iconRenderer.sprite = impactSprites[frameIndex];
        }

        Color color = iconRenderer.color;
        color.a = 1f - progress;
        iconRenderer.color = color;

        if (progress >= 1f)
            Destroy(gameObject);
    }
}