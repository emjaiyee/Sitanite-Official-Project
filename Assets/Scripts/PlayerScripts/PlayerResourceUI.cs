using UnityEngine;
using UnityEngine.UI;

public class PlayerResourceUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider staminaSlider;
    [Tooltip("Assign the mana fill Images in left-to-right order. Each Image must use vertical Filled mode.")]
    [SerializeField] private Image[] manaBars = new Image[5];

    private PlayerStats playerStats;
    private bool isSubscribed;

    private void Awake()
    {
        if (staminaSlider == null)
            staminaSlider = FindSlider("Stamina_Slider");

        if (healthSlider == null)
            healthSlider = FindSlider("Health_Slider");

        if (manaBars == null || manaBars.Length < 5)
            System.Array.Resize(ref manaBars, 5);
    }

    private void OnEnable()
    {
        TryBindToPlayer();
    }

    private void Start()
    {
        TryBindToPlayer();
    }

    private void Update()
    {
        if (!isSubscribed)
            TryBindToPlayer();
    }

    private void OnDisable()
    {
        if (isSubscribed && playerStats != null)
            playerStats.Changed -= Refresh;

        isSubscribed = false;
    }

    private void TryBindToPlayer()
    {
        if (isSubscribed || Player.Instance == null)
            return;

        playerStats = Player.Instance.GetComponent<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogWarning("[PlayerResourceUI] Player.Instance has no PlayerStats component.");
            return;
        }

        playerStats.Changed += Refresh;
        isSubscribed = true;
        Refresh(playerStats);
    }

    private void Refresh(PlayerStats stats)
    {
        SetSlider(healthSlider, stats.CurrentHealth, stats.MaxHealth);
        SetSlider(staminaSlider, stats.CurrentStamina, stats.MaxStamina);

        float manaUnits = stats.MaxMana <= 0f
            ? 0f
            : Mathf.Clamp01(stats.CurrentMana / stats.MaxMana) * manaBars.Length;

        for (int i = 0; i < manaBars.Length; i++)
        {
            if (manaBars[i] == null)
                continue;

            manaBars[i].type = Image.Type.Filled;
            manaBars[i].fillMethod = Image.FillMethod.Vertical;
            manaBars[i].fillOrigin = (int)Image.OriginVertical.Bottom;

            int rightToLeftIndex = manaBars.Length - 1 - i;
            manaBars[i].fillAmount = Mathf.Clamp01(manaUnits - rightToLeftIndex);
        }
    }

    private static void SetSlider(Slider slider, float value, float maximum)
    {
        if (slider == null) return;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = maximum <= 0 ? 0f : Mathf.Clamp01(value / (float)maximum);
    }

    private Slider FindSlider(string objectName)
    {
        Transform target = FindDeepChild(transform, objectName);
        return target == null ? null : target.GetComponent<Slider>();
    }

    private static Transform FindDeepChild(Transform root, string objectName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == objectName) return child;
        return null;
    }
}
