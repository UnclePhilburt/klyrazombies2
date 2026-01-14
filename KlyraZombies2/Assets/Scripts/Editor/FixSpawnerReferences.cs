using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Fixes zombie spawner prefab references that were lost during chunk splitting.
/// Menu: Project Klyra > Debug > Fix Spawner References
/// </summary>
public class FixSpawnerReferences : EditorWindow
{
    private GameObject[] m_ZombiePrefabs;
    private string m_ChunksFolder = "Assets/Scenes/Chunks";

    [MenuItem("Project Klyra/Debug/Fix Spawner References")]
    public static void ShowWindow()
    {
        GetWindow<FixSpawnerReferences>("Fix Spawner References");
    }

    private void OnGUI()
    {
        GUILayout.Label("Fix Zombie Spawner References", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This tool fixes spawners that lost their zombie prefab references during chunk splitting.", MessageType.Info);

        EditorGUILayout.Space();

        // Zombie prefabs selection
        EditorGUILayout.LabelField("Zombie Prefabs to Assign:", EditorStyles.boldLabel);

        if (m_ZombiePrefabs == null || m_ZombiePrefabs.Length == 0)
        {
            if (GUILayout.Button("Auto-Find Zombie Prefabs", GUILayout.Height(30)))
            {
                FindZombiePrefabs();
            }
        }

        EditorGUILayout.BeginVertical("box");
        SerializedObject so = new SerializedObject(this);
        SerializedProperty prefabsProp = so.FindProperty("m_ZombiePrefabs");
        EditorGUILayout.PropertyField(prefabsProp, true);
        so.ApplyModifiedProperties();
        EditorGUILayout.EndVertical();

        if (m_ZombiePrefabs != null && m_ZombiePrefabs.Length > 0)
        {
            EditorGUILayout.HelpBox($"Found {m_ZombiePrefabs.Length} zombie prefabs", MessageType.Info);
        }

        EditorGUILayout.Space();

        // Chunks folder
        m_ChunksFolder = EditorGUILayout.TextField("Chunks Folder", m_ChunksFolder);

        EditorGUILayout.Space();

        GUI.enabled = m_ZombiePrefabs != null && m_ZombiePrefabs.Length > 0;
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("FIX ALL SPAWNERS IN CHUNKS", GUILayout.Height(40)))
        {
            FixAllSpawners();
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        EditorGUILayout.Space();

        if (GUILayout.Button("Check Current Scene Spawners", GUILayout.Height(30)))
        {
            CheckCurrentScene();
        }
    }

    private void FindZombiePrefabs()
    {
        // Find all zombie prefabs in project
        string[] guids = AssetDatabase.FindAssets("t:Prefab zombie", new[] { "Assets" });
        List<GameObject> zombiePrefabs = new List<GameObject>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                // Check if it has ZombieAI component
                if (prefab.GetComponent<ZombieAI>() != null || prefab.GetComponentInChildren<ZombieAI>() != null)
                {
                    zombiePrefabs.Add(prefab);
                    Debug.Log($"Found zombie prefab: {prefab.name} at {path}");
                }
            }
        }

        m_ZombiePrefabs = zombiePrefabs.ToArray();
        Debug.Log($"Found {m_ZombiePrefabs.Length} zombie prefabs total");

        if (m_ZombiePrefabs.Length == 0)
        {
            EditorUtility.DisplayDialog("No Zombie Prefabs Found",
                "Could not find any prefabs with ZombieAI component. Please assign them manually.", "OK");
        }
    }

    private void FixAllSpawners()
    {
        if (!Directory.Exists(m_ChunksFolder))
        {
            EditorUtility.DisplayDialog("Error", $"Chunks folder not found: {m_ChunksFolder}", "OK");
            return;
        }

        // Get all chunk scenes
        string[] chunkFiles = Directory.GetFiles(m_ChunksFolder, "*.unity");
        int totalFixed = 0;
        int totalSpawners = 0;

        Debug.Log("=== FIXING SPAWNER REFERENCES ===");

        foreach (string chunkFile in chunkFiles)
        {
            Scene scene = EditorSceneManager.OpenScene(chunkFile, OpenSceneMode.Single);

            ZombieSpawner[] spawners = FindObjectsOfType<ZombieSpawner>();
            totalSpawners += spawners.Length;

            foreach (var spawner in spawners)
            {
                // Use reflection to set the private m_ZombiePrefabs field
                var prefabsField = typeof(ZombieSpawner).GetField("m_ZombiePrefabs",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (prefabsField != null)
                {
                    GameObject[] currentPrefabs = prefabsField.GetValue(spawner) as GameObject[];

                    if (currentPrefabs == null || currentPrefabs.Length == 0 || currentPrefabs[0] == null)
                    {
                        // Fix it!
                        prefabsField.SetValue(spawner, m_ZombiePrefabs);
                        EditorUtility.SetDirty(spawner);
                        totalFixed++;
                        Debug.Log($"Fixed spawner: {spawner.name} in {scene.name}");
                    }
                }
            }

            if (spawners.Length > 0)
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"Saved {scene.name} with {spawners.Length} spawners");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"=== COMPLETE: Fixed {totalFixed} out of {totalSpawners} spawners ===");

        EditorUtility.DisplayDialog("Fix Complete",
            $"Fixed {totalFixed} spawners out of {totalSpawners} total.\n\nAll chunk scenes have been saved.",
            "OK");
    }

    private void CheckCurrentScene()
    {
        ZombieSpawner[] spawners = FindObjectsOfType<ZombieSpawner>();

        Debug.Log($"=== CHECKING {spawners.Length} SPAWNERS IN CURRENT SCENE ===");

        int broken = 0;
        int working = 0;

        foreach (var spawner in spawners)
        {
            var prefabsField = typeof(ZombieSpawner).GetField("m_ZombiePrefabs",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (prefabsField != null)
            {
                GameObject[] prefabs = prefabsField.GetValue(spawner) as GameObject[];

                if (prefabs == null || prefabs.Length == 0 || prefabs[0] == null)
                {
                    Debug.LogError($"✗ BROKEN: {spawner.name} - No zombie prefabs assigned!", spawner);
                    broken++;
                }
                else
                {
                    Debug.Log($"✓ OK: {spawner.name} - {prefabs.Length} prefabs assigned", spawner);
                    working++;
                }
            }
        }

        Debug.Log($"=== RESULTS: {working} working, {broken} broken ===");

        if (broken > 0)
        {
            EditorUtility.DisplayDialog("Spawners Need Fixing",
                $"{broken} spawners have no zombie prefabs assigned.\n\nClick 'Fix All Spawners' to fix them.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("All Good!",
                $"All {working} spawners have zombie prefabs assigned correctly.",
                "OK");
        }
    }
}
