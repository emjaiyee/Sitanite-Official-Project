using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

[CustomEditor(typeof(CharacterPartDefinition), true)]
public class CharacterPartDefinitionEditor : Editor
{
    private static readonly string[] animationProperties =
    {
        "idleAnimation",
        "walkAnimation",
        "runningAnimation",
        "meleeAnimation",
        "castAnimation",
        "rangedAnimation",
        "dashAnimation",
        "deathAnimation"
    };

    private static readonly string[] excludedProperties =
    {
        "idleAnimation", "walkAnimation", "runningAnimation", "meleeAnimation",
        "castAnimation", "rangedAnimation", "dashAnimation", "deathAnimation",
        "slightlyDamagedPortrait", "injuredPortrait", "gravelyInjuredPortrait", "deadPortrait"
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, excludedProperties);

        CharacterPartDefinition definition = (CharacterPartDefinition)target;
        if (definition.Type == CharacterPartType.Body || definition.Type == CharacterPartType.Eyes)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("slightlyDamagedPortrait"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("injuredPortrait"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("gravelyInjuredPortrait"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("deadPortrait"));
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Animation Frames", EditorStyles.boldLabel);

        foreach (string propertyName in animationProperties)
            DrawAnimationProperty(propertyName);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawAnimationProperty(string propertyName)
    {
        SerializedProperty animationProperty = serializedObject.FindProperty(propertyName);
        if (animationProperty == null)
            return;

        EditorGUILayout.PropertyField(animationProperty, true);

        if (GUILayout.Button($"Slice And Assign {ObjectNames.NicifyVariableName(propertyName)}"))
        {
            serializedObject.ApplyModifiedProperties();

            foreach (Object targetObject in targets)
            {
                CharacterPartDefinition definition = targetObject as CharacterPartDefinition;
                DirectionalSpriteAnimation animation = GetAnimation(definition, propertyName);

                if (animation == null)
                    continue;

                Undo.RecordObject(definition, "Assign Animation Direction Frames");
                if (!TrySliceSpriteSheet(animation, propertyName, out string sliceError))
                {
                    Debug.LogWarning(
                        $"{definition.name} {ObjectNames.NicifyVariableName(propertyName)}: {sliceError}",
                        definition
                    );
                    continue;
                }

                if (!animation.TryAssignSourceFrames(out string error))
                {
                    Debug.LogWarning(
                        $"{definition.name} {ObjectNames.NicifyVariableName(propertyName)}: {error}",
                        definition
                    );
                    continue;
                }

                EditorUtility.SetDirty(definition);
            }

            serializedObject.Update();
        }
    }

    public static bool TrySliceSpriteSheet(
        DirectionalSpriteAnimation animation,
        string animationPropertyName,
        out string error,
        int directionCount = 8)
    {
        if (animation.spriteSheet == null)
        {
            error = "Assign an unsliced Sprite Sheet first.";
            return false;
        }

        string assetPath = AssetDatabase.GetAssetPath(animation.spriteSheet);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            error = "Sprite Sheet must be a texture asset inside the project.";
            return false;
        }

        int expectedFrameCount = directionCount * animation.framesPerDirection;
        if (importer.spriteImportMode == SpriteImportMode.Multiple)
        {
            Sprite[] existingFrames = LoadExistingSprites(assetPath);
            if (existingFrames.Length > 0)
            {
                if (existingFrames.Length != expectedFrameCount)
                {
                    error = $"The sheet has {existingFrames.Length} existing slices; expected {expectedFrameCount} for {directionCount} directions with {animation.framesPerDirection} frames each.";
                    return false;
                }

                animation.sourceFrames = existingFrames;
                error = null;
                return true;
            }
        }

        if (animation.cellSize.x < 1 || animation.cellSize.y < 1)
        {
            error = "Cell Size values must both be at least 1.";
            return false;
        }

        if (animation.pivot.x < 0f || animation.pivot.x > 1f ||
            animation.pivot.y < 0f || animation.pivot.y > 1f)
        {
            error = "Pivot values must be between 0 and 1.";
            return false;
        }

        int columns = animation.spriteSheet.width / animation.cellSize.x;
        int rows = animation.spriteSheet.height / animation.cellSize.y;
        int frameCount = columns * rows;

        if (animation.spriteSheet.width % animation.cellSize.x != 0 ||
            animation.spriteSheet.height % animation.cellSize.y != 0)
        {
            error = "Cell Size must divide the Sprite Sheet dimensions exactly.";
            return false;
        }

        if (frameCount != expectedFrameCount)
        {
            error = $"The grid contains {frameCount} frames; expected {expectedFrameCount} for {directionCount} directions with {animation.framesPerDirection} frames each.";
            return false;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();

        SpriteRect[] spriteRects = new SpriteRect[frameCount];
        string spriteNamePrefix = $"{animation.spriteSheet.name}_{animationPropertyName}";

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                int frameIndex = row * columns + column;
                spriteRects[frameIndex] = new SpriteRect
                {
                    name = $"{spriteNamePrefix}_{frameIndex:D3}",
                    rect = new Rect(
                        column * animation.cellSize.x,
                        (rows - row - 1) * animation.cellSize.y,
                        animation.cellSize.x,
                        animation.cellSize.y
                    ),
                    alignment = SpriteAlignment.Center,
                    pivot = animation.pivot,
                    spriteID = GUID.Generate()
                };
            }
        }

        dataProvider.SetSpriteRects(spriteRects);
        dataProvider.Apply();
        importer.SaveAndReimport();

        animation.sourceFrames = LoadSlicedSprites(assetPath, spriteNamePrefix, frameCount);
        if (animation.sourceFrames.Length != frameCount)
        {
            error = "Unity did not create all requested sprite slices.";
            return false;
        }

        error = null;
        return true;
    }

    public static bool TrySliceSpriteSheet(
        SpriteAnimation animation,
        string animationPropertyName,
        out string error)
    {
        if (animation == null)
        {
            error = "Animation is not assigned.";
            return false;
        }

        DirectionalSpriteAnimation sliceData = new DirectionalSpriteAnimation
        {
            spriteSheet = animation.spriteSheet,
            cellSize = animation.cellSize,
            pivot = animation.pivot,
            framesPerDirection = animation.frameCount
        };
        if (!TrySliceSpriteSheet(sliceData, animationPropertyName, out error, 1))
            return false;

        animation.frames = sliceData.sourceFrames;
        return true;
    }

    private static Sprite[] LoadSlicedSprites(
        string assetPath,
        string spriteNamePrefix,
        int frameCount)
    {
        Sprite[] sourceFrames = new Sprite[frameCount];
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

        foreach (Object asset in assets)
        {
            Sprite sprite = asset as Sprite;
            if (sprite == null || !sprite.name.StartsWith(spriteNamePrefix))
                continue;

            string indexText = sprite.name.Substring(spriteNamePrefix.Length + 1);
            if (int.TryParse(indexText, out int index) &&
                index >= 0 && index < sourceFrames.Length)
            {
                sourceFrames[index] = sprite;
            }
        }

        return sourceFrames;
    }

    private static Sprite[] LoadExistingSprites(string assetPath)
    {
        List<Sprite> sprites = new List<Sprite>();
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
        {
            if (asset is Sprite sprite)
                sprites.Add(sprite);
        }

        sprites.Sort((left, right) =>
        {
            int row = right.rect.y.CompareTo(left.rect.y);
            return row != 0 ? row : left.rect.x.CompareTo(right.rect.x);
        });
        return sprites.ToArray();
    }

    private static DirectionalSpriteAnimation GetAnimation(
        CharacterPartDefinition definition,
        string propertyName)
    {
        if (definition == null)
            return null;

        return propertyName switch
        {
            "idleAnimation" => definition.idleAnimation,
            "walkAnimation" => definition.walkAnimation,
            "runningAnimation" => definition.runningAnimation,
            "meleeAnimation" => definition.meleeAnimation,
            "castAnimation" => definition.castAnimation,
            "rangedAnimation" => definition.rangedAnimation,
            "dashAnimation" => definition.dashAnimation,
            "deathAnimation" => definition.deathAnimation,
            _ => null
        };
    }
}