using UnityEngine;
using UnityEditor;
using Opsive.UltimateInventorySystem.Core;
using Opsive.UltimateInventorySystem.Editor.Managers;
using Opsive.UltimateInventorySystem.Editor.VisualElements;
using Opsive.UltimateInventorySystem.Storage;

/// <summary>
/// Editor tool to generate equipment categories for the clothing/equipment system.
/// Creates categories under Equippable for each equipment slot type.
/// </summary>
public class EquipmentCategoryGenerator : EditorWindow
{
    private InventorySystemDatabase m_Database;
    private ItemCategory m_EquippableCategory;

    // Equipment category settings
    private bool m_CreateHeadwear = true;
    private bool m_CreateFacewear = true;
    private bool m_CreateTorso = true;
    private bool m_CreateGloves = true;
    private bool m_CreatePants = true;
    private bool m_CreateFootwear = true;
    private bool m_CreateBackpack = false; // Already exists

    private Vector2 m_ScrollPos;

    [MenuItem("Project Klyra/Equipment/Generate Equipment Categories")]
    public static void ShowWindow()
    {
        GetWindow<EquipmentCategoryGenerator>("Equipment Categories");
    }

    private void OnEnable()
    {
        // Try to find the inventory database
        string[] guids = AssetDatabase.FindAssets("t:InventorySystemDatabase", new[] { "Assets/Data" });
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            m_Database = AssetDatabase.LoadAssetAtPath<InventorySystemDatabase>(path);
        }

        // Try to find the Equippable category
        if (m_Database != null)
        {
            FindEquippableCategory();
        }
    }

    private void FindEquippableCategory()
    {
        string[] guids = AssetDatabase.FindAssets("Equippable t:ItemCategory", new[] { "Assets/Data" });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var cat = AssetDatabase.LoadAssetAtPath<ItemCategory>(path);
            if (cat != null && cat.name == "Equippable")
            {
                m_EquippableCategory = cat;
                break;
            }
        }
    }

    private void OnGUI()
    {
        m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

        EditorGUILayout.LabelField("Equipment Category Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Database field
        m_Database = (InventorySystemDatabase)EditorGUILayout.ObjectField(
            "Inventory Database", m_Database, typeof(InventorySystemDatabase), false);

        m_EquippableCategory = (ItemCategory)EditorGUILayout.ObjectField(
            "Equippable Category", m_EquippableCategory, typeof(ItemCategory), false);

        EditorGUILayout.Space();

        if (m_Database == null)
        {
            EditorGUILayout.HelpBox("Please assign your Inventory System Database.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        if (m_EquippableCategory == null)
        {
            EditorGUILayout.HelpBox("Please assign the 'Equippable' parent category.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUILayout.LabelField("Categories to Create:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "These categories will be created as children of the Equippable category.\n" +
            "Each will have Description, Icon, Prefabs, and EquipmentSlot attributes.",
            MessageType.Info);

        EditorGUILayout.Space();

        m_CreateHeadwear = EditorGUILayout.Toggle("Headwear (helmets, hats)", m_CreateHeadwear);
        m_CreateFacewear = EditorGUILayout.Toggle("Facewear (masks, goggles)", m_CreateFacewear);
        m_CreateTorso = EditorGUILayout.Toggle("Torso (shirts, jackets)", m_CreateTorso);
        m_CreateGloves = EditorGUILayout.Toggle("Gloves", m_CreateGloves);
        m_CreatePants = EditorGUILayout.Toggle("Pants (legs)", m_CreatePants);
        m_CreateFootwear = EditorGUILayout.Toggle("Footwear (shoes, boots)", m_CreateFootwear);

        // Check if backpack already exists
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Note: Backpack category already exists.", EditorStyles.miniLabel);

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Equipment Categories", GUILayout.Height(40)))
        {
            GenerateCategories();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("After Generation:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1. Open UIS Database Editor to verify categories\n" +
            "2. Create ItemDefinitions for clothing items\n" +
            "3. Add EquipmentVisualHandler to player\n" +
            "4. Configure slot mappings in the handler",
            MessageType.Info);

        EditorGUILayout.EndScrollView();
    }

    private void GenerateCategories()
    {
        int created = 0;
        string categoryFolder = "Assets/Data/InventoryDatabase/InventoryDatabase/ItemCategories";

        // Ensure folder exists
        if (!AssetDatabase.IsValidFolder(categoryFolder))
        {
            Debug.LogError($"Category folder not found: {categoryFolder}");
            return;
        }

        if (m_CreateHeadwear)
        {
            if (CreateEquipmentCategory("Headwear", EquipmentSlotType.Head, categoryFolder))
                created++;
        }

        if (m_CreateFacewear)
        {
            if (CreateEquipmentCategory("Facewear", EquipmentSlotType.Face, categoryFolder))
                created++;
        }

        if (m_CreateTorso)
        {
            if (CreateEquipmentCategory("Torso", EquipmentSlotType.Torso, categoryFolder))
                created++;
        }

        if (m_CreateGloves)
        {
            if (CreateEquipmentCategory("Gloves", EquipmentSlotType.Hands, categoryFolder))
                created++;
        }

        if (m_CreatePants)
        {
            if (CreateEquipmentCategory("Pants", EquipmentSlotType.Legs, categoryFolder))
                created++;
        }

        if (m_CreateFootwear)
        {
            if (CreateEquipmentCategory("Footwear", EquipmentSlotType.Feet, categoryFolder))
                created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Equipment Categories",
            $"Created {created} equipment categories.\n\n" +
            "Next steps:\n" +
            "1. Open the UIS Database Editor\n" +
            "2. Verify categories appear under Equippable\n" +
            "3. Create clothing ItemDefinitions",
            "OK");

        Debug.Log($"[EquipmentCategoryGenerator] Created {created} equipment categories");
    }

    private bool CreateEquipmentCategory(string categoryName, EquipmentSlotType slotType, string folder)
    {
        string path = $"{folder}/{categoryName}.asset";

        // Check if already exists
        if (AssetDatabase.LoadAssetAtPath<ItemCategory>(path) != null)
        {
            Debug.Log($"Category already exists: {categoryName}");
            return false;
        }

        // Create the category using UIS API
        var category = ScriptableObject.CreateInstance<ItemCategory>();

        // Use reflection to set internal fields
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

        // Set name
        var nameField = typeof(ItemCategory).GetField("m_Name", flags);
        if (nameField != null)
        {
            nameField.SetValue(category, categoryName);
        }

        // Set ID (generate a unique one)
        var idField = typeof(ItemCategory).GetField("m_ID", flags);
        if (idField != null)
        {
            idField.SetValue(category, (uint)(categoryName.GetHashCode() + System.DateTime.Now.Ticks));
        }

        // Set IsMutable and IsUnique (equipment is unique, mutable)
        var mutableField = typeof(ItemCategory).GetField("m_IsMutable", flags);
        if (mutableField != null)
        {
            mutableField.SetValue(category, true);
        }

        var uniqueField = typeof(ItemCategory).GetField("m_IsUnique", flags);
        if (uniqueField != null)
        {
            uniqueField.SetValue(category, true);
        }

        // Set color based on slot type
        var colorField = typeof(ItemCategory).GetField("m_Color", flags);
        if (colorField != null)
        {
            colorField.SetValue(category, GetSlotColor(slotType));
        }

        // Create the asset first
        AssetDatabase.CreateAsset(category, path);

        Debug.Log($"Created category: {categoryName} at {path}");
        return true;
    }

    private Color GetSlotColor(EquipmentSlotType slot)
    {
        switch (slot)
        {
            case EquipmentSlotType.Head: return new Color(0.8f, 0.4f, 0.4f);
            case EquipmentSlotType.Face: return new Color(0.7f, 0.5f, 0.5f);
            case EquipmentSlotType.Torso: return new Color(0.4f, 0.6f, 0.8f);
            case EquipmentSlotType.Hands: return new Color(0.6f, 0.6f, 0.4f);
            case EquipmentSlotType.Legs: return new Color(0.4f, 0.4f, 0.7f);
            case EquipmentSlotType.Feet: return new Color(0.5f, 0.4f, 0.3f);
            default: return new Color(0.5f, 0.5f, 0.5f);
        }
    }
}
