using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    [Header("Tutorial Configuration")]
    [Tooltip("List of tutorial steps in order.")]
    public List<TutorialStep> steps = new List<TutorialStep>();
    
    [Header("UI References")]
    [Tooltip("Text element to display the tutorial prompts.")]
    public TextMeshProUGUI promptTextUI;
    [Tooltip("Optional panel to hide/show the tutorial UI.")]
    public GameObject tutorialPanel;

    [Header("Floor")]
    [SerializeField] private FloorManager floorManager;
    [SerializeField] private RoomManager roomManager;

    [Header("Tutorial Progression")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerAttributesNTraits playerAttributes;
    [SerializeField] private PlayerStatsUI playerStatsUI;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private ItemData tutorialTrainingShield;
    [Min(0)] [SerializeField] private int fightRewardAttributePoints = 1;
    [Min(0)] [SerializeField] private int fightRewardTraitPoints = 3;

    [Header("Fading Transitions")]
    [Tooltip("CanvasGroup on the main Tutorial Panel (fades in at start, out at end).")]
    public CanvasGroup panelCanvasGroup;
    [Tooltip("How long the panel fade in and out takes (in seconds).")]
    public float panelFadeDuration = 0.5f;
    [Tooltip("How long the text fade in and out takes between steps (in seconds).")]
    public float textFadeDuration = 0.3f;

    private int currentStepIndex = 0;
    private bool isTutorialActive = false;
    private bool isTransitioning = false;
    private bool isCurrentStepActivated;
    private Coroutine transitionCoroutine;
    private Coroutine promptLifetimeCoroutine;
    private PlayerShieldDefense playerShieldDefense;
    private RectTransform defenseTimingRoot;
    private RectTransform defenseTimingCanvasRect;
    private RectTransform defenseTimingTrack;
    private RectTransform defenseTimingFill;
    private RectTransform defenseTimingMarker;
    private Image defenseTimingFillImage;
    private TextMeshProUGUI defenseTimingText;
    private Transform defenseTimingPlayer;
    private Camera defenseTimingCamera;
    private Canvas defenseTimingCanvas;
    private float parryWindowDuration;
    private float parryWindowEndsAt;
    private float defenseTimingFeedbackUntil;
    private string defenseTimingFeedback;
    private bool hasParryWindowTimer;
    private bool guidedParryPracticeComplete;

    private const float DefenseTimingTrackWidth = 340f;
    private const float GuidedParryPracticeWindow = 1.5f;
    private static readonly Color ParryTimingColor =
        new Color(0.25f, 0.85f, 0.55f);
    private static readonly Color CounterTimingColor =
        new Color(1f, 0.72f, 0.2f);
    
    // Tracks which keys have been successfully pressed in the current step
    private HashSet<KeyCode> keysPressedForCurrentStep = new HashSet<KeyCode>();

    public int CurrentStepIndex => currentStepIndex;
    public bool IsCurrentStepActive =>
        isTutorialActive &&
        isCurrentStepActivated &&
        !isTransitioning;

    private void Start()
    {
        if (!IsOnTutorialFloor())
        {
            HideTutorialPanel();
            return;
        }

        if (steps != null && steps.Count > 0)
        {
            StartTutorial();
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromShieldDefense();
        SetTutorialDefenseStaminaFree(false);
        SetTutorialParryWindow(0f);
        if (defenseTimingRoot != null)
            defenseTimingRoot.gameObject.SetActive(false);
    }

    public void StartTutorial()
    {
        if (!IsOnTutorialFloor())
        {
            HideTutorialPanel();
            return;
        }

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(StartTutorialRoutine());
    }

    private IEnumerator StartTutorialRoutine()
    {
        yield return null;

        if (!IsOnTutorialFloor())
            yield break;

        SetTutorialRoomGatewayLocked(1, true);
        if (HasShieldDefenseSteps())
        {
            EnsureDefenseTimingUI();
            EquipTutorialShieldIfNeeded();
            SubscribeToShieldDefense();
        }
        ResetTutorialPlayerProgression();

        currentStepIndex = 0;
        isTutorialActive = true;
        isTransitioning = true;
        
        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);

        // Reset alphas to 0 and ensure the panel doesn't block other UI clicks
        if (panelCanvasGroup != null) 
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
        }
        if (promptTextUI != null) 
        {
            Color c = promptTextUI.color;
            c.a = 0f;
            promptTextUI.color = c;
        }
            
        // Fade in the main panel first
        if (panelCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(panelCanvasGroup, 0f, 1f, panelFadeDuration));
        }

        isTransitioning = false;
        BeginCurrentStep();
    }

    private void Update()
    {
        if (!IsOnTutorialFloor())
        {
            if (isTutorialActive)
                EndTutorial();

            return;
        }

        UpdateDefenseTimingUI();

        if (
            !isTutorialActive ||
            steps == null ||
            currentStepIndex >= steps.Count ||
            isTransitioning
        )
            return;

        TutorialStep currentStep = steps[currentStepIndex];
        if (!isCurrentStepActivated && currentStep.waitForActivation)
        {
            if (currentStep.activationRoomNumber > 0)
            {
                if (roomManager == null)
                    roomManager = FindFirstObjectByType<RoomManager>();

                if (
                    roomManager != null &&
                    roomManager.CurrentPlayerRoom != null &&
                    roomManager.CurrentPlayerRoom.RoomNumber ==
                        currentStep.activationRoomNumber
                )
                {
                    ActivateCurrentStep(currentStepIndex);
                }
            }

            if (!isCurrentStepActivated)
                return;
        }

        if (TryCompletePanelRequirement(currentStep))
            return;

        if (
            currentStep.completionRequirement !=
                TutorialCompletionRequirement.RequiredKeys ||
            currentStep.requiresExternalCompletion ||
            currentStep.requiredKeys == null ||
            currentStep.requiredKeys.Count == 0
        )
            return;

        // Monitor player input for all of the current step's required keys
        foreach (KeyCode key in currentStep.requiredKeys)
        {
            // If the key hasn't been pressed yet during this step, check for it
            if (!keysPressedForCurrentStep.Contains(key) && Input.GetKeyDown(key))
            {
                keysPressedForCurrentStep.Add(key);
            }
        }

        // Check if all required keys have been successfully pressed at least once
        if (keysPressedForCurrentStep.Count >= currentStep.requiredKeys.Count)
        {
            CompleteCurrentStep();
        }
    }

    private void EnsureDefenseTimingUI()
    {
        if (defenseTimingRoot != null)
            return;

        if (tutorialPanel == null || promptTextUI == null)
        {
            Debug.LogError(
                "TutorialManager requires a TutorialPanel and prompt text " +
                "to display the defense timing guide.",
                this
            );
            return;
        }

        defenseTimingCanvas = tutorialPanel.GetComponentInParent<Canvas>();
        if (defenseTimingCanvas == null)
        {
            Debug.LogError(
                "TutorialManager requires a Canvas containing the TutorialPanel " +
                "to display the defense timing guide.",
                this
            );
            return;
        }

        defenseTimingCanvas = defenseTimingCanvas.rootCanvas;
        defenseTimingCanvasRect =
            defenseTimingCanvas.transform as RectTransform;
        defenseTimingCamera = Camera.main;
        if (defenseTimingCanvasRect == null || defenseTimingCamera == null)
        {
            Debug.LogError(
                "TutorialManager requires a screen-space Canvas and Main Camera " +
                "to position the defense timing guide above the player.",
                this
            );
            return;
        }

        if (Player.Instance == null)
        {
            Debug.LogError(
                "TutorialManager requires an active Player to display the " +
                "defense timing guide.",
                this
            );
            return;
        }
        defenseTimingPlayer = Player.Instance.transform;

        GameObject rootObject = new GameObject(
            "Defense Timing Guide",
            typeof(RectTransform)
        );
        rootObject.layer = defenseTimingCanvas.gameObject.layer;
        defenseTimingRoot = rootObject.GetComponent<RectTransform>();
        defenseTimingRoot.SetParent(defenseTimingCanvas.transform, false);
        defenseTimingRoot.anchorMin = new Vector2(0.5f, 0.5f);
        defenseTimingRoot.anchorMax = new Vector2(0.5f, 0.5f);
        defenseTimingRoot.pivot = new Vector2(0.5f, 0.5f);
        defenseTimingRoot.sizeDelta = new Vector2(370f, 66f);
        defenseTimingRoot.SetAsLastSibling();
        defenseTimingRoot.gameObject.SetActive(false);

        defenseTimingText = CreateDefenseTimingText(
            "Defense Timing Label",
            defenseTimingRoot
        );

        defenseTimingTrack = CreateDefenseTimingImage(
            "Defense Timing Track",
            defenseTimingRoot,
            new Vector2(DefenseTimingTrackWidth, 16f),
            new Vector2(0f, -14f),
            new Color(0.08f, 0.1f, 0.12f, 0.95f)
        );

        Image fillImage = CreateDefenseTimingImage(
            "Defense Timing Fill",
            defenseTimingTrack,
            Vector2.zero,
            Vector2.zero,
            ParryTimingColor
        ).GetComponent<Image>();
        defenseTimingFill = fillImage.rectTransform;
        defenseTimingFillImage = fillImage;
        defenseTimingFill.anchorMin = Vector2.zero;
        defenseTimingFill.anchorMax = new Vector2(0f, 1f);
        defenseTimingFill.offsetMin = Vector2.zero;
        defenseTimingFill.offsetMax = Vector2.zero;

        defenseTimingMarker = CreateDefenseTimingImage(
            "Defense Timing Marker",
            defenseTimingTrack,
            new Vector2(5f, 24f),
            Vector2.zero,
            Color.white
        );
        defenseTimingMarker.anchorMin = new Vector2(0f, 0.5f);
        defenseTimingMarker.anchorMax = new Vector2(0f, 0.5f);
    }

    private TextMeshProUGUI CreateDefenseTimingText(
        string objectName,
        RectTransform parent)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
        textObject.layer = parent.gameObject.layer;
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(parent, false);
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(0f, 19f);
        textRect.sizeDelta = new Vector2(370f, 26f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = promptTextUI != null ? promptTextUI.font : null;
        text.fontSize = 14f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        text.text = string.Empty;
        return text;
    }

    private RectTransform CreateDefenseTimingImage(
        string objectName,
        RectTransform parent,
        Vector2 size,
        Vector2 position,
        Color color)
    {
        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        imageObject.layer = parent.gameObject.layer;
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = imageObject.GetComponent<Image>();
        image.type = Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private void UpdateDefenseTimingUI()
    {
        if (defenseTimingRoot == null)
            return;

        if (
            !isTutorialActive ||
            steps == null ||
            currentStepIndex >= steps.Count
        )
        {
            defenseTimingRoot.gameObject.SetActive(false);
            return;
        }

        TutorialCompletionRequirement requirement =
            steps[currentStepIndex].completionRequirement;
        bool isParryStep =
            requirement == TutorialCompletionRequirement.SuccessfulShieldParry;
        bool isCounterStep =
            requirement == TutorialCompletionRequirement.ShieldCounterAttack;

        defenseTimingRoot.gameObject.SetActive(isParryStep || isCounterStep);
        if (!isParryStep && !isCounterStep)
            return;

        if (!UpdateDefenseTimingPosition())
        {
            defenseTimingRoot.gameObject.SetActive(false);
            return;
        }

        if (isParryStep)
        {
            if (!hasParryWindowTimer)
            {
                bool showingFeedback =
                    Time.time < defenseTimingFeedbackUntil &&
                    !string.IsNullOrEmpty(defenseTimingFeedback);
                SetDefenseTimingMessage(
                    showingFeedback
                        ? defenseTimingFeedback
                        : guidedParryPracticeComplete
                            ? "REAL PARRY TRIAL — guard just before impact."
                            : "PARRY PRACTICE — raise guard as the goblin swings.",
                    showingFeedback ? ParryTimingColor : Color.white
                );
                SetDefenseTimingProgress(0f, ParryTimingColor);
                return;
            }

            float remaining = Mathf.Max(0f, parryWindowEndsAt - Time.time);
            float progress = parryWindowDuration > 0f
                ? remaining / parryWindowDuration
                : 0f;
            string message = remaining > 0f
                ? $"PARRY WINDOW — {remaining:0.00}s"
                : "Window passed — release and raise your guard again.";
            Color messageColor = remaining > 0f
                ? ParryTimingColor
                : Color.white;
            if (Time.time < defenseTimingFeedbackUntil)
            {
                message = defenseTimingFeedback;
                messageColor = Color.white;
            }

            SetDefenseTimingMessage(message, messageColor);
            SetDefenseTimingProgress(progress, ParryTimingColor);
            return;
        }

        if (
            playerShieldDefense == null ||
            !playerShieldDefense.HasCounterOpportunity
        )
        {
            SetDefenseTimingMessage(
                "COUNTER WINDOW CLOSED — parry again to reopen it.",
                Color.white
            );
            SetDefenseTimingProgress(0f, CounterTimingColor);
            return;
        }

        float counterRemaining =
            playerShieldDefense.CounterOpportunityRemaining;
        float counterProgress =
            playerShieldDefense.CounterOpportunityDuration > 0f
                ? counterRemaining /
                    playerShieldDefense.CounterOpportunityDuration
                : 0f;
        SetDefenseTimingMessage(
            $"COUNTER NOW — Left Mouse Button ({counterRemaining:0.00}s)",
            CounterTimingColor
        );
        SetDefenseTimingProgress(counterProgress, CounterTimingColor);
    }

    private void SetDefenseTimingMessage(string message, Color color)
    {
        if (defenseTimingText == null)
            return;

        defenseTimingText.text = message;
        defenseTimingText.color = color;
    }

    private void SetDefenseTimingFeedback(string message)
    {
        defenseTimingFeedback = message;
        defenseTimingFeedbackUntil = Time.time + 1.5f;
    }

    private void SetDefenseTimingProgress(float progress, Color color)
    {
        if (
            defenseTimingFill == null ||
            defenseTimingFillImage == null ||
            defenseTimingMarker == null ||
            defenseTimingTrack == null
        )
        {
            return;
        }

        float normalizedProgress = Mathf.Clamp01(progress);
        defenseTimingFill.anchorMax =
            new Vector2(normalizedProgress, 1f);
        defenseTimingFill.offsetMin = Vector2.zero;
        defenseTimingFill.offsetMax = Vector2.zero;
        defenseTimingFillImage.color = color;
        defenseTimingMarker.anchoredPosition = new Vector2(
            DefenseTimingTrackWidth * normalizedProgress,
            0f
        );
    }

    private bool UpdateDefenseTimingPosition()
    {
        if (
            defenseTimingPlayer == null ||
            defenseTimingCamera == null ||
            defenseTimingCanvas == null ||
            defenseTimingCanvasRect == null
        )
        {
            return false;
        }

        Vector3 screenPosition = defenseTimingCamera.WorldToScreenPoint(
            defenseTimingPlayer.position + Vector3.up * 1.4f
        );
        if (screenPosition.z <= 0f)
            return false;

        Camera uiCamera =
            defenseTimingCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : defenseTimingCanvas.worldCamera;
        if (
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                defenseTimingCanvasRect,
                screenPosition,
                uiCamera,
                out Vector2 canvasPosition
            )
        )
        {
            return false;
        }

        defenseTimingRoot.anchoredPosition = canvasPosition;
        return true;
    }

    private void SetTutorialDefenseStaminaFree(bool free)
    {
        if (playerShieldDefense == null && Player.Instance != null)
        {
            playerShieldDefense =
                Player.Instance.GetComponent<PlayerShieldDefense>();
        }

        if (playerShieldDefense != null)
            playerShieldDefense.TutorialDefenseIsStaminaFree = free;
    }

    private void SetTutorialParryWindow(float duration)
    {
        if (playerShieldDefense == null && Player.Instance != null)
        {
            playerShieldDefense =
                Player.Instance.GetComponent<PlayerShieldDefense>();
        }

        if (playerShieldDefense != null)
        {
            playerShieldDefense.TutorialParryWindowOverride =
                Mathf.Max(0f, duration);
        }
    }

    private void ShowCurrentStep()
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(TransitionToStepRoutine(steps[currentStepIndex]));
    }

    private string GetCurrentStepPrompt(TutorialStep step)
    {
        if (
            step.completionRequirement !=
                TutorialCompletionRequirement.SuccessfulShieldParry
        )
        {
            return step.promptText;
        }

        return guidedParryPracticeComplete
            ? "Practice parry successful! Release and re-raise your guard just " +
                "before the goblin hits. The real window is 0.2 seconds."
            : "Practice first: raise your guard as the goblin attacks. Your " +
                "first parry has a forgiving 1.5-second window, then you'll try " +
                "the real timing.";
    }

    private void BeginCurrentStep()
    {
        TutorialStep currentStep = steps[currentStepIndex];
        isCurrentStepActivated = !currentStep.waitForActivation;
        SetTutorialDefenseStaminaFree(
            isCurrentStepActivated &&
            (
                currentStep.completionRequirement ==
                    TutorialCompletionRequirement.SuccessfulShieldBlock ||
                currentStep.completionRequirement ==
                    TutorialCompletionRequirement.SuccessfulShieldParry
            )
        );

        if (
            currentStep.completionRequirement ==
                TutorialCompletionRequirement.SuccessfulShieldParry
        )
        {
            guidedParryPracticeComplete = false;
            hasParryWindowTimer = false;
            defenseTimingFeedback = null;
            SetTutorialParryWindow(
                    isCurrentStepActivated
                        ? GuidedParryPracticeWindow
                        : 0f
            );
        }
        else
        {
            SetTutorialParryWindow(0f);
        }

        if (isCurrentStepActivated)
            ShowCurrentStep();
        else
            HideCurrentPrompt();
    }

    public void ActivateCurrentStep(int stepIndex)
    {
        if (
            !isTutorialActive ||
            isTransitioning ||
            isCurrentStepActivated ||
            stepIndex != currentStepIndex ||
            steps == null ||
            currentStepIndex >= steps.Count ||
            !steps[currentStepIndex].waitForActivation
        )
        {
            return;
        }

        isCurrentStepActivated = true;
        SetTutorialDefenseStaminaFree(
            steps[currentStepIndex].completionRequirement ==
                TutorialCompletionRequirement.SuccessfulShieldBlock ||
            steps[currentStepIndex].completionRequirement ==
                TutorialCompletionRequirement.SuccessfulShieldParry
        );
        SetTutorialParryWindow(
            steps[currentStepIndex].completionRequirement ==
                    TutorialCompletionRequirement.SuccessfulShieldParry &&
                !guidedParryPracticeComplete
                ? GuidedParryPracticeWindow
                : 0f
        );
        ShowCurrentStep();
    }

    private IEnumerator TransitionToStepRoutine(TutorialStep step)
    {
        isTransitioning = true;
        keysPressedForCurrentStep.Clear();

        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);

        // Fade out the text if it's currently visible (e.g. from a previous step)
        if (promptTextUI != null && promptTextUI.color.a > 0f)
        {
            yield return StartCoroutine(FadeTextAlpha(promptTextUI.color.a, 0f, textFadeDuration));
        }

        // Apply the new text
        if (promptTextUI != null)
        {
            promptTextUI.text = GetCurrentStepPrompt(step);
        }

        // Trigger start events for the current step
        step.onStepStart?.Invoke();

        // Fade the text back in
        if (promptTextUI != null)
        {
            yield return StartCoroutine(FadeTextAlpha(0f, 1f, textFadeDuration));
        }

        isTransitioning = false;
        StartPromptLifetime(step);
    }

    public void CompleteCurrentStep()
    {
        if (
            !isTutorialActive ||
            !isCurrentStepActivated ||
            isTransitioning
        )
        {
            return;
        }

        TutorialStep step = steps[currentStepIndex];
        ApplyStepCompletion(step);

        currentStepIndex++;

        // Dynamically transition to the next step
        if (currentStepIndex < steps.Count)
        {
            BeginCurrentStep();
        }
        else
        {
            EndTutorial();
        }
    }

    public void CompleteDefenseStepsAfterGoblinDefeated()
    {
        if (
            !isTutorialActive ||
            steps == null ||
            currentStepIndex >= steps.Count ||
            !IsShieldDefenseStep(steps[currentStepIndex])
        )
        {
            return;
        }

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        StopPromptLifetime();
        isTransitioning = false;
        isCurrentStepActivated = true;

        while (
            currentStepIndex < steps.Count &&
            IsShieldDefenseStep(steps[currentStepIndex])
        )
        {
            ApplyStepCompletion(steps[currentStepIndex]);
            currentStepIndex++;
        }

        SetTutorialDefenseStaminaFree(false);
        SetTutorialParryWindow(0f);

        if (currentStepIndex < steps.Count)
            BeginCurrentStep();
        else
            EndTutorial();
    }

    private void ApplyStepCompletion(TutorialStep step)
    {
        step.onStepComplete?.Invoke();

        if (step.unlockRoomGatewayOnCompletion > 0)
        {
            SetTutorialRoomGatewayLocked(
                step.unlockRoomGatewayOnCompletion,
                false
            );
        }

        if (step.grantsTutorialProgression)
            GrantTutorialPlayerProgression();

        if (step.unlocksFloorGatewayOnCompletion)
            UnlockTutorialFloorGateway();
    }

    private static bool IsShieldDefenseStep(TutorialStep step)
    {
        if (step == null)
            return false;

        return step.completionRequirement ==
                   TutorialCompletionRequirement.SuccessfulShieldBlock ||
               step.completionRequirement ==
                   TutorialCompletionRequirement.SuccessfulShieldParry ||
               step.completionRequirement ==
                   TutorialCompletionRequirement.ShieldCounterAttack;
    }

    public void EndTutorial()
    {
        isTutorialActive = false;
        SetTutorialDefenseStaminaFree(false);
        SetTutorialParryWindow(0f);
        UnsubscribeFromShieldDefense();
        StopPromptLifetime();
        CleanupTutorialDummy();
        
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        StartCoroutine(EndTutorialRoutine());
    }

    private void CleanupTutorialDummy()
    {
        TutorialDummyStepTrigger[] tutorialDummies =
            FindObjectsOfType<TutorialDummyStepTrigger>(true);

        foreach (TutorialDummyStepTrigger tutorialDummy in tutorialDummies)
        {
            if (tutorialDummy == null || tutorialDummy.gameObject == null)
                continue;

            Destroy(tutorialDummy.gameObject);
        }
    }

    private IEnumerator EndTutorialRoutine()
    {
        // Fade out the panel and the text at the same time
        Coroutine textFade = null;
        if (promptTextUI != null)
        {
            textFade = StartCoroutine(FadeTextAlpha(promptTextUI.color.a, 0f, panelFadeDuration));
        }

        if (panelCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(panelCanvasGroup, panelCanvasGroup.alpha, 0f, panelFadeDuration));
        }
        else if (textFade != null)
        {
            yield return textFade;
        }

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
            
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float targetAlpha, float duration)
    {
        if (cg == null) yield break;

        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }
        cg.alpha = targetAlpha;
    }

    private IEnumerator FadeTextAlpha(float startAlpha, float targetAlpha, float duration)
    {
        if (promptTextUI == null) yield break;

        Color c = promptTextUI.color;
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            promptTextUI.color = c;
            yield return null;
        }
        c.a = targetAlpha;
        promptTextUI.color = c;
    }

    private bool IsOnTutorialFloor()
    {
        if (floorManager == null)
            floorManager = FindFirstObjectByType<FloorManager>();

        if (floorManager == null)
        {
            Debug.LogError(
                "TutorialManager requires a FloorManager reference.",
                this
            );
            return false;
        }

        return floorManager.IsTutorialFloor;
    }

    private void HideTutorialPanel()
    {
        isTutorialActive = false;
        isCurrentStepActivated = false;
        SetTutorialDefenseStaminaFree(false);
        SetTutorialParryWindow(0f);
        StopPromptLifetime();
        CleanupTutorialDummy();

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
    }

    public void HideCurrentPrompt()
    {
        StopPromptLifetime();

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
    }

    private bool TryCompletePanelRequirement(TutorialStep step)
    {
        switch (step.completionRequirement)
        {
            case TutorialCompletionRequirement.AttributePanelOpened:
                if (GetPlayerStatsUI() != null && playerStatsUI.IsOpen)
                    CompleteCurrentStep();
                return true;

            case TutorialCompletionRequirement.AllocationPointsSpent:
                if (
                    GetPlayerStatsUI() != null &&
                    !playerStatsUI.HasRemainingAttributePoints() &&
                    !playerStatsUI.HasRemainingTraitPoints()
                )
                {
                    CompleteCurrentStep();
                }
                return true;

            case TutorialCompletionRequirement.AttributePanelClosed:
                if (GetPlayerStatsUI() != null && !playerStatsUI.IsOpen)
                    CompleteCurrentStep();
                return true;

            case TutorialCompletionRequirement.InventoryPanelOpened:
                if (GetPlayerInventory() != null && playerInventory.IsOpen)
                    CompleteCurrentStep();
                return true;

            case TutorialCompletionRequirement.InventoryPanelClosed:
                if (GetPlayerInventory() != null && !playerInventory.IsOpen)
                    CompleteCurrentStep();
                return true;

            default:
                return false;
        }
    }

    private void StartPromptLifetime(TutorialStep step)
    {
        StopPromptLifetime();

        if (step.promptLifetime > 0f)
        {
            promptLifetimeCoroutine = StartCoroutine(
                HidePromptAfterLifetime(step.promptLifetime)
            );
        }
    }

    private IEnumerator HidePromptAfterLifetime(float lifetime)
    {
        yield return new WaitForSeconds(lifetime);
        promptLifetimeCoroutine = null;
        HideCurrentPrompt();
    }

    private void StopPromptLifetime()
    {
        if (promptLifetimeCoroutine == null)
            return;

        StopCoroutine(promptLifetimeCoroutine);
        promptLifetimeCoroutine = null;
    }

    private void EquipTutorialShieldIfNeeded()
    {
        EquipmentManager equipmentManager = EquipmentManager.Instance;
        if (equipmentManager == null)
        {
            Debug.LogError(
                "TutorialManager requires an EquipmentManager to equip " +
                "the tutorial shield.",
                this
            );
            return;
        }

        if (equipmentManager.GetEquippedItem(EquipmentType.Shield) != null)
            return;

        if (tutorialTrainingShield == null)
        {
            Debug.LogError(
                "TutorialManager requires a training shield ItemData " +
                "when the player has no shield equipped.",
                this
            );
            return;
        }

        InventoryItem trainingShield =
            new InventoryItem(tutorialTrainingShield);
        if (
            !equipmentManager.Equip(
                EquipmentType.Shield,
                trainingShield,
                out _
            )
        )
        {
            Debug.LogError(
                "TutorialManager could not equip the training shield.",
                this
            );
        }
    }

    private bool HasShieldDefenseSteps()
    {
        if (steps == null)
            return false;

        foreach (TutorialStep step in steps)
        {
            if (
                step != null &&
                (
                    step.completionRequirement ==
                        TutorialCompletionRequirement.SuccessfulShieldBlock ||
                    step.completionRequirement ==
                        TutorialCompletionRequirement.SuccessfulShieldParry ||
                    step.completionRequirement ==
                        TutorialCompletionRequirement.ShieldCounterAttack
                )
            )
            {
                return true;
            }
        }

        return false;
    }

    private void SubscribeToShieldDefense()
    {
        PlayerShieldDefense currentDefense =
            Player.Instance != null
                ? Player.Instance.GetComponent<PlayerShieldDefense>()
                : null;

        if (playerShieldDefense == currentDefense)
            return;

        UnsubscribeFromShieldDefense();
        playerShieldDefense = currentDefense;

        if (playerShieldDefense == null)
        {
            Debug.LogError(
                "TutorialManager requires PlayerShieldDefense for the " +
                "tutorial defense steps.",
                this
            );
            return;
        }

        playerShieldDefense.OnSuccessfulBlock +=
            HandleSuccessfulShieldBlock;
        playerShieldDefense.OnGuardStarted += HandleShieldGuardStarted;
        playerShieldDefense.OnSuccessfulParry +=
            HandleSuccessfulShieldParry;
        playerShieldDefense.OnCounterAttackPerformed +=
            HandleShieldCounterAttack;
    }

    private void UnsubscribeFromShieldDefense()
    {
        if (playerShieldDefense == null)
            return;

        playerShieldDefense.OnSuccessfulBlock -=
            HandleSuccessfulShieldBlock;
        playerShieldDefense.OnGuardStarted -= HandleShieldGuardStarted;
        playerShieldDefense.OnSuccessfulParry -=
            HandleSuccessfulShieldParry;
        playerShieldDefense.OnCounterAttackPerformed -=
            HandleShieldCounterAttack;
        playerShieldDefense = null;
    }

    private void HandleShieldGuardStarted(float parryWindow)
    {
        parryWindowDuration = parryWindow;
        parryWindowEndsAt = Time.time + parryWindow;
        hasParryWindowTimer = parryWindow > 0f;
        defenseTimingFeedback = null;
    }

    private void HandleSuccessfulShieldBlock()
    {
        if (
            IsCurrentStepActive &&
            steps[currentStepIndex].completionRequirement ==
                TutorialCompletionRequirement.SuccessfulShieldParry
        )
        {
            SetDefenseTimingFeedback(
                "BLOCKED — release and raise your guard just before impact to parry."
            );
        }

        CompleteDefenseStep(
            TutorialCompletionRequirement.SuccessfulShieldBlock
        );
    }

    private void HandleSuccessfulShieldParry()
    {
        hasParryWindowTimer = false;

        if (
            IsCurrentStepActive &&
            steps[currentStepIndex].completionRequirement ==
                TutorialCompletionRequirement.SuccessfulShieldParry &&
            !guidedParryPracticeComplete
        )
        {
            guidedParryPracticeComplete = true;
            SetTutorialParryWindow(0f);
            if (promptTextUI != null)
            {
                promptTextUI.text = GetCurrentStepPrompt(
                    steps[currentStepIndex]
                );
            }
            SetDefenseTimingFeedback(
                "PRACTICE COMPLETE — now try the real 0.2s parry window."
            );
            return;
        }

        CompleteDefenseStep(
            TutorialCompletionRequirement.SuccessfulShieldParry
        );
    }

    private void HandleShieldCounterAttack()
    {
        CompleteDefenseStep(
            TutorialCompletionRequirement.ShieldCounterAttack
        );
    }

    private void CompleteDefenseStep(
        TutorialCompletionRequirement requirement)
    {
        if (
            IsCurrentStepActive &&
            steps[currentStepIndex].completionRequirement == requirement
        )
        {
            CompleteCurrentStep();
        }
    }

    private void ResetTutorialPlayerProgression()
    {
        ResolvePlayerProgressionReferences();

        if (playerStats == null || playerAttributes == null)
        {
            Debug.LogError(
                "TutorialManager requires PlayerStats and " +
                "PlayerAttributesNTraits to reset tutorial progression.",
                this
            );
            return;
        }

        playerStats.SetLevelAndExperience(0);
        playerAttributes.SetAllocationPoints(0, 0);
    }

    private void GrantTutorialPlayerProgression()
    {
        ResolvePlayerProgressionReferences();

        if (playerStats == null || playerAttributes == null)
        {
            Debug.LogError(
                "TutorialManager requires PlayerStats and " +
                "PlayerAttributesNTraits to grant tutorial progression.",
                this
            );
            return;
        }

        playerStats.SetLevelAndExperience(1);
        playerAttributes.SetAllocationPoints(
            fightRewardAttributePoints,
            fightRewardTraitPoints
        );
    }

    private void ResolvePlayerProgressionReferences()
    {
        if (Player.Instance == null)
            return;

        if (playerStats == null)
            playerStats = Player.Instance.GetComponent<PlayerStats>();

        if (playerAttributes == null)
        {
            playerAttributes =
                Player.Instance.GetComponent<PlayerAttributesNTraits>();
        }
    }

    private PlayerStatsUI GetPlayerStatsUI()
    {
        if (playerStatsUI == null)
        {
            playerStatsUI = FindFirstObjectByType<PlayerStatsUI>(
                FindObjectsInactive.Include
            );
        }

        return playerStatsUI;
    }

    private PlayerInventory GetPlayerInventory()
    {
        if (Player.Instance != null)
        {
            PlayerInventory runtimeInventory =
                Player.Instance.GetComponent<PlayerInventory>();
            if (runtimeInventory != null)
            {
                playerInventory = runtimeInventory;
                return playerInventory;
            }
        }

        playerInventory = FindFirstObjectByType<PlayerInventory>(
            FindObjectsInactive.Include
        );
        return playerInventory;
    }

    private void UnlockTutorialFloorGateway()
    {
        if (roomManager == null)
            roomManager = FindFirstObjectByType<RoomManager>();

        if (roomManager == null)
        {
            Debug.LogError(
                "TutorialManager requires a RoomManager reference.",
                this
            );
            return;
        }

        roomManager.UnlockTutorialFloorGateway();
    }

    private void SetTutorialRoomGatewayLocked(
        int roomNumber,
        bool locked)
    {
        if (roomManager == null)
            roomManager = FindFirstObjectByType<RoomManager>();

        if (roomManager == null)
        {
            Debug.LogError(
                "TutorialManager requires a RoomManager reference.",
                this
            );
            return;
        }

        roomManager.SetTutorialRoomGatewayLocked(
            roomNumber,
            locked
        );
    }
}
