using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class ManggagawayChecks
{
    [MenuItem("Tools/Manggagaway/Run Checks")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before running Manggagaway checks.");
        CheckHealth();
        CheckDirections();
        CheckDamageTypes();
        CheckOverwhelmAndUI();
        CheckPrefabWiring();
        Debug.Log("PASS: Manggagaway phase floor, invulnerability, refill, final death, ordinary enemy health, " +
            "direction sharing, independent attack damage types, spell defaults, healing cancellation, prefab wiring and independent main-boss UI.");
    }

    [MenuItem("Tools/Manggagaway/Run Floor Checks (Empty Scene)")]
    public static void RunFloorChecks()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            UnityEngine.Object.FindFirstObjectByType<FloorManager>() != null ||
            UnityEngine.Object.FindFirstObjectByType<EnemySpawnerManager>() != null ||
            Player.Instance != null)
            throw new InvalidOperationException("Run boss-floor checks in an empty scene outside Play Mode.");

        CheckFloor(false);
        CheckFloor(true);
    }

    [MenuItem("Tools/Manggagaway/Run Room Checks (Empty Scene)")]
    public static void RunRoomChecks()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            UnityEngine.Object.FindFirstObjectByType<RoomManager>() != null)
            throw new InvalidOperationException("Run room-resolution checks in an empty scene outside Play Mode.");

        GameObject root = new GameObject("Manggagaway Room Checks");
        string assetPath = AssetDatabase.GenerateUniqueAssetPath("Assets/ManggagawayRoomCheck.prefab");
        try
        {
            RoomManager manager = root.AddComponent<RoomManager>();
            RoomInstance[] rooms = new RoomInstance[2];
            for (int i = 0; i < rooms.Length; i++)
            {
                GameObject roomRoot = new GameObject("Runtime Room " + i);
                roomRoot.transform.SetParent(root.transform);
                roomRoot.transform.position = new Vector3(i * 20f, 0f, 0f);
                rooms[i] = roomRoot.AddComponent<RoomInstance>();
                roomRoot.AddComponent<BoxCollider2D>().size = new Vector2(6f, 6f);
                Get<List<RoomInstance>>(manager, "generatedRooms").Add(rooms[i]);
            }
            Physics2D.SyncTransforms();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rooms[0].gameObject, assetPath);
            RoomInstance assetRoom = prefab.GetComponent<RoomInstance>();
            Require(!assetRoom.gameObject.scene.IsValid(), "Fixture room must be a prefab asset.");

            GameObject bossRoot = new GameObject("Boss");
            bossRoot.SetActive(false);
            bossRoot.transform.SetParent(rooms[0].transform);
            EnemyManggagaway boss = bossRoot.AddComponent<EnemyManggagaway>();
            Set(boss, "room", assetRoom);
            Set(boss, "hasRoomBounds", true);
            Set(boss, "roomBounds", new Bounds(Vector3.one * 100f, Vector3.one));
            Invoke(boss, "ResolveRoom");
            Require(boss.Room == rooms[0] && Get<bool>(boss, "hasRoomBounds") &&
                Get<Bounds>(boss, "roomBounds").center == rooms[0].transform.position,
                "Spawned parent did not replace the prefab room and stale bounds.");

            bossRoot.transform.SetParent(rooms[1].transform);
            Invoke(boss, "ResolveRoom");
            Require(boss.Room == rooms[1] &&
                Get<Bounds>(boss, "roomBounds").center == rooms[1].transform.position,
                "Changing parent rooms did not refresh the room and bounds.");

            bossRoot.transform.SetParent(root.transform);
            Invoke(boss, "ResolveRoom");
            Require(boss.Room == rooms[1], "Unparented boss lost its valid explicit scene room.");
            Set(boss, "room", assetRoom);
            Invoke(boss, "ResolveRoom");
            Require(boss.Room == rooms[0], "Unparented boss retained a prefab asset instead of finding its runtime room.");
            Set(boss, "room", null);
            Invoke(boss, "ResolveRoom");
            Require(boss.Room == rooms[0], "Unassigned room no longer resolves through RoomManager.");
            bossRoot.transform.position = Vector3.one * 1000f;
            Set(boss, "room", assetRoom);
            Invoke(boss, "ResolveRoom");
            Require(boss.Room == null && !Get<bool>(boss, "hasRoomBounds"),
                "Failed room lookup retained a prefab asset or stale bounds.");
            Debug.Log("PASS: Manggagaway replaces prefab room references with spawned rooms, " +
                "refreshes bounds on reparenting, preserves explicit scene rooms, and resolves unparented bosses by position.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.DeleteAsset(assetPath);
        }
    }

    private static void CheckFloor(bool overlapsRegularFloor)
    {
        UnityEngine.Random.State randomState = UnityEngine.Random.state;
        GameObject root = new GameObject("Manggagaway Floor Checks");
        try
        {
            FloorManager floor = root.AddComponent<FloorManager>();
            RoomManager rooms = root.AddComponent<RoomManager>();
            EnemySpawnerManager spawner = root.AddComponent<EnemySpawnerManager>();
            Set(floor, "roomManager", rooms);
            Set(rooms, "floorManager", floor);
            Set(floor, "currentFloor", 4);
            FloorConfiguration regular = new FloorConfiguration
            {
                minimumFloor = 1, maximumFloor = overlapsRegularFloor ? 10 : 4,
                minimumRooms = 8, maximumRooms = 8
            };
            Get<List<FloorConfiguration>>(floor, "floorConfigurations").Add(regular);
            Require(!floor.IsManggagawayBossFloor && floor.CurrentConfiguration == regular,
                "Unassigned boss floor changed regular configuration selection.");
            object bossFloor = Get<object>(floor, "manggagawayBossFloor");
            bossFloor.GetType().GetField("floor").SetValue(bossFloor, 5);
            Require(!floor.IsManggagawayBossFloor, "Boss floor matched the wrong floor number.");

            GameObject prefab = new GameObject("Boss Room");
            prefab.transform.SetParent(root.transform);
            prefab.AddComponent<RoomInstance>();
            new GameObject("Player Spawn").AddComponent<RoomSpawnPoint>().transform.SetParent(prefab.transform);
            foreach (GatewayFlow flow in new[] { GatewayFlow.Forward, GatewayFlow.Backward, GatewayFlow.Floor, GatewayFlow.SecretForward })
            {
                Gateway gateway = new GameObject(flow.ToString()).AddComponent<Gateway>();
                gateway.transform.SetParent(prefab.transform);
                Set(gateway, "flow", flow);
            }
            for (int i = 0; i < 3; i++)
            {
                EnemySpawnPoint point = new GameObject("Boss Spawn " + i).AddComponent<EnemySpawnPoint>();
                point.transform.SetParent(prefab.transform);
                point.transform.localPosition = new Vector3(i * 3f, 2f, 0f);
                Set(point, "fixedSpawnCount", 99);
                Set(point, "minSpawn", 99);
                Set(point, "maxSpawn", 99);
                Set(point, "clearWhenAllDead", false);
                Set(point, "elevationLevel", 2);
            }
            Set(rooms, "manggagawayBossRoomPrefab", prefab);
            Set(rooms, "secretGatewayChance", 1f);
            Get<List<GameObject>>(rooms, "secretRoomPrefabs").Add(prefab);

            GameObject enemyPrefab = new GameObject("Manggagaway Prefab");
            enemyPrefab.SetActive(false);
            enemyPrefab.transform.SetParent(root.transform);
            enemyPrefab.AddComponent<EnemyManggagaway>();
            enemyPrefab.GetComponent<EnemyLevelXP>().SetLevel(7);
            object bossConfiguration = Get<object>(spawner, "manggagawayConfiguration");
            bossConfiguration.GetType().GetField("enemyPrefab").SetValue(bossConfiguration, enemyPrefab);

            GameObject playerRoot = new GameObject("Test Player");
            playerRoot.SetActive(false);
            playerRoot.transform.SetParent(root.transform);
            Player player = playerRoot.AddComponent<Player>();
            typeof(Player).GetProperty("Instance").SetValue(null, player);

            int bossEvents = 0;
            int clearEvents = 0;
            spawner.OnBossSpawned += _ => bossEvents++;
            spawner.OnRoomWaveCleared += _ => clearEvents++;
            floor.EnterNextFloor();
            Require(floor.CurrentFloor == 5 && floor.IsManggagawayBossFloor,
                "Could not advance into the exact boss floor.");
            Require(rooms.GeneratedRooms.Count == 1 && rooms.GeneratedRooms[0].name.StartsWith("Boss Room"),
                "Boss floor did not override overlapping regular room-count/prefab settings.");
            Require(Get<List<RoomInstance>>(rooms, "generatedSecretRooms").Count == 0,
                "Boss floor generated an extra secret room.");

            RoomInstance room = rooms.GeneratedRooms[0];
            EnemyManggagaway[] bosses = room.GetComponentsInChildren<EnemyManggagaway>(true);
            Require(bosses.Length == 1 && bossEvents == 1 && spawner.ActiveBossRoom == room,
                "Boss floor did not spawn and track exactly one Manggagaway.");
            EnemyHealth health = spawner.ActiveBossHealth;
            Require(health.GetComponent<EnemyLevelXP>().Level == 7 && !health.HasConfiguredStats &&
                health.GetComponent<AdaptiveEnemy>() == null,
                "Boss floor replaced the prefab's fixed level/stats or applied adaptation/balancing.");
            Require(health.GetComponent<EnemyElevationLevel>().CurrentLevel == 2,
                "Boss did not inherit its selected spawn point's elevation.");
            bool atSpawnPoint = false;
            foreach (EnemySpawnPoint point in room.GetComponentsInChildren<EnemySpawnPoint>(true))
                atSpawnPoint |= health.transform.position == point.transform.position;
            Require(atSpawnPoint && !rooms.IsFloorGatewayUnlocked && clearEvents == 0,
                "Boss was scattered away from its spawn point or floor unlocked before death.");
            spawner.SpawnFloorEnemies();
            spawner.SpawnRoomEnemies(room);
            Require(room.GetComponentsInChildren<EnemyManggagaway>(true).Length == 1 && bossEvents == 1,
                "Repeated spawn requests added enemies to the boss room.");

            Invoke(health, "Die");
            Require(rooms.IsFloorGatewayUnlocked && clearEvents == 1 && spawner.ActiveBossHealth == null,
                "Boss death did not clear the room and unlock the floor with wave clearing disabled.");
            Invoke(health, "Die");
            Require(clearEvents == 1, "Boss death cleared the room more than once.");
            regular.maximumFloor = 10;
            Set(floor, "currentFloor", 6);
            Require(!floor.IsManggagawayBossFloor && floor.CurrentConfiguration == regular,
                "Floors after the boss no longer use regular configurations.");
            Debug.Log($"PASS (overlapping regular range: {overlapsRegularFloor}): " +
                "Exact boss-floor progression and range override; one room, no secrets; " +
                "one fixed-level boss at a spawn point; boss UI event; no adaptation/balancing; " +
                "idempotent spawning and death-driven floor unlock; regular configuration preserved.");
        }
        finally
        {
            typeof(Player).GetProperty("Instance").SetValue(null, null);
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Random.state = randomState;
        }
    }

    private static void CheckHealth()
    {
        GameObject root = new GameObject("Manggagaway Check") { hideFlags = HideFlags.HideAndDontSave };
        root.SetActive(false);
        try
        {
            EnemyManggagaway boss = root.AddComponent<EnemyManggagaway>();
            EnemyHealth health = root.GetComponent<EnemyHealth>();
            Invoke(health, "Awake");
            Invoke(boss, "Awake");
            health.Init(100);
            Invoke(boss, "UpdatePhaseFloor");
            int deaths = 0;
            health.OnEnemyDied += _ => deaths++;
            health.OnEnemyDied += dead => Invoke(boss, "HandleDeath", dead);
            health.TakeDamage(1000);
            Require(health.CurrentHealth == 45 && deaths == 0, "Lethal first-form damage bypassed 45% floor.");

            IEnumerator transformation = (IEnumerator)Invoke(boss, "TransformToSecondForm");
            Require(transformation.MoveNext() && boss.IsTransforming && health.IsInvulnerable,
                "Transformation did not protect the boss.");
            health.TakeDamage(1000);
            Require(health.CurrentHealth == 45 && deaths == 0, "Transformation allowed damage.");
            Require(!transformation.MoveNext(), "Transformation did not complete.");
            Require(boss.IsSecondForm && !boss.IsTransforming && health.CurrentHealth == 100 && !health.IsInvulnerable &&
                health.DamageHealthFloor == 0, "Second form did not fully refill and remove first-form protection.");

            IEnumerator healing = (IEnumerator)Invoke(boss, "Heal");
            health.TakeDamage(30);
            Require(healing.MoveNext() && boss.IsHealing, "Healing did not start.");
            int before = health.CurrentHealth;
            Invoke(boss, "HandleDamage", before, 1);
            Require(!boss.IsHealing && !healing.MoveNext() && health.CurrentHealth == before, "Damage did not cancel healing.");

            health.Init(1000);
            health.TakeDamage(500);
            IEnumerator completeHeal = (IEnumerator)Invoke(boss, "Heal");
            int waits = 0;
            while (completeHeal.MoveNext())
            {
                Require(completeHeal.Current is WaitForSeconds, "Healing did not wait between ticks.");
                waits++;
                Require(waits <= 30, "Healing exceeded 30 one-second ticks.");
            }
            Require(waits == 30 && health.CurrentHealth == 800 && !boss.IsHealing,
                "Healing did not restore exactly 10 HP per tick for 30 ticks.");

            health.TakeDamage(1000);
            Require(health.CurrentHealth == 0 && deaths == 1 && boss.IsDead, "Second form did not die exactly once.");
            health.TakeDamage(1);
            health.Heal(100);
            Require(health.CurrentHealth == 0 && deaths == 1, "Dead boss was revived or died twice.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }

        GameObject ordinary = new GameObject("Ordinary Health Check") { hideFlags = HideFlags.HideAndDontSave };
        ordinary.SetActive(false);
        try
        {
            EnemyHealth health = ordinary.AddComponent<EnemyHealth>();
            Invoke(health, "Awake");
            health.Init(10);
            health.TakeDamage(3);
            Require(health.CurrentHealth == 7 && !health.IsMainBoss, "Ordinary enemy damage changed.");
            health.Heal(2);
            Require(health.CurrentHealth == 9, "Ordinary healing changed.");
            health.TakeDamage(100);
            Require(health.CurrentHealth == 0, "Ordinary enemies acquired a phase floor.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(ordinary);
        }
    }

    private static void CheckDirections()
    {
        foreach (CharacterDirection direction in Enum.GetValues(typeof(CharacterDirection)))
        {
            Require(ManggagawayDefinition.ResolveDirection(direction, false) == direction, "First form lost eight-way facing.");
            Require(ManggagawayDefinition.ResolveDirection(direction, true) == direction, "Second form lost eight-way facing.");
        }

        GameObject visual = new GameObject("Manggagaway Facing Check") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            visual.AddComponent<SpriteRenderer>();
            ManggagawaySpriteAnimator animator = visual.AddComponent<ManggagawaySpriteAnimator>();
            animator.Play(ManggagawayAnimationState.Idle, new Vector2(0f, 1f), false);
            Require(Get<CharacterDirection>(animator, "direction") == CharacterDirection.North, "First form lost north facing.");
            animator.Play(ManggagawayAnimationState.SecondFormIdle, new Vector2(-1f, 0.2f), true);
            Require(Get<CharacterDirection>(animator, "direction") == CharacterDirection.West, "Second form did not face West.");
            animator.Play(ManggagawayAnimationState.SecondFormWalk, new Vector2(0f, -1f), true);
            Require(Get<CharacterDirection>(animator, "direction") == CharacterDirection.South,
                "Second form lost south facing.");
            animator.Play(ManggagawayAnimationState.SecondFormWalk, new Vector2(1f, 1f), true);
            Require(Get<CharacterDirection>(animator, "direction") == CharacterDirection.NorthEast,
                "Second form lost diagonal facing.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(visual);
        }
    }

    private static void CheckDamageTypes()
    {
        GameObject root = new GameObject("Manggagaway Damage Type Check") { hideFlags = HideFlags.HideAndDontSave };
        root.SetActive(false);
        try
        {
            EnemyManggagaway boss = root.AddComponent<EnemyManggagaway>();
            Invoke(root.GetComponent<EnemyHealth>(), "Awake");
            Invoke(boss, "Awake");
            SerializedObject settings = new SerializedObject(boss);
            string[] fields = { "projectileDamageType", "beamDamageType", "aoeDamageType" };
            DamageType[] types = { DamageType.Fire, DamageType.Lightning, DamageType.Poison };
            for (int i = 0; i < fields.Length; i++)
            {
                SerializedProperty property = settings.FindProperty(fields[i]);
                Require(property != null, $"Missing attack damage type setting: {fields[i]}.");
                property.intValue = (int)types[i];
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
            for (int i = 0; i < fields.Length; i++)
            {
                DamageType type = Get<DamageType>(boss, fields[i]);
                Require(type == types[i], $"Attack damage type is not independent: {fields[i]}.");
                BossBalance.HostileDamage damage = (BossBalance.HostileDamage)Invoke(boss, "Damage", 7, type);
                Require(damage.Hits.Count == 1 && damage.Hits[0].type == types[i] &&
                    damage.Hits[0].damage == 7 && damage.Elevation == root.GetComponent<EnemyElevationLevel>().CurrentLevel,
                    $"Attack damage payload lost its type, amount or elevation: {fields[i]}.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void CheckPrefabWiring()
    {
        string folder = ManggagawaySetup.AssetFolder;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/EnemyManggagaway.prefab");
        Require(prefab != null, "Create Starter Assets first.");
        EnemyManggagaway boss = prefab.GetComponent<EnemyManggagaway>();
        Require(boss != null && prefab.GetComponent<EnemyHealth>().IsMainBoss &&
            !prefab.GetComponent<EnemyHealth>().IsMiniBoss && prefab.GetComponent<Animator>() == null &&
            prefab.GetComponentInChildren<ManggagawaySpriteAnimator>() != null, "Boss identity/animation wiring is incorrect.");
        Require(boss.Room == null, "Boss prefab must resolve its runtime room instead of referencing a room asset.");
        Require(prefab.CompareTag("Enemy") && prefab.GetComponentInChildren<SpriteRenderer>().sortingLayerName == "Player",
            "Boss tag/sorting layer is incorrect.");
        SerializedObject settings = new SerializedObject(boss);
        string[] references = { "projectilePrefab", "beamPrefab", "beamChargePrefab", "aoePrefab", "summonPrefab", "healPrefab", "teleportPrefab" };
        foreach (string reference in references)
        {
            GameObject effect = (GameObject)settings.FindProperty(reference).objectReferenceValue;
            Require(effect != null && effect.GetComponent<SpriteRenderer>() != null &&
                effect.GetComponent<ManggagawaySpellVisual>() != null, $"Missing spell visual wiring: {reference}.");
            Require(effect.GetComponent<SpriteRenderer>().sortingLayerName == "Effects", $"Incorrect effect sorting: {reference}.");
        }
        string[] cooldowns = { "projectileCooldown", "beamCooldown", "aoeCooldown", "summonCooldown", "healCooldown", "teleportCooldown" };
        float[] expected = { 1.5f, 8f, 10f, 30f, 45f, 30f };
        for (int i = 0; i < cooldowns.Length; i++)
            Require(Mathf.Approximately(settings.FindProperty(cooldowns[i]).floatValue, expected[i]), $"Unexpected {cooldowns[i]}.");
        Require(settings.FindProperty("projectileLifetime").floatValue == 2f &&
            settings.FindProperty("beamChargeDuration").floatValue == 1.5f &&
            settings.FindProperty("healPerTick").intValue == 10 &&
            settings.FindProperty("healDuration").floatValue == 30f, "Spell defaults differ from requested values.");
        GameObject ui = AssetDatabase.LoadAssetAtPath<GameObject>($"{folder}/MainBossHealthBar.prefab");
        Require(ui != null && new SerializedObject(ui.GetComponent<BossHealthBarUI>()).FindProperty("trackMainBoss").boolValue,
            "Main boss UI is not independent of mini-boss UI.");
    }

    private static void CheckOverwhelmAndUI()
    {
        GameObject root = new GameObject("Overwhelm Check") { hideFlags = HideFlags.HideAndDontSave };
        GameObject miniRoot = new GameObject("Mini Boss Check") { hideFlags = HideFlags.HideAndDontSave };
        GameObject mainUiRoot = new GameObject("Main UI Check") { hideFlags = HideFlags.HideAndDontSave };
        GameObject miniUiRoot = new GameObject("Mini UI Check") { hideFlags = HideFlags.HideAndDontSave };
        root.SetActive(false);
        miniRoot.SetActive(false);
        mainUiRoot.SetActive(false);
        miniUiRoot.SetActive(false);
        try
        {
            EnemyManggagaway boss = root.AddComponent<EnemyManggagaway>();
            EnemyHealth health = root.GetComponent<EnemyHealth>();
            Invoke(health, "Awake");
            Invoke(boss, "Awake");
            health.Init(100);
            for (int i = 0; i < 9; i++)
                Invoke(boss, "HandleDamage", 100 - i, 1);
            Require(!Get<bool>(boss, "overwhelmed"), "Fewer than 10 low-damage hits triggered teleportation.");
            Invoke(boss, "HandleDamage", 91, 1);
            Require(Get<bool>(boss, "overwhelmed"), "Ten successive hits did not trigger teleportation.");
            Invoke(boss, "ResetOverwhelm");
            Invoke(boss, "HandleDamage", 50, 14);
            Require(!Get<bool>(boss, "overwhelmed"), "Damage below 30% of runtime HP triggered teleportation.");
            Invoke(boss, "HandleDamage", 36, 1);
            Require(Get<bool>(boss, "overwhelmed"),
                $"30% of sequence-start runtime HP did not trigger teleportation: damage={Get<int>(boss, "sequenceDamage")}, " +
                $"start={Get<int>(boss, "sequenceStartHealth")}, threshold={Get<float>(boss, "overwhelmDamageFraction")}.");
            Set(boss, "lastHitTime", Time.time - 6f);
            Invoke(boss, "HandleDamage", 100, 1);
            Require(!Get<bool>(boss, "overwhelmed") && Get<int>(boss, "sequenceHits") == 1,
                "A gap longer than the succession window did not reset the sequence.");

            EnemyHealth miniHealth = miniRoot.AddComponent<EnemyHealth>();
            Invoke(miniHealth, "Awake");
            Set(miniHealth, "isMiniBoss", true);
            BossHealthBarUI mainUi = mainUiRoot.AddComponent<BossHealthBarUI>();
            BossHealthBarUI miniUi = miniUiRoot.AddComponent<BossHealthBarUI>();
            Set(mainUi, "trackMainBoss", true);
            Invoke(mainUi, "BindBoss", health);
            Invoke(miniUi, "BindBoss", miniHealth);
            Require(Get<EnemyHealth>(mainUi, "bossHealth") == health && Get<EnemyHealth>(miniUi, "bossHealth") == miniHealth,
                "Separate boss bars could not bind independently.");
            Invoke(mainUi, "BindBoss", miniHealth);
            Invoke(miniUi, "BindBoss", health);
            Require(Get<EnemyHealth>(mainUi, "bossHealth") == null && Get<EnemyHealth>(miniUi, "bossHealth") == null,
                "A health bar accepted the wrong boss type.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mainUiRoot);
            UnityEngine.Object.DestroyImmediate(miniUiRoot);
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(miniRoot);
        }
    }

    private static T Get<T>(object target, string field) =>
        (T)target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);

    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

    private static object Invoke(object target, string method, params object[] args)
    {
        MethodInfo info = target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
        if (info == null)
            throw new MissingMethodException(target.GetType().Name, method);
        return info.Invoke(target, args);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
