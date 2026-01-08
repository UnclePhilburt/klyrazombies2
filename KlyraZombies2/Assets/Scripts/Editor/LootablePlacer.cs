using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class LootablePlacer : EditorWindow
{
    // Categories for furniture
    private enum Category
    {
        Office,
        Kitchen,
        Bedroom,
        Garage,
        Store,
        Medical,
        Military,
        Misc,
        All
    }

    private Category currentCategory = Category.All;
    private Vector2 scrollPosition;
    private Vector2 prefabScrollPosition;

    // Prefab data
    private List<FurnitureInfo> allFurniture = new List<FurnitureInfo>();
    private List<FurnitureInfo> filteredFurniture = new List<FurnitureInfo>();
    private GameObject selectedPrefab;
    private GameObject previewInstance;

    // Placement settings
    private bool isPlacing = false;
    private float currentRotation = 0f;
    private float rotationSnap = 90f;

    // Loot table assignments per category
    private Dictionary<Category, LootTable> categoryLootTables = new Dictionary<Category, LootTable>();

    // Default loot table for any category without a specific one
    private LootTable defaultLootTable;

    // Search
    private string searchFilter = "";

    // Preview
    private int previewSize = 70;

    // Highlight icon for searchable containers
    private Sprite highlightIcon;

    // Synty paths to search for furniture
    private static readonly string[] FURNITURE_PATHS = new string[]
    {
        "Assets/Synty/PolygonApocalypse/Prefabs/Props",
        "Assets/Synty/PolygonApocalypse/Prefabs/Item",
        "Assets/Synty/PolygonMilitary/Prefabs/Props",
        "Assets/Synty/PolygonMilitary/Prefabs/Props/Military",
        "Assets/Synty/PolygonCity/Prefabs/Props",
        "Assets/Synty/PolygonGeneric/Prefabs/Props"
    };

    // Keywords to identify furniture types
    private static readonly Dictionary<Category, string[]> CATEGORY_KEYWORDS = new Dictionary<Category, string[]>
    {
        { Category.Office, new[] { "desk", "cabinet", "locker", "shelf", "drawer", "bin", "cardboard" } },
        { Category.Kitchen, new[] { "fridge", "kitchen", "cooler", "vending" } },
        { Category.Bedroom, new[] { "dresser", "drawer", "wardrobe" } },
        { Category.Garage, new[] { "toolbox", "tool", "workbench", "barrel", "crate" } },
        { Category.Store, new[] { "shop", "shelf", "fridge", "vending", "counter" } },
        { Category.Medical, new[] { "medical", "first", "health" } },
        { Category.Military, new[] { "ammo", "weapon", "gun", "locker", "crate_ammo", "ammocrate" } },
        { Category.Misc, new[] { "bag", "box", "trash", "rubbish", "duffle", "backpack" } }
    };

    [MenuItem("Tools/Lootable Placer")]
    public static void ShowWindow()
    {
        var window = GetWindow<LootablePlacer>("Lootable Placer");
        window.minSize = new Vector2(380, 600);
    }

    private void OnEnable()
    {
        LoadFurniture();
        FilterFurniture();
        LoadLootTables();
        LoadHighlightIcon();
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void LoadHighlightIcon()
    {
        // Try to load the default magnifier icon
        if (highlightIcon == null)
        {
            highlightIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Clean Vector Icons/T_1_magnifier_.png");
        }
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        CleanupPreview();
    }

    private void LoadFurniture()
    {
        allFurniture.Clear();

        foreach (string basePath in FURNITURE_PATHS)
        {
            if (!Directory.Exists(basePath)) continue;

            string[] files = Directory.GetFiles(basePath, "*.prefab", SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                string assetPath = file.Replace("\\", "/");
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                if (prefab != null && IsFurniture(prefab.name))
                {
                    var info = new FurnitureInfo
                    {
                        prefab = prefab,
                        name = prefab.name,
                        path = assetPath,
                        category = CategorizeFurniture(prefab.name)
                    };
                    allFurniture.Add(info);
                }
            }
        }

        Debug.Log($"[LootablePlacer] Loaded {allFurniture.Count} furniture prefabs");
    }

    private bool IsFurniture(string name)
    {
        name = name.ToLower();

        // Include these
        string[] include = { "desk", "cabinet", "locker", "shelf", "drawer", "fridge", "dresser",
                            "toolbox", "crate", "box", "bag", "bin", "medical", "ammo", "vending",
                            "cooler", "barrel", "wardrobe", "counter", "duffle", "trash", "rubbish" };

        foreach (var keyword in include)
        {
            if (name.Contains(keyword)) return true;
        }

        return false;
    }

    private Category CategorizeFurniture(string name)
    {
        name = name.ToLower();

        foreach (var kvp in CATEGORY_KEYWORDS)
        {
            foreach (var keyword in kvp.Value)
            {
                if (name.Contains(keyword.ToLower()))
                {
                    return kvp.Key;
                }
            }
        }

        return Category.Misc;
    }

    private void FilterFurniture()
    {
        filteredFurniture.Clear();

        foreach (var info in allFurniture)
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

            filteredFurniture.Add(info);
        }

        // Sort alphabetically
        filteredFurniture = filteredFurniture.OrderBy(p => p.name).ToList();
    }

    private void LoadLootTables()
    {
        // Try to find loot tables in Data folder
        string lootTablePath = "Assets/Data/LootTables";
        if (!Directory.Exists(lootTablePath)) return;

        string[] files = Directory.GetFiles(lootTablePath, "*.asset", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            string assetPath = file.Replace("\\", "/");
            LootTable table = AssetDatabase.LoadAssetAtPath<LootTable>(assetPath);

            if (table != null)
            {
                // Try to match table name to category
                string tableName = table.tableName.ToLower();
                foreach (Category cat in System.Enum.GetValues(typeof(Category)))
                {
                    if (cat == Category.All) continue;
                    if (tableName.Contains(cat.ToString().ToLower()))
                    {
                        categoryLootTables[cat] = table;
                        break;
                    }
                }
            }
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Lootable Container Placer", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Category tabs
        EditorGUILayout.BeginHorizontal();
        int catIndex = 0;
        foreach (Category cat in System.Enum.GetValues(typeof(Category)))
        {
            if (catIndex > 0 && catIndex % 5 == 0)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }

            GUI.backgroundColor = (currentCategory == cat) ? Color.cyan : Color.white;
            if (GUILayout.Button(cat.ToString(), GUILayout.Height(22)))
            {
                currentCategory = cat;
                FilterFurniture();
            }
            catIndex++;
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
            FilterFurniture();
        }
        if (GUILayout.Button("X", GUILayout.Width(25)))
        {
            searchFilter = "";
            FilterFurniture();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Loot table assignment
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Loot Table for Category", EditorStyles.boldLabel);

        Category tableCategory = currentCategory == Category.All ? Category.Misc : currentCategory;
        categoryLootTables.TryGetValue(tableCategory, out LootTable currentTable);

        EditorGUI.BeginChangeCheck();
        var newTable = (LootTable)EditorGUILayout.ObjectField(
            tableCategory.ToString(),
            currentTable,
            typeof(LootTable),
            false
        );
        if (EditorGUI.EndChangeCheck())
        {
            categoryLootTables[tableCategory] = newTable;
        }

        if (currentTable == null)
        {
            EditorGUILayout.HelpBox($"No loot table assigned for {tableCategory}. Create one at:\nAssets/Data/LootTables/", MessageType.Warning);

            if (GUILayout.Button("Create Loot Table"))
            {
                CreateLootTable(tableCategory);
            }
        }

        EditorGUILayout.Space(5);

        // Default loot table (used when category doesn't have one)
        EditorGUI.BeginChangeCheck();
        var newDefault = (LootTable)EditorGUILayout.ObjectField(
            "Default (Fallback)",
            defaultLootTable,
            typeof(LootTable),
            false
        );
        if (EditorGUI.EndChangeCheck())
        {
            defaultLootTable = newDefault;
        }

        if (defaultLootTable != null)
        {
            EditorGUILayout.HelpBox("Default table used when category has no specific table.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // Highlight icon setting
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Interaction Highlight", EditorStyles.boldLabel);
        highlightIcon = (Sprite)EditorGUILayout.ObjectField("Search Icon", highlightIcon, typeof(Sprite), false);
        if (highlightIcon == null)
        {
            EditorGUILayout.HelpBox("Assign a search icon for the highlight effect", MessageType.Info);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // Selected prefab info
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        if (selectedPrefab != null)
        {
            EditorGUILayout.LabelField("Selected:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(CleanName(selectedPrefab.name));

            if (GUILayout.Button(isPlacing ? "STOP PLACING (Esc)" : "START PLACING", GUILayout.Height(30)))
            {
                isPlacing = !isPlacing;
                if (!isPlacing) CleanupPreview();
            }

            if (isPlacing)
            {
                EditorGUILayout.HelpBox("Click in Scene to place\nQ/E or Scroll to rotate\nEsc to stop", MessageType.Info);
            }
        }
        else
        {
            EditorGUILayout.LabelField("Select furniture below", EditorStyles.centeredGreyMiniLabel);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // Rotation setting
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Rotation:", GUILayout.Width(60));
        currentRotation = EditorGUILayout.Slider(currentRotation, 0f, 360f);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Prefab grid
        EditorGUILayout.LabelField($"Furniture ({filteredFurniture.Count})", EditorStyles.boldLabel);

        prefabScrollPosition = EditorGUILayout.BeginScrollView(prefabScrollPosition);

        int columns = Mathf.Max(1, (int)((position.width - 20) / (previewSize + 10)));
        int currentCol = 0;

        EditorGUILayout.BeginHorizontal();
        foreach (var info in filteredFurniture)
        {
            if (currentCol >= columns)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                currentCol = 0;
            }

            EditorGUILayout.BeginVertical(GUILayout.Width(previewSize));

            GUI.backgroundColor = (selectedPrefab == info.prefab) ? Color.green : Color.white;

            var preview = AssetPreview.GetAssetPreview(info.prefab);
            GUIContent content = preview != null
                ? new GUIContent(preview)
                : new GUIContent(CleanName(info.name));

            if (GUILayout.Button(content, GUILayout.Width(previewSize), GUILayout.Height(previewSize)))
            {
                selectedPrefab = info.prefab;
                isPlacing = true;
                CleanupPreview();
            }

            GUI.backgroundColor = Color.white;

            // Name label
            string shortName = CleanName(info.name);
            if (shortName.Length > 10) shortName = shortName.Substring(0, 8) + "..";
            EditorGUILayout.LabelField(shortName, EditorStyles.miniLabel, GUILayout.Width(previewSize));

            EditorGUILayout.EndVertical();
            currentCol++;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();

        // Refresh button
        EditorGUILayout.Space(5);
        if (GUILayout.Button("Refresh Furniture List"))
        {
            LoadFurniture();
            FilterFurniture();
        }

        EditorGUILayout.Space(10);

        // Utility to add highlights to existing lootables
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Scene Utilities", EditorStyles.boldLabel);

        if (GUILayout.Button("Add Highlight to All Lootables"))
        {
            AddHighlightToExistingLootables();
        }
        EditorGUILayout.HelpBox("Adds InteractionHighlight to all LootableContainer objects in the scene that don't have one.", MessageType.None);
        EditorGUILayout.EndVertical();
    }

    private void AddHighlightToExistingLootables()
    {
        var lootables = FindObjectsByType<LootableContainer>(FindObjectsSortMode.None);
        int added = 0;

        foreach (var lootable in lootables)
        {
            // Check if already has highlight
            var existing = lootable.GetComponentInChildren<InteractionHighlight>();
            if (existing != null) continue;

            // Find the visual child (usually has MeshRenderer)
            Transform visual = null;
            foreach (Transform child in lootable.transform)
            {
                if (child.GetComponentInChildren<MeshRenderer>() != null)
                {
                    visual = child;
                    break;
                }
            }

            if (visual == null)
            {
                // Try the root if no visual child
                if (lootable.GetComponent<MeshRenderer>() != null)
                {
                    visual = lootable.transform;
                }
            }

            if (visual != null)
            {
                var highlight = visual.gameObject.AddComponent<InteractionHighlight>();

                // Set the search icon
                var iconField = typeof(InteractionHighlight).GetField("m_SearchIcon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (iconField != null && highlightIcon != null)
                {
                    iconField.SetValue(highlight, highlightIcon);
                }

                Undo.RegisterCreatedObjectUndo(highlight, "Add InteractionHighlight");
                added++;
            }
        }

        Debug.Log($"[LootablePlacer] Added InteractionHighlight to {added} lootable containers.");
    }

    private string CleanName(string name)
    {
        return name
            .Replace("SM_Prop_", "")
            .Replace("SM_Item_", "")
            .Replace("SM_Gen_Prop_", "")
            .Replace("_", " ");
    }

    private void OnSceneGUI(SceneView sceneView)
    {
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
            }
            else if (e.keyCode == KeyCode.E)
            {
                currentRotation += rotationSnap;
                currentRotation = ((currentRotation % 360f) + 360f) % 360f;
                e.Use();
                Repaint();
            }
        }

        // Handle scroll wheel for rotation
        if (e.type == EventType.ScrollWheel)
        {
            currentRotation += e.delta.y > 0 ? rotationSnap : -rotationSnap;
            currentRotation = ((currentRotation % 360f) + 360f) % 360f;
            e.Use();
            Repaint();
        }

        // Raycast to find placement position
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Vector3 placementPos = Vector3.zero;
        bool validPlacement = false;

        if (Physics.Raycast(ray, out RaycastHit hit, 10000f))
        {
            placementPos = hit.point;
            validPlacement = true;
        }
        else
        {
            // Fallback: place on Y=0 plane
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float distance))
            {
                placementPos = ray.GetPoint(distance);
                validPlacement = true;
            }
        }

        if (validPlacement)
        {
            // Update preview
            UpdatePreview(placementPos, currentRotation);

            // Draw placement indicator
            Handles.color = Color.green;
            Handles.DrawWireDisc(placementPos, Vector3.up, 0.5f);

            // Draw facing direction
            Vector3 forward = Quaternion.Euler(0, currentRotation, 0) * Vector3.forward * 1f;
            Handles.color = Color.blue;
            Handles.DrawLine(placementPos, placementPos + forward);

            // Handle click to place
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                PlaceLootable(placementPos, currentRotation);
                e.Use();
            }
        }

        sceneView.Repaint();

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

            // Disable colliders on preview
            foreach (var col in previewInstance.GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
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

    private void PlaceLootable(Vector3 position, float rotation)
    {
        if (selectedPrefab == null) return;

        // Create parent container
        GameObject container = GameObject.Find("Lootables");
        if (container == null)
        {
            container = new GameObject("Lootables");
            Undo.RegisterCreatedObjectUndo(container, "Create Lootables Container");
        }

        // Create lootable object (root)
        GameObject instance = new GameObject(selectedPrefab.name + "_Lootable");
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(0, rotation, 0);
        instance.transform.SetParent(container.transform);

        // Add visual mesh as child
        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);
        visual.name = "Visual";
        visual.transform.SetParent(instance.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;

        // Calculate bounds for colliders
        Bounds bounds = CalculateBounds(visual);

        // Add box collider (solid) to root
        BoxCollider boxCol = instance.AddComponent<BoxCollider>();
        boxCol.center = bounds.center - instance.transform.position;
        boxCol.size = bounds.size;
        boxCol.isTrigger = false;

        // Add Inventory component to root
        var inventory = instance.AddComponent<Opsive.UltimateInventorySystem.Core.InventoryCollections.Inventory>();

        // Add LootableContainer component to root
        var lootable = instance.AddComponent<LootableContainer>();

        // Assign loot table based on category (with fallback to default)
        var furnitureInfo = allFurniture.FirstOrDefault(f => f.prefab == selectedPrefab);
        LootTable tableToUse = null;
        Category itemCategory = Category.Misc;

        if (furnitureInfo.prefab != null)
        {
            itemCategory = furnitureInfo.category;
            categoryLootTables.TryGetValue(itemCategory, out tableToUse);
        }

        // Fallback to default table if no category-specific one
        if (tableToUse == null)
        {
            tableToUse = defaultLootTable;
        }

        lootable.lootTable = tableToUse;

        // Create Interactable child with trigger collider
        GameObject interactableChild = new GameObject("Interactable");
        interactableChild.transform.SetParent(instance.transform);
        interactableChild.transform.localPosition = Vector3.zero;
        interactableChild.transform.localRotation = Quaternion.identity;

        // Add sphere trigger collider to child
        SphereCollider triggerCol = interactableChild.AddComponent<SphereCollider>();
        triggerCol.center = boxCol.center;
        triggerCol.radius = Mathf.Max(bounds.size.x, bounds.size.z) * 0.75f + 0.5f; // Slightly larger than object
        triggerCol.isTrigger = true;

        // Add Interactable component (UIS) to child
        var interactableType = System.Type.GetType("Opsive.UltimateInventorySystem.Interactions.Interactable, Opsive.UltimateInventorySystem");
        if (interactableType != null)
        {
            var interactable = interactableChild.AddComponent(interactableType);

            // Set the layer mask to include player layer (layer 31 = 2^31 = 2147483648)
            var layerMaskField = interactableType.GetField("m_InteractorLayerMask", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (layerMaskField != null)
            {
                // Layer 31 bit mask: 1 << 31 = -2147483648 (signed int) or 2147483648 (unsigned)
                layerMaskField.SetValue(interactable, new LayerMask { value = 1 << 31 });
            }
        }

        // Add SimpleLootableStorage to the Interactable child (it extends InteractableBehavior)
        // This opens the StorageMenu showing both inventories side by side
        var storageComponent = interactableChild.AddComponent<SimpleLootableStorage>();

        // Set the storage inventory reference to the root object's inventory
        var storageInventoryField = typeof(SimpleLootableStorage).GetField("m_StorageInventory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (storageInventoryField != null)
        {
            storageInventoryField.SetValue(storageComponent, inventory);
        }

        // Add InteractionHighlight to the visual for glow + icon effect
        var highlight = visual.AddComponent<InteractionHighlight>();

        // Set the search icon via reflection
        var iconField = typeof(InteractionHighlight).GetField("m_SearchIcon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (iconField != null && highlightIcon != null)
        {
            iconField.SetValue(highlight, highlightIcon);
            Debug.Log($"[LootablePlacer] Added InteractionHighlight with icon: {highlightIcon.name}");
        }
        else
        {
            Debug.LogWarning($"[LootablePlacer] InteractionHighlight added but icon not set (highlightIcon={highlightIcon}, iconField={iconField})");
        }

        // Register undo
        Undo.RegisterCreatedObjectUndo(instance, "Place Lootable");

        Debug.Log($"Placed lootable: {instance.name} (Category: {itemCategory}) with {(tableToUse != null ? tableToUse.tableName : "no")} loot table");
    }

    private Bounds CalculateBounds(GameObject obj)
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
            bounds = new Bounds(obj.transform.position, Vector3.one);
        }

        return bounds;
    }

    private void CreateLootTable(Category category)
    {
        // Ensure directory exists
        string dir = "Assets/Data/LootTables";
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // Create new loot table
        LootTable table = ScriptableObject.CreateInstance<LootTable>();
        table.tableName = category.ToString() + " Loot";

        string path = $"{dir}/LootTable_{category}.asset";
        AssetDatabase.CreateAsset(table, path);
        AssetDatabase.SaveAssets();

        categoryLootTables[category] = table;

        // Select it in project
        Selection.activeObject = table;
        EditorGUIUtility.PingObject(table);

        Debug.Log($"Created loot table: {path}");
    }

    private struct FurnitureInfo
    {
        public GameObject prefab;
        public string name;
        public string path;
        public Category category;
    }
}
