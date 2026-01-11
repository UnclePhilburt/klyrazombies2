using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Finds prefabs that match item definition names and copies them to a folder for icon generation.
/// </summary>
public class ItemPrefabFinder : EditorWindow
{
    [MenuItem("Project Klyra/Items/Item Prefab Finder")]
    public static void ShowWindow()
    {
        GetWindow<ItemPrefabFinder>("Item Prefab Finder");
    }

    private Vector2 m_ScrollPosition;
    private List<ItemPrefabMatch> m_Matches = new List<ItemPrefabMatch>();
    private string m_OutputFolder = "Assets/Data/ItemPrefabs";

    private class ItemPrefabMatch
    {
        public string itemName;
        public List<GameObject> matchingPrefabs = new List<GameObject>();
        public GameObject selectedPrefab;
        public bool expanded;
    }

    // Direct mapping from item names to EXACT Synty prefab names (without .prefab extension)
    // These are the actual prefab names found in the Synty folders
    private static Dictionary<string, string[]> s_ExactPrefabNames = new Dictionary<string, string[]>
    {
        // Weapons
        { "AK-47", new[] { "SM_Wep_AssaultRifle_01", "SM_Wep_AssaultRifle_02", "SM_Wep_AssaultRifle_03" } },
        { "SR-9", new[] { "SM_Wep_Pistol_01", "SM_Wep_Pistol_02", "SM_Wep_Pistol_03", "SM_Wep_Pistol_04" } },

        // Ammo
        { "9mm Rounds", new[] { "SM_Item_Ammo_9mm_01", "SM_Item_Ammo_9mm_02" } },
        { "7.62mm Rounds", new[] { "SM_Item_Ammo_762_01", "SM_Item_Ammo_762_02" } },

        // Consumables / Food & Drink
        { "Alcohol", new[] { "SM_Item_Alcohol_01", "SM_Item_Alcohol_02", "SM_Item_Alcohol_03" } },
        { "Drink Can", new[] { "SM_Item_Can_01", "SM_Item_Can_02", "SM_Item_Can_03", "SM_Item_Can_04" } },
        { "Soda Can", new[] { "SM_Item_Can_01", "SM_Item_Can_02", "SM_Item_Can_03" } },
        { "Energy Drink", new[] { "SM_Item_Can_01", "SM_Item_EnergyDrink_01" } },
        { "Canned Food", new[] { "SM_Item_Can_01", "SM_Item_Can_02", "SM_Item_Food_Can_01" } },
        { "Water Bottle", new[] { "SM_Item_Bottle_01", "SM_Item_Bottle_02", "SM_Item_WaterBottle_01" } },
        { "Coffee Mug", new[] { "SM_Item_Mug_01", "SM_Item_Cup_01", "SM_Prop_Mug_01" } },
        { "Chips", new[] { "SM_Item_Chips_01", "SM_Item_Snack_01", "SM_Prop_Shop_Goods_01" } },
        { "Candy Bar", new[] { "SM_Item_Chocolate_01", "SM_Item_Candy_01", "SM_Item_Snack_01" } },

        // Medical
        { "Bandages", new[] { "SM_Item_Bandage_01", "SM_Item_FirstAid_01", "SM_Item_MedKit_01" } },
        { "Pills", new[] { "SM_Item_Pills_01", "SM_Item_Medicine_01" } },
        { "Painkillers", new[] { "SM_Item_Pills_01", "SM_Item_Medicine_01", "SM_Item_Painkiller_01" } },
        { "Hand Sanitizer", new[] { "SM_Item_Sanitizer_01", "SM_Item_Bottle_01" } },

        // Electronics
        { "Battery", new[] { "SM_Item_Battery_01", "SM_Item_Battery_02" } },
        { "Batteries", new[] { "SM_Item_Battery_01", "SM_Item_Battery_02" } },
        { "Flashlight", new[] { "SM_Item_Flashlight_01", "SM_Item_Torch_01", "SM_Wep_Flashlight_01" } },
        { "Smartphone", new[] { "SM_Item_Phone_01", "SM_Item_SmartPhone_01", "SM_Item_Mobile_01" } },
        { "Walkie Talkie", new[] { "SM_Item_WalkieTalkie_01", "SM_Item_Radio_01" } },
        { "Calculator", new[] { "SM_Item_Calculator_01", "SM_Prop_Calculator_01" } },
        { "USB Drive", new[] { "SM_Item_USB_01", "SM_Item_FlashDrive_01" } },
        { "Watch", new[] { "SM_Item_Watch_01", "SM_Prop_Watch_01" } },

        // Office Supplies
        { "Book", new[] { "SM_Item_Book_01", "SM_Item_Book_02", "SM_Item_Book_03", "SM_Prop_Book_01" } },
        { "Notebook", new[] { "SM_Item_Notebook_01", "SM_Item_Book_01", "SM_Prop_Notebook_01" } },
        { "Pen", new[] { "SM_Item_Pen_01", "SM_Prop_Pen_01" } },
        { "Pencil", new[] { "SM_Item_Pencil_01", "SM_Prop_Pencil_01" } },
        { "Clipboard", new[] { "SM_Item_Clipboard_01", "SM_Prop_Clipboard_01" } },
        { "Stapler", new[] { "SM_Item_Stapler_01", "SM_Prop_Stapler_01" } },
        { "Scissors", new[] { "SM_Item_Scissors_01", "SM_Prop_Scissors_01" } },
        { "Paper Clips", new[] { "SM_Item_PaperClip_01", "SM_Prop_PaperClip_01" } },
        { "Documents", new[] { "SM_Item_Document_01", "SM_Item_Paper_01", "SM_Prop_Paper_01" } },
        { "Manila Folder", new[] { "SM_Item_Folder_01", "SM_Prop_Folder_01", "SM_Item_Document_01" } },

        // Tools & Misc
        { "Duct Tape", new[] { "SM_Item_Tape_01", "SM_Item_DuctTape_01" } },
        { "Tape Roll", new[] { "SM_Item_Tape_01", "SM_Item_TapeRoll_01" } },
        { "String", new[] { "SM_Item_Rope_01", "SM_Item_Rope_Detailed_01", "SM_Item_String_01" } },
        { "Cloth Rag", new[] { "SM_Item_Cloth_01", "SM_Item_Rag_01", "SM_Item_Fabric_01" } },
        { "Rubber Bands", new[] { "SM_Item_RubberBand_01" } },
        { "Lighter", new[] { "SM_Item_Lighter_01", "SM_Item_Lighter_Flip_01" } },
        { "Matches", new[] { "SM_Item_Matches_01", "SM_Item_Match_01" } },
        { "Key", new[] { "SM_Item_Key_01", "SM_Prop_Key_01" } },
        { "Keys", new[] { "SM_Item_Key_01", "SM_Item_Keys_01", "SM_Prop_Keys_01" } },
        { "Cash", new[] { "SM_Item_Cash_01", "SM_Item_Money_01", "SM_Prop_Money_01" } },
        { "Cigarette", new[] { "SM_Item_Cigarette_01", "SM_Item_Cigarettes_01" } },
    };

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Item Prefab Finder", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Finds prefabs matching your item definitions. Click 'Find Matches' to search.", MessageType.Info);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Find Matching Prefabs", GUILayout.Height(30)))
        {
            FindMatchingPrefabs();
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        m_OutputFolder = EditorGUILayout.TextField("Output Folder", m_OutputFolder);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string folder = EditorUtility.OpenFolderPanel("Select Output Folder", m_OutputFolder, "");
            if (!string.IsNullOrEmpty(folder) && folder.StartsWith(Application.dataPath))
            {
                m_OutputFolder = "Assets" + folder.Substring(Application.dataPath.Length);
            }
        }
        EditorGUILayout.EndHorizontal();

        if (m_Matches.Count > 0 && GUILayout.Button("Copy Selected Prefabs to Output Folder", GUILayout.Height(25)))
        {
            CopySelectedPrefabs();
        }

        EditorGUILayout.Space(10);

        // Display matches
        if (m_Matches.Count > 0)
        {
            EditorGUILayout.LabelField($"Found {m_Matches.Count} Items", EditorStyles.boldLabel);

            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

            foreach (var match in m_Matches)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();

                // Status icon
                if (match.selectedPrefab != null)
                {
                    EditorGUILayout.LabelField("✓", GUILayout.Width(20));
                }
                else if (match.matchingPrefabs.Count > 0)
                {
                    EditorGUILayout.LabelField("?", GUILayout.Width(20));
                }
                else
                {
                    EditorGUILayout.LabelField("✗", GUILayout.Width(20));
                }

                // Item name
                EditorGUILayout.LabelField(match.itemName, EditorStyles.boldLabel, GUILayout.Width(150));

                // Selected prefab
                match.selectedPrefab = (GameObject)EditorGUILayout.ObjectField(
                    match.selectedPrefab, typeof(GameObject), false, GUILayout.Width(200));

                // Expand button if there are multiple matches
                if (match.matchingPrefabs.Count > 1)
                {
                    if (GUILayout.Button(match.expanded ? "▼" : "▶", GUILayout.Width(25)))
                    {
                        match.expanded = !match.expanded;
                    }
                    EditorGUILayout.LabelField($"({match.matchingPrefabs.Count} found)", GUILayout.Width(80));
                }
                else if (match.matchingPrefabs.Count == 1)
                {
                    EditorGUILayout.LabelField("(1 found)", GUILayout.Width(80));
                }
                else
                {
                    EditorGUILayout.LabelField("(none)", GUILayout.Width(80));
                }

                EditorGUILayout.EndHorizontal();

                // Show expanded list
                if (match.expanded && match.matchingPrefabs.Count > 1)
                {
                    EditorGUI.indentLevel++;
                    foreach (var prefab in match.matchingPrefabs)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("", GUILayout.Width(20));
                        if (GUILayout.Button("Select", GUILayout.Width(50)))
                        {
                            match.selectedPrefab = prefab;
                            match.expanded = false;
                        }
                        EditorGUILayout.ObjectField(prefab, typeof(GameObject), false);
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void FindMatchingPrefabs()
    {
        m_Matches.Clear();

        // Find all item definitions
        string[] itemGuids = AssetDatabase.FindAssets("t:ScriptableObject",
            new[] { "Assets/Data/InventoryDatabase/InventoryDatabase/ItemDefinitions" });

        // Find all prefabs in the entire Assets folder (Synty assets may be in various locations)
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");

        // Build a dictionary for fast lookup by prefab name
        Dictionary<string, List<string>> prefabsByName = new Dictionary<string, List<string>>();
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);

            // Only include Synty-style prefabs
            if (!name.StartsWith("SM_"))
                continue;

            if (!prefabsByName.ContainsKey(name))
            {
                prefabsByName[name] = new List<string>();
            }
            prefabsByName[name].Add(path);
        }

        Debug.Log($"Found {prefabsByName.Count} SM_* prefabs in project");

        // Process each item
        foreach (string guid in itemGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string itemName = Path.GetFileNameWithoutExtension(path);

            // Skip test items
            if (itemName.ToLower().Contains("test"))
                continue;

            var match = new ItemPrefabMatch { itemName = itemName };

            // First, try exact prefab name matches from our mapping
            if (s_ExactPrefabNames.TryGetValue(itemName, out var exactNames))
            {
                foreach (string exactName in exactNames)
                {
                    if (prefabsByName.TryGetValue(exactName, out var paths))
                    {
                        foreach (string prefabPath in paths)
                        {
                            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                            if (prefab != null && !match.matchingPrefabs.Contains(prefab))
                            {
                                match.matchingPrefabs.Add(prefab);
                            }
                        }
                    }
                }
            }

            // If no exact matches, try fuzzy matching
            if (match.matchingPrefabs.Count == 0)
            {
                string itemNameNormalized = itemName.Replace(" ", "").Replace("-", "").Replace(".", "").ToLower();

                foreach (var kvp in prefabsByName)
                {
                    string prefabName = kvp.Key;

                    // Extract the meaningful part after SM_Item_, SM_Prop_, SM_Wep_
                    string prefabCore = prefabName;
                    if (prefabName.StartsWith("SM_Item_"))
                        prefabCore = prefabName.Substring(8);
                    else if (prefabName.StartsWith("SM_Prop_"))
                        prefabCore = prefabName.Substring(8);
                    else if (prefabName.StartsWith("SM_Wep_"))
                        prefabCore = prefabName.Substring(7);
                    else if (prefabName.StartsWith("SM_Gen_"))
                        prefabCore = prefabName.Substring(7);

                    // Remove trailing numbers like _01, _02
                    prefabCore = System.Text.RegularExpressions.Regex.Replace(prefabCore, @"_\d+$", "");
                    string prefabCoreNormalized = prefabCore.Replace("_", "").ToLower();

                    // Check for match
                    bool matches = prefabCoreNormalized.Contains(itemNameNormalized) ||
                                   itemNameNormalized.Contains(prefabCoreNormalized);

                    // Also check individual words
                    if (!matches)
                    {
                        string[] itemWords = itemName.ToLower().Split(new[] { ' ', '_', '-' }, System.StringSplitOptions.RemoveEmptyEntries);
                        foreach (string word in itemWords)
                        {
                            if (word.Length > 2 && prefabCoreNormalized.Contains(word))
                            {
                                matches = true;
                                break;
                            }
                        }
                    }

                    if (matches)
                    {
                        foreach (string prefabPath in kvp.Value)
                        {
                            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                            if (prefab != null && !match.matchingPrefabs.Contains(prefab))
                            {
                                match.matchingPrefabs.Add(prefab);
                            }
                        }
                    }
                }
            }

            // Auto-select first match
            if (match.matchingPrefabs.Count > 0)
            {
                match.selectedPrefab = match.matchingPrefabs[0];
            }

            m_Matches.Add(match);
        }

        // Sort by whether they have matches (no matches first so user sees what needs attention)
        m_Matches = m_Matches.OrderBy(m => m.selectedPrefab != null ? 1 : 0)
                             .ThenBy(m => m.itemName)
                             .ToList();

        int matchedCount = m_Matches.Count(m => m.selectedPrefab != null);
        int unmatchedCount = m_Matches.Count - matchedCount;
        Debug.Log($"Found {m_Matches.Count} items: {matchedCount} with prefab matches, {unmatchedCount} without matches");
    }

    private void CopySelectedPrefabs()
    {
        if (!Directory.Exists(m_OutputFolder))
        {
            Directory.CreateDirectory(m_OutputFolder);
        }

        int count = 0;
        foreach (var match in m_Matches)
        {
            if (match.selectedPrefab != null)
            {
                string sourcePath = AssetDatabase.GetAssetPath(match.selectedPrefab);
                string destPath = Path.Combine(m_OutputFolder, match.itemName + ".prefab");

                // Create a prefab variant or copy
                if (!File.Exists(destPath))
                {
                    AssetDatabase.CopyAsset(sourcePath, destPath);
                    count++;
                }
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"Copied {count} prefabs to {m_OutputFolder}");
        EditorUtility.RevealInFinder(m_OutputFolder);
    }
}
