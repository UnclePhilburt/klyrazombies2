using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Editor tool to add all character skins to a player prefab.
/// Run from Project Klyra > Build Character Prefab
/// </summary>
public class CharacterPrefabBuilder : EditorWindow
{
    private GameObject m_TargetPrefab;
    private string m_ContainerName = "CharacterModels";
    private bool m_IncludeZombies = false;
    private bool m_DisableByDefault = true;
    private Vector2 m_ScrollPosition;
    private List<string> m_FoundPrefabs = new List<string>();

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

    [MenuItem("Project Klyra/Build Character Prefab")]
    public static void ShowWindow()
    {
        var window = GetWindow<CharacterPrefabBuilder>("Character Prefab Builder");
        window.minSize = new Vector2(500, 400);
        window.ScanForPrefabs();
    }

    private void ScanForPrefabs()
    {
        m_FoundPrefabs.Clear();

        foreach (string folder in SearchFolders)
        {
            if (!Directory.Exists(folder))
                continue;

            string[] files = Directory.GetFiles(folder, "SM_Chr_*.prefab", SearchOption.TopDirectoryOnly);

            foreach (string file in files)
            {
                string assetPath = file.Replace("\\", "/");
                string fileName = Path.GetFileNameWithoutExtension(assetPath);

                // Skip zombies unless included
                if (!m_IncludeZombies && fileName.ToLower().Contains("zombie"))
                    continue;

                // Skip attachments
                if (fileName.ToLower().Contains("attach"))
                    continue;

                m_FoundPrefabs.Add(assetPath);
            }
        }

        m_FoundPrefabs.Sort();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Character Prefab Builder", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Adds all character skins as children of your player prefab.", MessageType.Info);
        EditorGUILayout.Space(10);

        // Target prefab
        m_TargetPrefab = (GameObject)EditorGUILayout.ObjectField("Target Player Prefab", m_TargetPrefab, typeof(GameObject), false);

        // Container name
        m_ContainerName = EditorGUILayout.TextField("Container Name", m_ContainerName);

        // Options
        EditorGUI.BeginChangeCheck();
        m_IncludeZombies = EditorGUILayout.Toggle("Include Zombies", m_IncludeZombies);
        if (EditorGUI.EndChangeCheck())
        {
            ScanForPrefabs();
        }

        m_DisableByDefault = EditorGUILayout.Toggle("Disable Models By Default", m_DisableByDefault);

        EditorGUILayout.Space(10);

        // Stats
        EditorGUILayout.LabelField($"Found {m_FoundPrefabs.Count} character prefabs");

        EditorGUILayout.Space(10);

        // Buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Scan", GUILayout.Height(30)))
        {
            ScanForPrefabs();
        }

        GUI.enabled = m_TargetPrefab != null;
        if (GUILayout.Button("Add Characters to Prefab", GUILayout.Height(30)))
        {
            AddCharactersToPrefab();
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // Preview list
        EditorGUILayout.LabelField("Characters to Add:", EditorStyles.boldLabel);
        m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
        foreach (string prefabPath in m_FoundPrefabs)
        {
            string fileName = Path.GetFileNameWithoutExtension(prefabPath);
            EditorGUILayout.LabelField($"  {fileName}");
        }
        EditorGUILayout.EndScrollView();
    }

    private void AddCharactersToPrefab()
    {
        if (m_TargetPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign a target prefab.", "OK");
            return;
        }

        // Get prefab path
        string prefabPath = AssetDatabase.GetAssetPath(m_TargetPrefab);
        if (string.IsNullOrEmpty(prefabPath))
        {
            EditorUtility.DisplayDialog("Error", "Target must be a prefab asset.", "OK");
            return;
        }

        // Open prefab for editing
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

        try
        {
            // Find or create container
            Transform container = prefabRoot.transform.Find(m_ContainerName);
            if (container == null)
            {
                GameObject containerObj = new GameObject(m_ContainerName);
                containerObj.transform.SetParent(prefabRoot.transform);
                containerObj.transform.localPosition = Vector3.zero;
                containerObj.transform.localRotation = Quaternion.identity;
                containerObj.transform.localScale = Vector3.one;
                container = containerObj.transform;
            }

            // Get existing character names to avoid duplicates
            HashSet<string> existingNames = new HashSet<string>();
            foreach (Transform child in container)
            {
                existingNames.Add(child.name);
            }

            int added = 0;
            int skipped = 0;

            foreach (string charPrefabPath in m_FoundPrefabs)
            {
                string charName = Path.GetFileNameWithoutExtension(charPrefabPath);

                // Skip if already exists
                if (existingNames.Contains(charName))
                {
                    skipped++;
                    continue;
                }

                // Load character prefab
                GameObject charPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(charPrefabPath);
                if (charPrefab == null)
                {
                    Debug.LogWarning($"[CharacterPrefabBuilder] Could not load: {charPrefabPath}");
                    continue;
                }

                // Instantiate as child
                GameObject charInstance = (GameObject)PrefabUtility.InstantiatePrefab(charPrefab, container);
                charInstance.name = charName;
                charInstance.transform.localPosition = Vector3.zero;
                charInstance.transform.localRotation = Quaternion.identity;
                charInstance.transform.localScale = Vector3.one;

                // Disable by default
                if (m_DisableByDefault)
                {
                    charInstance.SetActive(false);
                }

                added++;
            }

            // Save prefab
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);

            Debug.Log($"[CharacterPrefabBuilder] Added {added} characters, skipped {skipped} duplicates");
            EditorUtility.DisplayDialog("Success",
                $"Added {added} character models to prefab.\nSkipped {skipped} (already existed).\n\nCharacters are in '{m_ContainerName}' container.",
                "OK");
        }
        finally
        {
            // Cleanup
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }
}
