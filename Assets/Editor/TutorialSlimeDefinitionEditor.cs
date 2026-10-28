using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TutorialSlimeDefinition))]
public class TutorialSlimeDefinitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("framesPerSecond")
        );
        EditorGUILayout.HelpBox(
            "Each state has eight directions: SW, S, SE, E, NE, N, NW, W. " +
            "Slices are read left-to-right, top-to-bottom. Each state " +
            "can have its own frame count per direction.",
            MessageType.Info
        );

        TutorialSlimeDefinition definition =
            (TutorialSlimeDefinition)target;

        foreach (
            TutorialSlimeAnimationState state
            in Enum.GetValues(typeof(TutorialSlimeAnimationState))
        )
        {
            string propertyName =
                char.ToLowerInvariant(state.ToString()[0]) +
                state.ToString().Substring(1);

            EditorGUILayout.PropertyField(
                serializedObject.FindProperty(propertyName),
                true
            );

            if (
                !GUILayout.Button(
                    $"Slice And Assign " +
                    $"{ObjectNames.NicifyVariableName(propertyName)}"
                )
            )
            {
                continue;
            }

            serializedObject.ApplyModifiedProperties();

            DirectionalSpriteAnimation animation =
                definition.GetAnimation(state);

            Undo.RecordObject(
                definition,
                "Assign Tutorial Slime Frames"
            );

            if (
                !CharacterPartDefinitionEditor.TrySliceSpriteSheet(
                    animation,
                    propertyName,
                    out string error
                )
            )
            {
                Debug.LogError(
                    $"{definition.name}: {error}",
                    definition
                );
                continue;
            }

            if (!animation.TryAssignSourceFrames(out error))
            {
                Debug.LogError(
                    $"{definition.name}: {error}",
                    definition
                );
                continue;
            }

            EditorUtility.SetDirty(definition);
            serializedObject.Update();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
