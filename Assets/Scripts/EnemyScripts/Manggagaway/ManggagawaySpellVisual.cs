using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ManggagawaySpellVisual : MonoBehaviour
{
    [SerializeField] private Sprite[] sprites;
    [Min(0.1f)] [SerializeField] private float framesPerSecond = 12f;
    [SerializeField] private bool loop = true;
    private SpriteRenderer spriteRenderer;
    private float startedAt;

    private void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();
    private void OnEnable() => startedAt = Time.time;

    private void Update()
    {
        if (sprites == null || sprites.Length == 0)
            return;
        int frame = Mathf.FloorToInt((Time.time - startedAt) * framesPerSecond);
        spriteRenderer.sprite = sprites[loop ? frame % sprites.Length : Mathf.Min(frame, sprites.Length - 1)];
    }
}
