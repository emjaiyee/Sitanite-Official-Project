using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerStats))]
public class PlayerStatsEditor : Editor
{
    private bool showEffectiveResources;
    private bool showEffectiveDamage;
    private bool showEffectiveResistance;
    private bool showEffectiveMovement;
    private bool showEffectiveRegeneration;

    private static readonly string[] runtimeFields =
    {
        "effectiveMaxHealth",
        "effectiveMaxMana",
        "effectiveMaxStamina",
        "effectivePierceDamage",
        "effectiveStabDamage",
        "effectiveSlashDamage",
        "effectiveBluntDamage",
        "effectiveFrostDamage",
        "effectivePoisonDamage",
        "effectiveLightningDamage",
        "effectivePsychicDamage",
        "effectiveNecrosisDamage",
        "effectiveWaterDamage",
        "effectiveEarthDamage",
        "effectiveFireDamage",
        "effectiveAirDamage",
        "effectivePhysicalDamage",
        "effectivePierceResistance",
        "effectiveStabResistance",
        "effectiveSlashResistance",
        "effectiveBluntResistance",
        "effectiveFrostResistance",
        "effectivePoisonResistance",
        "effectiveLightningResistance",
        "effectivePsychicResistance",
        "effectiveNecrosisResistance",
        "effectiveWaterResistance",
        "effectiveEarthResistance",
        "effectiveFireResistance",
        "effectiveAirResistance",
        "effectivePhysicalResistance",
        "effectiveMoveSpeed",
        "effectiveSprintSpeed",
        "effectiveDashSpeed",
        "effectiveHealthRegen",
        "effectiveManaRegen",
        "effectiveStaminaRegen"
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, runtimeFields);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Effective Runtime Values", EditorStyles.boldLabel);
        showEffectiveResources = EditorGUILayout.Foldout(
            showEffectiveResources,
            "Maximum Resources",
            true
        );
        if (showEffectiveResources)
            DrawFields("effectiveMaxHealth", "effectiveMaxMana", "effectiveMaxStamina");

        showEffectiveDamage = EditorGUILayout.Foldout(
            showEffectiveDamage,
            "Damage",
            true
        );
        if (showEffectiveDamage)
            DrawFields(
                "effectivePierceDamage", "effectiveStabDamage", "effectiveSlashDamage",
                "effectiveBluntDamage", "effectiveFrostDamage",
                "effectivePoisonDamage", "effectiveLightningDamage", "effectivePsychicDamage",
                "effectiveNecrosisDamage", "effectiveWaterDamage", "effectiveEarthDamage",
                "effectiveFireDamage", "effectiveAirDamage", "effectivePhysicalDamage"
            );

        showEffectiveResistance = EditorGUILayout.Foldout(
            showEffectiveResistance,
            "Damage Resistance",
            true
        );
        if (showEffectiveResistance)
            DrawFields(
                "effectivePierceResistance", "effectiveStabResistance", "effectiveSlashResistance",
                "effectiveBluntResistance", "effectiveFrostResistance",
                "effectivePoisonResistance", "effectiveLightningResistance", "effectivePsychicResistance",
                "effectiveNecrosisResistance", "effectiveWaterResistance", "effectiveEarthResistance",
                "effectiveFireResistance", "effectiveAirResistance", "effectivePhysicalResistance"
            );

        showEffectiveMovement = EditorGUILayout.Foldout(
            showEffectiveMovement,
            "Movement and Dash Speed",
            true
        );
        if (showEffectiveMovement)
            DrawFields("effectiveMoveSpeed", "effectiveSprintSpeed", "effectiveDashSpeed");

        showEffectiveRegeneration = EditorGUILayout.Foldout(
            showEffectiveRegeneration,
            "Regeneration",
            true
        );
        if (showEffectiveRegeneration)
            DrawFields("effectiveHealthRegen", "effectiveManaRegen", "effectiveStaminaRegen");

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawFields(params string[] propertyNames)
    {
        EditorGUI.indentLevel++;
        foreach (string propertyName in propertyNames)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
                EditorGUILayout.PropertyField(property);
        }
        EditorGUI.indentLevel--;
    }
}
