using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ManggagawaySetup
{
    public const string AssetFolder = "Assets/Prefabs/Enemies/Manggagaway";

    [MenuItem("Tools/Manggagaway/Create Starter Assets")]
    public static void CreateAssets()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before creating boss assets.");
        EnsureFolder(AssetFolder);
        ManggagawayDefinition definition = AssetDatabase.LoadAssetAtPath<ManggagawayDefinition>($"{AssetFolder}/ManggagawayDefinition.asset");
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<ManggagawayDefinition>();
            AssetDatabase.CreateAsset(definition, $"{AssetFolder}/ManggagawayDefinition.asset");
        }
        GameObject projectile = CreateSpell("SpellProjectile", new Color(0.7f, 0.2f, 0.9f), typeof(ManggagawayProjectile));
        GameObject beam = CreateSpell("Beam", new Color(0.9f, 0.2f, 0.5f), typeof(Beam));
        GameObject charge = CreateSpell("BeamCharge", Color.magenta);
        GameObject aoe = CreateSpell("AOE", new Color(0.8f, 0.15f, 0.2f));
        GameObject summoning = CreateSpell("Summoning", new Color(0.4f, 0.15f, 0.8f));
        GameObject heal = CreateSpell("Heal", Color.green);
        GameObject teleport = CreateSpell("Teleportation", Color.cyan);
        string bossPath = $"{AssetFolder}/EnemyManggagaway.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(bossPath) == null)
        {
            GameObject root = new GameObject("EnemyManggagaway");
            root.SetActive(false);
            try
            {
                root.layer = LayerMask.NameToLayer("Enemy");
                root.tag = "Enemy";
                EnemyHealth health = root.AddComponent<EnemyHealth>();
                SetInt(health, "maxHealth", 500);
                root.AddComponent<EnemyLevelXP>();
                root.AddComponent<EnemyElevationLevel>();
                CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
                collider.radius = 0.35f;
                Rigidbody2D body = root.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
                GameObject visual = new GameObject("Boss Sprite - Assign Definition Sheets");
                visual.layer = root.layer;
                visual.transform.SetParent(root.transform, false);
                SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sprite = PlaceholderSprite();
                renderer.color = Color.white;
                renderer.sortingLayerName = "Player";
                renderer.sortingOrder = 10;
                ManggagawaySpriteAnimator animator = visual.AddComponent<ManggagawaySpriteAnimator>();
                SetObject(animator, "definition", definition);
                EnemyManggagaway boss = root.AddComponent<EnemyManggagaway>();
                SetObject(boss, "spriteAnimator", animator);
                SetObject(boss, "projectilePrefab", projectile);
                SetObject(boss, "beamPrefab", beam);
                SetObject(boss, "beamChargePrefab", charge);
                SetObject(boss, "aoePrefab", aoe);
                SetObject(boss, "summonPrefab", summoning);
                SetObject(boss, "healPrefab", heal);
                SetObject(boss, "teleportPrefab", teleport);
                SerializedObject settings = new SerializedObject(boss);
                settings.FindProperty("blockingLayers").intValue = LayerMask.GetMask("Obstacle");
                settings.ApplyModifiedPropertiesWithoutUndo();
                root.SetActive(true);
                PrefabUtility.SaveAsPrefabAsset(root, bossPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
        CreateHealthBar();
        AssetDatabase.SaveAssetIfDirty(definition);
        Debug.Log($"Manggagaway assets ready in {AssetFolder}. Assign animation sheets and summon pool. " +
            "Place the boss inside a RoomInstance and the separate MainBossHealthBar on your HUD. Existing assets are never overwritten.");
    }

    private static GameObject CreateSpell(string name, Color color, Type behavior = null)
    {
        string path = $"{AssetFolder}/{name}.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            return existing;
        GameObject root = new GameObject(name);
        try
        {
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = PlaceholderSprite();
            renderer.color = color;
            renderer.sortingLayerName = "Effects";
            renderer.sortingOrder = 11;
            root.AddComponent<ManggagawaySpellVisual>();
            if (behavior != null)
                root.AddComponent(behavior);
            if (name == "SpellProjectile")
                root.transform.localScale = Vector3.one * 0.25f;
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void CreateHealthBar()
    {
        string path = $"{AssetFolder}/MainBossHealthBar.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            return;
        GameObject root = new GameObject("MainBossHealthBar", typeof(RectTransform), typeof(CanvasGroup));
        try
        {
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -80f);
            rect.sizeDelta = new Vector2(650f, 64f);
            GameObject sliderObject = new GameObject("Health", typeof(RectTransform), typeof(Image), typeof(Slider));
            sliderObject.transform.SetParent(root.transform, false);
            Stretch(sliderObject.GetComponent<RectTransform>());
            sliderObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, -32f);
            sliderObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -16f);
            sliderObject.GetComponent<Image>().color = new Color(0.1f, 0.02f, 0.08f, 0.9f);
            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(sliderObject.transform, false);
            Stretch(fillObject.GetComponent<RectTransform>());
            fillObject.GetComponent<Image>().color = new Color(0.65f, 0.1f, 0.7f);
            Slider slider = sliderObject.GetComponent<Slider>();
            slider.fillRect = fillObject.GetComponent<RectTransform>();
            slider.interactable = false;
            slider.value = 1f;
            GameObject labelObject = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(root.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0.5f);
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = 24f;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            BossHealthBarUI ui = root.AddComponent<BossHealthBarUI>();
            SetObject(ui, "canvasGroup", root.GetComponent<CanvasGroup>());
            SetObject(ui, "healthSlider", slider);
            SetObject(ui, "bossNameText", label);
            SerializedObject settings = new SerializedObject(ui);
            settings.FindProperty("trackMainBoss").boolValue = true;
            settings.ApplyModifiedPropertiesWithoutUndo();
            root.GetComponent<CanvasGroup>().alpha = 0f;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static Sprite PlaceholderSprite() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string parent = path.Substring(0, path.LastIndexOf('/'));
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
    }

    private static void SetObject(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        SerializedObject settings = new SerializedObject(target);
        settings.FindProperty(field).objectReferenceValue = value;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetInt(UnityEngine.Object target, string field, int value)
    {
        SerializedObject settings = new SerializedObject(target);
        settings.FindProperty(field).intValue = value;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }
}
