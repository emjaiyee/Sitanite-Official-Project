using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class IntroChecks
{
    [MenuItem("Tools/Intro/Run Checks")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before running Intro checks.");

        CheckScene();
        CheckAutomaticPlayback();
        CheckAdvance();
        CheckEscape();
        Debug.Log("PASS: Intro scene wiring, full-canvas artwork, TMP narration, startup ordering, 15-second holds, black-only slide swaps, four-slide completion, advance debounce, Escape during all phases, and exit music fade.");
    }

    private static void CheckScene()
    {
        Require(EditorBuildSettings.scenes[0].enabled &&
            EditorBuildSettings.scenes[0].path == "Assets/Scenes/Intro.unity", "Intro is not the startup scene.");
        Require(EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == "Assets/Scenes/Main Menu.unity"),
            "Main Menu is missing from build settings.");

        Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/Intro.unity");
        bool openedForCheck = !scene.IsValid() || !scene.isLoaded;
        if (openedForCheck)
            scene = EditorSceneManager.OpenScene("Assets/Scenes/Intro.unity", OpenSceneMode.Additive);
        try
        {
            IntroController controller = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<IntroController>(true)).Single();
            GameObject[] slides = Get<GameObject[]>(controller, "slides");
            Require(slides.Length == 4, "Expected four editable slides.");
            Require(Get<float>(controller, "slideDuration") == 15f, "Slide duration is not 15 seconds.");
            Require(Get<string>(controller, "mainMenuSceneName") == "Main Menu", "Wrong destination scene.");
            foreach (GameObject slide in slides)
            {
                Image image = slide.transform.Find("Splash Art - Assign PNG Sprite").GetComponent<Image>();
                Require(image.rectTransform.anchorMin == Vector2.zero && image.rectTransform.anchorMax == Vector2.one &&
                    image.rectTransform.sizeDelta == Vector2.zero && !image.preserveAspect, "Splash art does not fill the canvas.");
                Require(slide.GetComponentsInChildren<TextMeshProUGUI>(true).Length >= 2, "Missing editable TMP text.");
            }
            CanvasGroup fade = Get<CanvasGroup>(controller, "fadeCanvas");
            Require(fade.alpha == 1f && fade.GetComponent<Image>().color == Color.black, "Scene must start black.");
            Require(fade.transform.GetSiblingIndex() == fade.transform.parent.childCount - 1, "Fade must cover all UI.");
            AudioSource music = Get<AudioSource>(controller, "musicSource");
            Require(music != null && !music.playOnAwake && music.loop && music.spatialBlend == 0f,
                "Intro music is not configured for controlled 2D playback.");
        }
        finally
        {
            if (openedForCheck)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void CheckAutomaticPlayback()
    {
        using (Fixture fixture = new Fixture())
        {
            Require(fixture.Alpha == 1f && fixture.Index == 0, "Intro did not initialize black on slide one.");
            for (int i = 0; i < 4; i++)
            {
                fixture.Tick(0.5f);
                Require(Mathf.Approximately(fixture.Alpha, 0.5f), "Fade-in midpoint is incorrect.");
                fixture.Tick(0.5f);
                Require(fixture.Alpha == 0f && fixture.Phase == "Showing", "Fade-in did not complete.");
                fixture.Tick(14.99f);
                Require(fixture.Phase == "Showing" && fixture.Index == i, "Slide advanced before 15 seconds.");
                fixture.Tick(0.01f);
                Require(fixture.Phase == "FadingOut" && fixture.Index == i, "Slide did not advance at 15 seconds.");
                float volumeBefore = fixture.Music.volume;
                fixture.Tick(0.5f);
                Require(fixture.Index == i && Mathf.Approximately(fixture.Alpha, 0.5f), "Art changed before black.");
                if (i == 3)
                    Require(fixture.Music.volume < volumeBefore, "Exit music did not fade.");
                else
                    Require(fixture.Music.volume == volumeBefore, "Music faded between slides.");
                fixture.Tick(0.5f);
                Require(fixture.Alpha == 1f, "Transition did not reach full black.");
                Require(fixture.Index == Mathf.Min(i + 1, 3), "Wrong slide order.");
            }
            Require(fixture.Phase == "Complete" && fixture.Music.volume == 0f, "Fourth slide did not finish silently at black.");
            fixture.Tick(20f, true, true);
            Require(fixture.Phase == "Complete", "Completed intro accepted additional input.");
        }
    }

    private static void CheckAdvance()
    {
        using (Fixture fixture = new Fixture())
        {
            fixture.Tick(1f);
            fixture.Tick(0f, true);
            fixture.Tick(0.5f, true);
            Require(fixture.Index == 0, "Repeated input bypassed fade-out.");
            fixture.Tick(0.5f, true);
            Require(fixture.Index == 1 && fixture.Alpha == 1f, "Advance did not swap exactly once at black.");
            fixture.Tick(0.25f);
            float alphaBefore = fixture.Alpha;
            fixture.Tick(0f, true);
            Require(fixture.Alpha == alphaBefore, "Advance during fade-in caused a brightness jump.");
            fixture.Tick(1f);
            Require(fixture.Index == 2 && fixture.Alpha == 1f, "Advance during fade-in failed.");
            fixture.Tick(1f);
            fixture.Tick(0f, true);
            fixture.Tick(1f);
            fixture.Tick(1f);
            fixture.Tick(0f, true);
            fixture.Tick(1f);
            Require(fixture.Phase == "Complete" && fixture.Alpha == 1f, "Skipping the last slide did not exit at black.");
        }
    }

    private static void CheckEscape()
    {
        float[] initialTimes = { 0f, 0.5f, 1f };
        foreach (float initialTime in initialTimes)
        {
            using (Fixture fixture = new Fixture())
            {
                fixture.Tick(initialTime);
                float alphaBefore = fixture.Alpha;
                fixture.Tick(0f, true, true);
                Require(fixture.Alpha == alphaBefore, "Escape caused a brightness jump.");
                fixture.Tick(0.5f, true, true);
                Require(fixture.Phase == "FadingOut" && fixture.Index == 0, "Repeated Escape restarted or advanced the transition.");
                fixture.Tick(0.5f);
                Require(fixture.Phase == "Complete" && fixture.Alpha == 1f, "Escape failed during fade-in or hold.");
            }
        }

        using (Fixture fixture = new Fixture())
        {
            fixture.Tick(1f);
            fixture.Tick(0f, true);
            fixture.Tick(0.5f);
            fixture.Tick(0f, false, true);
            Require(Mathf.Approximately(fixture.Alpha, 0.5f), "Escape interrupted an existing fade discontinuously.");
            fixture.Tick(1f);
            Require(fixture.Index == 0 && fixture.Phase == "Complete" && fixture.Alpha == 1f,
                "Escape during a slide transition did not go directly to the menu.");
        }
    }

    private sealed class Fixture : IDisposable
    {
        private readonly GameObject root;
        private readonly IntroController controller;
        public AudioSource Music { get; }
        public float Alpha => Get<CanvasGroup>(controller, "fadeCanvas").alpha;
        public int Index => Get<int>(controller, "slideIndex");
        public string Phase => Get<object>(controller, "phase").ToString();

        public Fixture()
        {
            root = new GameObject("Intro Check") { hideFlags = HideFlags.HideAndDontSave };
            root.SetActive(false);
            controller = root.AddComponent<IntroController>();
            GameObject[] slides = new GameObject[4];
            for (int i = 0; i < slides.Length; i++)
            {
                slides[i] = new GameObject($"Slide {i}");
                slides[i].transform.SetParent(root.transform);
            }
            GameObject fade = new GameObject("Fade", typeof(RectTransform), typeof(CanvasGroup));
            fade.transform.SetParent(root.transform);
            Music = root.AddComponent<AudioSource>();
            Set(controller, "slides", slides);
            Set(controller, "fadeCanvas", fade.GetComponent<CanvasGroup>());
            Set(controller, "musicSource", Music);
            Invoke(controller, "Awake");
            Require(controller.enabled, "Valid intro configuration was rejected.");
            Music.volume = 0.8f;
        }

        public void Tick(float delta, bool advance = false, bool skip = false)
        {
            Invoke(controller, "Tick", delta, advance, skip);
            GameObject[] slides = Get<GameObject[]>(controller, "slides");
            for (int i = 0; i < slides.Length; i++)
                Require(slides[i].activeSelf == (i == Index), "Multiple slides are active.");
        }

        public void Dispose() => Object.DestroyImmediate(root);
    }

    private static T Get<T>(object target, string field) =>
        (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

    private static void Invoke(object target, string method, params object[] arguments) =>
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, arguments);

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
