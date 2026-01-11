using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Editor tool to populate the CharacterDatabase with all Synty character prefabs.
/// Excludes zombies and attachment prefabs.
/// </summary>
public class CharacterDatabasePopulator : EditorWindow
{
    private CharacterDatabase m_Database;
    private List<string> m_FoundPrefabs = new List<string>();
    private Vector2 m_ScrollPosition;
    private bool m_IncludeZombies = false;

    // Folders to search for character prefabs
    private static readonly string[] SearchFolders = new string[]
    {
        "Assets/Synty/PolygonApocalypse/Prefabs/Characters",
        "Assets/Synty/PolygonMilitary/Prefabs/Characters",
        "Assets/Synty/PolygonPoliceStation/Prefabs/Characters",
        "Assets/Synty/PolygonOffice/Prefabs/Characters",
        "Assets/PolygonCasino/Prefabs/Characters",
        "Assets/PolygonNightclubs/Prefabs/Characters",
        "Assets/PolygonConstruction/Prefabs/Characters",
        "Assets/PolygonFarm/Prefabs/Characters"
    };

    [MenuItem("Project Klyra/Populate Character Database")]
    public static void ShowWindow()
    {
        var window = GetWindow<CharacterDatabasePopulator>("Character Database Populator");
        window.minSize = new Vector2(500, 400);
        window.LoadDatabase();
        window.ScanForPrefabs();
    }

    private void LoadDatabase()
    {
        m_Database = Resources.Load<CharacterDatabase>("CharacterDatabase");
        if (m_Database == null)
        {
            // Try to find it in the project
            var guids = AssetDatabase.FindAssets("t:CharacterDatabase");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                m_Database = AssetDatabase.LoadAssetAtPath<CharacterDatabase>(path);
            }
        }
    }

    private void ScanForPrefabs()
    {
        m_FoundPrefabs.Clear();

        foreach (string folder in SearchFolders)
        {
            if (!Directory.Exists(folder))
                continue;

            // Get all prefabs in this folder (not subfolders like Attachments)
            string[] files = Directory.GetFiles(folder, "SM_Chr_*.prefab", SearchOption.TopDirectoryOnly);

            foreach (string file in files)
            {
                string assetPath = file.Replace("\\", "/");
                string fileName = Path.GetFileNameWithoutExtension(assetPath);

                // Skip zombies unless explicitly included
                if (!m_IncludeZombies && fileName.ToLower().Contains("zombie"))
                    continue;

                // Skip attachments
                if (fileName.ToLower().Contains("attach"))
                    continue;

                m_FoundPrefabs.Add(assetPath);
            }
        }

        // Sort alphabetically
        m_FoundPrefabs.Sort();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Character Database Populator", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Database reference
        EditorGUI.BeginChangeCheck();
        m_Database = (CharacterDatabase)EditorGUILayout.ObjectField("Database", m_Database, typeof(CharacterDatabase), false);
        if (EditorGUI.EndChangeCheck() && m_Database == null)
        {
            LoadDatabase();
        }

        if (m_Database == null)
        {
            EditorGUILayout.HelpBox("No CharacterDatabase found. Create one in Assets/Resources/", MessageType.Warning);
            if (GUILayout.Button("Create CharacterDatabase"))
            {
                CreateDatabase();
            }
            return;
        }

        EditorGUILayout.Space(10);

        // Options
        EditorGUI.BeginChangeCheck();
        m_IncludeZombies = EditorGUILayout.Toggle("Include Zombies", m_IncludeZombies);
        if (EditorGUI.EndChangeCheck())
        {
            ScanForPrefabs();
        }

        EditorGUILayout.Space(5);

        // Stats
        EditorGUILayout.LabelField($"Found {m_FoundPrefabs.Count} character prefabs");
        EditorGUILayout.LabelField($"Current database has {m_Database.CharacterCount} characters");

        EditorGUILayout.Space(10);

        // Buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Scan", GUILayout.Height(30)))
        {
            ScanForPrefabs();
        }
        if (GUILayout.Button("Populate Database", GUILayout.Height(30)))
        {
            PopulateDatabase();
        }
        if (GUILayout.Button("Clear Database", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Clear Database", "Remove all characters from the database?", "Yes", "No"))
            {
                ClearDatabase();
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // Preview list
        EditorGUILayout.LabelField("Found Prefabs:", EditorStyles.boldLabel);
        m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
        foreach (string prefabPath in m_FoundPrefabs)
        {
            string fileName = Path.GetFileNameWithoutExtension(prefabPath);
            string displayName = GenerateDisplayName(fileName);
            EditorGUILayout.LabelField($"  {displayName}");
        }
        EditorGUILayout.EndScrollView();
    }

    private void CreateDatabase()
    {
        // Ensure Resources folder exists
        if (!Directory.Exists("Assets/Resources"))
        {
            Directory.CreateDirectory("Assets/Resources");
        }

        m_Database = ScriptableObject.CreateInstance<CharacterDatabase>();
        AssetDatabase.CreateAsset(m_Database, "Assets/Resources/CharacterDatabase.asset");
        AssetDatabase.SaveAssets();
        Debug.Log("[CharacterDatabasePopulator] Created CharacterDatabase at Assets/Resources/CharacterDatabase.asset");
    }

    private void PopulateDatabase()
    {
        if (m_Database == null) return;

        Undo.RecordObject(m_Database, "Populate Character Database");

        // Clear existing
        m_Database.characters.Clear();

        int added = 0;
        foreach (string prefabPath in m_FoundPrefabs)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[CharacterDatabasePopulator] Could not load prefab: {prefabPath}");
                continue;
            }

            string fileName = Path.GetFileNameWithoutExtension(prefabPath);
            string displayName = GenerateDisplayName(fileName);

            // Create CharacterData asset
            CharacterData charData = ScriptableObject.CreateInstance<CharacterData>();
            charData.displayName = displayName;
            charData.characterPrefab = prefab;
            charData.prefabPath = prefabPath;
            charData.description = $"A {displayName.ToLower()} character.";

            // Save the CharacterData asset
            string dataFolder = "Assets/Data/Characters";
            if (!Directory.Exists(dataFolder))
            {
                Directory.CreateDirectory(dataFolder);
            }

            string safeFileName = fileName.Replace(" ", "_");
            string dataPath = $"{dataFolder}/{safeFileName}.asset";

            // Check if asset already exists
            CharacterData existing = AssetDatabase.LoadAssetAtPath<CharacterData>(dataPath);
            if (existing != null)
            {
                // Update existing
                existing.displayName = displayName;
                existing.characterPrefab = prefab;
                existing.prefabPath = prefabPath;
                EditorUtility.SetDirty(existing);
                m_Database.characters.Add(existing);
            }
            else
            {
                // Create new
                AssetDatabase.CreateAsset(charData, dataPath);
                m_Database.characters.Add(charData);
            }

            added++;
        }

        EditorUtility.SetDirty(m_Database);
        AssetDatabase.SaveAssets();

        Debug.Log($"[CharacterDatabasePopulator] Added {added} characters to database");
    }

    private void ClearDatabase()
    {
        if (m_Database == null) return;

        Undo.RecordObject(m_Database, "Clear Character Database");
        m_Database.characters.Clear();
        EditorUtility.SetDirty(m_Database);
        AssetDatabase.SaveAssets();

        Debug.Log("[CharacterDatabasePopulator] Cleared character database");
    }

    /// <summary>
    /// Converts prefab filename to readable display name.
    /// e.g., "SM_Chr_Biker_Male_01" -> "Biker Male"
    /// </summary>
    private string GenerateDisplayName(string fileName)
    {
        // Remove prefix and suffix
        string name = fileName;

        // Remove "SM_Chr_" prefix
        if (name.StartsWith("SM_Chr_"))
            name = name.Substring(7);

        // Remove "_01", "_02" etc. suffix
        if (name.Length > 3 && name[name.Length - 3] == '_' && char.IsDigit(name[name.Length - 2]) && char.IsDigit(name[name.Length - 1]))
            name = name.Substring(0, name.Length - 3);

        // Replace underscores with spaces
        name = name.Replace("_", " ");

        // Capitalize each word
        var words = name.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
            }
        }

        return string.Join(" ", words);
    }
}
