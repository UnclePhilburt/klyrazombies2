using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// Simple editor tool to fix Canvas for main menu with 3D background.
/// Menu: Project Klyra > Fix Main Menu Canvas
/// </summary>
public class MainMenuCanvasFixer : EditorWindow
{
    private Canvas m_Canvas;
    private float m_BackgroundDarken = 0.4f;
    private bool m_AddDarkenOverlay = true;

    [MenuItem("Project Klyra/Fix Main Menu Canvas")]
    public static void ShowWindow()
    {
        GetWindow<MainMenuCanvasFixer>("Fix Menu Canvas");
    }

    private void OnEnable()
    {
        m_Canvas = FindFirstObjectByType<Canvas>();
    }

    private void OnGUI()
    {
        GUILayout.Label("Fix Main Menu Canvas", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        m_Canvas = (Canvas)EditorGUILayout.ObjectField("Canvas", m_Canvas, typeof(Canvas), true);

        EditorGUILayout.Space();

        m_AddDarkenOverlay = EditorGUILayout.Toggle("Add Background Darkening", m_AddDarkenOverlay);
        if (m_AddDarkenOverlay)
        {
            m_BackgroundDarken = EditorGUILayout.Slider("Darken Amount", m_BackgroundDarken, 0f, 0.8f);
        }

        EditorGUILayout.Space(20);

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Fix Canvas (Screen Space Overlay)", GUILayout.Height(40)))
        {
            FixCanvas();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "This will:\n" +
            "1. Set Canvas to Screen Space - Overlay\n" +
            "2. Add CanvasScaler for responsive UI\n" +
            "3. Optionally add a dark overlay for readability\n\n" +
            "Position your camera manually in the Scene view.",
            MessageType.Info);
    }

    private void FixCanvas()
    {
        if (m_Canvas == null)
        {
            m_Canvas = FindFirstObjectByType<Canvas>();
            if (m_Canvas == null)
            {
                Debug.LogError("[MainMenuCanvasFixer] No Canvas found in scene!");
                return;
            }
        }

        Undo.RecordObject(m_Canvas, "Fix Canvas");

        // Set to Screen Space Overlay - UI renders on top of 3D scene
        m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        m_Canvas.sortingOrder = 100;

        // Setup CanvasScaler for responsive UI
        var scaler = m_Canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = Undo.AddComponent<CanvasScaler>(m_Canvas.gameObject);

        Undo.RecordObject(scaler, "Fix CanvasScaler");
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Add GraphicRaycaster if missing
        if (m_Canvas.GetComponent<GraphicRaycaster>() == null)
            Undo.AddComponent<GraphicRaycaster>(m_Canvas.gameObject);

        // Add darkening overlay
        if (m_AddDarkenOverlay)
        {
            AddDarkenOverlay();
        }

        EditorUtility.SetDirty(m_Canvas);
        Debug.Log("[MainMenuCanvasFixer] Canvas fixed! UI will now overlay your 3D scene.");
    }

    private void AddDarkenOverlay()
    {
        // Check if overlay already exists
        Transform existingOverlay = m_Canvas.transform.Find("BackgroundDarken");
        if (existingOverlay != null)
        {
            var img = existingOverlay.GetComponent<Image>();
            if (img != null)
            {
                Undo.RecordObject(img, "Update Darken");
                img.color = new Color(0, 0, 0, m_BackgroundDarken);
                Debug.Log("[MainMenuCanvasFixer] Updated existing darkening overlay");
            }
            return;
        }

        // Create new overlay
        GameObject overlay = new GameObject("BackgroundDarken");
        Undo.RegisterCreatedObjectUndo(overlay, "Create Darken Overlay");

        overlay.transform.SetParent(m_Canvas.transform, false);
        overlay.transform.SetAsFirstSibling(); // Behind everything else

        var rect = overlay.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = overlay.AddComponent<Image>();
        image.color = new Color(0, 0, 0, m_BackgroundDarken);
        image.raycastTarget = false; // Don't block clicks

        Debug.Log("[MainMenuCanvasFixer] Added background darkening overlay");
    }
}
