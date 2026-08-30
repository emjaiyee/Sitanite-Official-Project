using UnityEngine;

/// <summary>
/// Entry point for spawning floating damage numbers anywhere.
/// Place one instance in the scene and assign the DamagePopup prefab;
/// falls back to a code-built popup when no prefab is assigned.
/// Also owns the per-damage-type color mapping.
/// </summary>
public class DamagePopupSpawner : MonoBehaviour
{
    private static DamagePopupSpawner Instance;

    [Tooltip("Prefab with a styled DamagePopup component. Built from code if empty.")]
    [SerializeField] private DamagePopup popupPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Spawns a damage number at a world position.</summary>
    public static void Spawn(Vector3 position, float amount, Color color)
    {
        if (amount <= 0f)
            return;

        DamagePopup popup;

        if (Instance != null && Instance.popupPrefab != null)
        {
            popup = Instantiate(
                Instance.popupPrefab,
                position,
                Quaternion.identity
            );
        }
        else
        {
            GameObject popupObject = new GameObject("DamagePopup");
            popupObject.transform.position = position;
            popup = popupObject.AddComponent<DamagePopup>();
        }

        popup.Setup(amount, color);
    }

    /// <summary>Spawns a damage number above a target transform.</summary>
    public static void Spawn(Transform target, float amount, Color color, float heightOffset = 0.8f)
    {
        if (target == null)
            return;

        Spawn(target.position + Vector3.up * heightOffset, amount, color);
    }

    /// <summary>Spawns a damage number colored by its damage type.</summary>
    public static void Spawn(Transform target, float amount, DamageType damageType, float heightOffset = 0.8f)
    {
        Spawn(target, amount, GetColor(damageType), heightOffset);
    }

    /// <summary>
    /// Color per damage type. Physical sub-types (Slash/Blunt/Pierce/Stab)
    /// are plain white; generic Physical is a darker gray-white.
    /// </summary>
    public static Color GetColor(DamageType damageType)
    {
        if ((damageType & (DamageType.Slash | DamageType.Blunt | DamageType.Pierce | DamageType.Stab)) != 0)
            return Color.white;
        if ((damageType & DamageType.Physical) != 0)
            return new Color(0.62f, 0.62f, 0.66f); // darker physical
        if ((damageType & DamageType.Lightning) != 0)
            return new Color(0.40f, 0.70f, 1.00f); // blue
        if ((damageType & DamageType.Fire) != 0)
            return new Color(1.00f, 0.45f, 0.15f); // orange
        if ((damageType & DamageType.Frost) != 0)
            return new Color(0.55f, 0.90f, 1.00f); // icy cyan
        if ((damageType & DamageType.Poison) != 0)
            return new Color(0.45f, 0.85f, 0.30f); // green
        if ((damageType & DamageType.Psychic) != 0)
            return new Color(0.80f, 0.50f, 1.00f); // purple
        if ((damageType & DamageType.Necrosis) != 0)
            return new Color(0.55f, 0.30f, 0.65f); // dark violet
        if ((damageType & DamageType.Water) != 0)
            return new Color(0.25f, 0.55f, 1.00f); // deep blue
        if ((damageType & DamageType.Earth) != 0)
            return new Color(0.65f, 0.50f, 0.30f); // brown
        if ((damageType & DamageType.Air) != 0)
            return new Color(0.85f, 0.95f, 0.95f); // pale white-cyan

        return Color.white;
    }
}
