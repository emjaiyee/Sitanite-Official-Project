using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
    private Coroutine transitionCoroutine;
    
    // Tracks which keys have been successfully pressed in the current step
    private HashSet<KeyCode> keysPressedForCurrentStep = new HashSet<KeyCode>();

    private void Start()
    {
        // Automatically start the tutorial if steps exist, or you can call StartTutorial() externally
        if (steps != null && steps.Count > 0)
        {
            StartTutorial();
        }
    }

    public void StartTutorial()
    {
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(StartTutorialRoutine());
    }

    private IEnumerator StartTutorialRoutine()
    {
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
        ShowCurrentStep();
    }

    private void Update()
    {
        // Pause input monitoring if the tutorial is inactive, finished, or currently fading
        if (!isTutorialActive || steps == null || currentStepIndex >= steps.Count || isTransitioning) 
            return;

        TutorialStep currentStep = steps[currentStepIndex];

        // If no required keys are set, we just wait for an external script to call CompleteCurrentStep()
        if (currentStep.requiredKeys == null || currentStep.requiredKeys.Count == 0)
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

    private void ShowCurrentStep()
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(TransitionToStepRoutine(steps[currentStepIndex]));
    }

    private IEnumerator TransitionToStepRoutine(TutorialStep step)
    {
        isTransitioning = true;
        keysPressedForCurrentStep.Clear();

        // Fade out the text if it's currently visible (e.g. from a previous step)
        if (promptTextUI != null && promptTextUI.color.a > 0f)
        {
            yield return StartCoroutine(FadeTextAlpha(promptTextUI.color.a, 0f, textFadeDuration));
        }

        // Apply the new text
        if (promptTextUI != null)
        {
            promptTextUI.text = step.promptText;
        }

        // Trigger start events for the current step
        step.onStepStart?.Invoke();

        // Fade the text back in
        if (promptTextUI != null)
        {
            yield return StartCoroutine(FadeTextAlpha(0f, 1f, textFadeDuration));
        }

        isTransitioning = false;
    }

    public void CompleteCurrentStep()
    {
        if (!isTutorialActive || isTransitioning) return;

        TutorialStep step = steps[currentStepIndex];
        
        // Trigger completion events (e.g., granting the player an item)
        step.onStepComplete?.Invoke();

        currentStepIndex++;

        // Dynamically transition to the next step
        if (currentStepIndex < steps.Count)
        {
            ShowCurrentStep();
        }
        else
        {
            EndTutorial();
        }
    }

    public void EndTutorial()
    {
        isTutorialActive = false;
        
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        StartCoroutine(EndTutorialRoutine());
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
}
