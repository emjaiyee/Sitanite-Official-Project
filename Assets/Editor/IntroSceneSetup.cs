using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class IntroSceneSetup
{
    private const string ScenePath = "Assets/Scenes/Intro.unity";
    private static readonly string RequestPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/IntroSceneSetup.request"));
    private static readonly string ResultPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/IntroSceneSetup.result"));

    static IntroSceneSetup()
    {
        if (File.Exists(RequestPath))
            EditorApplication.delayCall += RunRequestedSetup;
    }

    private static void RunRequestedSetup()
    {
        if (!File.Exists(RequestPath))
            return;

        File.Delete(RequestPath);
        try
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                CreateScene();
            IntroChecks.Run();
            File.WriteAllText(ResultPath, "PASS: Intro scene saved, registered first in build settings, and checks passed.");
        }
        catch (Exception exception)
        {
            File.WriteAllText(ResultPath, $"FAIL: {exception}");
            Debug.LogException(exception);
        }
    }

    [MenuItem("Tools/Intro/Create Scene")]
    public static void CreateScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before creating Intro.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            throw new InvalidOperationException("Intro already exists. Edit the existing scene instead of overwriting it.");

        Scene previousScene = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            GameObject canvasObject = new GameObject("Intro Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject[] slides = new GameObject[4];
            Color[] colors =
            {
                new Color(0.12f, 0.16f, 0.24f),
                new Color(0.22f, 0.13f, 0.12f),
                new Color(0.12f, 0.21f, 0.18f),
                new Color(0.18f, 0.12f, 0.23f)
            };
            for (int i = 0; i < slides.Length; i++)
            {
                RectTransform slide = CreateRect($"Slide {i + 1:00}", canvasObject.transform);
                slides[i] = slide.gameObject;
                RectTransform art = CreateRect("Splash Art - Assign PNG Sprite", slide);
                Image image = art.gameObject.AddComponent<Image>();
                image.color = Color.white;
                image.preserveAspect = false;
                image.raycastTarget = false;

                RectTransform placeholder = CreateRect("Placeholder - Disable After Assigning Art", slide);
                Image placeholderImage = placeholder.gameObject.AddComponent<Image>();
                placeholderImage.color = colors[i];
                placeholderImage.raycastTarget = false;
                TextMeshProUGUI label = CreateText("Placeholder Label", placeholder,
                    $"SLIDE {i + 1:00}\nAssign splash art PNG and edit narration", 44f);
                label.rectTransform.anchorMin = new Vector2(0.1f, 0.35f);
                label.rectTransform.anchorMax = new Vector2(0.9f, 0.8f);

                RectTransform backing = CreateRect("Narration Backing", slide);
                backing.anchorMin = new Vector2(0f, 0f);
                backing.anchorMax = new Vector2(1f, 0.28f);
                Image backingImage = backing.gameObject.AddComponent<Image>();
                backingImage.color = new Color(0f, 0f, 0f, 0.78f);
                backingImage.raycastTarget = false;
                TextMeshProUGUI narration = CreateText("Narration (TMP)", backing,
                    $"[Slide {i + 1}: Replace this text with the events shown in this splash art.]", 36f);
                narration.rectTransform.offsetMin = new Vector2(120f, 65f);
                narration.rectTransform.offsetMax = new Vector2(-120f, -25f);
                slides[i].SetActive(i == 0);
            }

            TextMeshProUGUI hint = CreateText("Skip Controls (TMP)", canvasObject.transform,
                "Click / Space / Enter: Next slide     |     Esc: Skip intro", 24f);
            hint.rectTransform.anchorMax = new Vector2(1f, 0f);
            hint.rectTransform.sizeDelta = new Vector2(0f, 44f);
            hint.rectTransform.anchoredPosition = new Vector2(0f, 30f);
            hint.color = new Color(0.8f, 0.8f, 0.8f);

            RectTransform fade = CreateRect("Fade Overlay", canvasObject.transform);
            Image fadeImage = fade.gameObject.AddComponent<Image>();
            fadeImage.color = Color.black;
            fadeImage.raycastTarget = false;
            CanvasGroup fadeGroup = fade.gameObject.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 1f;

            GameObject musicObject = new GameObject("Intro Music", typeof(AudioSource));
            AudioSource music = musicObject.GetComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.spatialBlend = 0f;
            music.volume = 0.75f;

            GameObject controllerObject = new GameObject("Intro Controller", typeof(IntroController));
            SerializedObject controller = new SerializedObject(controllerObject.GetComponent<IntroController>());
            SerializedProperty slideArray = controller.FindProperty("slides");
            slideArray.arraySize = slides.Length;
            for (int i = 0; i < slides.Length; i++)
                slideArray.GetArrayElementAtIndex(i).objectReferenceValue = slides[i];
            controller.FindProperty("fadeCanvas").objectReferenceValue = fadeGroup;
            controller.FindProperty("musicSource").objectReferenceValue = music;
            controller.ApplyModifiedPropertiesWithoutUndo();

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Could not save Intro scene.");

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
                .Concat(EditorBuildSettings.scenes.Where(entry => entry.path != ScenePath)).ToArray();
            AssetDatabase.SaveAssets();
        }
        finally
        {
            SceneManager.SetActiveScene(previousScene);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size)
    {
        TextMeshProUGUI label = CreateRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.text = text;
        label.fontSize = size;
        label.enableAutoSizing = true;
        label.fontSizeMin = 18f;
        label.fontSizeMax = size;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }
}
