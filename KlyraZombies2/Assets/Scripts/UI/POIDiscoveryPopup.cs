using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UI popup that displays POI discovery notifications.
/// Styled to match CharacterInfoPopup with iOS-inspired design.
/// </summary>
public class POIDiscoveryPopup : MonoBehaviour
{
    public static POIDiscoveryPopup Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject m_PopupPanel;
    [SerializeField] private CanvasGroup m_CanvasGroup;
    [SerializeField] private TextMeshProUGUI m_DiscoveredLabel;
    [SerializeField] private TextMeshProUGUI m_LocationName;
    [SerializeField] private TextMeshProUGUI m_CategoryText;
    [SerializeField] private TextMeshProUGUI m_DescriptionText;
    [SerializeField] private TextMeshProUGUI m_LootHintText;

    [Header("Timing")]
    [SerializeField] private float m_DisplayDuration = 5f;
    [SerializeField] private float m_FadeInDuration = 0.3f;
    [SerializeField] private float m_FadeOutDuration = 0.8f;

    [Header("Auto Setup")]
    [SerializeField] private bool m_CreateUIOnAwake = true;

    [Header("Persistence")]
    [SerializeField] private bool m_SaveDiscoveredPOIs = true;
    private const string DISCOVERED_POIS_KEY = "DiscoveredPOIs";

    private HashSet<string> m_DiscoveredPOIs = new HashSet<string>();
    private Coroutine m_DisplayCoroutine;
    private bool m_IsShowing = false;
    private Queue<POIData> m_QueuedPOIs = new Queue<POIData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (m_CreateUIOnAwake && m_PopupPanel == null)
        {
            CreateUI();
        }

        if (m_PopupPanel != null)
        {
            m_PopupPanel.SetActive(false);
        }

        LoadDiscoveredPOIs();
    }

    /// <summary>
    /// Shows the discovery popup for a POI if not already discovered.
    /// </summary>
    /// <returns>True if shown, false if already discovered</returns>
    public bool Show(POIData poiData)
    {
        if (poiData == null)
        {
            Debug.LogWarning("[POIDiscoveryPopup] Cannot show null POI data");
            return false;
        }

        // Check if already discovered
        if (IsDiscovered(poiData.poiId))
        {
            return false;
        }

        // Mark as discovered
        MarkDiscovered(poiData.poiId);

        // Queue if already showing something
        if (m_IsShowing)
        {
            m_QueuedPOIs.Enqueue(poiData);
            return true;
        }

        ShowImmediate(poiData);
        return true;
    }

    /// <summary>
    /// Force shows the popup even if already discovered.
    /// </summary>
    public void ForceShow(POIData poiData)
    {
        if (poiData == null) return;

        if (m_IsShowing)
        {
            m_QueuedPOIs.Enqueue(poiData);
            return;
        }

        ShowImmediate(poiData);
    }

    private void ShowImmediate(POIData poiData)
    {
        // Update UI text
        if (m_DiscoveredLabel != null)
            m_DiscoveredLabel.text = "DISCOVERED";

        if (m_LocationName != null)
            m_LocationName.text = poiData.displayName;

        if (m_CategoryText != null)
            m_CategoryText.text = GetCategoryDisplayName(poiData.category);

        if (m_DescriptionText != null)
        {
            m_DescriptionText.text = poiData.description;
            m_DescriptionText.gameObject.SetActive(!string.IsNullOrEmpty(poiData.description));
        }

        if (m_LootHintText != null)
        {
            m_LootHintText.text = poiData.lootHint;
            m_LootHintText.gameObject.SetActive(!string.IsNullOrEmpty(poiData.lootHint));
        }

        // Start display coroutine
        if (m_DisplayCoroutine != null)
        {
            StopCoroutine(m_DisplayCoroutine);
        }
        m_DisplayCoroutine = StartCoroutine(DisplayCoroutine());
    }

    public bool IsDiscovered(string poiId)
    {
        return m_DiscoveredPOIs.Contains(poiId);
    }

    public void MarkDiscovered(string poiId)
    {
        if (m_DiscoveredPOIs.Add(poiId))
        {
            SaveDiscoveredPOIs();
        }
    }

    public void ResetAllDiscoveries()
    {
        m_DiscoveredPOIs.Clear();
        SaveDiscoveredPOIs();
        Debug.Log("[POIDiscoveryPopup] All POI discoveries reset");
    }

    private void LoadDiscoveredPOIs()
    {
        if (!m_SaveDiscoveredPOIs) return;

        string saved = PlayerPrefs.GetString(DISCOVERED_POIS_KEY, "");
        if (!string.IsNullOrEmpty(saved))
        {
            string[] ids = saved.Split(',');
            foreach (string id in ids)
            {
                if (!string.IsNullOrEmpty(id))
                    m_DiscoveredPOIs.Add(id);
            }
        }
    }

    private void SaveDiscoveredPOIs()
    {
        if (!m_SaveDiscoveredPOIs) return;

        string[] ids = new string[m_DiscoveredPOIs.Count];
        m_DiscoveredPOIs.CopyTo(ids);
        PlayerPrefs.SetString(DISCOVERED_POIS_KEY, string.Join(",", ids));
        PlayerPrefs.Save();
    }

    private string GetCategoryDisplayName(POICategory category)
    {
        switch (category)
        {
            case POICategory.Landmark: return "Landmark";
            case POICategory.Building: return "Building";
            case POICategory.Military: return "Military Installation";
            case POICategory.Medical: return "Medical Facility";
            case POICategory.Commercial: return "Commercial Area";
            case POICategory.Residential: return "Residential Area";
            case POICategory.Industrial: return "Industrial Zone";
            case POICategory.SafeZone: return "Safe Zone";
            case POICategory.DangerZone: return "Danger Zone";
            default: return "Location";
        }
    }

    private IEnumerator DisplayCoroutine()
    {
        m_IsShowing = true;

        if (m_PopupPanel != null)
            m_PopupPanel.SetActive(true);

        // Fade in
        yield return StartCoroutine(FadeIn());

        // Wait for display duration
        yield return new WaitForSeconds(m_DisplayDuration);

        // Fade out
        yield return StartCoroutine(FadeOut());

        // Show next queued POI if any
        if (m_QueuedPOIs.Count > 0)
        {
            yield return new WaitForSeconds(0.3f); // Brief pause between popups
            POIData next = m_QueuedPOIs.Dequeue();
            ShowImmediate(next);
        }
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
    /// Positioned at top-center for discovery notifications.
    /// </summary>
    private void CreateUI()
    {
        // Find or create canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create popup panel - top center for discovery
        m_PopupPanel = new GameObject("POIDiscoveryPopup");
        m_PopupPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = m_PopupPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -60f);
        panelRect.sizeDelta = new Vector2(320f, 10f);

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

        // iOS-style frosted glass background (dark mode)
        Image bg = bgContainer.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.14f, 0.92f);

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
        layout.padding = new RectOffset(20, 20, 16, 16);
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // iOS-style colors
        Color primaryText = new Color(1f, 1f, 1f, 1f);
        Color secondaryText = new Color(0.92f, 0.92f, 0.96f, 0.6f);
        Color accentGold = new Color(1f, 0.84f, 0f, 1f); // Gold for "DISCOVERED"
        Color subtleGray = new Color(0.92f, 0.92f, 0.96f, 0.4f);

        // "DISCOVERED" label - small, gold, centered
        m_DiscoveredLabel = CreateText(content.transform, "DiscoveredLabel", 10, FontStyles.Bold, accentGold, TextAlignmentOptions.Center);

        // Location name - prominent, white, centered
        m_LocationName = CreateText(content.transform, "LocationName", 20, FontStyles.Bold, primaryText, TextAlignmentOptions.Center);

        // Category - secondary text
        m_CategoryText = CreateText(content.transform, "CategoryText", 12, FontStyles.Normal, secondaryText, TextAlignmentOptions.Center);

        // Subtle divider
        CreateDivider(content.transform);

        // Description - optional
        m_DescriptionText = CreateText(content.transform, "DescriptionText", 12, FontStyles.Italic, subtleGray, TextAlignmentOptions.Center);

        // Loot hint - optional
        m_LootHintText = CreateText(content.transform, "LootHintText", 11, FontStyles.Normal, subtleGray, TextAlignmentOptions.Center);

        // Add content size fitter
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ContentSizeFitter panelFitter = m_PopupPanel.AddComponent<ContentSizeFitter>();
        panelFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Debug.Log("[POIDiscoveryPopup] UI created");
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, float fontSize, FontStyles style, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(280f, fontSize + 8f);

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

    private void CreateDivider(Transform parent)
    {
        GameObject divider = new GameObject("Divider");
        divider.transform.SetParent(parent, false);

        RectTransform rect = divider.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(280f, 10f);

        GameObject line = new GameObject("Line");
        line.transform.SetParent(divider.transform, false);
        RectTransform lineRect = line.AddComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0.2f, 0.5f);
        lineRect.anchorMax = new Vector2(0.8f, 0.5f);
        lineRect.sizeDelta = new Vector2(0f, 0.5f);
        lineRect.anchoredPosition = Vector2.zero;

        Image lineImg = line.AddComponent<Image>();
        lineImg.color = new Color(1f, 1f, 1f, 0.1f);

        LayoutElement layout = divider.AddComponent<LayoutElement>();
        layout.minHeight = 10f;
        layout.preferredHeight = 10f;
    }

    public bool IsShowing => m_IsShowing;
    public int DiscoveredCount => m_DiscoveredPOIs.Count;
}
