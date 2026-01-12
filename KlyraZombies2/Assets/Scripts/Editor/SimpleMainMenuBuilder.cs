using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// One-click setup for main menu on existing scene.
/// Menu: Project Klyra > Build Simple Main Menu
/// </summary>
public class SimpleMainMenuBuilder : EditorWindow
{
    [MenuItem("Project Klyra/Build Simple Main Menu")]
    public static void BuildMenu()
    {
        // Check for existing camera
        Camera cam = Camera.main;
        if (cam == null)
            cam = FindFirstObjectByType<Camera>();

        if (cam == null)
        {
            // Create camera
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            camObj.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.Skybox;
            Debug.Log("[SimpleMainMenuBuilder] Created Main Camera");
        }

        // Check for EventSystem
        var eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();

            // Try new Input System first
            var inputModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null)
                esObj.AddComponent(inputModuleType);
            else
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            Debug.Log("[SimpleMainMenuBuilder] Created EventSystem");
        }

        // Create Canvas
        GameObject canvasObj = new GameObject("MainMenuCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // ========== MAIN PANEL ==========
        GameObject mainPanel = CreatePanel(canvasObj.transform, "MainPanel");

        // Title
        GameObject title = CreateText(mainPanel.transform, "Title", "ZOMBIE SURVIVAL", 72, FontStyles.Bold);
        RectTransform titleRt = title.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.75f);
        titleRt.anchorMax = new Vector2(0.5f, 0.75f);
        titleRt.sizeDelta = new Vector2(800, 100);

        // Play Button
        GameObject playBtn = CreateButton(mainPanel.transform, "PlayButton", "PLAY", new Color(0.2f, 0.5f, 0.3f));
        PositionButton(playBtn, 0.5f, 0.45f, 280, 65);

        // Settings Button
        GameObject settingsBtn = CreateButton(mainPanel.transform, "SettingsButton", "SETTINGS", new Color(0.25f, 0.25f, 0.35f));
        PositionButton(settingsBtn, 0.5f, 0.32f, 280, 65);

        // Quit Button
        GameObject quitBtn = CreateButton(mainPanel.transform, "QuitButton", "QUIT", new Color(0.5f, 0.25f, 0.25f));
        PositionButton(quitBtn, 0.5f, 0.19f, 280, 65);

        // ========== SETTINGS PANEL ==========
        GameObject settingsPanel = CreatePanel(canvasObj.transform, "SettingsPanel");
        settingsPanel.SetActive(false);

        // Settings background
        GameObject settingsBg = new GameObject("Background");
        settingsBg.transform.SetParent(settingsPanel.transform, false);
        RectTransform bgRt = settingsBg.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0.5f, 0.5f);
        bgRt.anchorMax = new Vector2(0.5f, 0.5f);
        bgRt.sizeDelta = new Vector2(500, 450);
        Image bgImg = settingsBg.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        // Settings Title
        GameObject settingsTitle = CreateText(settingsPanel.transform, "SettingsTitle", "SETTINGS", 42, FontStyles.Bold);
        RectTransform stRt = settingsTitle.GetComponent<RectTransform>();
        stRt.anchorMin = new Vector2(0.5f, 0.78f);
        stRt.anchorMax = new Vector2(0.5f, 0.78f);
        stRt.sizeDelta = new Vector2(400, 60);

        // Master Volume
        GameObject masterLabel = CreateText(settingsPanel.transform, "MasterLabel", "Master Volume", 20, FontStyles.Normal);
        PositionElement(masterLabel, 0.5f, 0.65f, 200, 30);
        GameObject masterSlider = CreateSlider(settingsPanel.transform, "MasterVolumeSlider");
        PositionElement(masterSlider, 0.5f, 0.60f, 300, 25);

        // SFX Volume
        GameObject sfxLabel = CreateText(settingsPanel.transform, "SFXLabel", "SFX Volume", 20, FontStyles.Normal);
        PositionElement(sfxLabel, 0.5f, 0.52f, 200, 30);
        GameObject sfxSlider = CreateSlider(settingsPanel.transform, "SFXVolumeSlider");
        PositionElement(sfxSlider, 0.5f, 0.47f, 300, 25);

        // Music Volume
        GameObject musicLabel = CreateText(settingsPanel.transform, "MusicLabel", "Music Volume", 20, FontStyles.Normal);
        PositionElement(musicLabel, 0.5f, 0.39f, 200, 30);
        GameObject musicSlider = CreateSlider(settingsPanel.transform, "MusicVolumeSlider");
        PositionElement(musicSlider, 0.5f, 0.34f, 300, 25);

        // Back Button
        GameObject backBtn = CreateButton(settingsPanel.transform, "BackButton", "BACK", new Color(0.4f, 0.3f, 0.3f));
        PositionButton(backBtn, 0.5f, 0.18f, 180, 50);

        // ========== MAIN MENU CONTROLLER ==========
        GameObject controllerObj = new GameObject("MainMenuController");
        MainMenuUI menuUI = controllerObj.AddComponent<MainMenuUI>();

        // Wire up references using SerializedObject
        SerializedObject so = new SerializedObject(menuUI);
        so.FindProperty("m_MainPanel").objectReferenceValue = mainPanel;
        so.FindProperty("m_SettingsPanel").objectReferenceValue = settingsPanel;
        so.FindProperty("m_PlayButton").objectReferenceValue = playBtn.GetComponent<Button>();
        so.FindProperty("m_SettingsButton").objectReferenceValue = settingsBtn.GetComponent<Button>();
        so.FindProperty("m_QuitButton").objectReferenceValue = quitBtn.GetComponent<Button>();
        so.FindProperty("m_BackButton").objectReferenceValue = backBtn.GetComponent<Button>();
        so.FindProperty("m_MasterVolumeSlider").objectReferenceValue = masterSlider.GetComponent<Slider>();
        so.FindProperty("m_SFXVolumeSlider").objectReferenceValue = sfxSlider.GetComponent<Slider>();
        so.FindProperty("m_MusicVolumeSlider").objectReferenceValue = musicSlider.GetComponent<Slider>();
        so.ApplyModifiedProperties();

        // Mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[SimpleMainMenuBuilder] Main Menu built successfully!");
        Debug.Log("[SimpleMainMenuBuilder] Position your camera to look at the scene, then save.");

        // Select the camera so user can position it
        Selection.activeGameObject = cam.gameObject;
    }

    static GameObject CreatePanel(Transform parent, string name)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return obj;
    }

    static GameObject CreateText(Transform parent, string name, string text, int fontSize, FontStyles style)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
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

        // Button text
        GameObject textObj = CreateText(obj.transform, "Text", text, 28, FontStyles.Bold);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        return obj;
    }

    static GameObject CreateSlider(Transform parent, string name)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();

        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(obj.transform, false);
        RectTransform bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.25f, 1f);

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(obj.transform, false);
        RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.offsetMin = new Vector2(5, 5);
        fillAreaRt.offsetMax = new Vector2(-5, -5);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(0, 1);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.3f, 0.6f, 0.4f, 1f);

        // Handle Area
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(obj.transform, false);
        RectTransform handleAreaRt = handleArea.AddComponent<RectTransform>();
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.offsetMin = new Vector2(10, 0);
        handleAreaRt.offsetMax = new Vector2(-10, 0);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRt = handle.AddComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(20, 0);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;

        // Slider component
        Slider slider = obj.AddComponent<Slider>();
        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.targetGraphic = handleImg;

        return obj;
    }

    static void PositionButton(GameObject obj, float anchorX, float anchorY, float width, float height)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(anchorX, anchorY);
        rt.anchorMax = new Vector2(anchorX, anchorY);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = Vector2.zero;
    }

    static void PositionElement(GameObject obj, float anchorX, float anchorY, float width, float height)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(anchorX, anchorY);
        rt.anchorMax = new Vector2(anchorX, anchorY);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = Vector2.zero;
    }
}
