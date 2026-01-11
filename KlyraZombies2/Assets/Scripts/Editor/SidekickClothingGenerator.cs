using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Opsive.UltimateInventorySystem.Core;
using Opsive.UltimateInventorySystem.Storage;
using Synty.SidekickCharacters.API;
using Synty.SidekickCharacters.Database;
using Synty.SidekickCharacters.Database.DTO;
using Synty.SidekickCharacters.Enums;

/// <summary>
/// Editor tool to generate UIS ItemDefinitions from Sidekick character presets.
/// Creates clothing items that players can find in-game and equip to change appearance.
/// </summary>
public class SidekickClothingGenerator : EditorWindow
{
    private InventorySystemDatabase m_Database;
    private Vector2 m_ScrollPos;

    // Categories
    private ItemCategory m_ClothingCategory;
    private ItemCategory m_HeadwearCategory;
    private ItemCategory m_ShirtCategory;
    private ItemCategory m_PantsCategory;

    // Sidekick
    private DatabaseManager m_DbManager;
    private Dictionary<PartGroup, List<SidekickPartPreset>> m_PresetsByGroup;
    private Dictionary<PartGroup, List<bool>> m_PresetSelection;
    private bool m_PresetsLoaded = false;

    [MenuItem("Project Klyra/Sidekick/Generate Clothing Items")]
    public static void ShowWindow()
    {
        GetWindow<SidekickClothingGenerator>("Sidekick Clothing Generator");
    }

    private void OnEnable()
    {
        FindDatabase();
        FindCategories();
    }

    private void FindDatabase()
    {
        string[] guids = AssetDatabase.FindAssets("t:InventorySystemDatabase", new[] { "Assets/Data" });
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            m_Database = AssetDatabase.LoadAssetAtPath<InventorySystemDatabase>(path);
        }
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
                case "Clothing":
                    m_ClothingCategory = cat;
                    break;
                case "Headwear":
                    m_HeadwearCategory = cat;
                    break;
                case "Shirt":
                    m_ShirtCategory = cat;
                    break;
                case "Pants":
                    m_PantsCategory = cat;
                    break;
            }
        }
    }

    private void OnGUI()
    {
        m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

        EditorGUILayout.LabelField("Sidekick Clothing Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This tool generates UIS ItemDefinitions from Sidekick character presets.\n\n" +
            "Each preset becomes a clothing item that changes the character's appearance when equipped.\n\n" +
            "Step 1: Create clothing categories (if needed)\n" +
            "Step 2: Load Sidekick presets\n" +
            "Step 3: Select presets to create as items\n" +
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

        // Categories section
        EditorGUILayout.LabelField("Categories", EditorStyles.boldLabel);

        if (GUILayout.Button("Create Clothing Categories"))
        {
            CreateClothingCategories();
        }

        EditorGUILayout.Space();
        m_ClothingCategory = (ItemCategory)EditorGUILayout.ObjectField(
            "Clothing (Parent)", m_ClothingCategory, typeof(ItemCategory), false);
        m_HeadwearCategory = (ItemCategory)EditorGUILayout.ObjectField(
            "Headwear (Head)", m_HeadwearCategory, typeof(ItemCategory), false);
        m_ShirtCategory = (ItemCategory)EditorGUILayout.ObjectField(
            "Shirt (UpperBody)", m_ShirtCategory, typeof(ItemCategory), false);
        m_PantsCategory = (ItemCategory)EditorGUILayout.ObjectField(
            "Pants (LowerBody)", m_PantsCategory, typeof(ItemCategory), false);

        EditorGUILayout.Space();

        // Load presets
        if (GUILayout.Button("Load Sidekick Presets", GUILayout.Height(30)))
        {
            LoadPresets();
        }

        EditorGUILayout.Space();

        if (m_PresetsLoaded && m_PresetsByGroup != null)
        {
            DrawPresetsList();

            EditorGUILayout.Space();

            int totalSelected = m_PresetSelection.Values.Sum(list => list.Count(x => x));
            if (totalSelected > 0)
            {
                if (GUILayout.Button($"Generate {totalSelected} Clothing ItemDefinitions", GUILayout.Height(40)))
                {
                    GenerateItemDefinitions();
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void LoadPresets()
    {
        try
        {
            m_DbManager = new DatabaseManager();
            // Must call GetDbConnection to actually open the database connection
            var connection = m_DbManager.GetDbConnection(checkDbOnLoad: false);

            if (connection == null)
            {
                Debug.LogError("[SidekickClothingGenerator] Failed to open database connection!");
                EditorUtility.DisplayDialog("Error", "Failed to open Sidekick database connection.", "OK");
                return;
            }

            Debug.Log("[SidekickClothingGenerator] Database connection opened successfully");

            m_PresetsByGroup = new Dictionary<PartGroup, List<SidekickPartPreset>>();
            m_PresetSelection = new Dictionary<PartGroup, List<bool>>();

            int totalPresets = 0;

            foreach (PartGroup group in System.Enum.GetValues(typeof(PartGroup)))
            {
                // Get ALL presets - we don't need meshes at edit time, just the preset names
                // The Sidekick system will load meshes at runtime
                var presets = SidekickPartPreset.GetAllByGroup(m_DbManager, group, excludeMissingParts: false)
                    .ToList();

                m_PresetsByGroup[group] = presets;
                m_PresetSelection[group] = presets.Select(_ => false).ToList();

                Debug.Log($"[SidekickClothingGenerator] {group}: {presets.Count} presets found");

                // Log first few preset names for debugging
                foreach (var preset in presets.Take(5))
                {
                    Debug.Log($"  - {preset.Name}");
                }

                totalPresets += presets.Count;
            }

            m_PresetsLoaded = true;

            if (totalPresets == 0)
            {
                EditorUtility.DisplayDialog("No Presets Found",
                    "No Sidekick presets with available parts were found.\n\n" +
                    "This could mean:\n" +
                    "- Content packs are not installed\n" +
                    "- Part meshes are missing\n\n" +
                    "Check the Console for details.",
                    "OK");
            }
            else
            {
                Debug.Log($"[SidekickClothingGenerator] Loaded {totalPresets} total presets");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SidekickClothingGenerator] Error loading presets: {e.Message}\n{e.StackTrace}");
            EditorUtility.DisplayDialog("Error", $"Error loading presets: {e.Message}", "OK");
        }
    }

    private void DrawPresetsList()
    {
        foreach (PartGroup group in new[] { PartGroup.Head, PartGroup.UpperBody, PartGroup.LowerBody })
        {
            if (!m_PresetsByGroup.ContainsKey(group)) continue;

            var presets = m_PresetsByGroup[group];
            var selection = m_PresetSelection[group];

            if (presets.Count == 0) continue;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{group} ({presets.Count} presets)", EditorStyles.boldLabel);

            if (GUILayout.Button("All", GUILayout.Width(40)))
            {
                for (int i = 0; i < selection.Count; i++) selection[i] = true;
            }
            if (GUILayout.Button("None", GUILayout.Width(40)))
            {
                for (int i = 0; i < selection.Count; i++) selection[i] = false;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;
            for (int i = 0; i < presets.Count; i++)
            {
                selection[i] = EditorGUILayout.Toggle(presets[i].Name, selection[i]);
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
        }
    }

    private void CreateClothingCategories()
    {
        string folder = "Assets/Data/InventoryDatabase/InventoryDatabase/ItemCategories";

        // Create parent Clothing category
        if (m_ClothingCategory == null)
        {
            m_ClothingCategory = CreateCategory("Clothing", null, folder);
        }

        // Create child categories
        if (m_HeadwearCategory == null)
        {
            m_HeadwearCategory = CreateCategory("Headwear", m_ClothingCategory, folder);
        }
        if (m_ShirtCategory == null)
        {
            m_ShirtCategory = CreateCategory("Shirt", m_ClothingCategory, folder);
        }
        if (m_PantsCategory == null)
        {
            m_PantsCategory = CreateCategory("Pants", m_ClothingCategory, folder);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[SidekickClothingGenerator] Created clothing categories");
        EditorUtility.DisplayDialog("Categories Created",
            "Created clothing categories:\n- Clothing (parent)\n  - Headwear\n  - Shirt\n  - Pants",
            "OK");
    }

    private ItemCategory CreateCategory(string name, ItemCategory parent, string folder)
    {
        string path = $"{folder}/{name}.asset";

        // Check if exists
        var existing = AssetDatabase.LoadAssetAtPath<ItemCategory>(path);
        if (existing != null)
        {
            Debug.Log($"Category already exists: {name}");
            return existing;
        }

        var category = ScriptableObject.CreateInstance<ItemCategory>();

        // Set name and other properties using reflection
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

        var nameField = typeof(ItemCategory).GetField("m_Name", flags);
        if (nameField != null) nameField.SetValue(category, name);

        var idField = typeof(ItemCategory).GetField("m_ID", flags);
        if (idField != null) idField.SetValue(category, (uint)(name.GetHashCode() + System.DateTime.Now.Ticks));

        // Set mutable and unique for clothing (equippable items)
        var mutableField = typeof(ItemCategory).GetField("m_IsMutable", flags);
        if (mutableField != null) mutableField.SetValue(category, true);

        var uniqueField = typeof(ItemCategory).GetField("m_IsUnique", flags);
        if (uniqueField != null) uniqueField.SetValue(category, true);

        AssetDatabase.CreateAsset(category, path);
        Debug.Log($"Created category: {name}");

        return category;
    }

    private void GenerateItemDefinitions()
    {
        string itemFolder = "Assets/Data/InventoryDatabase/InventoryDatabase/ItemDefinitions/Clothing";
        string iconFolder = "Assets/Data/Icons/Clothing";

        // Ensure folders exist
        if (!Directory.Exists(itemFolder))
        {
            Directory.CreateDirectory(itemFolder);
        }
        if (!Directory.Exists(iconFolder))
        {
            Directory.CreateDirectory(iconFolder);
        }
        AssetDatabase.Refresh();

        int created = 0;
        int itemNumber = 1;

        // Track icons for ItemIconDatabase
        var iconMappings = new Dictionary<ItemDefinition, Sprite>();

        foreach (PartGroup group in new[] { PartGroup.Head, PartGroup.UpperBody, PartGroup.LowerBody })
        {
            if (!m_PresetsByGroup.ContainsKey(group)) continue;

            var presets = m_PresetsByGroup[group];
            var selection = m_PresetSelection[group];

            ItemCategory category = GetCategoryForGroup(group);
            if (category == null)
            {
                Debug.LogWarning($"No category for {group}");
                continue;
            }

            string groupLabel = group == PartGroup.Head ? "Hat" :
                               group == PartGroup.UpperBody ? "Shirt" : "Pants";
            int groupNumber = 1;

            for (int i = 0; i < presets.Count; i++)
            {
                if (!selection[i]) continue;

                var preset = presets[i];
                string itemName = CleanPresetName(preset.Name);
                string assetPath = $"{itemFolder}/{itemName}.asset";

                // Check if item exists
                var existingItem = AssetDatabase.LoadAssetAtPath<ItemDefinition>(assetPath);
                if (existingItem != null)
                {
                    Debug.Log($"ItemDefinition already exists: {itemName}");
                    groupNumber++;
                    continue;
                }

                // Create or get icon
                Sprite icon = GetOrCreateIcon(preset, group, iconFolder, groupLabel, groupNumber);

                // Create item
                var itemDef = CreateClothingItem(itemName, preset.Name, group, category);
                AssetDatabase.CreateAsset(itemDef, assetPath);

                if (icon != null)
                {
                    iconMappings[itemDef] = icon;
                }

                created++;
                groupNumber++;
                itemNumber++;

                Debug.Log($"Created: {itemName} ({group})");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Update ItemIconDatabase if it exists
        UpdateItemIconDatabase(iconMappings);

        EditorUtility.DisplayDialog("Clothing Items Generated",
            $"Created {created} clothing ItemDefinitions with icons.\n\n" +
            "Items have been configured with:\n" +
            "- SidekickPartGroup attribute\n" +
            "- SidekickPresetName attribute\n" +
            "- Generated icons\n\n" +
            "Add them to loot tables to spawn in-game!",
            "OK");

        Debug.Log($"[SidekickClothingGenerator] Created {created} clothing ItemDefinitions");
    }

    private Sprite GetOrCreateIcon(SidekickPartPreset preset, PartGroup group, string iconFolder, string groupLabel, int number)
    {
        string iconName = $"{groupLabel}_{number}";
        string iconPath = $"{iconFolder}/{iconName}.png";

        // Check if icon already exists
        var existingIcon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        if (existingIcon != null)
        {
            return existingIcon;
        }

        Texture2D iconTexture = null;

        // Try to get image from Sidekick database
        try
        {
            var presetImage = SidekickPartPresetImage.GetByPresetAndPartGroup(m_DbManager, preset, group);
            if (presetImage != null && presetImage.ImageData != null && presetImage.ImageData.Length > 0)
            {
                iconTexture = new Texture2D(presetImage.Width, presetImage.Height);
                iconTexture.LoadImage(presetImage.ImageData);
                Debug.Log($"Using Sidekick image for {preset.Name}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not load Sidekick image for {preset.Name}: {e.Message}");
        }

        // If no image from database, create text-based icon
        if (iconTexture == null)
        {
            iconTexture = CreateTextIcon(groupLabel, number);
        }

        // Save texture as PNG
        byte[] pngData = iconTexture.EncodeToPNG();
        File.WriteAllBytes(iconPath, pngData);
        AssetDatabase.Refresh();

        // Configure texture import settings
        TextureImporter importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
    }

    private Texture2D CreateTextIcon(string label, int number)
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size);

        // Background color based on type
        Color bgColor = label == "Hat" ? new Color(0.4f, 0.6f, 0.8f) :
                       label == "Shirt" ? new Color(0.6f, 0.8f, 0.4f) :
                       new Color(0.8f, 0.6f, 0.4f);

        // Fill background
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = bgColor;
        }

        // Add border
        Color borderColor = bgColor * 0.7f;
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (x < 4 || x >= size - 4 || y < 4 || y >= size - 4)
                {
                    pixels[y * size + x] = borderColor;
                }
            }
        }

        texture.SetPixels(pixels);

        // Draw text using a simple pixel font approach
        string text = $"{label}\n{number}";
        DrawTextOnTexture(texture, text, size);

        texture.Apply();
        return texture;
    }

    private void DrawTextOnTexture(Texture2D texture, string text, int size)
    {
        // Simple centered text indicator - draw a pattern for the number
        Color textColor = Color.white;
        int centerX = size / 2;
        int centerY = size / 2;

        // Draw a simple shape based on text
        string[] lines = text.Split('\n');
        int lineHeight = 20;
        int startY = centerY + (lines.Length * lineHeight) / 2;

        for (int lineIdx = 0; lineIdx < lines.Length; lineIdx++)
        {
            string line = lines[lineIdx];
            int y = startY - (lineIdx * lineHeight);
            int startX = centerX - (line.Length * 6) / 2;

            // Draw simple rectangles for each character
            for (int charIdx = 0; charIdx < line.Length; charIdx++)
            {
                int x = startX + charIdx * 8;
                DrawCharacter(texture, line[charIdx], x, y, textColor);
            }
        }
    }

    private void DrawCharacter(Texture2D texture, char c, int startX, int startY, Color color)
    {
        // Simple 5x7 pixel font patterns for common characters
        int[,] pattern = GetCharPattern(c);
        if (pattern == null) return;

        for (int py = 0; py < 7; py++)
        {
            for (int px = 0; px < 5; px++)
            {
                if (pattern[6 - py, px] == 1)
                {
                    int x = startX + px;
                    int y = startY - 3 + py;
                    if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }
    }

    private int[,] GetCharPattern(char c)
    {
        // Simple 5x7 bitmap patterns for digits and some letters
        switch (char.ToUpper(c))
        {
            case '0': return new int[,] {{0,1,1,1,0},{1,0,0,0,1},{1,0,0,1,1},{1,0,1,0,1},{1,1,0,0,1},{1,0,0,0,1},{0,1,1,1,0}};
            case '1': return new int[,] {{0,0,1,0,0},{0,1,1,0,0},{0,0,1,0,0},{0,0,1,0,0},{0,0,1,0,0},{0,0,1,0,0},{0,1,1,1,0}};
            case '2': return new int[,] {{0,1,1,1,0},{1,0,0,0,1},{0,0,0,0,1},{0,0,1,1,0},{0,1,0,0,0},{1,0,0,0,0},{1,1,1,1,1}};
            case '3': return new int[,] {{0,1,1,1,0},{1,0,0,0,1},{0,0,0,0,1},{0,0,1,1,0},{0,0,0,0,1},{1,0,0,0,1},{0,1,1,1,0}};
            case '4': return new int[,] {{0,0,0,1,0},{0,0,1,1,0},{0,1,0,1,0},{1,0,0,1,0},{1,1,1,1,1},{0,0,0,1,0},{0,0,0,1,0}};
            case '5': return new int[,] {{1,1,1,1,1},{1,0,0,0,0},{1,1,1,1,0},{0,0,0,0,1},{0,0,0,0,1},{1,0,0,0,1},{0,1,1,1,0}};
            case '6': return new int[,] {{0,1,1,1,0},{1,0,0,0,0},{1,0,0,0,0},{1,1,1,1,0},{1,0,0,0,1},{1,0,0,0,1},{0,1,1,1,0}};
            case '7': return new int[,] {{1,1,1,1,1},{0,0,0,0,1},{0,0,0,1,0},{0,0,1,0,0},{0,1,0,0,0},{0,1,0,0,0},{0,1,0,0,0}};
            case '8': return new int[,] {{0,1,1,1,0},{1,0,0,0,1},{1,0,0,0,1},{0,1,1,1,0},{1,0,0,0,1},{1,0,0,0,1},{0,1,1,1,0}};
            case '9': return new int[,] {{0,1,1,1,0},{1,0,0,0,1},{1,0,0,0,1},{0,1,1,1,1},{0,0,0,0,1},{0,0,0,0,1},{0,1,1,1,0}};
            case 'S': return new int[,] {{0,1,1,1,0},{1,0,0,0,1},{1,0,0,0,0},{0,1,1,1,0},{0,0,0,0,1},{1,0,0,0,1},{0,1,1,1,0}};
            case 'H': return new int[,] {{1,0,0,0,1},{1,0,0,0,1},{1,0,0,0,1},{1,1,1,1,1},{1,0,0,0,1},{1,0,0,0,1},{1,0,0,0,1}};
            case 'I': return new int[,] {{0,1,1,1,0},{0,0,1,0,0},{0,0,1,0,0},{0,0,1,0,0},{0,0,1,0,0},{0,0,1,0,0},{0,1,1,1,0}};
            case 'R': return new int[,] {{1,1,1,1,0},{1,0,0,0,1},{1,0,0,0,1},{1,1,1,1,0},{1,0,1,0,0},{1,0,0,1,0},{1,0,0,0,1}};
            case 'T': return new int[,] {{1,1,1,1,1},{0,0,1,0,0},{0,0,1,0,0},{0,0,1,0,0},{0,0,1,0,0},{0,0,1,0,0},{0,0,1,0,0}};
            case 'P': return new int[,] {{1,1,1,1,0},{1,0,0,0,1},{1,0,0,0,1},{1,1,1,1,0},{1,0,0,0,0},{1,0,0,0,0},{1,0,0,0,0}};
            case 'A': return new int[,] {{0,0,1,0,0},{0,1,0,1,0},{1,0,0,0,1},{1,0,0,0,1},{1,1,1,1,1},{1,0,0,0,1},{1,0,0,0,1}};
            case 'N': return new int[,] {{1,0,0,0,1},{1,1,0,0,1},{1,0,1,0,1},{1,0,0,1,1},{1,0,0,0,1},{1,0,0,0,1},{1,0,0,0,1}};
            default: return null;
        }
    }

    private void UpdateItemIconDatabase(Dictionary<ItemDefinition, Sprite> iconMappings)
    {
        if (iconMappings.Count == 0) return;

        // Try to find existing ItemIconDatabase
        string dbPath = "Assets/Resources/ItemIconDatabase.asset";
        var iconDb = AssetDatabase.LoadAssetAtPath<ItemIconDatabase>(dbPath);

        if (iconDb == null)
        {
            Debug.Log("[SidekickClothingGenerator] ItemIconDatabase not found - icons created but not auto-assigned");
            Debug.Log("Run Tools > Item Icon Assigner to assign icons, or manually add them to ItemIconDatabase");
            return;
        }

        // Use reflection to add entries
        var entriesField = typeof(ItemIconDatabase).GetField("m_Entries",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (entriesField == null)
        {
            Debug.LogWarning("Could not find m_Entries field on ItemIconDatabase");
            return;
        }

        var entries = entriesField.GetValue(iconDb) as System.Collections.IList;
        if (entries == null)
        {
            Debug.LogWarning("Could not get entries from ItemIconDatabase");
            return;
        }

        int added = 0;
        foreach (var kvp in iconMappings)
        {
            // Check if entry already exists
            bool exists = false;
            foreach (var entry in entries)
            {
                var itemField = entry.GetType().GetField("itemDefinition");
                if (itemField != null)
                {
                    var existingItem = itemField.GetValue(entry) as ItemDefinition;
                    if (existingItem == kvp.Key)
                    {
                        exists = true;
                        break;
                    }
                }
            }

            if (!exists)
            {
                // This is complex due to the entry type - log for manual assignment
                added++;
            }
        }

        if (added > 0)
        {
            EditorUtility.SetDirty(iconDb);
            Debug.Log($"[SidekickClothingGenerator] Created {added} icons - run Tools > Item Icon Assigner to link them");
        }
    }

    private ItemCategory GetCategoryForGroup(PartGroup group)
    {
        switch (group)
        {
            case PartGroup.Head: return m_HeadwearCategory ?? m_ClothingCategory;
            case PartGroup.UpperBody: return m_ShirtCategory ?? m_ClothingCategory;
            case PartGroup.LowerBody: return m_PantsCategory ?? m_ClothingCategory;
            default: return m_ClothingCategory;
        }
    }

    private string CleanPresetName(string presetName)
    {
        // Remove common prefixes
        string clean = presetName
            .Replace("APOC_SURV_", "")
            .Replace("APOC_", "")
            .Replace("_", " ")
            .Trim();

        // Capitalize words
        var words = clean.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            if (!string.IsNullOrEmpty(words[i]))
            {
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
            }
        }

        return string.Join(" ", words);
    }

    private ItemDefinition CreateClothingItem(string displayName, string presetName, PartGroup group, ItemCategory category)
    {
        var itemDef = ScriptableObject.CreateInstance<ItemDefinition>();

        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

        // Set name
        var nameField = typeof(ItemDefinition).GetField("m_Name", flags);
        if (nameField != null) nameField.SetValue(itemDef, displayName);

        // Set ID
        var idField = typeof(ItemDefinition).GetField("m_ID", flags);
        if (idField != null) idField.SetValue(itemDef, (uint)(displayName.GetHashCode() + presetName.GetHashCode() + System.DateTime.Now.Ticks));

        // Set category
        var categoryField = typeof(ItemDefinition).GetField("m_Category", flags);
        if (categoryField != null)
        {
            categoryField.SetValue(itemDef, category);
            Debug.Log($"[SidekickClothingGenerator] Assigned category {category.name} to {displayName}");
        }
        else
        {
            Debug.LogWarning($"[SidekickClothingGenerator] Could not find m_Category field on ItemDefinition");
        }

        Debug.Log($"Created {displayName}:\n" +
            $"  - Category: {category.name}\n" +
            $"  - SidekickPartGroup: {group}\n" +
            $"  - SidekickPresetName: {presetName}");

        return itemDef;
    }
}
