using UnityEngine;
using UnityEngine.U2D.Animation; // Required for SpriteResolver

/// <summary>
/// Changes the sprite of a GameObject using SpriteResolver.
/// Attach this script to the same GameObject that has a SpriteResolver.
/// </summary>
[RequireComponent(typeof(SpriteResolver))]
public class SpriteChanger : MonoBehaviour
{
    private SpriteResolver spriteResolver;

    void Awake()
    {
        // Get the SpriteResolver component
        spriteResolver = GetComponent<SpriteResolver>();

        if (spriteResolver == null)
        {
            Debug.LogError("SpriteResolver component not found on this GameObject.");
        }
    }

    /// <summary>
    /// Change the sprite by specifying category and label.
    /// </summary>
    /// <param name="category">The category name in the Sprite Library.</param>
    /// <param name="label">The label name in the Sprite Library.</param>
    public void ChangeSprite(string category, string label)
    {
        if (spriteResolver == null)
        {
            Debug.LogWarning("Cannot change sprite. SpriteResolver is missing.");
            return;
        }

        // Validate inputs
        if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(label))
        {
            Debug.LogWarning("Category or label is empty. Sprite change aborted.");
            return;
        }

        // Change sprite
        spriteResolver.SetCategoryAndLabel(category, label);

        // Force update to apply immediately
        spriteResolver.ResolveSpriteToSpriteRenderer();
    }

    // Example: Change sprite when pressing space
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Example: Switch to "Idle" sprite in "Character" category
            ChangeSprite("Character", "Idle");
        }
    }
}
