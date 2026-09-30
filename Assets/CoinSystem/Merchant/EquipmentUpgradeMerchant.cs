using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

[AddComponentMenu("NPC/Equipment Upgrade Merchant")]
public class EquipmentUpgradeMerchant : MonoBehaviour
{
    private static EquipmentUpgradeMerchant activeMerchant;
    private static EquipmentUpgradeMerchant activeUpgrade;

    [Header("Interaction")]
    [SerializeField] private string merchantName = "Master Smith";
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private Vector3 promptOffset = new Vector3(0f, 1.2f, 0f);

    [Header("Upgrade Pricing")]
    [Min(1)] [SerializeField] private int baseUpgradeCost = 100;
    [Min(1.01f)] [SerializeField] private float costMultiplier = 1.5f;

    [Header("Upgrade Work")]
    [Min(0.1f)] [SerializeField] private float baseWorkDuration = 1.5f;
    [Min(0f)] [SerializeField] private float additionalWorkPerUpgradeLevel = 0.75f;
    [Tooltip("Optional transform above the merchant where the progress bar should appear.")]
    [SerializeField] private Transform progressBarAnchor;
    [SerializeField] private Vector3 progressBarOffset = new Vector3(0f, 1.8f, 0f);

    [Header("Custom UI (Optional)")]
    [Tooltip("Assign your own Canvas and panel references to control the complete merchant layout. Leave empty to use the generated layout.")]
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private GameObject customPanelRoot;
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private Button openUpgradePanelButton;
    [SerializeField] private Button[] closePanelButtons;
    [SerializeField] private MerchantEquipmentOptionUI equipmentOptionPrefab;
    [SerializeField] private TextMeshProUGUI upgradeButtonLabel;

    private static readonly EquipmentType[] UpgradeableSlots =
    {
        EquipmentType.Weapon,
        EquipmentType.Helmet,
        EquipmentType.Chestplate,
        EquipmentType.Legging,
        EquipmentType.Shield
    };

    private Canvas promptCanvas;
    private TextMeshProUGUI promptText;
    [Header("Custom UI References")]
    [Tooltip("Parent where equipped gear option prefabs are added. Add a layout group here to control their arrangement.")]
    [SerializeField] private Transform equipmentListParent;
    [Tooltip("Text displaying the player's current coin count.")]
    [SerializeField] private TextMeshProUGUI coinText;
    [Tooltip("Text area displaying the selected item's upgrade preview.")]
    [SerializeField] private TextMeshProUGUI equipmentDetails;
    [Tooltip("Text area for upgrade results and validation messages.")]
    [SerializeField] private TextMeshProUGUI feedbackText;
    [Tooltip("Optional text displaying the player's class.")]
    [SerializeField] private TextMeshProUGUI playerClassText;
    [Tooltip("Optional placeholder shown when no player sprite preview is available.")]
    [SerializeField] private TextMeshProUGUI playerPreviewPlaceholder;
    [Tooltip("Optional RectTransform used as the live character preview area.")]
    [SerializeField] private RectTransform playerPreviewRoot;
    [Tooltip("Button that performs the selected upgrade.")]
    [SerializeField] private Button upgradeButton;
    [Tooltip("Optional icons ordered as Weapon, Helmet, Chestplate, Legging, Shield.")]
    [SerializeField] private Image[] equippedSlotIcons;
    [Tooltip("Optional buttons ordered North, South, West, East for custom preview layouts.")]
    [SerializeField] private Button[] playerPreviewDirectionButtons;
    private readonly List<GameObject> equipmentButtons = new List<GameObject>();
    private readonly List<InventoryItem> equipmentButtonItems = new List<InventoryItem>();
    private readonly List<MerchantEquipmentOptionUI> customEquipmentButtons = new List<MerchantEquipmentOptionUI>();
    private readonly List<SpriteRenderer> playerPreviewRenderers = new List<SpriteRenderer>();
    private readonly List<Image> playerPreviewImages = new List<Image>();
    private CharacterRenderer previewCharacter;
    private CharacterDirection previewDirection = CharacterDirection.South;
    private Canvas upgradeProgressCanvas;
    private Image upgradeProgressFill;
    private TextMeshProUGUI upgradeProgressLabel;
    private RectTransform upgradeHammer;
    private InventoryItem itemBeingUpgraded;
    private float upgradeWorkElapsed;
    private float upgradeWorkDuration;
    private float upgradeResultElapsed;
    private int pendingUpgradeCost;
    private bool upgradeInProgress;
    private bool upgradeResultVisible;
    private InventoryItem selectedItem;
    private PlayerWASD lockedMovement;
    private PlayerDash lockedDash;
    private bool lockedMovementByMerchant;
    private bool lockedDashByMerchant;
    private bool uiInitialized;

    public static bool IsUIOpen => activeMerchant != null;

    private void Awake()
    {
        if (customPanelRoot != null)
            customPanelRoot.SetActive(false);
        else if (uiCanvas != null)
            uiCanvas.gameObject.SetActive(false);

        CreatePrompt();
    }

    private void Update()
    {
        Player player = Player.Instance;
        bool inRange = player != null &&
            Vector2.Distance(transform.position, player.transform.position) <= interactionDistance;

        if (promptCanvas != null)
            promptCanvas.gameObject.SetActive(
                inRange && !IsUIOpen && activeUpgrade == null && !upgradeInProgress
            );

        if (inRange &&
            !IsUIOpen &&
            activeUpgrade == null &&
            !upgradeInProgress &&
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            OpenConfirmation();
        }

        if (upgradeInProgress)
            UpdateUpgradeWork();
        else if (upgradeResultVisible)
            UpdateUpgradeResult();

        if (upgradePanel != null && upgradePanel.activeInHierarchy)
        {
            UpdatePlayerPreview();
        }
    }

    private void OnDisable()
    {
        if (activeMerchant == this)
            CloseUI();
        if (upgradeInProgress || upgradeResultVisible)
            CancelUpgradeWork();
    }

    private void OnDestroy()
    {
        if (activeMerchant == this)
            CloseUI();
        if (activeUpgrade == this)
            CancelUpgradeWork();
    }

    private void CreatePrompt()
    {
        GameObject promptObject = new GameObject(
            "Merchant Interaction Prompt",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );
        promptObject.transform.SetParent(transform, false);
        promptObject.transform.localPosition = promptOffset;
        promptObject.transform.localScale = Vector3.one * 0.01f;
        promptCanvas = promptObject.GetComponent<Canvas>();
        promptCanvas.renderMode = RenderMode.WorldSpace;
        promptCanvas.sortingOrder = 50;

        RectTransform canvasRect = promptObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(220f, 42f);

        GameObject labelObject = new GameObject(
            "Prompt",
            typeof(RectTransform),
            typeof(Image)
        );
        labelObject.transform.SetParent(promptObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Image background = labelObject.GetComponent<Image>();
        background.color = new Color(0.06f, 0.06f, 0.09f, 0.9f);
        GameObject textObject = new GameObject(
            "Prompt Text",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );
        textObject.transform.SetParent(labelObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        promptText = textObject.GetComponent<TextMeshProUGUI>();
        promptText.font = GetUIFontAsset();
        promptText.fontSize = 26;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.color = Color.white;
        promptText.text = "Press E to talk";
        promptText.raycastTarget = false;
        promptObject.SetActive(false);
    }

    private void OpenConfirmation()
    {
        if (activeMerchant != null ||
            activeUpgrade != null ||
            (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsOpen))
            return;

        activeMerchant = this;
        PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
        if (inventory != null && inventory.IsOpen)
            inventory.SetInventoryState(false);

        PlayerStatsUI statsUI = FindFirstObjectByType<PlayerStatsUI>();
        if (statsUI != null && statsUI.IsOpen)
            statsUI.SetStatsWindowState(false);

        LockPlayerControls();
        if (!EnsureUI())
        {
            activeMerchant = null;
            UnlockPlayerControls();
            return;
        }

        if (customPanelRoot != null)
            customPanelRoot.SetActive(true);
        dialogPanel.SetActive(true);
        upgradePanel.SetActive(false);
        if (customPanelRoot == null)
            uiCanvas.gameObject.SetActive(true);
    }

    private bool EnsureUI()
    {
        if (uiInitialized)
            return true;

        EnsureEventSystem();

        if (uiCanvas != null)
        {
            if (customPanelRoot == null || dialogPanel == null || upgradePanel == null || equipmentListParent == null ||
                upgradeButton == null || openUpgradePanelButton == null ||
                closePanelButtons == null || closePanelButtons.Length == 0 ||
                coinText == null ||
                equipmentDetails == null || feedbackText == null)
            {
                Debug.LogError(
                    "EquipmentUpgradeMerchant: Custom UI requires a panel root, dialog and upgrade panels, " +
                    "an equipment list parent, open/close/upgrade buttons, and coin/details/feedback text references.",
                    this
                );
                return false;
            }

            ConfigureCustomUI();
            uiInitialized = true;
            return true;
        }

        GameObject canvasObject = new GameObject(
            "Equipment Upgrade UI",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );
        canvasObject.transform.SetParent(transform, false);
        uiCanvas = canvasObject.GetComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject backdrop = CreateImage(
            "Backdrop",
            canvasObject.transform,
            new Vector2(1920f, 1080f),
            Vector2.zero,
            new Color(0f, 0f, 0f, 0.72f)
        );
        backdrop.GetComponent<Image>().raycastTarget = true;

        GameObject frame = CreateImage(
            "Merchant Panel",
            backdrop.transform,
            new Vector2(1480f, 820f),
            Vector2.zero,
            new Color(0.075f, 0.08f, 0.12f, 1f)
        );
        AddText(frame.transform, "Title", merchantName, new Vector2(0f, 350f),
            new Vector2(1320f, 58f), 40, TextAlignmentOptions.Center, new Color(1f, 0.82f, 0.45f));
        AddText(frame.transform, "Subtitle", "A better edge for the road ahead",
            new Vector2(0f, 315f), new Vector2(1000f, 30f), 18,
            TextAlignmentOptions.Center, new Color(0.72f, 0.75f, 0.82f));

        dialogPanel = CreateRect("Confirmation", frame.transform, new Vector2(900f, 440f), Vector2.zero);
        AddText(dialogPanel.transform, "Question", "Ready to strengthen your gear?",
            new Vector2(0f, 65f), new Vector2(820f, 90f), 34, TextAlignmentOptions.Center, Color.white);
        AddText(dialogPanel.transform, "Description", "Choose an equipped weapon or armor piece to improve.",
            new Vector2(0f, 8f), new Vector2(760f, 42f), 19,
            TextAlignmentOptions.Center, new Color(0.72f, 0.75f, 0.82f));
        CreateButton(dialogPanel.transform, "Yes", "View equipment", new Vector2(-125f, -65f),
            new Vector2(230f, 62f), OpenUpgradePanel);
        CreateButton(dialogPanel.transform, "No thanks", "Not now", new Vector2(125f, -65f),
            new Vector2(230f, 62f), CloseUI);

        upgradePanel = CreateRect("Upgrade Panel", frame.transform, new Vector2(1380f, 680f), new Vector2(0f, -12f));
        GameObject characterCard = CreateImage(
            "Character Preview Card",
            upgradePanel.transform,
            new Vector2(390f, 590f),
            new Vector2(-490f, -4f),
            new Color(0.12f, 0.15f, 0.2f, 1f)
        );
        AddText(characterCard.transform, "Preview Heading", "YOUR CHARACTER",
            new Vector2(0f, 258f), new Vector2(350f, 34f), 19,
            TextAlignmentOptions.Center, new Color(1f, 0.82f, 0.45f));
        GameObject previewSurface = CreateImage(
            "Character Preview Surface",
            characterCard.transform,
            new Vector2(340f, 375f),
            new Vector2(0f, 34f),
            new Color(0.08f, 0.38f, 0.4f, 1f)
        );
        playerPreviewRoot = previewSurface.GetComponent<RectTransform>();
        playerPreviewPlaceholder = AddText(previewSurface.transform, "Preview Placeholder", "Character preview unavailable",
            Vector2.zero, new Vector2(300f, 50f), 16,
            TextAlignmentOptions.Center, new Color(0.8f, 0.84f, 0.88f));
        playerPreviewDirectionButtons = new[]
        {
            CreateButton(previewSurface.transform, "Face North", "\u2191", new Vector2(0f, 158f),
                new Vector2(42f, 42f), FacePreviewNorth),
            CreateButton(previewSurface.transform, "Face South", "\u2193", new Vector2(0f, -158f),
                new Vector2(42f, 42f), FacePreviewSouth),
            CreateButton(previewSurface.transform, "Face West", "\u2190", new Vector2(-145f, 0f),
                new Vector2(42f, 42f), FacePreviewWest),
            CreateButton(previewSurface.transform, "Face East", "\u2192", new Vector2(145f, 0f),
                new Vector2(42f, 42f), FacePreviewEast)
        };
        playerClassText = AddText(characterCard.transform, "Player Class", "CURRENT LOADOUT",
            new Vector2(0f, -188f), new Vector2(350f, 28f), 14,
            TextAlignmentOptions.Center, new Color(0.72f, 0.75f, 0.82f));
        AddText(characterCard.transform, "Loadout Heading", "EQUIPPED",
            new Vector2(0f, -220f), new Vector2(350f, 24f), 13,
            TextAlignmentOptions.Center, new Color(1f, 0.82f, 0.45f));
        equippedSlotIcons = new Image[UpgradeableSlots.Length];
        for (int index = 0; index < UpgradeableSlots.Length; index++)
        {
            float x = (index - (UpgradeableSlots.Length - 1) * 0.5f) * 68f;
            GameObject slotCard = CreateImage(
                $"Loadout Slot {UpgradeableSlots[index]}",
                characterCard.transform,
                new Vector2(60f, 66f),
                new Vector2(x, -264f),
                new Color(0.08f, 0.1f, 0.14f, 1f)
            );
            Image icon = CreateImage(
                "Icon",
                slotCard.transform,
                new Vector2(38f, 38f),
                new Vector2(0f, 8f),
                new Color(0.56f, 0.59f, 0.65f, 0.55f)
            ).GetComponent<Image>();
            icon.preserveAspect = true;
            equippedSlotIcons[index] = icon;
            AddText(slotCard.transform, "Slot Name", GetSlotLabel(UpgradeableSlots[index]),
                new Vector2(0f, -25f), new Vector2(58f, 18f), 9,
                TextAlignmentOptions.Center, new Color(0.8f, 0.82f, 0.86f));
        }

        GameObject equipmentFrame = CreateImage(
            "Upgrade Details Card",
            upgradePanel.transform,
            new Vector2(950f, 590f),
            new Vector2(205f, -4f),
            new Color(0.12f, 0.15f, 0.2f, 1f)
        );
        equipmentListParent = equipmentFrame.transform;
        AddText(equipmentFrame.transform, "Instruction", "Select an equipped item to preview its next upgrade",
            new Vector2(0f, 252f), new Vector2(880f, 34f), 18, TextAlignmentOptions.Center, Color.white);
        coinText = AddText(equipmentFrame.transform, "Coins", "COINS  0",
            new Vector2(0f, 215f), new Vector2(880f, 32f), 19, TextAlignmentOptions.Center,
            new Color(1f, 0.82f, 0.45f));
        CreateImage("Section Divider", equipmentFrame.transform,
            new Vector2(820f, 2f), new Vector2(0f, 190f), new Color(0.35f, 0.39f, 0.46f, 0.65f));
        equipmentDetails = AddText(equipmentFrame.transform, "Upgrade Details", "Select equipped gear to see its upgrade.",
            new Vector2(0f, -82f), new Vector2(850f, 205f), 16, TextAlignmentOptions.TopLeft, Color.white);
        feedbackText = AddText(equipmentFrame.transform, "Feedback", string.Empty,
            new Vector2(0f, -205f), new Vector2(850f, 26f), 15, TextAlignmentOptions.Center,
            new Color(1f, 0.78f, 0.42f));
        upgradeButton = CreateButton(equipmentFrame.transform, "Upgrade Button", "Upgrade",
            new Vector2(205f, -250f), new Vector2(250f, 56f), UpgradeSelectedItem);
        CreateButton(equipmentFrame.transform, "No thanks", "No thanks",
            new Vector2(-205f, -250f), new Vector2(250f, 56f), CloseUI);

        BindPreviewDirectionButtons();
        uiCanvas.gameObject.SetActive(false);
        uiInitialized = true;
        return true;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystem = new GameObject(
            "Merchant EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule)
        );
        eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    private void ConfigureCustomUI()
    {
        openUpgradePanelButton.onClick.RemoveListener(OpenUpgradePanel);
        openUpgradePanelButton.onClick.AddListener(OpenUpgradePanel);

        foreach (Button closeButton in closePanelButtons)
        {
            if (closeButton == null)
                continue;

            closeButton.onClick.RemoveListener(CloseUI);
            closeButton.onClick.AddListener(CloseUI);
        }

        upgradeButton.onClick.RemoveListener(UpgradeSelectedItem);
        upgradeButton.onClick.AddListener(UpgradeSelectedItem);
        BindPreviewDirectionButtons();
    }

    private void OpenUpgradePanel()
    {
        dialogPanel.SetActive(false);
        upgradePanel.SetActive(true);
        selectedItem = null;
        feedbackText.text = string.Empty;
        BuildPlayerPreview();
        RefreshCharacterLoadout();
        RefreshEquipmentOptions();
        UpdateSelectedItemDetails();
    }

    private void RefreshEquipmentOptions()
    {
        foreach (GameObject button in equipmentButtons)
        {
            if (button != null)
                Destroy(button);
        }
        equipmentButtons.Clear();
        equipmentButtonItems.Clear();
        foreach (MerchantEquipmentOptionUI button in customEquipmentButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }
        customEquipmentButtons.Clear();

        EquipmentManager equipment = EquipmentManager.Instance;
        List<InventoryItem> upgradeableItems = new List<InventoryItem>();
        HashSet<InventoryItem> addedItems = new HashSet<InventoryItem>();
        if (equipment != null)
        {
            foreach (EquipmentType slot in UpgradeableSlots)
            {
                InventoryItem item = equipment.GetEquippedItem(slot);
                if (IsUpgradeableItem(item) && addedItems.Add(item))
                    upgradeableItems.Add(item);
            }
        }

        PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
        InventoryGrid backpack = inventory != null ? inventory.MainBackPack : null;
        if (backpack != null)
        {
            foreach (InventoryItem item in backpack.GetItems())
            {
                if (IsUpgradeableItem(item) && addedItems.Add(item))
                    upgradeableItems.Add(item);
            }
        }

        coinText.text = $"COINS  {(inventory != null ? inventory.GetCoinCount() : 0)}";
        RefreshCharacterLoadout();

        if (upgradeableItems.Count == 0)
        {
            equipmentDetails.text = "You have no weapons or armor available to upgrade.";
            upgradeButton.interactable = false;
            return;
        }

        if (equipmentOptionPrefab != null)
        {
            foreach (InventoryItem item in upgradeableItems)
            {
                MerchantEquipmentOptionUI option = Instantiate(equipmentOptionPrefab, equipmentListParent);
                option.Bind(item, SelectItem, equipment != null && equipment.IsEquipped(item));
                customEquipmentButtons.Add(option);
            }

            return;
        }

        float buttonWidth = 150f;
        float startX = -((upgradeableItems.Count - 1) * (buttonWidth + 12f)) * 0.5f;
        for (int index = 0; index < upgradeableItems.Count; index++)
        {
            InventoryItem item = upgradeableItems[index];
            bool isEquipped = equipment != null && equipment.IsEquipped(item);
            Button equipmentButton = CreateButton(
                equipmentListParent,
                item.Data.itemName,
                string.Empty,
                new Vector2(startX + index * (buttonWidth + 12f), 125f),
                new Vector2(buttonWidth, 116f),
                () => SelectItem(item)
            );
            GameObject buttonObject = equipmentButton.gameObject;

            Image icon = CreateImage("Icon", buttonObject.transform,
                new Vector2(48f, 48f), new Vector2(0f, 20f), Color.white).GetComponent<Image>();
            icon.sprite = item.Data.inventoryIcon != null
                ? item.Data.inventoryIcon
                : item.Data.equipmentIcon;
            icon.preserveAspect = true;
            AddText(buttonObject.transform, "Item Name", item.Data.itemName,
                new Vector2(0f, -28f), new Vector2(buttonWidth - 8f, 25f), 12,
                TextAlignmentOptions.Center, Color.white);
            AddText(buttonObject.transform, "Item Level", $"LVL {item.UpgradeLevel}",
                new Vector2(0f, -48f), new Vector2(buttonWidth - 8f, 20f), 13,
                TextAlignmentOptions.Center, new Color(1f, 0.82f, 0.45f));
            AddText(buttonObject.transform, "Item Location", isEquipped ? "EQUIPPED" : "IN BAG",
                new Vector2(0f, -64f), new Vector2(buttonWidth - 8f, 18f), 10,
                TextAlignmentOptions.Center, new Color(0.78f, 0.81f, 0.86f));
            equipmentButtons.Add(buttonObject);
            equipmentButtonItems.Add(item);
        }

    }

    private static bool IsUpgradeableItem(InventoryItem item)
    {
        if (item == null || item.Data == null ||
            item.UpgradeLevel >= InventoryItem.MaximumUpgradeLevel)
            return false;

        foreach (EquipmentType slot in UpgradeableSlots)
        {
            if (item.Data.EquipmentType == slot)
                return true;
        }

        return false;
    }

    private void BuildPlayerPreview()
    {
        if (playerPreviewRoot == null)
            return;

        previewDirection = CharacterDirection.South;
        previewCharacter = null;
        foreach (Image image in playerPreviewImages)
        {
            if (image != null)
                Destroy(image.gameObject);
        }

        playerPreviewImages.Clear();
        playerPreviewRenderers.Clear();

        CharacterRenderer character = Player.Instance != null
            ? Player.Instance.GetComponentInChildren<CharacterRenderer>(true)
            : null;

        if (character == null)
        {
            if (playerPreviewPlaceholder != null)
                playerPreviewPlaceholder.gameObject.SetActive(true);
            if (playerClassText != null)
                playerClassText.text = "PLAYER PREVIEW UNAVAILABLE";
            return;
        }

        previewCharacter = character;
        playerPreviewRenderers.AddRange(character.GetComponentsInChildren<SpriteRenderer>(true));
        playerPreviewRenderers.Sort((left, right) => left.sortingOrder.CompareTo(right.sortingOrder));

        for (int index = 0; index < playerPreviewRenderers.Count; index++)
        {
            GameObject layerObject = CreateRect(
                $"Character Layer {index}",
                playerPreviewRoot,
                Vector2.zero,
                Vector2.zero
            );
            Image layerImage = layerObject.AddComponent<Image>();
            layerImage.raycastTarget = false;
            playerPreviewImages.Add(layerImage);
        }

        if (playerClassText != null)
            playerClassText.text = $"{character.Appearance.playerClass}  -  CURRENT LOADOUT";
        if (playerPreviewPlaceholder != null)
            playerPreviewPlaceholder.gameObject.SetActive(playerPreviewRenderers.Count == 0);
        UpdatePlayerPreview();
    }

    private void UpdatePlayerPreview()
    {
        if (playerPreviewRoot == null)
            return;

        bool hasBounds = false;
        Bounds characterBounds = new Bounds();
        for (int index = 0; index < playerPreviewRenderers.Count; index++)
        {
            SpriteRenderer source = playerPreviewRenderers[index];
            if (source == null || !source.enabled || !source.gameObject.activeInHierarchy || source.sprite == null)
                continue;

            if (!hasBounds)
            {
                characterBounds = source.bounds;
                hasBounds = true;
            }
            else
            {
                characterBounds.Encapsulate(source.bounds);
            }
        }

        if (!hasBounds)
        {
            if (playerPreviewPlaceholder != null)
                playerPreviewPlaceholder.gameObject.SetActive(true);
            return;
        }

        if (playerPreviewPlaceholder != null)
            playerPreviewPlaceholder.gameObject.SetActive(false);
        float scaleX = characterBounds.size.x > 0f ? 260f / characterBounds.size.x : 1f;
        float scaleY = characterBounds.size.y > 0f ? 300f / characterBounds.size.y : 1f;
        float scale = Mathf.Min(scaleX, scaleY);

        for (int index = 0; index < playerPreviewRenderers.Count; index++)
        {
            SpriteRenderer source = playerPreviewRenderers[index];
            Image previewImage = playerPreviewImages[index];
            Sprite previewSprite = source != null && previewCharacter != null
                ? previewCharacter.GetPreviewSprite(source, previewDirection)
                : null;
            bool visible = source != null &&
                source.gameObject.activeInHierarchy &&
                previewSprite != null;

            previewImage.enabled = visible;
            if (!visible)
                continue;

            Bounds sourceBounds = source.bounds;
            RectTransform previewRect = previewImage.rectTransform;
            previewImage.sprite = previewSprite;
            previewImage.color = source.color;
            previewImage.preserveAspect = false;
            previewRect.sizeDelta = new Vector2(sourceBounds.size.x, sourceBounds.size.y) * scale;
            previewRect.anchoredPosition = new Vector2(
                sourceBounds.center.x - characterBounds.center.x,
                sourceBounds.center.y - characterBounds.center.y
            ) * scale;
            previewRect.localScale = new Vector3(source.flipX ? -1f : 1f, source.flipY ? -1f : 1f, 1f);
        }
    }

    private void BindPreviewDirectionButtons()
    {
        if (playerPreviewDirectionButtons == null)
            return;

        UnityEngine.Events.UnityAction[] actions =
        {
            FacePreviewNorth,
            FacePreviewSouth,
            FacePreviewWest,
            FacePreviewEast
        };

        for (int index = 0; index < Mathf.Min(playerPreviewDirectionButtons.Length, actions.Length); index++)
        {
            Button button = playerPreviewDirectionButtons[index];
            if (button == null)
                continue;

            button.onClick.RemoveListener(actions[index]);
            button.onClick.AddListener(actions[index]);
        }
    }

    private void FacePreviewNorth() => SetPreviewDirection(CharacterDirection.North);
    private void FacePreviewSouth() => SetPreviewDirection(CharacterDirection.South);
    private void FacePreviewWest() => SetPreviewDirection(CharacterDirection.West);
    private void FacePreviewEast() => SetPreviewDirection(CharacterDirection.East);

    private void SetPreviewDirection(CharacterDirection direction)
    {
        previewDirection = direction;
        UpdatePlayerPreview();
    }

    private void RefreshCharacterLoadout()
    {
        EquipmentManager equipment = EquipmentManager.Instance;
        if (equippedSlotIcons == null)
            return;

        for (int index = 0; index < Mathf.Min(UpgradeableSlots.Length, equippedSlotIcons.Length); index++)
        {
            if (equippedSlotIcons[index] == null)
                continue;

            InventoryItem item = equipment != null
                ? equipment.GetEquippedItem(UpgradeableSlots[index])
                : null;
            Sprite sprite = item != null && item.Data != null
                ? (item.Data.inventoryIcon != null ? item.Data.inventoryIcon : item.Data.equipmentIcon)
                : null;

            equippedSlotIcons[index].sprite = sprite;
            equippedSlotIcons[index].enabled = sprite != null;
            equippedSlotIcons[index].color = sprite != null ? Color.white : new Color(0.56f, 0.59f, 0.65f, 0.45f);
        }
    }

    private static string GetSlotLabel(EquipmentType slot)
    {
        return slot switch
        {
            EquipmentType.Weapon => "WEAPON",
            EquipmentType.Helmet => "HELMET",
            EquipmentType.Chestplate => "CHEST",
            EquipmentType.Legging => "LEGS",
            EquipmentType.Shield => "SHIELD",
            _ => slot.ToString().ToUpperInvariant()
        };
    }

    private void SelectItem(InventoryItem item)
    {
        selectedItem = item;
        feedbackText.text = string.Empty;
        foreach (MerchantEquipmentOptionUI button in customEquipmentButtons)
        {
            if (button != null)
                button.SetSelected(button.Item == item);
        }

        for (int index = 0; index < equipmentButtons.Count; index++)
        {
            if (equipmentButtons[index] == null)
                continue;

            Image buttonImage = equipmentButtons[index].GetComponent<Image>();
            buttonImage.color = item != null && equipmentButtonItems[index] == item
                    ? new Color(0.31f, 0.28f, 0.19f, 1f)
                    : new Color(0.12f, 0.15f, 0.2f, 1f);
        }
        UpdateSelectedItemDetails();
    }

    private void UpdateSelectedItemDetails()
    {
        if (selectedItem == null || selectedItem.Data == null)
        {
            equipmentDetails.text = equipmentButtons.Count == 0
                && customEquipmentButtons.Count == 0
                ? "You have no weapons or armor available to upgrade."
                : "Select a weapon or armor item to see its upgrade.";
            upgradeButton.interactable = false;
            SetUpgradeButtonLabel("Upgrade");
            return;
        }

        if (selectedItem.UpgradeLevel >= InventoryItem.MaximumUpgradeLevel)
        {
            equipmentDetails.text =
                $"{selectedItem.Data.itemName} - MAX LEVEL\nUpgrade level: {selectedItem.UpgradeLevel}/{InventoryItem.MaximumUpgradeLevel}";
            SetUpgradeButtonLabel("Max level");
            upgradeButton.interactable = false;
            return;
        }

        equipmentDetails.text = BuildUpgradePreview(selectedItem);
        int cost = GetUpgradeCost(selectedItem);
        SetUpgradeButtonLabel($"Upgrade - {cost} coins");

        PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
        upgradeButton.interactable =
            inventory != null && inventory.GetCoinCount() >= cost;

        if (inventory == null)
            feedbackText.text = "Player inventory is unavailable.";
        else if (!upgradeButton.interactable)
            feedbackText.text = "Not enough coins.";
    }

    private string BuildUpgradePreview(InventoryItem item)
    {
        float currentMultiplier = item.UpgradeMultiplier;
        float nextMultiplier = currentMultiplier + InventoryItem.UpgradeBonusPerLevel;
        StringBuilder preview = new StringBuilder();
        preview.AppendLine($"{item.Data.itemName}  |  Level {item.UpgradeLevel} -> {item.UpgradeLevel + 1}");
        preview.AppendLine("Current stats  ->  After upgrade");

        foreach (EquipmentStat stat in item.Data.StatModifiers)
        {
            preview.AppendLine(
                $"{stat.statType}: {FormatValue(stat.value * currentMultiplier, stat.modifierType)}" +
                $"  ->  {FormatValue(stat.value * nextMultiplier, stat.modifierType)}"
            );
        }

        foreach (WeaponDamage damage in item.Data.WeaponDamages)
        {
            preview.AppendLine(
                $"{damage.damageType} {damage.damageSlot} damage: " +
                $"{FormatValue(damage.value * currentMultiplier, damage.modifierType)}  ->  " +
                $"{FormatValue(damage.value * nextMultiplier, damage.modifierType)}"
            );
        }

        if (item.Data.EquipmentType == EquipmentType.Weapon)
        {
            preview.AppendLine(
                $"Skill damage: {FormatValue(item.Data.SkillDamage * currentMultiplier, StatModifierType.Flat)}" +
                $"  ->  {FormatValue(item.Data.SkillDamage * nextMultiplier, StatModifierType.Flat)}"
            );
        }

        if (item.Data.StatModifiers.Count == 0 &&
            item.Data.WeaponDamages.Count == 0 &&
            item.Data.EquipmentType != EquipmentType.Weapon)
        {
            preview.AppendLine("No direct stats are configured for this equipment.");
        }

        return preview.ToString().TrimEnd();
    }

    private void UpgradeSelectedItem()
    {
        PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
        EquipmentManager equipment = EquipmentManager.Instance;
        bool isEquipped = selectedItem != null &&
            equipment != null &&
            equipment.IsEquipped(selectedItem);
        bool isInBackpack = selectedItem != null &&
            inventory != null &&
            inventory.MainBackPack != null &&
            inventory.MainBackPack.GetItems().Contains(selectedItem);

        if (!IsUpgradeableItem(selectedItem) || (!isEquipped && !isInBackpack))
        {
            selectedItem = null;
            feedbackText.text = "That item is no longer available in your inventory.";
            RefreshEquipmentOptions();
            UpdateSelectedItemDetails();
            return;
        }

        if (inventory == null)
        {
            feedbackText.text = "Player inventory is unavailable.";
            return;
        }

        int cost = GetUpgradeCost(selectedItem);
        if (inventory.GetCoinCount() < cost)
        {
            feedbackText.text = "Not enough coins.";
            UpdateSelectedItemDetails();
            return;
        }

        itemBeingUpgraded = selectedItem;
        pendingUpgradeCost = cost;
        upgradeWorkElapsed = 0f;
        upgradeWorkDuration = Mathf.Max(
            0.1f,
            baseWorkDuration + itemBeingUpgraded.UpgradeLevel * additionalWorkPerUpgradeLevel
        );
        upgradeInProgress = true;
        activeUpgrade = this;

        EnsureUpgradeProgressUI();
        upgradeProgressCanvas.gameObject.SetActive(true);
        upgradeProgressFill.color = new Color(1f, 0.63f, 0.19f, 1f);
        UpdateUpgradeProgressUI();
        CloseUI();
    }

    private void UpdateUpgradeWork()
    {
        upgradeWorkElapsed = Mathf.Min(upgradeWorkElapsed + Time.deltaTime, upgradeWorkDuration);
        UpdateUpgradeProgressUI();

        if (upgradeWorkElapsed >= upgradeWorkDuration)
        {
            bool completed = CompleteUpgradeWork();
            upgradeInProgress = false;
            upgradeResultVisible = true;
            upgradeResultElapsed = 0f;
            upgradeProgressLabel.text = completed ? "UPGRADE COMPLETE" : "UPGRADE CANCELLED";
            upgradeProgressFill.fillAmount = 1f;
            upgradeProgressFill.color = completed
                ? new Color(0.35f, 0.82f, 0.48f, 1f)
                : new Color(0.9f, 0.35f, 0.28f, 1f);
        }
    }

    private bool CompleteUpgradeWork()
    {
        PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
        EquipmentManager equipment = EquipmentManager.Instance;
        bool isEquipped = itemBeingUpgraded != null &&
            equipment != null &&
            equipment.IsEquipped(itemBeingUpgraded);
        InventoryGrid backpack = inventory != null ? inventory.MainBackPack : null;
        bool isInBackpack = itemBeingUpgraded != null &&
            backpack != null &&
            backpack.GetItems().Contains(itemBeingUpgraded);

        if (!IsUpgradeableItem(itemBeingUpgraded) || (!isEquipped && !isInBackpack))
        {
            Debug.LogWarning(
                "EquipmentUpgradeMerchant: Upgrade cancelled because the item is no longer available.",
                this
            );
            return false;
        }

        if (inventory == null || !inventory.TrySpendCoins(pendingUpgradeCost))
        {
            Debug.LogWarning(
                "EquipmentUpgradeMerchant: Upgrade cancelled because the player no longer has enough coins.",
                this
            );
            return false;
        }

        if (!itemBeingUpgraded.TryUpgrade())
        {
            Debug.LogError("EquipmentUpgradeMerchant: The item could not be upgraded when work completed.", this);
            return false;
        }

        if (isEquipped)
            equipment.NotifyItemUpgraded(itemBeingUpgraded);
        else
            backpack.NotifyItemUpdated(itemBeingUpgraded);

        return true;
    }

    private void UpdateUpgradeResult()
    {
        upgradeResultElapsed += Time.deltaTime;
        if (upgradeResultElapsed >= 0.75f)
            CancelUpgradeWork();
    }

    private void EnsureUpgradeProgressUI()
    {
        if (upgradeProgressCanvas != null)
            return;

        GameObject canvasObject = new GameObject(
            "Merchant Upgrade Progress",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler)
        );
        Transform parent = progressBarAnchor != null ? progressBarAnchor : transform;
        canvasObject.transform.SetParent(parent, false);
        canvasObject.transform.localPosition = progressBarAnchor != null
            ? Vector3.zero
            : progressBarOffset;
        canvasObject.transform.localRotation = Quaternion.identity;
        canvasObject.transform.localScale = Vector3.one * 0.01f;

        upgradeProgressCanvas = canvasObject.GetComponent<Canvas>();
        upgradeProgressCanvas.renderMode = RenderMode.WorldSpace;
        upgradeProgressCanvas.sortingOrder = 100;
        upgradeProgressCanvas.worldCamera = Camera.main;
        canvasObject.GetComponent<RectTransform>().sizeDelta = new Vector2(340f, 76f);

        GameObject background = CreateImage(
            "Progress Background",
            canvasObject.transform,
            new Vector2(320f, 68f),
            Vector2.zero,
            new Color(0.055f, 0.065f, 0.085f, 0.94f)
        );
        upgradeProgressLabel = AddText(
            background.transform,
            "Work Label",
            "HAMMERING",
            new Vector2(0f, 18f),
            new Vector2(260f, 28f),
            22,
            TextAlignmentOptions.Center,
            new Color(1f, 0.82f, 0.45f)
        );
        GameObject hammerPivot = CreateRect(
            "Hammer",
            background.transform,
            Vector2.one,
            new Vector2(138f, 16f)
        );
        upgradeHammer = hammerPivot.GetComponent<RectTransform>();
        CreateImage(
            "Handle",
            hammerPivot.transform,
            new Vector2(6f, 28f),
            new Vector2(0f, -8f),
            new Color(0.56f, 0.32f, 0.16f, 1f)
        );
        CreateImage(
            "Head",
            hammerPivot.transform,
            new Vector2(22f, 8f),
            new Vector2(0f, 5f),
            new Color(0.76f, 0.79f, 0.83f, 1f)
        );

        GameObject track = CreateImage(
            "Progress Track",
            background.transform,
            new Vector2(270f, 16f),
            new Vector2(0f, -16f),
            new Color(0.16f, 0.18f, 0.22f, 1f)
        );
        GameObject fillObject = CreateImage(
            "Progress Fill",
            track.transform,
            Vector2.zero,
            Vector2.zero,
            new Color(1f, 0.63f, 0.19f, 1f)
        );
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        upgradeProgressFill = fillObject.GetComponent<Image>();
        upgradeProgressFill.type = Image.Type.Filled;
        upgradeProgressFill.fillMethod = Image.FillMethod.Horizontal;
        upgradeProgressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        upgradeProgressFill.fillAmount = 0f;
        upgradeProgressFill.raycastTarget = false;
        background.GetComponent<Image>().raycastTarget = false;
        track.GetComponent<Image>().raycastTarget = false;
        canvasObject.SetActive(false);
    }

    private void UpdateUpgradeProgressUI()
    {
        if (upgradeProgressFill == null || upgradeProgressLabel == null)
            return;

        upgradeProgressFill.fillAmount = upgradeWorkDuration > 0f
            ? Mathf.Clamp01(upgradeWorkElapsed / upgradeWorkDuration)
            : 1f;
        int dotCount = Mathf.FloorToInt(Time.time * 3f) % 4;
        upgradeProgressLabel.text = $"HAMMERING{new string('.', dotCount)}";
        if (upgradeHammer != null)
            upgradeHammer.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 12f) * 35f);
    }

    private void CancelUpgradeWork()
    {
        if (upgradeProgressCanvas != null)
            upgradeProgressCanvas.gameObject.SetActive(false);

        if (activeUpgrade == this)
            activeUpgrade = null;

        itemBeingUpgraded = null;
        pendingUpgradeCost = 0;
        upgradeWorkElapsed = 0f;
        upgradeWorkDuration = 0f;
        upgradeResultElapsed = 0f;
        upgradeInProgress = false;
        upgradeResultVisible = false;
    }

    private int GetUpgradeCost(InventoryItem item)
    {
        float growth = Mathf.Max(1.01f, costMultiplier);
        int price = Mathf.Max(
            1,
            Mathf.RoundToInt(Mathf.Max(1, baseUpgradeCost) * Mathf.Pow(growth, item.UpgradeLevel))
        );

        if (item.UpgradeLevel > 0)
        {
            int previousPrice = Mathf.Max(
                1,
                Mathf.RoundToInt(Mathf.Max(1, baseUpgradeCost) * Mathf.Pow(growth, item.UpgradeLevel - 1))
            );
            price = Mathf.Max(price, previousPrice + 1);
        }

        return price;
    }

    private static string FormatValue(float value, StatModifierType type)
    {
        return type == StatModifierType.Percent
            ? $"{value:0.##}%"
            : $"{value:0.##}";
    }

    private void LockPlayerControls()
    {
        Player player = Player.Instance;
        if (player == null)
            return;

        lockedMovement = player.GetComponent<PlayerWASD>();
        lockedDash = player.GetComponent<PlayerDash>();

        if (lockedMovement != null && !lockedMovement.IsMovementLocked)
        {
            lockedMovement.LockMovement();
            lockedMovementByMerchant = true;
        }

        if (lockedDash != null && !lockedDash.IsDashLocked)
        {
            lockedDash.LockDash();
            lockedDashByMerchant = true;
        }
    }

    private void CloseUI()
    {
        if (customPanelRoot != null)
            customPanelRoot.SetActive(false);
        else if (uiCanvas != null)
            uiCanvas.gameObject.SetActive(false);

        if (activeMerchant == this)
            activeMerchant = null;

        UnlockPlayerControls();
    }

    private void UnlockPlayerControls()
    {
        if (lockedMovementByMerchant && lockedMovement != null)
            lockedMovement.UnlockMovement();
        if (lockedDashByMerchant && lockedDash != null)
            lockedDash.UnlockDash();

        lockedMovement = null;
        lockedDash = null;
        lockedMovementByMerchant = false;
        lockedDashByMerchant = false;
    }

    private void SetUpgradeButtonLabel(string value)
    {
        if (upgradeButtonLabel != null)
        {
            upgradeButtonLabel.text = value;
            return;
        }

        TextMeshProUGUI buttonText = upgradeButton != null
            ? upgradeButton.GetComponentInChildren<TextMeshProUGUI>()
            : null;
        if (buttonText != null)
            buttonText.text = value;
    }

    private static GameObject CreateRect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return child;
    }

    private static GameObject CreateImage(
        string name,
        Transform parent,
        Vector2 size,
        Vector2 position,
        Color color)
    {
        GameObject imageObject = CreateRect(name, parent, size, position);
        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        return imageObject;
    }

    private static TextMeshProUGUI AddText(
        Transform parent,
        string name,
        string value,
        Vector2 position,
        Vector2 size,
        int fontSize,
        TextAlignmentOptions alignment,
        Color color)
    {
        GameObject textObject = CreateRect(name, parent, size, position);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = GetUIFontAsset();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.text = value;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 position,
        Vector2 size,
        UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = CreateImage(name, parent, size, position,
            new Color(0.31f, 0.24f, 0.16f, 1f));
        Image image = buttonObject.GetComponent<Image>();
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.48f, 0.37f, 0.22f, 1f);
        colors.pressedColor = new Color(0.23f, 0.18f, 0.13f, 1f);
        button.colors = colors;
        if (!string.IsNullOrEmpty(label))
        {
            AddText(buttonObject.transform, "Label", label,
                Vector2.zero, size, 20, TextAlignmentOptions.Center, Color.white);
        }

        return button;
    }

    private static TMP_FontAsset GetUIFontAsset()
    {
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font != null)
            return font;

        font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font == null)
            Debug.LogError("EquipmentUpgradeMerchant: TextMesh Pro default font asset could not be loaded.");

        return font;
    }
}
