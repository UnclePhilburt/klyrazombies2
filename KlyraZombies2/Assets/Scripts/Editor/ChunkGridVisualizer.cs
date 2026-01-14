using UnityEngine;
using UnityEditor;

/// <summary>
/// Visualizes chunk grid boundaries in Scene view.
/// Menu: Project Klyra > World > Toggle Chunk Grid Overlay
/// </summary>
[InitializeOnLoad]
public class ChunkGridVisualizer
{
    private static bool s_ShowGrid = false;
    private static ChunkConfig s_Config;
    private static GUIStyle s_LabelStyle;

    // Keyboard shortcut: Ctrl+Shift+G to toggle
    [MenuItem("Project Klyra/World/Toggle Chunk Grid Overlay %#g")]
    public static void ToggleGrid()
    {
        s_ShowGrid = !s_ShowGrid;
        SceneView.RepaintAll();

        Debug.Log($"[ChunkGrid] Overlay {(s_ShowGrid ? "ENABLED" : "DISABLED")}");
    }

    [MenuItem("Project Klyra/World/Toggle Chunk Grid Overlay %#g", true)]
    public static bool ToggleGridValidate()
    {
        Menu.SetChecked("Project Klyra/World/Toggle Chunk Grid Overlay %#g", s_ShowGrid);
        return true;
    }

    static ChunkGridVisualizer()
    {
        SceneView.duringSceneGui += OnSceneGUI;

        // Load config
        s_Config = Resources.Load<ChunkConfig>("ChunkConfig");

        // Setup label style
        s_LabelStyle = new GUIStyle();
        s_LabelStyle.normal.textColor = Color.white;
        s_LabelStyle.fontSize = 12;
        s_LabelStyle.fontStyle = FontStyle.Bold;
        s_LabelStyle.alignment = TextAnchor.MiddleCenter;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!s_ShowGrid) return;
        if (s_Config == null)
        {
            s_Config = Resources.Load<ChunkConfig>("ChunkConfig");
            if (s_Config == null) return;
        }

        Handles.BeginGUI();

        // Draw instructions
        GUI.backgroundColor = new Color(0, 0, 0, 0.7f);
        GUILayout.BeginArea(new Rect(10, 80, 300, 80));
        GUILayout.BeginVertical("box");
        GUILayout.Label("Chunk Grid Overlay", EditorStyles.boldLabel);
        GUILayout.Label($"Grid: {s_Config.gridSizeX}x{s_Config.gridSizeZ}");
        GUILayout.Label($"Chunk Size: {s_Config.chunkSize}m");
        GUILayout.Label("Toggle: Ctrl+Shift+G");
        GUILayout.EndVertical();
        GUILayout.EndArea();

        Handles.EndGUI();

        // Draw grid in world space
        DrawChunkGrid();
    }

    private static void DrawChunkGrid()
    {
        Vector3 origin = s_Config.worldOrigin;
        float chunkSize = s_Config.chunkSize;
        int gridX = s_Config.gridSizeX;
        int gridZ = s_Config.gridSizeZ;

        // Calculate total world size
        float worldWidth = gridX * chunkSize;
        float worldHeight = gridZ * chunkSize;

        // Draw vertical lines (X axis)
        Handles.color = new Color(0, 1, 1, 0.5f); // Cyan
        for (int x = 0; x <= gridX; x++)
        {
            Vector3 start = origin + new Vector3(x * chunkSize, 0, 0);
            Vector3 end = origin + new Vector3(x * chunkSize, 0, worldHeight);

            // Thicker line for outer boundaries
            if (x == 0 || x == gridX)
                Handles.color = new Color(1, 1, 0, 0.8f); // Yellow for edges
            else
                Handles.color = new Color(0, 1, 1, 0.5f); // Cyan for internal

            Handles.DrawLine(start, end);
        }

        // Draw horizontal lines (Z axis)
        for (int z = 0; z <= gridZ; z++)
        {
            Vector3 start = origin + new Vector3(0, 0, z * chunkSize);
            Vector3 end = origin + new Vector3(worldWidth, 0, z * chunkSize);

            // Thicker line for outer boundaries
            if (z == 0 || z == gridZ)
                Handles.color = new Color(1, 1, 0, 0.8f); // Yellow for edges
            else
                Handles.color = new Color(0, 1, 1, 0.5f); // Cyan for internal

            Handles.DrawLine(start, end);
        }

        // Draw chunk labels
        DrawChunkLabels(origin, chunkSize, gridX, gridZ);
    }

    private static void DrawChunkLabels(Vector3 origin, float chunkSize, int gridX, int gridZ)
    {
        // Only draw labels if zoomed in enough
        Camera sceneCamera = SceneView.lastActiveSceneView?.camera;
        if (sceneCamera == null) return;

        for (int x = 0; x < gridX; x++)
        {
            for (int z = 0; z < gridZ; z++)
            {
                Vector3 chunkCenter = origin + new Vector3(
                    (x + 0.5f) * chunkSize,
                    0,
                    (z + 0.5f) * chunkSize
                );

                // Only draw label if chunk is visible and close enough
                float distanceToCamera = Vector3.Distance(sceneCamera.transform.position, chunkCenter);
                if (distanceToCamera > chunkSize * 5) continue; // Skip distant chunks

                // Draw chunk coordinate label
                GUIStyle labelStyle = new GUIStyle();
                labelStyle.normal.textColor = Color.white;
                labelStyle.fontSize = 14;
                labelStyle.fontStyle = FontStyle.Bold;
                labelStyle.alignment = TextAnchor.MiddleCenter;

                // Add shadow for better readability
                GUIStyle shadowStyle = new GUIStyle(labelStyle);
                shadowStyle.normal.textColor = Color.black;

                string label = $"({x},{z})";

                Handles.Label(chunkCenter + new Vector3(0.5f, 0, 0.5f), label, shadowStyle);
                Handles.Label(chunkCenter, label, labelStyle);

                // Draw chunk name below coordinates
                string chunkName = s_Config.GetChunkSceneName(x, z);
                labelStyle.fontSize = 10;
                shadowStyle.fontSize = 10;

                Handles.Label(chunkCenter + new Vector3(0.5f, -10, 0.5f), chunkName, shadowStyle);
                Handles.Label(chunkCenter + new Vector3(0, -10, 0), chunkName, labelStyle);
            }
        }

        // Draw origin marker
        Handles.color = Color.red;
        Handles.DrawWireCube(origin, Vector3.one * 10f);
        Handles.Label(origin + Vector3.up * 10f, "World Origin", s_LabelStyle);
    }
}
