using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class EquipmentUpgradeMerchantUIBuilder
{
    private const string MerchantCanvasName = "MerchantUpgradeCanvas";
    private const string MerchantName = "UpgradeMerchant";
    private const string PanelName = "MerchantPanel";
    private const string GeneratedMarkerName = "Merchant UI - Generated";

    [MenuItem("Tools/Sitanite/Build Merchant UI for UpgradeMerchant")]
    private static void BuildMerchantUI()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != "0 (4)")
        {
            EditorUtility.DisplayDialog(
                "Merchant UI Builder",
                "Open Assets/_Recovery/0 (4).unity as the active scene before building the merchant UI.",
                "OK"
            );
            return;
        }

        GameObject merchantObject = GameObject.Find(MerchantName);
        if (merchantObject == null)
        {
            EditorUtility.DisplayDialog(
                "Merchant UI Builder",
                $"Open the target scene and make sure it contains an active '{MerchantName}'.",
                "OK"
            );
            return;
        }

        EquipmentUpgradeMerchant merchant = merchantObject.GetComponent<EquipmentUpgradeMerchant>();
        if (merchant == null)
        {
            EditorUtility.DisplayDialog(
                "Merchant UI Builder",
                $"'{MerchantName}' needs an EquipmentUpgradeMerchant component.",
                "OK"
            );
            return;
        }

        GameObject canvasObject = GameObject.Find(MerchantCanvasName);
        Canvas canvas = canvasObject != null ? canvasObject.GetComponent<Canvas>() : null;
        if (canvasObject != null && canvas == null)
        {
            EditorUtility.DisplayDialog(
                "Merchant UI Builder",
                $"'{MerchantCanvasName}' already exists but does not have a Canvas component.",
                "OK"
            );
            return;
        }

        Transform existingPanel = canvasObject != null
            ? canvasObject.transform.Find(PanelName)
            : null;
        if (existingPanel != null && existingPanel.Find(GeneratedMarkerName) != null)
        {
            Selection.activeGameObject = existingPanel.gameObject;
            EditorUtility.DisplayDialog(
                "Merchant UI Builder",
                "A generated merchant UI already exists under MerchantUpgradeCanvas. Adjust it directly in the Hierarchy and Inspector.",
                "OK"
            );
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build Merchant Upgrade UI");

        if (canvasObject == null)
        {
            canvasObject = CreateMerchantCanvas();
            canvas = canvasObject.GetComponent<Canvas>();
        }

        GameObject panelRoot = existingPanel != null
            ? existingPanel.gameObject
            : CreatePanelRoot(canvasObject.transform);
        GameObject generatedRoot = CreateRect(
            GeneratedMarkerName,
            panelRoot.transform,
            Vector2.zero,
            Vector2.zero,
            new Vector2(1f, 1f),
            Vector2.zero,
            new Vector2(1f, 1f)
        );

        BuildAndBind(merchant, canvas, panelRoot, generatedRoot);

        panelRoot.SetActive(false);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = panelRoot;
        Undo.CollapseUndoOperations(undoGroup);

        EditorUtility.DisplayDialog(
            "Merchant UI Created",
            "MerchantPanel is built and wired to UpgradeMerchant. Save the scene, then adjust the generated controls in the Hierarchy and Inspector.",
            "OK"
        );
    }

    private static GameObject CreateMerchantCanvas()
    {
        GameObject canvasObject = new GameObject(
            MerchantCanvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create Merchant Canvas");

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2560f, 1440f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvasObject;
    }

    private static GameObject CreatePanelRoot(Transform canvas)
    {
        GameObject root = CreateRect(
            PanelName,
            canvas,
            Vector2.zero,
            Vector2.zero,
            Vector2.zero,
            Vector2.one,
            Vector2.zero
        );
        Image backdrop = root.AddComponent<Image>();
        backdrop.color = new Color(0.035f, 0.045f, 0.07f, 0.94f);
        backdrop.raycastTarget = true;
        return root;
    }

    private static void BuildAndBind(
        EquipmentUpgradeMerchant merchant,
        Canvas canvas,
        GameObject panelRoot,
        GameObject generatedRoot)
    {
        TextMeshProUGUI title = CreateText(
            generatedRoot.transform,
            "Merchant Title",
            "MASTER SMITH",
            new Vector2(0f, -44f),
            new Vector2(820f, 48f),
            32,
            new Color(1f, 0.82f, 0.45f),
            TextAlignmentOptions.Center
        );

        GameObject dialogPanel = CreatePanel(
            "DialogPanel",
            generatedRoot.transform,
            new Vector2(600f, 340f),
            new Vector2(0f, -10f),
            new Color(0.12f, 0.15f, 0.2f, 1f)
        );
        CreateText(dialogPanel.transform, "Greeting",
            "Ready to strengthen your gear?",
            new Vector2(0f, 78f), new Vector2(520f, 52f), 24,
            Color.white, TextAlignmentOptions.Center);
        CreateText(dialogPanel.transform, "Greeting Details",
            "Choose an equipped weapon or armor piece to improve.",
            new Vector2(0f, 28f), new Vector2(520f, 48f), 16,
            new Color(0.76f, 0.79f, 0.85f), TextAlignmentOptions.Center);
        Button openUpgradeButton = CreateButton(
            dialogPanel.transform, "Open Upgrade Panel", "View equipment",
            new Vector2(0f, -42f), new Vector2(260f, 52f)
        );
        Button dialogCloseButton = CreateButton(
            dialogPanel.transform, "Dialog Close Button", "Not now",
            new Vector2(0f, -106f), new Vector2(180f, 42f)
        );

        GameObject upgradePanel = CreatePanel(
            "UpgradePanel",
            generatedRoot.transform,
            new Vector2(1400f, 780f),
            new Vector2(0f, -26f),
            new Color(0.09f, 0.11f, 0.16f, 1f)
        );
        CreateText(upgradePanel.transform, "Upgrade Heading",
            "EQUIPMENT UPGRADES",
            new Vector2(0f, 344f), new Vector2(1220f, 42f), 26,
            new Color(1f, 0.82f, 0.45f), TextAlignmentOptions.Center);

        GameObject characterCard = CreatePanel(
            "Character Card",
            upgradePanel.transform,
            new Vector2(360f, 640f),
            new Vector2(-490f, -4f),
            new Color(0.14f, 0.18f, 0.23f, 1f)
        );
        CreateText(characterCard.transform, "Character Heading",
            "YOUR CHARACTER",
            new Vector2(0f, 286f), new Vector2(320f, 30f), 17,
            new Color(1f, 0.82f, 0.45f), TextAlignmentOptions.Center);
        GameObject previewSurface = CreatePanel(
            "Character Preview Surface",
            characterCard.transform,
            new Vector2(310f, 370f),
            new Vector2(0f, 48f),
            new Color(0.08f, 0.36f, 0.38f, 1f)
        );
        TextMeshProUGUI previewPlaceholder = CreateText(
            previewSurface.transform,
            "Preview Placeholder",
            "Character preview appears here",
            Vector2.zero,
            new Vector2(270f, 56f),
            15,
            new Color(0.84f, 0.88f, 0.9f),
            TextAlignmentOptions.Center
        );
        TextMeshProUGUI classText = CreateText(
            characterCard.transform,
            "Player Class",
            "CURRENT LOADOUT",
            new Vector2(0f, -160f),
            new Vector2(320f, 26f),
            14,
            new Color(0.82f, 0.84f, 0.88f),
            TextAlignmentOptions.Center
        );
        CreateText(characterCard.transform, "Equipped Heading",
            "EQUIPPED ITEMS",
            new Vector2(0f, -196f), new Vector2(320f, 24f), 13,
            new Color(1f, 0.82f, 0.45f), TextAlignmentOptions.Center);

        Image[] equippedSlotIcons = new Image[5];
        string[] slotNames = { "WEAPON", "HELMET", "CHEST", "LEGS", "SHIELD" };
        for (int index = 0; index < equippedSlotIcons.Length; index++)
        {
            float x = (index - 2) * 62f;
            GameObject slot = CreatePanel(
                $"{slotNames[index]} Slot",
                characterCard.transform,
                new Vector2(54f, 60f),
                new Vector2(x, -252f),
                new Color(0.07f, 0.09f, 0.13f, 1f)
            );
            equippedSlotIcons[index] = CreateImage(
                "Equipped Item Icon",
                slot.transform,
                new Vector2(38f, 38f),
                new Vector2(0f, 8f),
                new Color(1f, 1f, 1f, 0.48f)
            );
            CreateText(slot.transform, "Slot Label", slotNames[index],
                new Vector2(0f, -22f), new Vector2(52f, 16f), 8,
                new Color(0.83f, 0.85f, 0.89f), TextAlignmentOptions.Center);
        }

        GameObject detailsCard = CreatePanel(
            "Upgrade Details Card",
            upgradePanel.transform,
            new Vector2(950f, 640f),
            new Vector2(205f, -4f),
            new Color(0.14f, 0.18f, 0.23f, 1f)
        );
        CreateText(detailsCard.transform, "Gear Instruction",
            "Select an equipped item to preview its next upgrade",
            new Vector2(0f, 282f), new Vector2(890f, 30f), 17,
            Color.white, TextAlignmentOptions.Center);
        TextMeshProUGUI coinText = CreateText(
            detailsCard.transform,
            "Coin Count",
            "COINS  0",
            new Vector2(0f, 244f),
            new Vector2(880f, 30f),
            18,
            new Color(1f, 0.82f, 0.45f),
            TextAlignmentOptions.Center
        );
        CreateImage(
            "Details Divider",
            detailsCard.transform,
            new Vector2(870f, 2f),
            new Vector2(0f, 215f),
            new Color(0.38f, 0.43f, 0.5f, 0.75f)
        );
        GameObject equipmentList = CreateRect(
            "Equipment Options",
            detailsCard.transform,
            new Vector2(860f, 150f),
            new Vector2(0f, 122f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f)
        );
        HorizontalLayoutGroup equipmentLayout = equipmentList.AddComponent<HorizontalLayoutGroup>();
        equipmentLayout.spacing = 12f;
        equipmentLayout.childAlignment = TextAnchor.MiddleCenter;
        equipmentLayout.childControlWidth = false;
        equipmentLayout.childControlHeight = false;
        equipmentLayout.childForceExpandWidth = false;
        equipmentLayout.childForceExpandHeight = false;

        TextMeshProUGUI equipmentDetails = CreateText(
            detailsCard.transform,
            "Upgrade Preview",
            "Select equipped gear to see its upgrade.",
            new Vector2(0f, -72f),
            new Vector2(850f, 190f),
            15,
            Color.white,
            TextAlignmentOptions.TopLeft
        );
        TextMeshProUGUI feedbackText = CreateText(
            detailsCard.transform,
            "Upgrade Feedback",
            string.Empty,
            new Vector2(0f, -198f),
            new Vector2(850f, 26f),
            14,
            new Color(1f, 0.78f, 0.42f),
            TextAlignmentOptions.Center
        );
        Button upgradeButton = CreateButton(
            detailsCard.transform, "Upgrade Button", "Upgrade",
            new Vector2(170f, -254f), new Vector2(240f, 52f)
        );
        TextMeshProUGUI upgradeButtonLabel = upgradeButton.GetComponentInChildren<TextMeshProUGUI>();
        Button upgradeCloseButton = CreateButton(
            detailsCard.transform, "Upgrade Close Button", "Close",
            new Vector2(-170f, -254f), new Vector2(200f, 52f)
        );

        GameObject templatesRoot = CreateRect(
            "Merchant UI Templates",
            canvas.transform,
            Vector2.zero,
            Vector2.zero,
            Vector2.zero,
            Vector2.one,
            Vector2.zero
        );
        GameObject optionTemplate = CreatePanel(
            "Equipment Option Template",
            templatesRoot.transform,
            new Vector2(150f, 116f),
            Vector2.zero,
            new Color(0.12f, 0.15f, 0.2f, 1f)
        );
        Button optionButton = optionTemplate.AddComponent<Button>();
        optionButton.targetGraphic = optionTemplate.GetComponent<Image>();
        Image optionIcon = CreateImage(
            "Item Icon",
            optionTemplate.transform,
            new Vector2(48f, 48f),
            new Vector2(0f, 20f),
            Color.white
        );
        TextMeshProUGUI optionName = CreateText(
            optionTemplate.transform,
            "Item Name",
            "Item",
            new Vector2(0f, -28f),
            new Vector2(140f, 25f),
            12,
            Color.white,
            TextAlignmentOptions.Center
        );
        TextMeshProUGUI optionLevel = CreateText(
            optionTemplate.transform,
            "Upgrade Level",
            "LVL 0",
            new Vector2(0f, -48f),
            new Vector2(140f, 20f),
            13,
            new Color(1f, 0.82f, 0.45f),
            TextAlignmentOptions.Center
        );
        MerchantEquipmentOptionUI optionUI = optionTemplate.AddComponent<MerchantEquipmentOptionUI>();
        SerializedObject serializedOption = new SerializedObject(optionUI);
        SetReference(serializedOption, "button", optionButton);
        SetReference(serializedOption, "itemIcon", optionIcon);
        SetReference(serializedOption, "itemNameText", optionName);
        SetReference(serializedOption, "upgradeLevelText", optionLevel);
        SetReference(serializedOption, "selectionGraphic", optionTemplate.GetComponent<Image>());
        serializedOption.ApplyModifiedProperties();
        templatesRoot.SetActive(false);

        SerializedObject serializedMerchant = new SerializedObject(merchant);
        serializedMerchant.Update();
        SetReference(serializedMerchant, "uiCanvas", canvas);
        SetReference(serializedMerchant, "customPanelRoot", panelRoot);
        SetReference(serializedMerchant, "dialogPanel", dialogPanel);
        SetReference(serializedMerchant, "upgradePanel", upgradePanel);
        SetReference(serializedMerchant, "openUpgradePanelButton", openUpgradeButton);
        SetReference(serializedMerchant, "equipmentListParent", equipmentList.transform);
        SetReference(serializedMerchant, "coinText", coinText);
        SetReference(serializedMerchant, "equipmentDetails", equipmentDetails);
        SetReference(serializedMerchant, "feedbackText", feedbackText);
        SetReference(serializedMerchant, "playerClassText", classText);
        SetReference(serializedMerchant, "playerPreviewPlaceholder", previewPlaceholder);
        SetReference(serializedMerchant, "playerPreviewRoot", previewSurface.GetComponent<RectTransform>());
        SetReference(serializedMerchant, "upgradeButton", upgradeButton);
        SetReference(serializedMerchant, "upgradeButtonLabel", upgradeButtonLabel);
        SetReference(serializedMerchant, "equipmentOptionPrefab", optionUI);
        SerializedProperty closeButtons = serializedMerchant.FindProperty("closePanelButtons");
        closeButtons.arraySize = 2;
        closeButtons.GetArrayElementAtIndex(0).objectReferenceValue = dialogCloseButton;
        closeButtons.GetArrayElementAtIndex(1).objectReferenceValue = upgradeCloseButton;
        SerializedProperty slotIcons = serializedMerchant.FindProperty("equippedSlotIcons");
        slotIcons.arraySize = equippedSlotIcons.Length;
        for (int index = 0; index < equippedSlotIcons.Length; index++)
            slotIcons.GetArrayElementAtIndex(index).objectReferenceValue = equippedSlotIcons[index];
        serializedMerchant.ApplyModifiedProperties();
        EditorUtility.SetDirty(merchant);
        EditorUtility.SetDirty(title);

        dialogPanel.SetActive(true);
        upgradePanel.SetActive(false);
    }

    private static GameObject CreatePanel(
        string name,
        Transform parent,
        Vector2 size,
        Vector2 position,
        Color color)
    {
        GameObject panel = CreateRect(
            name, parent, size, position,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)
        );
        Image image = panel.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return panel;
    }

    private static Image CreateImage(
        string name,
        Transform parent,
        Vector2 size,
        Vector2 position,
        Color color)
    {
        GameObject imageObject = CreateRect(
            name, parent, size, position,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)
        );
        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI CreateText(
        Transform parent,
        string name,
        string value,
        Vector2 position,
        Vector2 size,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateRect(
            name, parent, size, position,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)
        );
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        if (text.font == null)
            text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        text.fontSize = fontSize;
        text.color = color;
        text.text = value;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 position,
        Vector2 size)
    {
        GameObject buttonObject = CreatePanel(
            name, parent, size, position, new Color(0.31f, 0.24f, 0.16f, 1f)
        );
        Image background = buttonObject.GetComponent<Image>();
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = background;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.31f, 0.24f, 0.16f, 1f);
        colors.highlightedColor = new Color(0.48f, 0.37f, 0.22f, 1f);
        colors.pressedColor = new Color(0.23f, 0.18f, 0.13f, 1f);
        button.colors = colors;
        CreateText(
            buttonObject.transform,
            "Label",
            label,
            Vector2.zero,
            size - new Vector2(12f, 8f),
            17,
            Color.white,
            TextAlignmentOptions.Center
        );
        return button;
    }

    private static GameObject CreateRect(
        string name,
        Transform parent,
        Vector2 size,
        Vector2 position,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(child, $"Create {name}");
        child.transform.SetParent(parent, false);
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return child;
    }

    private static void SetReference(SerializedObject target, string propertyName, Object value)
    {
        SerializedProperty property = target.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"Merchant UI Builder could not find serialized field '{propertyName}'.");
            return;
        }

        property.objectReferenceValue = value;
    }
}
