using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Editor tool to generate CharacterData assets from Synty character prefabs
/// </summary>
public class CharacterDatabaseGenerator : EditorWindow
{
    private Vector2 m_ScrollPos;
    private List<CharacterEntry> m_FoundCharacters = new List<CharacterEntry>();
    private bool m_ScannedOnce = false;

    // Folders to scan for characters
    private static readonly string[] CharacterFolders = new string[]
    {
        "Assets/Synty/PolygonApocalypse/Prefabs/Characters",
        "Assets/Synty/PolygonMilitary/Prefabs/Characters",
        "Assets/Synty/PolygonPoliceStation/Prefabs/Characters",
        "Assets/Synty/PolygonOffice/Prefabs/Characters",
        "Assets/Synty/PolygonCity/Prefabs/Characters",
        "Assets/PolygonNightclubs/Prefabs/Characters",
        "Assets/PolygonFarm/Prefabs/Characters"
    };

    // Prefabs to exclude (zombies, attachments, etc.)
    private static readonly string[] ExcludePatterns = new string[]
    {
        "Zombie",
        "Attach_",
        "Skeleton",
        "Dead"
    };

    private class CharacterEntry
    {
        public string prefabPath;
        public string displayName;
        public bool selected;
        public GameObject prefab;
    }

    [MenuItem("Project Klyra/Main Menu/Generate Character Database")]
    public static void ShowWindow()
    {
        var window = GetWindow<CharacterDatabaseGenerator>("Character Database Generator");
        window.minSize = new Vector2(500, 400);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Character Database Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("Scan for Characters", GUILayout.Height(30)))
        {
            ScanForCharacters();
        }

        if (m_FoundCharacters.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Found {m_FoundCharacters.Count} characters:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All"))
            {
                foreach (var c in m_FoundCharacters) c.selected = true;
            }
            if (GUILayout.Button("Select None"))
            {
                foreach (var c in m_FoundCharacters) c.selected = false;
            }
            if (GUILayout.Button("Select Apocalypse Only"))
            {
                foreach (var c in m_FoundCharacters)
                    c.selected = c.prefabPath.Contains("Apocalypse");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos, GUILayout.Height(250));
            foreach (var character in m_FoundCharacters)
            {
                EditorGUILayout.BeginHorizontal();
                character.selected = EditorGUILayout.Toggle(character.selected, GUILayout.Width(20));
                EditorGUILayout.LabelField(character.displayName);
                EditorGUILayout.LabelField(GetPackName(character.prefabPath), EditorStyles.miniLabel, GUILayout.Width(120));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            int selectedCount = m_FoundCharacters.Count(c => c.selected);
            EditorGUILayout.LabelField($"Selected: {selectedCount} characters");

            EditorGUILayout.Space();

            GUI.enabled = selectedCount > 0;
            if (GUILayout.Button("Generate Database", GUILayout.Height(40)))
            {
                GenerateDatabase();
            }
            GUI.enabled = true;
        }
        else if (m_ScannedOnce)
        {
            EditorGUILayout.HelpBox("No characters found. Make sure Synty character packs are imported.", MessageType.Warning);
        }
    }

    private void ScanForCharacters()
    {
        m_FoundCharacters.Clear();
        m_ScannedOnce = true;

        foreach (string folder in CharacterFolders)
        {
            if (!Directory.Exists(folder)) continue;

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);

                // Skip attachments and excluded patterns
                if (ExcludePatterns.Any(p => fileName.Contains(p))) continue;

                // Only include SM_Chr_ prefabs (characters)
                if (!fileName.StartsWith("SM_Chr_")) continue;

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                m_FoundCharacters.Add(new CharacterEntry
                {
                    prefabPath = path,
                    displayName = FormatDisplayName(fileName),
                    selected = path.Contains("Apocalypse"), // Default select apocalypse characters
                    prefab = prefab
                });
            }
        }

        // Sort by name
        m_FoundCharacters = m_FoundCharacters.OrderBy(c => c.displayName).ToList();

        Debug.Log($"[CharacterDatabaseGenerator] Found {m_FoundCharacters.Count} characters");
    }

    private string FormatDisplayName(string fileName)
    {
        // Remove SM_Chr_ prefix
        string name = fileName.Replace("SM_Chr_", "");

        // Remove _01, _02 suffixes
        if (name.Length > 3 && name[name.Length - 3] == '_')
        {
            name = name.Substring(0, name.Length - 3);
        }

        // Replace underscores with spaces
        name = name.Replace("_", " ");

        return name;
    }

    private string GetPackName(string path)
    {
        if (path.Contains("Apocalypse")) return "Apocalypse";
        if (path.Contains("Military")) return "Military";
        if (path.Contains("PoliceStation")) return "Police";
        if (path.Contains("Office")) return "Office";
        if (path.Contains("City")) return "City";
        if (path.Contains("Nightclubs")) return "Nightclub";
        if (path.Contains("Farm")) return "Farm";
        return "Other";
    }

    private void GenerateDatabase()
    {
        // Create output directories
        string charDataFolder = "Assets/Data/Characters";
        string resourcesFolder = "Assets/Resources";

        if (!Directory.Exists(charDataFolder))
        {
            Directory.CreateDirectory(charDataFolder);
        }
        if (!Directory.Exists(resourcesFolder))
        {
            Directory.CreateDirectory(resourcesFolder);
        }

        // Create CharacterData assets
        List<CharacterData> createdCharacters = new List<CharacterData>();

        foreach (var entry in m_FoundCharacters.Where(c => c.selected))
        {
            string assetPath = $"{charDataFolder}/{entry.displayName.Replace(" ", "")}.asset";

            // Check if already exists
            CharacterData existing = AssetDatabase.LoadAssetAtPath<CharacterData>(assetPath);
            if (existing != null)
            {
                createdCharacters.Add(existing);
                continue;
            }

            CharacterData charData = ScriptableObject.CreateInstance<CharacterData>();
            charData.displayName = entry.displayName;
            charData.characterPrefab = entry.prefab;
            charData.prefabPath = entry.prefabPath;
            charData.description = $"A {entry.displayName.ToLower()} survivor.";

            AssetDatabase.CreateAsset(charData, assetPath);
            createdCharacters.Add(charData);

            Debug.Log($"[CharacterDatabaseGenerator] Created: {entry.displayName}");
        }

        // Create or update CharacterDatabase
        string dbPath = $"{resourcesFolder}/CharacterDatabase.asset";
        CharacterDatabase database = AssetDatabase.LoadAssetAtPath<CharacterDatabase>(dbPath);

        if (database == null)
        {
            database = ScriptableObject.CreateInstance<CharacterDatabase>();
            AssetDatabase.CreateAsset(database, dbPath);
        }

        database.characters = createdCharacters;
        EditorUtility.SetDirty(database);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Character Database Generator",
            $"Created {createdCharacters.Count} character entries!\n\n" +
            $"Database saved to:\n{dbPath}\n\n" +
            $"Character data saved to:\n{charDataFolder}",
            "OK");

        // Select the database
        Selection.activeObject = database;
    }
}
