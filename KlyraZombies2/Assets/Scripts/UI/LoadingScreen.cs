using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Simple loading screen that shows while the game initializes.
/// Shows immediately and hides when SidekickCharacterSpawner finishes loading.
/// </summary>
[DefaultExecutionOrder(-1000)] // Run very early
public class LoadingScreen : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup m_CanvasGroup;
    [SerializeField] private Image m_LoadingSpinner;
    [SerializeField] private Text m_LoadingText;
    [SerializeField] private Slider m_ProgressBar;

    [Header("Settings")]
    [SerializeField] private float m_SpinSpeed = 200f;
    [SerializeField] private float m_FadeOutDuration = 0.5f;
    [SerializeField] private string[] m_LoadingMessages = new string[]
    {
        "Loading...",
        "Preparing character...",
        "Setting up world...",
        "Almost ready..."
    };

    [Header("Auto-Create UI")]
    [Tooltip("If true, creates a simple loading UI if none is assigned")]
    [SerializeField] private bool m_AutoCreateUI = true;

    private int m_CurrentMessageIndex = 0;
    private float m_MessageTimer = 0f;
    private float m_MessageInterval = 1.5f;
    private bool m_IsLoading = true;

    private static LoadingScreen s_Instance;
    public static LoadingScreen Instance => s_Instance;

    private void Awake()
    {
        s_Instance = this;

        // Make sure we're visible immediately
        if (m_CanvasGroup == null && m_AutoCreateUI)
        {
            CreateLoadingUI();
        }

        if (m_CanvasGroup != null)
        {
            m_CanvasGroup.alpha = 1f;
            m_CanvasGroup.blocksRaycasts = true;
        }

        // Don't destroy on load in case we need it between scenes
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!m_IsLoading) return;

        // Spin the loading spinner
        if (m_LoadingSpinner != null)
        {
            m_LoadingSpinner.transform.Rotate(0, 0, -m_SpinSpeed * Time.deltaTime);
        }

        // Cycle through loading messages
        m_MessageTimer += Time.deltaTime;
        if (m_MessageTimer >= m_MessageInterval && m_LoadingMessages.Length > 0)
        {
            m_MessageTimer = 0f;
            m_CurrentMessageIndex = (m_CurrentMessageIndex + 1) % m_LoadingMessages.Length;
            if (m_LoadingText != null)
            {
                m_LoadingText.text = m_LoadingMessages[m_CurrentMessageIndex];
            }
        }
    }

    /// <summary>
    /// Update the progress bar (0-1).
    /// </summary>
    public void SetProgress(float progress)
    {
        if (m_ProgressBar != null)
        {
            m_ProgressBar.value = Mathf.Clamp01(progress);
        }
    }

    /// <summary>
    /// Set the loading message.
    /// </summary>
    public void SetMessage(string message)
    {
        if (m_LoadingText != null)
        {
            m_LoadingText.text = message;
        }
    }

    /// <summary>
    /// Hide the loading screen with a fade.
    /// </summary>
    public void Hide()
    {
        if (!m_IsLoading) return;
        m_IsLoading = false;

        // Immediately stop blocking input
        if (m_CanvasGroup != null)
        {
            m_CanvasGroup.blocksRaycasts = false;
            m_CanvasGroup.interactable = false;
        }

        StartCoroutine(FadeOut());
    }

    /// <summary>
    /// Immediately hide (no fade).
    /// </summary>
    public void HideImmediate()
    {
        m_IsLoading = false;
        if (m_CanvasGroup != null)
        {
            m_CanvasGroup.alpha = 0f;
            m_CanvasGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        float startAlpha = m_CanvasGroup != null ? m_CanvasGroup.alpha : 1f;

        while (elapsed < m_FadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / m_FadeOutDuration;

            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            }

            yield return null;
        }

        if (m_CanvasGroup != null)
        {
            m_CanvasGroup.alpha = 0f;
            m_CanvasGroup.blocksRaycasts = false;
        }

        // Clear static instance and destroy
        if (s_Instance == this)
        {
            s_Instance = null;
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (s_Instance == this)
        {
            s_Instance = null;
        }
    }

    /// <summary>
    /// Create a simple loading UI programmatically.
    /// </summary>
    private void CreateLoadingUI()
    {
        // Create canvas
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // Always on top

        // Add canvas scaler
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Add raycaster
        gameObject.AddComponent<GraphicRaycaster>();

        // Add canvas group
        m_CanvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Create background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(transform, false);
        Image bg = bgObj.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        RectTransform bgRect = bg.rectTransform;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Create loading text
        GameObject textObj = new GameObject("LoadingText");
        textObj.transform.SetParent(transform, false);
        m_LoadingText = textObj.AddComponent<Text>();
        m_LoadingText.text = m_LoadingMessages.Length > 0 ? m_LoadingMessages[0] : "Loading...";
        m_LoadingText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        m_LoadingText.fontSize = 48;
        m_LoadingText.alignment = TextAnchor.MiddleCenter;
        m_LoadingText.color = Color.white;
        RectTransform textRect = m_LoadingText.rectTransform;
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(600, 100);
        textRect.anchoredPosition = new Vector2(0, 0);

        // Create spinner
        GameObject spinnerObj = new GameObject("Spinner");
        spinnerObj.transform.SetParent(transform, false);
        m_LoadingSpinner = spinnerObj.AddComponent<Image>();
        m_LoadingSpinner.color = Color.white;

        // Create a simple spinner texture (circle with gap)
        Texture2D spinnerTex = CreateSpinnerTexture(64);
        m_LoadingSpinner.sprite = Sprite.Create(spinnerTex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));

        RectTransform spinnerRect = m_LoadingSpinner.rectTransform;
        spinnerRect.anchorMin = new Vector2(0.5f, 0.5f);
        spinnerRect.anchorMax = new Vector2(0.5f, 0.5f);
        spinnerRect.sizeDelta = new Vector2(80, 80);
        spinnerRect.anchoredPosition = new Vector2(0, 100);

        // Create progress bar background
        GameObject progressBgObj = new GameObject("ProgressBarBg");
        progressBgObj.transform.SetParent(transform, false);
        Image progressBg = progressBgObj.AddComponent<Image>();
        progressBg.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        RectTransform progressBgRect = progressBg.rectTransform;
        progressBgRect.anchorMin = new Vector2(0.5f, 0.5f);
        progressBgRect.anchorMax = new Vector2(0.5f, 0.5f);
        progressBgRect.sizeDelta = new Vector2(400, 20);
        progressBgRect.anchoredPosition = new Vector2(0, -80);

        // Create progress bar
        GameObject progressObj = new GameObject("ProgressBar");
        progressObj.transform.SetParent(progressBgObj.transform, false);
        m_ProgressBar = progressObj.AddComponent<Slider>();
        m_ProgressBar.minValue = 0f;
        m_ProgressBar.maxValue = 1f;
        m_ProgressBar.value = 0f;

        // Setup slider components
        RectTransform progressRect = progressObj.GetComponent<RectTransform>();
        progressRect.anchorMin = Vector2.zero;
        progressRect.anchorMax = Vector2.one;
        progressRect.sizeDelta = Vector2.zero;

        // Fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(progressObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;

        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.6f, 1f, 1f);
        RectTransform fillRect = fillImage.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        m_ProgressBar.fillRect = fillRect;
        m_ProgressBar.targetGraphic = fillImage;

        Debug.Log("[LoadingScreen] Auto-created loading UI");
    }

    private Texture2D CreateSpinnerTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color transparent = new Color(0, 0, 0, 0);
        Color white = Color.white;

        float center = size / 2f;
        float outerRadius = size / 2f - 2;
        float innerRadius = size / 2f - 10;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Check if in ring
                if (dist >= innerRadius && dist <= outerRadius)
                {
                    // Calculate angle and create gap
                    float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    if (angle < 0) angle += 360f;

                    // Gap from 0-60 degrees
                    if (angle > 60f)
                    {
                        tex.SetPixel(x, y, white);
                    }
                    else
                    {
                        tex.SetPixel(x, y, transparent);
                    }
                }
                else
                {
                    tex.SetPixel(x, y, transparent);
                }
            }
        }

        tex.Apply();
        return tex;
    }
}
