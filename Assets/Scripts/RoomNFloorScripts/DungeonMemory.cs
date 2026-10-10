using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

[DisallowMultipleComponent]
public class DungeonMemory : MonoBehaviour
{
    public enum DeathOutcome { Dead, Reanimated }

    [Serializable]
    public class SavedLoot
    {
        public string itemId;
        public int quantity;
    }

    [Serializable]
    public class DamageStat
    {
        public DamageType type;
        public float damage;
        public float resistance;
    }

    [Serializable]
    public class SavedEquipment
    {
        public string itemId;
        public EquipmentType slot;
        public bool fallbackWeapon;
        public List<EquipmentStat> modifiers = new List<EquipmentStat>();
        public List<WeaponDamage> weaponDamages = new List<WeaponDamage>();
    }

    [Serializable]
    public class CombatSnapshot
    {
        public float health;
        public float sprintSpeed;
        public int level;
        public PlayerClass playerClass;
        public string hairId;
        public bool hideHeadwear;
        public List<DamageStat> stats = new List<DamageStat>();
        public List<SavedEquipment> equipment = new List<SavedEquipment>();
    }

    [Serializable]
    public class SavedDeath
    {
        public string id;
        public string characterName;
        public CharacterGender gender;
        public int floor;
        public DeathOutcome outcome;
        public List<SavedLoot> loot = new List<SavedLoot>();
        public List<SavedLoot> carriedLoot;
        public CombatSnapshot combat;
    }

    [Serializable]
    public class Archive
    {
        public int version = 1;
        public List<SavedDeath> deaths = new List<SavedDeath>();

        public void Remember(SavedDeath death)
        {
            deaths.Add(death);
            while (deaths.Count > 5)
                deaths.RemoveAt(0);
        }
    }

    [Serializable]
    private class ItemReference
    {
        [HideInInspector] public string id;
        public ItemData item;
    }

    [Serializable]
    private class PartReference
    {
        public string id;
        public CharacterPartDefinition part;
    }

    [Header("Body Visuals")]
    [SerializeField] private GameObject bodyPrefab;
    [SerializeField] private Sprite maleBodySprite;
    [SerializeField] private Sprite femaleBodySprite;

    [Header("Encounter")]
    [Range(0f, 1f)] [SerializeField] private float bodySpawnChance = 0.5f;
    [Range(0f, 1f)] [SerializeField] private float laterFloorSpawnChance = 0.25f;
    [Min(0f)] [SerializeField] private float lootScatterRadius = 1f;
    [Min(1)] [SerializeField] private int lootPlacementAttempts = 8;

    [Header("Item Catalog")]
    [SerializeField] private List<ItemReference> itemCatalog = new List<ItemReference>();
    [SerializeField] private List<PartReference> hairCatalog = new List<PartReference>();

    private const string SaveKey = "Sitanite.DungeonMemory.v1";
    private Archive archive;
    private bool persistenceAvailable = true;

    public IReadOnlyList<SavedDeath> RememberedDeaths => archive.deaths.AsReadOnly();

    private void Awake()
    {
        try
        {
            archive = PlayerPrefs.HasKey(SaveKey)
                ? JsonUtility.FromJson<Archive>(PlayerPrefs.GetString(SaveKey))
                : new Archive();

            if (archive == null || archive.version != 1 || archive.deaths == null)
                throw new ArgumentException("Unsupported or invalid dungeon memory archive.");

            archive.deaths.RemoveAll(death => death == null || string.IsNullOrEmpty(death.id));
            foreach (SavedDeath death in archive.deaths)
            {
                if (death.floor < 1 || death.loot == null || death.loot.Exists(loot => loot == null))
                    throw new ArgumentException("Invalid death record in dungeon memory archive.");
            }
        }
        catch (ArgumentException exception)
        {
            archive = new Archive();
            persistenceAvailable = false;
            Debug.LogError($"DungeonMemory: Saved data was not overwritten. {exception.Message}", this);
        }
    }

    public void RememberDeath(Player player, PlayerStats stats, int floor, DeathOutcome outcome)
    {
        if (!persistenceAvailable || player == null || stats == null || floor < 1)
            return;

        List<InventoryItem> carriedItems = GetCarriedItems(player);
        List<SavedLoot> carriedLoot = CaptureLoot(carriedItems, 1f, 1f);
        List<SavedLoot> loot = CaptureLoot(carriedItems);
        if (loot == null)
            return;

        CombatSnapshot combat = CaptureCombat(player, stats);
        if (combat == null || carriedLoot == null)
            return;

        archive.Remember(new SavedDeath
        {
            id = Guid.NewGuid().ToString("N"),
            characterName = stats.CharacterName,
            gender = stats.Gender,
            floor = floor,
            outcome = outcome,
            loot = loot,
            carriedLoot = carriedLoot,
            combat = combat
        });
        Save();
    }

    private List<InventoryItem> GetCarriedItems(Player player)
    {
        List<InventoryItem> carriedItems = new List<InventoryItem>();
        PlayerInventory inventory = player.GetComponentInChildren<PlayerInventory>(true);
        if (inventory != null && inventory.MainBackPack != null)
            carriedItems.AddRange(inventory.MainBackPack.GetItems());

        if (EquipmentManager.Instance != null)
        {
            foreach (EquipmentType slot in Enum.GetValues(typeof(EquipmentType)))
                carriedItems.Add(EquipmentManager.Instance.GetEquippedItem(slot));
        }

        PlayerEquipment weapon = player.GetComponentInChildren<PlayerEquipment>(true);
        if (weapon != null && weapon.CurrentWeaponData != null &&
            (EquipmentManager.Instance == null ||
             EquipmentManager.Instance.GetEquippedItem(EquipmentType.Weapon) == null))
        {
            carriedItems.Add(new InventoryItem(weapon.CurrentWeaponData));
        }

        if (DragDropManager.Instance != null)
            carriedItems.Add(DragDropManager.Instance.HeldItem);

        return carriedItems;
    }

    private CombatSnapshot CaptureCombat(Player player, PlayerStats stats)
    {
        CombatSnapshot result = new CombatSnapshot
        {
            health = stats.PreEquipmentMaxHealth,
            sprintSpeed = stats.SprintSpeed,
            level = stats.Level,
            playerClass = stats.PlayerClass
        };
        foreach (DamageType type in Enum.GetValues(typeof(DamageType)))
        {
            int value = (int)type;
            if (value <= 0 || (value & (value - 1)) != 0)
                continue;

            result.stats.Add(new DamageStat
            {
                type = type,
                damage = stats.GetPreEquipmentDamage(type),
                resistance = stats.GetPreEquipmentResistance(type)
            });
        }

        List<InventoryItem> gear = new List<InventoryItem>();
        if (EquipmentManager.Instance != null)
        {
            foreach (EquipmentType slot in Enum.GetValues(typeof(EquipmentType)))
            {
                InventoryItem item = EquipmentManager.Instance.GetEquippedItem(slot);
                if (item != null)
                    gear.Add(item);
            }
        }
        PlayerEquipment weapon = player.GetComponentInChildren<PlayerEquipment>(true);
        if (weapon != null && weapon.CurrentWeaponData != null &&
            !gear.Exists(item => item.Data.EquipmentType == EquipmentType.Weapon))
            gear.Add(new InventoryItem(weapon.CurrentWeaponData));

        ItemData fallback = null;
        if (!gear.Exists(item => item.Data.EquipmentType == EquipmentType.Weapon))
        {
            CharacterCustomizationController customization = player.GetComponentInChildren<CharacterCustomizationController>(true);
            fallback = customization != null ? customization.GetStartingWeapon(stats.PlayerClass) : null;
            if (fallback != null)
                gear.Add(new InventoryItem(fallback));
            else
                Debug.LogWarning($"DungeonMemory: No equipped or {stats.PlayerClass} starter weapon is available for reanimation.", this);
        }

        foreach (InventoryItem item in gear)
        {
            ItemReference reference = itemCatalog.Find(entry => entry != null && entry.item == item.Data);
            if (reference == null)
                return null;

            result.equipment.Add(new SavedEquipment
            {
                itemId = reference.id,
                slot = item.Data.EquipmentType,
                fallbackWeapon = item.Data == fallback,
                modifiers = new List<EquipmentStat>(item.Data.StatModifiers),
                weaponDamages = new List<WeaponDamage>(item.Data.WeaponDamages)
            });
        }

        CharacterRenderer character = player.GetComponentInChildren<CharacterRenderer>(true);
        if (character != null && character.Appearance != null)
        {
            result.hideHeadwear = character.Appearance.hideHeadwear;
            CharacterPartDefinition hair = character.Appearance.hair;
            if (hair != null)
            {
                PartReference reference = hairCatalog.Find(entry => entry != null && entry.part == hair);
                if (reference == null)
                {
                    Debug.LogError("DungeonMemory: Refresh Item Catalog to include the character's hair.", this);
                    return null;
                }
                result.hairId = reference.id;
            }
        }
        return result;
    }

    public ItemData ResolveItem(string id)
    {
        return itemCatalog.Find(entry => entry != null && entry.id == id)?.item;
    }

    public CharacterPartDefinition ResolveHair(string id)
    {
        return hairCatalog.Find(entry => entry != null && entry.id == id)?.part;
    }

    public List<SavedLoot> CaptureLoot(IEnumerable<InventoryItem> carriedItems, float minimumFraction = 0.25f, float maximumFraction = 0.5f)
    {
        List<SavedLoot> available = new List<SavedLoot>();
        HashSet<InventoryItem> seenItems = new HashSet<InventoryItem>();
        int totalQuantity = 0;

        foreach (InventoryItem item in carriedItems)
        {
            if (item == null || item.Data == null || item.Quantity <= 0 || !seenItems.Add(item))
                continue;

            ItemReference reference = itemCatalog.Find(entry => entry != null && entry.item == item.Data);
            if (reference == null || string.IsNullOrEmpty(reference.id))
            {
                Debug.LogError($"DungeonMemory: Refresh the item catalog before saving {item.Data.name}.", this);
                return null;
            }

            SavedLoot stack = available.Find(entry => entry.itemId == reference.id);
            if (stack == null)
            {
                stack = new SavedLoot { itemId = reference.id };
                available.Add(stack);
            }
            stack.quantity += item.Quantity;
            totalQuantity += item.Quantity;
        }

        List<SavedLoot> selected = new List<SavedLoot>();
        if (totalQuantity == 0)
            return selected;

        if (minimumFraction == 1f && maximumFraction == 1f)
            return available;

        int minimum = Mathf.Max(1, Mathf.CeilToInt(totalQuantity * minimumFraction));
        int maximum = Mathf.Max(minimum, Mathf.FloorToInt(totalQuantity * maximumFraction));
        int dropQuantity = Random.Range(minimum, maximum + 1);

        for (int count = 0; count < dropQuantity; count++)
        {
            int roll = Random.Range(0, totalQuantity);
            foreach (SavedLoot stack in available)
            {
                if (roll >= stack.quantity)
                {
                    roll -= stack.quantity;
                    continue;
                }

                SavedLoot drop = selected.Find(entry => entry.itemId == stack.itemId);
                if (drop == null)
                {
                    drop = new SavedLoot { itemId = stack.itemId };
                    selected.Add(drop);
                }
                drop.quantity++;
                stack.quantity--;
                totalQuantity--;
                break;
            }
        }
        return selected;
    }

    public void SpawnForFloor(int floor, IReadOnlyList<RoomInstance> rooms)
    {
        if (persistenceAvailable && rooms.Count > 0 && Player.Instance != null)
            StartCoroutine(SpawnAfterMapRegistration(floor, new List<RoomInstance>(rooms), Player.Instance));
    }

    public SavedDeath SelectReanimatedForFloor(int floor, Player player)
    {
        if (!persistenceAvailable || player == null)
            return null;

        foreach (SavedDeath death in archive.deaths)
        {
            if (death.outcome != DeathOutcome.Reanimated || death.floor > floor)
                continue;

            if (death.combat == null || death.carriedLoot == null ||
                death.combat.health <= 0f || death.combat.level < 1 ||
                death.combat.equipment == null || death.combat.stats == null || death.combat.stats.Count == 0 ||
                !death.combat.equipment.Exists(item => item != null && item.slot == EquipmentType.Weapon &&
                    !string.IsNullOrEmpty(item.itemId) && item.weaponDamages != null &&
                    item.weaponDamages.Exists(damage => damage.damageType != DamageType.None &&
                        death.combat.stats.Exists(stat => stat != null && stat.type == damage.damageType))))
            {
                Debug.LogWarning($"DungeonMemory: Reanimated record '{death.id}' has an incomplete combat snapshot or weapon; retained and skipped so other saved characters can spawn.", this);
                continue;
            }
            bool resolvable = death.combat.equipment.TrueForAll(item => item != null && ResolveItem(item.itemId) != null) &&
                death.carriedLoot.TrueForAll(item => item != null && ResolveItem(item.itemId) != null) &&
                (string.IsNullOrEmpty(death.combat.hairId) || ResolveHair(death.combat.hairId) != null);
            if (!resolvable || !player.TryRollDungeonMemory(death.id, floor))
                continue;

            if (Random.value < (death.floor == floor ? bodySpawnChance : laterFloorSpawnChance))
                return death;
        }
        return null;
    }

    private IEnumerator SpawnAfterMapRegistration(int floor, List<RoomInstance> rooms, Player player)
    {
        yield return null;

        if (player == null || player != Player.Instance || AStarManager.Instance == null)
            yield break;

        foreach (SavedDeath death in new List<SavedDeath>(archive.deaths))
        {
            if (death.outcome != DeathOutcome.Dead || death.floor > floor ||
                !player.TryRollDungeonMemory(death.id, floor))
                continue;

            if (Random.value >= (death.floor == floor ? bodySpawnChance : laterFloorSpawnChance))
                continue;

            if (TrySpawnBody(death, rooms))
            {
                archive.deaths.Remove(death);
                Save();
            }
        }
    }

    public List<SavedLoot> GetFallenLootPool(SavedDeath death)
    {
        List<SavedLoot> pool = new List<SavedLoot>();
        if (death.carriedLoot != null)
        {
            foreach (SavedLoot item in death.carriedLoot)
            {
                if (item == null || item.quantity <= 0)
                    continue;
                SavedLoot stack = pool.Find(entry => entry.itemId == item.itemId);
                if (stack == null)
                    pool.Add(new SavedLoot { itemId = item.itemId, quantity = item.quantity });
                else
                    stack.quantity += item.quantity;
            }
        }

        Dictionary<string, int> equippedCounts = new Dictionary<string, int>();
        if (death.combat?.equipment != null)
        {
            foreach (SavedEquipment item in death.combat.equipment)
            {
                if (item == null || item.fallbackWeapon || string.IsNullOrEmpty(item.itemId))
                    continue;
                equippedCounts.TryGetValue(item.itemId, out int count);
                equippedCounts[item.itemId] = count + 1;
            }
        }
        foreach (KeyValuePair<string, int> equipped in equippedCounts)
        {
            SavedLoot stack = pool.Find(entry => entry.itemId == equipped.Key);
            if (stack == null)
                pool.Add(new SavedLoot { itemId = equipped.Key, quantity = equipped.Value });
            else
                stack.quantity = Mathf.Max(stack.quantity, equipped.Value);
        }
        return pool;
    }

    public void CompleteFallenEncounter(SavedDeath death, RoomInstance room, Vector3 position)
    {
        List<InventoryItem> items = new List<InventoryItem>();
        foreach (SavedLoot loot in GetFallenLootPool(death))
        {
            ItemData item = ResolveItem(loot.itemId);
            if (item == null)
                return;
            items.Add(new InventoryItem(item, loot.quantity));
        }
        death.loot = CaptureLoot(items, 0.5f, 0.75f);
        death.outcome = DeathOutcome.Dead;
        if (archive.deaths.Contains(death))
            Save();
        if (TrySpawnBody(death, new List<RoomInstance> { room }, position))
        {
            archive.deaths.Remove(death);
            Save();
        }
    }

    private bool TrySpawnBody(SavedDeath death, List<RoomInstance> rooms, Vector3? preferredPosition = null)
    {
        Sprite sprite = death.gender == CharacterGender.Female ? femaleBodySprite : maleBodySprite;
        if (sprite == null || (death.loot.Count > 0 && BreakablePotManager.Instance == null))
        {
            Debug.LogWarning("DungeonMemory: Body sprite or BreakablePotManager missing; memory retained.", this);
            return false;
        }

        List<ItemData> items = new List<ItemData>();
        foreach (SavedLoot loot in death.loot)
        {
            ItemReference reference = itemCatalog.Find(entry => entry != null && entry.id == loot.itemId);
            if (reference == null || reference.item == null || loot.quantity <= 0)
            {
                Debug.LogWarning($"DungeonMemory: Cannot resolve saved loot {loot.itemId}; memory retained.", this);
                return false;
            }
            items.Add(reference.item);
        }

        int firstRoom = Random.Range(0, rooms.Count);
        for (int offset = 0; offset < rooms.Count; offset++)
        {
            RoomInstance room = rooms[(firstRoom + offset) % rooms.Count];
            if (room == null || AStarManager.Instance == null)
                continue;
            Vector3? position = preferredPosition.HasValue
                ? AStarManager.Instance.GetWalkableCellCenter(preferredPosition.Value) ?? FindBodyPosition(room)
                : FindBodyPosition(room);
            if (!position.HasValue)
                continue;

            GameObject root = new GameObject($"Remembered Body - {death.characterName}");
            root.transform.SetParent(room.transform, true);
            root.transform.position = position.Value;
            GameObject body = bodyPrefab != null
                ? Instantiate(bodyPrefab, position.Value, Quaternion.identity, root.transform)
                : root;
            SpriteRenderer renderer = body.GetComponentInChildren<SpriteRenderer>(true);
            if (renderer == null)
                renderer = body.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.enabled = true;
            Color bodyColor = renderer.color;
            bodyColor.a = 1f;
            renderer.color = bodyColor;
            body.SetActive(true);

            Tilemap map = AStarManager.Instance.GetWalkableTilemapAtPosition(position.Value);
            if (map != null && map.TryGetComponent(out TilemapRenderer mapRenderer))
            {
                renderer.sortingLayerID = mapRenderer.sortingLayerID;
                renderer.sortingOrder = mapRenderer.sortingOrder + 1;
                UnityEngine.Rendering.SortingGroup sortingGroup = body.GetComponentInChildren<UnityEngine.Rendering.SortingGroup>(true);
                if (sortingGroup != null)
                {
                    sortingGroup.sortingLayerID = mapRenderer.sortingLayerID;
                    sortingGroup.sortingOrder = mapRenderer.sortingOrder + 1;
                }
            }

            for (int index = 0; index < death.loot.Count; index++)
            {
                Vector3 origin = FindLootOrigin(position.Value, map);
                if (!BreakablePotManager.Instance.SpawnLoot(items[index], death.loot[index].quantity, origin, root.transform))
                {
                    root.SetActive(false);
                    Destroy(root);
                    return false;
                }
            }
            return true;
        }
        Debug.LogWarning("DungeonMemory: No registered walkable position was available for the corpse; memory retained.", this);
        return false;
    }

    private Vector3? FindBodyPosition(RoomInstance room)
    {
        if (room == null)
            return null;

        Vector3? chosen = null;
        int candidateCount = 0;
        foreach (AStarWalkableMap walkableMap in room.GetComponentsInChildren<AStarWalkableMap>())
        {
            Tilemap map = walkableMap.Tilemap;
            if (map == null)
                continue;

            foreach (Vector3Int cell in map.cellBounds.allPositionsWithin)
            {
                if (!map.HasTile(cell))
                    continue;

                Vector3 center = map.GetCellCenterWorld(cell);
                if (AStarManager.Instance.GetWalkableTilemapAtPosition(center) != map)
                    continue;

                candidateCount++;
                if (Random.Range(0, candidateCount) == 0)
                    chosen = center;
            }
        }
        return chosen;
    }

    private Vector3 FindLootOrigin(Vector3 bodyPosition, Tilemap map)
    {
        for (int attempt = 0; attempt < lootPlacementAttempts; attempt++)
        {
            Vector2 offset = Random.insideUnitCircle * lootScatterRadius;
            Vector3 candidate = bodyPosition + new Vector3(offset.x, offset.y, 0f);
            if (AStarManager.Instance.GetWalkableTilemapAtPosition(candidate) == map)
                return candidate;
        }
        return bodyPosition;
    }

    private void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(archive));
        PlayerPrefs.Save();
    }

#if UNITY_EDITOR
    private void Reset()
    {
        RefreshItemCatalog();
    }

    [ContextMenu("Refresh Item Catalog")]
    private void RefreshItemCatalog()
    {
        itemCatalog.Clear();
        foreach (string id in UnityEditor.AssetDatabase.FindAssets("t:ItemData", new[] { "Assets" }))
        {
            ItemData item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(
                UnityEditor.AssetDatabase.GUIDToAssetPath(id)
            );
            if (item != null)
                itemCatalog.Add(new ItemReference { id = id, item = item });
        }
        hairCatalog.Clear();
        foreach (string id in UnityEditor.AssetDatabase.FindAssets("t:CharacterPartDefinition", new[] { "Assets" }))
        {
            CharacterPartDefinition part = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterPartDefinition>(
                UnityEditor.AssetDatabase.GUIDToAssetPath(id)
            );
            if (part != null && part.Type == CharacterPartType.Hair)
                hairCatalog.Add(new PartReference { id = id, part = part });
        }
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}