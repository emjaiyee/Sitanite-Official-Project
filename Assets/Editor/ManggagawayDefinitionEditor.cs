using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ManggagawayDefinition))]
public class ManggagawayDefinitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("framesPerSecond"));
        EditorGUILayout.HelpBox(
            "First form, second form, and Dead: SW, S, SE, E, NE, N, NW, W. " +
            "Transformation has one non-directional frame sequence. " +
            "Slices are read left-to-right, top-to-bottom. Each state can have its own frame count.",
            MessageType.Info);
        ManggagawayDefinition definition = (ManggagawayDefinition)target;
        foreach (ManggagawayAnimationState state in Enum.GetValues(typeof(ManggagawayAnimationState)))
        {
            string propertyName = char.ToLowerInvariant(state.ToString()[0]) + state.ToString().Substring(1);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(propertyName), true);
            if (!GUILayout.Button($"Slice And Assign {ObjectNames.NicifyVariableName(propertyName)}"))
                continue;
            serializedObject.ApplyModifiedProperties();
            if (state == ManggagawayAnimationState.Transformation)
            {
                Undo.RecordObject(definition, "Assign Manggagaway Frames");
                if (!CharacterPartDefinitionEditor.TrySliceSpriteSheet(definition.transformation, propertyName, out string transformationError))
                {
                    Debug.LogError($"{definition.name}: {transformationError}", definition);
                    continue;
                }
                EditorUtility.SetDirty(definition);
                serializedObject.Update();
                continue;
            }
            DirectionalSpriteAnimation animation = definition.GetAnimation(state);
            Undo.RecordObject(definition, "Assign Manggagaway Frames");
            if (!CharacterPartDefinitionEditor.TrySliceSpriteSheet(animation, propertyName, out string error))
            {
                Debug.LogError($"{definition.name}: {error}", definition);
                continue;
            }
            if (!animation.TryAssignSourceFrames(out error))
            {
                Debug.LogError($"{definition.name}: {error}", definition);
                continue;
            }
            EditorUtility.SetDirty(definition);
            serializedObject.Update();
        }
        serializedObject.ApplyModifiedProperties();
    }
}
