using UnityEngine;

/// <summary>
/// Simple world-space health bar bound to an EnemyHealth component.
/// Uses a quad scaled from full (red) to empty (green->red).
/// Attach to the enemy; it auto-creates the bar above the sprite.
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class EnemyHealthBar : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private Vector2 barSize = new Vector2(0.8f, 0.12f);
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.75f, 0f);
    [SerializeField] private bool hideWhenFull = true;

    private EnemyHealth health;
    private Transform fill;
    private SpriteRenderer fillRenderer;
    private Transform root;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        CreateBar();
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnHealthChanged += HandleHealthChanged;

        UpdateBar();
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnHealthChanged -= HandleHealthChanged;
    }

    private void LateUpdate()
    {
        if (root == null)
            return;

        root.position = transform.position + offset;

        // Keep the bar facing the camera.
        if (Camera.main != null)
            root.rotation = Camera.main.transform.rotation;
    }

    private void HandleHealthChanged(EnemyHealth source)
    {
        UpdateBar();
    }

    private void UpdateBar()
    {
        if (fill == null || health == null)
            return;

        float percent = health.MaxHealth > 0
            ? Mathf.Clamp01((float)health.CurrentHealth / health.MaxHealth)
            : 0f;

        fill.localScale = new Vector3(percent, 1f, 1f);

        // Anchor the fill to the left so it drains right-to-left.
        fill.localPosition = new Vector3(-(1f - percent) * 0.5f, 0f, 0f);

        if (fillRenderer != null)
            fillRenderer.color = Color.Lerp(Color.red, Color.green, percent);

        if (hideWhenFull && root != null)
            root.gameObject.SetActive(percent < 1f && health.CurrentHealth > 0);
    }

    private void CreateBar()
    {
        root = new GameObject("HealthBar").transform;
        root.SetParent(transform, false);
        root.localPosition = offset;

        // Background
        SpriteRenderer background = CreateQuad("Background", root, new Color(0f, 0f, 0f, 0.6f));
        background.transform.localScale = new Vector3(barSize.x, barSize.y, 1f);

        // Fill
        fillRenderer = CreateQuad("Fill", root, Color.green);
        fill = fillRenderer.transform;
        fill.localScale = new Vector3(barSize.x, barSize.y, 1f);

        UpdateBar();
    }

    private SpriteRenderer CreateQuad(string name, Transform parent, Color color)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;

        Collider collider = quad.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        quad.transform.SetParent(parent, false);
        quad.transform.localPosition = Vector3.zero;

        SpriteRenderer renderer = quad.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = color;
            renderer.sortingOrder = 100;
        }

        return renderer;
    }
}
