using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Displays a persistent row of control hints along the bottom of the screen.
/// Clean, minimal, always visible but unobtrusive.
/// </summary>
public class ControlsHintBar : MonoBehaviour
{
    public static ControlsHintBar Instance { get; private set; }

    /// <summary>
    /// Auto-creates the controls bar when the game starts.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        // Don't create in main menu or other non-gameplay scenes
        if (Instance != null) return;

        // Check if there's a player in the scene (indicates gameplay scene)
        if (GameObject.FindWithTag("Player") != null || FindFirstObjectByType<CharacterSpawner>() != null)
        {
            GameObject barObj = new GameObject("ControlsHintBar");
            barObj.AddComponent<ControlsHintBar>();
        }
    }

    [Header("Settings")]
    [SerializeField] private float m_BottomOffset = 5f;
    [SerializeField] private float m_Opacity = 0.35f;
    [SerializeField] private float m_FontSize = 11f;
    [SerializeField] private float m_Spacing = 3f;

    [Header("Controls to Display")]
    [SerializeField] private List<ControlHint> m_Controls = new List<ControlHint>()
    {
        new ControlHint("WASD", "Move"),
        new ControlHint("LMB", "Fire"),
        new ControlHint("RMB", "Aim"),
        new ControlHint("R", "Reload"),
        new ControlHint("1-3", "Weapons"),
        new ControlHint("T", "Holster"),
        new ControlHint("TAB", "Inventory"),
        new ControlHint("E", "Interact"),
        new ControlHint("F", "Flashlight"),
        new ControlHint("SHIFT", "Sprint"),
        new ControlHint("C", "Crouch")
    };

    private GameObject m_BarObject;
    private CanvasGroup m_CanvasGroup;

    [System.Serializable]
    public class ControlHint
    {
        public string key;
        public string action;

        public ControlHint(string key, string action)
        {
            this.key = key;
            this.action = action;
        }
    }

    private void Awake()
    {
        Instance = this;
        CreateUI();
    }

    private void CreateUI()
    {
        // Create our own canvas with constant pixel size (no scaling)
        GameObject canvasObj = new GameObject("ControlsHintCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // On top of other UI

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Create bar container - anchored to bottom center
        m_BarObject = new GameObject("ControlsHintBar");
        m_BarObject.transform.SetParent(canvas.transform, false);

        RectTransform barRect = m_BarObject.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 0f);
        barRect.anchorMax = new Vector2(0.5f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        barRect.anchoredPosition = new Vector2(0f, m_BottomOffset);

        // Canvas group for opacity
        m_CanvasGroup = m_BarObject.AddComponent<CanvasGroup>();
        m_CanvasGroup.alpha = m_Opacity;
        m_CanvasGroup.interactable = false;
        m_CanvasGroup.blocksRaycasts = false;

        // Build single text string with all controls
        string controlsText = "";
        for (int i = 0; i < m_Controls.Count; i++)
        {
            controlsText += $"<b>{m_Controls[i].key}</b> {m_Controls[i].action}";
            if (i < m_Controls.Count - 1)
                controlsText += "   ";
        }

        // Single text element
        TextMeshProUGUI tmp = m_BarObject.AddComponent<TextMeshProUGUI>();
        tmp.text = controlsText;
        tmp.fontSize = m_FontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;

        barRect.sizeDelta = new Vector2(Screen.width - 40f, 20f);
    }

    /// <summary>
    /// Show or hide the controls bar.
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (m_BarObject != null)
            m_BarObject.SetActive(visible);
    }

    /// <summary>
    /// Set the opacity of the controls bar (0-1).
    /// </summary>
    public void SetOpacity(float opacity)
    {
        m_Opacity = Mathf.Clamp01(opacity);
        if (m_CanvasGroup != null)
            m_CanvasGroup.alpha = m_Opacity;
    }

}
