using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Spell : ProjectileBase
{
    [Header("Visual Animation")]
    [SerializeField] private Sprite[] flightSprites;
    [Min(1f)] [SerializeField] private float framesPerSecond = 12f;
    [Tooltip("Loop the flight sprites. If off, the last frame holds until impact.")]
    [SerializeField] private bool loopAnimation = true;
    [SerializeField] private Light2D spellLight;

    private SpriteRenderer visualRenderer;

    protected override void OnInitialized()
    {
        visualRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spellLight == null)
            spellLight = GetComponentInChildren<Light2D>();

        if (spellLight != null)
            spellLight.lightType = Light2D.LightType.Sprite;

        if (visualRenderer != null && flightSprites != null && flightSprites.Length > 0)
            visualRenderer.sprite = flightSprites[0];
    }

    protected override void OnUpdate()
    {
        if (visualRenderer == null || flightSprites == null || flightSprites.Length == 0)
            return;

        float elapsed = Time.time - StartTime;
        int frameIndex = Mathf.FloorToInt(elapsed * framesPerSecond);

        frameIndex = loopAnimation
            ? frameIndex % flightSprites.Length
            : Mathf.Min(frameIndex, flightSprites.Length - 1);

        visualRenderer.sprite = flightSprites[frameIndex];

        if (spellLight != null)
            spellLight.lightCookieSprite = visualRenderer.sprite;
    }
}