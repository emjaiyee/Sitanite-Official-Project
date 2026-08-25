using UnityEngine;
using UnityEngine.UI;

public class PlayerResourceUI : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private PlayerStats playerStats;

    [Header("UI References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private Image[] manaCrystals = new Image[3];

    [Header("Mana Crystal Display")]
    [Range(0f, 1f)][SerializeField] private float emptyOpacity = 0.23f;
    [SerializeField] private float manaReactivationDuration = 0.35f;

    private void Awake()
    {
        if (playerStats == null) playerStats = FindFirstObjectByType<PlayerStats>();
        if (staminaSlider == null) staminaSlider = FindSlider("Stamina_Slider");
        if (healthSlider == null) healthSlider = FindSlider("Health_Slider");
        if (manaCrystals == null || manaCrystals.Length != 3) manaCrystals = new Image[3];
        for (int i = 0; i < manaCrystals.Length; i++)
            if (manaCrystals[i] == null) manaCrystals[i] = FindImage(i == 0 ? "1stMana" : i == 1 ? "2ndMana " : "3rdMana");
    }

    private void OnEnable()
    {
        if (playerStats != null) playerStats.Changed += Refresh;
    }

    private void Start()
    {
        if (playerStats != null) Refresh(playerStats);
    }

    private void OnDisable()
    {
        if (playerStats != null) playerStats.Changed -= Refresh;
    }

    private void Refresh(PlayerStats stats)
    {
        SetSlider(healthSlider, stats.CurrentHealth, stats.MaxHealth);
        SetSlider(staminaSlider, stats.CurrentStamina, stats.MaxStamina);
        float manaPerCrystal = Mathf.Max(1f, stats.MaxMana / 3f);
        for (int i = 0; i < manaCrystals.Length; i++)
        {
            if (manaCrystals[i] == null) continue;
            float threshold = stats.MaxMana - (i + 1) * manaPerCrystal;
            bool active = stats.CurrentMana > threshold;
            Color color = manaCrystals[i].color;
            color.a = active ? 1f : emptyOpacity;
            manaCrystals[i].CrossFadeAlpha(color.a, manaReactivationDuration, true);
            manaCrystals[i].color = color;
        }
    }

    private static void SetSlider(Slider slider, int value, int maximum)
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

    private Image FindImage(string objectName)
    {
        Transform target = FindDeepChild(transform, objectName);
        return target == null ? null : target.GetComponent<Image>();
    }

    private static Transform FindDeepChild(Transform root, string objectName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == objectName) return child;
        return null;
    }
}
