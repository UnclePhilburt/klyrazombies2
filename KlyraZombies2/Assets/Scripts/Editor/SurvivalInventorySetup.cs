using UnityEngine;
using UnityEditor;
using Opsive.UltimateInventorySystem.Core;
using Opsive.UltimateInventorySystem.Storage;
using Opsive.UltimateInventorySystem.Core.AttributeSystem;
using System.Collections.Generic;

public class SurvivalInventorySetup : EditorWindow
{
    private InventorySystemDatabase m_Database;
    private Vector2 m_ScrollPos;

    [MenuItem("Tools/Survival Inventory Setup")]
    public static void ShowWindow()
    {
        GetWindow<SurvivalInventorySetup>("Survival Inventory Setup");
    }

    private void OnEnable()
    {
        // Try to find existing database
        string[] guids = AssetDatabase.FindAssets("t:InventorySystemDatabase", new[] { "Assets/Data" });
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            m_Database = AssetDatabase.LoadAssetAtPath<InventorySystemDatabase>(path);
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Survival Game Inventory Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        m_Database = (InventorySystemDatabase)EditorGUILayout.ObjectField(
            "Inventory Database", m_Database, typeof(InventorySystemDatabase), false);

        if (m_Database == null)
        {
            EditorGUILayout.HelpBox("Please assign your Inventory Database", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space();
        m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

        // Category Section
        EditorGUILayout.LabelField("Step 1: Categories", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Creates weapon categories:\n" +
            "- Equippable > Weapon > RangedWeapon\n" +
            "- Ammo", MessageType.Info);

        if (GUILayout.Button("Create Categories", GUILayout.Height(30)))
        {
            CreateCategories();
        }

        EditorGUILayout.Space(20);

        // Items Section
        EditorGUILayout.LabelField("Step 2: Items", EditorStyles.boldLabel);

        EditorGUILayout.Space(10);

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Create Weapons & Ammo", GUILayout.Height(40)))
        {
            CreateWeapons();
            CreateAmmo();
            EditorUtility.DisplayDialog("Done", "Created:\n- AK-47\n- SR-9\n- 7.62mm Rounds\n- 9mm Rounds", "OK");
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "After creating items, open:\n" +
            "Tools > Opsive > Ultimate Inventory System > Main Manager\n" +
            "to configure additional attributes and link Character Item prefabs.",
            MessageType.Info);
    }

    private string GetDatabaseFolder()
    {
        string dbPath = AssetDatabase.GetAssetPath(m_Database);
        return System.IO.Path.GetDirectoryName(dbPath);
    }

    private void CreateCategories()
    {
        string folder = GetDatabaseFolder();
        string catFolder = folder + "/ItemCategories";

        if (!AssetDatabase.IsValidFolder(catFolder))
        {
            AssetDatabase.CreateFolder(folder, "ItemCategories");
        }

        // Find or create root "All" category
        ItemCategory allCategory = FindCategory("All");

        // Weapon Categories
        var equippable = CreateCategory("Equippable", allCategory, catFolder);
        var weapon = CreateCategory("Weapon", equippable, catFolder);
        CreateCategory("RangedWeapon", weapon, catFolder);

        // Ammo Category
        CreateCategory("Ammo", allCategory, catFolder);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Created weapon categories!");
        EditorUtility.DisplayDialog("Success", "Created categories:\n- Equippable > Weapon > RangedWeapon\n- Ammo", "OK");
    }

    private ItemCategory FindCategory(string name)
    {
        if (m_Database == null) return null;

        foreach (var cat in m_Database.ItemCategories)
        {
            if (cat != null && cat.name == name)
                return cat;
        }
        return null;
    }

    private ItemCategory CreateCategory(string name, ItemCategory parent, string folder)
    {
        // Check if already exists
        var existing = FindCategory(name);
        if (existing != null)
        {
            Debug.Log($"Category '{name}' already exists, skipping.");
            return existing;
        }

        var category = ScriptableObject.CreateInstance<ItemCategory>();
        category.name = name;

        string path = $"{folder}/{name}.asset";
        AssetDatabase.CreateAsset(category, path);

        // Add to database using the manager
        var categories = new List<ItemCategory>(m_Database.ItemCategories);
        categories.Add(category);

        // Use serialized object to modify
        SerializedObject so = new SerializedObject(m_Database);
        SerializedProperty catProp = so.FindProperty("m_ItemCategories");
        catProp.arraySize++;
        catProp.GetArrayElementAtIndex(catProp.arraySize - 1).objectReferenceValue = category;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(m_Database);
        Debug.Log($"Created category: {name}");

        return category;
    }

    private ItemDefinition CreateItem(string name, string categoryName, string folder)
    {
        // Check if already exists
        foreach (var item in m_Database.ItemDefinitions)
        {
            if (item != null && item.name == name)
            {
                Debug.Log($"Item '{name}' already exists, skipping.");
                return item;
            }
        }

        var category = FindCategory(categoryName);
        if (category == null)
        {
            Debug.LogError($"Category '{categoryName}' not found! Create categories first.");
            return null;
        }

        var itemDef = ScriptableObject.CreateInstance<ItemDefinition>();
        itemDef.name = name;

        string path = $"{folder}/{name}.asset";
        AssetDatabase.CreateAsset(itemDef, path);

        // Set category via reflection (Opsive uses internal setters)
        var catField = typeof(ItemDefinition).GetField("m_Category",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (catField != null)
        {
            catField.SetValue(itemDef, category);
        }

        // Add to database
        SerializedObject so = new SerializedObject(m_Database);
        SerializedProperty itemsProp = so.FindProperty("m_ItemDefinitions");
        itemsProp.arraySize++;
        itemsProp.GetArrayElementAtIndex(itemsProp.arraySize - 1).objectReferenceValue = itemDef;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(itemDef);
        EditorUtility.SetDirty(m_Database);

        return itemDef;
    }

    private void CreateWeapons()
    {
        string folder = GetDatabaseFolder() + "/ItemDefinitions/Weapons";
        EnsureFolder(folder);

        // Only weapons that have been set up with Character Item prefabs
        string[] rangedWeapons = { "AK-47", "SR-9" };
        foreach (var w in rangedWeapons)
        {
            CreateItem(w, "RangedWeapon", folder);
        }

        // No melee weapons set up yet

        SaveAndRefresh();
        Debug.Log("Created weapon items!");
    }

    private void CreateAmmo()
    {
        string folder = GetDatabaseFolder() + "/ItemDefinitions/Ammo";
        EnsureFolder(folder);

        // Only ammo for weapons we have set up
        string[] ammoTypes = { "7.62mm Rounds", "9mm Rounds" };
        foreach (var a in ammoTypes)
        {
            CreateItem(a, "Ammo", folder);
        }

        SaveAndRefresh();
        Debug.Log("Created ammo items!");
    }

    private void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = System.IO.Path.GetDirectoryName(path);
            string folderName = System.IO.Path.GetFileName(path);

            if (!AssetDatabase.IsValidFolder(parent))
            {
                string grandParent = System.IO.Path.GetDirectoryName(parent);
                string parentName = System.IO.Path.GetFileName(parent);
                AssetDatabase.CreateFolder(grandParent, parentName);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    private void SaveAndRefresh()
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
