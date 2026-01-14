using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Validates that all objects are in the correct chunks based on their rendered bounds.
/// Can detect and fix misplaced objects.
/// Menu: Project Klyra > World > Chunk Validator
/// </summary>
public class ChunkValidator : EditorWindow
{
    private ChunkConfig m_Config;
    private string m_ChunksFolder = "Assets/Scenes/Chunks";

    private bool m_HasValidated = false;
    private int m_TotalObjects = 0;
    private int m_CorrectObjects = 0;
    private int m_MisplacedObjects = 0;
    private List<MisplacedObject> m_MisplacedList = new List<MisplacedObject>();
    private Vector2 m_ScrollPosition = Vector2.zero;

    private class MisplacedObject
    {
        public GameObject obj;
        public string objectName;
        public string sceneName;
        public Vector2Int currentChunk;
        public Vector2Int correctChunk;
        public Vector3 renderedCenter;
        public float distanceFromCorrectChunk;
    }

    [MenuItem("Project Klyra/World/Chunk Validator")]
    public static void ShowWindow()
    {
        GetWindow<ChunkValidator>("Chunk Validator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Chunk Validator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Validates that objects are in the correct chunks based on their rendered bounds.", MessageType.Info);
        EditorGUILayout.Space();

        // Config
        m_Config = (ChunkConfig)EditorGUILayout.ObjectField("Chunk Config", m_Config, typeof(ChunkConfig), false);

        if (m_Config == null)
        {
            EditorGUILayout.HelpBox("Please assign a ChunkConfig asset.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space();

        // Chunks folder
        EditorGUILayout.BeginHorizontal();
        m_ChunksFolder = EditorGUILayout.TextField("Chunks Folder", m_ChunksFolder);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Chunks Folder", "Assets/Scenes", "");
            if (!string.IsNullOrEmpty(path))
            {
                m_ChunksFolder = "Assets" + path.Substring(Application.dataPath.Length);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Validate button
        GUI.enabled = m_Config != null && !string.IsNullOrEmpty(m_ChunksFolder);
        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("VALIDATE ALL CHUNKS", GUILayout.Height(35)))
        {
            ValidateAllChunks();
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        EditorGUILayout.Space();

        // Results
        if (m_HasValidated)
        {
            EditorGUILayout.LabelField("Validation Results", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Total Objects: {m_TotalObjects}");

            GUI.color = Color.green;
            EditorGUILayout.LabelField($"Correctly Placed: {m_CorrectObjects} ({(m_TotalObjects > 0 ? (float)m_CorrectObjects / m_TotalObjects * 100 : 0):F1}%)");
            GUI.color = Color.white;

            if (m_MisplacedObjects > 0)
            {
                GUI.color = Color.red;
                EditorGUILayout.LabelField($"Misplaced Objects: {m_MisplacedObjects} ({(m_TotalObjects > 0 ? (float)m_MisplacedObjects / m_TotalObjects * 100 : 0):F1}%)");
                GUI.color = Color.white;

                EditorGUILayout.Space();

                // Fix button
                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("FIX ALL MISPLACED OBJECTS", GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog("Fix Misplaced Objects",
                        $"This will move {m_MisplacedObjects} objects to their correct chunks.\n\nMake sure you have a backup! Continue?",
                        "Fix", "Cancel"))
                    {
                        FixMisplacedObjects();
                    }
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.Space();

                // List of misplaced objects
                EditorGUILayout.LabelField("Misplaced Objects:", EditorStyles.boldLabel);

                m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition, GUILayout.ExpandHeight(true));

                foreach (var misplaced in m_MisplacedList)
                {
                    EditorGUILayout.BeginVertical("box");

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Object: {misplaced.objectName}", EditorStyles.boldLabel);
                    if (misplaced.obj != null && GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        Selection.activeGameObject = misplaced.obj;
                        SceneView.FrameLastActiveSceneView();
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.LabelField($"Scene: {misplaced.sceneName}");

                    GUI.color = Color.red;
                    EditorGUILayout.LabelField($"Current Chunk: ({misplaced.currentChunk.x}, {misplaced.currentChunk.y})");
                    GUI.color = Color.green;
                    EditorGUILayout.LabelField($"Should Be In: ({misplaced.correctChunk.x}, {misplaced.correctChunk.y})");
                    GUI.color = Color.white;

                    EditorGUILayout.LabelField($"Rendered Center: {misplaced.renderedCenter}");
                    EditorGUILayout.LabelField($"Distance: {misplaced.distanceFromCorrectChunk:F1}m from correct chunk");

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
                }

                EditorGUILayout.EndScrollView();
            }
            else
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField("✓ All objects are correctly placed!", EditorStyles.boldLabel);
                GUI.color = Color.white;
            }
        }
    }

    private void ValidateAllChunks()
    {
        if (!Directory.Exists(m_ChunksFolder))
        {
            EditorUtility.DisplayDialog("Error", $"Chunks folder not found: {m_ChunksFolder}", "OK");
            return;
        }

        // Reset results
        m_HasValidated = false;
        m_TotalObjects = 0;
        m_CorrectObjects = 0;
        m_MisplacedObjects = 0;
        m_MisplacedList.Clear();

        // Get all chunk scenes
        string[] chunkFiles = Directory.GetFiles(m_ChunksFolder, "Chunk_*.unity");

        if (chunkFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", $"No chunk scenes found in: {m_ChunksFolder}", "OK");
            return;
        }

        Debug.Log($"[ChunkValidator] Validating {chunkFiles.Length} chunk scenes...");

        // Save current scene setup
        var currentScenes = new List<string>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            currentScenes.Add(SceneManager.GetSceneAt(i).path);
        }

        // Open and validate each chunk
        foreach (var chunkFile in chunkFiles)
        {
            // Parse chunk coordinates from filename (e.g., "Chunk_5_3.unity")
            string fileName = Path.GetFileNameWithoutExtension(chunkFile);
            string[] parts = fileName.Split('_');
            if (parts.Length != 3)
            {
                Debug.LogWarning($"[ChunkValidator] Skipping invalid chunk filename: {fileName}");
                continue;
            }

            if (!int.TryParse(parts[1], out int chunkX) || !int.TryParse(parts[2], out int chunkZ))
            {
                Debug.LogWarning($"[ChunkValidator] Could not parse chunk coordinates: {fileName}");
                continue;
            }

            Vector2Int currentChunk = new Vector2Int(chunkX, chunkZ);

            // Open scene
            Scene scene = EditorSceneManager.OpenScene(chunkFile, OpenSceneMode.Single);
            GameObject[] rootObjects = scene.GetRootGameObjects();

            foreach (var obj in rootObjects)
            {
                ValidateObject(obj, currentChunk, fileName);
            }
        }

        // Restore original scene setup
        if (currentScenes.Count > 0)
        {
            EditorSceneManager.OpenScene(currentScenes[0], OpenSceneMode.Single);
            for (int i = 1; i < currentScenes.Count; i++)
            {
                EditorSceneManager.OpenScene(currentScenes[i], OpenSceneMode.Additive);
            }
        }

        m_HasValidated = true;

        // Sort misplaced by distance
        m_MisplacedList = m_MisplacedList.OrderByDescending(m => m.distanceFromCorrectChunk).ToList();

        string resultMsg = $"Validation Complete!\n\n" +
                          $"Total Objects: {m_TotalObjects}\n" +
                          $"Correctly Placed: {m_CorrectObjects}\n" +
                          $"Misplaced: {m_MisplacedObjects}";

        if (m_MisplacedObjects > 0)
        {
            resultMsg += $"\n\nAccuracy: {(float)m_CorrectObjects / m_TotalObjects * 100:F1}%";
            Debug.LogWarning($"[ChunkValidator] {resultMsg}");
            EditorUtility.DisplayDialog("Validation Complete", resultMsg, "OK");
        }
        else
        {
            Debug.Log($"[ChunkValidator] {resultMsg}");
            EditorUtility.DisplayDialog("Validation Complete", resultMsg + "\n\n✓ All objects correctly placed!", "OK");
        }
    }

    private void ValidateObject(GameObject obj, Vector2Int currentChunk, string sceneName)
    {
        m_TotalObjects++;

        // Get where object should be based on rendered bounds
        Vector2Int correctChunk = GetChunkFromRenderedBounds(obj);

        // Clamp to valid range
        correctChunk.x = Mathf.Clamp(correctChunk.x, 0, m_Config.gridSizeX - 1);
        correctChunk.y = Mathf.Clamp(correctChunk.y, 0, m_Config.gridSizeZ - 1);

        if (correctChunk == currentChunk)
        {
            m_CorrectObjects++;
        }
        else
        {
            m_MisplacedObjects++;

            // Calculate distance from correct chunk
            Vector3 renderedCenter = GetRenderedCenter(obj);
            Vector3 correctChunkCenter = m_Config.GetChunkBounds(correctChunk.x, correctChunk.y).center;
            float distance = Vector3.Distance(new Vector3(renderedCenter.x, 0, renderedCenter.z),
                                             new Vector3(correctChunkCenter.x, 0, correctChunkCenter.z));

            m_MisplacedList.Add(new MisplacedObject
            {
                obj = obj,
                objectName = obj.name,
                sceneName = sceneName,
                currentChunk = currentChunk,
                correctChunk = correctChunk,
                renderedCenter = renderedCenter,
                distanceFromCorrectChunk = distance
            });

            Debug.LogWarning($"[ChunkValidator] Misplaced: '{obj.name}' in {sceneName} (chunk {currentChunk}) should be in chunk {correctChunk}. Center: {renderedCenter}");
        }
    }

    private Vector2Int GetChunkFromRenderedBounds(GameObject obj)
    {
        Vector3 center = GetRenderedCenter(obj);
        return m_Config.WorldToChunk(center);
    }

    private Vector3 GetRenderedCenter(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length > 0)
        {
            Bounds combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }
            return combinedBounds.center;
        }

        // No renderers - use transform position
        return obj.transform.position;
    }

    private void FixMisplacedObjects()
    {
        if (m_MisplacedList.Count == 0)
            return;

        Debug.Log($"[ChunkValidator] Fixing {m_MisplacedList.Count} misplaced objects...");

        // Group misplaced objects by their correct chunk
        Dictionary<Vector2Int, List<MisplacedObject>> objectsByCorrectChunk = new Dictionary<Vector2Int, List<MisplacedObject>>();

        foreach (var misplaced in m_MisplacedList)
        {
            if (!objectsByCorrectChunk.ContainsKey(misplaced.correctChunk))
                objectsByCorrectChunk[misplaced.correctChunk] = new List<MisplacedObject>();

            objectsByCorrectChunk[misplaced.correctChunk].Add(misplaced);
        }

        int movedCount = 0;

        // Process each chunk
        foreach (var kvp in objectsByCorrectChunk)
        {
            Vector2Int targetChunk = kvp.Key;
            List<MisplacedObject> objectsToMove = kvp.Value;

            // Open target chunk scene
            string targetScenePath = Path.Combine(m_ChunksFolder, m_Config.GetChunkSceneName(targetChunk.x, targetChunk.y) + ".unity");

            if (!File.Exists(targetScenePath))
            {
                Debug.LogError($"[ChunkValidator] Target chunk scene not found: {targetScenePath}");
                continue;
            }

            Scene targetScene = EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Single);

            // Open source scenes and move objects
            foreach (var misplaced in objectsToMove)
            {
                string sourceScenePath = Path.Combine(m_ChunksFolder, misplaced.sceneName + ".unity");

                if (!File.Exists(sourceScenePath))
                {
                    Debug.LogError($"[ChunkValidator] Source scene not found: {sourceScenePath}");
                    continue;
                }

                // Open source scene additively if not already open
                Scene sourceScene = SceneManager.GetSceneByPath(sourceScenePath);
                if (!sourceScene.isLoaded)
                {
                    sourceScene = EditorSceneManager.OpenScene(sourceScenePath, OpenSceneMode.Additive);
                }

                // Find the object by name in the source scene
                GameObject[] rootObjects = sourceScene.GetRootGameObjects();
                GameObject objToMove = null;

                foreach (var root in rootObjects)
                {
                    if (root.name == misplaced.objectName)
                    {
                        objToMove = root;
                        break;
                    }
                }

                if (objToMove != null)
                {
                    // Move to target scene
                    SceneManager.MoveGameObjectToScene(objToMove, targetScene);
                    movedCount++;
                    Debug.Log($"[ChunkValidator] Moved '{objToMove.name}' from {misplaced.sceneName} to chunk ({targetChunk.x},{targetChunk.y})");
                }
                else
                {
                    Debug.LogWarning($"[ChunkValidator] Could not find object '{misplaced.objectName}' in {misplaced.sceneName}");
                }

                // Save and close source scene
                EditorSceneManager.SaveScene(sourceScene);
                EditorSceneManager.CloseScene(sourceScene, false);
            }

            // Save target scene
            EditorSceneManager.SaveScene(targetScene);
        }

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Fix Complete",
            $"Successfully moved {movedCount} objects to their correct chunks.\n\nRun validation again to verify.",
            "OK");

        // Re-validate
        ValidateAllChunks();
    }
}
