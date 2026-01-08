using UnityEngine;
using UnityEditor;
using Opsive.UltimateInventorySystem.Core;
using Opsive.UltimateInventorySystem.Core.AttributeSystem;
using Opsive.UltimateInventorySystem.Editor.Managers;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Generates ItemDefinitions for office loot items with proper category assignments.
/// </summary>
public class OfficeItemGenerator : EditorWindow
{
    private enum ItemType
    {
        Documents,
        OfficeSupplies,
        Electronics,
        Valuables,
        FoodAndDrink,
        Medical,
        Misc
    }

    // Office items to generate with their categories
    private static readonly ItemData[] OFFICE_ITEMS = new ItemData[]
    {
        // Documents & Papers
        new ItemData("Documents", "Important documents. Might be useful for trading.", ItemType.Documents),
        new ItemData("Manila Folder", "A folder with papers inside.", ItemType.Documents),
        new ItemData("Notebook", "A spiral notebook with notes.", ItemType.Documents),
        new ItemData("Book", "A hardcover book.", ItemType.Documents),
        new ItemData("Magazine", "An office magazine.", ItemType.Documents),
        new ItemData("Clipboard", "A clipboard with papers.", ItemType.Documents),

        // Office Supplies
        new ItemData("Pen", "A ballpoint pen. Could be used as an improvised weapon.", ItemType.OfficeSupplies),
        new ItemData("Pencil", "A wooden pencil.", ItemType.OfficeSupplies),
        new ItemData("Scissors", "Sharp scissors. Useful for crafting.", ItemType.OfficeSupplies),
        new ItemData("Stapler", "A heavy stapler. Could do some damage.", ItemType.OfficeSupplies),
        new ItemData("Tape Roll", "Duct tape. Essential for repairs.", ItemType.OfficeSupplies),
        new ItemData("Rubber Bands", "A bundle of rubber bands.", ItemType.OfficeSupplies),
        new ItemData("Paper Clips", "A box of paper clips.", ItemType.OfficeSupplies),

        // Electronics
        new ItemData("Batteries", "AA batteries. Power for devices.", ItemType.Electronics),
        new ItemData("Battery", "A single AA battery.", ItemType.Electronics),
        new ItemData("Flashlight", "A small flashlight.", ItemType.Electronics),
        new ItemData("USB Drive", "A USB drive. Might contain valuable data.", ItemType.Electronics),
        new ItemData("Calculator", "A basic calculator.", ItemType.Electronics),
        new ItemData("Laptop", "A portable computer. Valuable electronics.", ItemType.Electronics),
        new ItemData("Headphones", "Over-ear headphones.", ItemType.Electronics),
        new ItemData("Smartphone", "A mobile phone.", ItemType.Electronics),
        new ItemData("Walkie Talkie", "A two-way radio. Useful for communication.", ItemType.Electronics),
        new ItemData("SD Card", "A memory card with data.", ItemType.Electronics),

        // Valuables
        new ItemData("Cash", "Paper money. Still has some value.", ItemType.Valuables),
        new ItemData("Watch", "A wristwatch. Could be traded.", ItemType.Valuables),
        new ItemData("Keys", "A set of keys. Might open something.", ItemType.Valuables),
        new ItemData("Key", "A single key. Opens something.", ItemType.Valuables),
        new ItemData("ID Card", "An employee ID card. Might grant access somewhere.", ItemType.Valuables),
        new ItemData("Briefcase", "A leather briefcase. Might contain valuables.", ItemType.Valuables),
        new ItemData("Trophy", "An office trophy. Could be traded.", ItemType.Valuables),

        // Consumables - Food
        new ItemData("Candy Bar", "A chocolate bar. Restores a small amount of energy.", ItemType.FoodAndDrink),
        new ItemData("Chips", "A bag of chips. A small snack.", ItemType.FoodAndDrink),
        new ItemData("Soda Can", "A can of soda. Refreshing.", ItemType.FoodAndDrink),
        new ItemData("Drink Can", "A generic drink can.", ItemType.FoodAndDrink),
        new ItemData("Energy Drink", "An energy drink. Boosts stamina.", ItemType.FoodAndDrink),
        new ItemData("Water Bottle", "A bottle of water. Essential for survival.", ItemType.FoodAndDrink),
        new ItemData("Coffee Mug", "A ceramic mug. Might be useful.", ItemType.FoodAndDrink),
        new ItemData("Donut", "A glazed donut. Sweet and satisfying.", ItemType.FoodAndDrink),
        new ItemData("Sandwich", "A wrapped sandwich. A quick meal.", ItemType.FoodAndDrink),
        new ItemData("Canned Food", "Canned food. Long shelf life.", ItemType.FoodAndDrink),
        new ItemData("Alcohol", "A bottle of alcohol. Multiple uses.", ItemType.FoodAndDrink),

        // Medical
        new ItemData("Bandages", "Basic bandages for wounds.", ItemType.Medical),
        new ItemData("Painkillers", "Over-the-counter pain relief.", ItemType.Medical),
        new ItemData("Hand Sanitizer", "Alcohol-based sanitizer.", ItemType.Medical),
        new ItemData("Pills", "Assorted pills. Could be useful.", ItemType.Medical),

        // Misc
        new ItemData("Lighter", "A disposable lighter. Start fires.", ItemType.Misc),
        new ItemData("Matches", "A box of matches.", ItemType.Misc),
        new ItemData("String", "A roll of string. Useful for crafting.", ItemType.Misc),
        new ItemData("Cloth Rag", "A piece of cloth. Many uses.", ItemType.Misc),
        new ItemData("Duct Tape", "Heavy duty tape. Essential for repairs.", ItemType.Misc),
        new ItemData("Cigarette", "A pack of cigarettes. Trade item.", ItemType.Misc),
    };

    // Category name mappings
    private static readonly Dictionary<ItemType, string> CATEGORY_NAMES = new Dictionary<ItemType, string>
    {
        { ItemType.Documents, "Documents and Papers" },
        { ItemType.OfficeSupplies, "Office Supplies" },
        { ItemType.Electronics, "Electronics" },
        { ItemType.Valuables, "Valuables" },
        { ItemType.FoodAndDrink, "Food and Drink" },
        { ItemType.Medical, "Office Medical" },
        { ItemType.Misc, "Office Misc" },
    };

    private string outputPath = "Assets/Data/InventoryDatabase/InventoryDatabase/ItemDefinitions";
    private bool useSubCategories = true;
    private ItemCategory fallbackCategory;

    // Cached categories
    private Dictionary<ItemType, ItemCategory> categoryCache = new Dictionary<ItemType, ItemCategory>();

    [MenuItem("Tools/Generate Office Items")]
    public static void ShowWindow()
    {
        GetWindow<OfficeItemGenerator>("Office Item Generator");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Office Item Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox($"This will create {OFFICE_ITEMS.Length} office item definitions for your loot tables.\n\n" +
            "Items will be assigned to their appropriate categories:\n" +
            "- Documents and Papers\n- Office Supplies\n- Electronics\n- Valuables\n- Food and Drink\n- Office Medical\n- Office Misc", MessageType.Info);

        EditorGUILayout.Space(10);

        outputPath = EditorGUILayout.TextField("Output Path", outputPath);

        EditorGUILayout.Space(5);

        useSubCategories = EditorGUILayout.Toggle("Use Sub-Categories", useSubCategories);

        fallbackCategory = (ItemCategory)EditorGUILayout.ObjectField(
            "Fallback Category",
            fallbackCategory,
            typeof(ItemCategory),
            false
        );

        EditorGUILayout.Space(10);

        // List items by category
        EditorGUILayout.LabelField("Items to Create:", EditorStyles.boldLabel);

        foreach (ItemType itemType in System.Enum.GetValues(typeof(ItemType)))
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(CATEGORY_NAMES[itemType], EditorStyles.miniBoldLabel);

            EditorGUILayout.BeginHorizontal();
            int col = 0;
            foreach (var item in OFFICE_ITEMS)
            {
                if (item.itemType != itemType) continue;

                EditorGUILayout.LabelField($"• {item.name}", GUILayout.Width(120));
                col++;
                if (col >= 4)
                {
                    col = 0;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space(10);

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Generate Office Items", GUILayout.Height(30)))
        {
            GenerateItems();
        }
        GUI.backgroundColor = Color.white;
    }

    private void GenerateItems()
    {
        // Ensure directory exists
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
            AssetDatabase.Refresh();
        }

        // Cache categories
        CacheCategories();

        int created = 0;
        int skipped = 0;

        foreach (var itemData in OFFICE_ITEMS)
        {
            string assetPath = $"{outputPath}/{itemData.name}.asset";

            // Skip if already exists
            if (File.Exists(assetPath))
            {
                Debug.Log($"[OfficeItemGenerator] Skipping {itemData.name} - already exists");
                skipped++;
                continue;
            }

            // Get category for this item
            ItemCategory category = GetCategoryForItem(itemData.itemType);

            // Create ItemDefinition
            var itemDef = ScriptableObject.CreateInstance<ItemDefinition>();
            itemDef.name = itemData.name;

            // Set up the item definition using serialized properties
            var serializedObject = new SerializedObject(itemDef);

            // Set name
            var nameProp = serializedObject.FindProperty("m_Name");
            if (nameProp != null)
            {
                nameProp.stringValue = itemData.name;
            }

            // Set category
            if (category != null)
            {
                var categoryProp = serializedObject.FindProperty("m_Category");
                if (categoryProp != null)
                {
                    categoryProp.objectReferenceValue = category;
                }
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            // Save asset
            AssetDatabase.CreateAsset(itemDef, assetPath);
            created++;

            Debug.Log($"[OfficeItemGenerator] Created: {itemData.name} -> {(category != null ? category.name : "No Category")}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[OfficeItemGenerator] Created {created} items, skipped {skipped} existing items at {outputPath}");

        EditorUtility.DisplayDialog("Office Item Generator",
            $"Created {created} new items\nSkipped {skipped} existing items",
            "OK");

        // Select the folder in project
        var folder = AssetDatabase.LoadAssetAtPath<Object>(outputPath);
        if (folder != null)
        {
            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
        }
    }

    private void CacheCategories()
    {
        categoryCache.Clear();

        foreach (var kvp in CATEGORY_NAMES)
        {
            string categoryName = kvp.Value;
            string[] guids = AssetDatabase.FindAssets($"t:ItemCategory {categoryName}");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var category = AssetDatabase.LoadAssetAtPath<ItemCategory>(path);
                if (category != null && category.name == categoryName)
                {
                    categoryCache[kvp.Key] = category;
                    Debug.Log($"[OfficeItemGenerator] Found category: {categoryName}");
                    break;
                }
            }
        }
    }

    private ItemCategory GetCategoryForItem(ItemType itemType)
    {
        if (!useSubCategories)
        {
            return fallbackCategory;
        }

        if (categoryCache.TryGetValue(itemType, out var category))
        {
            return category;
        }

        // Fallback to Office or Uncategorized
        if (fallbackCategory != null)
        {
            return fallbackCategory;
        }

        string[] guids = AssetDatabase.FindAssets("t:ItemCategory Office");
        if (guids.Length > 0)
        {
            return AssetDatabase.LoadAssetAtPath<ItemCategory>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        guids = AssetDatabase.FindAssets("t:ItemCategory Uncategorized");
        if (guids.Length > 0)
        {
            return AssetDatabase.LoadAssetAtPath<ItemCategory>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        return null;
    }

    private struct ItemData
    {
        public string name;
        public string description;
        public ItemType itemType;

        public ItemData(string name, string description, ItemType itemType)
        {
            this.name = name;
            this.description = description;
            this.itemType = itemType;
        }
    }
}
