using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class RoadBuilder : EditorWindow
{
    // Prefab paths
    private const string APOCALYPSE_ENV = "Assets/Synty/PolygonApocalypse/Prefabs/Environment";
    private const string GENERIC_ENV = "Assets/Synty/PolygonGeneric/Prefabs/Environment";

    // Road types
    private enum RoadType
    {
        Straight,
        Corner,
        Intersection,
        TJunction,
        End,
        Parking,
        Special,
        Damaged,
        Gravel
    }

    // Road piece info
    private class RoadPiece
    {
        public GameObject prefab;
        public string name;
        public RoadType type;
        public Vector3 size;          // Actual measured size
        public Vector3[] exitPoints;  // Local positions where roads can connect
        public float[] exitRotations; // Y rotation at each exit
    }

    private List<RoadPiece> allRoads = new List<RoadPiece>();
    private List<RoadPiece> filteredRoads = new List<RoadPiece>();
    private RoadType currentFilter = RoadType.Straight;

    private RoadPiece selectedRoad;
    private GameObject previewInstance;
    private bool isPlacing = false;

    // Snapping
    private List<SnapPoint> activeSnapPoints = new List<SnapPoint>();
    private SnapPoint? hoveredSnap = null;
    private float snapDistance = 3f;
    private bool autoSnap = true;

    // Placement
    private bool snapToGrid = true;
    private float heightOffset = 0.01f;

    // Drag placement
    private bool isDragging = false;
    private Vector3 lastPlacedPosition;
    private float lastPlacedHeight = -1f;  // Track height for level roads
    private float dragPlaceInterval = 0.5f; // Min distance between drag-placed roads

    // Terrain flattening
    private bool flattenTerrain = true;
    private float flattenPadding = 1f;
    private float flattenBlend = 2f;
    private bool levelRoads = true;  // Keep roads level, flatten terrain to match
    private float maxRoadSlope = 0.1f; // Max height change per road length when not level

    // Rotation
    private float currentRotation = 0f;
    private float rotationSnap = 90f;

    // UI
    private Vector2 scrollPosition;
    private int previewSize = 70;

    private struct SnapPoint
    {
        public Vector3 worldPosition;
        public float worldRotation;
        public float height;  // Store the height for matching
        public GameObject sourceRoad;
    }

    [MenuItem("Project Klyra/Level Design/Road Builder")]
    public static void ShowWindow()
    {
        var window = GetWindow<RoadBuilder>("Road Builder");
        window.minSize = new Vector2(320, 500);
    }

    private void OnEnable()
    {
        LoadRoadPrefabs();
        FilterRoads();
        FindExistingSnapPoints();
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        CleanupPreview();
    }

    private void LoadRoadPrefabs()
    {
        allRoads.Clear();

        // Load Apocalypse roads
        LoadRoadsFromPath(APOCALYPSE_ENV);

        // Load Generic gravel roads
        LoadRoadsFromPath(GENERIC_ENV);

        Debug.Log($"Loaded {allRoads.Count} road pieces");
    }

    private void LoadRoadsFromPath(string path)
    {
        if (!Directory.Exists(path)) return;

        string[] files = Directory.GetFiles(path, "*.prefab", SearchOption.TopDirectoryOnly);
        foreach (string file in files)
        {
            string assetPath = file.Replace("\\", "/");
            string fileName = Path.GetFileNameWithoutExtension(file).ToLower();

            // Only load road-related prefabs
            if (!fileName.Contains("road") && !fileName.Contains("driveway") && !fileName.Contains("path"))
                continue;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null) continue;

            // Auto-detect prefab size
            Vector3 size = MeasurePrefabSize(prefab);

            var piece = new RoadPiece
            {
                prefab = prefab,
                name = prefab.name,
                type = CategorizeRoad(fileName),
                size = size
            };

            // Set up exit points based on measured size
            SetupExitPoints(piece);

            allRoads.Add(piece);
        }
    }

    private Vector3 MeasurePrefabSize(GameObject prefab)
    {
        // Instantiate temporarily to measure bounds
        GameObject temp = Instantiate(prefab);
        temp.hideFlags = HideFlags.HideAndDontSave;

        Bounds bounds = new Bounds(temp.transform.position, Vector3.zero);
        bool hasBounds = false;

        // Get bounds from all renderers
        foreach (var renderer in temp.GetComponentsInChildren<Renderer>())
        {
            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        // Also check mesh filters
        foreach (var meshFilter in temp.GetComponentsInChildren<MeshFilter>())
        {
            if (meshFilter.sharedMesh != null)
            {
                Bounds meshBounds = meshFilter.sharedMesh.bounds;
                Vector3 worldCenter = meshFilter.transform.TransformPoint(meshBounds.center);
                Vector3 worldSize = Vector3.Scale(meshBounds.size, meshFilter.transform.lossyScale);

                if (!hasBounds)
                {
                    bounds = new Bounds(worldCenter, worldSize);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(new Bounds(worldCenter, worldSize));
                }
            }
        }

        DestroyImmediate(temp);

        if (!hasBounds)
        {
            return new Vector3(10f, 0.1f, 10f); // Default fallback
        }

        // Return size, ensuring minimum values
        return new Vector3(
            Mathf.Max(bounds.size.x, 1f),
            Mathf.Max(bounds.size.y, 0.1f),
            Mathf.Max(bounds.size.z, 1f)
        );
    }

    private RoadType CategorizeRoad(string name)
    {
        if (name.Contains("damaged")) return RoadType.Damaged;
        if (name.Contains("gravel")) return RoadType.Gravel;
        if (name.Contains("corner")) return RoadType.Corner;
        if (name.Contains("crossing")) return RoadType.Intersection;
        if (name.Contains("tjunction") || name.Contains("t_junction")) return RoadType.TJunction;
        if (name.Contains("end")) return RoadType.End;
        if (name.Contains("parking")) return RoadType.Parking;
        if (name.Contains("bridge") || name.Contains("ramp") || name.Contains("speed") ||
            name.Contains("median") || name.Contains("arrow") || name.Contains("patch"))
            return RoadType.Special;

        return RoadType.Straight;
    }

    private void SetupExitPoints(RoadPiece piece)
    {
        float halfX = piece.size.x / 2f;
        float halfZ = piece.size.z / 2f;

        // Use the larger dimension as the "length" for straight roads
        float halfLength = Mathf.Max(halfX, halfZ);
        float halfWidth = Mathf.Min(halfX, halfZ);

        switch (piece.type)
        {
            case RoadType.Straight:
            case RoadType.Damaged:
            case RoadType.Special:
                piece.exitPoints = new Vector3[] {
                    new Vector3(0, 0, halfLength),
                    new Vector3(0, 0, -halfLength)
                };
                piece.exitRotations = new float[] { 0f, 180f };
                break;

            case RoadType.Corner:
                piece.exitPoints = new Vector3[] {
                    new Vector3(0, 0, halfLength),
                    new Vector3(halfLength, 0, 0)
                };
                piece.exitRotations = new float[] { 0f, 90f };
                break;

            case RoadType.Intersection:
                piece.exitPoints = new Vector3[] {
                    new Vector3(0, 0, halfLength),
                    new Vector3(0, 0, -halfLength),
                    new Vector3(halfLength, 0, 0),
                    new Vector3(-halfLength, 0, 0)
                };
                piece.exitRotations = new float[] { 0f, 180f, 90f, 270f };
                break;

            case RoadType.TJunction:
                piece.exitPoints = new Vector3[] {
                    new Vector3(0, 0, halfLength),
                    new Vector3(halfLength, 0, 0),
                    new Vector3(-halfLength, 0, 0)
                };
                piece.exitRotations = new float[] { 0f, 90f, 270f };
                break;

            case RoadType.End:
                piece.exitPoints = new Vector3[] {
                    new Vector3(0, 0, -halfLength)
                };
                piece.exitRotations = new float[] { 180f };
                break;

            case RoadType.Parking:
            case RoadType.Gravel:
            default:
                piece.exitPoints = new Vector3[] {
                    new Vector3(0, 0, halfLength),
                    new Vector3(0, 0, -halfLength)
                };
                piece.exitRotations = new float[] { 0f, 180f };
                break;
        }
    }

    private void FilterRoads()
    {
        filteredRoads = allRoads.Where(r => r.type == currentFilter).OrderBy(r => r.name).ToList();
    }

    private void FindExistingSnapPoints()
    {
        activeSnapPoints.Clear();

        GameObject container = GameObject.Find("Roads");
        if (container == null) return;

        foreach (Transform child in container.transform)
        {
            RoadPiece piece = allRoads.FirstOrDefault(r => child.name.StartsWith(r.name));
            if (piece == null) continue;

            AddSnapPointsForRoad(child.gameObject, piece);
        }
    }

    private void AddSnapPointsForRoad(GameObject roadObj, RoadPiece piece)
    {
        if (piece.exitPoints == null) return;

        for (int i = 0; i < piece.exitPoints.Length; i++)
        {
            Vector3 localExit = piece.exitPoints[i];
            float localRot = piece.exitRotations[i];

            Vector3 worldPos = roadObj.transform.TransformPoint(localExit);
            float worldRot = roadObj.transform.eulerAngles.y + localRot;

            bool isConnected = IsSnapPointConnected(worldPos);

            if (!isConnected)
            {
                activeSnapPoints.Add(new SnapPoint
                {
                    worldPosition = worldPos,
                    worldRotation = worldRot,
                    height = roadObj.transform.position.y,  // Store road height
                    sourceRoad = roadObj
                });
            }
        }
    }

    private bool IsSnapPointConnected(Vector3 position)
    {
        GameObject container = GameObject.Find("Roads");
        if (container == null) return false;

        foreach (Transform child in container.transform)
        {
            float dist = Vector3.Distance(child.position, position);
            if (dist < snapDistance && dist > 0.1f)
            {
                return true;
            }
        }
        return false;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(5);

        // Type filter tabs
        EditorGUILayout.BeginHorizontal();
        string[] shortNames = { "Str", "Crn", "Int", "T", "End", "Park", "Spc", "Dmg", "Grv" };
        RoadType[] types = (RoadType[])System.Enum.GetValues(typeof(RoadType));

        for (int i = 0; i < types.Length; i++)
        {
            GUI.backgroundColor = (currentFilter == types[i]) ? Color.cyan : Color.white;
            if (GUILayout.Button(shortNames[i], GUILayout.Height(25)))
            {
                currentFilter = types[i];
                FilterRoads();
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Selected info
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        if (selectedRoad != null)
        {
            EditorGUILayout.LabelField("Selected:", selectedRoad.name.Replace("SM_Env_", "").Replace("SM_Gen_Env_", ""));
            EditorGUILayout.LabelField($"Size: {selectedRoad.size.x:F1} x {selectedRoad.size.z:F1}m", EditorStyles.miniLabel);

            if (GUILayout.Button(isPlacing ? "STOP PLACING (Esc)" : "START PLACING", GUILayout.Height(30)))
            {
                isPlacing = !isPlacing;
                if (isPlacing) FindExistingSnapPoints();
                else CleanupPreview();
            }

            if (isPlacing)
            {
                EditorGUILayout.HelpBox(
                    "Click to place / Drag to chain\n" +
                    "Q/E or Scroll = rotate\n" +
                    "Green = snap points\n" +
                    "Esc = stop", MessageType.Info);
            }
        }
        else
        {
            EditorGUILayout.LabelField("Select a road piece below", EditorStyles.centeredGreyMiniLabel);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // Settings
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
        autoSnap = EditorGUILayout.Toggle("Auto-Snap", autoSnap);
        snapToGrid = EditorGUILayout.Toggle("Grid Snap (free place)", snapToGrid);
        rotationSnap = EditorGUILayout.Slider("Rotation Snap", rotationSnap, 15f, 90f);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // Terrain flattening
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Terrain", EditorStyles.boldLabel);
        flattenTerrain = EditorGUILayout.Toggle("Flatten Under Roads", flattenTerrain);
        if (flattenTerrain)
        {
            levelRoads = EditorGUILayout.Toggle("Keep Roads Level", levelRoads);
            if (!levelRoads)
            {
                maxRoadSlope = EditorGUILayout.Slider("Max Slope", maxRoadSlope, 0.05f, 0.3f);
            }
            flattenPadding = EditorGUILayout.Slider("Extra Width", flattenPadding, 0f, 5f);
            flattenBlend = EditorGUILayout.Slider("Blend", flattenBlend, 0f, 5f);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // Road prefab grid
        EditorGUILayout.LabelField($"Roads ({filteredRoads.Count})", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        int columns = Mathf.Max(1, (int)((position.width - 20) / (previewSize + 10)));
        int currentCol = 0;

        EditorGUILayout.BeginHorizontal();
        foreach (var road in filteredRoads)
        {
            if (currentCol >= columns)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                currentCol = 0;
            }

            EditorGUILayout.BeginVertical(GUILayout.Width(previewSize));

            GUI.backgroundColor = (selectedRoad == road) ? Color.green : Color.white;

            var preview = AssetPreview.GetAssetPreview(road.prefab);
            GUIContent content = preview != null
                ? new GUIContent(preview)
                : new GUIContent(road.name.Replace("SM_Env_Road_", "").Replace("SM_Gen_Env_Road_", ""));

            if (GUILayout.Button(content, GUILayout.Width(previewSize), GUILayout.Height(previewSize)))
            {
                selectedRoad = road;
                isPlacing = true;
                currentRotation = 0f;
                CleanupPreview();
                FindExistingSnapPoints();
            }

            GUI.backgroundColor = Color.white;

            string shortName = road.name.Replace("SM_Env_Road_", "").Replace("SM_Gen_Env_Road_", "").Replace("SM_Env_", "");
            if (shortName.Length > 10) shortName = shortName.Substring(0, 8) + "..";
            EditorGUILayout.LabelField(shortName, EditorStyles.miniLabel, GUILayout.Width(previewSize));

            EditorGUILayout.EndVertical();
            currentCol++;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();

        // Utility buttons
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh"))
        {
            LoadRoadPrefabs();
            FilterRoads();
            FindExistingSnapPoints();
        }
        if (GUILayout.Button("Refresh Snaps"))
        {
            FindExistingSnapPoints();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        // Always draw snap points if we have roads
        if (activeSnapPoints.Count > 0)
        {
            DrawSnapPoints();
        }

        if (!isPlacing || selectedRoad == null) return;

        Event e = Event.current;

        // Escape to cancel
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            isPlacing = false;
            isDragging = false;
            CleanupPreview();
            e.Use();
            Repaint();
            return;
        }

        // Q/E to rotate
        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.Q)
            {
                currentRotation -= rotationSnap;
                currentRotation = ((currentRotation % 360f) + 360f) % 360f;
                e.Use();
                Repaint();
                return;
            }
            else if (e.keyCode == KeyCode.E)
            {
                currentRotation += rotationSnap;
                currentRotation = ((currentRotation % 360f) + 360f) % 360f;
                e.Use();
                Repaint();
                return;
            }
        }

        // Scroll to rotate
        if (e.type == EventType.ScrollWheel)
        {
            currentRotation += e.delta.y > 0 ? rotationSnap : -rotationSnap;
            currentRotation = ((currentRotation % 360f) + 360f) % 360f;
            e.Use();
            Repaint();
            return;
        }

        // Find placement position
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Vector3 placementPos = Vector3.zero;
        float placementRot = currentRotation;
        float placementHeight = 0f;
        bool validPlacement = false;
        bool snappedToRoad = false;
        hoveredSnap = null;

        // Check for snap points first
        if (autoSnap && activeSnapPoints.Count > 0)
        {
            SnapPoint? closest = FindClosestSnapPoint(ray);
            if (closest.HasValue)
            {
                hoveredSnap = closest.Value;
                placementPos = closest.Value.worldPosition;
                placementPos.y = closest.Value.height; // Match neighbor height!
                placementRot = closest.Value.worldRotation + 180f;
                placementHeight = closest.Value.height;
                validPlacement = true;
                snappedToRoad = true;
            }
        }

        // Raycast to terrain/ground
        if (!validPlacement)
        {
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 10000f))
            {
                placementPos = hit.point;
                validPlacement = true;
            }
            else
            {
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                float distance;
                if (groundPlane.Raycast(ray, out distance))
                {
                    placementPos = ray.GetPoint(distance);
                    validPlacement = true;
                }
            }

            // Grid snap for free placement
            if (validPlacement && snapToGrid)
            {
                float gridSize = Mathf.Max(selectedRoad.size.x, selectedRoad.size.z);
                placementPos.x = Mathf.Round(placementPos.x / gridSize) * gridSize;
                placementPos.z = Mathf.Round(placementPos.z / gridSize) * gridSize;
            }

            placementRot = currentRotation;
        }

        if (validPlacement)
        {
            UpdatePreview(placementPos, placementRot);

            // Draw placement indicator
            Handles.color = snappedToRoad ? Color.green : Color.yellow;
            float indicatorSize = Mathf.Max(selectedRoad.size.x, selectedRoad.size.z) / 2f;
            Handles.DrawWireDisc(placementPos, Vector3.up, indicatorSize);

            // Draw direction arrow
            Vector3 forward = Quaternion.Euler(0, placementRot, 0) * Vector3.forward * indicatorSize;
            Handles.DrawLine(placementPos, placementPos + forward);
            Handles.ConeHandleCap(0, placementPos + forward * 0.8f, Quaternion.LookRotation(forward), 0.5f, EventType.Repaint);

            // Handle mouse input for drag placement
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                // Determine target height: snapped road > last placed (if level) > auto
                float targetH = snappedToRoad ? placementHeight : (levelRoads && lastPlacedHeight >= 0 ? lastPlacedHeight : -1f);
                float placedHeight = PlaceRoadWithUndo(placementPos, placementRot, targetH);
                lastPlacedPosition = placementPos;
                lastPlacedHeight = placedHeight;
                isDragging = true;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && e.button == 0 && isDragging)
            {
                // Place while dragging if we've moved far enough
                float distFromLast = Vector3.Distance(placementPos, lastPlacedPosition);
                float roadLength = Mathf.Max(selectedRoad.size.x, selectedRoad.size.z);

                if (distFromLast >= roadLength * 0.9f)
                {
                    // When dragging, use last placed height for level roads
                    float targetH = snappedToRoad ? placementHeight : (levelRoads ? lastPlacedHeight : -1f);
                    float placedHeight = PlaceRoadWithUndo(placementPos, placementRot, targetH);
                    lastPlacedPosition = placementPos;
                    lastPlacedHeight = placedHeight;
                }
                e.Use();
            }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                isDragging = false;
                // Reset last height when releasing so next click starts fresh (unless snapping)
                if (!levelRoads)
                {
                    lastPlacedHeight = -1f;
                }
                e.Use();
            }
        }

        sceneView.Repaint();

        if (e.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }
    }

    private void DrawSnapPoints()
    {
        foreach (var snap in activeSnapPoints)
        {
            bool isHovered = hoveredSnap.HasValue &&
                Vector3.Distance(hoveredSnap.Value.worldPosition, snap.worldPosition) < 0.1f;

            Handles.color = isHovered ? Color.yellow : Color.green;
            float size = isHovered ? 1.2f : 0.6f;

            Handles.SphereHandleCap(0, snap.worldPosition, Quaternion.identity, size, EventType.Repaint);

            // Draw direction arrow
            Vector3 dir = Quaternion.Euler(0, snap.worldRotation, 0) * Vector3.forward;
            Handles.DrawLine(snap.worldPosition, snap.worldPosition + dir * 2f);
        }
    }

    private SnapPoint? FindClosestSnapPoint(Ray ray)
    {
        SnapPoint? closest = null;
        float closestScreenDist = float.MaxValue;

        foreach (var snap in activeSnapPoints)
        {
            Vector3 screenPos = HandleUtility.WorldToGUIPoint(snap.worldPosition);
            Vector2 mousePos = Event.current.mousePosition;
            float screenDist = Vector2.Distance(screenPos, mousePos);

            // More forgiving snap distance (80 pixels)
            if (screenDist < 80f && screenDist < closestScreenDist)
            {
                closestScreenDist = screenDist;
                closest = snap;
            }
        }

        return closest;
    }

    private void UpdatePreview(Vector3 position, float rotation)
    {
        if (previewInstance == null || !previewInstance.name.StartsWith(selectedRoad.name))
        {
            CleanupPreview();
            previewInstance = Instantiate(selectedRoad.prefab);
            previewInstance.name = selectedRoad.name + "_Preview";
            previewInstance.hideFlags = HideFlags.HideAndDontSave;

            // Disable colliders
            foreach (var col in previewInstance.GetComponentsInChildren<Collider>())
                col.enabled = false;

            // Make semi-transparent
            foreach (var renderer in previewInstance.GetComponentsInChildren<Renderer>())
            {
                foreach (var mat in renderer.materials)
                {
                    if (mat != null)
                    {
                        mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, 0.5f);
                    }
                }
            }
        }

        previewInstance.transform.position = position;
        previewInstance.transform.rotation = Quaternion.Euler(0, rotation, 0);
    }

    private void CleanupPreview()
    {
        if (previewInstance != null)
        {
            DestroyImmediate(previewInstance);
            previewInstance = null;
        }
    }

    private float PlaceRoadWithUndo(Vector3 position, float rotation, float matchHeight)
    {
        // Start undo group for road + terrain
        Undo.SetCurrentGroupName("Place Road");
        int undoGroup = Undo.GetCurrentGroup();

        float finalHeight;

        // Flatten terrain first if enabled
        if (flattenTerrain)
        {
            float targetHeight = matchHeight >= 0 ? matchHeight : -1f;
            float terrainHeight = FlattenTerrainUnderRoad(position, rotation, selectedRoad.size, targetHeight);
            position.y = terrainHeight + heightOffset;
            finalHeight = terrainHeight;
        }
        else if (matchHeight >= 0)
        {
            position.y = matchHeight + heightOffset;
            finalHeight = matchHeight;
        }
        else
        {
            finalHeight = position.y;
        }

        // Create road instance
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(selectedRoad.prefab);
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(0, rotation, 0);

        // Parent to container
        GameObject container = GameObject.Find("Roads");
        if (container == null)
        {
            container = new GameObject("Roads");
            Undo.RegisterCreatedObjectUndo(container, "Create Roads Container");
        }
        instance.transform.SetParent(container.transform);

        Undo.RegisterCreatedObjectUndo(instance, "Place Road");

        // Collapse undo group
        Undo.CollapseUndoOperations(undoGroup);

        // Refresh snap points
        FindExistingSnapPoints();

        return finalHeight;
    }

    private float FlattenTerrainUnderRoad(Vector3 position, float rotation, Vector3 roadSize, float targetHeight)
    {
        Terrain terrain = GetTerrainAtPosition(position);
        if (terrain == null)
        {
            return position.y;
        }

        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;

        // Register terrain for undo BEFORE modifying
        Undo.RegisterCompleteObjectUndo(terrainData, "Flatten Terrain");

        float roadLength = Mathf.Max(roadSize.x, roadSize.z);
        float totalSize = roadLength + (flattenPadding * 2) + (flattenBlend * 2);
        float halfSize = totalSize / 2f;

        int heightmapRes = terrainData.heightmapResolution;
        float terrainWidth = terrainData.size.x;
        float terrainLength = terrainData.size.z;

        Quaternion rot = Quaternion.Euler(0, rotation, 0);

        // Determine target height
        float centerHeight;
        if (targetHeight >= 0)
        {
            // Match neighbor/specified height
            centerHeight = (targetHeight - terrainPos.y) / terrainData.size.y;
        }
        else
        {
            // Sample terrain to determine height
            int sampleCount = 5;
            float totalHeight = 0f;
            float highestHeight = 0f;
            float lowestHeight = float.MaxValue;

            for (int i = 0; i < sampleCount; i++)
            {
                for (int j = 0; j < sampleCount; j++)
                {
                    float sx = (i / (float)(sampleCount - 1) - 0.5f) * roadLength;
                    float sz = (j / (float)(sampleCount - 1) - 0.5f) * roadLength;
                    Vector3 samplePos = position + rot * new Vector3(sx, 0, sz);
                    float h = terrain.SampleHeight(samplePos);
                    totalHeight += h;
                    if (h > highestHeight) highestHeight = h;
                    if (h < lowestHeight) lowestHeight = h;
                }
            }

            if (levelRoads)
            {
                // Use average height for level roads - creates smoother results
                float avgHeight = totalHeight / (sampleCount * sampleCount);
                // Bias slightly toward higher to avoid roads sinking
                centerHeight = Mathf.Lerp(avgHeight, highestHeight, 0.3f) / terrainData.size.y;
            }
            else
            {
                // Allow slope - use center point height adjusted by max slope
                float centerTerrainHeight = terrain.SampleHeight(position);
                centerHeight = centerTerrainHeight / terrainData.size.y;
            }
        }

        // Get list of existing roads in the area for per-point checking
        List<RoadBounds> existingRoads = GetExistingRoadsInArea(position, totalSize);

        // Calculate bounds
        Vector3[] corners = new Vector3[4];
        corners[0] = position + rot * new Vector3(-halfSize, 0, -halfSize);
        corners[1] = position + rot * new Vector3(halfSize, 0, -halfSize);
        corners[2] = position + rot * new Vector3(-halfSize, 0, halfSize);
        corners[3] = position + rot * new Vector3(halfSize, 0, halfSize);

        int minX = int.MaxValue, maxX = int.MinValue;
        int minZ = int.MaxValue, maxZ = int.MinValue;

        foreach (var corner in corners)
        {
            int x = Mathf.RoundToInt((corner.x - terrainPos.x) / terrainWidth * heightmapRes);
            int z = Mathf.RoundToInt((corner.z - terrainPos.z) / terrainLength * heightmapRes);
            minX = Mathf.Min(minX, x);
            maxX = Mathf.Max(maxX, x);
            minZ = Mathf.Min(minZ, z);
            maxZ = Mathf.Max(maxZ, z);
        }

        minX = Mathf.Clamp(minX, 0, heightmapRes - 1);
        maxX = Mathf.Clamp(maxX, 0, heightmapRes - 1);
        minZ = Mathf.Clamp(minZ, 0, heightmapRes - 1);
        maxZ = Mathf.Clamp(maxZ, 0, heightmapRes - 1);

        int sizeX = maxX - minX + 1;
        int sizeZ = maxZ - minZ + 1;

        if (sizeX <= 0 || sizeZ <= 0) return position.y;

        float[,] heights = terrainData.GetHeights(minX, minZ, sizeX, sizeZ);

        float roadHalfSize = roadLength / 2f + flattenPadding;

        for (int z = 0; z < sizeZ; z++)
        {
            for (int x = 0; x < sizeX; x++)
            {
                float worldX = (minX + x) / (float)heightmapRes * terrainWidth + terrainPos.x;
                float worldZ = (minZ + z) / (float)heightmapRes * terrainLength + terrainPos.z;

                Vector3 toPoint = new Vector3(worldX - position.x, 0, worldZ - position.z);
                Vector3 localPoint = Quaternion.Inverse(rot) * toPoint;

                float distFromRoadX = Mathf.Max(0, Mathf.Abs(localPoint.x) - roadHalfSize);
                float distFromRoadZ = Mathf.Max(0, Mathf.Abs(localPoint.z) - roadHalfSize);
                float distFromRoad = Mathf.Max(distFromRoadX, distFromRoadZ);

                float blend;
                if (distFromRoad <= 0)
                {
                    blend = 1f;
                }
                else if (distFromRoad < flattenBlend)
                {
                    float t = distFromRoad / flattenBlend;
                    blend = 1f - (t * t * (3f - 2f * t));
                }
                else
                {
                    blend = 0f;
                }

                // Calculate the target height for this point
                float targetHeightForPoint = Mathf.Lerp(heights[z, x], centerHeight, blend);

                // Check if this point is under an existing road - if so, don't raise above it
                Vector3 worldPoint = new Vector3(worldX, 0, worldZ);
                foreach (var road in existingRoads)
                {
                    if (IsPointUnderRoad(worldPoint, road))
                    {
                        float maxHeight = (road.height - terrainPos.y) / terrainData.size.y - 0.002f;
                        targetHeightForPoint = Mathf.Min(targetHeightForPoint, maxHeight);
                    }
                }

                heights[z, x] = targetHeightForPoint;
            }
        }

        terrainData.SetHeights(minX, minZ, heights);

        return centerHeight * terrainData.size.y + terrainPos.y;
    }

    private Terrain GetTerrainAtPosition(Vector3 position)
    {
        Terrain[] terrains = Terrain.activeTerrains;
        foreach (var terrain in terrains)
        {
            Vector3 terrainPos = terrain.transform.position;
            TerrainData data = terrain.terrainData;

            if (position.x >= terrainPos.x && position.x <= terrainPos.x + data.size.x &&
                position.z >= terrainPos.z && position.z <= terrainPos.z + data.size.z)
            {
                return terrain;
            }
        }

        if (terrains.Length > 0)
            return terrains[0];

        return null;
    }

    private struct RoadBounds
    {
        public Vector3 position;
        public float rotation;
        public Vector3 size;
        public float height;
    }

    private List<RoadBounds> GetExistingRoadsInArea(Vector3 position, float radius)
    {
        List<RoadBounds> roads = new List<RoadBounds>();

        GameObject container = GameObject.Find("Roads");
        if (container == null) return roads;

        foreach (Transform child in container.transform)
        {
            // Check horizontal distance
            float horizontalDist = Vector2.Distance(
                new Vector2(position.x, position.z),
                new Vector2(child.position.x, child.position.z)
            );

            if (horizontalDist < radius)
            {
                // Find the road piece info to get size
                RoadPiece piece = allRoads.FirstOrDefault(r => child.name.StartsWith(r.name));
                Vector3 size = piece != null ? piece.size : new Vector3(10f, 0.1f, 10f);

                roads.Add(new RoadBounds
                {
                    position = child.position,
                    rotation = child.eulerAngles.y,
                    size = size,
                    height = child.position.y
                });
            }
        }

        return roads;
    }

    private bool IsPointUnderRoad(Vector3 point, RoadBounds road)
    {
        // Transform point to road's local space
        Vector3 toPoint = point - road.position;
        Quaternion invRot = Quaternion.Euler(0, -road.rotation, 0);
        Vector3 localPoint = invRot * toPoint;

        // Check if point is within road bounds (with small padding)
        float halfX = road.size.x / 2f + 0.5f;
        float halfZ = road.size.z / 2f + 0.5f;

        return Mathf.Abs(localPoint.x) < halfX && Mathf.Abs(localPoint.z) < halfZ;
    }
}
