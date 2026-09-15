using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BreakablePot : MonoBehaviour, IDamageable
{
    [Header("References")]
    [SerializeField] private SpriteRenderer potRenderer;
    [SerializeField] private Sprite brokenSprite;
    [SerializeField] private Transform spawnPoint;

    private bool broken;
    private Collider2D potCollider;
    private Vector2 intactSpriteSize;
    private Vector3 intactRendererScale;

    public bool IsBroken => broken;

    private void Awake()
    {
        if (potRenderer == null)
            potRenderer = GetComponent<SpriteRenderer>();

        potCollider = GetComponent<Collider2D>();

        if (potRenderer != null && potRenderer.sprite != null)
        {
            intactSpriteSize = potRenderer.sprite.bounds.size;
            intactRendererScale = potRenderer.transform.localScale;
        }
    }

    public void TakeDamage(int amount, DamageType damageType = DamageType.Slash, Vector3? damageSource = null)
    {
        TryBreak();
    }

    public bool TryBreak()
    {
        if (broken) return false;

        broken = true;

        if (potRenderer != null && brokenSprite != null)
        {
            potRenderer.sprite = brokenSprite;
            PreserveSpriteSize();
        }

        if (potCollider != null)
            potCollider.enabled = false;

        BreakablePotManager.Instance?.SpawnLoot(
            spawnPoint != null ? spawnPoint.position : transform.position
        );
        return true;
    }

    private void PreserveSpriteSize()
    {
        Vector2 brokenSpriteSize = potRenderer.sprite.bounds.size;
        if (intactSpriteSize.x <= 0f || intactSpriteSize.y <= 0f ||
            brokenSpriteSize.x <= 0f || brokenSpriteSize.y <= 0f)
            return;

        potRenderer.transform.localScale = new Vector3(
            intactRendererScale.x * intactSpriteSize.x / brokenSpriteSize.x,
            intactRendererScale.y * intactSpriteSize.y / brokenSpriteSize.y,
            intactRendererScale.z
        );
    }
}