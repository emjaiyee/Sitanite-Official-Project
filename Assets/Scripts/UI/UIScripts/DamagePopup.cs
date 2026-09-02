using TMPro;
using UnityEngine;

/// <summary>
/// World-space floating damage number. Spawns just above a target,
/// drifts only slightly, and fades in place before self-destroying.
/// Self-builds its own TextMeshPro if none is assigned, so it
/// can be spawned purely from code.
/// </summary>
public class DamagePopup : MonoBehaviour
{
    private const string EffectsSortingLayer = "Effects";
    private const int EffectsSortingOrder = 10000;

    [Header("Motion")]
    [Tooltip("Upward drift speed. Kept low so the number stays near the origin.")]
    [SerializeField] private float floatSpeed = 0.5f;
    [SerializeField] private float lifetime = 0.9f;
    [Tooltip("Small sideways jitter so stacked numbers don't perfectly overlap.")]
    [SerializeField] private Vector2 horizontalJitter = new Vector2(-0.12f, 0.12f);

    [Header("Visual")]
    [SerializeField] private float fontSize = 3f;
    [Tooltip("Optional font. Leave empty to use the TMP default font asset.")]
    [SerializeField] private TMP_FontAsset font;

    private TextMeshPro textMesh;
    private float elapsed;
    private float driftX;
    private Color baseColor = Color.white;

    private void Awake()
    {
        EnsureText();
    }

    /// <summary>Sets the displayed value and tint.</summary>
    public void Setup(float amount, Color color)
    {
        EnsureText();

        baseColor = color;

        int rounded = Mathf.RoundToInt(amount);
        textMesh.text = rounded.ToString();
        textMesh.color = baseColor;

        driftX = Random.Range(horizontalJitter.x, horizontalJitter.y);
        elapsed = 0f;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        // Float up + slight sideways drift.
        transform.position += new Vector3(
            driftX,
            floatSpeed,
            0f
        ) * Time.deltaTime;

        // Fade out over lifetime.
        float percent = Mathf.Clamp01(elapsed / lifetime);
        Color color = baseColor;
        color.a = 1f - percent;
        textMesh.color = color;

        if (elapsed >= lifetime)
            Destroy(gameObject);
    }

    private void EnsureText()
    {
        if (textMesh != null)
            return;

        textMesh = GetComponent<TextMeshPro>();

        if (textMesh == null)
            textMesh = gameObject.AddComponent<TextMeshPro>();

        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontSize = fontSize;
        textMesh.textWrappingMode = TextWrappingModes.NoWrap;

        if (font != null)
            textMesh.font = font;

        // Center the rect so it pops at the exact spawn point.
        RectTransform rect = textMesh.rectTransform;
        rect.sizeDelta = new Vector2(2f, 1f);
        MeshRenderer renderer = textMesh.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = EffectsSortingLayer;
            renderer.sortingOrder = EffectsSortingOrder;
        }
    }
}
