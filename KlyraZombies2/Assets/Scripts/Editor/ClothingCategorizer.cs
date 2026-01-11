using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Categorizes clothing items as Shirt or Pants based on their Sidekick preset.
/// Run from: Tools > Inventory > Categorize Clothing Items
/// </summary>
public class ClothingCategorizer : EditorWindow
{
    private const string CLOTHING_FOLDER = "Assets/Data/InventoryDatabase/InventoryDatabase/ItemDefinitions/Clothing";
    private const string SHIRT_CATEGORY_GUID = "f509666376c26488bbbb80b8d4a540c9";
    private const string PANTS_CATEGORY_GUID = "479ced862ae1444e0bfd5e602e982ba2";
    private const string UNCATEGORIZED_GUID = "73a3fedf5a3ed40e5b1b044d3bfd169e";

    [MenuItem("Tools/Inventory/Categorize Clothing Items")]
    public static void CategorizeClothingItems()
    {
        string[] assetFiles = Directory.GetFiles(CLOTHING_FOLDER, "*.asset", SearchOption.TopDirectoryOnly);

        int shirtCount = 0;
        int pantsCount = 0;
        int skippedCount = 0;

        foreach (string filePath in assetFiles)
        {
            if (filePath.EndsWith(".meta")) continue;

            string content = File.ReadAllText(filePath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            // Determine category based on item name
            // Items ending in "Upper" or containing "Shirt" go to Shirt
            // Items ending in "Lower" or containing "Pants" go to Pants
            // For numbered presets (e.g., "Modern Civilians 04"), we'll assign to Shirt
            // and the handler will apply both upper and lower body

            string newCategoryGuid;
            string categoryName;

            if (fileName.ToLower().Contains("pants") || fileName.ToLower().Contains("lower"))
            {
                newCategoryGuid = PANTS_CATEGORY_GUID;
                categoryName = "Pants";
                pantsCount++;
            }
            else
            {
                // Default to Shirt for full outfits
                newCategoryGuid = SHIRT_CATEGORY_GUID;
                categoryName = "Shirt";
                shirtCount++;
            }

            // Replace the category reference
            string oldCategoryRef = $"m_Category: {{fileID: 11400000, guid: {UNCATEGORIZED_GUID}, type: 2}}";
            string newCategoryRef = $"m_Category: {{fileID: 11400000, guid: {newCategoryGuid}, type: 2}}";

            if (content.Contains(oldCategoryRef))
            {
                content = content.Replace(oldCategoryRef, newCategoryRef);
                File.WriteAllText(filePath, content);
                Debug.Log($"[ClothingCategorizer] {fileName} -> {categoryName}");
            }
            else if (!content.Contains(newCategoryGuid))
            {
                // Item might have a different category, skip it
                skippedCount++;
                Debug.LogWarning($"[ClothingCategorizer] Skipped {fileName} - not Uncategorized");
            }
        }

        AssetDatabase.Refresh();

        string message = $"Categorized {shirtCount} items as Shirt, {pantsCount} items as Pants.";
        if (skippedCount > 0)
        {
            message += $"\nSkipped {skippedCount} items (already categorized).";
        }

        Debug.Log($"[ClothingCategorizer] {message}");
        EditorUtility.DisplayDialog("Clothing Categorization Complete", message, "OK");
    }
}
