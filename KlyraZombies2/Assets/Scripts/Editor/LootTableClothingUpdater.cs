using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Adds clothing items to loot tables after they've been imported by Unity.
/// Run from: Tools > Inventory > Add Clothing to Loot Tables
/// </summary>
public class LootTableClothingUpdater : EditorWindow
{
    private const string CLOTHING_FOLDER = "Assets/Data/InventoryDatabase/InventoryDatabase/ItemDefinitions/Clothing";
    private const string LOOT_TABLE_PATH = "Assets/Data/LootTables/LootTable_Bedroom.asset";
    private const string LOOT_TABLE_SCRIPT_GUID = "782064d13caaf42fd96e0f140886bd08";

    [MenuItem("Tools/Inventory/Add Clothing to Loot Tables")]
    public static void AddClothingToLootTables()
    {
        // Get all clothing item GUIDs
        string[] assetFiles = Directory.GetFiles(CLOTHING_FOLDER, "*.asset", SearchOption.TopDirectoryOnly);

        if (assetFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("No Items", "No clothing items found in:\n" + CLOTHING_FOLDER, "OK");
            return;
        }

        List<string> clothingGuids = new List<string>();
        foreach (string assetPath in assetFiles)
        {
            if (assetPath.EndsWith(".meta")) continue;

            string relativePath = assetPath.Replace(Application.dataPath.Replace("/Assets", "/"), "");
            string guid = AssetDatabase.AssetPathToGUID(relativePath);

            if (!string.IsNullOrEmpty(guid))
            {
                clothingGuids.Add(guid);
                Debug.Log($"[LootTableUpdater] Found: {Path.GetFileNameWithoutExtension(assetPath)} -> {guid}");
            }
        }

        if (clothingGuids.Count == 0)
        {
            EditorUtility.DisplayDialog("No GUIDs", "Unity hasn't imported the clothing assets yet.\n\nPlease wait for Unity to finish importing, then try again.", "OK");
            return;
        }

        // Read the loot table
        if (!File.Exists(LOOT_TABLE_PATH))
        {
            EditorUtility.DisplayDialog("Error", "Loot table not found:\n" + LOOT_TABLE_PATH, "OK");
            return;
        }

        string lootTableContent = File.ReadAllText(LOOT_TABLE_PATH);

        // Check if clothing items are already in the table
        int alreadyPresent = 0;
        foreach (string guid in clothingGuids)
        {
            if (lootTableContent.Contains(guid))
            {
                alreadyPresent++;
            }
        }

        if (alreadyPresent == clothingGuids.Count)
        {
            EditorUtility.DisplayDialog("Already Added", $"All {clothingGuids.Count} clothing items are already in the loot table.", "OK");
            return;
        }

        // Build new entries for items not already in the table
        System.Text.StringBuilder newEntries = new System.Text.StringBuilder();
        int addedCount = 0;

        foreach (string guid in clothingGuids)
        {
            if (!lootTableContent.Contains(guid))
            {
                newEntries.AppendLine($"  - itemDefinition: {{fileID: 11400000, guid: {guid}, type: 2}}");
                newEntries.AppendLine("    weight: 30");
                newEntries.AppendLine("    minAmount: 1");
                newEntries.AppendLine("    maxAmount: 1");
                addedCount++;
            }
        }

        // Find the end of possibleItems array and insert before the final empty line
        // The possibleItems ends before any other property or end of file
        int insertIndex = lootTableContent.LastIndexOf("    maxAmount: 1");
        if (insertIndex == -1)
        {
            EditorUtility.DisplayDialog("Error", "Could not find insertion point in loot table.", "OK");
            return;
        }

        insertIndex = lootTableContent.IndexOf('\n', insertIndex) + 1;

        string newContent = lootTableContent.Insert(insertIndex, newEntries.ToString());
        File.WriteAllText(LOOT_TABLE_PATH, newContent);

        AssetDatabase.Refresh();

        string message = $"Added {addedCount} clothing items to LootTable_Bedroom.\n" +
                        $"({alreadyPresent} items were already present)";
        Debug.Log($"[LootTableUpdater] {message}");
        EditorUtility.DisplayDialog("Loot Table Updated", message, "OK");
    }
}
