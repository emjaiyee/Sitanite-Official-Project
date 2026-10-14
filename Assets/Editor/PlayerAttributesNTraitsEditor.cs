using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerAttributesNTraits))]
public class PlayerAttributesNTraitsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PlayerAttributesNTraits attributes =
            (PlayerAttributesNTraits)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            "Current Basic Attack Critical Chance",
            $"{attributes.BasicAttackCriticalChance * 100f:0.##}%"
        );
    }
}
