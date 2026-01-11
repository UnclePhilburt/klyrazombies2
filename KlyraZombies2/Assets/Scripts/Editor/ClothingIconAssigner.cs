using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Assigns icons to clothing items in the ItemIconDatabase.
/// Run from: Tools > Inventory > Assign Clothing Icons
/// </summary>
public class ClothingIconAssigner : EditorWindow
{
    private const string CLOTHING_FOLDER = "Assets/Data/InventoryDatabase/InventoryDatabase/ItemDefinitions/Clothing";
    private const string ICON_DATABASE_PATH = "Assets/Resources/ItemIconDatabase.asset";
    private const string ARMOR_ICON_PATH = "Assets/Synty/InterfaceApocalypseHUD/Sprites/Icons_Inventory/ICON_Apocalpyse_Inventory_Armor_01.png";

    [MenuItem("Tools/Inventory/Assign Clothing Icons")]
    public static void AssignClothingIcons()
    {
        // Load the icon
        var armorIcon = AssetDatabase.LoadAssetAtPath<Sprite>(ARMOR_ICON_PATH);
        if (armorIcon == null)
        {
            // Try to get it as texture and find the sprite
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ARMOR_ICON_PATH);
            if (tex != null)
            {
                var sprites = AssetDatabase.LoadAllAssetsAtPath(ARMOR_ICON_PATH);
                foreach (var obj in sprites)
                {
                    if (obj is Sprite s)
                    {
                        armorIcon = s;
                        break;
                    }
                }
            }
        }

        if (armorIcon == null)
        {
            Debug.LogError("[ClothingIconAssigner] Could not find armor icon at: " + ARMOR_ICON_PATH);
            return;
        }

        // Load or create the ItemIconDatabase
        var iconDb = AssetDatabase.LoadAssetAtPath<ItemIconDatabase>(ICON_DATABASE_PATH);
        if (iconDb == null)
        {
            Debug.LogError("[ClothingIconAssigner] ItemIconDatabase not found at: " + ICON_DATABASE_PATH);
            return;
        }

        // Find all clothing items
        var clothingGuids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { CLOTHING_FOLDER });
        int assignedCount = 0;

        foreach (var guid in clothingGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

            if (asset == null) continue;

            // Check if it's an ItemDefinition
            var itemDefType = asset.GetType();
            if (!itemDefType.Name.Contains("ItemDefinition")) continue;

            // Add to database
            var itemDef = asset as Opsive.UltimateInventorySystem.Core.ItemDefinition;
            if (itemDef != null)
            {
                iconDb.AddEntry(itemDef, armorIcon);
                assignedCount++;
            }
        }

        // Clear the cache so changes take effect at runtime
        iconDb.ClearCache();

        EditorUtility.SetDirty(iconDb);
        AssetDatabase.SaveAssets();

        Debug.Log($"[ClothingIconAssigner] Assigned armor icon to {assignedCount} clothing items");
        EditorUtility.DisplayDialog("Clothing Icons Assigned",
            $"Assigned armor icon to {assignedCount} clothing items.", "OK");
    }
}
