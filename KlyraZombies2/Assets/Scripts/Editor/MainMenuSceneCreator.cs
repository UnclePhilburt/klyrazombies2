using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Creates a fresh MainMenu scene with character creation built-in.
/// </summary>
public class MainMenuSceneCreator : EditorWindow
{
    private string m_GameSceneName = "MainMap";
    private string m_SceneSavePath = "Assets/Scenes/MainMenu.unity";
    private bool m_BackupExisting = true;

    [MenuItem("Project Klyra/Create Fresh Main Menu Scene")]
    public static void ShowWindow()
    {
        GetWindow<MainMenuSceneCreator>("Create Main Menu");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Create Fresh Main Menu Scene", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This creates a brand new MainMenu scene with:\n" +
            "- Title screen (Play, Settings, Quit)\n" +
            "- Character Creation panel\n" +
            "- Sidekick preview character\n" +
            "- Everything wired up and ready to go",
            MessageType.Info);

        EditorGUILayout.Space();

        m_GameSceneName = EditorGUILayout.TextField("Game Scene Name", m_GameSceneName);
        m_SceneSavePath = EditorGUILayout.TextField("Save Path", m_SceneSavePath);
        m_BackupExisting = EditorGUILayout.Toggle("Backup Existing Scene", m_BackupExisting);

        EditorGUILayout.Space();

        if (GUILayout.Button("Create Main Menu Scene", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("Create Main Menu",
                "This will create a new MainMenu scene.\n\n" +
                (m_BackupExisting ? "Your existing MainMenu.unity will be backed up." : "WARNING: Existing scene will be overwritten!"),
                "Create", "Cancel"))
            {
                // Store values before closing
                string sceneName = m_GameSceneName;
                string savePath = m_SceneSavePath;
                bool backup = m_BackupExisting;

                // Close window first
                Close();

                // Clear selection
                Selection.activeObject = null;

                // Delay to next frame
                EditorApplication.delayCall += () =>
                {
                    Selection.activeObject = null;
                    CreateScene(sceneName, savePath, backup);
                };
            }
        }
    }

    private static void CreateScene(string gameSceneName, string savePath, bool backup)
    {
        try
        {
            // Lock inspector to prevent errors
            ActiveEditorTracker.sharedTracker.isLocked = true;

            // Backup
            if (backup && System.IO.File.Exists(savePath))
            {
                string backupPath = savePath.Replace(".unity", "_backup.unity");
                AssetDatabase.CopyAsset(savePath, backupPath);
                Debug.Log($"[MainMenuSceneCreator] Backed up to: {backupPath}");
            }

            // New scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Build scene contents
            BuildScene(gameSceneName);

            // Save
            EditorSceneManager.SaveScene(scene, savePath);

            Selection.activeObject = null;

            // Unlock inspector
            ActiveEditorTracker.sharedTracker.isLocked = false;

            Debug.Log("[MainMenuSceneCreator] Scene created successfully!");
            EditorUtility.DisplayDialog("Success", "Main Menu scene created!\n\nPress Play to test.", "OK");
        }
        catch (System.Exception e)
        {
            ActiveEditorTracker.sharedTracker.isLocked = false;
            Debug.LogError($"[MainMenuSceneCreator] Error: {e.Message}\n{e.StackTrace}");
        }
    }

    private static void BuildScene(string gameSceneName)
    {
        // Camera
        var cameraObj = new GameObject("Main Camera");
        var camera = cameraObj.AddComponent<Camera>();
        cameraObj.AddComponent<AudioListener>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        cameraObj.transform.position = new Vector3(1.5f, 1f, -3f);
        cameraObj.transform.rotation = Quaternion.Euler(5, 0, 0);
        cameraObj.tag = "MainCamera";

        // Light
        var lightObj = new GameObject("Directional Light");
        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

        // Event System
        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        // Canvas
        var canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Panels
        var mainPanel = CreatePanel("MainPanel", canvasObj.transform, true);
        var settingsPanel = CreatePanel("SettingsPanel", canvasObj.transform, false);
        var charPanel = CreatePanel("CharacterCreationPanel", canvasObj.transform, false);

        // Main Panel Content
        BuildMainPanel(mainPanel);

        // Settings Panel Content
        BuildSettingsPanel(settingsPanel);

        // Character Creation Content
        BuildCharacterCreationPanel(charPanel);

        // Character Preview
        var charPreview = BuildCharacterPreview();

        // Controller
        var controllerObj = new GameObject("MainMenuController");
        var menuUI = controllerObj.AddComponent<MainMenuUI>();

        // Wire MainMenuUI
        var menuSO = new SerializedObject(menuUI);
        menuSO.FindProperty("m_MainPanel").objectReferenceValue = mainPanel;
        menuSO.FindProperty("m_SettingsPanel").objectReferenceValue = settingsPanel;
        menuSO.FindProperty("m_CharacterCreationPanel").objectReferenceValue = charPanel;
        menuSO.FindProperty("m_PlayButton").objectReferenceValue = mainPanel.transform.Find("Buttons/PlayButton")?.GetComponent<Button>();
        menuSO.FindProperty("m_SettingsButton").objectReferenceValue = mainPanel.transform.Find("Buttons/SettingsButton")?.GetComponent<Button>();
        menuSO.FindProperty("m_QuitButton").objectReferenceValue = mainPanel.transform.Find("Buttons/QuitButton")?.GetComponent<Button>();
        menuSO.FindProperty("m_BackButton").objectReferenceValue = settingsPanel.transform.Find("BackButton")?.GetComponent<Button>();
        menuSO.FindProperty("m_MasterVolumeSlider").objectReferenceValue = settingsPanel.transform.Find("Container/MasterRow")?.GetComponentInChildren<Slider>();
        menuSO.FindProperty("m_MusicVolumeSlider").objectReferenceValue = settingsPanel.transform.Find("Container/MusicRow")?.GetComponentInChildren<Slider>();
        menuSO.FindProperty("m_SFXVolumeSlider").objectReferenceValue = settingsPanel.transform.Find("Container/SFXRow")?.GetComponentInChildren<Slider>();
        menuSO.FindProperty("m_GameSceneName").stringValue = gameSceneName;
        menuSO.ApplyModifiedProperties();

        // Wire CharacterCreationUI
        var charUI = charPanel.GetComponent<CharacterCreationUI>();
        if (charUI != null)
        {
            var charSO = new SerializedObject(charUI);
            charSO.FindProperty("m_CharacterController").objectReferenceValue = charPreview.GetComponent<SidekickPlayerController>();
            charSO.FindProperty("m_MainMenuUI").objectReferenceValue = menuUI;
            charSO.FindProperty("m_GameSceneName").stringValue = gameSceneName;

            var controls = charPanel.transform.Find("Controls");
            if (controls != null)
            {
                charSO.FindProperty("m_PrevHeadButton").objectReferenceValue = controls.Find("FaceRow/PrevBtn")?.GetComponent<Button>();
                charSO.FindProperty("m_NextHeadButton").objectReferenceValue = controls.Find("FaceRow/NextBtn")?.GetComponent<Button>();
                charSO.FindProperty("m_HeadLabel").objectReferenceValue = controls.Find("FaceRow/Label")?.GetComponent<TextMeshProUGUI>();
                charSO.FindProperty("m_BodyTypeSlider").objectReferenceValue = controls.Find("BodyTypeRow")?.GetComponentInChildren<Slider>();
                charSO.FindProperty("m_BodyTypeLabel").objectReferenceValue = controls.Find("BodyTypeRow/Label")?.GetComponent<TextMeshProUGUI>();
                charSO.FindProperty("m_MusclesSlider").objectReferenceValue = controls.Find("MusclesRow")?.GetComponentInChildren<Slider>();
                charSO.FindProperty("m_MusclesLabel").objectReferenceValue = controls.Find("MusclesRow/Label")?.GetComponent<TextMeshProUGUI>();
                charSO.FindProperty("m_BodySizeSlider").objectReferenceValue = controls.Find("SizeRow")?.GetComponentInChildren<Slider>();
                charSO.FindProperty("m_BodySizeLabel").objectReferenceValue = controls.Find("SizeRow/Label")?.GetComponent<TextMeshProUGUI>();
                charSO.FindProperty("m_RandomizeButton").objectReferenceValue = controls.Find("Buttons/RandomBtn")?.GetComponent<Button>();
                charSO.FindProperty("m_BackButton").objectReferenceValue = controls.Find("Buttons/BackBtn")?.GetComponent<Button>();
                charSO.FindProperty("m_ConfirmButton").objectReferenceValue = controls.Find("Buttons/StartBtn")?.GetComponent<Button>();
            }

            charSO.FindProperty("m_CharacterSpawnPoint").objectReferenceValue = charPreview.transform.parent.Find("SpawnPoint");
            charSO.ApplyModifiedProperties();
        }
    }

    private static GameObject CreatePanel(string name, Transform parent, bool active)
    {
        var panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var img = panel.AddComponent<Image>();
        img.color = new Color(0.08f, 0.08f, 0.1f, 0.98f);
        panel.SetActive(active);
        return panel;
    }

    private static void BuildMainPanel(GameObject panel)
    {
        // Title
        var title = CreateText("Title", panel.transform, "ZOMBIE SURVIVAL", 64);
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 0.8f);
        titleRect.sizeDelta = new Vector2(600, 80);

        // Buttons container
        var buttons = new GameObject("Buttons");
        buttons.transform.SetParent(panel.transform, false);
        var btnRect = buttons.AddComponent<RectTransform>();
        btnRect.anchorMin = btnRect.anchorMax = new Vector2(0.5f, 0.4f);
        btnRect.sizeDelta = new Vector2(300, 200);
        var vlg = buttons.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 15;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;

        CreateButton("PlayButton", buttons.transform, "PLAY", new Color(0.2f, 0.5f, 0.2f), 55);
        CreateButton("SettingsButton", buttons.transform, "SETTINGS", new Color(0.25f, 0.25f, 0.3f), 55);
        CreateButton("QuitButton", buttons.transform, "QUIT", new Color(0.5f, 0.2f, 0.2f), 55);
    }

    private static void BuildSettingsPanel(GameObject panel)
    {
        // Title
        var title = CreateText("Title", panel.transform, "SETTINGS", 48);
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 0.85f);
        titleRect.sizeDelta = new Vector2(400, 60);

        // Container
        var container = new GameObject("Container");
        container.transform.SetParent(panel.transform, false);
        var contRect = container.AddComponent<RectTransform>();
        contRect.anchorMin = new Vector2(0.3f, 0.35f);
        contRect.anchorMax = new Vector2(0.7f, 0.7f);
        contRect.offsetMin = contRect.offsetMax = Vector2.zero;
        var vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 20;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        CreateSliderRow("MasterRow", container.transform, "Master Volume", 0, 1, 1);
        CreateSliderRow("MusicRow", container.transform, "Music Volume", 0, 1, 1);
        CreateSliderRow("SFXRow", container.transform, "SFX Volume", 0, 1, 1);

        // Back button
        var back = CreateButton("BackButton", panel.transform, "BACK", new Color(0.3f, 0.3f, 0.35f), 50);
        var backRect = back.GetComponent<RectTransform>();
        backRect.anchorMin = backRect.anchorMax = new Vector2(0.5f, 0.15f);
        backRect.sizeDelta = new Vector2(200, 50);
    }

    private static void BuildCharacterCreationPanel(GameObject panel)
    {
        panel.AddComponent<CharacterCreationUI>();

        // Controls container (left side)
        var controls = new GameObject("Controls");
        controls.transform.SetParent(panel.transform, false);
        var ctrlRect = controls.AddComponent<RectTransform>();
        ctrlRect.anchorMin = new Vector2(0, 0);
        ctrlRect.anchorMax = new Vector2(0.35f, 1);
        ctrlRect.offsetMin = new Vector2(20, 20);
        ctrlRect.offsetMax = new Vector2(0, -20);
        var ctrlBg = controls.AddComponent<Image>();
        ctrlBg.color = new Color(0.12f, 0.12f, 0.15f, 0.95f);

        var vlg = controls.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 25, 20);
        vlg.spacing = 12;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        // Title
        var title = CreateText("Title", controls.transform, "CREATE CHARACTER", 26);
        AddLayout(title, -1, 35);

        // Face row
        var faceRow = CreateNavRow("FaceRow", controls.transform, "Face: 1/10");
        AddLayout(faceRow, -1, 45);

        // Spacer
        AddSpacer(controls.transform, 15);

        // Body sliders
        var bodyType = CreateSliderRow("BodyTypeRow", controls.transform, "Body Type: Neutral", -100, 100, 0);
        AddLayout(bodyType, -1, 55);

        var muscles = CreateSliderRow("MusclesRow", controls.transform, "Build: Average", 0, 100, 50);
        AddLayout(muscles, -1, 55);

        var size = CreateSliderRow("SizeRow", controls.transform, "Size: Average", -100, 100, 0);
        AddLayout(size, -1, 55);

        // Spacer
        AddSpacer(controls.transform, 20);

        // Buttons
        var buttons = new GameObject("Buttons");
        buttons.transform.SetParent(controls.transform, false);
        buttons.AddComponent<RectTransform>();
        AddLayout(buttons, -1, 50);
        var hlg = buttons.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;

        CreateButton("RandomBtn", buttons.transform, "RANDOM", new Color(0.3f, 0.3f, 0.35f), 0);
        CreateButton("BackBtn", buttons.transform, "BACK", new Color(0.4f, 0.25f, 0.25f), 0);
        CreateButton("StartBtn", buttons.transform, "START", new Color(0.25f, 0.5f, 0.25f), 0);
    }

    private static GameObject BuildCharacterPreview()
    {
        var container = new GameObject("CharacterPreviewContainer");
        container.transform.position = new Vector3(1.5f, 0, 0);

        var preview = new GameObject("CharacterPreview");
        preview.transform.SetParent(container.transform);
        preview.transform.localPosition = Vector3.zero;

        var controller = preview.AddComponent<SidekickPlayerController>();
        var so = new SerializedObject(controller);
        so.FindProperty("m_BuildFromScratch").boolValue = true;
        so.FindProperty("m_UnderwearOnly").boolValue = true;
        so.FindProperty("m_PreferBasePartsOnly").boolValue = true;
        so.FindProperty("m_DebugLog").boolValue = true;
        so.FindProperty("m_LoadSavedAppearance").boolValue = false;
        so.ApplyModifiedProperties();

        var spawn = new GameObject("SpawnPoint");
        spawn.transform.SetParent(container.transform);
        spawn.transform.localPosition = Vector3.zero;

        return preview;
    }

    private static GameObject CreateText(string name, Transform parent, string text, int size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 40);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return go;
    }

    private static GameObject CreateButton(string name, Transform parent, string text, Color color, float height)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, height > 0 ? height : 45);
        var img = go.AddComponent<Image>();
        img.color = color;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(go.transform, false);
        var txtRect = txtGo.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = txtRect.offsetMax = Vector2.zero;
        var tmp = txtGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        if (height > 0)
        {
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = height;
        }

        return go;
    }

    private static GameObject CreateNavRow(string name, Transform parent, string label)
    {
        var row = new GameObject(name);
        row.transform.SetParent(parent, false);
        row.AddComponent<RectTransform>();
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;

        var prev = CreateButton("PrevBtn", row.transform, "<", new Color(0.3f, 0.3f, 0.35f), 40);
        AddLayout(prev, 45, 40);

        var lbl = CreateText("Label", row.transform, label, 16);
        AddLayout(lbl, 110, 40);

        var next = CreateButton("NextBtn", row.transform, ">", new Color(0.3f, 0.3f, 0.35f), 40);
        AddLayout(next, 45, 40);

        return row;
    }

    private static GameObject CreateSliderRow(string name, Transform parent, string labelText, float min, float max, float val)
    {
        var row = new GameObject(name);
        row.transform.SetParent(parent, false);
        row.AddComponent<RectTransform>();
        var vlg = row.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 3;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        var lbl = CreateText("Label", row.transform, labelText, 14);
        lbl.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
        AddLayout(lbl, -1, 20);

        var slider = CreateSlider("Slider", row.transform, min, max, val);
        AddLayout(slider, -1, 25);

        return row;
    }

    private static GameObject CreateSlider(string name, Transform parent, float min, float max, float val)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(200, 20);

        var slider = go.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = val;

        // Background
        var bg = new GameObject("Background");
        bg.transform.SetParent(go.transform, false);
        var bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.35f);
        bgRect.anchorMax = new Vector2(1, 0.65f);
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f);

        // Fill
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(go.transform, false);
        var faRect = fillArea.AddComponent<RectTransform>();
        faRect.anchorMin = new Vector2(0, 0.35f);
        faRect.anchorMax = new Vector2(1, 0.65f);
        faRect.offsetMin = new Vector2(5, 0);
        faRect.offsetMax = new Vector2(-5, 0);

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fill.AddComponent<Image>().color = new Color(0.35f, 0.55f, 0.35f);

        // Handle
        var handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(go.transform, false);
        var haRect = handleArea.AddComponent<RectTransform>();
        haRect.anchorMin = Vector2.zero;
        haRect.anchorMax = Vector2.one;
        haRect.offsetMin = new Vector2(10, 0);
        haRect.offsetMax = new Vector2(-10, 0);

        var handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        var hRect = handle.AddComponent<RectTransform>();
        hRect.sizeDelta = new Vector2(18, 0);
        var hImg = handle.AddComponent<Image>();
        hImg.color = Color.white;

        slider.fillRect = fillRect;
        slider.handleRect = hRect;
        slider.targetGraphic = hImg;

        return go;
    }

    private static void AddLayout(GameObject go, float w, float h)
    {
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        if (w > 0) le.minWidth = le.preferredWidth = w;
        if (h > 0) le.minHeight = le.preferredHeight = h;
    }

    private static void AddSpacer(Transform parent, float h)
    {
        var spacer = new GameObject("Spacer");
        spacer.transform.SetParent(parent, false);
        spacer.AddComponent<RectTransform>();
        var le = spacer.AddComponent<LayoutElement>();
        le.minHeight = le.preferredHeight = h;
    }
}
