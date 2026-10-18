using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterPortrait : MonoBehaviour
{
    [Header("Portrait Renderers")]
    [SerializeField] private Image bodyPortrait;
    [SerializeField] private Image eyesPortrait;
    [SerializeField] private Image hairPortrait;
    [SerializeField] private Image torsoPortrait;
    [SerializeField] private Image headwearPortrait;
    [SerializeField] private Image weaponPortrait;
    [SerializeField] private Image shieldPortrait;
    [SerializeField] private Image classPortrait;

    [Header("Damage Shake")]
    [Tooltip("Optional common parent of the paperdoll layers, excluding buttons. If empty, the layers shake together individually.")]
    [SerializeField] private RectTransform portraitShakeRoot;
    [SerializeField, Min(0.01f)] private float shakeDuration = 0.2f;
    [SerializeField, Min(0f)] private float minimumShakeDistance = 2f;
    [SerializeField, Min(0f)] private float maximumShakeDistance = 16f;
    [SerializeField, Min(1f)] private float shakeFrequency = 40f;

    [Header("Class Icons")]
    [SerializeField] private Sprite warriorIcon;
    [SerializeField] private Sprite rangerIcon;
    [SerializeField] private Sprite mageIcon;

    [Header("Portrait Buttons")]
    [SerializeField] private Button inventoryButton;
    [SerializeField] private Button statsButton;

    [Header("Stats Button Highlight")]
    [SerializeField] private Image statsButtonHighlight;
    [SerializeField] private Color highlightColor = new Color(1f, 0.8f, 0f, 0.5f);
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseMin = 0.9f;
    [SerializeField] private float pulseMax = 1.1f;
    [SerializeField] private float restingSize = 1f;

    [Header("Class Tooltip")]
    [SerializeField] private UIHoverTooltip classTooltip;
    [TextArea(2, 6)]
    [SerializeField] private string warriorDescription;
    [TextArea(2, 6)]
    [SerializeField] private string rangerDescription;
    [TextArea(2, 6)]
    [SerializeField] private string mageDescription;

    private CharacterCustomizationController customizationController;
    private PlayerInventory playerInventory;
    private PlayerStatsUI playerStatsUI;
    private PlayerAttributesNTraits playerAttributes;
    private PlayerStats playerStats;
    private Coroutine shakeRoutine;
    private readonly List<RectTransform> shakeTargets = new List<RectTransform>();
    private readonly List<Vector3> shakeOrigins = new List<Vector3>();

    private bool shouldHighlightStats;
    private float pulseTimer;

    private void Start()
    {
        if (classTooltip == null && classPortrait != null)
            classTooltip = classPortrait.GetComponent<UIHoverTooltip>();

        FindCustomizationController();

        if (customizationController == null)
        {
            Debug.LogError(
                "CharacterPortrait: Could not find CharacterCustomizationController.",
                this
            );

            return;
        }

        customizationController.OnAppearanceChanged += Refresh;
        FindPlayerUIReferences();
        BindButtons();
        SubscribeToAttributeChanges();
        if (playerStats != null)
        {
            playerStats.Changed += OnHealthChanged;
            playerStats.Damaged += OnDamaged;
        }

        Refresh();
        UpdateStatsButtonHighlight();
        
        // Set initial resting size
        if (statsButton != null && !shouldHighlightStats)
        {
            statsButton.transform.localScale = Vector3.one * restingSize;
        }
    }

    private void OnDestroy()
    {
        if (customizationController != null)
        {
            customizationController.OnAppearanceChanged -= Refresh;
        }

        UnsubscribeFromAttributeChanges();
        UnbindButtons();
        if (playerStats != null)
        {
            playerStats.Changed -= OnHealthChanged;
            playerStats.Damaged -= OnDamaged;
        }
    }

    private void OnDisable()
    {
        StopShake();
    }

    private void Update()
    {
        if (shouldHighlightStats && statsButton != null)
        {
            pulseTimer += Time.deltaTime * pulseSpeed;
            float scale = Mathf.Lerp(pulseMin, pulseMax, (Mathf.Sin(pulseTimer) + 1f) * 0.5f);
            statsButton.transform.localScale = Vector3.one * scale;
        }
    }

    private void FindCustomizationController()
    {
        if (Player.Instance == null)
        {
            Debug.LogError(
                "CharacterPortrait: Player.Instance is NULL!",
                this
            );

            return;
        }

        customizationController =
            Player.Instance.GetComponent<CharacterCustomizationController>();

        if (customizationController == null)
        {
            Debug.LogError(
                "CharacterPortrait: CharacterCustomizationController not found on Player!",
                Player.Instance
            );
        }
    }

    private void FindPlayerUIReferences()
    {
        if (Player.Instance == null)
            return;

        playerInventory = Player.Instance.GetComponent<PlayerInventory>();
        playerStatsUI = FindFirstObjectByType<PlayerStatsUI>(
            FindObjectsInactive.Include
        );
        playerAttributes = Player.Instance.GetComponent<PlayerAttributesNTraits>();
        playerStats = Player.Instance.GetComponent<PlayerStats>();
    }

    private void BindButtons()
    {
        if (inventoryButton != null && playerInventory != null)
            inventoryButton.onClick.AddListener(playerInventory.ToggleInventory);

        if (statsButton != null && playerStatsUI != null)
            statsButton.onClick.AddListener(playerStatsUI.ToggleStatsWindow);
    }

    private void UnbindButtons()
    {
        if (inventoryButton != null && playerInventory != null)
            inventoryButton.onClick.RemoveListener(playerInventory.ToggleInventory);

        if (statsButton != null && playerStatsUI != null)
            statsButton.onClick.RemoveListener(playerStatsUI.ToggleStatsWindow);
    }

    public void Refresh()
    {
        if (customizationController == null)
            return;

        CharacterAppearance appearance =
            customizationController.GetAppearance();

        SetPortrait(bodyPortrait, appearance.body);
        SetPortrait(eyesPortrait, appearance.eyes);
        SetPortrait(torsoPortrait, appearance.torso);
        SetPortrait(weaponPortrait, appearance.weapon);
        SetPortrait(shieldPortrait, appearance.shield);
        UpdateClassPortrait(appearance.playerClass);
        UpdateClassTooltip(appearance.playerClass);

        UpdateHairPortrait(appearance);
        UpdateHeadwearPortrait(appearance);
    }

    private void UpdateClassPortrait(PlayerClass playerClass)
    {
        if (classPortrait == null)
            return;

        Sprite icon = null;

        switch (playerClass)
        {
            case PlayerClass.Warrior:
                icon = warriorIcon;
                break;

            case PlayerClass.Ranger:
                icon = rangerIcon;
                break;

            case PlayerClass.Mage:
                icon = mageIcon;
                break;
        }

        classPortrait.sprite = icon;
        classPortrait.enabled = icon != null;
    }

    private void UpdateClassTooltip(PlayerClass playerClass)
    {
        if (classTooltip == null)
            return;

        string classDescription = playerClass switch
        {
            PlayerClass.Warrior => warriorDescription,
            PlayerClass.Ranger => rangerDescription,
            PlayerClass.Mage => mageDescription,
            _ => string.Empty
        };

        classTooltip.SetDescription(classDescription);
    }

    private void SetHairPortrait(CharacterAppearance appearance)
    {
        if (hairPortrait == null)
            return;

        // Hide portrait hair if the current headwear hides hair.
        if (appearance.headwear != null &&
            appearance.headwear.hidesHair)
        {
            hairPortrait.sprite = null;
            hairPortrait.enabled = false;
            return;
        }

        // Otherwise, display the currently selected hair.
        SetPortrait(hairPortrait, appearance.hair);
    }

    private void SetPortrait(
        Image image,
        CharacterPartDefinition definition)
    {
        if (image == null)
            return;

        float healthFraction = playerStats != null
            ? playerStats.CurrentHealth / playerStats.MaxHealth
            : 1f;
        image.sprite = definition != null ? definition.GetPortrait(healthFraction) : null;
        image.enabled = image.sprite != null;
    }

    private void OnHealthChanged(PlayerStats source)
    {
        if (customizationController == null)
            return;

        CharacterAppearance appearance = customizationController.GetAppearance();
        SetPortrait(bodyPortrait, appearance.body);
        SetPortrait(eyesPortrait, appearance.eyes);
    }

    private void OnDamaged(float damageTaken, float healthBeforeDamage)
    {
        if (!isActiveAndEnabled || damageTaken <= 0f || healthBeforeDamage <= 0f)
            return;

        StopShake();
        shakeTargets.Clear();
        shakeOrigins.Clear();

        if (portraitShakeRoot != null)
        {
            shakeTargets.Add(portraitShakeRoot);
        }
        else
        {
            Image[] layers = { bodyPortrait, eyesPortrait, hairPortrait, torsoPortrait,
                headwearPortrait, weaponPortrait, shieldPortrait };
            foreach (Image layer in layers)
            {
                if (layer != null && !shakeTargets.Contains(layer.rectTransform))
                    shakeTargets.Add(layer.rectTransform);
            }

            for (int targetIndex = shakeTargets.Count - 1; targetIndex >= 0; targetIndex--)
            {
                for (int parentIndex = 0; parentIndex < shakeTargets.Count; parentIndex++)
                {
                    if (targetIndex != parentIndex &&
                        shakeTargets[targetIndex].IsChildOf(shakeTargets[parentIndex]))
                    {
                        shakeTargets.RemoveAt(targetIndex);
                        break;
                    }
                }
            }
        }

        foreach (RectTransform target in shakeTargets)
            shakeOrigins.Add(target.localPosition);

        float intensity = Mathf.Clamp01(damageTaken / healthBeforeDamage);
        shakeRoutine = StartCoroutine(ShakePortrait(intensity));
    }

    private IEnumerator ShakePortrait(float intensity)
    {
        float duration = Mathf.Max(0.01f, shakeDuration);
        float distance = Mathf.Lerp(minimumShakeDistance, maximumShakeDistance, intensity);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float direction = Mathf.FloorToInt(elapsed * shakeFrequency) % 2 == 0 ? 1f : -1f;
            float offset = direction * distance * (1f - elapsed / duration);
            for (int targetIndex = 0; targetIndex < shakeTargets.Count; targetIndex++)
            {
                if (shakeTargets[targetIndex] != null)
                    shakeTargets[targetIndex].localPosition = shakeOrigins[targetIndex] + Vector3.right * offset;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        RestoreShakePositions();
        shakeRoutine = null;
    }

    private void StopShake()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        RestoreShakePositions();
    }

    private void RestoreShakePositions()
    {
        for (int targetIndex = 0; targetIndex < shakeTargets.Count; targetIndex++)
        {
            if (shakeTargets[targetIndex] != null)
                shakeTargets[targetIndex].localPosition = shakeOrigins[targetIndex];
        }
    }

    private void UpdateHairPortrait(CharacterAppearance appearance)
    {
        if (hairPortrait == null)
            return;

        if (appearance.headwear != null &&
            appearance.headwear.hidesHair &&
            !appearance.hideHeadwear)
        {
            hairPortrait.sprite = null;
            hairPortrait.enabled = false;
            return;
        }

        SetPortrait(hairPortrait, appearance.hair);
    }
    private void UpdateHeadwearPortrait(CharacterAppearance appearance)
    {
        if (headwearPortrait == null)
            return;

        if (appearance.hideHeadwear)
        {
            headwearPortrait.sprite = null;
            headwearPortrait.enabled = false;
            return;
        }

        SetPortrait(
            headwearPortrait,
            appearance.headwear
        );
    }

    private void SubscribeToAttributeChanges()
    {
        if (playerAttributes != null)
        {
            playerAttributes.Changed += OnAttributesChanged;
        }
    }

    private void UnsubscribeFromAttributeChanges()
    {
        if (playerAttributes != null)
        {
            playerAttributes.Changed -= OnAttributesChanged;
        }
    }

    private void OnAttributesChanged(PlayerAttributesNTraits source)
    {
        UpdateStatsButtonHighlight();
    }

    private void UpdateStatsButtonHighlight()
    {
        bool hasAvailablePoints = HasAvailablePoints();

        if (hasAvailablePoints != shouldHighlightStats)
        {
            shouldHighlightStats = hasAvailablePoints;

            if (statsButton != null)
            {
                if (shouldHighlightStats)
                {
                    pulseTimer = 0f;
                }
                else
                {
                    statsButton.transform.localScale = Vector3.one * restingSize;
                }
            }

            if (statsButtonHighlight != null)
            {
                statsButtonHighlight.enabled = shouldHighlightStats;
                if (shouldHighlightStats)
                {
                    statsButtonHighlight.color = highlightColor;
                }
            }
        }
    }

    private bool HasAvailablePoints()
    {
        if (playerAttributes == null)
            return false;

        return playerAttributes.AvailableAttributePoints > 0 ||
               playerAttributes.AvailableTraitPoints > 0;
    }
}