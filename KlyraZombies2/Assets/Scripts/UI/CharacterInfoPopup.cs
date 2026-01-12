using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// UI popup that displays random character backstory info when the player spawns.
/// Auto-dismisses after a set duration.
/// </summary>
public class CharacterInfoPopup : MonoBehaviour
{
    public static CharacterInfoPopup Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject m_PopupPanel;
    [SerializeField] private CanvasGroup m_CanvasGroup;
    [SerializeField] private TextMeshProUGUI m_NameText;
    [SerializeField] private TextMeshProUGUI m_AgeText;
    [SerializeField] private TextMeshProUGUI m_OccupationText;
    [SerializeField] private TextMeshProUGUI m_FamilyText;
    [SerializeField] private TextMeshProUGUI m_FunFactText;
    [SerializeField] private TextMeshProUGUI m_DaysSurvivedText;

    [Header("Timing")]
    [SerializeField] private float m_DisplayDuration = 10f;
    [SerializeField] private float m_FadeInDuration = 0.5f;
    [SerializeField] private float m_FadeOutDuration = 1f;

    [Header("Input")]
    [SerializeField] private KeyCode m_DismissKey = KeyCode.Space;
    [SerializeField] private bool m_ClickToDismiss = true;

    [Header("Auto Setup")]
    [SerializeField] private bool m_CreateUIOnAwake = true;

    [Header("UI Scale")]
    [SerializeField] private float m_UIScaleMultiplier = 1.5f;
    [SerializeField] private float m_PanelWidth = 320f;
    [SerializeField] private Vector2 m_PanelPosition = new Vector2(30f, 140f);

    [Header("Font Sizes")]
    [SerializeField] private float m_DayFontSize = 13f;
    [SerializeField] private float m_NameFontSize = 22f;
    [SerializeField] private float m_DetailsFontSize = 15f;
    [SerializeField] private float m_SubtleFontSize = 14f;

    private Canvas m_Canvas;
    private CharacterBackstory m_CurrentBackstory;
    private Coroutine m_DisplayCoroutine;
    private bool m_IsShowing = false;

    private void Awake()
    {
        Instance = this;

        if (m_CreateUIOnAwake && m_PopupPanel == null)
        {
            CreateUI();
        }

        if (m_PopupPanel != null)
        {
            m_PopupPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (!m_IsShowing) return;

        // Check for dismiss input
        if (Input.GetKeyDown(m_DismissKey) || (m_ClickToDismiss && Input.GetMouseButtonDown(0)))
        {
            Dismiss();
        }
    }

    /// <summary>
    /// Shows the popup with a randomly generated backstory.
    /// </summary>
    public void ShowRandom()
    {
        Show(CharacterBackstory.GenerateRandom());
    }

    /// <summary>
    /// Shows the popup with the given backstory.
    /// </summary>
    public void Show(CharacterBackstory backstory)
    {
        if (backstory == null)
        {
            Debug.LogWarning("[CharacterInfoPopup] Cannot show null backstory");
            return;
        }

        m_CurrentBackstory = backstory;

        // Update UI text - iOS clean formatting
        if (m_DaysSurvivedText != null)
            m_DaysSurvivedText.text = $"DAY {backstory.daysSurvived}";

        if (m_NameText != null)
            m_NameText.text = backstory.characterName;

        if (m_AgeText != null)
            m_AgeText.text = $"{backstory.age} years old";

        if (m_OccupationText != null)
            m_OccupationText.text = backstory.formerOccupation;

        if (m_FamilyText != null)
            m_FamilyText.text = $"\"{backstory.familyStatus}\"";

        if (m_FunFactText != null)
            m_FunFactText.text = backstory.funFact;

        // Start display coroutine
        if (m_DisplayCoroutine != null)
        {
            StopCoroutine(m_DisplayCoroutine);
        }
        m_DisplayCoroutine = StartCoroutine(DisplayCoroutine());
    }

    /// <summary>
    /// Dismisses the popup early.
    /// </summary>
    public void Dismiss()
    {
        if (!m_IsShowing) return;

        if (m_DisplayCoroutine != null)
        {
            StopCoroutine(m_DisplayCoroutine);
        }
        StartCoroutine(FadeOut());
    }

    private IEnumerator DisplayCoroutine()
    {
        m_IsShowing = true;

        // Show panel
        if (m_PopupPanel != null)
            m_PopupPanel.SetActive(true);

        // Fade in
        yield return StartCoroutine(FadeIn());

        // Wait for display duration
        yield return new WaitForSeconds(m_DisplayDuration);

        // Fade out
        yield return StartCoroutine(FadeOut());
    }

    private IEnumerator FadeIn()
    {
        if (m_CanvasGroup == null) yield break;

        m_CanvasGroup.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < m_FadeInDuration)
        {
            elapsed += Time.deltaTime;
            m_CanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / m_FadeInDuration);
            yield return null;
        }

        m_CanvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOut()
    {
        if (m_CanvasGroup == null)
        {
            if (m_PopupPanel != null)
                m_PopupPanel.SetActive(false);
            m_IsShowing = false;
            yield break;
        }

        float startAlpha = m_CanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < m_FadeOutDuration)
        {
            elapsed += Time.deltaTime;
            m_CanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / m_FadeOutDuration);
            yield return null;
        }

        m_CanvasGroup.alpha = 0f;
        m_IsShowing = false;

        if (m_PopupPanel != null)
            m_PopupPanel.SetActive(false);
    }

    /// <summary>
    /// Creates the UI programmatically with iOS-inspired styling.
    /// </summary>
    private void CreateUI()
    {
        // Always create our own canvas with proper scaling
        GameObject canvasObj = new GameObject("CharacterInfoCanvas");
        m_Canvas = canvasObj.AddComponent<Canvas>();
        m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        m_Canvas.sortingOrder = 50; // Below inventory UI

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        // Use UI scale multiplier for high-DPI displays
        float baseRef = 1080f / Mathf.Max(0.5f, m_UIScaleMultiplier);
        scaler.referenceResolution = new Vector2(baseRef * 16f / 9f, baseRef);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Create popup panel - iOS style: bottom left, compact
        m_PopupPanel = new GameObject("CharacterInfoPopup");
        m_PopupPanel.transform.SetParent(m_Canvas.transform, false);

        RectTransform panelRect = m_PopupPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0f, 0f);
        panelRect.pivot = new Vector2(0f, 0f);
        panelRect.anchoredPosition = m_PanelPosition;
        panelRect.sizeDelta = new Vector2(m_PanelWidth, 10f); // Width from setting, height auto

        // Add canvas group for fading
        m_CanvasGroup = m_PopupPanel.AddComponent<CanvasGroup>();

        // Create rounded background container
        GameObject bgContainer = new GameObject("Background");
        bgContainer.transform.SetParent(m_PopupPanel.transform, false);
        RectTransform bgRect = bgContainer.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // iOS-style frosted glass background (dark mode)
        Image bg = bgContainer.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.14f, 0.92f); // iOS dark mode card

        // Content container with padding
        GameObject content = new GameObject("Content");
        content.transform.SetParent(m_PopupPanel.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.sizeDelta = Vector2.zero;
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        // Add vertical layout - tighter iOS spacing
        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 14, 14);
        layout.spacing = 2f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // iOS-style colors
        Color primaryText = new Color(1f, 1f, 1f, 1f);
        Color secondaryText = new Color(0.92f, 0.92f, 0.96f, 0.6f);
        Color accentBlue = new Color(0.04f, 0.52f, 1f, 1f); // iOS blue
        Color subtleGray = new Color(0.92f, 0.92f, 0.96f, 0.4f);

        // Day survived - small label at top
        m_DaysSurvivedText = CreateiOSText(content.transform, "DaysSurvivedText", m_DayFontSize, FontStyles.Bold, accentBlue);

        // Name - prominent
        m_NameText = CreateiOSText(content.transform, "NameText", m_NameFontSize, FontStyles.Bold, primaryText);

        // Age & Occupation on same conceptual line
        m_AgeText = CreateiOSText(content.transform, "AgeText", m_DetailsFontSize, FontStyles.Normal, secondaryText);
        m_OccupationText = CreateiOSText(content.transform, "OccupationText", m_DetailsFontSize, FontStyles.Normal, secondaryText);

        // Subtle divider
        CreateDivider(content.transform);

        // Family - italic, emotional
        m_FamilyText = CreateiOSText(content.transform, "FamilyText", m_SubtleFontSize, FontStyles.Italic, subtleGray);

        // Fun fact
        m_FunFactText = CreateiOSText(content.transform, "FunFactText", m_SubtleFontSize, FontStyles.Normal, subtleGray);

        // Add content size fitter to auto-size panel
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Also add to panel
        ContentSizeFitter panelFitter = m_PopupPanel.AddComponent<ContentSizeFitter>();
        panelFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Debug.Log("[CharacterInfoPopup] iOS-style UI created");
    }

    private TextMeshProUGUI CreateiOSText(Transform parent, string name, float fontSize, FontStyles style, Color color)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        float textWidth = m_PanelWidth - 32f; // Panel width minus padding
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(textWidth, fontSize + 8f);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.lineSpacing = -5f; // Tighter line spacing like iOS

        // Add layout element
        LayoutElement layoutElem = textObj.AddComponent<LayoutElement>();
        layoutElem.minHeight = fontSize + 8f;
        layoutElem.preferredHeight = fontSize + 8f;
        layoutElem.flexibleHeight = 0f;

        return tmp;
    }

    private void CreateDivider(Transform parent)
    {
        GameObject divider = new GameObject("Divider");
        divider.transform.SetParent(parent, false);

        float dividerWidth = m_PanelWidth - 32f; // Panel width minus padding
        RectTransform rect = divider.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(dividerWidth, 9f);

        // Create the line as a child
        GameObject line = new GameObject("Line");
        line.transform.SetParent(divider.transform, false);
        RectTransform lineRect = line.AddComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0f, 0.5f);
        lineRect.anchorMax = new Vector2(1f, 0.5f);
        lineRect.sizeDelta = new Vector2(0f, 0.5f);
        lineRect.anchoredPosition = Vector2.zero;

        Image lineImg = line.AddComponent<Image>();
        lineImg.color = new Color(1f, 1f, 1f, 0.1f); // Subtle iOS separator

        LayoutElement layout = divider.AddComponent<LayoutElement>();
        layout.minHeight = 9f;
        layout.preferredHeight = 9f;
    }

    public CharacterBackstory CurrentBackstory => m_CurrentBackstory;
    public bool IsShowing => m_IsShowing;
}
