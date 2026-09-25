using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

    [Header("Collider References")]
    [SerializeField] private Collider2D rampMovementCollider;

    public Collider2D RampMovementCollider => rampMovementCollider;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "Multiple Player instances found! Destroying duplicate.",
                this
            );

            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (GetComponent<EquipmentCharacterVisualSync>() == null)
        {
            gameObject.AddComponent<EquipmentCharacterVisualSync>();
        }

        if (GetComponent<PlayerRoomTracker>() == null)
            gameObject.AddComponent<PlayerRoomTracker>();

        DontDestroyOnLoad(gameObject);

        Debug.Log(
            "PLAYER AWAKE — Player.Instance has been assigned.",
            this
        );
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}