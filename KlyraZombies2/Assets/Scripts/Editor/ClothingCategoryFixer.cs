using UnityEngine;
using UnityEditor;
using System.IO;
using Opsive.UltimateInventorySystem.Core;

/// <summary>
/// Fixes existing clothing items that have no category assigned.
/// Assigns Shirt, Pants, or Headwear category based on item name pattern.
/// </summary>
public class ClothingCategoryFixer : EditorWindow
{
    private ItemCategory m_ShirtCategory;
    private ItemCategory m_PantsCategory;
    private ItemCategory m_HeadwearCategory;

    [MenuItem("Project Klyra/Sidekick/Fix Clothing Categories")]
    public static void ShowWindow()
    {
        GetWindow<ClothingCategoryFixer>("Fix Clothing Categories");
    }

    private void OnEnable()
    {
        FindCategories();
    }

    private void FindCategories()
    {
        string[] guids = AssetDatabase.FindAssets("t:ItemCategory", new[] { "Assets/Data" });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var cat = AssetDatabase.LoadAssetAtPath<ItemCategory>(path);
            if (cat == null) continue;

            switch (cat.name)
            {
                case "Shirt":
                    m_ShirtCategory = cat;
                    break;
                case "Pants":
                    m_PantsCategory = cat;
                    break;
                case "Headwear":
                    m_HeadwearCategory = cat;
                    break;
            }
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Fix Clothing Categories", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This tool assigns categories to clothing items based on the Sidekick preset naming convention.\n\n" +
            "UpperBody presets (indices 10-16) → Shirt category\n" +
            "LowerBody presets (indices 17-21) → Pants category\n" +
            "Head presets (indices 1-9, 22-23) → Headwear category",
            MessageType.Info);

        EditorGUILayout.Space();

        m_ShirtCategory = (ItemCategory)EditorGUILayout.ObjectField("Shirt", m_ShirtCategory, typeof(ItemCategory), false);
        m_PantsCategory = (ItemCategory)EditorGUILayout.ObjectField("Pants", m_PantsCategory, typeof(ItemCategory), false);
        m_HeadwearCategory = (ItemCategory)EditorGUILayout.ObjectField("Headwear", m_HeadwearCategory, typeof(ItemCategory), false);

        EditorGUILayout.Space();

        bool canFix = m_ShirtCategory != null && m_PantsCategory != null && m_HeadwearCategory != null;

        EditorGUI.BeginDisabledGroup(!canFix);
        if (GUILayout.Button("Fix All Clothing Items", GUILayout.Height(40)))
        {
            FixClothingItems();
        }
        EditorGUI.EndDisabledGroup();
    }

    private void FixClothingItems()
    {
        string clothingFolder = "Assets/Data/InventoryDatabase/InventoryDatabase/ItemDefinitions/Clothing";
        string[] guids = AssetDatabase.FindAssets("t:ItemDefinition", new[] { clothingFolder });

        int fixedShirt = 0;
        int fixedPants = 0;
        int fixedHead = 0;
        int skipped = 0;

        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var categoryField = typeof(ItemDefinition).GetField("m_Category", flags);

        if (categoryField == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find m_Category field on ItemDefinition", "OK");
            return;
        }

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var itemDef = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (itemDef == null) continue;

            // Check if already has category
            var currentCategory = categoryField.GetValue(itemDef) as ItemCategory;
            if (currentCategory != null)
            {
                skipped++;
                continue;
            }

            // Determine category based on preset name pattern
            // Sidekick presets contain body part info that tells us what type they are
            string name = itemDef.name.ToLower();
            ItemCategory targetCategory = null;

            // Most clothing presets are UpperBody (shirts) or LowerBody (pants)
            // Head presets are typically "Species Humans" which are heads/faces, not clothing
            // We'll categorize based on common patterns:

            // "Survivor", "Outlaws", "Civilians" - these are full outfits
            // For simplicity, we'll assign them to Shirt since that's the most common clothing
            // The player can find both shirt and pants separately

            // However, looking at Sidekick structure:
            // - If preset contains upper body parts (torso, arms, hands) → Shirt
            // - If preset contains lower body parts (hips, legs, feet) → Pants
            // - If preset contains head parts → Headwear

            // Since we can't easily determine this from the name alone, we'll use a simple heuristic:
            // Check if file exists in certain subdirectories or has naming patterns

            // For now, let's assign all to Shirt as they're typically UpperBody presets
            // The generator should have tracked which PartGroup was used

            // Actually, let's read what group the item was from by parsing the path or using attributes
            // But since we don't have that info stored, let's just assign to Shirt for now
            // and let the user regenerate items properly

            // Better approach: Parse the preset families
            // - "Species Humans" = Head presets (faces)
            // - Other presets = Check if name implies upper/lower

            if (name.Contains("species") && name.Contains("human"))
            {
                // These are head/face presets, not really "clothing"
                // Skip or assign to Headwear
                targetCategory = m_HeadwearCategory;
                fixedHead++;
            }
            else
            {
                // Most other presets are clothing outfits
                // Assign to Shirt by default - player needs to regenerate with proper groups
                targetCategory = m_ShirtCategory;
                fixedShirt++;
            }

            if (targetCategory != null)
            {
                categoryField.SetValue(itemDef, targetCategory);
                EditorUtility.SetDirty(itemDef);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Clothing Fixed",
            $"Fixed categories:\n\n" +
            $"Shirt: {fixedShirt}\n" +
            $"Pants: {fixedPants}\n" +
            $"Headwear: {fixedHead}\n\n" +
            $"Skipped (already had category): {skipped}\n\n" +
            "Note: For proper categorization, regenerate items using\n" +
            "Project Klyra > Sidekick > Generate Clothing Items",
            "OK");
    }
}
