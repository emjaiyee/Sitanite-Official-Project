using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

[InitializeOnLoad]
public static class DungeonMemoryChecks
{
    private static readonly string RequestPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/DungeonMemoryChecks.request"));
    private static readonly string ResultPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/DungeonMemoryChecks.result"));

    static DungeonMemoryChecks()
    {
        if (File.Exists(RequestPath))
            EditorApplication.delayCall += RunRequestedChecks;
    }

    private static void RunRequestedChecks()
    {
        if (!File.Exists(RequestPath))
            return;

        File.Delete(RequestPath);
        Run();
    }

    [MenuItem("Tools/Dungeon Memory/Run Checks")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            File.WriteAllText(ResultPath, "BLOCKED: Exit Play Mode and run Tools > Dungeon Memory > Run Checks.");
            return;
        }

        Random.State previousRandomState = Random.state;
        try
        {
            CheckCatalogRefresh();
            CheckArchive();
            CheckLootSampling();
            CheckStartingWeapon();
            CheckFallenLootPool();
            CheckRunCounters();
            CheckBossBalance();
            CheckFallenConfiguration();
            CheckFallenLevelXP();
            CheckDeathNamePresentation();
            CheckBossNames();
            CheckEnemyWeaponSkills();
            CheckReanimatedFloorRolls();
            CheckRememberedFallenPrefab();
            CheckDelayedWaveClear();
            CheckSlam();
            CheckWalkablePlacement();
            const string result = "PASS: Hair-only catalog; archive/snapshot JSON and eviction; seeded loot sampling and equipment-pool repair; class starter fallback without invented loot; incomplete legacy selection; boss balance and XP; hostile skill cooldowns, targeting, damage and charge cancellation; Player-only projectile/effect masks; spawn physics/paperdoll ordering and delayed wave clear; visible assigned corpse prefab and sparse A* placement; authored slam.";
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
            Random.state = previousRandomState;
        }
    }

    [MenuItem("Tools/Dungeon Memory/Run Name Checks")]
    public static void RunNameChecks()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play Mode before running name checks.");
            return;
        }

        try
        {
            CheckFallenConfiguration();
            CheckDeathNamePresentation();
            CheckBossNames();
            Debug.Log("PASS: Death name row and reveal order; mini-boss name and final level; Fallen name and level modifier; regular, dead and unbound labels cleared.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static void CheckCatalogRefresh()
    {
        GameObject root = TemporaryObject("Catalog Refresh Check", false);
        try
        {
            DungeonMemory memory = root.AddComponent<DungeonMemory>();
            Invoke(memory, "RefreshItemCatalog");
            int expectedHairCount = 0;
            foreach (string id in AssetDatabase.FindAssets("t:CharacterPartDefinition", new[] { "Assets" }))
            {
                CharacterPartDefinition part = AssetDatabase.LoadAssetAtPath<CharacterPartDefinition>(AssetDatabase.GUIDToAssetPath(id));
                if (part == null)
                    continue;

                if (part.Type == CharacterPartType.Hair)
                {
                    expectedHairCount++;
                    Require(memory.ResolveHair(id) == part, "Hair definition was missing from the refreshed catalog.");
                }
                else
                {
                    Require(memory.ResolveHair(id) == null, "Non-hair definition was included in Hair Catalog.");
                }
            }

            int expectedItemCount = 0;
            foreach (string id in AssetDatabase.FindAssets("t:ItemData", new[] { "Assets" }))
            {
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(id));
                if (item == null)
                    continue;

                expectedItemCount++;
                Require(memory.ResolveItem(id) == item, "Item Catalog changed while filtering Hair Catalog.");
            }

            SerializedObject serialized = new SerializedObject(memory);
            Require(serialized.FindProperty("hairCatalog").arraySize == expectedHairCount &&
                serialized.FindProperty("itemCatalog").arraySize == expectedItemCount, "Refreshed catalogs contained unexpected or duplicate entries.");
            Debug.Log($"DungeonMemory catalog check: {expectedItemCount} items, {expectedHairCount} hair definitions.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void CheckArchive()
    {
        DungeonMemory.Archive archive = new DungeonMemory.Archive();
        for (int index = 0; index < 6; index++)
        {
            archive.Remember(new DungeonMemory.SavedDeath
            {
                id = index.ToString(), characterName = "Name \"with quotes\"",
                floor = index + 1, gender = CharacterGender.Female,
                outcome = DungeonMemory.DeathOutcome.Reanimated,
                loot = new List<DungeonMemory.SavedLoot>
                {
                    new DungeonMemory.SavedLoot { itemId = "stable-asset-id", quantity = 3 }
                },
                carriedLoot = new List<DungeonMemory.SavedLoot> { new DungeonMemory.SavedLoot { itemId = "stable-asset-id", quantity = 10 } },
                combat = new DungeonMemory.CombatSnapshot
                {
                    health = 80f, sprintSpeed = 2f, level = 5, hairId = "hair-id", hideHeadwear = true,
                    stats = new List<DungeonMemory.DamageStat> { new DungeonMemory.DamageStat { type = DamageType.Slash, damage = 20f, resistance = 10f } }
                }
            });
        }
        Require(archive.deaths.Count == 5 && archive.deaths[0].id == "1" && archive.deaths[4].id == "5", "Oldest death eviction failed.");
        DungeonMemory.Archive restored = JsonUtility.FromJson<DungeonMemory.Archive>(JsonUtility.ToJson(archive));
        Require(restored.deaths.Count == 5 && restored.deaths[0].floor == 2 &&
            restored.deaths[0].gender == CharacterGender.Female &&
            restored.deaths[0].outcome == DungeonMemory.DeathOutcome.Reanimated &&
            restored.deaths[0].loot[0].itemId == "stable-asset-id" &&
            restored.deaths[0].loot[0].quantity == 3 && restored.deaths[0].characterName == "Name \"with quotes\"", "Archive JSON round-trip failed.");
        Require(restored.deaths[0].carriedLoot[0].quantity == 10 && restored.deaths[0].combat.health == 80f &&
            restored.deaths[0].combat.hideHeadwear && restored.deaths[0].combat.hairId == "hair-id" &&
            restored.deaths[0].combat.stats[0].damage == 20f, "Combat/full-loot snapshot was lost in JSON.");
    }

    private static void CheckLootSampling()
    {
        GameObject root = TemporaryObject("Memory Sampling", false);
        ItemData first = ScriptableObject.CreateInstance<ItemData>();
        ItemData second = ScriptableObject.CreateInstance<ItemData>();
        try
        {
            DungeonMemory memory = root.AddComponent<DungeonMemory>();
            SerializedObject serialized = new SerializedObject(memory);
            SerializedProperty catalog = serialized.FindProperty("itemCatalog");
            catalog.arraySize = 2;
            catalog.GetArrayElementAtIndex(0).FindPropertyRelative("id").stringValue = "first";
            catalog.GetArrayElementAtIndex(0).FindPropertyRelative("item").objectReferenceValue = first;
            catalog.GetArrayElementAtIndex(1).FindPropertyRelative("id").stringValue = "second";
            catalog.GetArrayElementAtIndex(1).FindPropertyRelative("item").objectReferenceValue = second;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            InventoryItem backpack = new InventoryItem(first, 10);
            InventoryItem equipment = new InventoryItem(second, 10);
            for (int seed = 0; seed < 128; seed++)
            {
                Random.InitState(seed);
                List<DungeonMemory.SavedLoot> selected = memory.CaptureLoot(new[] { backpack, equipment, backpack, null });
                int quantity = 0;
                foreach (DungeonMemory.SavedLoot loot in selected)
                {
                    Require((loot.itemId == "first" || loot.itemId == "second") && loot.quantity > 0 && loot.quantity <= 10, "Loot stack exceeded available quantity.");
                    quantity += loot.quantity;
                }
                Require(quantity >= 5 && quantity <= 10, "Loot must be 25-50% of 20 units, without double-counting held items.");
                Require(backpack.Quantity == 10 && equipment.Quantity == 10, "Sampling mutated carried items.");
                int fallenQuantity = 0;
                foreach (DungeonMemory.SavedLoot loot in memory.CaptureLoot(new[] { backpack, equipment }, 0.5f, 0.75f))
                    fallenQuantity += loot.quantity;
                Require(fallenQuantity >= 10 && fallenQuantity <= 15, "Fallen loot must be 50-75% of all 20 units.");
            }
            Require(memory.CaptureLoot(new[] { backpack, equipment }, 1f, 1f).TrueForAll(item => item.quantity == 10), "Full carried inventory was not preserved.");
            Require(memory.CaptureLoot(Array.Empty<InventoryItem>()).Count == 0, "Empty inventory sample failed.");
            Require(memory.CaptureLoot(new[] { new InventoryItem(first) })[0].quantity == 1, "Single-item rounding failed.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(first);
            UnityEngine.Object.DestroyImmediate(second);
        }
    }

    private static void CheckStartingWeapon()
    {
        GameObject root = TemporaryObject("Starting Weapon Check", false);
        ItemData data = ScriptableObject.CreateInstance<ItemData>();
        EquipmentManager previous = EquipmentManager.Instance;
        try
        {
            Player player = root.AddComponent<Player>();
            SetField(data, "equipmentType", EquipmentType.Weapon);
            PlayerEquipment weapon = root.AddComponent<PlayerEquipment>();
            typeof(PlayerEquipment).GetProperty("CurrentWeaponData").GetSetMethod(true).Invoke(weapon, new object[] { data });
            EquipmentManager equipment = root.AddComponent<EquipmentManager>();
            typeof(EquipmentManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { equipment });
            DungeonMemory memory = root.AddComponent<DungeonMemory>();
            List<InventoryItem> items = (List<InventoryItem>)Invoke(memory, "GetCarriedItems", player);
            Require(items.FindAll(item => item != null && item.Data == data).Count == 1, "Starting weapon was omitted from carried loot.");

            InventoryItem registered = new InventoryItem(data);
            Dictionary<EquipmentType, InventoryItem> slots = (Dictionary<EquipmentType, InventoryItem>)typeof(EquipmentManager)
                .GetField("currentEquipment", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(equipment);
            slots[EquipmentType.Weapon] = registered;
            items = (List<InventoryItem>)Invoke(memory, "GetCarriedItems", player);
            Require(items.FindAll(item => item != null && item.Data == data).Count == 1 && items.Contains(registered), "Registered weapon was duplicated by the fallback.");

            slots.Clear();
            typeof(PlayerEquipment).GetProperty("CurrentWeaponData").GetSetMethod(true).Invoke(weapon, new object[] { null });
            PlayerStats stats = root.AddComponent<PlayerStats>();
            SetField(stats, "playerClass", PlayerClass.Mage);
            CharacterCustomizationController customization = root.AddComponent<CharacterCustomizationController>();
            SetField(customization, "mageStartingGear", new[] { data });
            SetTestCatalog(memory, data);
            DungeonMemory.CombatSnapshot snapshot = (DungeonMemory.CombatSnapshot)Invoke(memory, "CaptureCombat", player, stats);
            Require(snapshot != null && snapshot.playerClass == PlayerClass.Mage && snapshot.equipment.Count == 1 &&
                snapshot.equipment[0].itemId == "weapon" && snapshot.equipment[0].fallbackWeapon,
                "Unarmed death did not snapshot its class starter weapon.");
            items = (List<InventoryItem>)Invoke(memory, "GetCarriedItems", player);
            Require(items.TrueForAll(item => item == null), "A fallback starter weapon was invented as carried loot.");
        }
        finally
        {
            typeof(EquipmentManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { previous });
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(data);
        }
    }

    private static void CheckFallenLootPool()
    {
        GameObject root = TemporaryObject("Fallen Equipment Loot Check", false);
        try
        {
            DungeonMemory memory = root.AddComponent<DungeonMemory>();
            DungeonMemory.SavedDeath death = TestReanimatedDeath();
            death.carriedLoot.Add(new DungeonMemory.SavedLoot { itemId = "inventory", quantity = 10 });
            death.combat.equipment.Add(new DungeonMemory.SavedEquipment { itemId = "armour", slot = EquipmentType.Chestplate });
            death.combat.equipment.Add(new DungeonMemory.SavedEquipment { itemId = "starter", slot = EquipmentType.Weapon, fallbackWeapon = true });
            List<DungeonMemory.SavedLoot> pool = memory.GetFallenLootPool(death);
            Require(pool.Count == 3 && pool.Find(item => item.itemId == "weapon")?.quantity == 1 &&
                pool.Find(item => item.itemId == "armour")?.quantity == 1 && !pool.Exists(item => item.itemId == "starter"),
                "Equipment was omitted from the Fallen loot pool or fallback gear invented loot.");
            Require(death.carriedLoot.Count == 1 && death.carriedLoot[0].quantity == 10, "Loot-pool repair mutated the saved inventory.");
            death.carriedLoot.Add(new DungeonMemory.SavedLoot { itemId = "weapon", quantity = 2 });
            death.carriedLoot.Add(new DungeonMemory.SavedLoot { itemId = "armour", quantity = 1 });
            pool = memory.GetFallenLootPool(death);
            Require(pool.Count == 3 && pool.Find(item => item.itemId == "weapon").quantity == 2 &&
                pool.Find(item => item.itemId == "armour").quantity == 1, "Already captured equipment was duplicated in loot.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void CheckRunCounters()
    {
        GameObject root = TemporaryObject("Run Counter Check", false);
        try
        {
            Player player = root.AddComponent<Player>();
            RoomInstance room = root.AddComponent<RoomInstance>();
            player.RecordEnemyDefeated(false);
            player.RecordEnemyDefeated(false);
            player.RecordEnemyDefeated(true);
            player.RecordRoomCleared(room);
            player.RecordRoomCleared(room);
            player.RecordFloorEntered(1);
            player.RecordFloorEntered(1);
            player.RecordFloorEntered(2);
            Require(player.TryRollDungeonMemory("body") && !player.TryRollDungeonMemory("body"), "Memory rolled more than once in a run.");
            Require(player.TryRollDungeonMemory("body", 2) && !player.TryRollDungeonMemory("body", 2) && player.TryRollDungeonMemory("body", 3), "Later floors did not receive independent rolls.");
            player.MarkDungeonMemoryEncountered("body");
            Require(!player.TryRollDungeonMemory("body", 4), "An encountered memory spawned again on a later floor.");
            Player.RunStatistics stats = player.FinishRun();
            Require(stats.kills == 2 && stats.miniBossKills == 1 && stats.roomsCleared == 1 && stats.floorsEntered == 2, "Run counters failed.");
            player.RecordEnemyDefeated(false);
            player.RecordFloorEntered(3);
            Require(player.FinishRun().kills == 2 && player.FinishRun().floorsEntered == 2 && !player.TryRollDungeonMemory("new"), "Finished run was not frozen.");
            UnityEngine.Object.DestroyImmediate(player);
            Player next = root.AddComponent<Player>();
            Require(next.TryRollDungeonMemory("body") && next.FinishRun().kills == 0, "New character did not reset run state.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void CheckBossBalance()
    {
        Require(BossBalance.Health(80f, 100f) == 130f, "Boss health formula failed.");
        Require(BossBalance.AdjustBase(20f, 5, 3) == 18f && BossBalance.AdjustBase(20f, 3, 5) == 22f &&
            BossBalance.AdjustBase(1f, 10, 1) == 0f, "Signed level-gap formula or clamp failed.");
        List<DungeonMemory.SavedEquipment> gear = new List<DungeonMemory.SavedEquipment>
        {
            new DungeonMemory.SavedEquipment
            {
                slot = EquipmentType.Weapon,
                modifiers = new List<EquipmentStat> { new EquipmentStat { statType = StatType.Damage, modifierType = StatModifierType.Percent, value = 20f } },
                weaponDamages = new List<WeaponDamage> { new WeaponDamage { damageType = DamageType.Slash, value = 10f } }
            }
        };
        Require(Mathf.Approximately(BossBalance.ApplyHalfGear(10f, gear, StatType.Damage, DamageType.Slash), 16.5f), "Flat/percent weapon modifiers were not halved.");
    }

    private static void CheckFallenConfiguration()
    {
        GameObject root = TemporaryObject("Fallen Configuration Check", false);
        ItemData weapon = ScriptableObject.CreateInstance<ItemData>();
        CharacterPartDefinition part = ScriptableObject.CreateInstance<CharacterPartDefinition>();
        EquipmentManager previousEquipment = EquipmentManager.Instance;
        GameObject actor = null;
        try
        {
            typeof(EquipmentManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { null });
            SerializedObject serializedWeapon = new SerializedObject(weapon);
            serializedWeapon.FindProperty("equipmentType").enumValueIndex = (int)EquipmentType.Weapon;
            serializedWeapon.FindProperty("weaponStats").FindPropertyRelative("attack").FindPropertyRelative("range").floatValue = 2f;
            serializedWeapon.ApplyModifiedPropertiesWithoutUndo();
            DungeonMemory memory = root.AddComponent<DungeonMemory>();
            SetTestCatalog(memory, weapon);
            PlayerStats player = root.AddComponent<PlayerStats>();
            player.maxHealth = 100f;
            SetField(player, "level", 3);
            actor = TemporaryObject("Fallen Actor", false);
            FallenDescenderRenderer renderer = actor.AddComponent<FallenDescenderRenderer>();
            SpriteRenderer spriteRenderer = actor.AddComponent<SpriteRenderer>();
            SetField(renderer, "maleBody", part);
            SetField(renderer, "maleEyes", part);
            SetField(renderer, "bodyRenderer", spriteRenderer);
            SetField(renderer, "eyesRenderer", spriteRenderer);
            FallenDescender fallen = actor.AddComponent<FallenDescender>();
            Invoke(fallen, "Awake");
            DungeonMemory.SavedDeath death = TestReanimatedDeath();
            death.characterName = "Arlen";
            death.combat.stats.Add(new DungeonMemory.DamageStat { type = DamageType.Fire, damage = 200f, resistance = 0f });
            Require(fallen.Configure(death, memory, null, player, new BossBalance.SpawnModifiers { damage = 3f, resistance = 2f, health = 7f, level = 1 }), "Valid Fallen character could not be configured.");
            EnemyHealth health = actor.GetComponent<EnemyHealth>();
            Require(health.MaxHealth == 137 && fallen.Level == 6 && death.combat.level == 5 && health.GetDamageResistance(DamageType.Slash) == 10f, "Fallen level modifier mutated its saved level or scaled combat stats.");
            Require(health.IsMiniBoss && health.EnemyName == "Fallen Descender Arlen", "Fallen mini-boss did not replace its name with the remembered character name.");
            WeaponController skills = actor.GetComponent<WeaponController>();
            Require(skills != null && Mathf.Approximately(skills.AttackCooldownSeconds, Mathf.Max(0.05f, weapon.AttackCooldown * 1.5f)) &&
                Mathf.Approximately(skills.SkillCooldownSeconds, Mathf.Max(0.05f, weapon.SkillCooldown * 2.5f)), "Fallen weapon cooldown multipliers failed.");
            FallenDescenderLevelXP experience = actor.GetComponent<FallenDescenderLevelXP>();
            Require(experience != null && experience.Level == 6 && experience.ExperienceReward == 110f &&
                actor.GetComponent<EnemyLevelXP>() == null, "Fallen configuration used regular scaling XP instead of the saved-level reward.");
            List<DungeonMemory.DamageStat> hits = (List<DungeonMemory.DamageStat>)typeof(FallenDescender)
                .GetField("attackHits", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(fallen);
            Require(hits.Count == 1 && hits[0].type == DamageType.Slash && hits[0].damage == 25f, "Unused base damage types leaked into weapon attacks.");
            health.ApplyLevelScaling(50);
            Require(health.MaxHealth == 137, "Normal enemy scaling overwrote Fallen health.");

            Rigidbody2D body = actor.GetComponent<Rigidbody2D>();
            FieldInfo animationState = typeof(FallenDescenderRenderer).GetField("state", BindingFlags.NonPublic | BindingFlags.Instance);
            Invoke(fallen, "ChangeState", FallenDescender.BrainState.Chase);
            Invoke(fallen, "UpdateMovementAnimation");
            Require((CharacterAnimationState)animationState.GetValue(renderer) == CharacterAnimationState.Idle, "Stationary Fallen chase played Running.");
            body.position += Vector2.right * 0.1f;
            Invoke(fallen, "UpdateMovementAnimation");
            Require((CharacterAnimationState)animationState.GetValue(renderer) == CharacterAnimationState.Running, "Moving Fallen did not play Running.");
            Invoke(fallen, "UpdateMovementAnimation");
            Require((CharacterAnimationState)animationState.GetValue(renderer) == CharacterAnimationState.Idle, "Stopped Fallen did not return to Idle.");
            Invoke(fallen, "ChangeState", FallenDescender.BrainState.Attack);
            CharacterAnimationState attackAnimation = (CharacterAnimationState)animationState.GetValue(renderer);
            body.position += Vector2.right * 0.1f;
            Invoke(fallen, "UpdateMovementAnimation");
            Require((CharacterAnimationState)animationState.GetValue(renderer) == attackAnimation, "Movement interrupted the Fallen attack animation.");
            Invoke(fallen, "ChangeState", FallenDescender.BrainState.Skill);
            CharacterAnimationState skillAnimation = (CharacterAnimationState)animationState.GetValue(renderer);
            Invoke(fallen, "UpdateMovementAnimation");
            Require((CharacterAnimationState)animationState.GetValue(renderer) == skillAnimation, "Stationary Fallen interrupted its skill animation.");
            Invoke(fallen, "ChangeState", FallenDescender.BrainState.Recover);
            Invoke(fallen, "UpdateMovementAnimation");
            Require((CharacterAnimationState)animationState.GetValue(renderer) == CharacterAnimationState.Idle, "Finished Fallen attack did not return to Idle.");

            BaseArrow projectile = actor.AddComponent<BaseArrow>();
            BoxCollider2D collider = actor.AddComponent<BoxCollider2D>();
            SetField(projectile, "enemyHits", hits);
            Invoke(projectile, "OnTriggerEnter2D", collider);
            Require(health.CurrentHealth == 137, "Hostile projectile damaged an enemy instead of the player.");
        }
        finally
        {
            typeof(EquipmentManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { previousEquipment });
            if (actor != null)
                UnityEngine.Object.DestroyImmediate(actor);
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(weapon);
            UnityEngine.Object.DestroyImmediate(part);
        }
    }

    private static void CheckFallenLevelXP()
    {
        GameObject root = TemporaryObject("Fallen Level XP Check", false);
        GameObject playerObject = TemporaryObject("Fallen XP Recipient", false);
        Player previousPlayer = Player.Instance;
        try
        {
            root.AddComponent<FallenDescender>();
            EnemyHealth health = root.GetComponent<EnemyHealth>();
            SetField(health, "baseSlashResistance", 7f);
            health.Init(80);
            FallenDescenderLevelXP experience = root.GetComponent<FallenDescenderLevelXP>();
            Invoke(experience, "Awake");
            Invoke(experience, "OnEnable");
            Invoke(experience, "OnEnable");

            int[] levels = { 1, 2, 3, 5, 0 };
            float[] rewards = { 10f, 30f, 50f, 90f, 10f };
            for (int index = 0; index < levels.Length; index++)
            {
                experience.SetLevel(levels[index]);
                Require(experience.Level == Mathf.Max(1, levels[index]) && experience.ExperienceReward == rewards[index], "Fallen XP curve or minimum-level clamp failed.");
                Require(health.MaxHealth == 80 && health.CurrentHealth == 80 && health.GetDamageResistance(DamageType.Slash) == 7f, "Fallen level tracking modified combat stats.");
            }
            experience.SetLevel(2);
            experience.SetExperienceReward(0f);
            Require(experience.ExperienceReward == 20f, "Level 2 did not add 20 EXP to a zero base reward.");
            experience.SetExperienceReward(10f);
            experience.AddExperienceReward(5f);
            Require(experience.ExperienceReward == 35f, "Spawner XP bonus changed the linear per-level increment.");
            experience.SetExperienceReward(10f);

            EnemyLevelXP legacy = root.AddComponent<EnemyLevelXP>();
            Invoke(legacy, "Awake");
            Invoke(legacy, "OnEnable");
            legacy.SetLevel(50);
            Require(health.MaxHealth == 80 && health.CurrentHealth == 80 && health.GetDamageResistance(DamageType.Slash) == 7f, "Legacy regular XP scaled a Fallen Descender.");

            Player player = playerObject.AddComponent<Player>();
            PlayerStats stats = playerObject.AddComponent<PlayerStats>();
            typeof(Player).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { player });
            Action<GameObject> handlers = (Action<GameObject>)typeof(EnemyHealth)
                .GetField("OnEnemyDied", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(health);
            SetField(experience, "grantExperienceOnDeath", false);
            handlers?.Invoke(root);
            Require(stats.ExperiencePoints == 0f, "Disabled Fallen XP reward or legacy component still awarded experience.");
            SetField(experience, "grantExperienceOnDeath", true);
            handlers?.Invoke(root);
            handlers?.Invoke(root);
            Require(stats.ExperiencePoints == 30f, "Fallen death reward was missing, duplicated or used exponential XP.");
            Invoke(experience, "OnDisable");
            Invoke(legacy, "OnDisable");
        }
        finally
        {
            typeof(Player).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { previousPlayer });
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(playerObject);
        }
    }

    private static void CheckDeathNamePresentation()
    {
        GameObject root = TemporaryObject("Death Name Check", false);
        GameObject panelObject = TemporaryObject("Results Panel", false);
        try
        {
            panelObject.transform.SetParent(root.transform);
            Canvas canvas = root.AddComponent<Canvas>();
            DeathResultsPanel results = root.AddComponent<DeathResultsPanel>();
            CanvasGroup panel = panelObject.AddComponent<CanvasGroup>();
            SetField(results, "resultsCanvas", canvas);
            SetField(results, "panel", panel);
            string[] fields = { "characterNameText", "killsText", "miniBossKillsText", "roomsClearedText", "floorsEnteredText", "outcomeText" };
            TMP_Text[] texts = new TMP_Text[fields.Length];
            for (int index = 0; index < fields.Length; index++)
            {
                GameObject textObject = new GameObject(fields[index], typeof(RectTransform));
                textObject.transform.SetParent(index == fields.Length - 1 ? root.transform : panelObject.transform);
                texts[index] = textObject.AddComponent<TextMeshProUGUI>();
                SetField(results, fields[index], texts[index]);
            }
            foreach (string field in new[] { "slamDuration", "settleDuration", "rowDelay", "resultsHoldDuration", "panelFadeDuration", "outcomeFadeDuration", "outcomeHoldDuration" })
                SetField(results, field, 0f);
            Player.RunStatistics statistics = new Player.RunStatistics { kills = 4, miniBossKills = 1, roomsCleared = 2, floorsEntered = 3 };
            IEnumerator show = results.Show(statistics, true, null, "Arlen");
            Require(show.MoveNext() && panel.alpha == 1f && texts[0].alpha == 0f, "Death name appeared before the panel slam.");
            Drain((IEnumerator)show.Current);
            Require(show.MoveNext() && show.MoveNext(), "Death name reveal was missing.");
            Require(texts[0].text == "Name: Arlen" && texts[0].alpha == 1f && texts[1].alpha == 0f, "Death name was not the first revealed row.");
            Drain((IEnumerator)show.Current);
            Drain(show);
            Require(texts[1].text == "Kills: 4" && texts[2].text == "Mini-boss Kills: 1" &&
                texts[3].text == "Rooms Cleared: 2" && texts[4].text == "Floors Entered: 3" &&
                texts[5].text == "You are reanimated" && !canvas.enabled, "Name row changed statistics or outcome presentation.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void CheckBossNames()
    {
        GameObject root = TemporaryObject("Boss Name Check", false);
        try
        {
            GameObject textObject = new GameObject("Boss Name", typeof(RectTransform));
            textObject.transform.SetParent(root.transform);
            TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
            BossHealthBarUI ui = root.AddComponent<BossHealthBarUI>();
            SetField(ui, "bossNameText", text);
            EnemyHealth health = root.AddComponent<EnemyHealth>();
            health.Init(100);
            health.SetEnemyName("The Warden");
            EnemyLevelXP regularLevel = root.AddComponent<EnemyLevelXP>();
            regularLevel.SetLevel(4);
            Invoke(ui, "BindBoss", health);
            Require(text.text == string.Empty, "Regular enemy displayed a boss name.");
            SetField(health, "isMiniBoss", true);
            Invoke(ui, "RefreshName");
            Require(text.text == "The Warden | Lvl 4", "Mini-boss label ignored its name or final spawn level.");
            regularLevel.SetLevel(6);
            Invoke(ui, "RefreshName");
            Require(text.text == "The Warden | Lvl 6", "Mini-boss label did not refresh its modified level.");
            FallenDescenderLevelXP fallenLevel = root.AddComponent<FallenDescenderLevelXP>();
            fallenLevel.SetLevel(8);
            health.SetEnemyName("Fallen Descender Arlen");
            Invoke(ui, "RefreshName");
            Require(text.text == "Fallen Descender Arlen | Lvl 8", "Fallen label used the regular XP level instead of the dedicated tracker.");
            typeof(EnemyHealth).GetProperty("CurrentHealth").GetSetMethod(true).Invoke(health, new object[] { 0 });
            Invoke(ui, "RefreshName");
            Require(text.text == string.Empty, "Dead mini-boss retained a name label.");
            health.Init(100);
            Invoke(ui, "RefreshName");
            Invoke(ui, "UnbindBoss");
            Require(text.text == string.Empty, "Unbound mini-boss retained a name label.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void CheckEnemyWeaponSkills()
    {
        GameObject root = TemporaryObject("Enemy Weapon Skill Check", false);
        GameObject playerObject = TemporaryObject("Hostile Skill Player", false);
        ItemData weapon = ScriptableObject.CreateInstance<ItemData>();
        EquipmentManager previousEquipment = EquipmentManager.Instance;
        PlayerElevationLevel previousElevation = PlayerElevationLevel.Instance;
        try
        {
            typeof(EquipmentManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { null });
            PlayerElevationLevel elevation = playerObject.AddComponent<PlayerElevationLevel>();
            typeof(PlayerElevationLevel).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { elevation });
            playerObject.layer = LayerMask.NameToLayer("Player");
            PlayerStats player = playerObject.AddComponent<PlayerStats>();
            player.maxHealth = 1000f;
            SetField(player, "currentHealth", 1000f);
            player.baseSlashResistance = 0f;
            Collider2D first = playerObject.AddComponent<BoxCollider2D>();
            Collider2D second = playerObject.AddComponent<CircleCollider2D>();
            EnemyHealth enemy = root.AddComponent<EnemyHealth>();
            enemy.Init(100);
            root.layer = LayerMask.NameToLayer("Enemy");
            Collider2D enemyCollider = root.AddComponent<BoxCollider2D>();
            List<DungeonMemory.DamageStat> hits = new List<DungeonMemory.DamageStat>
            {
                new DungeonMemory.DamageStat { type = DamageType.Slash, damage = 20f }
            };
            BossBalance.HostileDamage context = new BossBalance.HostileDamage(hits, 0);
            context.HitTargets(new[] { first, second, enemyCollider });
            Require(player.CurrentHealth == 980f && enemy.CurrentHealth == 100, "Hostile skills damaged enemies or double-hit a multi-collider player.");
            SetField(elevation, "currentLevel", 5);
            context.HitTargets(new[] { first });
            Require(player.CurrentHealth == 980f, "Hostile skill ignored elevation.");
            SetField(elevation, "currentLevel", 0);

            SerializedObject serialized = new SerializedObject(weapon);
            serialized.FindProperty("equipmentType").enumValueIndex = (int)EquipmentType.Weapon;
            SerializedProperty stats = serialized.FindProperty("weaponStats");
            stats.FindPropertyRelative("attack").FindPropertyRelative("cooldown").floatValue = 2f;
            SerializedProperty damageEntries = stats.FindPropertyRelative("attack").FindPropertyRelative("damages");
            damageEntries.arraySize = 1;
            damageEntries.GetArrayElementAtIndex(0).FindPropertyRelative("damageType").intValue = (int)DamageType.Slash;
            damageEntries.GetArrayElementAtIndex(0).FindPropertyRelative("damageSlot").enumValueIndex = (int)DamageSlot.Primary;
            damageEntries.GetArrayElementAtIndex(0).FindPropertyRelative("value").floatValue = 20f;
            stats.FindPropertyRelative("skill").FindPropertyRelative("cooldown").floatValue = 3f;
            stats.FindPropertyRelative("skillType").enumValueIndex = (int)WeaponSkillType.Beam;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            WeaponController controller = root.AddComponent<WeaponController>();
            controller.ConfigureForEnemy(weapon, hits, 0, root.transform, root.transform);
            Require(controller.AttackCooldownSeconds == 3f && controller.SkillCooldownSeconds == 7.5f, "Cooldown multipliers were not applied to weapon seconds.");
            Invoke(controller, "DamageTargets", (object)new[] { first, second, enemyCollider });
            Require(player.CurrentHealth < 980f && enemy.CurrentHealth == 100, "Hostile weapon skill did not damage PlayerStats exclusively.");
            controller.UseSkillForEnemy(Vector2.right, playerObject.transform.position, 0);
            Require(controller.IsPerformingEnemySkill && !controller.CanUseSkill, "Fallen charged skill did not begin or respect its cooldown.");
            controller.CancelEnemyActions();
            Require(!controller.IsPerformingEnemySkill, "Fallen death did not cancel charging.");

            Spell spell = root.AddComponent<Spell>();
            spell.InitializeForEnemy(hits, 1f, 10f, false, 0);
            Require(new SerializedObject(spell).FindProperty("hittableLayers").intValue == LayerMask.GetMask("Player"), "Hostile Spell retained Enemy hittable layers.");
            typeof(ProjectileBase).GetMethod("OnTriggerEnter2D", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(spell, new object[] { enemyCollider });
            Require(enemy.CurrentHealth == 100, "Hostile Spell hit an enemy.");
            BaseArrow arrow = root.AddComponent<BaseArrow>();
            SetField(arrow, "enableDebugLogs", false);
            arrow.LaunchForEnemy(Vector2.right, hits, 1f, 10f, false, 0);
            Require(new SerializedObject(arrow).FindProperty("hittableLayers").intValue == LayerMask.GetMask("Player"), "Hostile BaseArrow retained Enemy hittable layers.");
            root.AddComponent<Beam>().SetEnemyDamage(new BossBalance.HostileDamage(hits, 0));
            root.AddComponent<Typhoon>().SetEnemyDamage(new BossBalance.HostileDamage(hits, 0));
            root.AddComponent<VeilOfFire>().SetEnemyDamage(new BossBalance.HostileDamage(hits, 0));
            foreach (MonoBehaviour effect in new MonoBehaviour[] { root.GetComponent<Beam>(), root.GetComponent<Typhoon>(), root.GetComponent<VeilOfFire>() })
                Require(((LayerMask)effect.GetType().GetField("hittableLayers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(effect)).value ==
                    LayerMask.GetMask("Player"), "A persistent hostile effect retained Enemy hittable layers.");
        }
        finally
        {
            typeof(EquipmentManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { previousEquipment });
            typeof(PlayerElevationLevel).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { previousElevation });
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(playerObject);
            UnityEngine.Object.DestroyImmediate(weapon);
        }
    }

    private static void CheckReanimatedFloorRolls()
    {
        GameObject root = TemporaryObject("Reanimation Rolls", false);
        ItemData weapon = ScriptableObject.CreateInstance<ItemData>();
        try
        {
            DungeonMemory memory = root.AddComponent<DungeonMemory>();
            SetTestCatalog(memory, weapon);
            DungeonMemory.Archive archive = new DungeonMemory.Archive();
            DungeonMemory.SavedDeath incomplete = TestReanimatedDeath();
            incomplete.id = "legacy-empty-snapshot";
            incomplete.combat = new DungeonMemory.CombatSnapshot();
            archive.Remember(incomplete);
            DungeonMemory.SavedDeath noWeapon = TestReanimatedDeath();
            noWeapon.id = "snapshot-without-weapon";
            noWeapon.combat.equipment.Clear();
            archive.Remember(noWeapon);
            DungeonMemory.SavedDeath death = TestReanimatedDeath();
            archive.Remember(death);
            SetField(memory, "archive", archive);
            SetField(memory, "bodySpawnChance", 0f);
            SetField(memory, "laterFloorSpawnChance", 1f);
            Player player = root.AddComponent<Player>();
            Require(memory.SelectReanimatedForFloor(1, player) == null && memory.SelectReanimatedForFloor(2, player) == null, "Memory spawned below its floor or ignored failed first roll.");
            Require(memory.SelectReanimatedForFloor(3, player) == death && memory.SelectReanimatedForFloor(3, player) == null, "Incomplete snapshots blocked a valid Fallen, or later-floor retry/duplicate-roll guard failed.");
            player.MarkDungeonMemoryEncountered(death.id);
            Require(memory.SelectReanimatedForFloor(4, player) == null && archive.deaths.Count == 3, "Encounter repeated or valid/legacy archive records were consumed before defeat.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(weapon);
        }
    }

    private static void CheckRememberedFallenPrefab()
    {
        const string saveKey = "Sitanite.DungeonMemory.v1";
        if (!PlayerPrefs.HasKey(saveKey))
            return;

        DungeonMemory.Archive archive = JsonUtility.FromJson<DungeonMemory.Archive>(PlayerPrefs.GetString(saveKey));
        if (archive?.deaths == null)
            return;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/EnemyScripts/FallenDescender/FallenDescender.prefab");
        if (prefab == null)
            return;

        GameObject root = TemporaryObject("Saved Fallen Prefab Check", false);
        GameObject actor = null;
        try
        {
            DungeonMemory memory = root.AddComponent<DungeonMemory>();
            Invoke(memory, "RefreshItemCatalog");
            SetField(memory, "archive", archive);
            SetField(memory, "bodySpawnChance", 1f);
            SetField(memory, "laterFloorSpawnChance", 1f);
            Player player = root.AddComponent<Player>();
            PlayerStats stats = root.AddComponent<PlayerStats>();
            stats.maxHealth = 100f;
            int floor = 1;
            foreach (DungeonMemory.SavedDeath death in archive.deaths)
                floor = Mathf.Max(floor, death.floor);

            DungeonMemory.SavedDeath selected = memory.SelectReanimatedForFloor(floor, player);
            if (selected == null)
                return;

            actor = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            actor.hideFlags = HideFlags.HideAndDontSave;
            actor.SetActive(false);
            FallenDescender fallen = actor.GetComponent<FallenDescender>();
            Require(fallen != null, "Authored Fallen prefab has no FallenDescender on its root.");
            Invoke(fallen, "Awake");
            Require(fallen.Configure(selected, memory, null, stats, new BossBalance.SpawnModifiers()),
                $"Saved reanimation '{selected.id}' failed on the authored Fallen prefab: {fallen.ConfigurationError}");
            Debug.Log($"DungeonMemory saved-prefab check: reanimated level {fallen.Level}, weapon '{fallen.Weapon.name}' configured successfully; PlayerPrefs unchanged.");
        }
        finally
        {
            if (actor != null)
                UnityEngine.Object.DestroyImmediate(actor);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void CheckDelayedWaveClear()
    {
        GameObject root = TemporaryObject("Delayed Wave Clear", false);
        GameObject prefab = TemporaryObject("Fallen Prefab", false);
        GameObject spawned = null;
        try
        {
            prefab.AddComponent<FallenDescender>();
            prefab.AddComponent<EnemyLevelXP>();
            SpriteRenderer lower = prefab.AddComponent<SpriteRenderer>();
            lower.sortingOrder = 2;
            GameObject upperObject = TemporaryObject("Upper Paperdoll", false);
            upperObject.transform.SetParent(prefab.transform, false);
            upperObject.AddComponent<SpriteRenderer>().sortingOrder = 9;
            EnemySpawnPoint point = root.AddComponent<EnemySpawnPoint>();
            bool cleared = false;
            point.OnWaveCleared += () => cleared = true;
            spawned = point.SpawnEnemyAt(Vector3.zero, prefab, 7, root.transform);
            Require(spawned.GetComponent<FallenDescenderLevelXP>()?.Level == 7 &&
                !spawned.GetComponent<EnemyLevelXP>().enabled, "Fallen spawn did not use its dedicated XP tracker or left legacy XP enabled.");
            Require(spawned.GetComponent<Rigidbody2D>().constraints == RigidbodyConstraints2D.FreezeRotation, "Spawner froze Fallen movement.");
            SpriteRenderer[] parts = spawned.GetComponentsInChildren<SpriteRenderer>(true);
            Require(parts[0].sortingOrder == 2 && parts[1].sortingOrder == 9 &&
                spawned.GetComponent<UnityEngine.Rendering.SortingGroup>() != null, "Spawner destroyed paperdoll ordering.");
            EnemyHealth health = spawned.GetComponent<EnemyHealth>();
            Action<GameObject> deathHandlers = (Action<GameObject>)typeof(EnemyHealth).GetField("OnEnemyDied", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(health);
            deathHandlers?.Invoke(spawned);
            Require(!cleared, "Fallen wave cleared before the corpse handoff.");
            FallenDescender fallen = spawned.GetComponent<FallenDescender>();
            Action<GameObject> corpseHandlers = (Action<GameObject>)typeof(FallenDescender).GetField("CorpseCreated", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(fallen);
            corpseHandlers?.Invoke(spawned);
            Require(cleared, "Fallen corpse handoff did not clear the room wave.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    private static DungeonMemory.SavedDeath TestReanimatedDeath()
    {
        return new DungeonMemory.SavedDeath
        {
            id = "test-fallen", floor = 2, outcome = DungeonMemory.DeathOutcome.Reanimated,
            carriedLoot = new List<DungeonMemory.SavedLoot>(),
            combat = new DungeonMemory.CombatSnapshot
            {
                health = 80f, sprintSpeed = 2f, level = 5,
                stats = new List<DungeonMemory.DamageStat> { new DungeonMemory.DamageStat { type = DamageType.Slash, damage = 20f, resistance = 10f } },
                equipment = new List<DungeonMemory.SavedEquipment>
                {
                    new DungeonMemory.SavedEquipment
                    {
                        itemId = "weapon", slot = EquipmentType.Weapon,
                        weaponDamages = new List<WeaponDamage> { new WeaponDamage { damageType = DamageType.Slash, value = 8f } }
                    }
                }
            }
        };
    }

    private static void SetTestCatalog(DungeonMemory memory, ItemData weapon)
    {
        SerializedObject serialized = new SerializedObject(memory);
        SerializedProperty catalog = serialized.FindProperty("itemCatalog");
        catalog.arraySize = 1;
        catalog.GetArrayElementAtIndex(0).FindPropertyRelative("id").stringValue = "weapon";
        catalog.GetArrayElementAtIndex(0).FindPropertyRelative("item").objectReferenceValue = weapon;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CheckSlam()
    {
        GameObject root = TemporaryObject("Slam Check", false);
        try
        {
            DeathResultsPanel panel = root.AddComponent<DeathResultsPanel>();
            SetField(panel, "slamDuration", 0f);
            SetField(panel, "settleDuration", 0f);
            Vector3 scale = new Vector3(1.2f, 0.8f, 1f);
            Vector3 position = new Vector3(15f, -20f, 0f);
            root.transform.localScale = scale;
            root.transform.localPosition = position;
            IEnumerator animation = (IEnumerator)Invoke(panel, "Slam", root.transform);
            Drain(animation);
            Require(root.transform.localScale == scale && root.transform.localPosition == position, "Slam changed the authored end transform.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void CheckWalkablePlacement()
    {
        GameObject root = TemporaryObject("Walkable Check", true);
        AStarManager previous = AStarManager.Instance;
        Tile tile = ScriptableObject.CreateInstance<Tile>();
        GameObject corpsePrefab = TemporaryObject("Hidden Corpse Prefab", false);
        Texture2D corpseTexture = new Texture2D(2, 2);
        Sprite corpseSprite = Sprite.Create(corpseTexture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f);
        try
        {
            root.transform.position = new Vector3(10000f, 10000f, 0f);
            root.AddComponent<Grid>();
            RoomInstance room = root.AddComponent<RoomInstance>();
            GameObject mapObject = TemporaryObject("Sparse Walkable Map", true);
            mapObject.transform.SetParent(root.transform, false);
            Tilemap map = mapObject.AddComponent<Tilemap>();
            TilemapRenderer mapRenderer = mapObject.AddComponent<TilemapRenderer>();
            mapRenderer.sortingOrder = 50;
            AStarWalkableMap walkable = mapObject.AddComponent<AStarWalkableMap>();
            SetField(walkable, "tilemap", map);
            map.SetTile(Vector3Int.zero, tile);
            map.SetTile(new Vector3Int(2, 1, 0), tile);
            AStarManager manager = root.AddComponent<AStarManager>();
            typeof(AStarManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { manager });
            manager.RegisterWalkableTilemap(walkable);
            DungeonMemory memory = root.AddComponent<DungeonMemory>();
            SetField(memory, "lootScatterRadius", 20f);
            for (int index = 0; index < 32; index++)
            {
                Vector3? body = (Vector3?)Invoke(memory, "FindBodyPosition", room);
                Require(body.HasValue && map.HasTile(map.WorldToCell(body.Value)), "Body placement left the sparse walkable map.");
                Vector3 loot = (Vector3)Invoke(memory, "FindLootOrigin", body.Value, map);
                Require(map.HasTile(map.WorldToCell(loot)), "Scattered loot left the walkable map.");
            }
            SpriteRenderer corpseRenderer = corpsePrefab.AddComponent<SpriteRenderer>();
            corpseRenderer.enabled = false;
            corpseRenderer.color = new Color(1f, 1f, 1f, 0f);
            corpsePrefab.AddComponent<UnityEngine.Rendering.SortingGroup>();
            SetField(memory, "bodyPrefab", corpsePrefab);
            SetField(memory, "maleBodySprite", corpseSprite);
            DungeonMemory.SavedDeath death = TestReanimatedDeath();
            Require((bool)Invoke(memory, "TrySpawnBody", death, new List<RoomInstance> { room }, (Vector3?)root.transform.position + Vector3.right * 100f),
                "Off-tile Fallen corpse did not fall back to its room's walkable map.");
            SpriteRenderer renderedCorpse = room.GetComponentInChildren<SpriteRenderer>();
            Require(renderedCorpse != null && renderedCorpse.sprite == corpseSprite && renderedCorpse.enabled && renderedCorpse.color.a == 1f &&
                renderedCorpse.sortingOrder == 51 && renderedCorpse.GetComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder == 51,
                "Assigned corpse prefab remained inactive, transparent or underneath the floor.");
        }
        finally
        {
            typeof(AStarManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { previous });
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(tile);
            UnityEngine.Object.DestroyImmediate(corpsePrefab);
            UnityEngine.Object.DestroyImmediate(corpseSprite);
            UnityEngine.Object.DestroyImmediate(corpseTexture);
        }
    }

    private static GameObject TemporaryObject(string name, bool active)
    {
        GameObject result = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
        result.SetActive(active);
        return result;
    }

    private static void SetField(object target, string field, object value)
    {
        target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    }

    private static object Invoke(object target, string method, params object[] arguments)
    {
        return target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, arguments);
    }

    private static void Drain(IEnumerator routine)
    {
        while (routine.MoveNext())
        {
            if (routine.Current is IEnumerator nested)
                Drain(nested);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}