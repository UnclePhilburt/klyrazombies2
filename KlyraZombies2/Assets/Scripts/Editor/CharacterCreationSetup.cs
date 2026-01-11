using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor tool to automatically set up the Character Creation UI in the Main Menu scene.
/// Creates all necessary UI elements, preview character, and wires everything together.
/// </summary>
public class CharacterCreationSetup : EditorWindow
{
    private Canvas m_TargetCanvas;
    private MainMenuUI m_MainMenuUI;
    private string m_GameSceneName = "MainMap";

    [MenuItem("Project Klyra/Setup Character Creation UI")]
    public static void ShowWindow()
    {
        GetWindow<CharacterCreationSetup>("Character Creation Setup");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Character Creation Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This tool creates the Character Creation UI panel and preview character.\n\n" +
            "Requirements:\n" +
            "- Must be in the MainMenu scene\n" +
            "- Scene must have a Canvas\n" +
            "- Scene must have MainMenuUI component",
            MessageType.Info);

        EditorGUILayout.Space();

        m_TargetCanvas = EditorGUILayout.ObjectField("Target Canvas", m_TargetCanvas, typeof(Canvas), true) as Canvas;
        m_MainMenuUI = EditorGUILayout.ObjectField("Main Menu UI", m_MainMenuUI, typeof(MainMenuUI), true) as MainMenuUI;
        m_GameSceneName = EditorGUILayout.TextField("Game Scene Name", m_GameSceneName);

        EditorGUILayout.Space();

        if (GUILayout.Button("Auto-Find Components"))
        {
            AutoFindComponents();
        }

        EditorGUILayout.Space();

        GUI.enabled = m_TargetCanvas != null && m_MainMenuUI != null;

        if (GUILayout.Button("Create Character Creation UI", GUILayout.Height(40)))
        {
            CreateCharacterCreationUI();
        }

        GUI.enabled = true;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Individual Steps", EditorStyles.boldLabel);

        if (GUILayout.Button("1. Create Preview Character Only"))
        {
            CreatePreviewCharacter();
        }

        GUI.enabled = m_TargetCanvas != null;
        if (GUILayout.Button("2. Create UI Panel Only"))
        {
            CreateUIPanel();
        }
        GUI.enabled = true;
    }

    private void AutoFindComponents()
    {
        if (m_TargetCanvas == null)
        {
            m_TargetCanvas = FindFirstObjectByType<Canvas>();
        }

        if (m_MainMenuUI == null)
        {
            m_MainMenuUI = FindFirstObjectByType<MainMenuUI>();
        }

        if (m_TargetCanvas != null)
            Debug.Log($"[CharacterCreationSetup] Found Canvas: {m_TargetCanvas.name}");
        else
            Debug.LogWarning("[CharacterCreationSetup] No Canvas found in scene!");

        if (m_MainMenuUI != null)
            Debug.Log($"[CharacterCreationSetup] Found MainMenuUI: {m_MainMenuUI.name}");
        else
            Debug.LogWarning("[CharacterCreationSetup] No MainMenuUI found in scene!");
    }

    private void CreateCharacterCreationUI()
    {
        // Create preview character first
        GameObject previewCharacter = CreatePreviewCharacter();

        // Create UI panel
        GameObject panel = CreateUIPanel();

        // Wire everything together
        WireComponents(panel, previewCharacter);

        Debug.Log("[CharacterCreationSetup] Character Creation UI setup complete!");
        EditorUtility.DisplayDialog("Success",
            "Character Creation UI has been set up!\n\n" +
            "Created:\n" +
            "- CharacterPreview (with SidekickPlayerController)\n" +
            "- CharacterCreationPanel (with all UI elements)\n\n" +
            "The MainMenuUI has been updated to reference the new panel.\n" +
            "Press Play to test!",
            "OK");
    }

    private GameObject CreatePreviewCharacter()
    {
        // Clear selection first to avoid inspector issues
        Selection.activeGameObject = null;

        GameObject preview = null;
        GameObject container = null;

        // Check if preview already exists
        GameObject existing = GameObject.Find("CharacterPreview");
        if (existing != null)
        {
            Debug.Log("[CharacterCreationSetup] CharacterPreview already exists, checking for components");
            preview = existing;
            container = existing.transform.parent?.gameObject;
        }
        else
        {
            // Create container
            container = new GameObject("CharacterCreator");
            container.transform.position = new Vector3(0, 0, 5);

            // Create preview character
            preview = new GameObject("CharacterPreview");
            preview.transform.SetParent(container.transform);
            preview.transform.localPosition = Vector3.zero;

            // Register undo for container (includes children)
            Undo.RegisterCreatedObjectUndo(container, "Create Character Creator");

            Debug.Log("[CharacterCreationSetup] Created new CharacterPreview");
        }

        // Create spawn point if it doesn't exist
        Transform spawnPointTransform = container?.transform.Find("SpawnPoint");
        if (spawnPointTransform == null && container != null)
        {
            GameObject spawnPoint = new GameObject("SpawnPoint");
            spawnPoint.transform.SetParent(container.transform);
            spawnPoint.transform.localPosition = Vector3.zero;
        }

        // Add SidekickPlayerController if not present
        SidekickPlayerController controller = preview.GetComponent<SidekickPlayerController>();
        if (controller == null)
        {
            controller = Undo.AddComponent<SidekickPlayerController>(preview);
            Debug.Log("[CharacterCreationSetup] Added SidekickPlayerController to CharacterPreview");
        }

        // Configure for preview mode (build from scratch, underwear only)
        if (controller != null)
        {
            SerializedObject so = new SerializedObject(controller);

            // Enable build from scratch mode (creates character, doesn't need existing skeleton)
            var buildFromScratchProp = so.FindProperty("m_BuildFromScratch");
            if (buildFromScratchProp != null) buildFromScratchProp.boolValue = true;

            // Enable underwear only mode
            var underwearProp = so.FindProperty("m_UnderwearOnly");
            if (underwearProp != null) underwearProp.boolValue = true;

            // Enable debug logging
            var debugProp = so.FindProperty("m_DebugLog");
            if (debugProp != null) debugProp.boolValue = true;

            // Don't load saved appearance in preview (start fresh)
            var loadProp = so.FindProperty("m_LoadSavedAppearance");
            if (loadProp != null) loadProp.boolValue = false;

            so.ApplyModifiedProperties();
            Debug.Log("[CharacterCreationSetup] Configured SidekickPlayerController for preview mode (BuildFromScratch + UnderwearOnly)");
        }
        else
        {
            Debug.LogError("[CharacterCreationSetup] Failed to add SidekickPlayerController!");
        }

        // Delay selection to next frame to avoid inspector issues
        EditorApplication.delayCall += () =>
        {
            if (preview != null)
            {
                Selection.activeGameObject = preview;
            }
        };

        return preview;
    }

    private GameObject CreateUIPanel()
    {
        if (m_TargetCanvas == null)
        {
            Debug.LogError("[CharacterCreationSetup] No canvas assigned!");
            return null;
        }

        // Clear selection first to avoid inspector issues
        Selection.activeGameObject = null;

        // Check if panel already exists
        Transform existingPanel = m_TargetCanvas.transform.Find("CharacterCreationPanel");
        if (existingPanel != null)
        {
            Debug.Log("[CharacterCreationSetup] CharacterCreationPanel already exists");
            return existingPanel.gameObject;
        }

        // Create main panel
        GameObject panel = CreatePanel("CharacterCreationPanel", m_TargetCanvas.transform);
        panel.SetActive(false); // Start hidden

        // Add background
        Image panelBg = panel.GetComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // Set panel to fill screen
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Create content container (left side for controls)
        GameObject controlsContainer = CreatePanel("ControlsContainer", panel.transform);
        RectTransform controlsRect = controlsContainer.GetComponent<RectTransform>();
        controlsRect.anchorMin = new Vector2(0, 0);
        controlsRect.anchorMax = new Vector2(0.35f, 1);
        controlsRect.offsetMin = new Vector2(20, 20);
        controlsRect.offsetMax = new Vector2(-20, -20);
        Image controlsBg = controlsContainer.GetComponent<Image>();
        controlsBg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        // Add vertical layout
        VerticalLayoutGroup vlg = controlsContainer.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.spacing = 15;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Title
        GameObject title = CreateText("Title", controlsContainer.transform, "CREATE YOUR CHARACTER", 28, FontStyles.Bold);
        SetLayoutElement(title, -1, 50);

        // Spacer
        CreateSpacer(controlsContainer.transform, 20);

        // Face Section
        GameObject faceLabel = CreateText("FaceLabel", controlsContainer.transform, "FACE", 18, FontStyles.Bold);
        SetLayoutElement(faceLabel, -1, 30);

        GameObject faceNav = CreateNavigationRow("FaceNavigation", controlsContainer.transform, "Face: 1/10");
        SetLayoutElement(faceNav, -1, 50);

        // Spacer
        CreateSpacer(controlsContainer.transform, 15);

        // Body Shape Section
        GameObject bodyLabel = CreateText("BodyShapeLabel", controlsContainer.transform, "BODY SHAPE", 18, FontStyles.Bold);
        SetLayoutElement(bodyLabel, -1, 30);

        // Body Type Slider
        GameObject bodyTypeRow = CreateSliderRow("BodyTypeRow", controlsContainer.transform, "Body Type", -100, 100, 0);
        SetLayoutElement(bodyTypeRow, -1, 60);

        // Muscles Slider
        GameObject musclesRow = CreateSliderRow("MusclesRow", controlsContainer.transform, "Build", 0, 100, 50);
        SetLayoutElement(musclesRow, -1, 60);

        // Body Size Slider
        GameObject bodySizeRow = CreateSliderRow("BodySizeRow", controlsContainer.transform, "Size", -100, 100, 0);
        SetLayoutElement(bodySizeRow, -1, 60);

        // Spacer
        CreateSpacer(controlsContainer.transform, 30);

        // Buttons
        GameObject buttonsRow = CreatePanel("ButtonsRow", controlsContainer.transform);
        Image buttonsBg = buttonsRow.GetComponent<Image>();
        buttonsBg.color = Color.clear;
        SetLayoutElement(buttonsRow, -1, 50);

        HorizontalLayoutGroup hlg = buttonsRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 15;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        CreateButton("RandomizeButton", buttonsRow.transform, "RANDOMIZE", new Color(0.3f, 0.3f, 0.3f));
        CreateButton("BackButton", buttonsRow.transform, "BACK", new Color(0.4f, 0.2f, 0.2f));
        CreateButton("ConfirmButton", buttonsRow.transform, "START GAME", new Color(0.2f, 0.5f, 0.2f));

        // Register undo before adding components
        Undo.RegisterCreatedObjectUndo(panel, "Create Character Creation Panel");

        // Add CharacterCreationUI component
        CharacterCreationUI creationUI = Undo.AddComponent<CharacterCreationUI>(panel);

        Debug.Log("[CharacterCreationSetup] Created CharacterCreationPanel with all UI elements");

        return panel;
    }

    private void WireComponents(GameObject panel, GameObject previewCharacter)
    {
        if (panel == null || previewCharacter == null)
        {
            Debug.LogWarning("[CharacterCreationSetup] Cannot wire components - panel or preview is null");
            return;
        }

        CharacterCreationUI creationUI = panel.GetComponent<CharacterCreationUI>();
        if (creationUI == null)
        {
            Debug.LogWarning("[CharacterCreationSetup] Cannot wire components - CharacterCreationUI not found");
            return;
        }

        SidekickPlayerController controller = previewCharacter.GetComponent<SidekickPlayerController>();
        if (controller == null)
        {
            Debug.LogWarning("[CharacterCreationSetup] Cannot wire components - SidekickPlayerController not found");
            return;
        }

        // Use SerializedObject for proper wiring
        SerializedObject so = new SerializedObject(creationUI);

        // Helper to safely set property
        void SetProperty(string name, Object value)
        {
            var prop = so.FindProperty(name);
            if (prop != null) prop.objectReferenceValue = value;
        }

        void SetStringProperty(string name, string value)
        {
            var prop = so.FindProperty(name);
            if (prop != null) prop.stringValue = value;
        }

        // Wire character controller
        SetProperty("m_CharacterController", controller);

        // Wire main menu UI
        SetProperty("m_MainMenuUI", m_MainMenuUI);

        // Wire game scene name
        SetStringProperty("m_GameSceneName", m_GameSceneName);

        // Find and wire UI elements
        Transform controlsContainer = panel.transform.Find("ControlsContainer");
        if (controlsContainer != null)
        {
            // Face navigation
            Transform faceNav = controlsContainer.Find("FaceNavigation");
            if (faceNav != null)
            {
                SetProperty("m_PrevHeadButton", faceNav.Find("PrevButton")?.GetComponent<Button>());
                SetProperty("m_NextHeadButton", faceNav.Find("NextButton")?.GetComponent<Button>());
                SetProperty("m_HeadLabel", faceNav.Find("Label")?.GetComponent<TextMeshProUGUI>());
            }

            // Sliders
            Transform bodyTypeRow = controlsContainer.Find("BodyTypeRow");
            if (bodyTypeRow != null)
            {
                SetProperty("m_BodyTypeSlider", bodyTypeRow.GetComponentInChildren<Slider>());
                SetProperty("m_BodyTypeLabel", bodyTypeRow.Find("Label")?.GetComponent<TextMeshProUGUI>());
            }

            Transform musclesRow = controlsContainer.Find("MusclesRow");
            if (musclesRow != null)
            {
                SetProperty("m_MusclesSlider", musclesRow.GetComponentInChildren<Slider>());
                SetProperty("m_MusclesLabel", musclesRow.Find("Label")?.GetComponent<TextMeshProUGUI>());
            }

            Transform bodySizeRow = controlsContainer.Find("BodySizeRow");
            if (bodySizeRow != null)
            {
                SetProperty("m_BodySizeSlider", bodySizeRow.GetComponentInChildren<Slider>());
                SetProperty("m_BodySizeLabel", bodySizeRow.Find("Label")?.GetComponent<TextMeshProUGUI>());
            }

            // Buttons
            Transform buttonsRow = controlsContainer.Find("ButtonsRow");
            if (buttonsRow != null)
            {
                SetProperty("m_RandomizeButton", buttonsRow.Find("RandomizeButton")?.GetComponent<Button>());
                SetProperty("m_BackButton", buttonsRow.Find("BackButton")?.GetComponent<Button>());
                SetProperty("m_ConfirmButton", buttonsRow.Find("ConfirmButton")?.GetComponent<Button>());
            }

            // Spawn point
            Transform spawnPoint = previewCharacter.transform.parent?.Find("SpawnPoint");
            if (spawnPoint != null)
            {
                SetProperty("m_CharacterSpawnPoint", spawnPoint);
            }
        }

        so.ApplyModifiedProperties();

        // Wire MainMenuUI to reference the panel
        if (m_MainMenuUI != null)
        {
            SerializedObject mainMenuSo = new SerializedObject(m_MainMenuUI);
            var panelProp = mainMenuSo.FindProperty("m_CharacterCreationPanel");
            if (panelProp != null)
            {
                panelProp.objectReferenceValue = panel;
                mainMenuSo.ApplyModifiedProperties();
            }
        }

        Debug.Log("[CharacterCreationSetup] Wired all component references");
    }

    #region UI Creation Helpers

    private GameObject CreatePanel(string name, Transform parent)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        panel.AddComponent<Image>();

        return panel;
    }

    private GameObject CreateText(string name, Transform parent, string text, int fontSize, FontStyles style = FontStyles.Normal)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 40);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return go;
    }

    private GameObject CreateButton(string name, Transform parent, string text, Color bgColor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(120, 40);

        Image img = go.AddComponent<Image>();
        img.color = bgColor;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        // Button text
        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);

        RectTransform textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 16;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return go;
    }

    private GameObject CreateNavigationRow(string name, Transform parent, string labelText)
    {
        GameObject row = new GameObject(name);
        row.transform.SetParent(parent, false);

        RectTransform rowRect = row.AddComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(300, 50);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // Prev button
        GameObject prevBtn = CreateButton("PrevButton", row.transform, "<", new Color(0.3f, 0.3f, 0.3f));
        SetLayoutElement(prevBtn, 50, 40);

        // Label
        GameObject label = CreateText("Label", row.transform, labelText, 18);
        SetLayoutElement(label, 150, 40);

        // Next button
        GameObject nextBtn = CreateButton("NextButton", row.transform, ">", new Color(0.3f, 0.3f, 0.3f));
        SetLayoutElement(nextBtn, 50, 40);

        return row;
    }

    private GameObject CreateSliderRow(string name, Transform parent, string labelText, float min, float max, float value)
    {
        GameObject row = new GameObject(name);
        row.transform.SetParent(parent, false);

        RectTransform rowRect = row.AddComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(300, 60);

        VerticalLayoutGroup vlg = row.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 5;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Label
        GameObject label = CreateText("Label", row.transform, $"{labelText}: Average", 14);
        SetLayoutElement(label, -1, 20);

        // Slider
        GameObject sliderGo = CreateSlider("Slider", row.transform, min, max, value);
        SetLayoutElement(sliderGo, -1, 30);

        return row;
    }

    private GameObject CreateSlider(string name, Transform parent, float min, float max, float value)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 20);

        Slider slider = go.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;

        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(go.transform, false);
        RectTransform bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.25f);
        bgRect.anchorMax = new Vector2(1, 0.75f);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f);

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(go.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1, 0.75f);
        fillAreaRect.offsetMin = new Vector2(5, 0);
        fillAreaRect.offsetMax = new Vector2(-5, 0);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.4f, 0.6f, 0.4f);

        // Handle Area
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(go.transform, false);
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10, 0);
        handleAreaRect.offsetMax = new Vector2(-10, 0);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 0);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;

        // Wire slider
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImg;

        return go;
    }

    private void CreateSpacer(Transform parent, float height)
    {
        GameObject spacer = new GameObject("Spacer");
        spacer.transform.SetParent(parent, false);
        RectTransform rect = spacer.AddComponent<RectTransform>();
        LayoutElement le = spacer.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;
    }

    private void SetLayoutElement(GameObject go, float width, float height)
    {
        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();

        if (width > 0)
        {
            le.minWidth = width;
            le.preferredWidth = width;
        }
        if (height > 0)
        {
            le.minHeight = height;
            le.preferredHeight = height;
        }
    }

    #endregion
}
