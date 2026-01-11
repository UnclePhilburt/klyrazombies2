using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Opsive.UltimateInventorySystem.Core;
using Opsive.UltimateInventorySystem.Storage;

/// <summary>
/// Generates equipment ItemDefinitions from Synty attachment prefabs.
/// </summary>
public class EquipmentItemGenerator : EditorWindow
{
    private InventorySystemDatabase m_Database;
    private Vector2 m_ScrollPos;
    private List<AttachmentInfo> m_FoundAttachments = new List<AttachmentInfo>();
    private bool m_ScannedForAttachments = false;

    // Category selection
    private ItemCategory m_HeadwearCategory;
    private ItemCategory m_FacewearCategory;
    private ItemCategory m_BackpackCategory;

    // Filter settings
    private bool m_IncludeHelmets = true;
    private bool m_IncludeHats = true;
    private bool m_IncludeGoggles = true;
    private bool m_IncludeMasks = true;
    private bool m_IncludeGlasses = true;
    private bool m_IncludeArmor = true;
    private bool m_IncludePouches = true;

    private class AttachmentInfo
    {
        public string name;
        public string path;
        public GameObject prefab;
        public EquipmentSlotType suggestedSlot;
        public bool selected;
    }

    [MenuItem("Project Klyra/Equipment/Generate Equipment Items")]
    public static void ShowWindow()
    {
        GetWindow<EquipmentItemGenerator>("Equipment Item Generator");
    }

    private void OnEnable()
    {
        // Find the inventory database
        string[] guids = AssetDatabase.FindAssets("t:InventorySystemDatabase", new[] { "Assets/Data" });
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            m_Database = AssetDatabase.LoadAssetAtPath<InventorySystemDatabase>(path);
        }

        // Find categories
        FindCategories();
    }

    private void FindCategories()
    {
        string[] categoryGuids = AssetDatabase.FindAssets("t:ItemCategory", new[] { "Assets/Data" });
        foreach (var guid in categoryGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var cat = AssetDatabase.LoadAssetAtPath<ItemCategory>(path);
            if (cat == null) continue;

            switch (cat.name)
            {
                case "Headwear":
                    m_HeadwearCategory = cat;
                    break;
                case "Facewear":
                    m_FacewearCategory = cat;
                    break;
                case "Backpack":
                    m_BackpackCategory = cat;
                    break;
            }
        }
    }

    private void OnGUI()
    {
        m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

        EditorGUILayout.LabelField("Equipment Item Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This tool scans Synty attachment prefabs and generates UIS ItemDefinitions for them.\n\n" +
            "Step 1: Run 'Generate Equipment Categories' first\n" +
            "Step 2: Scan for attachments\n" +
            "Step 3: Select items to generate\n" +
            "Step 4: Generate ItemDefinitions",
            MessageType.Info);

        EditorGUILayout.Space();

        // Database
        m_Database = (InventorySystemDatabase)EditorGUILayout.ObjectField(
            "Inventory Database", m_Database, typeof(InventorySystemDatabase), false);

        if (m_Database == null)
        {
            EditorGUILayout.HelpBox("Please assign your Inventory System Database.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUILayout.Space();

        // Categories
        EditorGUILayout.LabelField("Categories", EditorStyles.boldLabel);
        m_HeadwearCategory = (ItemCategory)EditorGUILayout.ObjectField(
            "Headwear", m_HeadwearCategory, typeof(ItemCategory), false);
        m_FacewearCategory = (ItemCategory)EditorGUILayout.ObjectField(
            "Facewear", m_FacewearCategory, typeof(ItemCategory), false);
        m_BackpackCategory = (ItemCategory)EditorGUILayout.ObjectField(
            "Backpack", m_BackpackCategory, typeof(ItemCategory), false);

        if (m_HeadwearCategory == null && m_FacewearCategory == null)
        {
            EditorGUILayout.HelpBox(
                "No equipment categories found. Run 'Project Klyra > Equipment > Generate Equipment Categories' first.",
                MessageType.Warning);
        }

        EditorGUILayout.Space();

        // Filter settings
        EditorGUILayout.LabelField("Include Types", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        m_IncludeHelmets = EditorGUILayout.Toggle("Helmets", m_IncludeHelmets);
        m_IncludeHats = EditorGUILayout.Toggle("Hats", m_IncludeHats);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        m_IncludeGoggles = EditorGUILayout.Toggle("Goggles", m_IncludeGoggles);
        m_IncludeMasks = EditorGUILayout.Toggle("Masks", m_IncludeMasks);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        m_IncludeGlasses = EditorGUILayout.Toggle("Glasses", m_IncludeGlasses);
        m_IncludeArmor = EditorGUILayout.Toggle("Armor", m_IncludeArmor);
        EditorGUILayout.EndHorizontal();
        m_IncludePouches = EditorGUILayout.Toggle("Pouches", m_IncludePouches);

        EditorGUILayout.Space();

        // Scan button
        if (GUILayout.Button("Scan for Synty Attachments", GUILayout.Height(30)))
        {
            ScanForAttachments();
        }

        EditorGUILayout.Space();

        if (m_ScannedForAttachments)
        {
            EditorGUILayout.LabelField($"Found {m_FoundAttachments.Count} attachment prefabs", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All"))
            {
                foreach (var att in m_FoundAttachments) att.selected = true;
            }
            if (GUILayout.Button("Select None"))
            {
                foreach (var att in m_FoundAttachments) att.selected = false;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // List attachments by slot
            DrawAttachmentsBySlot(EquipmentSlotType.Head, "Head (Helmets, Hats)");
            DrawAttachmentsBySlot(EquipmentSlotType.Face, "Face (Masks, Goggles, Glasses)");
            DrawAttachmentsBySlot(EquipmentSlotType.ShoulderLeft, "Shoulders");
            DrawAttachmentsBySlot(EquipmentSlotType.KneeLeft, "Knees");
            DrawAttachmentsBySlot(EquipmentSlotType.Belt, "Belt (Pouches)");
            DrawAttachmentsBySlot(EquipmentSlotType.Back, "Back");

            EditorGUILayout.Space();

            int selectedCount = m_FoundAttachments.Count(a => a.selected);
            if (selectedCount > 0)
            {
                if (GUILayout.Button($"Generate {selectedCount} ItemDefinitions", GUILayout.Height(40)))
                {
                    GenerateItemDefinitions();
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawAttachmentsBySlot(EquipmentSlotType slot, string label)
    {
        var items = m_FoundAttachments.Where(a => a.suggestedSlot == slot).ToList();
        if (items.Count == 0) return;

        EditorGUILayout.LabelField($"{label} ({items.Count})", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        foreach (var att in items)
        {
            EditorGUILayout.BeginHorizontal();
            att.selected = EditorGUILayout.Toggle(att.selected, GUILayout.Width(20));
            EditorGUILayout.LabelField(att.name);
            if (GUILayout.Button("View", GUILayout.Width(50)))
            {
                Selection.activeObject = att.prefab;
                EditorGUIUtility.PingObject(att.prefab);
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.Space();
    }

    // Items that are NOT wearable equipment
    private static readonly string[] NonClothingPatterns = new[]
    {
        // Weapons & Explosives
        "Bomb", "Grenade", "Bullets", "Mags", "Knife", "Holster", "Mace",
        // Items & Tools
        "Camera", "Canteen", "Glowstick", "Radio", "Flashlight", "Torch",
        "Clipboard", "Badge", "Patch", "Piercings", "Zip_Ties", "Cuffs",
        // Body parts (not equipment)
        "Glass_01", // Character glasses mesh, not eyewear
        // Supply items
        "SupplyBag", // This is more like a backpack, handled separately
    };

    // Items that ARE wearable equipment
    private static readonly string[] ClothingPatterns = new[]
    {
        // Head
        "Helmet", "Hat", "Cap", "Hood", "Headset", "Earmuffs", "Headband",
        "Beret", "Bandana",
        // Face
        "Mask", "Goggles", "Glasses", "GasMask", "Eyepatch", "Visor",
        // Body Armor
        "Armour", "Armor", "Vest", "Plate",
        // Accessories
        "Ear_Piece", "Chin_Strap",
        // Back
        "Backpack",
    };

    private void ScanForAttachments()
    {
        m_FoundAttachments.Clear();

        // Search patterns
        string[] searchFolders = new[] { "Assets/Synty" };
        string[] guids = AssetDatabase.FindAssets("SM_Chr_Attach_ t:Prefab", searchFolders);

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);

            // Skip hair, beard, facial features
            if (fileName.Contains("_Hair_") || fileName.Contains("_Beard_") ||
                fileName.Contains("_Moustache_") || fileName.Contains("_Eyebrow"))
            {
                continue;
            }

            // Skip non-clothing items
            if (IsNonClothing(fileName))
            {
                continue;
            }

            // Only include if it matches clothing patterns
            if (!IsClothing(fileName))
            {
                continue;
            }

            // Determine slot type and filter
            var slot = DetermineSlotType(fileName);
            if (!ShouldInclude(fileName, slot)) continue;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            m_FoundAttachments.Add(new AttachmentInfo
            {
                name = CleanName(fileName),
                path = path,
                prefab = prefab,
                suggestedSlot = slot,
                selected = false
            });
        }

        // Sort by name
        m_FoundAttachments = m_FoundAttachments.OrderBy(a => a.name).ToList();
        m_ScannedForAttachments = true;

        Debug.Log($"[EquipmentItemGenerator] Found {m_FoundAttachments.Count} equipment attachments");
    }

    private bool IsNonClothing(string fileName)
    {
        string lower = fileName.ToLower();
        foreach (var pattern in NonClothingPatterns)
        {
            if (lower.Contains(pattern.ToLower()))
            {
                return true;
            }
        }
        return false;
    }

    private bool IsClothing(string fileName)
    {
        string lower = fileName.ToLower();
        foreach (var pattern in ClothingPatterns)
        {
            if (lower.Contains(pattern.ToLower()))
            {
                return true;
            }
        }
        return false;
    }

    private EquipmentSlotType DetermineSlotType(string fileName)
    {
        string lower = fileName.ToLower();

        if (lower.Contains("helmet") || lower.Contains("hat"))
            return EquipmentSlotType.Head;

        if (lower.Contains("mask") || lower.Contains("goggle") || lower.Contains("glass") ||
            lower.Contains("eyepatch") || lower.Contains("gasmask"))
            return EquipmentSlotType.Face;

        if (lower.Contains("shoulder_l") || lower.Contains("_l_"))
            return EquipmentSlotType.ShoulderLeft;
        if (lower.Contains("shoulder_r") || lower.Contains("_r_"))
            return EquipmentSlotType.ShoulderRight;
        if (lower.Contains("shoulder"))
            return EquipmentSlotType.ShoulderLeft; // Default to left

        if (lower.Contains("knee_l"))
            return EquipmentSlotType.KneeLeft;
        if (lower.Contains("knee_r"))
            return EquipmentSlotType.KneeRight;
        if (lower.Contains("knee"))
            return EquipmentSlotType.KneeLeft;

        if (lower.Contains("backpack") || lower.Contains("supplybag"))
            return EquipmentSlotType.Back;

        if (lower.Contains("pouch") || lower.Contains("holster") || lower.Contains("mags") ||
            lower.Contains("knife") || lower.Contains("radio"))
            return EquipmentSlotType.Belt;

        // Default
        return EquipmentSlotType.Head;
    }

    private bool ShouldInclude(string fileName, EquipmentSlotType slot)
    {
        string lower = fileName.ToLower();

        if (lower.Contains("helmet") && !m_IncludeHelmets) return false;
        if (lower.Contains("hat") && !m_IncludeHats) return false;
        if (lower.Contains("goggle") && !m_IncludeGoggles) return false;
        if (lower.Contains("mask") && !m_IncludeMasks) return false;
        if (lower.Contains("glass") && !m_IncludeGlasses) return false;
        if ((lower.Contains("armour") || lower.Contains("armor")) && !m_IncludeArmor) return false;
        if (lower.Contains("pouch") && !m_IncludePouches) return false;

        return true;
    }

    private string CleanName(string fileName)
    {
        // Remove prefix
        string name = fileName.Replace("SM_Chr_Attach_", "");

        // Split by underscore and clean up
        var parts = name.Split('_');
        var cleanParts = new List<string>();

        foreach (var part in parts)
        {
            // Skip numeric suffixes like "01"
            if (int.TryParse(part, out _)) continue;
            // Skip L/R suffixes
            if (part == "L" || part == "R") continue;

            cleanParts.Add(part);
        }

        return string.Join(" ", cleanParts);
    }

    private void GenerateItemDefinitions()
    {
        int created = 0;
        string folder = "Assets/Data/InventoryDatabase/InventoryDatabase/ItemDefinitions";

        var selectedItems = m_FoundAttachments.Where(a => a.selected).ToList();

        foreach (var att in selectedItems)
        {
            // Determine category
            ItemCategory category = null;
            switch (att.suggestedSlot)
            {
                case EquipmentSlotType.Head:
                    category = m_HeadwearCategory;
                    break;
                case EquipmentSlotType.Face:
                    category = m_FacewearCategory;
                    break;
                case EquipmentSlotType.Back:
                    category = m_BackpackCategory;
                    break;
            }

            if (category == null)
            {
                Debug.LogWarning($"No category for {att.name} ({att.suggestedSlot})");
                continue;
            }

            string assetPath = $"{folder}/{att.name}.asset";

            // Check if already exists
            if (AssetDatabase.LoadAssetAtPath<ItemDefinition>(assetPath) != null)
            {
                Debug.Log($"ItemDefinition already exists: {att.name}");
                continue;
            }

            // Create ItemDefinition
            var itemDef = ScriptableObject.CreateInstance<ItemDefinition>();

            // Use reflection to set internal fields
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            var nameField = typeof(ItemDefinition).GetField("m_Name", flags);
            if (nameField != null) nameField.SetValue(itemDef, att.name);

            var idField = typeof(ItemDefinition).GetField("m_ID", flags);
            if (idField != null) idField.SetValue(itemDef, (uint)(att.name.GetHashCode() + System.DateTime.Now.Ticks + created));

            AssetDatabase.CreateAsset(itemDef, assetPath);
            created++;

            Debug.Log($"Created: {att.name}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Equipment Items Generated",
            $"Created {created} ItemDefinitions.\n\n" +
            "Note: You need to assign categories and prefabs in the UIS Database Editor.",
            "OK");

        Debug.Log($"[EquipmentItemGenerator] Created {created} ItemDefinitions");
    }
}
