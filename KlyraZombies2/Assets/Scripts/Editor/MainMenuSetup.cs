using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor tool to create a properly configured Main Menu scene for character creation.
/// Creates UI for individual part selection (Hair, Eyebrows, Nose, Ears, Facial Hair),
/// body shape sliders, and color pickers (Skin, Hair, Eyes).
/// </summary>
public class MainMenuSetup : EditorWindow
{
    [MenuItem("Project Klyra/Create Main Menu Scene")]
    public static void ShowWindow()
    {
        CreateMainMenuScene();
    }

    public static void CreateMainMenuScene()
    {
        // Create new scene
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ========== CAMERA ==========
        GameObject cameraObj = new GameObject("Main Camera");
        Camera cam = cameraObj.AddComponent<Camera>();
        cameraObj.AddComponent<AudioListener>();
        cameraObj.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f);
        // Position camera to frame character on right side of screen, closer for larger appearance
        cam.transform.position = new Vector3(0.6f, 1f, -1.8f);
        cam.transform.rotation = Quaternion.Euler(5, 0, 0);

        // ========== LIGHTING ==========
        CreateLighting();

        // ========== EVENT SYSTEM ==========
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        // Try to add InputSystemUIInputModule for new Input System, fall back to StandaloneInputModule
        var inputModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputModuleType != null)
        {
            eventSystem.AddComponent(inputModuleType);
            Debug.Log("[MainMenuSetup] Using InputSystemUIInputModule");
        }
        else
        {
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[MainMenuSetup] Using StandaloneInputModule");
        }

        // ========== 3D CHARACTER PREVIEW (Outside Canvas!) ==========
        GameObject characterPreview = new GameObject("CharacterPreview");
        // Position character to the right of camera center (camera at x=0.6, character at x=1.4)
        characterPreview.transform.position = new Vector3(1.4f, 0, 0);
        characterPreview.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f); // Slightly larger

        GameObject spawnPoint = new GameObject("SpawnPoint");
        spawnPoint.transform.SetParent(characterPreview.transform);
        spawnPoint.transform.localPosition = Vector3.zero;
        spawnPoint.transform.localRotation = Quaternion.Euler(0, 180, 0);

        // Add SidekickPlayerController
        SidekickPlayerController controller = characterPreview.AddComponent<SidekickPlayerController>();
        SerializedObject controllerSO = new SerializedObject(controller);
        controllerSO.FindProperty("m_BuildFromScratch").boolValue = true;
        controllerSO.FindProperty("m_LoadSavedAppearance").boolValue = false;
        controllerSO.FindProperty("m_UnderwearOnly").boolValue = true;
        controllerSO.FindProperty("m_DebugLog").boolValue = true;

        // Try to find a human idle animator controller (avoid zombie animations)
        string[] animGuids = AssetDatabase.FindAssets("Idle t:AnimatorController");
        RuntimeAnimatorController foundController = null;
        foreach (var guid in animGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            // Skip zombie animations
            if (path.ToLower().Contains("zombie")) continue;
            // Prefer Synty or human animations
            if (path.Contains("Synty") || path.Contains("Human") || path.Contains("Character"))
            {
                foundController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
                Debug.Log($"[MainMenuSetup] Using animator: {path}");
                break;
            }
        }
        // If no specific idle found, just don't assign one (T-pose is fine for character creation)
        if (foundController != null)
        {
            controllerSO.FindProperty("m_AnimatorController").objectReferenceValue = foundController;
        }
        else
        {
            Debug.Log("[MainMenuSetup] No suitable animator found, character will be in default pose");
        }
        controllerSO.ApplyModifiedProperties();

        // ========== CANVAS ==========
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // ========== MAIN PANEL ==========
        GameObject mainPanel = CreateFullScreenPanel(canvasObj.transform, "MainPanel", new Color(0.08f, 0.08f, 0.12f, 1f));

        // Title
        GameObject title = CreateText(mainPanel.transform, "Title", "ZOMBIE SURVIVAL", 72);
        SetAnchors(title, new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), new Vector2(600, 100));

        // Buttons
        GameObject playBtn = CreateButton(mainPanel.transform, "PlayButton", "PLAY", new Color(0.2f, 0.5f, 0.3f, 1f));
        SetAnchors(playBtn, new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.45f), new Vector2(250, 60));

        GameObject settingsBtn = CreateButton(mainPanel.transform, "SettingsButton", "SETTINGS", new Color(0.25f, 0.25f, 0.3f, 1f));
        SetAnchors(settingsBtn, new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f), new Vector2(250, 60));

        GameObject quitBtn = CreateButton(mainPanel.transform, "QuitButton", "QUIT", new Color(0.5f, 0.25f, 0.25f, 1f));
        SetAnchors(quitBtn, new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f), new Vector2(250, 60));

        // ========== SETTINGS PANEL ==========
        GameObject settingsPanel = CreateCenteredPanel(canvasObj.transform, "SettingsPanel", new Color(0.1f, 0.1f, 0.15f, 0.98f), 600, 500);
        settingsPanel.SetActive(false);

        // Settings content
        GameObject settingsTitle = CreateText(settingsPanel.transform, "Title", "SETTINGS", 36);
        SetAnchors(settingsTitle, new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f), new Vector2(300, 50));

        GameObject settingsBack = CreateButton(settingsPanel.transform, "BackButton", "BACK", new Color(0.4f, 0.3f, 0.3f, 1f));
        SetAnchors(settingsBack, new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.1f), new Vector2(150, 45));

        // ========== CHARACTER CREATION PANEL (LEFT SIDE WITH SCROLLVIEW) ==========
        var charCreationUIData = CreateCharacterCreationPanel(canvasObj.transform);
        charCreationUIData.panel.SetActive(false);

        // ========== MAIN MENU CONTROLLER ==========
        GameObject menuControllerObj = new GameObject("MainMenuController");
        MainMenuUI mainMenuUI = menuControllerObj.AddComponent<MainMenuUI>();

        SerializedObject menuSO = new SerializedObject(mainMenuUI);
        menuSO.FindProperty("m_MainPanel").objectReferenceValue = mainPanel;
        menuSO.FindProperty("m_SettingsPanel").objectReferenceValue = settingsPanel;
        menuSO.FindProperty("m_CharacterCreationPanel").objectReferenceValue = charCreationUIData.panel;
        menuSO.FindProperty("m_CharacterPreviewContainer").objectReferenceValue = characterPreview;
        menuSO.FindProperty("m_PlayButton").objectReferenceValue = playBtn.GetComponent<Button>();
        menuSO.FindProperty("m_SettingsButton").objectReferenceValue = settingsBtn.GetComponent<Button>();
        menuSO.FindProperty("m_QuitButton").objectReferenceValue = quitBtn.GetComponent<Button>();
        menuSO.ApplyModifiedProperties();

        // ========== CHARACTER CREATION UI ==========
        CharacterCreationUI charUI = charCreationUIData.panel.AddComponent<CharacterCreationUI>();
        WireUpCharacterCreationUI(charUI, controller, spawnPoint, mainMenuUI, charCreationUIData);

        // Save scene
        EditorSceneManager.MarkSceneDirty(newScene);
        string savePath = "Assets/Scenes/MainMenu.unity";
        EditorSceneManager.SaveScene(newScene, savePath);

        Debug.Log($"[MainMenuSetup] Created Main Menu scene at {savePath}");
        EditorUtility.DisplayDialog("Main Menu Created",
            "Main Menu scene created successfully!\n\n" +
            "Features:\n" +
            "- Individual part pickers (Hair, Eyebrows, Nose, Ears, Facial Hair)\n" +
            "- Body shape sliders\n" +
            "- Color pickers (Skin, Hair, Eyes)\n" +
            "- Character visible on RIGHT side\n" +
            "- Proper 3-point lighting\n\n" +
            "Click PLAY to show character creation.",
            "OK");
    }

    static void CreateLighting()
    {
        // Key light
        GameObject keyLight = new GameObject("Key Light");
        Light kLight = keyLight.AddComponent<Light>();
        kLight.type = LightType.Directional;
        kLight.intensity = 1.2f;
        kLight.color = new Color(1f, 0.95f, 0.9f);
        keyLight.transform.rotation = Quaternion.Euler(45, -45, 0);

        // Fill light
        GameObject fillLight = new GameObject("Fill Light");
        Light fLight = fillLight.AddComponent<Light>();
        fLight.type = LightType.Directional;
        fLight.intensity = 0.4f;
        fLight.color = new Color(0.7f, 0.8f, 1f);
        fillLight.transform.rotation = Quaternion.Euler(30, 135, 0);

        // Rim light
        GameObject rimLight = new GameObject("Rim Light");
        Light rLight = rimLight.AddComponent<Light>();
        rLight.type = LightType.Directional;
        rLight.intensity = 0.6f;
        rLight.color = new Color(0.9f, 0.9f, 1f);
        rimLight.transform.rotation = Quaternion.Euler(10, 180, 0);
    }

    struct CharacterCreationUIData
    {
        public GameObject panel;
        // Body Shape
        public Slider bodyTypeSlider;
        public Slider musclesSlider;
        public Slider bodySizeSlider;
        public TextMeshProUGUI bodyTypeLabel;
        public TextMeshProUGUI musclesLabel;
        public TextMeshProUGUI bodySizeLabel;
        // Parts
        public Button prevHairBtn, nextHairBtn;
        public TextMeshProUGUI hairLabel;
        public Button prevEyebrowsBtn, nextEyebrowsBtn;
        public TextMeshProUGUI eyebrowsLabel;
        public Button prevNoseBtn, nextNoseBtn;
        public TextMeshProUGUI noseLabel;
        public Button prevEarsBtn, nextEarsBtn;
        public TextMeshProUGUI earsLabel;
        public Button prevFacialHairBtn, nextFacialHairBtn, clearFacialHairBtn;
        public TextMeshProUGUI facialHairLabel;
        // Colors
        public Image skinColorPreview;
        public Button prevSkinColorBtn, nextSkinColorBtn;
        public Image hairColorPreview;
        public Button prevHairColorBtn, nextHairColorBtn;
        public Image eyeColorPreview;
        public Button prevEyeColorBtn, nextEyeColorBtn;
        // Action buttons
        public Button randomizeBtn;
        public Button backBtn;
        public Button confirmBtn;
    }

    static CharacterCreationUIData CreateCharacterCreationPanel(Transform parent)
    {
        CharacterCreationUIData data = new CharacterCreationUIData();

        // Main panel - left side only
        data.panel = CreatePanel(parent, "CharacterCreationPanel", new Color(0.06f, 0.06f, 0.1f, 0.98f));
        RectTransform panelRect = data.panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(0.35f, 1); // Left 35%
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Simple scroll area - just use the panel directly with a viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(data.panel.transform, false);
        RectTransform viewportRt = viewport.AddComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = new Vector2(0, 50); // Leave room for buttons
        viewportRt.offsetMax = Vector2.zero;
        Image viewportImg = viewport.AddComponent<Image>();
        viewportImg.color = new Color(0, 0, 0, 0.01f); // Nearly invisible but raycast-able
        viewportImg.raycastTarget = true; // This is key for scroll events!
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        // Content that will be scrolled
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(-20, 700); // Fixed height, will expand

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(15, 15, 10, 10);

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Add ScrollRect to viewport
        ScrollRect scroll = viewport.AddComponent<ScrollRect>();
        scroll.content = contentRect;
        scroll.viewport = viewportRt;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = false; // No momentum - direct control
        scroll.scrollSensitivity = 30f;

        // ========== CONTENT ==========

        // Title
        CreateLayoutText(content.transform, "Title", "CREATE CHARACTER", 24, 35);
        CreateSpacer(content.transform, 10);

        // ===== BODY SHAPE SECTION =====
        CreateSectionHeader(content.transform, "BODY SHAPE");

        var bodyTypeRow = CreateSliderRow(content.transform, "BodyTypeRow", "Body Type", -100, 100, 0);
        data.bodyTypeSlider = bodyTypeRow.GetComponentInChildren<Slider>();
        data.bodyTypeLabel = CreateLayoutText(content.transform, "BodyTypeLabel", "Neutral", 14, 18).GetComponent<TextMeshProUGUI>();

        var musclesRow = CreateSliderRow(content.transform, "MusclesRow", "Build", 0, 100, 50);
        data.musclesSlider = musclesRow.GetComponentInChildren<Slider>();
        data.musclesLabel = CreateLayoutText(content.transform, "MusclesLabel", "Average", 14, 18).GetComponent<TextMeshProUGUI>();

        var sizeRow = CreateSliderRow(content.transform, "SizeRow", "Size", -100, 100, 0);
        data.bodySizeSlider = sizeRow.GetComponentInChildren<Slider>();
        data.bodySizeLabel = CreateLayoutText(content.transform, "SizeLabel", "Average", 14, 18).GetComponent<TextMeshProUGUI>();

        CreateSpacer(content.transform, 10);

        // ===== FACE PARTS SECTION =====
        CreateSectionHeader(content.transform, "FACE");

        var hairRow = CreateSelectorRow(content.transform, "HairRow", "Hair: 1/20");
        data.prevHairBtn = hairRow.transform.Find("PrevButton").GetComponent<Button>();
        data.nextHairBtn = hairRow.transform.Find("NextButton").GetComponent<Button>();
        data.hairLabel = hairRow.transform.Find("Label").GetComponent<TextMeshProUGUI>();

        var eyebrowsRow = CreateSelectorRow(content.transform, "EyebrowsRow", "Eyebrows: 1/10");
        data.prevEyebrowsBtn = eyebrowsRow.transform.Find("PrevButton").GetComponent<Button>();
        data.nextEyebrowsBtn = eyebrowsRow.transform.Find("NextButton").GetComponent<Button>();
        data.eyebrowsLabel = eyebrowsRow.transform.Find("Label").GetComponent<TextMeshProUGUI>();

        var noseRow = CreateSelectorRow(content.transform, "NoseRow", "Nose: 1/8");
        data.prevNoseBtn = noseRow.transform.Find("PrevButton").GetComponent<Button>();
        data.nextNoseBtn = noseRow.transform.Find("NextButton").GetComponent<Button>();
        data.noseLabel = noseRow.transform.Find("Label").GetComponent<TextMeshProUGUI>();

        var earsRow = CreateSelectorRow(content.transform, "EarsRow", "Ears: 1/6");
        data.prevEarsBtn = earsRow.transform.Find("PrevButton").GetComponent<Button>();
        data.nextEarsBtn = earsRow.transform.Find("NextButton").GetComponent<Button>();
        data.earsLabel = earsRow.transform.Find("Label").GetComponent<TextMeshProUGUI>();

        var facialHairRow = CreateSelectorRowWithClear(content.transform, "FacialHairRow", "Facial Hair: None");
        data.prevFacialHairBtn = facialHairRow.transform.Find("PrevButton").GetComponent<Button>();
        data.nextFacialHairBtn = facialHairRow.transform.Find("NextButton").GetComponent<Button>();
        data.clearFacialHairBtn = facialHairRow.transform.Find("ClearButton").GetComponent<Button>();
        data.facialHairLabel = facialHairRow.transform.Find("Label").GetComponent<TextMeshProUGUI>();

        CreateSpacer(content.transform, 10);

        // ===== COLORS SECTION =====
        CreateSectionHeader(content.transform, "COLORS");

        var skinColorRow = CreateColorRow(content.transform, "SkinColorRow", "Skin", new Color(0.87f, 0.72f, 0.53f));
        data.prevSkinColorBtn = skinColorRow.transform.Find("PrevButton").GetComponent<Button>();
        data.nextSkinColorBtn = skinColorRow.transform.Find("NextButton").GetComponent<Button>();
        data.skinColorPreview = skinColorRow.transform.Find("ColorPreview").GetComponent<Image>();

        var hairColorRow = CreateColorRow(content.transform, "HairColorRow", "Hair", new Color(0.1f, 0.05f, 0.02f));
        data.prevHairColorBtn = hairColorRow.transform.Find("PrevButton").GetComponent<Button>();
        data.nextHairColorBtn = hairColorRow.transform.Find("NextButton").GetComponent<Button>();
        data.hairColorPreview = hairColorRow.transform.Find("ColorPreview").GetComponent<Image>();

        var eyeColorRow = CreateColorRow(content.transform, "EyeColorRow", "Eyes", new Color(0.25f, 0.15f, 0.08f));
        data.prevEyeColorBtn = eyeColorRow.transform.Find("PrevButton").GetComponent<Button>();
        data.nextEyeColorBtn = eyeColorRow.transform.Find("NextButton").GetComponent<Button>();
        data.eyeColorPreview = eyeColorRow.transform.Find("ColorPreview").GetComponent<Image>();

        // ===== BUTTONS (anchored at bottom of panel) =====
        GameObject buttonsRow = new GameObject("Buttons");
        buttonsRow.transform.SetParent(data.panel.transform, false); // Parent to panel, not content
        RectTransform btnRowRect = buttonsRow.AddComponent<RectTransform>();
        btnRowRect.anchorMin = new Vector2(0, 0);
        btnRowRect.anchorMax = new Vector2(1, 0);
        btnRowRect.pivot = new Vector2(0.5f, 0);
        btnRowRect.anchoredPosition = new Vector2(0, 10);
        btnRowRect.sizeDelta = new Vector2(0, 45);

        HorizontalLayoutGroup hlg = buttonsRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.padding = new RectOffset(15, 15, 0, 0);

        data.randomizeBtn = CreateLayoutButton(buttonsRow.transform, "RandomButton", "RANDOM", new Color(0.3f, 0.35f, 0.4f, 1f), 85, 38).GetComponent<Button>();
        data.backBtn = CreateLayoutButton(buttonsRow.transform, "BackButton", "BACK", new Color(0.45f, 0.3f, 0.3f, 1f), 85, 38).GetComponent<Button>();
        data.confirmBtn = CreateLayoutButton(buttonsRow.transform, "StartButton", "START", new Color(0.3f, 0.45f, 0.3f, 1f), 85, 38).GetComponent<Button>();

        return data;
    }

    static void WireUpCharacterCreationUI(CharacterCreationUI charUI, SidekickPlayerController controller,
        GameObject spawnPoint, MainMenuUI mainMenuUI, CharacterCreationUIData data)
    {
        SerializedObject so = new SerializedObject(charUI);

        so.FindProperty("m_CharacterController").objectReferenceValue = controller;
        so.FindProperty("m_CharacterSpawnPoint").objectReferenceValue = spawnPoint.transform;
        so.FindProperty("m_MainMenuUI").objectReferenceValue = mainMenuUI;

        // Body shape
        so.FindProperty("m_BodyTypeSlider").objectReferenceValue = data.bodyTypeSlider;
        so.FindProperty("m_MusclesSlider").objectReferenceValue = data.musclesSlider;
        so.FindProperty("m_BodySizeSlider").objectReferenceValue = data.bodySizeSlider;
        so.FindProperty("m_BodyTypeLabel").objectReferenceValue = data.bodyTypeLabel;
        so.FindProperty("m_MusclesLabel").objectReferenceValue = data.musclesLabel;
        so.FindProperty("m_BodySizeLabel").objectReferenceValue = data.bodySizeLabel;

        // Parts
        so.FindProperty("m_PrevHairButton").objectReferenceValue = data.prevHairBtn;
        so.FindProperty("m_NextHairButton").objectReferenceValue = data.nextHairBtn;
        so.FindProperty("m_HairLabel").objectReferenceValue = data.hairLabel;
        so.FindProperty("m_PrevEyebrowsButton").objectReferenceValue = data.prevEyebrowsBtn;
        so.FindProperty("m_NextEyebrowsButton").objectReferenceValue = data.nextEyebrowsBtn;
        so.FindProperty("m_EyebrowsLabel").objectReferenceValue = data.eyebrowsLabel;
        so.FindProperty("m_PrevNoseButton").objectReferenceValue = data.prevNoseBtn;
        so.FindProperty("m_NextNoseButton").objectReferenceValue = data.nextNoseBtn;
        so.FindProperty("m_NoseLabel").objectReferenceValue = data.noseLabel;
        so.FindProperty("m_PrevEarsButton").objectReferenceValue = data.prevEarsBtn;
        so.FindProperty("m_NextEarsButton").objectReferenceValue = data.nextEarsBtn;
        so.FindProperty("m_EarsLabel").objectReferenceValue = data.earsLabel;
        so.FindProperty("m_PrevFacialHairButton").objectReferenceValue = data.prevFacialHairBtn;
        so.FindProperty("m_NextFacialHairButton").objectReferenceValue = data.nextFacialHairBtn;
        so.FindProperty("m_ClearFacialHairButton").objectReferenceValue = data.clearFacialHairBtn;
        so.FindProperty("m_FacialHairLabel").objectReferenceValue = data.facialHairLabel;

        // Colors
        so.FindProperty("m_SkinColorPreview").objectReferenceValue = data.skinColorPreview;
        so.FindProperty("m_PrevSkinColorButton").objectReferenceValue = data.prevSkinColorBtn;
        so.FindProperty("m_NextSkinColorButton").objectReferenceValue = data.nextSkinColorBtn;
        so.FindProperty("m_HairColorPreview").objectReferenceValue = data.hairColorPreview;
        so.FindProperty("m_PrevHairColorButton").objectReferenceValue = data.prevHairColorBtn;
        so.FindProperty("m_NextHairColorButton").objectReferenceValue = data.nextHairColorBtn;
        so.FindProperty("m_EyeColorPreview").objectReferenceValue = data.eyeColorPreview;
        so.FindProperty("m_PrevEyeColorButton").objectReferenceValue = data.prevEyeColorBtn;
        so.FindProperty("m_NextEyeColorButton").objectReferenceValue = data.nextEyeColorBtn;

        // Action buttons
        so.FindProperty("m_RandomizeButton").objectReferenceValue = data.randomizeBtn;
        so.FindProperty("m_BackButton").objectReferenceValue = data.backBtn;
        so.FindProperty("m_ConfirmButton").objectReferenceValue = data.confirmBtn;

        so.ApplyModifiedProperties();
    }

    // ========== HELPERS ==========

    static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        Image img = obj.AddComponent<Image>();
        img.color = color;
        return obj;
    }

    static GameObject CreateFullScreenPanel(Transform parent, string name, Color color)
    {
        GameObject obj = CreatePanel(parent, name, color);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return obj;
    }

    static GameObject CreateCenteredPanel(Transform parent, string name, Color color, float width, float height)
    {
        GameObject obj = CreatePanel(parent, name, color);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        return obj;
    }

    static GameObject CreateText(Transform parent, string name, string text, int fontSize)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        return obj;
    }

    static GameObject CreateLayoutText(Transform parent, string name, string text, int fontSize, float height)
    {
        GameObject obj = CreateText(parent, name, text, fontSize);
        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        return obj;
    }

    static GameObject CreateButton(Transform parent, string name, string text, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        Image img = obj.AddComponent<Image>();
        img.color = color;
        Button btn = obj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(color.r + 0.15f, color.g + 0.15f, color.b + 0.15f, 1f);
        cb.pressedColor = new Color(color.r - 0.1f, color.g - 0.1f, color.b - 0.1f, 1f);
        btn.colors = cb;

        GameObject textObj = CreateText(obj.transform, "Text", text, 22);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        return obj;
    }

    static GameObject CreateLayoutButton(Transform parent, string name, string text, Color color, float width, float height)
    {
        GameObject obj = CreateButton(parent, name, text, color);
        obj.GetComponentInChildren<TextMeshProUGUI>().fontSize = 14;
        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.preferredHeight = height;
        return obj;
    }

    static void SetAnchors(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
    }

    static void CreateSpacer(Transform parent, float height)
    {
        GameObject obj = new GameObject("Spacer");
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = height;
    }

    static void CreateSectionHeader(Transform parent, string text)
    {
        GameObject header = CreateLayoutText(parent, "Header_" + text, text, 16, 25);
        header.GetComponent<TextMeshProUGUI>().color = new Color(0.7f, 0.75f, 0.8f, 1f);
        header.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
    }

    static GameObject CreateSelectorRow(Transform parent, string name, string labelText)
    {
        GameObject row = new GameObject(name);
        row.transform.SetParent(parent, false);
        row.AddComponent<RectTransform>();
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        LayoutElement rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 35;

        CreateLayoutButton(row.transform, "PrevButton", "<", new Color(0.3f, 0.35f, 0.4f, 1f), 35, 30);
        var label = CreateLayoutText(row.transform, "Label", labelText, 14, 30);
        label.GetComponent<LayoutElement>().preferredWidth = 130;
        CreateLayoutButton(row.transform, "NextButton", ">", new Color(0.3f, 0.35f, 0.4f, 1f), 35, 30);

        return row;
    }

    static GameObject CreateSelectorRowWithClear(Transform parent, string name, string labelText)
    {
        GameObject row = new GameObject(name);
        row.transform.SetParent(parent, false);
        row.AddComponent<RectTransform>();
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 5;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        LayoutElement rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 35;

        CreateLayoutButton(row.transform, "PrevButton", "<", new Color(0.3f, 0.35f, 0.4f, 1f), 30, 28);
        var label = CreateLayoutText(row.transform, "Label", labelText, 13, 28);
        label.GetComponent<LayoutElement>().preferredWidth = 110;
        CreateLayoutButton(row.transform, "NextButton", ">", new Color(0.3f, 0.35f, 0.4f, 1f), 30, 28);
        CreateLayoutButton(row.transform, "ClearButton", "X", new Color(0.45f, 0.3f, 0.3f, 1f), 28, 28);

        return row;
    }

    static GameObject CreateColorRow(Transform parent, string name, string labelText, Color defaultColor)
    {
        GameObject row = new GameObject(name);
        row.transform.SetParent(parent, false);
        row.AddComponent<RectTransform>();
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        LayoutElement rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 35;

        // Label
        var label = CreateLayoutText(row.transform, "RowLabel", labelText + ":", 14, 30);
        label.GetComponent<LayoutElement>().preferredWidth = 50;
        label.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;

        CreateLayoutButton(row.transform, "PrevButton", "<", new Color(0.3f, 0.35f, 0.4f, 1f), 30, 28);

        // Color preview
        GameObject colorPreview = new GameObject("ColorPreview");
        colorPreview.transform.SetParent(row.transform, false);
        colorPreview.AddComponent<RectTransform>();
        Image previewImg = colorPreview.AddComponent<Image>();
        previewImg.color = defaultColor;
        LayoutElement previewLE = colorPreview.AddComponent<LayoutElement>();
        previewLE.preferredWidth = 60;
        previewLE.preferredHeight = 28;

        CreateLayoutButton(row.transform, "NextButton", ">", new Color(0.3f, 0.35f, 0.4f, 1f), 30, 28);

        return row;
    }

    static GameObject CreateSliderRow(Transform parent, string name, string labelText, float min, float max, float defaultVal)
    {
        // Row container
        GameObject row = new GameObject(name);
        row.transform.SetParent(parent, false);
        RectTransform rowRt = row.AddComponent<RectTransform>();
        LayoutElement rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 28;

        // Label on the left (small width)
        GameObject label = CreateText(row.transform, "Label", labelText, 11);
        RectTransform labelRt = label.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0, 0);
        labelRt.anchorMax = new Vector2(0, 1);
        labelRt.pivot = new Vector2(0, 0.5f);
        labelRt.anchoredPosition = new Vector2(0, 0);
        labelRt.sizeDelta = new Vector2(55, 0);
        label.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;

        // Slider takes remaining space
        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(row.transform, false);
        RectTransform sliderRt = sliderObj.AddComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0, 0);
        sliderRt.anchorMax = new Vector2(1, 1);
        sliderRt.offsetMin = new Vector2(60, 4); // Start after label
        sliderRt.offsetMax = new Vector2(-5, -4);

        // Thin track line (background) - full width
        GameObject track = new GameObject("Track");
        track.transform.SetParent(sliderObj.transform, false);
        RectTransform trackRt = track.AddComponent<RectTransform>();
        trackRt.anchorMin = new Vector2(0, 0.5f);
        trackRt.anchorMax = new Vector2(1, 0.5f);
        trackRt.offsetMin = new Vector2(6, -1);
        trackRt.offsetMax = new Vector2(-6, 1);
        Image trackImg = track.AddComponent<Image>();
        trackImg.color = new Color(0.3f, 0.3f, 0.35f, 1f);

        // Fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0, 0.5f);
        fillAreaRt.anchorMax = new Vector2(1, 0.5f);
        fillAreaRt.offsetMin = new Vector2(6, -1);
        fillAreaRt.offsetMax = new Vector2(-6, 1);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0, 0);
        fillRt.anchorMax = new Vector2(0, 1);
        fillRt.pivot = new Vector2(0, 0.5f);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.4f, 0.75f, 0.5f, 1f);

        // Handle slide area
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRt = handleArea.AddComponent<RectTransform>();
        handleAreaRt.anchorMin = new Vector2(0, 0.5f);
        handleAreaRt.anchorMax = new Vector2(1, 0.5f);
        handleAreaRt.offsetMin = new Vector2(6, -10);
        handleAreaRt.offsetMax = new Vector2(-6, 10);

        // Small circular handle - FIXED size, not stretching
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRt = handle.AddComponent<RectTransform>();
        handleRt.anchorMin = new Vector2(0, 0.5f);
        handleRt.anchorMax = new Vector2(0, 0.5f);
        handleRt.pivot = new Vector2(0.5f, 0.5f);
        handleRt.sizeDelta = new Vector2(14, 14); // Fixed small circle size
        handleRt.anchoredPosition = Vector2.zero;
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;

        // Slider component
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = defaultVal;
        slider.targetGraphic = handleImg;

        return row;
    }
}
