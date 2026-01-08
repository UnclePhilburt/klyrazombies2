using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Loot Table", menuName = "Klyra/Loot Table")]
public class LootTable : ScriptableObject
{
    [System.Serializable]
    public class LootEntry
    {
        [Tooltip("Reference to the UIS ItemDefinition")]
        public ScriptableObject itemDefinition;

        [Tooltip("How likely this item is to spawn (higher = more common)")]
        [Range(1, 100)]
        public int weight = 10;

        [Tooltip("Minimum amount if this item is selected")]
        public int minAmount = 1;

        [Tooltip("Maximum amount if this item is selected")]
        public int maxAmount = 1;
    }

    [Header("Loot Table Settings")]
    [Tooltip("Display name for this loot table")]
    public string tableName = "Generic Loot";

    [Tooltip("Minimum number of item types to spawn")]
    [Range(0, 10)]
    public int minItemTypes = 1;

    [Tooltip("Maximum number of item types to spawn")]
    [Range(1, 20)]
    public int maxItemTypes = 3;

    [Tooltip("Chance that this container has nothing (0-100)")]
    [Range(0, 100)]
    public int emptyChance = 10;

    [Header("Possible Items")]
    public List<LootEntry> possibleItems = new List<LootEntry>();

    /// <summary>
    /// Rolls random loot from this table
    /// </summary>
    /// <returns>List of (ItemDefinition, amount) tuples</returns>
    public List<(ScriptableObject item, int amount)> RollLoot()
    {
        var result = new List<(ScriptableObject, int)>();

        // Check for empty container
        if (Random.Range(0, 100) < emptyChance)
        {
            return result;
        }

        if (possibleItems.Count == 0)
        {
            return result;
        }

        // Determine how many item types to spawn
        int numTypes = Random.Range(minItemTypes, maxItemTypes + 1);

        // Calculate total weight
        int totalWeight = 0;
        foreach (var entry in possibleItems)
        {
            if (entry.itemDefinition != null)
                totalWeight += entry.weight;
        }

        if (totalWeight == 0) return result;

        // Keep track of what we've already added to avoid duplicates
        HashSet<ScriptableObject> addedItems = new HashSet<ScriptableObject>();

        // Roll for each item type
        int attempts = 0;
        int maxAttempts = numTypes * 3; // Prevent infinite loop

        while (result.Count < numTypes && attempts < maxAttempts)
        {
            attempts++;

            // Roll random weight
            int roll = Random.Range(0, totalWeight);
            int currentWeight = 0;

            foreach (var entry in possibleItems)
            {
                if (entry.itemDefinition == null) continue;

                currentWeight += entry.weight;
                if (roll < currentWeight)
                {
                    // Skip if already added
                    if (addedItems.Contains(entry.itemDefinition))
                        break;

                    // Add this item
                    int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);
                    result.Add((entry.itemDefinition, amount));
                    addedItems.Add(entry.itemDefinition);
                    break;
                }
            }
        }

        return result;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(LootTable))]
public class LootTableEditor : Editor
{
    private ScriptableObject categoryToAdd;
    private int defaultWeight = 10;
    private bool showDropChances = false;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LootTable table = (LootTable)target;

        EditorGUILayout.Space(10);

        // Auto-populate section
        EditorGUILayout.LabelField("Auto-Populate", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        categoryToAdd = (ScriptableObject)EditorGUILayout.ObjectField(
            "From Category",
            categoryToAdd,
            typeof(ScriptableObject),
            false
        );

        defaultWeight = EditorGUILayout.IntSlider("Default Weight", defaultWeight, 1, 50);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Add Items from Category"))
        {
            AddItemsFromCategory(table);
        }

        if (GUILayout.Button("Add ALL Items"))
        {
            AddAllItems(table);
        }

        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Clear All Items"))
        {
            if (EditorUtility.DisplayDialog("Clear Loot Table",
                "Are you sure you want to remove all items from this loot table?",
                "Yes", "Cancel"))
            {
                Undo.RecordObject(table, "Clear Loot Table");
                table.possibleItems.Clear();
                EditorUtility.SetDirty(table);
            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Loot Preview", EditorStyles.boldLabel);

        if (GUILayout.Button("Test Roll Loot"))
        {
            var loot = table.RollLoot();
            if (loot.Count == 0)
            {
                Debug.Log($"[{table.tableName}] Container is empty!");
            }
            else
            {
                string result = $"[{table.tableName}] Rolled {loot.Count} items:\n";
                foreach (var (item, amount) in loot)
                {
                    result += $"  - {item.name} x{amount}\n";
                }
                Debug.Log(result);
            }
        }

        // Show weight percentages (collapsible)
        if (table.possibleItems.Count > 0)
        {
            EditorGUILayout.Space(5);
            showDropChances = EditorGUILayout.Foldout(showDropChances, $"Drop Chances ({table.possibleItems.Count} items)");

            if (showDropChances)
            {
                int totalWeight = 0;
                foreach (var entry in table.possibleItems)
                {
                    if (entry.itemDefinition != null)
                        totalWeight += entry.weight;
                }

                if (totalWeight > 0)
                {
                    foreach (var entry in table.possibleItems)
                    {
                        if (entry.itemDefinition != null)
                        {
                            float chance = (entry.weight / (float)totalWeight) * 100f;
                            EditorGUILayout.LabelField($"  {entry.itemDefinition.name}: {chance:F1}%", EditorStyles.miniLabel);
                        }
                    }
                }
            }
        }
    }

    private void AddItemsFromCategory(LootTable table)
    {
        if (categoryToAdd == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a category first.", "OK");
            return;
        }

        // Get category name
        string categoryName = categoryToAdd.name;

        // Find all ItemDefinitions
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject",
            new[] { "Assets/Data/InventoryDatabase/InventoryDatabase/ItemDefinitions" });

        int added = 0;
        Undo.RecordObject(table, "Add Items from Category");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var itemDef = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

            if (itemDef == null) continue;

            // Check if it's an ItemDefinition by looking for category property
            var serializedItem = new SerializedObject(itemDef);
            var categoryProp = serializedItem.FindProperty("m_Category");

            if (categoryProp != null && categoryProp.objectReferenceValue == categoryToAdd)
            {
                // Check if already in table
                bool exists = false;
                foreach (var entry in table.possibleItems)
                {
                    if (entry.itemDefinition == itemDef)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    table.possibleItems.Add(new LootTable.LootEntry
                    {
                        itemDefinition = itemDef,
                        weight = defaultWeight,
                        minAmount = 1,
                        maxAmount = 1
                    });
                    added++;
                }
            }
        }

        EditorUtility.SetDirty(table);
        EditorUtility.DisplayDialog("Items Added", $"Added {added} items from category '{categoryName}'", "OK");
    }

    private void AddAllItems(LootTable table)
    {
        // Find all ItemDefinitions
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject",
            new[] { "Assets/Data/InventoryDatabase/InventoryDatabase/ItemDefinitions" });

        int added = 0;
        Undo.RecordObject(table, "Add All Items");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // Skip directories
            if (AssetDatabase.IsValidFolder(path)) continue;

            var itemDef = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (itemDef == null) continue;

            // Check if it's an ItemDefinition (has m_Category property)
            var serializedItem = new SerializedObject(itemDef);
            var categoryProp = serializedItem.FindProperty("m_Category");
            if (categoryProp == null) continue;

            // Skip weapons and ammo
            if (categoryProp.objectReferenceValue != null)
            {
                string catName = categoryProp.objectReferenceValue.name.ToLower();
                if (catName.Contains("weapon") || catName.Contains("ammo") || catName.Contains("backpack"))
                    continue;
            }

            // Check if already in table
            bool exists = false;
            foreach (var entry in table.possibleItems)
            {
                if (entry.itemDefinition == itemDef)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                table.possibleItems.Add(new LootTable.LootEntry
                {
                    itemDefinition = itemDef,
                    weight = defaultWeight,
                    minAmount = 1,
                    maxAmount = 1
                });
                added++;
            }
        }

        EditorUtility.SetDirty(table);
        EditorUtility.DisplayDialog("Items Added", $"Added {added} items to loot table\n(Weapons, ammo, and backpacks were skipped)", "OK");
    }
}
#endif
