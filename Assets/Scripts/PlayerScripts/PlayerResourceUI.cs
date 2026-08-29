using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerResourceUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private Slider experienceSlider;
    [Tooltip("Assign the mana fill Images in left-to-right order. Each Image must use vertical Filled mode.")]
    [SerializeField] private Image[] manaBars = new Image[5];

    [Header("Experience Fill Animation")]
    [SerializeField] private Image experienceFillImage;
    [SerializeField] private Sprite[] experienceFillSprites;
    [SerializeField, Min(0.01f)] private float experienceFillSpriteFrameDuration = 0.08f;

    [Header("Experience Handle Animation")]
    [SerializeField] private Image experienceHandleImage;
    [SerializeField] private Sprite[] experienceHandleSprites;
    [SerializeField, Min(0.01f)] private float experienceHandleSpriteFrameDuration = 0.08f;

    [SerializeField, Min(0f)] private float experienceSliderAnimationDuration = 0.2f;

    private PlayerStats playerStats;
    private bool isSubscribed;
    private Coroutine experienceSliderRoutine;
    private Coroutine experienceFillSpriteRoutine;
    private Coroutine experienceHandleSpriteRoutine;

    private void Awake()
    {
        if (staminaSlider == null)
            staminaSlider = FindSlider("Stamina_Slider");

        if (healthSlider == null)
            healthSlider = FindSlider("Health_Slider");

        if (experienceSlider == null)
            experienceSlider = FindSlider("Experience_Slider");

        if (manaBars == null || manaBars.Length < 5)
            System.Array.Resize(ref manaBars, 5);

        ResolveExperienceFillImage();
        ResolveExperienceHandleImage();
    }

    private void OnEnable()
    {
        TryBindToPlayer();
        StartExperienceAnimations();
    }

    private void Start()
    {
        TryBindToPlayer();
        StartExperienceAnimations();
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

        if (experienceSliderRoutine != null)
        {
            StopCoroutine(experienceSliderRoutine);
            experienceSliderRoutine = null;
        }

        if (experienceFillSpriteRoutine != null)
        {
            StopCoroutine(experienceFillSpriteRoutine);
            experienceFillSpriteRoutine = null;
        }

        if (experienceHandleSpriteRoutine != null)
        {
            StopCoroutine(experienceHandleSpriteRoutine);
            experienceHandleSpriteRoutine = null;
        }
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
        RefreshExperienceBar(stats);

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

    private void RefreshExperienceBar(PlayerStats stats)
    {
        if (experienceSlider == null)
            return;

        AnimateExperienceSlider(stats.ExperienceToNextLevel <= 0f
            ? 0f
            : Mathf.Clamp01(
                stats.ExperiencePoints /
                stats.ExperienceToNextLevel
            ));
    }

    private void AnimateExperienceSlider(float targetValue)
    {
        if (experienceSlider == null)
            return;

        if (experienceSliderRoutine != null)
            StopCoroutine(experienceSliderRoutine);

        experienceSliderRoutine = StartCoroutine(AnimateExperienceSliderRoutine(targetValue));
    }

    private IEnumerator AnimateExperienceSliderRoutine(float targetValue)
    {
        float startValue = experienceSlider.value;
        float duration = Mathf.Max(0f, experienceSliderAnimationDuration);

        if (duration <= 0f)
        {
            experienceSlider.SetValueWithoutNotify(targetValue);
            experienceSliderRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            experienceSlider.SetValueWithoutNotify(Mathf.Lerp(startValue, targetValue, t));

            yield return null;
        }

        experienceSlider.SetValueWithoutNotify(targetValue);
        experienceSliderRoutine = null;
    }

    private void ResolveExperienceFillImage()
    {
        if (experienceFillImage != null)
            return;

        if (experienceSlider == null || experienceSlider.fillRect == null)
            return;

        experienceFillImage = experienceSlider.fillRect.GetComponent<Image>();
    }

    private void ResolveExperienceHandleImage()
    {
        if (experienceHandleImage != null)
            return;

        if (experienceSlider == null || experienceSlider.handleRect == null)
            return;

        experienceHandleImage = experienceSlider.handleRect.GetComponent<Image>();
    }

    private void StartExperienceFillAnimation()
    {
        ResolveExperienceFillImage();

        if (experienceFillSpriteRoutine != null)
        {
            StopCoroutine(experienceFillSpriteRoutine);
            experienceFillSpriteRoutine = null;
        }

        if (experienceFillImage == null || experienceFillSprites == null || experienceFillSprites.Length == 0)
            return;

        experienceFillSpriteRoutine = StartCoroutine(AnimateExperienceFillSprites());
    }

    private void StartExperienceHandleAnimation()
    {
        ResolveExperienceHandleImage();

        if (experienceHandleSpriteRoutine != null)
        {
            StopCoroutine(experienceHandleSpriteRoutine);
            experienceHandleSpriteRoutine = null;
        }

        if (experienceHandleImage == null || experienceHandleSprites == null || experienceHandleSprites.Length == 0)
            return;

        experienceHandleSpriteRoutine = StartCoroutine(AnimateExperienceHandleSprites());
    }

    private void StartExperienceAnimations()
    {
        StartExperienceFillAnimation();
        StartExperienceHandleAnimation();
    }

    private IEnumerator AnimateExperienceFillSprites()
    {
        int frameIndex = 0;

        while (true)
        {
            if (experienceFillImage == null || experienceFillSprites == null || experienceFillSprites.Length == 0)
            {
                experienceFillSpriteRoutine = null;
                yield break;
            }

            Sprite sprite = experienceFillSprites[frameIndex];
            if (sprite != null)
                experienceFillImage.sprite = sprite;

            frameIndex = (frameIndex + 1) % experienceFillSprites.Length;

            yield return new WaitForSecondsRealtime(experienceFillSpriteFrameDuration);
        }
    }

    private IEnumerator AnimateExperienceHandleSprites()
    {
        int frameIndex = 0;

        while (true)
        {
            if (experienceHandleImage == null || experienceHandleSprites == null || experienceHandleSprites.Length == 0)
            {
                experienceHandleSpriteRoutine = null;
                yield break;
            }

            Sprite sprite = experienceHandleSprites[frameIndex];
            if (sprite != null)
                experienceHandleImage.sprite = sprite;

            frameIndex = (frameIndex + 1) % experienceHandleSprites.Length;

            yield return new WaitForSecondsRealtime(experienceHandleSpriteFrameDuration);
        }
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
