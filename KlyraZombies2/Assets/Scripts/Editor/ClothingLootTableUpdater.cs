using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Opsive.UltimateInventorySystem.Core;

/// <summary>
/// Editor tool to add clothing items to loot tables.
/// Only adds apocalypse-appropriate clothing (Survivors, Outlaws, Modern Civilians).
/// </summary>
public class ClothingLootTableUpdater : EditorWindow
{
    private LootTable m_BedroomTable;
    private LootTable m_StoreTable;
    private int m_ClothingWeight = 5; // Lower weight than regular items
    private bool m_IncludeSurvivors = true;
    private bool m_IncludeOutlaws = true;
    private bool m_IncludeCivilians = true;

    [MenuItem("Project Klyra/Loot/Add Clothing to Loot Tables")]
    public static void ShowWindow()
    {
        GetWindow<ClothingLootTableUpdater>("Clothing Loot Updater");
    }

    private void OnEnable()
    {
        LoadLootTables();
    }

    private void LoadLootTables()
    {
        // Find loot tables
        string[] guids = AssetDatabase.FindAssets("t:LootTable");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            LootTable table = AssetDatabase.LoadAssetAtPath<LootTable>(path);
            if (table == null) continue;

            if (table.name.Contains("Bedroom"))
                m_BedroomTable = table;
            else if (table.name.Contains("Store"))
                m_StoreTable = table;
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Add Clothing to Loot Tables", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "This tool adds apocalypse-appropriate clothing items to loot tables.\n" +
            "Clothing includes Sidekick presets that change character appearance when equipped.",
            MessageType.Info);

        GUILayout.Space(10);

        m_BedroomTable = (LootTable)EditorGUILayout.ObjectField("Bedroom Loot Table", m_BedroomTable, typeof(LootTable), false);
        m_StoreTable = (LootTable)EditorGUILayout.ObjectField("Store Loot Table", m_StoreTable, typeof(LootTable), false);

        GUILayout.Space(10);

        GUILayout.Label("Clothing Types to Add:", EditorStyles.boldLabel);
        m_IncludeSurvivors = EditorGUILayout.Toggle("Apocalypse Survivors", m_IncludeSurvivors);
        m_IncludeOutlaws = EditorGUILayout.Toggle("Apocalypse Outlaws", m_IncludeOutlaws);
        m_IncludeCivilians = EditorGUILayout.Toggle("Modern Civilians", m_IncludeCivilians);

        GUILayout.Space(5);
        m_ClothingWeight = EditorGUILayout.IntSlider("Drop Weight", m_ClothingWeight, 1, 20);

        GUILayout.Space(20);

        if (GUILayout.Button("Add Clothing to Both Tables", GUILayout.Height(30)))
        {
            AddClothingToTables();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Remove All Clothing from Tables"))
        {
            RemoveClothingFromTables();
        }
    }

    private void AddClothingToTables()
    {
        // Find all clothing item definitions
        List<ItemDefinition> clothingItems = FindClothingItems();

        if (clothingItems.Count == 0)
        {
            EditorUtility.DisplayDialog("No Items Found",
                "No apocalypse-appropriate clothing items found in the Clothing folder.",
                "OK");
            return;
        }

        int addedToBedroom = 0;
        int addedToStore = 0;

        // Add to bedroom table
        if (m_BedroomTable != null)
        {
            addedToBedroom = AddItemsToTable(m_BedroomTable, clothingItems);
            EditorUtility.SetDirty(m_BedroomTable);
        }

        // Add to store table
        if (m_StoreTable != null)
        {
            addedToStore = AddItemsToTable(m_StoreTable, clothingItems);
            EditorUtility.SetDirty(m_StoreTable);
        }

        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Clothing Added",
            $"Added {addedToBedroom} items to Bedroom table\n" +
            $"Added {addedToStore} items to Store table\n" +
            $"(Duplicates were skipped)",
            "OK");
    }

    private List<ItemDefinition> FindClothingItems()
    {
        List<ItemDefinition> items = new List<ItemDefinition>();

        // Search in the Clothing folder
        string clothingPath = "Assets/Data/InventoryDatabase/InventoryDatabase/ItemDefinitions/Clothing";
        string[] guids = AssetDatabase.FindAssets("t:ItemDefinition", new[] { clothingPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item == null) continue;

            string name = item.name;

            // Filter for apocalypse-appropriate items only
            bool include = false;
            if (m_IncludeSurvivors && name.Contains("Apocalypse Survivor")) include = true;
            if (m_IncludeOutlaws && name.Contains("Apocalypse Outlaws")) include = true;
            if (m_IncludeCivilians && name.Contains("Modern Civilians")) include = true;

            if (include)
            {
                items.Add(item);
            }
        }

        Debug.Log($"[ClothingLootUpdater] Found {items.Count} apocalypse-appropriate clothing items");
        return items;
    }

    private int AddItemsToTable(LootTable table, List<ItemDefinition> items)
    {
        int added = 0;

        // Get existing item GUIDs to avoid duplicates
        HashSet<string> existingGuids = new HashSet<string>();
        if (table.possibleItems != null)
        {
            foreach (var entry in table.possibleItems)
            {
                if (entry.itemDefinition != null)
                {
                    string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(entry.itemDefinition));
                    existingGuids.Add(guid);
                }
            }
        }

        // Initialize possibleItems if null
        if (table.possibleItems == null)
        {
            table.possibleItems = new List<LootTable.LootEntry>();
        }

        foreach (var item in items)
        {
            string itemGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(item));
            if (existingGuids.Contains(itemGuid))
            {
                continue; // Skip duplicates
            }

            LootTable.LootEntry entry = new LootTable.LootEntry
            {
                itemDefinition = item,
                weight = m_ClothingWeight,
                minAmount = 1,
                maxAmount = 1
            };

            table.possibleItems.Add(entry);
            existingGuids.Add(itemGuid);
            added++;
        }

        return added;
    }

    private void RemoveClothingFromTables()
    {
        int removedFromBedroom = 0;
        int removedFromStore = 0;

        if (m_BedroomTable != null)
        {
            removedFromBedroom = RemoveClothingFromTable(m_BedroomTable);
            EditorUtility.SetDirty(m_BedroomTable);
        }

        if (m_StoreTable != null)
        {
            removedFromStore = RemoveClothingFromTable(m_StoreTable);
            EditorUtility.SetDirty(m_StoreTable);
        }

        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Clothing Removed",
            $"Removed {removedFromBedroom} items from Bedroom table\n" +
            $"Removed {removedFromStore} items from Store table",
            "OK");
    }

    private int RemoveClothingFromTable(LootTable table)
    {
        if (table.possibleItems == null) return 0;

        int originalCount = table.possibleItems.Count;

        // Remove items that are in the Clothing folder
        table.possibleItems.RemoveAll(entry =>
        {
            if (entry.itemDefinition == null) return false;
            string path = AssetDatabase.GetAssetPath(entry.itemDefinition);
            return path.Contains("ItemDefinitions/Clothing/");
        });

        return originalCount - table.possibleItems.Count;
    }
}
