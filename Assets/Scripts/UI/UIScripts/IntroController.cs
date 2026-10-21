using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class IntroController : MonoBehaviour
{
    [Header("Slides (in playback order)")]
    [SerializeField] private GameObject[] slides;
    [Tooltip("Fully visible time per slide, excluding fades.")]
    [SerializeField, Min(0.01f)] private float slideDuration = 15f;

    [Header("Transition")]
    [SerializeField] private CanvasGroup fadeCanvas;
    [Tooltip("Duration of each fade. Slide changes and scene loading happen only at full black.")]
    [SerializeField, Min(0.01f)] private float fadeDuration = 1f;
    [SerializeField] private string mainMenuSceneName = "Main Menu";

    [Header("Music")]
    [Tooltip("Assign the intro OST to this AudioSource's AudioClip field.")]
    [SerializeField] private AudioSource musicSource;
    [Tooltip("Used until a MusicVolume preference is saved by the main menu.")]
    [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 0.75f;

    private enum Phase { FadingIn, Showing, FadingOut, Complete }

    private Phase phase;
    private int slideIndex;
    private float elapsed;
    private float fadeStartAlpha;
    private float fadeStartVolume;
    private bool exitRequested;
    private bool sceneLoadStarted;

    private void Awake()
    {
        if (fadeCanvas == null || musicSource == null || slides == null || slides.Length == 0 ||
            !float.IsFinite(slideDuration) || slideDuration <= 0f ||
            !float.IsFinite(fadeDuration) || fadeDuration <= 0f ||
            string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.LogError("IntroController: Assign slides, fade overlay, music source, destination and positive finite durations.", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < slides.Length; i++)
        {
            if (slides[i] == null)
            {
                Debug.LogError($"IntroController: Slide {i + 1} is not assigned.", this);
                enabled = false;
                return;
            }
        }

        fadeCanvas.transform.SetAsLastSibling();
        fadeCanvas.alpha = 1f;
        fadeCanvas.blocksRaycasts = true;
        fadeCanvas.interactable = false;
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.volume = Mathf.Clamp01(PlayerPrefs.GetFloat("MusicVolume", defaultMusicVolume));
        ShowSlide(0);
        phase = Phase.FadingIn;
    }

    private void Start()
    {
        if (!Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            Debug.LogError($"IntroController: Scene '{mainMenuSceneName}' is not enabled in the build scene list.", this);
            enabled = false;
            return;
        }

        if (musicSource.clip != null)
            musicSource.Play();
        else
            Debug.LogWarning("IntroController: Assign the intro OST to Intro Music > AudioSource > AudioClip. The intro will play without music until assigned.", this);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        bool skip = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        bool advance = (mouse != null && mouse.leftButton.wasPressedThisFrame) ||
            (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame ||
                keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame));

        Tick(Time.unscaledDeltaTime, advance, skip);

        if (phase == Phase.Complete && !sceneLoadStarted)
        {
            sceneLoadStarted = true;
            SceneManager.LoadSceneAsync(mainMenuSceneName);
        }
    }

    private void Tick(float deltaTime, bool advance, bool skip)
    {
        if (phase == Phase.Complete)
            return;

        if (skip && !exitRequested)
        {
            exitRequested = true;
            BeginFadeOut();
        }
        else if (advance && (phase == Phase.Showing || phase == Phase.FadingIn))
        {
            exitRequested = slideIndex == slides.Length - 1;
            BeginFadeOut();
        }

        elapsed += deltaTime;
        switch (phase)
        {
            case Phase.FadingIn:
                fadeCanvas.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                if (elapsed >= fadeDuration)
                {
                    phase = Phase.Showing;
                    elapsed = 0f;
                }
                break;

            case Phase.Showing:
                if (elapsed >= slideDuration)
                {
                    exitRequested = slideIndex == slides.Length - 1;
                    BeginFadeOut();
                }
                break;

            case Phase.FadingOut:
                float progress = Mathf.Clamp01(elapsed / fadeDuration);
                fadeCanvas.alpha = Mathf.Lerp(fadeStartAlpha, 1f, progress);
                if (exitRequested)
                    musicSource.volume = Mathf.Lerp(fadeStartVolume, 0f, progress);

                if (elapsed >= fadeDuration)
                {
                    if (exitRequested)
                    {
                        musicSource.Stop();
                        phase = Phase.Complete;
                    }
                    else
                    {
                        ShowSlide(slideIndex + 1);
                        phase = Phase.FadingIn;
                        elapsed = 0f;
                    }
                }
                break;
        }
    }

    private void BeginFadeOut()
    {
        phase = Phase.FadingOut;
        elapsed = 0f;
        fadeStartAlpha = fadeCanvas.alpha;
        fadeStartVolume = musicSource.volume;
    }

    private void ShowSlide(int index)
    {
        slideIndex = index;
        for (int i = 0; i < slides.Length; i++)
            slides[i].SetActive(i == index);
    }
}
