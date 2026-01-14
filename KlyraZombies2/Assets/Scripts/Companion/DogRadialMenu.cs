using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Radial menu for dog commands. Hold Q to open, move mouse to select, release to execute.
/// </summary>
public class DogRadialMenu : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode m_MenuKey = KeyCode.Q;

    [Header("UI References")]
    [SerializeField] private Canvas m_Canvas;
    [SerializeField] private RectTransform m_MenuPanel;
    [SerializeField] private Image m_BackgroundImage;
    [SerializeField] private Image m_SelectionIndicator;

    [Header("Menu Options")]
    [SerializeField] private RectTransform[] m_OptionButtons;
    [SerializeField] private Image[] m_OptionIcons;
    [SerializeField] private Text[] m_OptionLabels;

    [Header("Settings")]
    [SerializeField] private float m_MenuRadius = 100f;
    [SerializeField] private float m_DeadZone = 30f;
    [SerializeField] private Color m_NormalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color m_HighlightColor = new Color(0.8f, 0.6f, 0.2f, 0.9f);
    [SerializeField] private Color m_DisabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    public enum DogCommand
    {
        None = -1,
        Attack = 0,
        StayFollow = 1,
        Come = 2,
        Search = 3
    }

    private readonly string[] m_CommandNames = { "Attack", "Stay/Follow", "Come", "Search" };
    private readonly string[] m_CommandDescriptions = {
        "Attack target",
        "Toggle stay/follow",
        "Return to me",
        "Find nearby loot"
    };

    private bool m_IsOpen;
    private int m_SelectedOption = -1;
    private Vector2 m_MenuCenter;
    private bool m_WasBuilt;

    // Singleton
    public static DogRadialMenu Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (m_Canvas == null)
        {
            BuildUI();
        }

        CloseMenu();
    }

    private void Update()
    {
        // Check if dog exists
        if (DogCompanion.Instance == null)
        {
            if (m_IsOpen) CloseMenu();
            return;
        }

        // Handle menu input
        if (Input.GetKeyDown(m_MenuKey))
        {
            OpenMenu();
        }
        else if (Input.GetKeyUp(m_MenuKey))
        {
            ExecuteSelection();
            CloseMenu();
        }

        if (m_IsOpen)
        {
            UpdateSelection();
        }
    }

    private void BuildUI()
    {
        if (m_WasBuilt) return;
        m_WasBuilt = true;

        // Create Canvas
        GameObject canvasObj = new GameObject("DogRadialMenuCanvas");
        canvasObj.transform.SetParent(transform);
        m_Canvas = canvasObj.AddComponent<Canvas>();
        m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        m_Canvas.sortingOrder = 100;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // Create menu panel (center of screen)
        GameObject panelObj = new GameObject("MenuPanel");
        panelObj.transform.SetParent(m_Canvas.transform);
        m_MenuPanel = panelObj.AddComponent<RectTransform>();
        m_MenuPanel.anchoredPosition = Vector2.zero;
        m_MenuPanel.sizeDelta = new Vector2(m_MenuRadius * 3f, m_MenuRadius * 3f);

        // Create background circle
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(m_MenuPanel);
        m_BackgroundImage = bgObj.AddComponent<Image>();
        m_BackgroundImage.color = new Color(0, 0, 0, 0.7f);
        var bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.sizeDelta = new Vector2(m_MenuRadius * 2.5f, m_MenuRadius * 2.5f);

        // Create center text
        GameObject centerObj = new GameObject("CenterText");
        centerObj.transform.SetParent(m_MenuPanel);
        var centerText = centerObj.AddComponent<Text>();
        centerText.text = "DOG";
        centerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        centerText.fontSize = 24;
        centerText.fontStyle = FontStyle.Bold;
        centerText.color = Color.white;
        centerText.alignment = TextAnchor.MiddleCenter;
        var centerRect = centerObj.GetComponent<RectTransform>();
        centerRect.anchoredPosition = Vector2.zero;
        centerRect.sizeDelta = new Vector2(100, 40);

        // Create 4 option buttons around the circle
        m_OptionButtons = new RectTransform[4];
        m_OptionIcons = new Image[4];
        m_OptionLabels = new Text[4];

        float[] angles = { 90f, 0f, 270f, 180f }; // Top, Right, Bottom, Left

        for (int i = 0; i < 4; i++)
        {
            float angle = angles[i] * Mathf.Deg2Rad;
            Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * m_MenuRadius;

            // Button container
            GameObject btnObj = new GameObject($"Option_{m_CommandNames[i]}");
            btnObj.transform.SetParent(m_MenuPanel);
            m_OptionButtons[i] = btnObj.AddComponent<RectTransform>();
            m_OptionButtons[i].anchoredPosition = pos;
            m_OptionButtons[i].sizeDelta = new Vector2(80, 80);

            // Icon background
            GameObject iconBg = new GameObject("IconBg");
            iconBg.transform.SetParent(m_OptionButtons[i]);
            m_OptionIcons[i] = iconBg.AddComponent<Image>();
            m_OptionIcons[i].color = m_NormalColor;
            var iconRect = iconBg.GetComponent<RectTransform>();
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(60, 60);

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(m_OptionButtons[i]);
            m_OptionLabels[i] = labelObj.AddComponent<Text>();
            m_OptionLabels[i].text = m_CommandNames[i];
            m_OptionLabels[i].font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            m_OptionLabels[i].fontSize = 14;
            m_OptionLabels[i].fontStyle = FontStyle.Bold;
            m_OptionLabels[i].color = Color.white;
            m_OptionLabels[i].alignment = TextAnchor.MiddleCenter;
            var labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchoredPosition = new Vector2(0, -45);
            labelRect.sizeDelta = new Vector2(100, 30);
        }

        // Selection indicator
        GameObject selObj = new GameObject("SelectionIndicator");
        selObj.transform.SetParent(m_MenuPanel);
        selObj.transform.SetAsFirstSibling(); // Behind buttons
        m_SelectionIndicator = selObj.AddComponent<Image>();
        m_SelectionIndicator.color = m_HighlightColor;
        var selRect = selObj.GetComponent<RectTransform>();
        selRect.anchoredPosition = Vector2.zero;
        selRect.sizeDelta = new Vector2(70, 70);
        m_SelectionIndicator.gameObject.SetActive(false);

        Debug.Log("[DogRadialMenu] UI built");
    }

    private void OpenMenu()
    {
        if (!m_WasBuilt) BuildUI();

        m_IsOpen = true;
        m_MenuPanel.gameObject.SetActive(true);
        m_MenuCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        m_SelectedOption = -1;

        // Show cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Slow time slightly for tactical feel
        Time.timeScale = 0.3f;

        UpdateVisuals();
    }

    private void CloseMenu()
    {
        m_IsOpen = false;

        if (m_MenuPanel != null)
            m_MenuPanel.gameObject.SetActive(false);

        // Hide cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Restore time
        Time.timeScale = 1f;
    }

    private void UpdateSelection()
    {
        Vector2 mousePos = Input.mousePosition;
        Vector2 delta = mousePos - m_MenuCenter;
        float distance = delta.magnitude;

        if (distance < m_DeadZone)
        {
            m_SelectedOption = -1;
        }
        else
        {
            // Calculate angle and determine selection
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            // Map angle to option (0=right, 90=top, 180=left, 270=bottom)
            // Our layout: Top=Attack(0), Right=StayFollow(1), Bottom=Come(2), Left=Search(3)
            if (angle >= 45f && angle < 135f)
                m_SelectedOption = 0; // Top - Attack
            else if (angle >= 315f || angle < 45f)
                m_SelectedOption = 1; // Right - Stay/Follow
            else if (angle >= 225f && angle < 315f)
                m_SelectedOption = 2; // Bottom - Come
            else
                m_SelectedOption = 3; // Left - Search
        }

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        for (int i = 0; i < m_OptionIcons.Length; i++)
        {
            if (m_OptionIcons[i] != null)
            {
                m_OptionIcons[i].color = (i == m_SelectedOption) ? m_HighlightColor : m_NormalColor;
            }
        }

        // Move selection indicator
        if (m_SelectionIndicator != null)
        {
            if (m_SelectedOption >= 0 && m_SelectedOption < m_OptionButtons.Length)
            {
                m_SelectionIndicator.gameObject.SetActive(true);
                m_SelectionIndicator.rectTransform.anchoredPosition = m_OptionButtons[m_SelectedOption].anchoredPosition;
            }
            else
            {
                m_SelectionIndicator.gameObject.SetActive(false);
            }
        }
    }

    private void ExecuteSelection()
    {
        if (m_SelectedOption < 0) return;

        var dog = DogCompanion.Instance;
        if (dog == null) return;

        DogCommand cmd = (DogCommand)m_SelectedOption;

        switch (cmd)
        {
            case DogCommand.Attack:
                dog.CommandAttackCrosshairTarget();
                break;

            case DogCommand.StayFollow:
                dog.CommandStayFollow();
                break;

            case DogCommand.Come:
                dog.CommandCome();
                break;

            case DogCommand.Search:
                dog.CommandSearch();
                break;
        }

        Debug.Log($"[DogRadialMenu] Executed command: {cmd}");
    }

    public bool IsMenuOpen => m_IsOpen;
}
