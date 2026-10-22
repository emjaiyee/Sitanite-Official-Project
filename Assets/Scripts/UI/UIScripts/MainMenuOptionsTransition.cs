using System.Collections;
using UnityEngine;

public class MainMenuOptionsTransition : MonoBehaviour
{
    [SerializeField] private CanvasGroup menuButtons;
    [SerializeField] private CanvasGroup menuLogos;
    [SerializeField] private CanvasGroup optionsPanel;
    [SerializeField] private SceneTransitionManager sceneTransitionManager;
    [Min(0f)]
    [SerializeField] private float fadeDuration = 0.25f;
    [Min(1f)]
    [SerializeField] private float playZoomScale = 1.06f;
    [Min(0f)]
    [SerializeField] private float playZoomDuration = 0.3f;
    [SerializeField] private AudioClip playSfx;
    [Range(0f, 1f)]
    [SerializeField] private float playSfxVolume = 1f;

    private bool isTransitioning;
    private bool isOptionsOpen;
    private Vector3 menuButtonsScale;
    private Vector3 menuLogosScale;
    private AudioSource audioSource;

    private void Awake()
    {
        if (menuButtons == null || menuLogos == null || optionsPanel == null ||
            sceneTransitionManager == null)
        {
            Debug.LogError(
                "MainMenuOptionsTransition requires menu CanvasGroups and a SceneTransitionManager.",
                this
            );
            enabled = false;
            return;
        }

        menuButtonsScale = menuButtons.transform.localScale;
        menuLogosScale = menuLogos.transform.localScale;
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        AudioManager.RegisterSfxSource(audioSource);

        SetAlpha(menuButtons, 1f);
        SetAlpha(menuLogos, 1f);
        SetInteraction(menuButtons, true);
        SetInteraction(menuLogos, true);
        SetAlpha(optionsPanel, 0f);
        SetInteraction(optionsPanel, false);
        optionsPanel.gameObject.SetActive(false);
    }

    public void OpenOptions()
    {
        if (isTransitioning || isOptionsOpen)
            return;

        StartCoroutine(TransitionToOptions());
    }

    public void CloseOptions()
    {
        if (isTransitioning || !isOptionsOpen)
            return;

        StartCoroutine(TransitionToMenu());
    }

    public void Play()
    {
        if (isTransitioning || isOptionsOpen)
            return;

        if (playSfx != null)
            audioSource.PlayOneShot(playSfx, playSfxVolume);

        StartCoroutine(TransitionToGame());
    }

    private IEnumerator TransitionToGame()
    {
        isTransitioning = true;
        SetInteraction(menuButtons, false);
        SetInteraction(menuLogos, false);

        float elapsed = 0f;
        float duration = Mathf.Max(0f, playZoomDuration);
        Vector3 buttonsTargetScale = menuButtonsScale * Mathf.Max(1f, playZoomScale);
        Vector3 logosTargetScale = menuLogosScale * Mathf.Max(1f, playZoomScale);

        if (duration > 0f)
        {
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = t * t * (3f - 2f * t);
                menuButtons.transform.localScale = Vector3.Lerp(
                    menuButtonsScale,
                    buttonsTargetScale,
                    easedT
                );
                menuLogos.transform.localScale = Vector3.Lerp(
                    menuLogosScale,
                    logosTargetScale,
                    easedT
                );
                yield return null;
            }
        }

        menuButtons.transform.localScale = buttonsTargetScale;
        menuLogos.transform.localScale = logosTargetScale;
        yield return FadeMenu(1f, 0f);
        sceneTransitionManager.TransitionToScene();
    }

    private IEnumerator TransitionToOptions()
    {
        isTransitioning = true;
        SetInteraction(menuButtons, false);
        SetInteraction(menuLogos, false);

        yield return FadeMenu(1f, 0f);

        optionsPanel.gameObject.SetActive(true);
        SetAlpha(optionsPanel, 0f);
        yield return Fade(optionsPanel, 0f, 1f);

        SetInteraction(optionsPanel, true);
        isOptionsOpen = true;
        isTransitioning = false;
    }

    private IEnumerator TransitionToMenu()
    {
        isTransitioning = true;
        SetInteraction(optionsPanel, false);

        yield return Fade(optionsPanel, 1f, 0f);

        optionsPanel.gameObject.SetActive(false);
        yield return FadeMenu(0f, 1f);

        SetInteraction(menuButtons, true);
        SetInteraction(menuLogos, true);
        isOptionsOpen = false;
        isTransitioning = false;
    }

    private IEnumerator FadeMenu(float from, float to)
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0f, fadeDuration);

        if (duration == 0f)
        {
            SetAlpha(menuButtons, to);
            SetAlpha(menuLogos, to);
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            SetAlpha(menuButtons, alpha);
            SetAlpha(menuLogos, alpha);
            yield return null;
        }

        SetAlpha(menuButtons, to);
        SetAlpha(menuLogos, to);
    }

    private IEnumerator Fade(CanvasGroup group, float from, float to)
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0f, fadeDuration);

        if (duration == 0f)
        {
            SetAlpha(group, to);
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(group, Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetAlpha(group, to);
    }

    private static void SetAlpha(CanvasGroup group, float alpha)
    {
        group.alpha = alpha;
    }

    private static void SetInteraction(CanvasGroup group, bool interactable)
    {
        group.interactable = interactable;
        group.blocksRaycasts = interactable;
    }
}
