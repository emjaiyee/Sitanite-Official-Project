using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PlayerFeedbackChecks
{
    private static readonly string RequestPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/PlayerFeedbackChecks.panels.request"));
    private static readonly string ResultPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/PlayerFeedbackChecks.result"));

    static PlayerFeedbackChecks()
    {
        if (File.Exists(RequestPath))
            EditorApplication.delayCall += CheckRequest;
    }

    private static void CheckRequest()
    {
        if (File.Exists(RequestPath))
        {
            File.Delete(RequestPath);
            Run();
        }
    }

    [MenuItem("Tools/Player Feedback/Run Checks")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            File.WriteAllText(ResultPath, "BLOCKED: Exit Play Mode before running player feedback checks.");
            return;
        }

        List<UnityEngine.Object> temporaryObjects = new List<UnityEngine.Object>();
        UnityEngine.Random.State randomState = UnityEngine.Random.state;
        try
        {
            CheckPortraits(temporaryObjects);
            CheckHurtAudioAndHealing(temporaryObjects);
            CheckShake(temporaryObjects);
            CheckPanelRouting(temporaryObjects);
            const string result = "PASS: Five health portrait stages and inclusive cutoffs; body/eyes exclusivity and missing-sprite fallback; 15% pre-hit HP audio threshold, 2s cooldown, smaller-HP scaling and healing exclusion; synchronized, damage-scaled horizontal shake and position restoration; opposite panel toggles close-only and Escape closes player panels without pausing.";
            File.WriteAllText(ResultPath, result);
            Debug.Log(result);
        }
        catch (Exception exception)
        {
            File.WriteAllText(ResultPath, $"FAIL: {exception}");
            Debug.LogException(exception);
        }
        finally
        {
            for (int objectIndex = temporaryObjects.Count - 1; objectIndex >= 0; objectIndex--)
            {
                if (temporaryObjects[objectIndex] != null)
                    UnityEngine.Object.DestroyImmediate(temporaryObjects[objectIndex]);
            }
            UnityEngine.Random.state = randomState;
        }
    }

    private static void CheckPortraits(List<UnityEngine.Object> temporaryObjects)
    {
        Texture2D texture = new Texture2D(2, 2);
        temporaryObjects.Add(texture);
        Sprite[] sprites = new Sprite[5];
        for (int spriteIndex = 0; spriteIndex < sprites.Length; spriteIndex++)
        {
            sprites[spriteIndex] = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
            temporaryObjects.Add(sprites[spriteIndex]);
        }

        CharacterPartDefinition definition = ScriptableObject.CreateInstance<CharacterPartDefinition>();
        temporaryObjects.Add(definition);
        definition.portrait = sprites[0];
        definition.slightlyDamagedPortrait = sprites[1];
        definition.injuredPortrait = sprites[2];
        definition.gravelyInjuredPortrait = sprites[3];
        definition.deadPortrait = sprites[4];

        foreach (CharacterPartType partType in Enum.GetValues(typeof(CharacterPartType)))
        {
            SetField(definition, "type", partType);
            bool healthSensitive = partType == CharacterPartType.Body || partType == CharacterPartType.Eyes;
            float[] healthFractions = { 1f, 0.7501f, 0.75f, 0.5001f, 0.5f, 0.2501f, 0.25f, 0.001f, 0f };
            int[] expectedStages = { 0, 0, 1, 1, 2, 2, 3, 3, 4 };
            for (int stageIndex = 0; stageIndex < healthFractions.Length; stageIndex++)
                Require(definition.GetPortrait(healthFractions[stageIndex]) == sprites[healthSensitive ? expectedStages[stageIndex] : 0],
                    $"Wrong health portrait for {partType} at {healthFractions[stageIndex]}.");
        }

        SetField(definition, "type", CharacterPartType.Body);
        definition.injuredPortrait = null;
        Require(definition.GetPortrait(0.5f) == sprites[0], "Missing health portrait did not fall back to normal.");
    }

    private static void CheckHurtAudioAndHealing(List<UnityEngine.Object> temporaryObjects)
    {
        GameObject playerObject = new GameObject("Player Feedback Check") { hideFlags = HideFlags.HideAndDontSave };
        temporaryObjects.Add(playerObject);
        PlayerStats stats = playerObject.AddComponent<PlayerStats>();
        PlayerSoundHelper sound = playerObject.AddComponent<PlayerSoundHelper>();
        AudioClip clip = AudioClip.Create("Feedback Check Silence", 441, 1, 44100, false);
        temporaryObjects.Add(clip);
        SetField(sound, "audioSource", playerObject.GetComponent<AudioSource>());
        SetField(sound, "playerStats", stats);
        SetField(sound, "hurtClips", new List<AudioClip> { clip });
        SetField(sound, "femaleHurtClips", new List<AudioClip> { clip });

        sound.PlayHurt(7.49f, 50f);
        Require(float.IsNegativeInfinity(GetField<float>(sound, "nextHurtTime")), "Sub-threshold damage played a hurt clip.");
        sound.PlayHurt(7.5f, 50f);
        float nextHurtTime = GetField<float>(sound, "nextHurtTime");
        Require(Mathf.Abs(nextHurtTime - (Time.time + 2f)) < 0.01f,
            $"15% damage did not start the two-second cooldown: next={nextHurtTime}, now={Time.time}, threshold={GetField<float>(sound, "hurtDamageThreshold")}, source={playerObject.GetComponent<AudioSource>()}.");
        sound.PlayHurt(25f, 50f);
        Require(GetField<float>(sound, "nextHurtTime") == nextHurtTime, "Hurt audio ignored its cooldown.");

        SetField(sound, "nextHurtTime", float.NegativeInfinity);
        sound.PlayHurt(3.75f, 25f);
        Require(!float.IsNegativeInfinity(GetField<float>(sound, "nextHurtTime")), "Threshold did not scale with reduced pre-hit HP.");
        SetField(sound, "nextHurtTime", float.NegativeInfinity);
        sound.PlayHurt(0f, 25f);
        sound.PlayHurt(-10f, 25f);
        Require(float.IsNegativeInfinity(GetField<float>(sound, "nextHurtTime")), "Non-damage triggered hurt audio.");

        int damageEvents = 0;
        stats.Damaged += (damage, healthBefore) => damageEvents++;
        SetField(stats, "currentHealth", 50f);
        stats.Heal(10f);
        Require(stats.CurrentHealth > 50f && damageEvents == 0 &&
            float.IsNegativeInfinity(GetField<float>(sound, "nextHurtTime")), "Healing triggered damage feedback.");
    }

    private static void CheckShake(List<UnityEngine.Object> temporaryObjects)
    {
        GameObject portraitObject = new GameObject("Portrait Check", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
        GameObject eyesObject = new GameObject("Eyes Check", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
        temporaryObjects.Add(portraitObject);
        temporaryObjects.Add(eyesObject);
        CharacterPortrait portrait = portraitObject.AddComponent<CharacterPortrait>();
        RectTransform body = portraitObject.GetComponent<RectTransform>();
        RectTransform eyes = eyesObject.GetComponent<RectTransform>();
        body.localPosition = new Vector3(10f, 20f, 0f);
        eyes.localPosition = new Vector3(12f, 22f, 0f);
        GetField<List<RectTransform>>(portrait, "shakeTargets").AddRange(new[] { body, eyes });
        GetField<List<Vector3>>(portrait, "shakeOrigins").AddRange(new[] { body.localPosition, eyes.localPosition });

        IEnumerator smallShake = (IEnumerator)Invoke(portrait, "ShakePortrait", 0.15f);
        smallShake.MoveNext();
        float smallDistance = body.localPosition.x - 10f;
        Require(Mathf.Approximately(smallDistance, eyes.localPosition.x - 12f) && body.localPosition.y == 20f,
            "Paperdoll layers did not move together horizontally.");
        Invoke(portrait, "RestoreShakePositions");
        IEnumerator largeShake = (IEnumerator)Invoke(portrait, "ShakePortrait", 1f);
        largeShake.MoveNext();
        Require(body.localPosition.x - 10f > smallDistance, "Higher damage fraction did not produce a stronger shake.");
        Invoke(portrait, "StopShake");
        Require(body.localPosition == new Vector3(10f, 20f, 0f) && eyes.localPosition == new Vector3(12f, 22f, 0f),
            "Stopping shake did not restore every layer.");
    }

    private static void CheckPanelRouting(List<UnityEngine.Object> temporaryObjects)
    {
        GameObject root = new GameObject("Panel Routing Check") { hideFlags = HideFlags.HideAndDontSave };
        temporaryObjects.Add(root);
        root.SetActive(false);
        PlayerInventory inventory = root.AddComponent<PlayerInventory>();
        PlayerStatsUI stats = root.AddComponent<PlayerStatsUI>();
        PauseMenuUI pause = root.AddComponent<PauseMenuUI>();
        GameObject inventoryPanel = new GameObject("Inventory Check Panel");
        GameObject statsPanel = new GameObject("Stats Check Panel");
        GameObject pausePanel = new GameObject("Pause Check Panel");
        inventoryPanel.transform.SetParent(root.transform);
        statsPanel.transform.SetParent(root.transform);
        pausePanel.transform.SetParent(root.transform);
        SetField(inventory, "inventoryPanel", inventoryPanel);
        SetField(inventory, "statsUI", stats);
        SetField(stats, "statsWindow", statsPanel);
        SetField(stats, "inventory", inventory);
        SetField(stats, "suppressedUIs", Array.Empty<UISuppressor>());
        SetField(pause, "pausePanel", pausePanel);
        SetField(pause, "cachedStatsUI", stats);
        SetField(pause, "cachedInventory", inventory);
        CameraAim camera = UnityEngine.Object.FindFirstObjectByType<CameraAim>(FindObjectsInactive.Include);
        bool previousCameraInput = camera != null && GetField<bool>(camera, "inputEnabled");
        float previousTimeScale = Time.timeScale;
        try
        {
            statsPanel.SetActive(false);
            pausePanel.SetActive(false);
            stats.ToggleStatsWindow();
            Require(!inventory.IsOpen && !stats.IsOpen, "Stats toggle opened stats instead of only closing inventory.");
            stats.ToggleStatsWindow();
            Require(stats.IsOpen && !inventory.IsOpen, "Stats did not open on a subsequent toggle.");
            inventory.ToggleInventory();
            Require(!stats.IsOpen && !inventory.IsOpen, "Inventory toggle opened inventory instead of only closing stats.");
            inventory.ToggleInventory();
            Require(inventory.IsOpen && !stats.IsOpen, "Inventory did not open on a subsequent toggle.");
            Invoke(pause, "HandlePauseInput");
            Require(!inventory.IsOpen && !pausePanel.activeSelf && Time.timeScale == previousTimeScale,
                "Escape paused instead of closing inventory.");
            statsPanel.SetActive(true);
            Invoke(pause, "HandlePauseInput");
            Require(!stats.IsOpen && !pausePanel.activeSelf && Time.timeScale == previousTimeScale,
                "Escape paused instead of closing stats.");
        }
        finally
        {
            if (camera != null)
                camera.SetInputEnabled(previousCameraInput);
            Time.timeScale = previousTimeScale;
        }
    }

    private static void SetField(object target, string name, object value)
    {
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }

    private static T GetField<T>(object target, string name)
    {
        return (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    }

    private static object Invoke(object target, string name, params object[] arguments)
    {
        return target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, arguments);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}