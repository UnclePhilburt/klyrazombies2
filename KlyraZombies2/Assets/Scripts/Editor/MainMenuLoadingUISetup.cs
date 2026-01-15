using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Editor tool to add loading UI to MainMenu scene.
/// Menu: Project Klyra > UI > Add Loading UI to MainMenu
/// </summary>
public class MainMenuLoadingUISetup : Editor
{
    [MenuItem("Project Klyra/UI/Add Loading UI to MainMenu")]
    public static void AddLoadingUI()
    {
        // Find MainMenuUI in scene
        MainMenuUI mainMenuUI = FindObjectOfType<MainMenuUI>();
        if (mainMenuUI == null)
        {
            EditorUtility.DisplayDialog("Error",
                "MainMenuUI not found in current scene!\n\nOpen your MainMenu scene first.",
                "OK");
            return;
        }

        // Find the Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No Canvas found in scene!",
                "OK");
            return;
        }

        // Check if loading panel already exists
        Transform existingPanel = canvas.transform.Find("LoadingPanel");
        if (existingPanel != null)
        {
            EditorUtility.DisplayDialog("Already Exists",
                "LoadingPanel already exists in the scene.\n\nDelete it first if you want to recreate it.",
                "OK");
            return;
        }

        // Create Loading Panel
        GameObject loadingPanel = new GameObject("LoadingPanel");
        loadingPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = loadingPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;

        // Add dark background
        Image panelImage = loadingPanel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // Create container for content (centered)
        GameObject container = new GameObject("Container");
        container.transform.SetParent(loadingPanel.transform, false);

        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(500, 150);
        containerRect.anchoredPosition = Vector2.zero;

        // Create Title Text
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(container.transform, false);

        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.7f);
        titleRect.anchorMax = new Vector2(1, 1f);
        titleRect.sizeDelta = Vector2.zero;
        titleRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "LOADING";
        titleText.fontSize = 36;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;

        // Create Loading Status Text
        GameObject statusObj = new GameObject("LoadingText");
        statusObj.transform.SetParent(container.transform, false);

        RectTransform statusRect = statusObj.AddComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0, 0.35f);
        statusRect.anchorMax = new Vector2(1, 0.65f);
        statusRect.sizeDelta = Vector2.zero;
        statusRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.text = "Connecting to server...";
        statusText.fontSize = 20;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.color = new Color(0.8f, 0.8f, 0.8f, 1f);

        // Create Progress Bar Background
        GameObject progressBg = new GameObject("ProgressBarBackground");
        progressBg.transform.SetParent(container.transform, false);

        RectTransform progressBgRect = progressBg.AddComponent<RectTransform>();
        progressBgRect.anchorMin = new Vector2(0.1f, 0.1f);
        progressBgRect.anchorMax = new Vector2(0.9f, 0.25f);
        progressBgRect.sizeDelta = Vector2.zero;
        progressBgRect.anchoredPosition = Vector2.zero;

        Image progressBgImage = progressBg.AddComponent<Image>();
        progressBgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        // Create Slider (Progress Bar)
        GameObject sliderObj = new GameObject("ProgressBar");
        sliderObj.transform.SetParent(container.transform, false);

        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.1f, 0.1f);
        sliderRect.anchorMax = new Vector2(0.9f, 0.25f);
        sliderRect.sizeDelta = Vector2.zero;
        sliderRect.anchoredPosition = Vector2.zero;

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 0;
        slider.interactable = false;

        // Create Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);

        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;
        fillAreaRect.anchoredPosition = Vector2.zero;

        // Create Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);

        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;

        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.3f, 0.7f, 1f, 1f); // Nice blue color

        slider.fillRect = fillRect;

        // Start panel hidden
        loadingPanel.SetActive(false);

        // Wire up references to MainMenuUI
        SerializedObject serializedUI = new SerializedObject(mainMenuUI);

        SerializedProperty loadingPanelProp = serializedUI.FindProperty("m_LoadingPanel");
        SerializedProperty loadingTextProp = serializedUI.FindProperty("m_LoadingText");
        SerializedProperty progressBarProp = serializedUI.FindProperty("m_LoadingProgressBar");

        if (loadingPanelProp != null)
            loadingPanelProp.objectReferenceValue = loadingPanel;
        if (loadingTextProp != null)
            loadingTextProp.objectReferenceValue = statusText;
        if (progressBarProp != null)
            progressBarProp.objectReferenceValue = slider;

        serializedUI.ApplyModifiedProperties();

        // Mark scene dirty
        EditorUtility.SetDirty(mainMenuUI);
        EditorSceneManager.MarkSceneDirty(mainMenuUI.gameObject.scene);

        Debug.Log("[MainMenuLoadingUISetup] Loading UI created and wired up!");

        EditorUtility.DisplayDialog("Success",
            "Loading UI added to MainMenu!\n\n" +
            "Created:\n" +
            "- LoadingPanel (dark overlay)\n" +
            "- Loading text\n" +
            "- Progress bar\n\n" +
            "References have been auto-wired to MainMenuUI.\n\n" +
            "Don't forget to SAVE the scene!",
            "OK");

        // Select the new panel
        Selection.activeGameObject = loadingPanel;
    }
}
