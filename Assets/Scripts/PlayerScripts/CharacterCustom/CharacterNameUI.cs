using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CharacterNameUI : MonoBehaviour
{
    [Header("Name Entry")]
    [SerializeField] private GameObject nameEntryWindow;
    [SerializeField] private TMP_InputField nameInput;

    [Header("Confirmation")]
    [SerializeField] private GameObject confirmationWindow;
    [SerializeField] private CharacterConfirmationUI confirmationUI;

    [Header("Scene Transition")]
    [SerializeField] private SceneTransitionManager sceneTransitionManager;
    [SerializeField] private string nextSceneName;

    [Header("Tutorial Choice")]
    [SerializeField] private GameObject tutorialChoicePanel;

    private PlayerStats playerStats;
    private bool characterConfirmed;

    private void Start()
    {
        FindPlayerStats();

        if (confirmationWindow != null)
        {
            confirmationWindow.SetActive(false);
        }

        if (tutorialChoicePanel != null)
        {
            tutorialChoicePanel.SetActive(false);
        }

        // DO NOT enable the name entry window here.
        // The CharacterCreationPageButton will open it.
    }

    private void FindPlayerStats()
    {
        if (Player.Instance == null)
        {
            Debug.LogError(
                "CharacterNameUI: Player.Instance is NULL!",
                this
            );

            return;
        }

        playerStats =
            Player.Instance.GetComponent<PlayerStats>();

        if (playerStats == null)
        {
            Debug.LogError(
                "CharacterNameUI: PlayerStats not found on Player!",
                Player.Instance
            );
        }
    }

    public void EnterName()
    {
        if (nameInput == null)
            return;

        string enteredName =
            nameInput.text.Trim();

        if (string.IsNullOrEmpty(enteredName))
        {
            Debug.LogWarning(
                "CharacterNameUI: Character name cannot be empty."
            );

            return;
        }

        // Prepare the confirmation screen.
        if (confirmationUI != null)
        {
            confirmationUI.SetPreviewName(enteredName);
            confirmationUI.Refresh();
        }

        // Hide name entry.
        if (nameEntryWindow != null)
        {
            nameEntryWindow.SetActive(false);
        }

        // Show confirmation.
        if (confirmationWindow != null)
        {
            confirmationWindow.SetActive(true);
        }
    }

    public void CancelNameConfirmation()
    {
        // Hide confirmation.
        if (confirmationWindow != null)
        {
            confirmationWindow.SetActive(false);
        }

        // Bring the name entry back.
        if (nameEntryWindow != null)
        {
            nameEntryWindow.SetActive(true);
        }
    }

    public void ConfirmName()
    {
        if (playerStats == null)
        {
            FindPlayerStats();
        }

        if (playerStats == null)
            return;

        if (nameInput == null)
            return;

        string enteredName =
            nameInput.text.Trim();

        if (string.IsNullOrEmpty(enteredName))
            return;

        // Save name.
        playerStats.SetCharacterName(enteredName);

        // Save gender and class.
        CharacterCustomizationController customizationController =
            Player.Instance.GetComponent<CharacterCustomizationController>();

        if (customizationController != null)
        {
            CharacterAppearance appearance =
                customizationController.GetAppearance();

            playerStats.SetCharacterGender(
                appearance.gender
            );

            playerStats.SetPlayerClass(
                appearance.playerClass
            );
        }

        Debug.Log(
            $"Character created: " +
            $"{playerStats.CharacterName} | " +
            $"{playerStats.Gender} | " +
            $"{playerStats.PlayerClass}"
        );

        if (confirmationWindow != null)
        {
            confirmationWindow.SetActive(false);
        }

        characterConfirmed = true;
        ShowTutorialChoice();
    }

    public void StartTutorial()
    {
        StartDungeonAtFloor(FloorManager.TutorialFloor);
    }

    public void SkipTutorial()
    {
        StartDungeonAtFloor(1);
    }

    private void StartDungeonAtFloor(int floor)
    {
        if (!characterConfirmed || playerStats == null)
            return;

        playerStats.RequestStartingFloor(floor);

        if (tutorialChoicePanel != null)
            tutorialChoicePanel.SetActive(false);

        if (sceneTransitionManager != null)
        {
            sceneTransitionManager.TransitionToScene(
                nextSceneName
            );
        }
        else
        {
            Debug.LogError(
                "CharacterNameUI: SceneTransitionManager is not assigned.",
                this
            );
        }
    }

    private void ShowTutorialChoice()
    {
        if (tutorialChoicePanel == null)
        {
            Debug.LogError(
                "CharacterNameUI: Tutorial Choice Panel is not assigned.",
                this
            );
            return;
        }

        tutorialChoicePanel.SetActive(true);
    }

    private GameObject CreateTutorialChoicePanel()
    {
        RectTransform parent = transform.parent as RectTransform;
        if (parent == null)
        {
            Debug.LogError(
                "CharacterNameUI requires a RectTransform parent to create " +
                "the tutorial choice panel.",
                this
            );
            return null;
        }

        GameObject panel = new GameObject(
            "TutorialChoicePanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        panel.transform.SetParent(parent, false);

        RectTransform panelTransform = panel.GetComponent<RectTransform>();
        panelTransform.anchorMin = new Vector2(0.5f, 0.5f);
        panelTransform.anchorMax = new Vector2(0.5f, 0.5f);
        panelTransform.anchoredPosition = Vector2.zero;
        panelTransform.sizeDelta = new Vector2(640f, 260f);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.05f, 0.05f, 0.08f, 0.96f);

        CreateChoiceText(
            panel.transform,
            "Would you like to play the tutorial?",
            new Vector2(0f, 70f),
            new Vector2(560f, 70f),
            32
        );

        CreateChoiceButton(
            panel.transform,
            "TutorialCheckButton",
            "✓",
            new Vector2(-130f, -55f),
            StartTutorial
        );
        CreateChoiceButton(
            panel.transform,
            "TutorialSkipButton",
            "X",
            new Vector2(130f, -55f),
            SkipTutorial
        );

        return panel;
    }

    private void CreateChoiceButton(
        Transform parent,
        string objectName,
        string label,
        Vector2 position,
        UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonTransform =
            buttonObject.GetComponent<RectTransform>();
        buttonTransform.anchorMin = new Vector2(0.5f, 0.5f);
        buttonTransform.anchorMax = new Vector2(0.5f, 0.5f);
        buttonTransform.anchoredPosition = position;
        buttonTransform.sizeDelta = new Vector2(180f, 80f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.28f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(action);

        CreateChoiceText(
            buttonObject.transform,
            label,
            Vector2.zero,
            new Vector2(160f, 60f),
            42
        );
    }

    private void CreateChoiceText(
        Transform parent,
        string textValue,
        Vector2 position,
        Vector2 size,
        float fontSize)
    {
        GameObject textObject = new GameObject(
            "Text",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
        textObject.transform.SetParent(parent, false);

        RectTransform textTransform = textObject.GetComponent<RectTransform>();
        textTransform.anchorMin = new Vector2(0.5f, 0.5f);
        textTransform.anchorMax = new Vector2(0.5f, 0.5f);
        textTransform.anchoredPosition = position;
        textTransform.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = textValue;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
    }
}