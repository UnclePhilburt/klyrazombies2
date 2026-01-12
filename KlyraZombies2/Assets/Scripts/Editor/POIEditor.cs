#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor tools for creating and managing Points of Interest.
/// </summary>
public class POIEditor : EditorWindow
{
    private string m_NewPOIName = "New Location";
    private string m_NewPOIDescription = "";
    private POICategory m_NewPOICategory = POICategory.Building;
    private float m_TriggerRadius = 15f;
    private string m_LootHint = "";

    private Vector2 m_ScrollPosition;
    private POITrigger[] m_AllTriggers;

    [MenuItem("Project Klyra/POI/POI Manager")]
    public static void ShowWindow()
    {
        var window = GetWindow<POIEditor>("POI Manager");
        window.minSize = new Vector2(350, 400);
    }

    [MenuItem("Project Klyra/POI/Create POI at View")]
    public static void CreatePOIAtView()
    {
        // Get scene view camera position
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
        {
            EditorUtility.DisplayDialog("No Scene View", "Please have a Scene View open and focused.", "OK");
            return;
        }

        Vector3 position = sceneView.camera.transform.position + sceneView.camera.transform.forward * 20f;
        position.y = 0; // Ground level

        CreatePOITrigger("New POI", position, 15f);
    }

    [MenuItem("GameObject/POI/Create POI Trigger", false, 10)]
    public static void CreatePOITriggerMenu()
    {
        Vector3 position = Vector3.zero;

        // Use selection position if available
        if (Selection.activeTransform != null)
        {
            position = Selection.activeTransform.position;
        }

        CreatePOITrigger("New POI", position, 15f);
    }

    private void OnEnable()
    {
        RefreshTriggerList();
    }

    private void OnFocus()
    {
        RefreshTriggerList();
    }

    private void RefreshTriggerList()
    {
        m_AllTriggers = FindObjectsByType<POITrigger>(FindObjectsSortMode.None);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("POI Manager", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

        DrawCreateSection();
        EditorGUILayout.Space(15);
        DrawExistingPOIsSection();
        EditorGUILayout.Space(15);
        DrawUtilitiesSection();

        EditorGUILayout.EndScrollView();
    }

    private void DrawCreateSection()
    {
        EditorGUILayout.LabelField("Create New POI", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        m_NewPOIName = EditorGUILayout.TextField("Name", m_NewPOIName);
        m_NewPOICategory = (POICategory)EditorGUILayout.EnumPopup("Category", m_NewPOICategory);

        EditorGUILayout.LabelField("Description");
        m_NewPOIDescription = EditorGUILayout.TextArea(m_NewPOIDescription, GUILayout.Height(40));

        m_LootHint = EditorGUILayout.TextField("Loot Hint", m_LootHint);
        m_TriggerRadius = EditorGUILayout.Slider("Trigger Radius", m_TriggerRadius, 5f, 100f);

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Create at Origin", GUILayout.Height(30)))
        {
            CreateCompletePOI(Vector3.zero);
        }

        if (GUILayout.Button("Create at View", GUILayout.Height(30)))
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                Vector3 position = sceneView.camera.transform.position + sceneView.camera.transform.forward * 20f;
                position.y = 0;
                CreateCompletePOI(position);
            }
            else
            {
                CreateCompletePOI(Vector3.zero);
            }
        }

        EditorGUILayout.EndHorizontal();

        if (Selection.activeTransform != null)
        {
            if (GUILayout.Button($"Create at Selection ({Selection.activeTransform.name})", GUILayout.Height(25)))
            {
                CreateCompletePOI(Selection.activeTransform.position);
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawExistingPOIsSection()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"POIs in Scene ({(m_AllTriggers?.Length ?? 0)})", EditorStyles.boldLabel);
        if (GUILayout.Button("Refresh", GUILayout.Width(60)))
        {
            RefreshTriggerList();
        }
        EditorGUILayout.EndHorizontal();

        if (m_AllTriggers == null || m_AllTriggers.Length == 0)
        {
            EditorGUILayout.HelpBox("No POI Triggers found in scene.", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        foreach (var trigger in m_AllTriggers)
        {
            if (trigger == null) continue;

            EditorGUILayout.BeginHorizontal();

            // POI name or GameObject name
            string displayName = trigger.POIData != null ? trigger.POIData.displayName : trigger.gameObject.name;
            string category = trigger.POIData != null ? trigger.POIData.category.ToString() : "No Data";

            EditorGUILayout.LabelField($"{displayName} ({category})", GUILayout.MinWidth(150));

            if (GUILayout.Button("Select", GUILayout.Width(50)))
            {
                Selection.activeGameObject = trigger.gameObject;
                SceneView.lastActiveSceneView?.Frame(new Bounds(trigger.transform.position, Vector3.one * trigger.TriggerRadius * 2), false);
            }

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                if (EditorUtility.DisplayDialog("Delete POI", $"Delete POI '{displayName}'?", "Delete", "Cancel"))
                {
                    Undo.DestroyObjectImmediate(trigger.gameObject);
                    RefreshTriggerList();
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawUtilitiesSection()
    {
        EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (GUILayout.Button("Reset All Discoveries (PlayerPrefs)"))
        {
            if (EditorUtility.DisplayDialog("Reset Discoveries", "This will clear all discovered POIs from PlayerPrefs. Continue?", "Reset", "Cancel"))
            {
                PlayerPrefs.DeleteKey("DiscoveredPOIs");
                PlayerPrefs.Save();
                Debug.Log("[POIEditor] Cleared all discovered POIs from PlayerPrefs");
            }
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("Open POI Data Folder"))
        {
            string path = "Assets/Data/POIs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
            EditorUtility.RevealInFinder(path);
        }

        EditorGUILayout.EndVertical();
    }

    private void CreateCompletePOI(Vector3 position)
    {
        // Create POI Data asset
        POIData poiData = CreatePOIDataAsset(m_NewPOIName, m_NewPOIDescription, m_NewPOICategory, m_LootHint);

        // Create trigger in scene
        GameObject triggerObj = CreatePOITrigger(m_NewPOIName, position, m_TriggerRadius);

        // Assign data to trigger
        POITrigger trigger = triggerObj.GetComponent<POITrigger>();
        SerializedObject so = new SerializedObject(trigger);
        so.FindProperty("m_POIData").objectReferenceValue = poiData;
        so.ApplyModifiedProperties();

        // Select the new trigger
        Selection.activeGameObject = triggerObj;
        SceneView.lastActiveSceneView?.Frame(new Bounds(position, Vector3.one * m_TriggerRadius * 2), false);

        RefreshTriggerList();

        Debug.Log($"[POIEditor] Created POI '{m_NewPOIName}' at {position}");
    }

    private static POIData CreatePOIDataAsset(string displayName, string description, POICategory category, string lootHint)
    {
        // Ensure directory exists
        string directory = "Assets/Data/POIs";
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Create asset
        POIData poiData = ScriptableObject.CreateInstance<POIData>();
        poiData.displayName = displayName;
        poiData.description = description;
        poiData.category = category;
        poiData.lootHint = lootHint;
        poiData.poiId = displayName.Replace(" ", "_").ToLower();

        // Generate unique filename
        string filename = displayName.Replace(" ", "_");
        string path = $"{directory}/POI_{filename}.asset";
        path = AssetDatabase.GenerateUniqueAssetPath(path);

        AssetDatabase.CreateAsset(poiData, path);
        AssetDatabase.SaveAssets();

        return poiData;
    }

    private static GameObject CreatePOITrigger(string name, Vector3 position, float radius)
    {
        // Create parent container if it doesn't exist
        GameObject container = GameObject.Find("--- POI Triggers ---");
        if (container == null)
        {
            container = new GameObject("--- POI Triggers ---");
            Undo.RegisterCreatedObjectUndo(container, "Create POI Container");
        }

        // Create trigger object
        GameObject triggerObj = new GameObject($"POI_{name.Replace(" ", "_")}");
        triggerObj.transform.SetParent(container.transform);
        triggerObj.transform.position = position;

        // Add trigger component (it will auto-add SphereCollider)
        POITrigger trigger = triggerObj.AddComponent<POITrigger>();

        // Set radius via serialized property
        SerializedObject so = new SerializedObject(trigger);
        so.FindProperty("m_TriggerRadius").floatValue = radius;
        so.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(triggerObj, "Create POI Trigger");

        return triggerObj;
    }
}
#endif
