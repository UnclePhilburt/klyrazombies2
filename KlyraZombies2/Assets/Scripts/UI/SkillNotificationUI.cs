using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Displays skill level up notifications on screen.
/// Auto-creates UI and shows temporary popups when skills increase.
/// </summary>
public class SkillNotificationUI : MonoBehaviour
{
    public static SkillNotificationUI Instance { get; private set; }

    [Header("Display Settings")]
    [SerializeField] private float m_DisplayDuration = 3f;
    [SerializeField] private float m_FadeInDuration = 0.3f;
    [SerializeField] private float m_FadeOutDuration = 0.5f;

    [Header("Position")]
    [SerializeField] private Vector2 m_ScreenPosition = new Vector2(0f, 200f); // Center, above middle

    [Header("Styling")]
    [SerializeField] private Color m_BackgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.9f);
    [SerializeField] private Color m_BorderColor = new Color(0.4f, 0.7f, 0.3f, 0.9f); // Green for level up
    [SerializeField] private Color m_TitleColor = new Color(0.5f, 0.9f, 0.4f, 1f); // Bright green
    [SerializeField] private Color m_TextColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private float m_TitleFontSize = 18f;
    [SerializeField] private float m_TextFontSize = 14f;

    [Header("Size")]
    [SerializeField] private Vector2 m_PanelSize = new Vector2(280f, 70f);
    [SerializeField] private float m_BorderWidth = 2f;

    [Header("Audio")]
    [SerializeField] private AudioClip m_LevelUpSound;
    [SerializeField] [Range(0f, 1f)] private float m_SoundVolume = 0.7f;

    // UI Elements
    private Canvas m_Canvas;
    private GameObject m_NotificationPanel;
    private CanvasGroup m_CanvasGroup;
    private TextMeshProUGUI m_TitleText;
    private TextMeshProUGUI m_DescriptionText;
    private AudioSource m_AudioSource;

    // State
    private Coroutine m_CurrentNotification;
    private bool m_Initialized = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        CreateUI();
        m_Initialized = true;
    }

    private void CreateUI()
    {
        // Create canvas with constant pixel size (won't affect other UI)
        GameObject canvasObj = new GameObject("SkillNotificationCanvas");
        canvasObj.transform.SetParent(transform);
        m_Canvas = canvasObj.AddComponent<Canvas>();
        m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        m_Canvas.sortingOrder = 150; // Above most UI

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Create notification panel
        m_NotificationPanel = new GameObject("NotificationPanel");
        m_NotificationPanel.transform.SetParent(m_Canvas.transform, false);

        var panelRect = m_NotificationPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = m_ScreenPosition;
        panelRect.sizeDelta = m_PanelSize;

        // Canvas group for fading
        m_CanvasGroup = m_NotificationPanel.AddComponent<CanvasGroup>();
        m_CanvasGroup.alpha = 0f;
        m_CanvasGroup.blocksRaycasts = false;
        m_CanvasGroup.interactable = false;

        // Border
        var borderObj = new GameObject("Border");
        borderObj.transform.SetParent(m_NotificationPanel.transform, false);
        var borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;
        var borderImg = borderObj.AddComponent<Image>();
        borderImg.color = m_BorderColor;

        // Background
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(m_NotificationPanel.transform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(m_BorderWidth, m_BorderWidth);
        bgRect.offsetMax = new Vector2(-m_BorderWidth, -m_BorderWidth);
        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = m_BackgroundColor;

        // Title text (e.g., "ENDURANCE LEVEL UP!")
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(m_NotificationPanel.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.5f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.offsetMin = new Vector2(10, 0);
        titleRect.offsetMax = new Vector2(-10, -8);
        m_TitleText = titleObj.AddComponent<TextMeshProUGUI>();
        m_TitleText.fontSize = m_TitleFontSize;
        m_TitleText.fontStyle = FontStyles.Bold;
        m_TitleText.color = m_TitleColor;
        m_TitleText.alignment = TextAlignmentOptions.Center;
        m_TitleText.text = "SKILL LEVEL UP!";

        // Description text (e.g., "Max Stamina now 115")
        var descObj = new GameObject("Description");
        descObj.transform.SetParent(m_NotificationPanel.transform, false);
        var descRect = descObj.AddComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0, 0);
        descRect.anchorMax = new Vector2(1, 0.5f);
        descRect.offsetMin = new Vector2(10, 8);
        descRect.offsetMax = new Vector2(-10, 0);
        m_DescriptionText = descObj.AddComponent<TextMeshProUGUI>();
        m_DescriptionText.fontSize = m_TextFontSize;
        m_DescriptionText.color = m_TextColor;
        m_DescriptionText.alignment = TextAlignmentOptions.Center;
        m_DescriptionText.text = "Max Stamina now 115";

        // Audio source
        m_AudioSource = gameObject.AddComponent<AudioSource>();
        m_AudioSource.playOnAwake = false;

        // Start hidden
        m_NotificationPanel.SetActive(false);
    }

    /// <summary>
    /// Show a skill level up notification.
    /// </summary>
    public void ShowSkillLevelUp(string skillName, int newLevel, string effectDescription)
    {
        if (!m_Initialized) return;

        // Stop any existing notification
        if (m_CurrentNotification != null)
        {
            StopCoroutine(m_CurrentNotification);
        }

        m_TitleText.text = $"{skillName.ToUpper()} LEVEL {newLevel}!";
        m_DescriptionText.text = effectDescription;

        m_CurrentNotification = StartCoroutine(ShowNotificationCoroutine());
    }

    /// <summary>
    /// Show endurance level up specifically.
    /// </summary>
    public void ShowEnduranceLevelUp(int newLevel, float newMaxStamina)
    {
        ShowSkillLevelUp("Endurance", newLevel, $"Max Stamina now {newMaxStamina:F0}");
    }

    private IEnumerator ShowNotificationCoroutine()
    {
        m_NotificationPanel.SetActive(true);

        // Play sound
        if (m_LevelUpSound != null && m_AudioSource != null)
        {
            m_AudioSource.PlayOneShot(m_LevelUpSound, m_SoundVolume);
        }

        // Fade in
        float elapsed = 0f;
        while (elapsed < m_FadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            m_CanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / m_FadeInDuration);
            yield return null;
        }
        m_CanvasGroup.alpha = 1f;

        // Hold
        yield return new WaitForSecondsRealtime(m_DisplayDuration);

        // Fade out
        elapsed = 0f;
        while (elapsed < m_FadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            m_CanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / m_FadeOutDuration);
            yield return null;
        }
        m_CanvasGroup.alpha = 0f;

        m_NotificationPanel.SetActive(false);
        m_CurrentNotification = null;
    }

    /// <summary>
    /// Show a generic notification message.
    /// </summary>
    public void ShowNotification(string title, string description, Color? borderColor = null)
    {
        if (!m_Initialized) return;

        if (m_CurrentNotification != null)
        {
            StopCoroutine(m_CurrentNotification);
        }

        m_TitleText.text = title;
        m_DescriptionText.text = description;

        // Temporarily change border color if specified
        if (borderColor.HasValue)
        {
            var borderImg = m_NotificationPanel.transform.Find("Border")?.GetComponent<Image>();
            if (borderImg != null)
            {
                borderImg.color = borderColor.Value;
            }
        }

        m_CurrentNotification = StartCoroutine(ShowNotificationCoroutine());
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && m_Initialized)
        {
            // Update colors in real-time
            var borderImg = m_NotificationPanel?.transform.Find("Border")?.GetComponent<Image>();
            if (borderImg != null) borderImg.color = m_BorderColor;

            var bgImg = m_NotificationPanel?.transform.Find("Background")?.GetComponent<Image>();
            if (bgImg != null) bgImg.color = m_BackgroundColor;

            if (m_TitleText != null)
            {
                m_TitleText.color = m_TitleColor;
                m_TitleText.fontSize = m_TitleFontSize;
            }

            if (m_DescriptionText != null)
            {
                m_DescriptionText.color = m_TextColor;
                m_DescriptionText.fontSize = m_TextFontSize;
            }

            var panelRect = m_NotificationPanel?.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.anchoredPosition = m_ScreenPosition;
                panelRect.sizeDelta = m_PanelSize;
            }
        }
    }
#endif
}
