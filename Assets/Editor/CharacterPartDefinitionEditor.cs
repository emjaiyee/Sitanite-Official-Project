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

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, animationProperties);

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

    private static bool TrySliceSpriteSheet(
        DirectionalSpriteAnimation animation,
        string animationPropertyName,
        out string error)
    {
        if (animation.spriteSheet == null)
        {
            error = "Assign an unsliced Sprite Sheet first.";
            return false;
        }

        if (animation.cellSize.x < 1 || animation.cellSize.y < 1)
        {
            error = "Cell Size values must both be at least 1.";
            return false;
        }

        string assetPath = AssetDatabase.GetAssetPath(animation.spriteSheet);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            error = "Sprite Sheet must be a texture asset inside the project.";
            return false;
        }

        int columns = animation.spriteSheet.width / animation.cellSize.x;
        int rows = animation.spriteSheet.height / animation.cellSize.y;
        int expectedFrameCount = 8 * animation.framesPerDirection;
        int frameCount = columns * rows;

        if (animation.spriteSheet.width % animation.cellSize.x != 0 ||
            animation.spriteSheet.height % animation.cellSize.y != 0)
        {
            error = "Cell Size must divide the Sprite Sheet dimensions exactly.";
            return false;
        }

        if (frameCount != expectedFrameCount)
        {
            error = $"The grid contains {frameCount} frames; expected {expectedFrameCount} for 8 directions with {animation.framesPerDirection} frames each.";
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
                    pivot = new Vector2(0.5f, 0.5f),
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