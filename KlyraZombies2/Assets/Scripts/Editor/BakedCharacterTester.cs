using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Quick test tool for baked character prefabs.
/// Creates a minimal test scene and builds for WebGL.
/// </summary>
public class BakedCharacterTester : EditorWindow
{
    private GameObject m_CharacterPrefab;
    private RuntimeAnimatorController m_AnimatorController;

    [MenuItem("Project Klyra/Sidekick/Test Baked Character")]
    public static void ShowWindow()
    {
        var window = GetWindow<BakedCharacterTester>("Test Baked Character");
        window.minSize = new Vector2(400, 250);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Baked Character Tester", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Quickly test your baked character prefab.\n\n" +
            "1. Drag your baked prefab below\n" +
            "2. Click 'Create Test Scene'\n" +
            "3. Build for WebGL",
            MessageType.Info);

        EditorGUILayout.Space(10);

        m_CharacterPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Baked Character Prefab", m_CharacterPrefab, typeof(GameObject), false);

        m_AnimatorController = (RuntimeAnimatorController)EditorGUILayout.ObjectField(
            "Animator Controller (Optional)", m_AnimatorController, typeof(RuntimeAnimatorController), false);

        EditorGUILayout.Space(20);

        GUI.enabled = m_CharacterPrefab != null;

        if (GUILayout.Button("Create Test Scene", GUILayout.Height(35)))
        {
            CreateTestScene();
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("Create Test Scene & Open Build Settings", GUILayout.Height(35)))
        {
            CreateTestScene();
            EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
        }

        GUI.enabled = true;
    }

    private void CreateTestScene()
    {
        // Save current scene if needed
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
        }

        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Add camera
        var cameraObj = new GameObject("Main Camera");
        cameraObj.tag = "MainCamera";
        var camera = cameraObj.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.2f, 0.2f, 0.3f);
        cameraObj.transform.position = new Vector3(0, 1.5f, 3f);
        cameraObj.transform.LookAt(new Vector3(0, 1f, 0));
        cameraObj.AddComponent<AudioListener>();

        // Add directional light
        var lightObj = new GameObject("Directional Light");
        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Instantiate character prefab
        var character = (GameObject)PrefabUtility.InstantiatePrefab(m_CharacterPrefab);
        character.transform.position = Vector3.zero;
        character.transform.rotation = Quaternion.Euler(0, 180f, 0); // Face camera

        // Add animator controller if provided
        if (m_AnimatorController != null)
        {
            var animator = character.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.runtimeAnimatorController = m_AnimatorController;
            }
        }

        // Add a simple rotation script so we can see the character from all angles
        var rotator = character.AddComponent<SimpleRotator>();

        // Add ground plane
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(2, 1, 2);
        var groundRenderer = ground.GetComponent<Renderer>();
        groundRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        groundRenderer.material.color = new Color(0.3f, 0.3f, 0.3f);

        // Add UI text
        var canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var textObj = new GameObject("Instructions");
        textObj.transform.SetParent(canvasObj.transform);
        var text = textObj.AddComponent<UnityEngine.UI.Text>();
        text.text = "Baked Character Test\nPress A/D to rotate";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 24;
        text.color = Color.white;
        text.alignment = TextAnchor.UpperLeft;
        var rectTransform = textObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.anchoredPosition = new Vector2(20, -20);
        rectTransform.sizeDelta = new Vector2(400, 100);

        // Save scene
        string scenePath = "Assets/Scenes/BakedCharacterTest.unity";
        EditorSceneManager.SaveScene(scene, scenePath);

        // Add to build settings
        var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool alreadyInBuild = false;
        foreach (var s in buildScenes)
        {
            if (s.path == scenePath)
            {
                alreadyInBuild = true;
                break;
            }
        }
        if (!alreadyInBuild)
        {
            buildScenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
        }

        Debug.Log($"[BakedCharacterTester] Created test scene at: {scenePath}");
        EditorUtility.DisplayDialog("Test Scene Created",
            $"Test scene created at:\n{scenePath}\n\nNow build for WebGL to test!", "OK");
    }
}

/// <summary>
/// Simple rotation script for testing.
/// </summary>
public class SimpleRotator : MonoBehaviour
{
    public float rotationSpeed = 100f;

    void Update()
    {
        float input = 0;
        if (Input.GetKey(KeyCode.A)) input = 1;
        if (Input.GetKey(KeyCode.D)) input = -1;

        if (input != 0)
        {
            transform.Rotate(Vector3.up, input * rotationSpeed * Time.deltaTime);
        }
    }
}
