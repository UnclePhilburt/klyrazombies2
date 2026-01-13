using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Batch bakes NavMesh for all chunk scenes.
/// Menu: Project Klyra > World > Bake All Chunk NavMeshes
/// </summary>
public class ChunkNavMeshBaker : EditorWindow
{
    private string m_ChunksFolder = "Assets/Scenes/Chunks";
    private bool m_SkipEmptyChunks = true;
    private int m_MinObjectsToProcess = 10;
    private List<string> m_ChunkScenes = new List<string>();
    private int m_CurrentIndex = 0;
    private bool m_IsBaking = false;
    private string m_Status = "";

    [MenuItem("Project Klyra/World/Bake All Chunk NavMeshes")]
    public static void ShowWindow()
    {
        GetWindow<ChunkNavMeshBaker>("NavMesh Baker");
    }

    private void OnGUI()
    {
        GUILayout.Label("Chunk NavMesh Baker", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        m_ChunksFolder = EditorGUILayout.TextField("Chunks Folder", m_ChunksFolder);
        m_SkipEmptyChunks = EditorGUILayout.Toggle("Skip Empty Chunks", m_SkipEmptyChunks);

        if (m_SkipEmptyChunks)
        {
            m_MinObjectsToProcess = EditorGUILayout.IntField("Min Objects to Bake", m_MinObjectsToProcess);
        }

        EditorGUILayout.Space();

        if (!m_IsBaking)
        {
            if (GUILayout.Button("Find Chunk Scenes"))
            {
                FindChunkScenes();
            }

            if (m_ChunkScenes.Count > 0)
            {
                EditorGUILayout.LabelField($"Found {m_ChunkScenes.Count} chunk scenes");

                EditorGUILayout.Space();

                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("BAKE ALL NAVMESHES", GUILayout.Height(35)))
                {
                    StartBaking();
                }
                GUI.backgroundColor = Color.white;
            }
        }
        else
        {
            EditorGUILayout.LabelField($"Progress: {m_CurrentIndex} / {m_ChunkScenes.Count}");
            EditorGUILayout.LabelField(m_Status);

            if (GUILayout.Button("Cancel"))
            {
                m_IsBaking = false;
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "This will:\n" +
            "1. Open each chunk scene\n" +
            "2. Bake NavMesh for that chunk\n" +
            "3. Save and close\n" +
            "4. Move to next chunk\n\n" +
            "May take several minutes for large worlds.",
            MessageType.Info);
    }

    private void FindChunkScenes()
    {
        m_ChunkScenes.Clear();

        if (!Directory.Exists(m_ChunksFolder))
        {
            Debug.LogError($"[NavMeshBaker] Folder not found: {m_ChunksFolder}");
            return;
        }

        string[] files = Directory.GetFiles(m_ChunksFolder, "Chunk_*.unity");

        foreach (var file in files)
        {
            string path = file.Replace("\\", "/");
            m_ChunkScenes.Add(path);
        }

        m_ChunkScenes.Sort();
        Debug.Log($"[NavMeshBaker] Found {m_ChunkScenes.Count} chunk scenes");
    }

    private void StartBaking()
    {
        if (m_ChunkScenes.Count == 0)
        {
            FindChunkScenes();
            if (m_ChunkScenes.Count == 0) return;
        }

        // Save current scene
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        m_IsBaking = true;
        m_CurrentIndex = 0;
        EditorApplication.update += BakeNextChunk;
    }

    private void BakeNextChunk()
    {
        if (!m_IsBaking || m_CurrentIndex >= m_ChunkScenes.Count)
        {
            // Done
            EditorApplication.update -= BakeNextChunk;
            m_IsBaking = false;
            m_Status = "Complete!";
            Debug.Log("[NavMeshBaker] All chunks processed!");
            EditorUtility.DisplayDialog("NavMesh Baker", "All chunk NavMeshes have been baked!", "OK");
            return;
        }

        string scenePath = m_ChunkScenes[m_CurrentIndex];
        string sceneName = Path.GetFileNameWithoutExtension(scenePath);
        m_Status = $"Baking: {sceneName}...";

        try
        {
            // Open the chunk scene
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Count objects to see if we should skip
            int objectCount = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                objectCount += CountObjects(root.transform);
            }

            if (m_SkipEmptyChunks && objectCount < m_MinObjectsToProcess)
            {
                Debug.Log($"[NavMeshBaker] Skipping {sceneName} (only {objectCount} objects)");
                m_CurrentIndex++;
                return;
            }

            // Bake NavMesh using NavMeshSurface
            Debug.Log($"[NavMeshBaker] Baking {sceneName} ({objectCount} objects)...");

            // Find or create NavMeshSurface
            NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();

            if (surface == null)
            {
                // Create a NavMeshSurface on a new GameObject
                GameObject navObj = new GameObject("NavMeshSurface");
                surface = navObj.AddComponent<NavMeshSurface>();
                surface.collectObjects = CollectObjects.All;
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            }

            // Bake the NavMesh
            surface.BuildNavMesh();

            // Save the scene with baked NavMesh
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[NavMeshBaker] Completed {sceneName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NavMeshBaker] Error baking {sceneName}: {e.Message}");
        }

        m_CurrentIndex++;
        Repaint();
    }

    private int CountObjects(Transform t)
    {
        int count = 1;
        foreach (Transform child in t)
        {
            count += CountObjects(child);
        }
        return count;
    }
}
