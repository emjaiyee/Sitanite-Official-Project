using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyHealth))]
public class AdaptiveEnemy : MonoBehaviour
{
    [SerializeField] private DamageType resistantType;
    [SerializeField] private bool swarming;
    [SerializeField] private float damageResistance;
    private RoomInstance room;
    private readonly List<SpriteRenderer> sources = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> overlays = new List<SpriteRenderer>();
    private Color highlight;

    public DamageType ResistantType => resistantType;
    public bool Active => resistantType != DamageType.None;
    public bool Swarming => Active && swarming;
    public float DamageResistance => damageResistance;

    public void Configure(DungeonMemory.Adaptation adaptation, RoomInstance encounterRoom, PlayerStats player)
    {
        EnemyHealth health = GetComponent<EnemyHealth>();
        if (health.IsMiniBoss || health.IsMainBoss || GetComponent<EnemyLich>() != null || GetComponent<FallenDescender>() != null || Active)
            return;

        resistantType = adaptation.damageType;
        swarming = adaptation.swarm;
        room = encounterRoom;
        if (!Active)
            return;

        damageResistance = ResistanceForDamage(player != null ? player.GetEffectiveDamage(resistantType) : 0f, 3f);
        string baseName = string.IsNullOrWhiteSpace(health.EnemyName) ? gameObject.name.Replace("(Clone)", string.Empty).Trim() : health.EnemyName;
        health.SetEnemyName($"{Title(resistantType)} {baseName}");
        Vector3 scale = transform.localScale;
        transform.localScale = new Vector3(Mathf.Sign(scale.x) * 1.25f, Mathf.Sign(scale.y) * 1.25f, scale.z);
        highlight = HighlightColor(resistantType);
        foreach (SpriteRenderer source in GetComponentsInChildren<SpriteRenderer>(true))
        {
            GameObject overlayObject = new GameObject("Adaptation Highlight");
            overlayObject.layer = source.gameObject.layer;
            overlayObject.transform.SetParent(source.transform, false);
            SpriteRenderer overlay = overlayObject.AddComponent<SpriteRenderer>();
            overlay.sharedMaterial = source.sharedMaterial;
            sources.Add(source);
            overlays.Add(overlay);
        }
        SyncHighlights();
    }

    public static float ResistanceForDamage(float incomingDamage, float remainingDamage)
    {
        return Mathf.Max(0f, incomingDamage - remainingDamage);
    }

    public int Mitigate(int incomingDamage, DamageType type)
    {
        if (!Matches(type))
            return incomingDamage;
        int remainder = Random.Range(1, 6);
        damageResistance = ResistanceForDamage(incomingDamage, remainder);
        return Mathf.RoundToInt(Mathf.Max(1f, incomingDamage - damageResistance));
    }

    public static float FairRawDamage(float maxHealth, float playerResistance)
    {
        return Mathf.Max(1f, maxHealth * 0.05f) + playerResistance;
    }

    public static DamageType GetAdaptedDamageType(EnemyHealth attacker)
    {
        if (attacker == null || attacker.IsMiniBoss || attacker.IsMainBoss)
            return DamageType.None;
        AdaptiveEnemy adaptation = attacker.GetComponent<AdaptiveEnemy>();
        return adaptation != null ? adaptation.ResistantType : DamageType.None;
    }

    public bool Matches(DamageType type) => Active && (type & resistantType) != 0;

    public bool DetectsPlayerInRoom()
    {
        if (!Active || !swarming || room == null || Player.Instance == null)
            return false;
        PlayerRoomTracker tracker = Player.Instance.GetComponent<PlayerRoomTracker>();
        PlayerStats player = Player.Instance.GetComponent<PlayerStats>();
        return tracker != null && tracker.CurrentRoom == room && player != null && !player.IsDead;
    }

    private void LateUpdate()
    {
        SyncHighlights();
    }

    private void SyncHighlights()
    {
        for (int index = 0; index < sources.Count; index++)
        {
            SpriteRenderer source = sources[index];
            SpriteRenderer overlay = overlays[index];
            if (source == null || overlay == null)
                continue;
            overlay.enabled = source.enabled;
            overlay.sprite = source.sprite;
            overlay.flipX = source.flipX;
            overlay.flipY = source.flipY;
            overlay.drawMode = source.drawMode;
            overlay.size = source.size;
            overlay.sortingLayerID = source.sortingLayerID;
            overlay.sortingOrder = source.sortingOrder + 1;
            overlay.spriteSortPoint = source.spriteSortPoint;
            overlay.maskInteraction = source.maskInteraction;
            overlay.color = new Color(highlight.r, highlight.g, highlight.b, source.color.a * 0.4f);
        }
    }

    public static Color HighlightColor(DamageType type)
    {
        if ((type & (DamageType.Physical | DamageType.Slash | DamageType.Stab | DamageType.Blunt | DamageType.Pierce)) != 0)
            return new Color(0.8f, 0.8f, 0.82f);
        return DamagePopupSpawner.GetColor(type);
    }

    public static string Title(DamageType type) => type switch
    {
        DamageType.Pierce or DamageType.Slash or DamageType.Blunt or DamageType.Stab or DamageType.Physical => "Thick Skinned",
        DamageType.Frost => "Icy",
        DamageType.Poison => "Venomous",
        DamageType.Lightning => "Conduit",
        DamageType.Psychic => "Clairvoyant",
        DamageType.Necrosis => "Rotting",
        DamageType.Water => "Dripping",
        DamageType.Earth => "Stone-scaled",
        DamageType.Fire => "Burnt",
        DamageType.Air => "Aero",
        _ => string.Empty
    };
}