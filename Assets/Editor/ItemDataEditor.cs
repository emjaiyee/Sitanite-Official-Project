using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemData))]
public class ItemDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSection("Display Settings", "itemName", "itemDescription", "inventoryIcon", "lootIcon", "equipmentIcon");
        DrawSection("Classification", "equipmentType", "rarity", "characterDefinition");
        DrawSection("Stat Cap", "statCapType", "statCapAttribute", "statCapTrait", "statCapValue");

        EquipmentType equipmentType = (EquipmentType)serializedObject
            .FindProperty("equipmentType").enumValueIndex;

        if (equipmentType == EquipmentType.Weapon)
            DrawSection("Weapon Stats", "weaponStats");
        else if (equipmentType != EquipmentType.None)
            DrawSection("Stat Modifiers", "statModifiers");

        DrawSection("Item Grid Layout", "gridSize", "gridWidth", "gridHeight");
        DrawSection("Stacking Configuration", "isStackable", "maxStackSize");

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSection(string title, params string[] propertyNames)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        foreach (string propertyName in propertyNames)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
                EditorGUILayout.PropertyField(property, true);
        }
    }
}