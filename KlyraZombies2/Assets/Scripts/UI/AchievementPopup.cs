using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UI popup that displays achievement unlock notifications.
/// Styled to match CharacterInfoPopup with iOS-inspired design.
/// </summary>
public class AchievementPopup : MonoBehaviour
{
    public static AchievementPopup Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject m_PopupPanel;
    [SerializeField] private CanvasGroup m_CanvasGroup;
    [SerializeField] private TextMeshProUGUI m_UnlockedLabel;
    [SerializeField] private TextMeshProUGUI m_TitleText;
    [SerializeField] private TextMeshProUGUI m_DescriptionText;
    [SerializeField] private Image m_IconImage;

    [Header("Timing")]
    [SerializeField] private float m_DisplayDuration = 4f;
    [SerializeField] private float m_FadeInDuration = 0.3f;
    [SerializeField] private float m_FadeOutDuration = 0.6f;

    [Header("Audio")]
    [SerializeField] private AudioClip m_UnlockSound;

    [Header("Auto Setup")]
    [SerializeField] private bool m_CreateUIOnAwake = true;

    private Coroutine m_DisplayCoroutine;
    private bool m_IsShowing = false;
    private Queue<AchievementData> m_QueuedAchievements = new Queue<AchievementData>();
    private AudioSource m_AudioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (m_CreateUIOnAwake && m_PopupPanel == null)
        {
            CreateUI();
        }

        if (m_PopupPanel != null)
        {
            m_PopupPanel.SetActive(false);
        }

        m_AudioSource = gameObject.AddComponent<AudioSource>();
        m_AudioSource.playOnAwake = false;
    }

    /// <summary>
    /// Shows the achievement unlock popup.
    /// </summary>
    public void Show(AchievementData achievement)
    {
        Debug.Log($"[AchievementPopup] Show() called for: {achievement?.title ?? "NULL"}");

        if (achievement == null)
        {
            Debug.LogWarning("[AchievementPopup] Cannot show null achievement");
            return;
        }

        // Queue if already showing something
        if (m_IsShowing)
        {
            Debug.Log($"[AchievementPopup] Already showing, queuing: {achievement.title}");
            m_QueuedAchievements.Enqueue(achievement);
            return;
        }

        ShowImmediate(achievement);
    }

    private void ShowImmediate(AchievementData achievement)
    {
        Debug.Log($"[AchievementPopup] ShowImmediate() - Panel: {m_PopupPanel != null}, CanvasGroup: {m_CanvasGroup != null}");

        // Recreate UI if it was destroyed (e.g., scene change) or Canvas is gone
        // Use ReferenceEquals to catch Unity's fake null for destroyed objects
        bool panelValid = m_PopupPanel != null && m_PopupPanel.gameObject != null;
        bool canvasGroupValid = m_CanvasGroup != null;
        Canvas parentCanvas = panelValid ? m_PopupPanel.GetComponentInParent<Canvas>() : null;

        if (!panelValid || !canvasGroupValid || parentCanvas == null)
        {
            Debug.Log($"[AchievementPopup] UI invalid (Panel:{panelValid}, CanvasGroup:{canvasGroupValid}, Canvas:{parentCanvas != null}), recreating...");
            m_PopupPanel = null;
            m_CanvasGroup = null;
            CreateUI();
        }

        // Update UI
        if (m_UnlockedLabel != null)
            m_UnlockedLabel.text = "ACHIEVEMENT UNLOCKED";

        if (m_TitleText != null)
            m_TitleText.text = achievement.title;

        if (m_DescriptionText != null)
        {
            m_DescriptionText.text = achievement.description;
            m_DescriptionText.gameObject.SetActive(!string.IsNullOrEmpty(achievement.description));
        }

        if (m_IconImage != null)
        {
            if (achievement.icon != null)
            {
                m_IconImage.sprite = achievement.icon;
                m_IconImage.gameObject.SetActive(true);
            }
            else
            {
                m_IconImage.gameObject.SetActive(false);
            }
        }

        // Play sound
        if (m_UnlockSound != null && m_AudioSource != null)
        {
            m_AudioSource.PlayOneShot(m_UnlockSound);
        }

        // Start display coroutine
        if (m_DisplayCoroutine != null)
        {
            StopCoroutine(m_DisplayCoroutine);
        }
        m_DisplayCoroutine = StartCoroutine(DisplayCoroutine());
    }

    private IEnumerator DisplayCoroutine()
    {
        Debug.Log("[AchievementPopup] DisplayCoroutine started");
        m_IsShowing = true;

        if (m_PopupPanel != null)
        {
            m_PopupPanel.SetActive(true);
            Debug.Log($"[AchievementPopup] Panel activated. Active: {m_PopupPanel.activeSelf}, Position: {m_PopupPanel.GetComponent<RectTransform>()?.anchoredPosition}");
        }
        else
        {
            Debug.LogError("[AchievementPopup] Panel is NULL in DisplayCoroutine!");
        }

        // Fade in
        Debug.Log("[AchievementPopup] Starting FadeIn");
        yield return StartCoroutine(FadeIn());
        Debug.Log("[AchievementPopup] FadeIn complete");

        // Wait for display duration
        yield return new WaitForSeconds(m_DisplayDuration);

        // Fade out
        yield return StartCoroutine(FadeOut());

        // Show next queued achievement if any
        if (m_QueuedAchievements.Count > 0)
        {
            yield return new WaitForSeconds(0.3f);
            AchievementData next = m_QueuedAchievements.Dequeue();
            ShowImmediate(next);
        }
    }

    private IEnumerator FadeIn()
    {
        if (m_CanvasGroup == null)
        {
            Debug.LogWarning("[AchievementPopup] FadeIn - CanvasGroup is null!");
            yield break;
        }

        m_CanvasGroup.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < m_FadeInDuration)
        {
            if (m_CanvasGroup == null) yield break; // Safety check
            elapsed += Time.deltaTime;
            m_CanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / m_FadeInDuration);
            yield return null;
        }

        if (m_CanvasGroup != null)
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
            if (m_CanvasGroup == null) break; // Safety check
            elapsed += Time.deltaTime;
            m_CanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / m_FadeOutDuration);
            yield return null;
        }

        if (m_CanvasGroup != null)
            m_CanvasGroup.alpha = 0f;
        m_IsShowing = false;

        if (m_PopupPanel != null)
            m_PopupPanel.SetActive(false);
    }

    /// <summary>
    /// Creates the UI programmatically with iOS-inspired styling.
    /// Positioned in center of screen.
    /// </summary>
    private void CreateUI()
    {
        Debug.Log("[AchievementPopup] CreateUI called - building fresh UI");

        // Destroy any existing UI children first
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        // Create our own persistent canvas (don't use scene canvas - it gets destroyed on scene load)
        GameObject canvasObj = new GameObject("AchievementCanvas");
        canvasObj.transform.SetParent(transform); // Parent to this DontDestroyOnLoad object
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // On top of everything
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Create popup panel - CENTER, moved up
        m_PopupPanel = new GameObject("AchievementPopupPanel");
        m_PopupPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = m_PopupPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f); // Center
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0f, 100f); // Centered, 100px above middle
        panelRect.sizeDelta = new Vector2(450f, 10f);

        // Add canvas group for fading
        m_CanvasGroup = m_PopupPanel.AddComponent<CanvasGroup>();
        m_CanvasGroup.blocksRaycasts = false;

        // Create background container
        GameObject bgContainer = new GameObject("Background");
        bgContainer.transform.SetParent(m_PopupPanel.transform, false);
        RectTransform bgRect = bgContainer.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // iOS-style frosted glass background (dark mode) with gold tint
        Image bg = bgContainer.AddComponent<Image>();
        bg.color = new Color(0.14f, 0.13f, 0.10f, 0.94f); // Slightly gold-tinted dark

        // Content container with padding
        GameObject content = new GameObject("Content");
        content.transform.SetParent(m_PopupPanel.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.sizeDelta = Vector2.zero;
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        // Add vertical layout
        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 14, 14);
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Colors
        Color primaryText = new Color(1f, 1f, 1f, 1f);
        Color accentGold = new Color(1f, 0.84f, 0f, 1f);
        Color subtleGray = new Color(0.92f, 0.92f, 0.96f, 0.5f);

        // Trophy icon placeholder (will be replaced if achievement has icon)
        GameObject iconContainer = new GameObject("IconContainer");
        iconContainer.transform.SetParent(content.transform, false);
        RectTransform iconContainerRect = iconContainer.AddComponent<RectTransform>();
        iconContainerRect.sizeDelta = new Vector2(40f, 40f);

        LayoutElement iconLayout = iconContainer.AddComponent<LayoutElement>();
        iconLayout.minHeight = 40f;
        iconLayout.preferredHeight = 40f;

        m_IconImage = iconContainer.AddComponent<Image>();
        m_IconImage.color = accentGold;
        m_IconImage.preserveAspect = true;
        m_IconImage.gameObject.SetActive(false); // Hidden by default, shown when achievement has icon

        // "ACHIEVEMENT UNLOCKED" label
        m_UnlockedLabel = CreateText(content.transform, "UnlockedLabel", 14, FontStyles.Bold, accentGold, TextAlignmentOptions.Center);
        m_UnlockedLabel.characterSpacing = 4f; // Letter spacing for emphasis

        // Achievement title
        m_TitleText = CreateText(content.transform, "TitleText", 28, FontStyles.Bold, primaryText, TextAlignmentOptions.Center);

        // Description
        m_DescriptionText = CreateText(content.transform, "DescriptionText", 16, FontStyles.Normal, subtleGray, TextAlignmentOptions.Center);

        // Add content size fitter
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ContentSizeFitter panelFitter = m_PopupPanel.AddComponent<ContentSizeFitter>();
        panelFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Debug.Log("[AchievementPopup] UI created");
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, float fontSize, FontStyles style, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(260f, fontSize + 8f);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.lineSpacing = -5f;

        LayoutElement layoutElem = textObj.AddComponent<LayoutElement>();
        layoutElem.minHeight = fontSize + 8f;
        layoutElem.preferredHeight = fontSize + 8f;
        layoutElem.flexibleHeight = 0f;

        return tmp;
    }

    public bool IsShowing => m_IsShowing;
}
