using UnityEngine;

public sealed class AttackHitIconVisual : MonoBehaviour
{
    private const float Lifetime = 0.65f;
    private const float RiseDistance = 0.35f;
    private const float MuzzleFlashLifetime = 0.12f;
    private const float SpellCastFlashLifetime = 0.24f;

    private static bool warnedAboutMissingIcon;
    private static Sprite muzzleFlashSprite;
    private static Sprite spellCastFlashSprite;

    private SpriteRenderer iconRenderer;
    private Vector3 startLocalPosition;
    private Vector3 baseLocalScale;
    private float elapsed;
    private bool isImpactBurst;
    private bool isRisingParticle;
    private bool isMuzzleFlash;
    private bool isSpellCastFlash;
    private float lifetime = Lifetime;
    private float riseDistance = RiseDistance;
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

    public static void PlayMuzzleFlash(
        Transform firePoint,
        SpriteRenderer sortingReference,
        Vector2 direction)
    {
        if (firePoint == null || direction.sqrMagnitude <= 0.1f)
            return;

        EnsureMuzzleFlashSprite();

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        GameObject flashObject = new GameObject("Ranged Muzzle Flash");
        flashObject.layer = firePoint.gameObject.layer;
        flashObject.transform.SetPositionAndRotation(
            firePoint.position,
            Quaternion.Euler(0f, 0f, angle)
        );
        flashObject.transform.SetParent(firePoint, true);

        SpriteRenderer flashRenderer =
            flashObject.AddComponent<SpriteRenderer>();
        flashRenderer.sprite = muzzleFlashSprite;
        flashRenderer.color = new Color(1.2f, 0.82f, 0.42f, 1f);
        if (sortingReference != null)
        {
            flashRenderer.sortingLayerID = sortingReference.sortingLayerID;
            flashRenderer.sortingOrder = sortingReference.sortingOrder + 2;
        }

        float parentScaleX = Mathf.Max(
            0.0001f,
            Mathf.Abs(firePoint.lossyScale.x)
        );
        float localScale = 0.55f /
            (muzzleFlashSprite.bounds.size.x * parentScaleX);
        flashObject.transform.localScale = Vector3.one * localScale;

        AttackHitIconVisual visual =
            flashObject.AddComponent<AttackHitIconVisual>();
        visual.InitializeMuzzleFlash(flashRenderer);
    }

    public static void PlaySpellCastFlash(
        Transform firePoint,
        SpriteRenderer sortingReference,
        Vector2 direction)
    {
        if (firePoint == null || direction.sqrMagnitude <= 0.0001f)
            return;

        EnsureSpellCastFlashSprite();

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        GameObject flashObject = new GameObject("Arcane Cast Flash");
        flashObject.layer = firePoint.gameObject.layer;
        flashObject.transform.SetPositionAndRotation(
            firePoint.position,
            Quaternion.Euler(0f, 0f, angle)
        );
        flashObject.transform.SetParent(firePoint, true);

        SpriteRenderer flashRenderer =
            flashObject.AddComponent<SpriteRenderer>();
        flashRenderer.sprite = spellCastFlashSprite;
        flashRenderer.color = new Color(0.3f, 0.1f, 2f, 1f);
        if (sortingReference != null)
        {
            flashRenderer.sortingLayerID = sortingReference.sortingLayerID;
            flashRenderer.sortingOrder = sortingReference.sortingOrder + 3;
        }

        float parentScaleX = Mathf.Max(
            0.0001f,
            Mathf.Abs(firePoint.lossyScale.x)
        );
        float localScale = 0.48f /
            (spellCastFlashSprite.bounds.size.x * parentScaleX);
        flashObject.transform.localScale = Vector3.one * localScale;

        AttackHitIconVisual visual =
            flashObject.AddComponent<AttackHitIconVisual>();
        visual.InitializeSpellCastFlash(flashRenderer);
    }

    private static void EnsureSpellCastFlashSprite()
    {
        if (spellCastFlashSprite != null)
            return;

        const int textureSize = 32;
        Texture2D texture = new Texture2D(
            textureSize,
            textureSize,
            TextureFormat.RGBA32,
            false
        );
        texture.name = "ArcaneCastFlashTexture";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[textureSize * textureSize];
        Vector2 center = new Vector2(
            (textureSize - 1) * 0.5f,
            (textureSize - 1) * 0.5f
        );
        float outerRadius = textureSize * 0.48f;
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                Vector2 offset = new Vector2(x, y) - center;
                float distance = offset.magnitude;
                float radialGlow = Mathf.Clamp01(1f - distance / outerRadius);
                float starArm =
                    Mathf.Clamp01(
                        1f - Mathf.Min(
                            Mathf.Abs(offset.x),
                            Mathf.Abs(offset.y)
                        ) / 2f
                    ) *
                    Mathf.Clamp01(1f - distance / (textureSize * 0.55f));
                float alpha = Mathf.Max(radialGlow * 0.5f, starArm);
                pixels[y * textureSize + x] =
                    new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        spellCastFlashSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            textureSize
        );
        spellCastFlashSprite.name = "ArcaneCastFlash";
    }

    private static void EnsureMuzzleFlashSprite()
    {
        if (muzzleFlashSprite != null)
            return;

        const int width = 32;
        const int height = 16;
        Texture2D texture = new Texture2D(
            width,
            height,
            TextureFormat.RGBA32,
            false
        );
        texture.name = "RangedMuzzleFlashTexture";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[width * height];
        Vector2 center = new Vector2(
            (width - 1) * 0.5f,
            (height - 1) * 0.5f
        );
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 offset = new Vector2(x, y) - center;
                float horizontalFlare =
                    Mathf.Clamp01(1f - Mathf.Abs(offset.x) / (width * 0.5f)) *
                    Mathf.Clamp01(1f - Mathf.Abs(offset.y) / 2.5f);
                float centerGlow =
                    Mathf.Clamp01(1f - offset.sqrMagnitude / 28f);
                float alpha = Mathf.Max(horizontalFlare, centerGlow);
                pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        muzzleFlashSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            width
        );
        muzzleFlashSprite.name = "RangedMuzzleFlash";
    }

    public static void PlayRisingParticles(
        Transform targetTransform,
        SpriteRenderer targetRenderer,
        Sprite particleSprite,
        Color color,
        int particleCount,
        float particleWorldHeight)
    {
        if (targetTransform == null || particleSprite == null || particleCount <= 0)
            return;

        Vector3 center = targetRenderer != null
            ? targetRenderer.bounds.center
            : targetTransform.position;
        int sortingLayerId = targetRenderer != null
            ? targetRenderer.sortingLayerID
            : 0;
        int sortingOrder = targetRenderer != null
            ? targetRenderer.sortingOrder + 1
            : 1;
        float parentScaleY = Mathf.Max(0.0001f, targetTransform.lossyScale.y);
        float spriteHeight = Mathf.Max(0.0001f, particleSprite.bounds.size.y);
        float localScale = Mathf.Max(0.01f, particleWorldHeight) /
            (spriteHeight * parentScaleY);

        for (int index = 0; index < particleCount; index++)
        {
            GameObject particle = new GameObject("Rising Particle");
            if (targetRenderer != null)
                particle.layer = targetRenderer.gameObject.layer;
            particle.transform.SetParent(targetTransform, false);
            particle.transform.position = center + new Vector3(
                Random.Range(-0.4f, 0.4f),
                Random.Range(-0.1f, 0.35f),
                0f
            );
            particle.transform.localScale = Vector3.one * localScale;

            SpriteRenderer renderer = particle.AddComponent<SpriteRenderer>();
            renderer.sprite = particleSprite;
            renderer.color = color;
            renderer.sortingLayerID = sortingLayerId;
            renderer.sortingOrder = sortingOrder;

            AttackHitIconVisual visual =
                particle.AddComponent<AttackHitIconVisual>();
            visual.InitializeRising(
                renderer,
                Random.Range(0.65f, 0.95f),
                Random.Range(0.45f, 0.8f)
            );
        }
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

    private void InitializeRising(
        SpriteRenderer renderer,
        float particleLifetime,
        float particleRiseDistance)
    {
        iconRenderer = renderer;
        startLocalPosition = transform.localPosition;
        isRisingParticle = true;
        lifetime = particleLifetime;
        riseDistance = particleRiseDistance;
    }

    private void InitializeMuzzleFlash(SpriteRenderer renderer)
    {
        iconRenderer = renderer;
        baseLocalScale = transform.localScale;
        isMuzzleFlash = true;
        lifetime = MuzzleFlashLifetime;
    }

    private void InitializeSpellCastFlash(SpriteRenderer renderer)
    {
        iconRenderer = renderer;
        baseLocalScale = transform.localScale;
        isSpellCastFlash = true;
        lifetime = SpellCastFlashLifetime;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float impactLifetime = impactSprites != null
            ? impactSprites.Length / impactFramesPerSecond
            : lifetime;
        float progress = Mathf.Clamp01(elapsed / impactLifetime);

        if (isMuzzleFlash)
        {
            float scale = progress < 0.2f
                ? Mathf.Lerp(0.55f, 1.2f, progress / 0.2f)
                : Mathf.Lerp(1.2f, 0.75f, (progress - 0.2f) / 0.8f);
            transform.localScale = baseLocalScale * scale;
        }
        else if (isSpellCastFlash)
        {
            transform.localScale =
                baseLocalScale * Mathf.Lerp(0.45f, 1.35f, progress);
            transform.Rotate(0f, 0f, 180f * Time.deltaTime);
        }
        else if (isRisingParticle)
        {
            transform.localPosition = startLocalPosition +
                Vector3.up * (riseDistance * progress);
        }
        else if (isImpactBurst)
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