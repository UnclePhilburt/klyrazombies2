using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor tool to set up the dog companion system in the scene.
/// </summary>
public class DogCompanionSetup : EditorWindow
{
    private GameObject[] m_DogPrefabs = new GameObject[3];
    private string[] m_BreedNames = { "German Shepherd", "Doberman", "Ridgeback" };
    private string[] m_PrefabPaths = {
        "Assets/PolygonDog/Prefabs/Dogs/Unity_SK_Animals_Dog_GermanShepherd_Collar_01.prefab",
        "Assets/PolygonDog/Prefabs/Dogs/Unity_SK_Animals_Dog_Doberman_Collar_01.prefab",
        "Assets/PolygonDog/Prefabs/Dogs/Unity_SK_Animals_Dog_Ridgeback_Collar_01.prefab"
    };

    [MenuItem("Project Klyra/Companion/Setup Dog Companion System")]
    public static void ShowWindow()
    {
        GetWindow<DogCompanionSetup>("Dog Companion Setup");
    }

    private void OnEnable()
    {
        // Try to load prefabs
        for (int i = 0; i < m_PrefabPaths.Length; i++)
        {
            m_DogPrefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabPaths[i]);
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Dog Companion Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Show prefab status
        EditorGUILayout.LabelField("Dog Prefabs:", EditorStyles.boldLabel);
        for (int i = 0; i < m_DogPrefabs.Length; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(m_BreedNames[i], GUILayout.Width(120));

            if (m_DogPrefabs[i] != null)
            {
                EditorGUILayout.LabelField("✓ Found", EditorStyles.boldLabel, GUILayout.Width(80));
            }
            else
            {
                EditorGUILayout.LabelField("✗ Missing", GUILayout.Width(80));
            }

            m_DogPrefabs[i] = (GameObject)EditorGUILayout.ObjectField(m_DogPrefabs[i], typeof(GameObject), false);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "This will create:\n" +
            "• DogSpawner - Spawns random dog breed with player\n" +
            "• DogRadialMenu - Q key to open command menu\n" +
            "\nThe dog will follow the player and can:\n" +
            "• Attack targets silently (melee alternative)\n" +
            "• Stay/Follow on command\n" +
            "• Search for nearby loot",
            MessageType.Info);

        EditorGUILayout.Space();

        // Check if components already exist
        bool hasSpawner = FindFirstObjectByType<DogSpawner>() != null;
        bool hasMenu = FindFirstObjectByType<DogRadialMenu>() != null;

        if (hasSpawner || hasMenu)
        {
            EditorGUILayout.HelpBox(
                $"Existing components found:\n" +
                $"• DogSpawner: {(hasSpawner ? "Yes" : "No")}\n" +
                $"• DogRadialMenu: {(hasMenu ? "Yes" : "No")}",
                MessageType.Warning);
        }

        EditorGUILayout.Space();

        // Validate prefabs
        int validPrefabs = 0;
        foreach (var prefab in m_DogPrefabs)
        {
            if (prefab != null) validPrefabs++;
        }

        GUI.enabled = validPrefabs > 0;

        if (GUILayout.Button("Setup Dog Companion System", GUILayout.Height(40)))
        {
            SetupDogSystem();
        }

        GUI.enabled = true;

        EditorGUILayout.Space();

        if (GUILayout.Button("Remove Dog Companion System"))
        {
            RemoveDogSystem();
        }
    }

    private void SetupDogSystem()
    {
        // Find or create manager object
        GameObject managerObj = GameObject.Find("DogCompanionManager");
        if (managerObj == null)
        {
            managerObj = new GameObject("DogCompanionManager");
            Undo.RegisterCreatedObjectUndo(managerObj, "Create Dog Companion Manager");
        }

        // Add DogSpawner
        DogSpawner spawner = managerObj.GetComponent<DogSpawner>();
        if (spawner == null)
        {
            spawner = managerObj.AddComponent<DogSpawner>();
        }

        // Set prefabs on spawner via SerializedObject
        SerializedObject so = new SerializedObject(spawner);
        SerializedProperty prefabsProp = so.FindProperty("m_DogPrefabs");

        // Count valid prefabs
        int validCount = 0;
        foreach (var p in m_DogPrefabs) if (p != null) validCount++;

        prefabsProp.arraySize = validCount;
        int index = 0;
        for (int i = 0; i < m_DogPrefabs.Length; i++)
        {
            if (m_DogPrefabs[i] != null)
            {
                prefabsProp.GetArrayElementAtIndex(index).objectReferenceValue = m_DogPrefabs[i];
                index++;
            }
        }
        so.ApplyModifiedProperties();

        // Add DogRadialMenu
        DogRadialMenu menu = managerObj.GetComponent<DogRadialMenu>();
        if (menu == null)
        {
            menu = managerObj.AddComponent<DogRadialMenu>();
        }

        // Select the object
        Selection.activeGameObject = managerObj;

        EditorUtility.DisplayDialog("Success",
            $"Dog Companion System setup complete!\n\n" +
            $"• DogSpawner added with {validCount} breed(s)\n" +
            $"• DogRadialMenu added (Q key)\n\n" +
            $"The dog will spawn automatically when the player spawns.",
            "OK");

        Debug.Log("[DogCompanionSetup] Setup complete!");
    }

    private void RemoveDogSystem()
    {
        if (!EditorUtility.DisplayDialog("Remove Dog System",
            "This will remove the DogCompanionManager object. Continue?",
            "Yes", "Cancel"))
        {
            return;
        }

        GameObject managerObj = GameObject.Find("DogCompanionManager");
        if (managerObj != null)
        {
            Undo.DestroyObjectImmediate(managerObj);
            Debug.Log("[DogCompanionSetup] Dog system removed");
        }

        // Also remove any spawned dogs
        var existingDog = GameObject.Find("DogCompanion");
        if (existingDog != null)
        {
            Undo.DestroyObjectImmediate(existingDog);
        }

        EditorUtility.DisplayDialog("Removed", "Dog Companion System has been removed.", "OK");
    }

    [MenuItem("Project Klyra/Companion/Spawn Test Dog (Play Mode Only)")]
    public static void SpawnTestDog()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Error", "This only works in Play Mode!", "OK");
            return;
        }

        var spawner = FindFirstObjectByType<DogSpawner>();
        if (spawner != null)
        {
            spawner.SpawnDog();
        }
        else
        {
            Debug.LogError("No DogSpawner found in scene!");
        }
    }
}
