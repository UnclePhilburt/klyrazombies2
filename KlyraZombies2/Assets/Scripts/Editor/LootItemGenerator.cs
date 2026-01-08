using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using Opsive.UltimateInventorySystem.Core;
using Opsive.UltimateInventorySystem.Storage;

public class LootItemGenerator : EditorWindow
{
    private InventorySystemDatabase database;
    private ItemCategory uncategorizedCategory;
    private ItemCategory ammoCategory;

    private Vector2 scrollPos;
    private bool showOfficeItems = true;
    private bool showKitchenItems = false;
    private bool showMedicalItems = false;
    private bool showWeaponItems = false;
    private bool showToolItems = false;

    // Track what's been created
    private HashSet<string> existingItems = new HashSet<string>();

    [MenuItem("Tools/Loot Item Generator")]
    public static void ShowWindow()
    {
        var window = GetWindow<LootItemGenerator>("Loot Item Generator");
        window.minSize = new Vector2(400, 500);
    }

    private void OnEnable()
    {
        FindDatabase();
        RefreshExistingItems();
    }

    private void FindDatabase()
    {
        // Try to find the inventory database
        string[] guids = AssetDatabase.FindAssets("t:InventorySystemDatabase");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("InventoryDatabase"))
            {
                database = AssetDatabase.LoadAssetAtPath<InventorySystemDatabase>(path);
                break;
            }
        }

        // Find categories
        string[] categoryGuids = AssetDatabase.FindAssets("t:ItemCategory");
        foreach (string guid in categoryGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemCategory cat = AssetDatabase.LoadAssetAtPath<ItemCategory>(path);
            if (cat != null)
            {
                if (cat.name == "Uncategorized") uncategorizedCategory = cat;
                if (cat.name == "Ammo") ammoCategory = cat;
            }
        }
    }

    private void RefreshExistingItems()
    {
        existingItems.Clear();
        string[] guids = AssetDatabase.FindAssets("t:ItemDefinition");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item != null)
            {
                existingItems.Add(item.name);
            }
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Loot Item Generator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Creates ItemDefinitions for loot items based on Synty prefabs.", MessageType.Info);

        EditorGUILayout.Space(10);

        // Database reference
        database = (InventorySystemDatabase)EditorGUILayout.ObjectField("Inventory Database", database, typeof(InventorySystemDatabase), false);

        if (database == null)
        {
            EditorGUILayout.HelpBox("Please assign your InventorySystemDatabase!", MessageType.Error);
            return;
        }

        EditorGUILayout.Space(10);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // Office Items
        showOfficeItems = EditorGUILayout.Foldout(showOfficeItems, "Office Items", true);
        if (showOfficeItems)
        {
            DrawItemSection(GetOfficeItems());
        }

        // Kitchen Items
        showKitchenItems = EditorGUILayout.Foldout(showKitchenItems, "Kitchen/Food Items", true);
        if (showKitchenItems)
        {
            DrawItemSection(GetKitchenItems());
        }

        // Medical Items
        showMedicalItems = EditorGUILayout.Foldout(showMedicalItems, "Medical Items", true);
        if (showMedicalItems)
        {
            DrawItemSection(GetMedicalItems());
        }

        // Tool Items
        showToolItems = EditorGUILayout.Foldout(showToolItems, "Tools & Utility", true);
        if (showToolItems)
        {
            DrawItemSection(GetToolItems());
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10);

        // Bulk create buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Create All Office Items", GUILayout.Height(30)))
        {
            CreateItemsFromList(GetOfficeItems());
            CreateOfficeLootTable();
        }
        if (GUILayout.Button("Create ALL Items", GUILayout.Height(30)))
        {
            CreateItemsFromList(GetOfficeItems());
            CreateItemsFromList(GetKitchenItems());
            CreateItemsFromList(GetMedicalItems());
            CreateItemsFromList(GetToolItems());
            CreateAllLootTables();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        if (GUILayout.Button("Refresh"))
        {
            FindDatabase();
            RefreshExistingItems();
        }
    }

    private void DrawItemSection(List<ItemData> items)
    {
        EditorGUI.indentLevel++;
        foreach (var item in items)
        {
            EditorGUILayout.BeginHorizontal();

            bool exists = existingItems.Contains(item.name);
            GUI.enabled = !exists;

            if (exists)
            {
                EditorGUILayout.LabelField($"  {item.name}", EditorStyles.miniLabel);
                GUI.backgroundColor = Color.green;
                GUILayout.Button("Created", GUILayout.Width(60));
                GUI.backgroundColor = Color.white;
            }
            else
            {
                EditorGUILayout.LabelField($"  {item.name}", EditorStyles.miniLabel);
                if (GUILayout.Button("Create", GUILayout.Width(60)))
                {
                    CreateItemDefinition(item);
                    RefreshExistingItems();
                }
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }
        EditorGUI.indentLevel--;
    }

    private void CreateItemDefinition(ItemData data)
    {
        // Find or create category
        ItemCategory category = uncategorizedCategory;

        // Create the ItemDefinition
        ItemDefinition itemDef = ScriptableObject.CreateInstance<ItemDefinition>();
        itemDef.name = data.name;

        // Set up the item using reflection since some properties might be internal
        var nameField = typeof(ItemDefinition).GetField("m_Name", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (nameField != null) nameField.SetValue(itemDef, data.name);

        // Ensure directory exists
        string dir = "Assets/Data/InventoryDatabase/InventoryDatabase/ItemDefinitions";
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // Save the asset
        string path = $"{dir}/{data.name}.asset";
        AssetDatabase.CreateAsset(itemDef, path);

        // Try to find and assign the prefab
        if (!string.IsNullOrEmpty(data.prefabPath))
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(data.prefabPath);
            if (prefab != null)
            {
                // The prefab would need to be assigned via the item's attributes
                // This requires more complex UIS integration
                Debug.Log($"Created {data.name} - Prefab found at {data.prefabPath}");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Created ItemDefinition: {data.name}");
    }

    private void CreateItemsFromList(List<ItemData> items)
    {
        foreach (var item in items)
        {
            if (!existingItems.Contains(item.name))
            {
                CreateItemDefinition(item);
            }
        }
        RefreshExistingItems();
        AssetDatabase.Refresh();
    }

    private void CreateOfficeLootTable()
    {
        CreateLootTable("Office", GetOfficeItems(), new Dictionary<string, int>
        {
            // Common
            { "Smartphone", 40 },
            { "Battery", 35 },
            { "Book", 30 },
            { "Pencil", 50 },
            { "Clipboard", 25 },
            { "Duct Tape", 20 },
            // Uncommon
            { "Flashlight", 15 },
            { "Pills", 12 },
            { "Key", 10 },
            { "Drink Can", 20 },
            { "Canned Food", 15 },
            { "Walkie Talkie", 8 },
            // Rare
            { "9mm Rounds", 5 },
            // Junk
            { "Alcohol", 15 },
            { "Cigarette", 20 }
        });
    }

    private void CreateAllLootTables()
    {
        CreateOfficeLootTable();

        // Kitchen loot table
        CreateLootTable("Kitchen", GetKitchenItems(), new Dictionary<string, int>
        {
            { "Canned Food", 40 },
            { "Drink Can", 35 },
            { "Water Bottle", 30 },
            { "Bread", 20 },
            { "Cooked Meat", 15 },
            { "Rice", 25 },
            { "Alcohol", 20 },
            { "Frying Pan", 10 },
            { "Pills", 8 }
        });

        // Medical loot table
        CreateLootTable("Medical", GetMedicalItems(), new Dictionary<string, int>
        {
            { "Pills", 50 },
            { "Bandage", 40 },
            { "First Aid Kit", 20 },
            { "Alcohol", 15 }
        });

        Debug.Log("Created all loot tables!");
    }

    private void CreateLootTable(string tableName, List<ItemData> possibleItems, Dictionary<string, int> weights)
    {
        string dir = "Assets/Data/LootTables";
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // Check if already exists
        string path = $"{dir}/LootTable_{tableName}.asset";
        if (File.Exists(path))
        {
            Debug.Log($"Loot table {tableName} already exists, skipping.");
            return;
        }

        LootTable table = ScriptableObject.CreateInstance<LootTable>();
        table.tableName = tableName + " Loot";
        table.minItemTypes = 1;
        table.maxItemTypes = 4;
        table.emptyChance = 15;

        // Find ItemDefinitions and add them
        table.possibleItems = new List<LootTable.LootEntry>();

        foreach (var itemData in possibleItems)
        {
            // Try to find the ItemDefinition
            string[] guids = AssetDatabase.FindAssets($"t:ItemDefinition {itemData.name}");
            ItemDefinition itemDef = null;

            foreach (string guid in guids)
            {
                string itemPath = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<ItemDefinition>(itemPath);
                if (def != null && def.name == itemData.name)
                {
                    itemDef = def;
                    break;
                }
            }

            if (itemDef != null)
            {
                int weight = weights.ContainsKey(itemData.name) ? weights[itemData.name] : 10;

                table.possibleItems.Add(new LootTable.LootEntry
                {
                    itemDefinition = itemDef,
                    weight = weight,
                    minAmount = itemData.minAmount,
                    maxAmount = itemData.maxAmount
                });
            }
        }

        AssetDatabase.CreateAsset(table, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"Created LootTable: {tableName} with {table.possibleItems.Count} items");
    }

    // Item data definitions
    private struct ItemData
    {
        public string name;
        public string prefabPath;
        public int minAmount;
        public int maxAmount;

        public ItemData(string name, string prefab, int min = 1, int max = 1)
        {
            this.name = name;
            this.prefabPath = prefab;
            this.minAmount = min;
            this.maxAmount = max;
        }
    }

    private List<ItemData> GetOfficeItems()
    {
        return new List<ItemData>
        {
            // Common
            new ItemData("Smartphone", "Assets/Synty/PolygonApocalypse/Prefabs/Item/SM_Item_SmartPhone_01.prefab"),
            new ItemData("Battery", "Assets/Synty/PolygonApocalypse/Prefabs/Item/SM_Item_Battery_01.prefab", 1, 3),
            new ItemData("Book", "Assets/Synty/PolygonApocalypse/Prefabs/Item/SM_Item_Book_01.prefab"),
            new ItemData("Pencil", "Assets/Synty/PolygonMilitary/Prefabs/Items/SM_Item_Pencil_01.prefab", 1, 4),
            new ItemData("Clipboard", "Assets/Synty/PolygonMilitary/Prefabs/Items/SM_Item_Clipboard_01.prefab"),
            new ItemData("Duct Tape", "Assets/Synty/PolygonMilitary/Prefabs/Items/SM_Item_Tape_01.prefab"),
            // Uncommon
            new ItemData("Flashlight", "Assets/Synty/PolygonApocalypse/Prefabs/Props/SM_Prop_Flashlight_01.prefab"),
            new ItemData("Pills", "Assets/Synty/PolygonApocalypse/Prefabs/Item/SM_Item_Pills_01.prefab"),
            new ItemData("Key", "Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Key_01.prefab"),
            new ItemData("Drink Can", "Assets/Synty/PolygonApocalypse/Prefabs/Item/SM_Item_Drink_01.prefab"),
            new ItemData("Canned Food", "Assets/Synty/PolygonApocalypse/Prefabs/Item/SM_Item_Can_01.prefab"),
            new ItemData("Walkie Talkie", "Assets/Synty/PolygonApocalypse/Prefabs/Item/SM_Item_WalkieTalkie_01.prefab"),
            // Junk
            new ItemData("Alcohol", "Assets/Synty/PolygonApocalypse/Prefabs/Item/SM_Item_Alcohol_01.prefab"),
            new ItemData("Cigarette", "Assets/Synty/PolygonMilitary/Prefabs/Items/SM_Item_Cigarette_01.prefab", 1, 5)
        };
    }

    private List<ItemData> GetKitchenItems()
    {
        return new List<ItemData>
        {
            new ItemData("Canned Food", "Assets/Synty/PolygonApocalypse/Prefabs/Item/SM_Item_Can_01.prefab", 1, 3),
            new ItemData("Drink Can", "Assets/Synty/PolygonApocalypse/Prefabs/Item/SM_Item_Drink_01.prefab", 1, 2),
            new ItemData("Water Bottle", "Assets/Synty/PolygonApocalypse/Prefabs/Item/SM_Item_Drink_Bottle_01.prefab"),
            new ItemData("Bread", "Assets/Synty/PolygonApocalypse/Prefabs/Item/SM_Item_Bread_01.prefab"),
            new ItemData("Cooked Meat", "Assets/Synty/PolygonApocalypse/Prefabs/Item/SM_Item_Meat_Cooked_01.prefab"),
            new ItemData("Rice", "Assets/Synty/PolygonApocalypse/Prefabs/Item/SM_Item_Rice_01.prefab"),
            new ItemData("Alcohol", "Assets/Synty/PolygonApocalypse/Prefabs/Item/SM_Item_Alcohol_01.prefab"),
            new ItemData("Frying Pan", "Assets/Synty/PolygonApocalypse/Prefabs/Item/SM_Item_Frypan_02.prefab"),
            new ItemData("Pills", "Assets/Synty/PolygonApocalypse/Prefabs/Item/SM_Item_Pills_01.prefab")
        };
    }

    private List<ItemData> GetMedicalItems()
    {
        return new List<ItemData>
        {
            new ItemData("Pills", "Assets/Synty/PolygonApocalypse/Prefabs/Item/SM_Item_Pills_01.prefab", 1, 3),
            new ItemData("Bandage", "", 1, 5), // No specific prefab
            new ItemData("First Aid Kit", "", 1, 1),
            new ItemData("Alcohol", "Assets/Synty/PolygonApocalypse/Prefabs/Item/SM_Item_Alcohol_01.prefab") // Medical alcohol
        };
    }

    private List<ItemData> GetToolItems()
    {
        return new List<ItemData>
        {
            new ItemData("Flashlight", "Assets/Synty/PolygonApocalypse/Prefabs/Props/SM_Prop_Flashlight_01.prefab"),
            new ItemData("Battery", "Assets/Synty/PolygonApocalypse/Prefabs/Item/SM_Item_Battery_01.prefab", 1, 4),
            new ItemData("Duct Tape", "Assets/Synty/PolygonMilitary/Prefabs/Items/SM_Item_Tape_01.prefab"),
            new ItemData("Rope", "Assets/Synty/PolygonApocalypse/Prefabs/Item/SM_Item_Rope_Detailed_01.prefab"),
            new ItemData("Crowbar", "Assets/Synty/PolygonMilitary/Prefabs/Items/SM_Item_Crowbar_01.prefab"),
            new ItemData("Walkie Talkie", "Assets/Synty/PolygonApocalypse/Prefabs/Item/SM_Item_WalkieTalkie_01.prefab"),
            new ItemData("Binoculars", "Assets/Synty/PolygonMilitary/Prefabs/Items/SM_Item_Binoculars_01.prefab"),
            new ItemData("Compass", "Assets/Synty/PolygonApocalypse/Prefabs/Item/SM_Item_Compass_01.prefab"),
            new ItemData("Lighter", "Assets/Synty/PolygonApocalypse/Prefabs/Item/SM_Item_Lighter_Flip_01.prefab"),
            new ItemData("Gas Can", "Assets/Synty/PolygonApocalypse/Prefabs/Props/SM_Prop_GasCan_01.prefab"),
            new ItemData("Gas Mask", "Assets/Synty/PolygonMilitary/Prefabs/Items/SM_Item_GasMask_01.prefab")
        };
    }
}
