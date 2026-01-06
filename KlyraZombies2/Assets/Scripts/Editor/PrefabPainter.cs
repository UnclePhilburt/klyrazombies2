using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Paints prefabs onto surfaces with random rotation and scale.
/// Use: Window > Level Design > Prefab Painter
/// </summary>
public class PrefabPainter : EditorWindow
{
    [System.Serializable]
    public class PrefabEntry
    {
        public GameObject prefab;
        public float weight = 1f;
    }

    private List<PrefabEntry> prefabs = new List<PrefabEntry>();
    private float brushSize = 5f;
    private float density = 0.5f;
    private float minScale = 0.8f;
    private float maxScale = 1.2f;
    private bool randomYRotation = true;
    private bool randomXZRotation = false;
    private float maxTilt = 0f;
    private LayerMask paintLayer = ~0;
    private Transform parentObject;

    private bool isPainting = false;
    private Vector2 scrollPos;
    private SerializedObject serializedObject;
    private bool autoStatic = true;
    private bool autoGPUInstancing = true;

    // Preset paths for auto-loading
    private static readonly string[] GrassPresetPaths = new string[]
    {
        // Nature Biomes - Best grass clumps
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Grass_Short_Clump_01.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Grass_Short_Clump_02.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Grass_Short_Clump_03.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Grass_Med_Clump_01.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Grass_Med_Clump_02.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Grass_Tall_Clump_01.prefab",
    };

    private static readonly string[] BushPresetPaths = new string[]
    {
        // Bushes for variety
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Bush_01.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Bush_02.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Bush_03.prefab",
        "Assets/Synty/PolygonMilitary/Prefabs/Environment/SM_Env_Bush_Small_01.prefab",
        "Assets/Synty/PolygonMilitary/Prefabs/Environment/SM_Env_Bush_Small_02.prefab",
    };

    private static readonly string[] RocksSmallPresetPaths = new string[]
    {
        // Small rocks for scatter painting
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Rock_Small_01.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Rock_Small_Pile_01.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Rock_Small_Pile_02.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Rock_Ground_01.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Rock_Ground_02.prefab",
    };

    private static readonly string[] RocksBigPresetPaths = new string[]
    {
        // Bigger rocks and boulders
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Rock_01.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Rock_02.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Rock_03.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Rock_04.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Rock_05.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Rock_Round_01.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Rock_Pile_01.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Rock_Pile_02.prefab",
    };

    private static readonly string[] FlowersPresetPaths = new string[]
    {
        // Flowers
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Flowers_Flat_01.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Flowers_Flat_02.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Flowers_Flat_03.prefab",
    };

    private static readonly string[] TreesForestPresetPaths = new string[]
    {
        // Forest trees - birch and meadow
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Tree_Birch_01.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Tree_Birch_02.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Tree_Birch_03.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Tree_Meadow_01.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Tree_Meadow_02.prefab",
    };

    private static readonly string[] TreesFruitPresetPaths = new string[]
    {
        // Fruit trees - great for farm areas
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Tree_Fruit_01.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Tree_Fruit_02.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Tree_Fruit_03.prefab",
        "Assets/PolygonFarm/Prefabs/Environments/SM_Env_Tree_Apple_Grown_01.prefab",
        "Assets/PolygonFarm/Prefabs/Environments/SM_Env_Tree_Cherry_Grown_01.prefab",
        "Assets/PolygonFarm/Prefabs/Environments/SM_Env_Tree_Pear_Grown_01.prefab",
    };

    private static readonly string[] TreesFarmPresetPaths = new string[]
    {
        // Generic farm trees
        "Assets/PolygonFarm/Prefabs/Generic/SM_Generic_Tree_01.prefab",
        "Assets/PolygonFarm/Prefabs/Generic/SM_Generic_Tree_02.prefab",
        "Assets/PolygonFarm/Prefabs/Generic/SM_Generic_Tree_03.prefab",
        "Assets/PolygonFarm/Prefabs/Generic/SM_Generic_Tree_04.prefab",
        "Assets/PolygonFarm/Prefabs/Environments/SM_Env_Tree_Large_01.prefab",
        "Assets/PolygonFarm/Prefabs/Generic/SM_Generic_TreeDead_01.prefab",
        "Assets/PolygonFarm/Prefabs/Generic/SM_Generic_TreeStump_01.prefab",
    };

    private static readonly string[] FarmPlantsPresetPaths = new string[]
    {
        // Farm-style grass and plants
        "Assets/PolygonFarm/Prefabs/Generic/SM_Generic_Grass_Patch_01.prefab",
        "Assets/PolygonFarm/Prefabs/Generic/SM_Generic_Grass_Patch_02.prefab",
        "Assets/PolygonFarm/Prefabs/Generic/SM_Generic_Grass_Patch_03.prefab",
        "Assets/PolygonFarm/Prefabs/Plants/SM_Prop_Plant_Ground_01_S.prefab",
        "Assets/PolygonFarm/Prefabs/Plants/SM_Prop_Plant_Ground_01_M.prefab",
        "Assets/PolygonFarm/Prefabs/Plants/SM_Prop_Plant_Bush_01_S.prefab",
        "Assets/PolygonFarm/Prefabs/Plants/SM_Prop_Plant_Bush_02_S.prefab",
    };

    private static readonly string[] ApocalypsePresetPaths = new string[]
    {
        // Apocalypse grass tufts (overgrown look)
        "Assets/Synty/PolygonApocalypse/Prefabs/Environment/SM_Env_Grass_Tuft_01.prefab",
        "Assets/Synty/PolygonApocalypse/Prefabs/Environment/SM_Env_Grass_Tuft_02.prefab",
        "Assets/Synty/PolygonApocalypse/Prefabs/Environment/SM_Env_Grass_Tuft_03.prefab",
        "Assets/Synty/PolygonApocalypse/Prefabs/Environment/SM_Env_Bushes_01.prefab",
        "Assets/Synty/PolygonApocalypse/Prefabs/Environment/SM_Env_Bushes_02.prefab",
        "Assets/Synty/PolygonApocalypse/Prefabs/Environment/SM_Env_Bushes_03.prefab",
        "Assets/Synty/PolygonApocalypse/Prefabs/Environment/SM_Env_Bushes_04.prefab",
    };

    private static readonly string[] MixedNaturePresetPaths = new string[]
    {
        // A nice mix of everything for natural areas
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Grass_Short_Clump_01.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Grass_Short_Clump_02.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Grass_Med_Clump_01.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Bush_01.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Rock_Small_01.prefab",
        "Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Prefabs/SM_Env_Flowers_Flat_01.prefab",
    };

    [MenuItem("Window/Level Design/Prefab Painter")]
    public static void ShowWindow()
    {
        GetWindow<PrefabPainter>("Prefab Painter");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;

        // Add a default entry if empty
        if (prefabs.Count == 0)
        {
            prefabs.Add(new PrefabEntry());
        }
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.LabelField("Prefab Painter", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Painting toggle
        EditorGUI.BeginChangeCheck();
        isPainting = EditorGUILayout.Toggle("Enable Painting (hold Shift+Click)", isPainting);
        if (EditorGUI.EndChangeCheck() && isPainting)
        {
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Prefabs to Paint", EditorStyles.boldLabel);

        // Prefab list
        for (int i = 0; i < prefabs.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            prefabs[i].prefab = (GameObject)EditorGUILayout.ObjectField(
                prefabs[i].prefab, typeof(GameObject), false);

            prefabs[i].weight = EditorGUILayout.FloatField(prefabs[i].weight, GUILayout.Width(50));

            if (GUILayout.Button("-", GUILayout.Width(25)))
            {
                prefabs.RemoveAt(i);
                i--;
            }

            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("+ Add Prefab"))
        {
            prefabs.Add(new PrefabEntry());
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quick Presets (click to add)", EditorStyles.boldLabel);

        // Row 1: Ground cover
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Grass")) LoadPreset(GrassPresetPaths);
        if (GUILayout.Button("Bushes")) LoadPreset(BushPresetPaths);
        if (GUILayout.Button("Flowers")) LoadPreset(FlowersPresetPaths);
        EditorGUILayout.EndHorizontal();

        // Row 2: Rocks
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Small Rocks")) LoadPreset(RocksSmallPresetPaths);
        if (GUILayout.Button("Big Rocks")) LoadPreset(RocksBigPresetPaths);
        EditorGUILayout.EndHorizontal();

        // Row 3: Trees
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Forest Trees")) LoadPreset(TreesForestPresetPaths);
        if (GUILayout.Button("Fruit Trees")) LoadPreset(TreesFruitPresetPaths);
        if (GUILayout.Button("Farm Trees")) LoadPreset(TreesFarmPresetPaths);
        EditorGUILayout.EndHorizontal();

        // Row 4: Themed
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Farm Plants")) LoadPreset(FarmPlantsPresetPaths);
        if (GUILayout.Button("Apocalypse")) LoadPreset(ApocalypsePresetPaths);
        if (GUILayout.Button("Mixed Nature")) LoadPreset(MixedNaturePresetPaths);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        if (GUILayout.Button("Clear All Prefabs"))
        {
            prefabs.Clear();
            prefabs.Add(new PrefabEntry());
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Brush Settings", EditorStyles.boldLabel);

        brushSize = EditorGUILayout.Slider("Brush Size", brushSize, 0.5f, 50f);
        density = EditorGUILayout.Slider("Density", density, 0.1f, 5f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Randomization", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Scale Range");
        minScale = EditorGUILayout.FloatField(minScale, GUILayout.Width(50));
        EditorGUILayout.LabelField("to", GUILayout.Width(20));
        maxScale = EditorGUILayout.FloatField(maxScale, GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();

        randomYRotation = EditorGUILayout.Toggle("Random Y Rotation", randomYRotation);
        maxTilt = EditorGUILayout.Slider("Max Tilt (X/Z)", maxTilt, 0f, 45f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);

        parentObject = (Transform)EditorGUILayout.ObjectField(
            "Parent Object", parentObject, typeof(Transform), true);

        // Quick parent creation
        if (GUILayout.Button("Create New Parent Object"))
        {
            GameObject parent = new GameObject("PaintedProps_" + System.DateTime.Now.ToString("HHmmss"));
            parentObject = parent.transform;
            Undo.RegisterCreatedObjectUndo(parent, "Create Paint Parent");
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Performance", EditorStyles.boldLabel);
        autoStatic = EditorGUILayout.Toggle("Auto-mark Static", autoStatic);
        autoGPUInstancing = EditorGUILayout.Toggle("Auto GPU Instancing", autoGPUInstancing);

        // Show object count
        int paintedCount = 0;
        if (parentObject != null)
        {
            paintedCount = parentObject.childCount;
            EditorGUILayout.HelpBox($"Painted objects: {paintedCount}",
                paintedCount > 2000 ? MessageType.Warning : MessageType.Info);

            if (paintedCount > 2000)
            {
                EditorGUILayout.HelpBox("Warning: 2000+ objects may cause lag. Consider lower density or using terrain grass instead.", MessageType.Warning);
            }
        }

        if (GUILayout.Button("Optimize All Painted Objects"))
        {
            OptimizeAllPaintedObjects();
        }

        if (parentObject != null && GUILayout.Button("Combine Meshes (Permanent)"))
        {
            if (EditorUtility.DisplayDialog("Combine Meshes",
                "This will permanently merge all painted objects into a single mesh. This cannot be undone easily. Continue?",
                "Yes, Combine", "Cancel"))
            {
                CombinePaintedMeshes();
            }
        }

        EditorGUILayout.Space();

        // Instructions
        EditorGUILayout.HelpBox(
            "How to use:\n" +
            "1. Add prefabs to the list above\n" +
            "2. Enable painting\n" +
            "3. Hold SHIFT and click/drag in Scene view\n" +
            "4. Use [ and ] keys to adjust brush size",
            MessageType.Info);

        EditorGUILayout.Space();

        // Erase mode
        EditorGUILayout.LabelField("Eraser", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Hold SHIFT+CTRL and click to erase painted objects", MessageType.Info);

        EditorGUILayout.EndScrollView();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!isPainting) return;

        Event e = Event.current;

        // Handle bracket keys for brush size
        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.LeftBracket)
            {
                brushSize = Mathf.Max(0.5f, brushSize - 1f);
                Repaint();
                e.Use();
            }
            else if (e.keyCode == KeyCode.RightBracket)
            {
                brushSize = Mathf.Min(50f, brushSize + 1f);
                Repaint();
                e.Use();
            }
        }

        // Raycast to find ground position
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f, paintLayer))
        {
            // Draw brush preview
            Handles.color = new Color(0f, 1f, 0f, 0.3f);
            Handles.DrawSolidDisc(hit.point, hit.normal, brushSize);
            Handles.color = Color.green;
            Handles.DrawWireDisc(hit.point, hit.normal, brushSize);

            // Paint on shift+click
            if (e.shift && !e.control && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
            {
                PaintPrefabs(hit.point, hit.normal);
                e.Use();
            }

            // Erase on shift+ctrl+click
            if (e.shift && e.control && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
            {
                ErasePrefabs(hit.point);
                e.Use();
            }

            SceneView.RepaintAll();
        }

        // Prevent selection while painting
        if (isPainting && e.shift)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }
    }

    private void PaintPrefabs(Vector3 center, Vector3 normal)
    {
        if (prefabs.Count == 0) return;

        // Calculate how many to spawn based on density
        int count = Mathf.RoundToInt(density * brushSize);
        count = Mathf.Max(1, count);

        for (int i = 0; i < count; i++)
        {
            // Get random prefab based on weights
            GameObject prefab = GetRandomPrefab();
            if (prefab == null) continue;

            // Random position within brush
            Vector2 randomCircle = Random.insideUnitCircle * brushSize;
            Vector3 spawnPos = center + new Vector3(randomCircle.x, 0, randomCircle.y);

            // Raycast down to find exact ground position
            RaycastHit groundHit;
            if (Physics.Raycast(spawnPos + Vector3.up * 100f, Vector3.down, out groundHit, 200f, paintLayer))
            {
                spawnPos = groundHit.point;

                // Calculate rotation
                Quaternion rotation = Quaternion.identity;

                if (randomYRotation)
                {
                    rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                }

                if (maxTilt > 0)
                {
                    rotation *= Quaternion.Euler(
                        Random.Range(-maxTilt, maxTilt),
                        0,
                        Random.Range(-maxTilt, maxTilt));
                }

                // Random scale
                float scale = Random.Range(minScale, maxScale);

                // Spawn the prefab
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.position = spawnPos;
                instance.transform.rotation = rotation;
                instance.transform.localScale = Vector3.one * scale;

                if (parentObject != null)
                {
                    instance.transform.SetParent(parentObject);
                }

                // Auto-optimize
                if (autoStatic)
                {
                    GameObjectUtility.SetStaticEditorFlags(instance, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
                }

                if (autoGPUInstancing)
                {
                    EnableGPUInstancing(instance);
                }

                Undo.RegisterCreatedObjectUndo(instance, "Paint Prefab");
            }
        }
    }

    private void EnableGPUInstancing(GameObject obj)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            foreach (var mat in renderer.sharedMaterials)
            {
                if (mat != null && !mat.enableInstancing)
                {
                    mat.enableInstancing = true;
                    EditorUtility.SetDirty(mat);
                }
            }
        }
    }

    private void OptimizeAllPaintedObjects()
    {
        int optimizedCount = 0;

        // Find all objects under parent or all prefab instances
        GameObject[] toOptimize;

        if (parentObject != null)
        {
            toOptimize = new GameObject[parentObject.childCount];
            for (int i = 0; i < parentObject.childCount; i++)
            {
                toOptimize[i] = parentObject.GetChild(i).gameObject;
            }
        }
        else
        {
            // Find all instances of our prefabs in scene
            toOptimize = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        }

        foreach (var obj in toOptimize)
        {
            if (obj == null) continue;

            // Mark static
            GameObjectUtility.SetStaticEditorFlags(obj,
                StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.OccludeeStatic |
                StaticEditorFlags.OccluderStatic);

            // Enable GPU instancing
            EnableGPUInstancing(obj);
            optimizedCount++;
        }

        Debug.Log($"Optimized {optimizedCount} objects (Static + GPU Instancing enabled)");
    }

    private void CombinePaintedMeshes()
    {
        if (parentObject == null || parentObject.childCount == 0)
        {
            Debug.LogWarning("No painted objects to combine!");
            return;
        }

        // Group by material
        Dictionary<Material, List<CombineInstance>> materialGroups = new Dictionary<Material, List<CombineInstance>>();

        MeshFilter[] meshFilters = parentObject.GetComponentsInChildren<MeshFilter>();

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.sharedMesh == null) continue;

            Renderer renderer = mf.GetComponent<Renderer>();
            if (renderer == null) continue;

            Material mat = renderer.sharedMaterial;
            if (mat == null) continue;

            if (!materialGroups.ContainsKey(mat))
            {
                materialGroups[mat] = new List<CombineInstance>();
            }

            CombineInstance ci = new CombineInstance();
            ci.mesh = mf.sharedMesh;
            ci.transform = mf.transform.localToWorldMatrix;
            materialGroups[mat].Add(ci);
        }

        // Create combined meshes for each material
        GameObject combinedParent = new GameObject("CombinedMeshes_" + System.DateTime.Now.ToString("HHmmss"));
        combinedParent.transform.position = Vector3.zero;

        int meshIndex = 0;
        foreach (var kvp in materialGroups)
        {
            Material mat = kvp.Key;
            List<CombineInstance> combines = kvp.Value;

            // Unity has a vertex limit per mesh, so chunk if needed
            int maxVerts = 65535;
            int currentVerts = 0;
            List<CombineInstance> currentChunk = new List<CombineInstance>();

            for (int i = 0; i < combines.Count; i++)
            {
                int meshVerts = combines[i].mesh.vertexCount;

                if (currentVerts + meshVerts > maxVerts && currentChunk.Count > 0)
                {
                    // Create mesh from current chunk
                    CreateCombinedMesh(combinedParent, mat, currentChunk, meshIndex++);
                    currentChunk.Clear();
                    currentVerts = 0;
                }

                currentChunk.Add(combines[i]);
                currentVerts += meshVerts;
            }

            // Create final chunk
            if (currentChunk.Count > 0)
            {
                CreateCombinedMesh(combinedParent, mat, currentChunk, meshIndex++);
            }
        }

        // Mark combined objects as static
        GameObjectUtility.SetStaticEditorFlags(combinedParent,
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.ContributeGI);

        foreach (Transform child in combinedParent.transform)
        {
            GameObjectUtility.SetStaticEditorFlags(child.gameObject,
                StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.OccludeeStatic |
                StaticEditorFlags.OccluderStatic);
        }

        // Delete original objects
        int originalCount = parentObject.childCount;
        DestroyImmediate(parentObject.gameObject);
        parentObject = combinedParent.transform;

        Debug.Log($"Combined {originalCount} objects into {meshIndex} optimized meshes!");
        Selection.activeGameObject = combinedParent;
    }

    private void CreateCombinedMesh(GameObject parent, Material mat, List<CombineInstance> combines, int index)
    {
        Mesh combinedMesh = new Mesh();
        combinedMesh.name = $"CombinedMesh_{mat.name}_{index}";
        combinedMesh.CombineMeshes(combines.ToArray(), true, true);

        GameObject meshObj = new GameObject($"Combined_{mat.name}_{index}");
        meshObj.transform.SetParent(parent.transform);
        meshObj.transform.position = Vector3.zero;
        meshObj.transform.rotation = Quaternion.identity;

        MeshFilter filter = meshObj.AddComponent<MeshFilter>();
        filter.sharedMesh = combinedMesh;

        MeshRenderer renderer = meshObj.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = mat;

        // Add mesh collider if needed (optional, can be slow)
        // meshObj.AddComponent<MeshCollider>().sharedMesh = combinedMesh;

        // Save the mesh as an asset
        string path = $"Assets/CombinedMeshes";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder("Assets", "CombinedMeshes");
        }
        AssetDatabase.CreateAsset(combinedMesh, $"{path}/{combinedMesh.name}.asset");
    }

    private void ErasePrefabs(Vector3 center)
    {
        // Find all objects within brush radius
        Collider[] colliders = Physics.OverlapSphere(center, brushSize);

        foreach (Collider col in colliders)
        {
            // Check if this is a prefab instance
            if (PrefabUtility.IsPartOfPrefabInstance(col.gameObject))
            {
                GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(col.gameObject);

                // Check if it's one of our paintable prefabs
                foreach (var entry in prefabs)
                {
                    if (entry.prefab != null &&
                        PrefabUtility.GetCorrespondingObjectFromSource(root) == entry.prefab)
                    {
                        Undo.DestroyObjectImmediate(root);
                        break;
                    }
                }
            }
        }
    }

    private GameObject GetRandomPrefab()
    {
        float totalWeight = 0f;
        foreach (var entry in prefabs)
        {
            if (entry.prefab != null)
                totalWeight += entry.weight;
        }

        if (totalWeight <= 0) return null;

        float random = Random.Range(0f, totalWeight);
        float current = 0f;

        foreach (var entry in prefabs)
        {
            if (entry.prefab != null)
            {
                current += entry.weight;
                if (random <= current)
                    return entry.prefab;
            }
        }

        return prefabs[0].prefab;
    }

    private void LoadPreset(string[] paths)
    {
        // Add to existing prefabs instead of replacing
        foreach (string path in paths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                // Check if already in list
                bool exists = false;
                foreach (var entry in prefabs)
                {
                    if (entry.prefab == prefab)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    prefabs.Add(new PrefabEntry { prefab = prefab, weight = 1f });
                }
            }
            else
            {
                Debug.LogWarning($"Prefab not found: {path}");
            }
        }

        // Remove empty entries
        prefabs.RemoveAll(e => e.prefab == null);

        if (prefabs.Count == 0)
        {
            prefabs.Add(new PrefabEntry());
        }

        Repaint();
    }
}
