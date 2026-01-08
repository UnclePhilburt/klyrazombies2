using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class BuildingPlacer : EditorWindow
{
    // Prefab paths
    private const string APOCALYPSE_BUILDINGS = "Assets/Synty/PolygonApocalypse/Prefabs/Buildings";
    private const string APOCALYPSE_PROPS = "Assets/Synty/PolygonApocalypse/Prefabs/Props";
    private const string MILITARY_BUILDINGS = "Assets/Synty/PolygonMilitary/Prefabs/Buildings";

    // Categories
    private enum Category
    {
        Houses,
        Commercial,
        Industrial,
        Military,
        Props,
        All
    }

    private Category currentCategory = Category.Houses;
    private Vector2 scrollPosition;
    private Vector2 prefabScrollPosition;

    // Prefab data
    private List<PrefabInfo> allPrefabs = new List<PrefabInfo>();
    private List<PrefabInfo> filteredPrefabs = new List<PrefabInfo>();
    private GameObject selectedPrefab;
    private GameObject previewInstance;

    // Placement settings
    private bool alignToGround = true;
    private bool randomRotation = false;
    private float rotationSnap = 90f;
    private float currentRotation = 0f;
    private float placementOffset = 0f;
    private bool isPlacing = false;

    // Road snapping
    private bool snapToRoads = true;
    private float roadSnapDistance = 20f;
    private float buildingSetback = 2f; // Distance from road edge
    private bool autoFaceRoad = true;
    private List<RoadInfo> cachedRoads = new List<RoadInfo>();

    // Search
    private string searchFilter = "";

    private struct RoadInfo
    {
        public Vector3 position;
        public float rotation;
        public Vector3 size;
        public GameObject gameObject;
    }

    // Preview
    private Texture2D[] prefabPreviews;
    private int previewSize = 80;

    [MenuItem("Tools/Building Placer")]
    public static void ShowWindow()
    {
        var window = GetWindow<BuildingPlacer>("Building Placer");
        window.minSize = new Vector2(350, 500);
    }

    private void OnEnable()
    {
        LoadPrefabs();
        FilterPrefabs();
        CacheRoads();
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        CleanupPreview();
    }

    private void LoadPrefabs()
    {
        allPrefabs.Clear();

        // Load Apocalypse buildings
        LoadPrefabsFromPath(APOCALYPSE_BUILDINGS, "Apocalypse");

        // Load Military buildings
        LoadPrefabsFromPath(MILITARY_BUILDINGS, "Military");

        // Load some props
        LoadPrefabsFromPath(APOCALYPSE_PROPS, "Props");

        Debug.Log($"Loaded {allPrefabs.Count} prefabs");
    }

    private void LoadPrefabsFromPath(string path, string source)
    {
        if (!Directory.Exists(path)) return;

        string[] files = Directory.GetFiles(path, "*.prefab", SearchOption.TopDirectoryOnly);
        foreach (string file in files)
        {
            string assetPath = file.Replace("\\", "/");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (prefab != null)
            {
                var info = new PrefabInfo
                {
                    prefab = prefab,
                    name = prefab.name,
                    path = assetPath,
                    source = source,
                    category = CategorizeBuilding(prefab.name)
                };
                allPrefabs.Add(info);
            }
        }
    }

    private Category CategorizeBuilding(string name)
    {
        name = name.ToLower();

        if (name.Contains("house") || name.Contains("apartment") || name.Contains("motel") || name.Contains("trailer"))
            return Category.Houses;

        if (name.Contains("diner") || name.Contains("cafe") || name.Contains("church") || name.Contains("shop") || name.Contains("store"))
            return Category.Commercial;

        if (name.Contains("auto") || name.Contains("warehouse") || name.Contains("factory") || name.Contains("crane") ||
            name.Contains("silo") || name.Contains("tank") || name.Contains("tower") || name.Contains("pylon"))
            return Category.Industrial;

        if (name.Contains("barracks") || name.Contains("tent") || name.Contains("bunker") || name.Contains("military") ||
            name.Contains("guard") || name.Contains("camo") || name.Contains("sandbag"))
            return Category.Military;

        if (name.Contains("prop") || name.Contains("barrel") || name.Contains("crate") || name.Contains("box"))
            return Category.Props;

        return Category.Commercial; // Default
    }

    private void FilterPrefabs()
    {
        filteredPrefabs.Clear();

        foreach (var info in allPrefabs)
        {
            // Category filter
            if (currentCategory != Category.All && info.category != currentCategory)
                continue;

            // Search filter
            if (!string.IsNullOrEmpty(searchFilter))
            {
                if (!info.name.ToLower().Contains(searchFilter.ToLower()))
                    continue;
            }

            // Skip modular pieces for main view (bunker pieces, etc)
            if (IsModularPiece(info.name) && currentCategory != Category.All)
                continue;

            filteredPrefabs.Add(info);
        }

        // Sort alphabetically
        filteredPrefabs = filteredPrefabs.OrderBy(p => p.name).ToList();
    }

    private bool IsModularPiece(string name)
    {
        name = name.ToLower();
        return name.Contains("_wall_") || name.Contains("_floor_") || name.Contains("_ceiling_") ||
               name.Contains("_beam_") || name.Contains("_corner_") || name.Contains("_door_") ||
               name.Contains("_window_") || name.Contains("_stair") || name.Contains("booth_seat") ||
               name.Contains("stool_") || name.Contains("_table_") || name.Contains("_text_");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(5);

        // Category tabs
        EditorGUILayout.BeginHorizontal();
        foreach (Category cat in System.Enum.GetValues(typeof(Category)))
        {
            GUI.backgroundColor = (currentCategory == cat) ? Color.cyan : Color.white;
            if (GUILayout.Button(cat.ToString(), GUILayout.Height(25)))
            {
                currentCategory = cat;
                FilterPrefabs();
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Search
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
        string newSearch = EditorGUILayout.TextField(searchFilter);
        if (newSearch != searchFilter)
        {
            searchFilter = newSearch;
            FilterPrefabs();
        }
        if (GUILayout.Button("X", GUILayout.Width(25)))
        {
            searchFilter = "";
            FilterPrefabs();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Selected prefab info
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        if (selectedPrefab != null)
        {
            EditorGUILayout.LabelField("Selected:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(selectedPrefab.name);

            if (GUILayout.Button(isPlacing ? "STOP PLACING (Esc)" : "START PLACING",
                GUILayout.Height(30)))
            {
                isPlacing = !isPlacing;
                if (!isPlacing) CleanupPreview();
            }

            if (isPlacing)
            {
                EditorGUILayout.HelpBox("Click in Scene view to place\nScroll wheel to rotate\nEsc to stop", MessageType.Info);
            }
        }
        else
        {
            EditorGUILayout.LabelField("Select a prefab below", EditorStyles.centeredGreyMiniLabel);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // Placement settings
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Placement Settings", EditorStyles.boldLabel);
        alignToGround = EditorGUILayout.Toggle("Align to Ground", alignToGround);
        randomRotation = EditorGUILayout.Toggle("Random Rotation", randomRotation);
        if (!randomRotation)
        {
            rotationSnap = EditorGUILayout.Slider("Rotation Snap", rotationSnap, 15f, 90f);
            currentRotation = EditorGUILayout.Slider("Current Rotation", currentRotation, 0f, 360f);
        }
        placementOffset = EditorGUILayout.Slider("Height Offset", placementOffset, -2f, 2f);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // Road snapping settings
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Road Snapping", EditorStyles.boldLabel);
        snapToRoads = EditorGUILayout.Toggle("Snap to Roads", snapToRoads);
        if (snapToRoads)
        {
            autoFaceRoad = EditorGUILayout.Toggle("Auto-Face Road", autoFaceRoad);
            roadSnapDistance = EditorGUILayout.Slider("Snap Distance", roadSnapDistance, 5f, 50f);
            buildingSetback = EditorGUILayout.Slider("Setback from Road", buildingSetback, 0f, 10f);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Roads cached: {cachedRoads.Count}", EditorStyles.miniLabel);
            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
            {
                CacheRoads();
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // Prefab grid
        EditorGUILayout.LabelField($"Prefabs ({filteredPrefabs.Count})", EditorStyles.boldLabel);

        prefabScrollPosition = EditorGUILayout.BeginScrollView(prefabScrollPosition);

        int columns = Mathf.Max(1, (int)((position.width - 20) / (previewSize + 10)));
        int currentCol = 0;

        EditorGUILayout.BeginHorizontal();
        foreach (var info in filteredPrefabs)
        {
            if (currentCol >= columns)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                currentCol = 0;
            }

            EditorGUILayout.BeginVertical(GUILayout.Width(previewSize));

            // Prefab button with preview
            GUI.backgroundColor = (selectedPrefab == info.prefab) ? Color.green : Color.white;

            var preview = AssetPreview.GetAssetPreview(info.prefab);
            GUIContent content = preview != null
                ? new GUIContent(preview)
                : new GUIContent(info.name.Replace("SM_Bld_", "").Replace("SM_Prop_", ""));

            if (GUILayout.Button(content, GUILayout.Width(previewSize), GUILayout.Height(previewSize)))
            {
                selectedPrefab = info.prefab;
                isPlacing = true;
                CleanupPreview();
            }

            GUI.backgroundColor = Color.white;

            // Name label (shortened)
            string shortName = info.name.Replace("SM_Bld_", "").Replace("SM_Prop_", "");
            if (shortName.Length > 12) shortName = shortName.Substring(0, 10) + "..";
            EditorGUILayout.LabelField(shortName, EditorStyles.miniLabel, GUILayout.Width(previewSize));

            EditorGUILayout.EndVertical();
            currentCol++;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();

        // Refresh button
        EditorGUILayout.Space(5);
        if (GUILayout.Button("Refresh Prefabs"))
        {
            LoadPrefabs();
            FilterPrefabs();
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        // Draw road edges when snapping is enabled
        if (snapToRoads && cachedRoads.Count > 0)
        {
            DrawRoadEdges();
        }

        if (!isPlacing || selectedPrefab == null) return;

        Event e = Event.current;

        // Handle escape
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            isPlacing = false;
            CleanupPreview();
            e.Use();
            Repaint();
            return;
        }

        // Handle Q/E for rotation
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

        // Handle scroll wheel for rotation
        if (e.type == EventType.ScrollWheel)
        {
            currentRotation += e.delta.y > 0 ? rotationSnap : -rotationSnap;
            currentRotation = ((currentRotation % 360f) + 360f) % 360f;
            e.Use();
            Repaint();
            return;
        }

        // Raycast to find placement position
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        RaycastHit hit;

        Vector3 placementPos = Vector3.zero;
        float placementRot = currentRotation;
        bool validPlacement = false;
        bool snappedToRoad = false;

        if (Physics.Raycast(ray, out hit, 10000f))
        {
            placementPos = hit.point + Vector3.up * placementOffset;
            validPlacement = true;
        }
        else
        {
            // Fallback: place on Y=0 plane
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            float distance;
            if (groundPlane.Raycast(ray, out distance))
            {
                placementPos = ray.GetPoint(distance) + Vector3.up * placementOffset;
                validPlacement = true;
            }
        }

        // Try to snap to road
        if (validPlacement && snapToRoads && cachedRoads.Count > 0)
        {
            RoadSnapResult? snapResult = FindNearestRoadSnap(placementPos);
            if (snapResult.HasValue)
            {
                placementPos = snapResult.Value.position;
                if (autoFaceRoad && !randomRotation)
                {
                    placementRot = snapResult.Value.rotation;
                }
                snappedToRoad = true;
            }
        }

        if (validPlacement)
        {
            // Update or create preview
            UpdatePreview(placementPos, placementRot);

            // Draw placement indicator
            Handles.color = snappedToRoad ? Color.cyan : Color.green;
            Handles.DrawWireDisc(placementPos, Vector3.up, 2f);
            Handles.DrawLine(placementPos, placementPos + Vector3.up * 3f);

            // Draw facing direction
            Vector3 forward = Quaternion.Euler(0, placementRot, 0) * Vector3.forward * 3f;
            Handles.color = Color.blue;
            Handles.DrawLine(placementPos, placementPos + forward);

            // Handle click to place
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                PlaceBuilding(placementPos, placementRot);
                e.Use();
            }
        }

        // Force repaint for smooth preview
        sceneView.Repaint();

        // Prevent selection in scene view while placing
        if (e.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }
    }

    private void UpdatePreview(Vector3 position, float rotation)
    {
        if (previewInstance == null || previewInstance.name != selectedPrefab.name + "_Preview")
        {
            CleanupPreview();
            previewInstance = Instantiate(selectedPrefab);
            previewInstance.name = selectedPrefab.name + "_Preview";
            previewInstance.hideFlags = HideFlags.HideAndDontSave;

            // Make it semi-transparent
            SetPreviewMaterial(previewInstance);
        }

        previewInstance.transform.position = position;
        previewInstance.transform.rotation = Quaternion.Euler(0, rotation, 0);
    }

    private void SetPreviewMaterial(GameObject obj)
    {
        // Disable colliders on preview
        foreach (var col in obj.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }
    }

    private void CleanupPreview()
    {
        if (previewInstance != null)
        {
            DestroyImmediate(previewInstance);
            previewInstance = null;
        }
    }

    private void PlaceBuilding(Vector3 position, float rotation)
    {
        if (selectedPrefab == null) return;

        float finalRotation = randomRotation ? Random.Range(0f, 360f) : rotation;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(0, finalRotation, 0);

        // Parent to a container for organization
        GameObject container = GameObject.Find("Buildings");
        if (container == null)
        {
            container = new GameObject("Buildings");
        }
        instance.transform.SetParent(container.transform);

        Undo.RegisterCreatedObjectUndo(instance, "Place Building");

        Debug.Log($"Placed {selectedPrefab.name} at {position}");
    }

    private struct PrefabInfo
    {
        public GameObject prefab;
        public string name;
        public string path;
        public string source;
        public Category category;
    }

    private struct RoadSnapResult
    {
        public Vector3 position;
        public float rotation;
        public RoadInfo road;
    }

    private void CacheRoads()
    {
        cachedRoads.Clear();

        GameObject roadsContainer = GameObject.Find("Roads");
        if (roadsContainer == null) return;

        foreach (Transform child in roadsContainer.transform)
        {
            // Measure road size
            Vector3 size = MeasureObjectSize(child.gameObject);

            cachedRoads.Add(new RoadInfo
            {
                position = child.position,
                rotation = child.eulerAngles.y,
                size = size,
                gameObject = child.gameObject
            });
        }

        Debug.Log($"Cached {cachedRoads.Count} roads for building snapping");
    }

    private Vector3 MeasureObjectSize(GameObject obj)
    {
        Bounds bounds = new Bounds(obj.transform.position, Vector3.zero);
        bool hasBounds = false;

        foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
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

        if (!hasBounds)
        {
            return new Vector3(10f, 0.1f, 10f);
        }

        return bounds.size;
    }

    private void DrawRoadEdges()
    {
        Handles.color = new Color(0.5f, 0.8f, 1f, 0.3f);

        foreach (var road in cachedRoads)
        {
            Quaternion rot = Quaternion.Euler(0, road.rotation, 0);
            float halfX = road.size.x / 2f;
            float halfZ = road.size.z / 2f;

            // Draw road outline
            Vector3[] corners = new Vector3[4];
            corners[0] = road.position + rot * new Vector3(-halfX, 0.1f, -halfZ);
            corners[1] = road.position + rot * new Vector3(halfX, 0.1f, -halfZ);
            corners[2] = road.position + rot * new Vector3(halfX, 0.1f, halfZ);
            corners[3] = road.position + rot * new Vector3(-halfX, 0.1f, halfZ);

            Handles.DrawLine(corners[0], corners[1]);
            Handles.DrawLine(corners[1], corners[2]);
            Handles.DrawLine(corners[2], corners[3]);
            Handles.DrawLine(corners[3], corners[0]);
        }
    }

    private RoadSnapResult? FindNearestRoadSnap(Vector3 position)
    {
        RoadSnapResult? bestResult = null;
        float bestDistance = roadSnapDistance;

        foreach (var road in cachedRoads)
        {
            // Transform position to road's local space
            Vector3 toPoint = position - road.position;
            Quaternion invRot = Quaternion.Euler(0, -road.rotation, 0);
            Vector3 localPoint = invRot * toPoint;

            float halfX = road.size.x / 2f;
            float halfZ = road.size.z / 2f;

            // Check each edge of the road
            // Left edge (-X)
            TrySnapToEdge(road, localPoint, new Vector3(-halfX - buildingSetback, 0, localPoint.z),
                road.rotation + 90f, halfZ, ref bestResult, ref bestDistance, position);

            // Right edge (+X)
            TrySnapToEdge(road, localPoint, new Vector3(halfX + buildingSetback, 0, localPoint.z),
                road.rotation - 90f, halfZ, ref bestResult, ref bestDistance, position);

            // Front edge (+Z)
            TrySnapToEdge(road, localPoint, new Vector3(localPoint.x, 0, halfZ + buildingSetback),
                road.rotation + 180f, halfX, ref bestResult, ref bestDistance, position);

            // Back edge (-Z)
            TrySnapToEdge(road, localPoint, new Vector3(localPoint.x, 0, -halfZ - buildingSetback),
                road.rotation, halfX, ref bestResult, ref bestDistance, position);
        }

        return bestResult;
    }

    private void TrySnapToEdge(RoadInfo road, Vector3 localPoint, Vector3 snapLocalPos,
        float facingRotation, float edgeHalfLength, ref RoadSnapResult? bestResult, ref float bestDistance, Vector3 worldPos)
    {
        // Clamp to edge length
        if (Mathf.Abs(snapLocalPos.x) > road.size.x / 2f + buildingSetback + 0.1f)
        {
            snapLocalPos.x = Mathf.Clamp(snapLocalPos.x, -road.size.x / 2f, road.size.x / 2f);
        }
        if (Mathf.Abs(snapLocalPos.z) > road.size.z / 2f + buildingSetback + 0.1f)
        {
            snapLocalPos.z = Mathf.Clamp(snapLocalPos.z, -road.size.z / 2f, road.size.z / 2f);
        }

        // Convert back to world space
        Quaternion rot = Quaternion.Euler(0, road.rotation, 0);
        Vector3 snapWorldPos = road.position + rot * snapLocalPos;
        snapWorldPos.y = worldPos.y; // Keep original height

        float distance = Vector3.Distance(worldPos, snapWorldPos);

        if (distance < bestDistance)
        {
            bestDistance = distance;
            bestResult = new RoadSnapResult
            {
                position = snapWorldPos,
                rotation = facingRotation,
                road = road
            };
        }
    }
}
