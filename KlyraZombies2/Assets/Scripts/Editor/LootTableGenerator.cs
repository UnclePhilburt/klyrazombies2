#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Editor tool to generate loot tables for all LootablePlacer categories
/// </summary>
public class LootTableGenerator : EditorWindow
{
    // Mapping from LootablePlacer categories to UIS item categories
    private static readonly Dictionary<string, string[]> CATEGORY_MAPPING = new Dictionary<string, string[]>
    {
        { "Office", new[] { "Office", "Office Supplies", "Documents and Papers", "Electronics" } },
        { "Kitchen", new[] { "Food and Drink" } },
        { "Bedroom", new[] { "Valuables", "Electronics", "Office Misc" } },
        { "Garage", new[] { "Electronics", "Office Misc" } },
        { "Store", new[] { "Food and Drink", "Electronics", "Valuables" } },
        { "Medical", new[] { "Office Medical" } },
        { "Military", new[] { "Ammo" } }, // Special case - ammo only in military
        { "Police", new[] { "Electronics", "Office Misc", "Documents and Papers" } },
        { "Outdoor", new[] { "Food and Drink", "Electronics", "Office Misc" } },
        { "Bathroom", new[] { "Office Medical", "Office Misc" } },
        { "Misc", new[] { "Valuables", "Electronics", "Office Misc", "Food and Drink" } }
    };

    // Weight adjustments per loot table type
    private static readonly Dictionary<string, Dictionary<string, int>> WEIGHT_OVERRIDES = new Dictionary<string, Dictionary<string, int>>
    {
        { "Kitchen", new Dictionary<string, int> { { "Food and Drink", 20 } } },
        { "Medical", new Dictionary<string, int> { { "Office Medical", 25 } } },
        { "Office", new Dictionary<string, int> { { "Office Supplies", 15 }, { "Documents and Papers", 20 } } },
        { "Store", new Dictionary<string, int> { { "Food and Drink", 15 }, { "Valuables", 5 } } },
        { "Military", new Dictionary<string, int> { { "Ammo", 30 } } }
    };

    // Loot table settings per category
    private static readonly Dictionary<string, (int minItems, int maxItems, int emptyChance)> TABLE_SETTINGS = new Dictionary<string, (int, int, int)>
    {
        { "Office", (1, 4, 15) },
        { "Kitchen", (1, 3, 20) },
        { "Bedroom", (0, 2, 30) },
        { "Garage", (1, 4, 10) },
        { "Store", (2, 5, 5) },
        { "Medical", (1, 3, 15) },
        { "Military", (1, 2, 25) },
        { "Police", (1, 3, 20) },
        { "Outdoor", (1, 3, 25) },
        { "Bathroom", (0, 2, 40) },
        { "Misc", (1, 3, 20) }
    };

    [MenuItem("Project Klyra/Loot/Generate All Tables")]
    public static void GenerateAllLootTables()
    {
        // Ensure directory exists
        string dir = "Assets/Data/LootTables";
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // Load all UIS ItemCategories
        var categories = LoadItemCategories();

        // Load all ItemDefinitions
        var allItems = LoadAllItemDefinitions();

        int created = 0;
        int updated = 0;

        foreach (var mapping in CATEGORY_MAPPING)
        {
            string tableName = mapping.Key;
            string[] uisCategories = mapping.Value;

            string path = $"{dir}/LootTable_{tableName}.asset";

            LootTable table = AssetDatabase.LoadAssetAtPath<LootTable>(path);
            bool isNew = (table == null);

            if (isNew)
            {
                table = ScriptableObject.CreateInstance<LootTable>();
                table.tableName = tableName + " Loot";
            }
            else
            {
                Undo.RecordObject(table, "Update Loot Table");
                table.possibleItems.Clear();
            }

            // Apply settings
            if (TABLE_SETTINGS.TryGetValue(tableName, out var settings))
            {
                table.minItemTypes = settings.minItems;
                table.maxItemTypes = settings.maxItems;
                table.emptyChance = settings.emptyChance;
            }

            // Get weight overrides for this table
            WEIGHT_OVERRIDES.TryGetValue(tableName, out var weightOverrides);

            // Add items from each UIS category
            int itemsAdded = 0;
            foreach (string uisCatName in uisCategories)
            {
                if (!categories.TryGetValue(uisCatName, out var category))
                {
                    Debug.LogWarning($"[LootTableGenerator] Category not found: {uisCatName}");
                    continue;
                }

                // Get default weight or override
                int weight = 10;
                if (weightOverrides != null && weightOverrides.TryGetValue(uisCatName, out int overrideWeight))
                {
                    weight = overrideWeight;
                }

                // Find items in this category
                foreach (var item in allItems)
                {
                    if (IsItemInCategory(item, category))
                    {
                        // Skip weapons and backpacks (except for Military which can have ammo)
                        string itemName = item.name.ToLower();
                        if (tableName != "Military")
                        {
                            if (itemName.Contains("weapon") || itemName.Contains("gun") ||
                                itemName.Contains("rifle") || itemName.Contains("pistol") ||
                                itemName.Contains("backpack"))
                                continue;
                        }

                        // Check if already added
                        bool exists = table.possibleItems.Any(e => e.itemDefinition == item);
                        if (exists) continue;

                        // Determine amount based on item type
                        int minAmt = 1;
                        int maxAmt = 1;

                        // Stackable items get higher amounts
                        if (itemName.Contains("round") || itemName.Contains("ammo") || itemName.Contains("bullet"))
                        {
                            minAmt = 5;
                            maxAmt = 20;
                        }
                        else if (itemName.Contains("bandage") || itemName.Contains("pill") || itemName.Contains("cash"))
                        {
                            minAmt = 1;
                            maxAmt = 3;
                        }

                        table.possibleItems.Add(new LootTable.LootEntry
                        {
                            itemDefinition = item,
                            weight = weight,
                            minAmount = minAmt,
                            maxAmount = maxAmt
                        });
                        itemsAdded++;
                    }
                }
            }

            // Save
            if (isNew)
            {
                AssetDatabase.CreateAsset(table, path);
                created++;
            }
            else
            {
                EditorUtility.SetDirty(table);
                updated++;
            }

            Debug.Log($"[LootTableGenerator] {tableName}: {itemsAdded} items");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Loot Tables Generated",
            $"Created: {created}\nUpdated: {updated}\n\nCheck Assets/Data/LootTables/",
            "OK");
    }

    private static Dictionary<string, ScriptableObject> LoadItemCategories()
    {
        var result = new Dictionary<string, ScriptableObject>();

        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject",
            new[] { "Assets/Data/InventoryDatabase/InventoryDatabase/ItemCategories" });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var category = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (category != null)
            {
                result[category.name] = category;
            }
        }

        return result;
    }

    private static List<ScriptableObject> LoadAllItemDefinitions()
    {
        var result = new List<ScriptableObject>();

        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject",
            new[] { "Assets/Data/InventoryDatabase/InventoryDatabase/ItemDefinitions" });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetDatabase.IsValidFolder(path)) continue;

            var item = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (item != null)
            {
                // Verify it's an ItemDefinition by checking for m_Category
                var so = new SerializedObject(item);
                if (so.FindProperty("m_Category") != null)
                {
                    result.Add(item);
                }
            }
        }

        return result;
    }

    private static bool IsItemInCategory(ScriptableObject item, ScriptableObject targetCategory)
    {
        var so = new SerializedObject(item);
        var categoryProp = so.FindProperty("m_Category");

        if (categoryProp == null || categoryProp.objectReferenceValue == null)
            return false;

        // Direct match
        if (categoryProp.objectReferenceValue == targetCategory)
            return true;

        // Check parent category (for hierarchical categories)
        var itemCategory = categoryProp.objectReferenceValue as ScriptableObject;
        if (itemCategory != null)
        {
            var catSo = new SerializedObject(itemCategory);
            var parentProp = catSo.FindProperty("m_ParentCategory");
            if (parentProp != null && parentProp.objectReferenceValue == targetCategory)
                return true;
        }

        return false;
    }

    [MenuItem("Project Klyra/Loot/Generate Zombie Table")]
    public static void GenerateZombieLootTable()
    {
        string dir = "Assets/Data/LootTables";
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string path = $"{dir}/ZombieLootTable.asset";

        LootTable table = AssetDatabase.LoadAssetAtPath<LootTable>(path);
        bool isNew = (table == null);

        if (isNew)
        {
            table = ScriptableObject.CreateInstance<LootTable>();
            table.tableName = "Zombie Corpse";
        }
        else
        {
            Undo.RecordObject(table, "Update Zombie Loot Table");
            table.possibleItems.Clear();
        }

        // Zombie loot settings - sparse loot, mostly empty
        table.minItemTypes = 0;
        table.maxItemTypes = 2;
        table.emptyChance = 60; // 60% chance of no loot

        // Load all ItemDefinitions
        var allItems = LoadAllItemDefinitions();

        // Zombie-appropriate items (common stuff they might have had)
        string[] zombieLootKeywords = new string[]
        {
            "cash", "watch", "keys", "wallet", "lighter", "matches", "cigarette",
            "bandage", "pill", "painkiller", "phone", "smartphone",
            "flashlight", "battery", "id", "card"
        };

        // Rare weapon drops - zombies might have been armed survivors
        string[] rareWeaponDrops = new string[]
        {
            "sr-9", "ak-47", "9mm rounds", "7.62mm rounds"
        };

        // Weight map - higher = more common
        Dictionary<string, int> weightMap = new Dictionary<string, int>
        {
            // Common items
            { "cash", 15 },
            { "watch", 5 },
            { "keys", 10 },
            { "lighter", 8 },
            { "cigarette", 10 },
            { "bandage", 5 },
            { "pill", 3 },
            { "painkiller", 3 },
            { "phone", 5 },
            { "smartphone", 5 },
            { "flashlight", 3 },
            { "battery", 8 },
            { "matches", 8 },
            // Rare weapon drops (low weight = rare)
            { "sr-9", 2 },      // ~1-2% chance
            { "ak-47", 1 },     // ~0.5-1% chance (very rare)
            { "9mm", 3 },       // Ammo slightly more common than guns
            { "7.62", 2 }
        };

        int itemsAdded = 0;
        foreach (var item in allItems)
        {
            string itemName = item.name.ToLower();
            bool shouldAdd = false;
            int weight = 10;
            int minAmt = 1;
            int maxAmt = 1;

            // Check common loot keywords
            foreach (string keyword in zombieLootKeywords)
            {
                if (itemName.Contains(keyword))
                {
                    // Skip backpacks
                    if (itemName.Contains("backpack"))
                        continue;

                    shouldAdd = true;

                    // Get weight
                    foreach (var kv in weightMap)
                    {
                        if (itemName.Contains(kv.Key))
                        {
                            weight = kv.Value;
                            break;
                        }
                    }

                    // Determine amounts
                    if (itemName.Contains("cash"))
                    {
                        minAmt = 1;
                        maxAmt = 5;
                    }
                    else if (itemName.Contains("cigarette"))
                    {
                        minAmt = 1;
                        maxAmt = 3;
                    }
                    else if (itemName.Contains("battery"))
                    {
                        minAmt = 1;
                        maxAmt = 2;
                    }
                    break;
                }
            }

            // Check rare weapon/ammo drops
            if (!shouldAdd)
            {
                foreach (string rareItem in rareWeaponDrops)
                {
                    if (itemName.Contains(rareItem.Replace("-", "").Replace(" ", "")) ||
                        itemName.Replace("-", "").Replace(" ", "").Contains(rareItem.Replace("-", "").Replace(" ", "")))
                    {
                        shouldAdd = true;

                        // Get weight for rare items
                        foreach (var kv in weightMap)
                        {
                            if (itemName.Contains(kv.Key.Replace("-", "")))
                            {
                                weight = kv.Value;
                                break;
                            }
                        }

                        // Ammo amounts
                        if (itemName.Contains("round") || itemName.Contains("ammo") || itemName.Contains("mm"))
                        {
                            minAmt = 5;
                            maxAmt = 15;
                        }
                        break;
                    }
                }
            }

            // Also check exact name matches for weapons
            if (!shouldAdd)
            {
                if (itemName == "sr-9" || itemName == "ak-47" ||
                    itemName == "9mm rounds" || itemName == "7.62mm rounds")
                {
                    shouldAdd = true;
                    if (itemName == "sr-9") weight = 2;
                    else if (itemName == "ak-47") weight = 1;
                    else if (itemName.Contains("9mm")) { weight = 3; minAmt = 5; maxAmt = 15; }
                    else if (itemName.Contains("7.62")) { weight = 2; minAmt = 5; maxAmt = 15; }
                }
            }

            if (!shouldAdd) continue;

            // Check if already added
            bool exists = table.possibleItems.Any(e => e.itemDefinition == item);
            if (exists) continue;

            table.possibleItems.Add(new LootTable.LootEntry
            {
                itemDefinition = item,
                weight = weight,
                minAmount = minAmt,
                maxAmount = maxAmt
            });
            itemsAdded++;
        }

        // Save
        if (isNew)
        {
            AssetDatabase.CreateAsset(table, path);
        }
        else
        {
            EditorUtility.SetDirty(table);
        }

        // Also copy to Resources for runtime loading
        string resourcesDir = "Assets/Resources/LootTables";
        if (!Directory.Exists(resourcesDir))
        {
            Directory.CreateDirectory(resourcesDir);
        }

        // Create a copy in Resources
        LootTable resourceTable = AssetDatabase.LoadAssetAtPath<LootTable>($"{resourcesDir}/ZombieLootTable.asset");
        if (resourceTable == null)
        {
            resourceTable = ScriptableObject.CreateInstance<LootTable>();
            AssetDatabase.CreateAsset(resourceTable, $"{resourcesDir}/ZombieLootTable.asset");
        }

        EditorUtility.CopySerialized(table, resourceTable);
        EditorUtility.SetDirty(resourceTable);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Zombie Loot Table Generated",
            $"Added {itemsAdded} items to zombie loot table.\n\nLocation: {path}\nAlso copied to Resources for runtime loading.",
            "OK");
    }

    [MenuItem("Project Klyra/Loot/List Items by Category")]
    public static void ListItemsByCategory()
    {
        var categories = LoadItemCategories();
        var items = LoadAllItemDefinitions();

        Debug.Log("=== ITEMS BY CATEGORY ===");

        foreach (var cat in categories.OrderBy(c => c.Key))
        {
            var catItems = items.Where(i => IsItemInCategory(i, cat.Value)).ToList();
            if (catItems.Count > 0)
            {
                Debug.Log($"\n{cat.Key} ({catItems.Count} items):");
                foreach (var item in catItems.OrderBy(i => i.name))
                {
                    Debug.Log($"  - {item.name}");
                }
            }
        }
    }
}
#endif
