using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Generates separate Shirt and Pants items with unique names.
/// The SidekickClothingEquipHandler uses a lookup table to map these to presets.
/// Run from: Tools > Inventory > Generate Separate Clothing Items
/// </summary>
public class SeparateClothingGenerator : EditorWindow
{
    private const string CLOTHING_FOLDER = "Assets/Data/InventoryDatabase/InventoryDatabase/ItemDefinitions/Clothing";
    private const string SHIRT_CATEGORY_GUID = "f509666376c26488bbbb80b8d4a540c9";
    private const string PANTS_CATEGORY_GUID = "479ced862ae1444e0bfd5e602e982ba2";
    private const string ITEM_DEF_SCRIPT_GUID = "53ac5fa5da8102b468a92c21620db0d0";
    private const string ARMOR_ICON_GUID = "3f185c73ff4c87c4a88e1727c3c77b45";

    // Survivor clothing
    private static readonly (string shirtName, string pantsName)[] SURVIVOR_ITEMS = {
        ("Survivor Jacket", "Survivor Cargo Pants"),
        ("Survivor Hoodie", "Survivor Jeans"),
        ("Survivor Vest", "Survivor Work Pants"),
        ("Survivor T-Shirt", "Survivor Shorts"),
        ("Survivor Flannel", "Survivor Khakis"),
    };

    // Outlaw clothing
    private static readonly (string shirtName, string pantsName)[] OUTLAW_ITEMS = {
        ("Raider Jacket", "Raider Pants"),
        ("Scavenger Vest", "Scavenger Cargos"),
        ("Bandit Hoodie", "Bandit Jeans"),
        ("Wasteland Coat", "Wasteland Trousers"),
        ("Road Warrior Top", "Road Warrior Pants"),
        ("Marauder Jacket", "Marauder Cargos"),
        ("Punk Vest", "Punk Pants"),
        ("Biker Jacket", "Biker Jeans"),
        ("Nomad Shirt", "Nomad Pants"),
        ("Drifter Coat", "Drifter Trousers"),
    };

    [MenuItem("Tools/Inventory/Generate Separate Clothing Items")]
    public static void GenerateSeparateClothing()
    {
        if (!Directory.Exists(CLOTHING_FOLDER))
        {
            Directory.CreateDirectory(CLOTHING_FOLDER);
        }

        DeleteOldClothingItems();

        int shirtCount = 0;
        int pantsCount = 0;

        foreach (var item in SURVIVOR_ITEMS)
        {
            CreateClothingItem(item.shirtName, SHIRT_CATEGORY_GUID);
            shirtCount++;
            CreateClothingItem(item.pantsName, PANTS_CATEGORY_GUID);
            pantsCount++;
        }

        foreach (var item in OUTLAW_ITEMS)
        {
            CreateClothingItem(item.shirtName, SHIRT_CATEGORY_GUID);
            shirtCount++;
            CreateClothingItem(item.pantsName, PANTS_CATEGORY_GUID);
            pantsCount++;
        }

        AssetDatabase.Refresh();

        string message = $"Generated {shirtCount} Shirts and {pantsCount} Pants.\n\nRemember to add them to loot tables!";
        Debug.Log($"[SeparateClothingGenerator] {message}");
        EditorUtility.DisplayDialog("Clothing Generated", message, "OK");
    }

    private static void DeleteOldClothingItems()
    {
        if (!Directory.Exists(CLOTHING_FOLDER)) return;

        string[] files = Directory.GetFiles(CLOTHING_FOLDER, "*.asset");
        foreach (var file in files)
        {
            File.Delete(file);
            if (File.Exists(file + ".meta")) File.Delete(file + ".meta");
        }
        if (files.Length > 0)
            Debug.Log($"[SeparateClothingGenerator] Deleted {files.Length} old items");
    }

    private static void CreateClothingItem(string displayName, string categoryGuid)
    {
        string filePath = Path.Combine(CLOTHING_FOLDER, $"{displayName}.asset");
        uint itemId = (uint)(displayName.GetHashCode() & 0x7FFFFFFF);
        uint defaultItemId = (uint)((displayName + "_def").GetHashCode() & 0x7FFFFFFF);

        string content = $@"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {ITEM_DEF_SCRIPT_GUID}, type: 3}}
  m_Name: {displayName}
  m_EditorClassIdentifier: Opsive.UltimateInventorySystem::Opsive.UltimateInventorySystem.Core.ItemDefinition
  m_ID: {itemId}
  m_Category: {{fileID: 11400000, guid: {categoryGuid}, type: 2}}
  m_Parent: {{fileID: 0}}
  m_ChildrenData:
    m_ObjectType:
    m_ValueHashes:
    m_LongValueHashes:
    m_ValuePositions:
    m_Values:
    m_UnityObjects: []
    m_Version:
  m_ItemDefinitionAttributeCollection:
    m_AttributeCollectionData: []
  m_DefaultItem:
    m_ID: {defaultItemId}
    m_Name:
    m_ItemDefinitionID: 0
    m_ItemDefinition: {{fileID: 0}}
    m_ItemAttributeCollection:
      m_AttributeCollectionData: []
  m_EditorIcon: {{fileID: 21300000, guid: {ARMOR_ICON_GUID}, type: 3}}
";

        File.WriteAllText(filePath, content);
        Debug.Log($"[SeparateClothingGenerator] Created: {displayName}");
    }
}
