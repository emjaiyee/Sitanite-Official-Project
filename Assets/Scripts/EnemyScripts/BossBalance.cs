using System;
using System.Collections.Generic;
using UnityEngine;

public static class BossBalance
{
    public sealed class HostileDamage
    {
        public readonly List<DungeonMemory.DamageStat> Hits;
        public readonly int Elevation;
        public LayerMask Layers => LayerMask.GetMask("Player");
        private readonly Dictionary<PlayerStats, float> nextHitTimes = new Dictionary<PlayerStats, float>();

        public HostileDamage(IReadOnlyList<DungeonMemory.DamageStat> hits, int elevation)
        {
            Hits = new List<DungeonMemory.DamageStat>(hits);
            Elevation = elevation;
        }

        public PlayerStats Resolve(Collider2D collider)
        {
            if (collider == null || (Layers.value & (1 << collider.gameObject.layer)) == 0)
                return null;
            PlayerStats player = collider.GetComponentInParent<PlayerStats>();
            return CanHitPlayer(player, Elevation) ? player : null;
        }

        public void Hit(PlayerStats player, float scale = 1f)
        {
            if (!CanHitPlayer(player, Elevation))
                return;
            foreach (DungeonMemory.DamageStat hit in Hits)
            {
                int damage = Mathf.RoundToInt(hit.damage * scale);
                if (hit.type != DamageType.None && damage > 0)
                    player.TakeDamage(damage, hit.type);
            }
        }

        public void HitTargets(Collider2D[] colliders, float interval = 0f, float scale = 1f)
        {
            HashSet<PlayerStats> seen = new HashSet<PlayerStats>();
            foreach (Collider2D collider in colliders)
            {
                PlayerStats player = Resolve(collider);
                if (player == null || !seen.Add(player))
                    continue;
                if (nextHitTimes.TryGetValue(player, out float nextHit) && Time.time < nextHit)
                    continue;
                Hit(player, scale);
                nextHitTimes[player] = Time.time + interval;
            }
        }
    }

    public struct SpawnModifiers
    {
        public float health;
        public float resistance;
        public float damage;
        public int level;
        public float experience;
    }

    public static float Health(float previousHealth, float playerMaxHealth)
    {
        return Mathf.Max(1f, previousHealth + playerMaxHealth * 0.5f);
    }

    public static float AdjustBase(float previousValue, int enemyLevel, int playerLevel)
    {
        return Mathf.Max(0f, previousValue + playerLevel - enemyLevel);
    }

    public static bool CanHitPlayer(PlayerStats player, int elevation)
    {
        return player != null && !player.IsDead &&
            (PlayerElevationLevel.Instance == null || Mathf.Abs(PlayerElevationLevel.Instance.CurrentLevel - elevation) <= 1);
    }

    public static void HitPlayer(PlayerStats player, IReadOnlyList<DungeonMemory.DamageStat> hits, int elevation)
    {
        if (!CanHitPlayer(player, elevation))
            return;

        foreach (DungeonMemory.DamageStat hit in hits)
        {
            if (hit.type != DamageType.None && hit.damage > 0f)
                player.TakeDamage(Mathf.RoundToInt(hit.damage), hit.type);
        }
    }

    public static IEnumerable<DamageType> DamageTypes()
    {
        foreach (DamageType type in Enum.GetValues(typeof(DamageType)))
        {
            int value = (int)type;
            if (value > 0 && (value & (value - 1)) == 0)
                yield return type;
        }
    }

    public static float ApplyHalfGear(float value, List<DungeonMemory.SavedEquipment> equipment, StatType statType, DamageType damageType = DamageType.None)
    {
        float flat = 0f;
        float percent = 0f;
        foreach (DungeonMemory.SavedEquipment item in equipment)
        {
            foreach (EquipmentStat modifier in item.modifiers)
            {
                if (modifier.statType != statType ||
                    (modifier.damageType != DamageType.None && damageType != DamageType.None &&
                     (modifier.damageType & damageType) == 0))
                    continue;

                if (modifier.modifierType == StatModifierType.Percent)
                    percent += modifier.value * 0.5f;
                else
                    flat += modifier.value * 0.5f;
            }
            if (statType != StatType.Damage || item.slot != EquipmentType.Weapon)
                continue;

            foreach (WeaponDamage modifier in item.weaponDamages)
            {
                if (modifier.damageType == DamageType.None || (modifier.damageType & damageType) == 0)
                    continue;

                if (modifier.modifierType == StatModifierType.Percent)
                    percent += modifier.value * 0.5f;
                else
                    flat += modifier.value * 0.5f;
            }
        }
        return Mathf.Max(0f, (value + flat) * (1f + percent / 100f));
    }
}