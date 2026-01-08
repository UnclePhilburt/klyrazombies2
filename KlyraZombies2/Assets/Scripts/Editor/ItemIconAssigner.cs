using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Opsive.UltimateInventorySystem.Core;
using Opsive.UltimateInventorySystem.Core.AttributeSystem;
using Opsive.UltimateInventorySystem.Editor.Utility;
using OpsiveEditorUtility = Opsive.Shared.Editor.Utility.EditorUtility;

/// <summary>
/// Editor tool to assign icons to ItemDefinitions.
/// Can auto-match icons based on name similarity or manual assignment.
/// Includes predefined mappings for Synty Apocalypse HUD icons.
/// </summary>
public class ItemIconAssigner : EditorWindow
{
    [MenuItem("Tools/Item Icon Assigner")]
    public static void ShowWindow()
    {
        GetWindow<ItemIconAssigner>("Item Icon Assigner");
    }

    private Vector2 m_ScrollPosition;
    private string m_IconFolder = "Assets/Synty/InterfaceApocalypseHUD/Sprites";
    private string m_ItemDefinitionsFolder = "Assets/Data/InventoryDatabase/InventoryDatabase/ItemDefinitions";

    // Predefined mappings for icons
    // Key = ItemDefinition name (case-insensitive), Value = icon filename (without extension)
    // Only includes items with GOOD visual matches - no forced/bad matches
    private static readonly Dictionary<string, string> ApocalypseIconMappings = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
    {
        // Medical / Consumables
        { "Pills", "ICON_SM_Item_Pills_01" },
        { "Painkillers", "ICON_SM_Item_Pills_01" },
        { "Bandages", "ICON_SM_Item_HealthKit_01" },

        // Batteries
        { "Battery", "ICON_SM_Item_Battery_01" },
        { "Batteries", "ICON_SM_Item_Battery_02" },

        // Ammo
        { "9mm Rounds", "ICON_SM_Item_Ammo_9mm_Open_01" },
        { "7.62mm Rounds", "ICON_SM_Item_Bullet_Large_01" },

        // Weapons
        { "AK-47", "ICON_SM_Wep_AssaultRifle_02" },
        { "SR-9", "ICON_SM_Wep_Pistol_01" },

        // Food & Drink
        { "Canned Food", "ICON_SM_Item_Can_04" },
        { "Water Bottle", "ICON_SM_Item_Bottle_01" },
        { "Alcohol", "ICON_SM_Item_Alcohol_06" },
        { "Energy Drink", "ICON_SM_Item_Drink_Bottle_02" },
        { "Soda Can", "ICON_SM_Item_Can_04" },
        { "Drink Can", "ICON_SM_Item_Can_04" },

        // Resources
        { "Cash", "ICON_SM_Prop_Money_Strapped_04" },
        { "Hand Sanitizer", "ICON_SM_Prop_SprayBottle_01" },

        // From Clean Vector Icons pack
        { "Key", "T_14_key_" },
        { "Keys", "T_14_key_" },
        { "Book", "T_7_book_" },
        { "Notebook", "T_7_book_" },
        { "Documents", "T_1_letter_" },
        { "Manila Folder", "T_1_letter_" },
        { "Watch", "T_26_clock_" },
        { "Lighter", "T_1_fire_" },
        { "Matches", "T_1_fire_" },

        // Generic scrap for misc small items (Apocalypse HUD)
        { "Rubber Bands", "ICON_SM_Prop_Scav_Scrap_13" },
        { "Paper Clips", "ICON_SM_Prop_Scav_Scrap_13" },
        { "String", "ICON_SM_Prop_Scav_Scrap_27" },
        { "Duct Tape", "ICON_SM_Prop_Scav_Scrap_27" },
        { "Tape Roll", "ICON_SM_Prop_Scav_Scrap_27" },
        { "Cloth Rag", "ICON_SM_Prop_Scav_Scrap_27" },

        // Backpacks (note: Synty has a typo in the filename - "Apocalpyse")
        { "Backpack", "ICON_Apocalpyse_Inventory_Backpack_01" },
        { "Small Backpack", "ICON_Apocalpyse_Inventory_Backpack_01" },
        { "Large Backpack", "ICON_Apocalpyse_Inventory_Backpack_01" },
    };

    // Additional icon folders to search
    private static readonly string[] AdditionalIconFolders = new string[]
    {
        "Assets/Clean Vector Icons",
        "Assets/Synty/InterfaceApocalypseHUD/Sprites/Icons_Inventory"
    };

    private List<ItemIconMapping> m_Mappings = new List<ItemIconMapping>();

    private class ItemIconMapping
    {
        public ItemDefinition itemDefinition;
        public Sprite currentIcon;
        public Sprite newIcon;
        public string itemPath;
        public List<Sprite> suggestedIcons = new List<Sprite>();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Item Icon Assigner", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Assign icons to ItemDefinitions. Click 'Scan' to find items and icons.", MessageType.Info);

        EditorGUILayout.Space(10);

        // Folder settings
        EditorGUILayout.BeginHorizontal();
        m_IconFolder = EditorGUILayout.TextField("Icons Folder", m_IconFolder);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string folder = EditorUtility.OpenFolderPanel("Select Icons Folder", m_IconFolder, "");
            if (!string.IsNullOrEmpty(folder) && folder.StartsWith(Application.dataPath))
            {
                m_IconFolder = "Assets" + folder.Substring(Application.dataPath.Length);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        m_ItemDefinitionsFolder = EditorGUILayout.TextField("ItemDefinitions Folder", m_ItemDefinitionsFolder);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string folder = EditorUtility.OpenFolderPanel("Select ItemDefinitions Folder", m_ItemDefinitionsFolder, "");
            if (!string.IsNullOrEmpty(folder) && folder.StartsWith(Application.dataPath))
            {
                m_ItemDefinitionsFolder = "Assets" + folder.Substring(Application.dataPath.Length);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        if (GUILayout.Button("Scan for Items and Icons"))
        {
            ScanForItemsAndIcons();
        }

        EditorGUILayout.Space(10);

        // Remove any null mappings (deleted items) - do this before drawing
        m_Mappings.RemoveAll(m => m.itemDefinition == null);

        // Display mappings
        if (m_Mappings.Count > 0)
        {
            EditorGUILayout.LabelField($"Found {m_Mappings.Count} Items", EditorStyles.boldLabel);

            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

            // Use for loop with index to ensure we're modifying the actual list items
            for (int i = 0; i < m_Mappings.Count; i++)
            {
                if (m_Mappings[i].itemDefinition == null) continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();

                // Item name
                EditorGUILayout.LabelField(m_Mappings[i].itemDefinition.name, GUILayout.Width(150));

                // Current icon
                EditorGUILayout.BeginVertical(GUILayout.Width(70));
                EditorGUILayout.LabelField("Current:", GUILayout.Width(60));
                m_Mappings[i].currentIcon = (Sprite)EditorGUILayout.ObjectField(m_Mappings[i].currentIcon, typeof(Sprite), false, GUILayout.Width(64), GUILayout.Height(64));
                EditorGUILayout.EndVertical();

                // Arrow
                EditorGUILayout.LabelField("→", GUILayout.Width(20));

                // New icon
                EditorGUILayout.BeginVertical(GUILayout.Width(70));
                EditorGUILayout.LabelField("New:", GUILayout.Width(60));
                Sprite newIconValue = (Sprite)EditorGUILayout.ObjectField(m_Mappings[i].newIcon, typeof(Sprite), false, GUILayout.Width(64), GUILayout.Height(64));
                if (newIconValue != m_Mappings[i].newIcon)
                {
                    m_Mappings[i].newIcon = newIconValue;
                    Debug.Log($"Set newIcon for {m_Mappings[i].itemDefinition.name} to {(newIconValue != null ? newIconValue.name : "NULL")}");
                }
                EditorGUILayout.EndVertical();

                // Suggestions
                if (m_Mappings[i].suggestedIcons.Count > 0)
                {
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField("Suggestions:", GUILayout.Width(80));
                    EditorGUILayout.BeginHorizontal();
                    for (int j = 0; j < Mathf.Min(3, m_Mappings[i].suggestedIcons.Count); j++)
                    {
                        var suggestion = m_Mappings[i].suggestedIcons[j];
                        if (GUILayout.Button(new GUIContent(suggestion.texture), GUILayout.Width(40), GUILayout.Height(40)))
                        {
                            m_Mappings[i].newIcon = suggestion;
                            Debug.Log($"Suggestion clicked: Set newIcon for {m_Mappings[i].itemDefinition.name} to {suggestion.name}");
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            // Count how many have newIcon set
            int readyCount = m_Mappings.Count(m => m.newIcon != null && m.newIcon != m.currentIcon);
            int withSuggestions = m_Mappings.Count(m => m.suggestedIcons.Count > 0);

            EditorGUILayout.HelpBox($"{readyCount} items ready to update. {withSuggestions} items have suggestions.", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button($"Apply All New Icons ({readyCount})", GUILayout.Height(30)))
            {
                ApplyIcons();
            }
            if (GUILayout.Button("Auto-assign All Suggestions", GUILayout.Height(30)))
            {
                foreach (var mapping in m_Mappings)
                {
                    if (mapping.suggestedIcons.Count > 0)
                    {
                        mapping.newIcon = mapping.suggestedIcons[0];
                    }
                }
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear All New Icons", GUILayout.Height(25)))
            {
                foreach (var mapping in m_Mappings)
                {
                    mapping.newIcon = null;
                }
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Icon Database (Workaround)", EditorStyles.boldLabel);
            if (GUILayout.Button("Build Icon Database (Recommended)", GUILayout.Height(30)))
            {
                BuildIconDatabase();
            }
            EditorGUILayout.HelpBox("This creates an ItemIconDatabase asset that the UI can use directly, bypassing UIS attribute issues.", MessageType.Info);
        }
    }

    private void ScanForItemsAndIcons()
    {
        m_Mappings.Clear();

        // Find all ItemDefinitions
        string[] itemGuids = AssetDatabase.FindAssets("t:ItemDefinition", new[] { m_ItemDefinitionsFolder });

        // Find all sprites in icons folder and subfolders
        List<Sprite> allIcons = new List<Sprite>();
        Dictionary<string, Sprite> iconByName = new Dictionary<string, Sprite>(System.StringComparer.OrdinalIgnoreCase);

        // Build list of all folders to search
        List<string> foldersToSearch = new List<string> { m_IconFolder };
        foldersToSearch.AddRange(AdditionalIconFolders);

        foreach (string folder in foldersToSearch)
        {
            if (!Directory.Exists(folder)) continue;

            // Search recursively in all subfolders
            string[] iconGuids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
            foreach (string guid in iconGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null && !allIcons.Contains(sprite))
                {
                    allIcons.Add(sprite);
                    // Store by filename for predefined mapping lookup
                    string filename = Path.GetFileNameWithoutExtension(path);
                    if (!iconByName.ContainsKey(filename))
                    {
                        iconByName[filename] = sprite;
                    }
                }
            }

            // Also load textures as sprites
            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            foreach (string guid in textureGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                // Make sure it's imported as a sprite
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                }

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null && !allIcons.Contains(sprite))
                {
                    allIcons.Add(sprite);
                    string filename = Path.GetFileNameWithoutExtension(path);
                    if (!iconByName.ContainsKey(filename))
                    {
                        iconByName[filename] = sprite;
                    }
                }
            }
        }

        Debug.Log($"Loaded {allIcons.Count} icons from {foldersToSearch.Count} folders");

        // Create mappings
        int predefinedMatches = 0;
        foreach (string guid in itemGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);

            if (item != null)
            {
                // Get current icon via SerializedObject since m_EditorIcon is private
                SerializedObject so = new SerializedObject(item);
                SerializedProperty iconProp = so.FindProperty("m_EditorIcon");
                Sprite currentIcon = iconProp != null ? iconProp.objectReferenceValue as Sprite : null;

                var mapping = new ItemIconMapping
                {
                    itemDefinition = item,
                    itemPath = path,
                    currentIcon = currentIcon
                };

                // First, check predefined Apocalypse HUD mappings
                if (ApocalypseIconMappings.TryGetValue(item.name, out string iconFilename))
                {
                    if (iconByName.TryGetValue(iconFilename, out Sprite predefinedIcon))
                    {
                        mapping.suggestedIcons.Insert(0, predefinedIcon);
                        mapping.newIcon = predefinedIcon;
                        predefinedMatches++;
                    }
                }

                // Then find additional suggestions based on name similarity
                string itemNameLower = item.name.ToLower().Replace(" ", "").Replace("_", "");
                foreach (var icon in allIcons)
                {
                    if (mapping.suggestedIcons.Contains(icon)) continue; // Skip if already suggested

                    string iconNameLower = icon.name.ToLower().Replace(" ", "").Replace("_", "").Replace("icon", "").Replace("sm", "");

                    // Check for name match
                    if (iconNameLower.Contains(itemNameLower) || itemNameLower.Contains(iconNameLower))
                    {
                        mapping.suggestedIcons.Add(icon);
                    }
                    else
                    {
                        // Check individual words
                        string[] itemWords = item.name.ToLower().Split(new[] { ' ', '_' }, System.StringSplitOptions.RemoveEmptyEntries);
                        foreach (string word in itemWords)
                        {
                            if (word.Length > 3 && iconNameLower.Contains(word))
                            {
                                if (!mapping.suggestedIcons.Contains(icon))
                                {
                                    mapping.suggestedIcons.Add(icon);
                                }
                                break;
                            }
                        }
                    }
                }

                // Auto-assign first suggestion if no current icon and no predefined match
                if (mapping.newIcon == null && mapping.suggestedIcons.Count > 0)
                {
                    mapping.newIcon = mapping.suggestedIcons[0];
                }

                m_Mappings.Add(mapping);
            }
        }

        int withSuggestions = m_Mappings.Count(m => m.suggestedIcons.Count > 0);
        int withNewIcon = m_Mappings.Count(m => m.newIcon != null);
        Debug.Log($"Found {m_Mappings.Count} items and {allIcons.Count} icons. {predefinedMatches} predefined matches, {withSuggestions} items have suggestions, {withNewIcon} auto-assigned.");

        // Log first few items without suggestions for debugging
        var noSuggestions = m_Mappings.Where(m => m.suggestedIcons.Count == 0).Take(10).ToList();
        if (noSuggestions.Count > 0)
        {
            Debug.Log("Items without suggestions: " + string.Join(", ", noSuggestions.Select(m => m.itemDefinition.name)));
        }

        // Log first few icons for debugging
        if (allIcons.Count > 0)
        {
            Debug.Log("Sample icons (first 10): " + string.Join(", ", allIcons.Take(10).Select(i => i.name)));
        }
    }

    private void ApplyIcons()
    {
        int count = 0;
        int failed = 0;

        // Debug: Log the state of all mappings
        Debug.LogWarning($"=== ApplyIcons called. Total mappings: {m_Mappings.Count} ===");

        if (m_Mappings.Count == 0)
        {
            Debug.LogError("No mappings! Click 'Scan for Items and Icons' first.");
            return;
        }

        int withNewIcon = 0;
        int sameAsCurrent = 0;
        foreach (var m in m_Mappings)
        {
            if (m.newIcon != null)
            {
                withNewIcon++;
                if (m.newIcon == m.currentIcon)
                    sameAsCurrent++;
            }
        }
        Debug.LogWarning($"Mappings with newIcon set: {withNewIcon}, same as current: {sameAsCurrent}");

        // Start recording undo for all changes
        Undo.SetCurrentGroupName("Apply Item Icons");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (var mapping in m_Mappings)
        {
            // Apply if newIcon is set - even if same as currentIcon, we need to ensure
            // the RUNTIME Icon attribute is also set (not just the editor icon)
            if (mapping.newIcon != null)
            {
                try
                {
                    // Record the object for undo before modifying
                    Undo.RegisterCompleteObjectUndo(mapping.itemDefinition, "Set Item Icon");

                    SerializedObject so = new SerializedObject(mapping.itemDefinition);

                    // Set m_EditorIcon (for Unity Editor display)
                    SerializedProperty editorIconProp = so.FindProperty("m_EditorIcon");
                    if (editorIconProp != null)
                    {
                        editorIconProp.objectReferenceValue = mapping.newIcon;
                    }

                    // Set the runtime Icon attribute by directly modifying serialized data
                    bool setRuntimeIcon = SetIconAttributeViaSerializedProperty(so, mapping.newIcon);

                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(mapping.itemDefinition);
                    mapping.currentIcon = mapping.newIcon;
                    count++;

                    if (setRuntimeIcon)
                    {
                        Debug.Log($"Set icon for '{mapping.itemDefinition.name}' to '{mapping.newIcon.name}'");
                    }
                    else
                    {
                        // No Sprite attribute found - icon won't show in game
                        failed++;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error setting icon for '{mapping.itemDefinition.name}': {e.Message}");
                    failed++;
                }
            }
        }

        Undo.CollapseUndoOperations(undoGroup);

        // Force save all modified assets
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (count == 0)
        {
            Debug.LogWarning("No icons were applied. Make sure items have 'New' icons assigned.");
        }
        else if (failed > 0)
        {
            Debug.LogWarning($"Applied {count} editor icons. {failed} items don't have runtime 'Icon' attribute (icons won't show in-game). Add Icon attribute to categories via UIS Database Editor.");
        }
        else
        {
            Debug.Log($"Successfully applied {count} icons to both editor and runtime!");
        }
    }

    /// <summary>
    /// Sets the Icon attribute by directly modifying serialized data.
    /// If the attribute doesn't exist, it creates one.
    /// </summary>
    private bool SetIconAttributeViaSerializedProperty(SerializedObject so, Sprite newIcon)
    {
        // Navigate to m_ItemDefinitionAttributeCollection.m_AttributeCollectionData
        var attrCollectionProp = so.FindProperty("m_ItemDefinitionAttributeCollection");
        if (attrCollectionProp == null) return false;

        var attrDataArray = attrCollectionProp.FindPropertyRelative("m_AttributeCollectionData");
        if (attrDataArray == null || !attrDataArray.isArray) return false;

        // Find the Sprite attribute (Icon)
        for (int i = 0; i < attrDataArray.arraySize; i++)
        {
            var attrData = attrDataArray.GetArrayElementAtIndex(i);
            var objectType = attrData.FindPropertyRelative("m_ObjectType");

            if (objectType != null && objectType.stringValue.Contains("Sprite"))
            {
                // Found the Icon attribute - set the sprite in m_UnityObjects
                return SetSpriteOnAttribute(attrData, newIcon);
            }
        }

        // No Sprite attribute found - CREATE one!
        return AddIconAttribute(attrDataArray, newIcon);
    }

    /// <summary>
    /// Sets the sprite value on an existing attribute.
    /// </summary>
    private bool SetSpriteOnAttribute(SerializedProperty attrData, Sprite newIcon)
    {
        try
        {
            var unityObjects = attrData.FindPropertyRelative("m_UnityObjects");
            if (unityObjects == null || !unityObjects.isArray) return false;

            // UIS stores 2 copies: default value and override value
            unityObjects.ClearArray();
            unityObjects.InsertArrayElementAtIndex(0);
            unityObjects.InsertArrayElementAtIndex(1);

            var elem0 = unityObjects.GetArrayElementAtIndex(0);
            var elem1 = unityObjects.GetArrayElementAtIndex(1);
            if (elem0 != null) elem0.objectReferenceValue = newIcon;
            if (elem1 != null) elem1.objectReferenceValue = newIcon;

            // Also update m_Values to indicate override is set
            var valuesProp = attrData.FindPropertyRelative("m_Values");
            if (valuesProp != null && valuesProp.propertyType == SerializedPropertyType.String)
            {
                string values = valuesProp.stringValue;
                if (!string.IsNullOrEmpty(values))
                {
                    // Change indices from ffffffffffffffff to 0000000001000000
                    if (values.Length >= 16 && values.EndsWith("ffffffffffffffff"))
                    {
                        values = values.Substring(0, values.Length - 16) + "0000000001000000";
                        // Also set override flag (byte 5, chars 8-9) from 00 to 01
                        if (values.Length >= 10)
                        {
                            char[] chars = values.ToCharArray();
                            chars[8] = '0';
                            chars[9] = '1';
                            values = new string(chars);
                        }
                        valuesProp.stringValue = values;
                    }
                }
            }

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"SetSpriteOnAttribute error: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Adds a new Icon attribute to an item that doesn't have one.
    /// </summary>
    private bool AddIconAttribute(SerializedProperty attrDataArray, Sprite newIcon)
    {
        try
        {
            // Add new element to the array
            int newIndex = attrDataArray.arraySize;
            attrDataArray.InsertArrayElementAtIndex(newIndex);
            var newAttr = attrDataArray.GetArrayElementAtIndex(newIndex);

            if (newAttr == null)
            {
                Debug.LogWarning("Failed to create new attribute element");
                return false;
            }

            // Set the type to Sprite attribute
            var objectType = newAttr.FindPropertyRelative("m_ObjectType");
            if (objectType != null && objectType.propertyType == SerializedPropertyType.String)
            {
                objectType.stringValue = "Opsive.UltimateInventorySystem.Core.AttributeSystem.Attribute`1[[UnityEngine.Sprite, UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]";
            }

            // Set m_LongValueHashes (copied from working Icon attribute)
            var longValueHashes = newAttr.FindPropertyRelative("m_LongValueHashes");
            if (longValueHashes != null && longValueHashes.propertyType == SerializedPropertyType.String)
            {
                longValueHashes.stringValue = "0d00eb254f8d1b295b713f72e636d894086efd392442dff734ebab2aec72d130f446621b49ba2a58a8823c4a519fa651af0255e014ce1cfaf3d4ee4cb64be1ae";
            }

            // Set m_ValuePositions
            var valuePositions = newAttr.FindPropertyRelative("m_ValuePositions");
            if (valuePositions != null && valuePositions.propertyType == SerializedPropertyType.String)
            {
                valuePositions.stringValue = "0000000004000000050000000500000009000000090000000d00000011000000";
            }

            // Set m_Values with "Icon" name and override flag set, indices pointing to objects 0 and 1
            // 49636f6e = "Icon", 01 = override flag set, rest is hashes and indices
            var values = newAttr.FindPropertyRelative("m_Values");
            if (values != null && values.propertyType == SerializedPropertyType.String)
            {
                values.stringValue = "49636f6e0001000000000000000000000001000000";
            }

            // Set m_Version
            var version = newAttr.FindPropertyRelative("m_Version");
            if (version != null && version.propertyType == SerializedPropertyType.String)
            {
                version.stringValue = "3.4";
            }

            // Set m_UnityObjects with the sprite
            var unityObjects = newAttr.FindPropertyRelative("m_UnityObjects");
            if (unityObjects != null && unityObjects.isArray)
            {
                unityObjects.ClearArray();
                unityObjects.InsertArrayElementAtIndex(0);
                unityObjects.InsertArrayElementAtIndex(1);
                var elem0 = unityObjects.GetArrayElementAtIndex(0);
                var elem1 = unityObjects.GetArrayElementAtIndex(1);
                if (elem0 != null) elem0.objectReferenceValue = newIcon;
                if (elem1 != null) elem1.objectReferenceValue = newIcon;
            }

            Debug.Log($"Added new Icon attribute with sprite: {newIcon.name}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"AddIconAttribute error: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Builds an ItemIconDatabase asset from the current mappings.
    /// This is a workaround that bypasses UIS attribute serialization issues.
    /// </summary>
    private void BuildIconDatabase()
    {
        // Create or load existing database
        string dbPath = "Assets/Resources/ItemIconDatabase.asset";
        ItemIconDatabase database = AssetDatabase.LoadAssetAtPath<ItemIconDatabase>(dbPath);

        if (database == null)
        {
            database = ScriptableObject.CreateInstance<ItemIconDatabase>();
            AssetDatabase.CreateAsset(database, dbPath);
        }

        int count = 0;
        foreach (var mapping in m_Mappings)
        {
            Sprite iconToUse = mapping.newIcon ?? mapping.currentIcon;
            if (iconToUse != null && mapping.itemDefinition != null)
            {
                database.AddEntry(mapping.itemDefinition, iconToUse);
                count++;
            }
        }

        database.ClearCache();
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();

        Debug.Log($"Built ItemIconDatabase with {count} icons at {dbPath}");
        EditorGUIUtility.PingObject(database);
    }
}
