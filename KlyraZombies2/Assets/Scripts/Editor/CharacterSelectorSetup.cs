using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Editor script to set up the Character Selector UI in the MainMenu scene.
/// Run from Project Klyra > Setup Character Selector
/// </summary>
public class CharacterSelectorSetup : EditorWindow
{
    [MenuItem("Project Klyra/Setup Character Selector")]
    public static void SetupCharacterSelector()
    {
        // Find MainMenuUI in scene
        MainMenuUI mainMenuUI = Object.FindObjectOfType<MainMenuUI>();
        if (mainMenuUI == null)
        {
            EditorUtility.DisplayDialog("Error", "MainMenuUI not found in scene. Make sure you have the MainMenu scene open.", "OK");
            return;
        }

        // Find Canvas
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "Canvas not found in scene.", "OK");
            return;
        }

        // Find MainPanel
        Transform mainPanel = null;
        var images = Object.FindObjectsOfType<Image>();
        foreach (var img in images)
        {
            if (img.gameObject.name == "MainPanel")
            {
                mainPanel = img.transform;
                break;
            }
        }

        if (mainPanel == null)
        {
            EditorUtility.DisplayDialog("Error", "MainPanel not found in Canvas.", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Setup Character Selector");

        // Create Character Preview Point (3D space, not UI)
        GameObject previewPoint = new GameObject("CharacterPreviewPoint");
        previewPoint.transform.position = new Vector3(2f, 0f, 0f); // Position to the right of center
        previewPoint.transform.rotation = Quaternion.Euler(0, 180, 0); // Face the camera
        Undo.RegisterCreatedObjectUndo(previewPoint, "Create Preview Point");

        // Create CharacterSelector container in MainPanel
        GameObject selectorContainer = new GameObject("CharacterSelector");
        selectorContainer.transform.SetParent(mainPanel, false);

        RectTransform selectorRect = selectorContainer.AddComponent<RectTransform>();
        selectorRect.anchorMin = new Vector2(0.5f, 0);
        selectorRect.anchorMax = new Vector2(0.5f, 0);
        selectorRect.pivot = new Vector2(0.5f, 0);
        selectorRect.anchoredPosition = new Vector2(200, 100); // Bottom right area
        selectorRect.sizeDelta = new Vector2(300, 150);

        // Add CharacterSelectorUI component
        CharacterSelectorUI selectorUI = selectorContainer.AddComponent<CharacterSelectorUI>();

        // Create Character Name Text
        GameObject nameTextObj = new GameObject("CharacterNameText");
        nameTextObj.transform.SetParent(selectorContainer.transform, false);

        RectTransform nameRect = nameTextObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 1);
        nameRect.anchorMax = new Vector2(1, 1);
        nameRect.pivot = new Vector2(0.5f, 1);
        nameRect.anchoredPosition = new Vector2(0, 0);
        nameRect.sizeDelta = new Vector2(0, 40);

        TextMeshProUGUI nameText = nameTextObj.AddComponent<TextMeshProUGUI>();
        nameText.text = "Character Name";
        nameText.fontSize = 24;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = Color.white;

        // Create Character Count Text
        GameObject countTextObj = new GameObject("CharacterCountText");
        countTextObj.transform.SetParent(selectorContainer.transform, false);

        RectTransform countRect = countTextObj.AddComponent<RectTransform>();
        countRect.anchorMin = new Vector2(0, 1);
        countRect.anchorMax = new Vector2(1, 1);
        countRect.pivot = new Vector2(0.5f, 1);
        countRect.anchoredPosition = new Vector2(0, -40);
        countRect.sizeDelta = new Vector2(0, 30);

        TextMeshProUGUI countText = countTextObj.AddComponent<TextMeshProUGUI>();
        countText.text = "1 / 10";
        countText.fontSize = 18;
        countText.alignment = TextAlignmentOptions.Center;
        countText.color = new Color(0.7f, 0.7f, 0.7f);

        // Create button container
        GameObject buttonContainer = new GameObject("Buttons");
        buttonContainer.transform.SetParent(selectorContainer.transform, false);

        RectTransform buttonContainerRect = buttonContainer.AddComponent<RectTransform>();
        buttonContainerRect.anchorMin = new Vector2(0, 0);
        buttonContainerRect.anchorMax = new Vector2(1, 0);
        buttonContainerRect.pivot = new Vector2(0.5f, 0);
        buttonContainerRect.anchoredPosition = new Vector2(0, 20);
        buttonContainerRect.sizeDelta = new Vector2(0, 50);

        HorizontalLayoutGroup layout = buttonContainer.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 20;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        // Create Prev Button
        GameObject prevButtonObj = CreateButton("PrevButton", "< Prev", buttonContainer.transform);
        Button prevButton = prevButtonObj.GetComponent<Button>();

        // Create Next Button
        GameObject nextButtonObj = CreateButton("NextButton", "Next >", buttonContainer.transform);
        Button nextButton = nextButtonObj.GetComponent<Button>();

        // Wire up CharacterSelectorUI
        SerializedObject selectorSO = new SerializedObject(selectorUI);
        selectorSO.FindProperty("m_PreviewPoint").objectReferenceValue = previewPoint.transform;
        selectorSO.FindProperty("m_PrevButton").objectReferenceValue = prevButton;
        selectorSO.FindProperty("m_NextButton").objectReferenceValue = nextButton;
        selectorSO.FindProperty("m_CharacterNameText").objectReferenceValue = nameText;
        selectorSO.FindProperty("m_CharacterCountText").objectReferenceValue = countText;
        selectorSO.FindProperty("m_RotationSpeed").floatValue = 30f;
        selectorSO.FindProperty("m_PreviewScale").floatValue = 1f;
        selectorSO.ApplyModifiedProperties();

        // Wire up MainMenuUI
        SerializedObject mainMenuSO = new SerializedObject(mainMenuUI);
        mainMenuSO.FindProperty("m_CharacterSelector").objectReferenceValue = selectorUI;
        mainMenuSO.FindProperty("m_PrevCharacterButton").objectReferenceValue = prevButton;
        mainMenuSO.FindProperty("m_NextCharacterButton").objectReferenceValue = nextButton;
        mainMenuSO.ApplyModifiedProperties();

        // Mark scene dirty
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[CharacterSelectorSetup] Character selector UI created successfully!");
        Debug.Log("  - CharacterPreviewPoint created at (2, 0, 0)");
        Debug.Log("  - CharacterSelector UI added to MainPanel");
        Debug.Log("  - MainMenuUI wired up with character selector references");
        Debug.Log("");
        Debug.Log("Next steps:");
        Debug.Log("1. Run 'Project Klyra > Populate Character Database' to add characters");
        Debug.Log("2. Adjust CharacterPreviewPoint position to match your camera angle");
        Debug.Log("3. Save the scene");

        EditorUtility.DisplayDialog("Success",
            "Character selector UI created!\n\n" +
            "Next steps:\n" +
            "1. Run 'Project Klyra > Populate Character Database'\n" +
            "2. Adjust CharacterPreviewPoint position\n" +
            "3. Save the scene", "OK");
    }

    private static GameObject CreateButton(string name, string text, Transform parent)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(100, 40);

        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.3f);

        Button button = buttonObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.3f);
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.5f);
        colors.pressedColor = new Color(0.15f, 0.15f, 0.25f);
        button.colors = colors;

        // Add text child
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = text;
        buttonText.fontSize = 18;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;

        return buttonObj;
    }
}
