using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Floating health bar that appears above zombies when damaged.
/// Auto-hides after a delay.
/// </summary>
public class ZombieHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image m_FillImage;
    [SerializeField] private Image m_BackgroundImage;
    [SerializeField] private Canvas m_Canvas;

    [Header("Settings")]
    [SerializeField] private float m_ShowDuration = 3f;
    [SerializeField] private float m_HeightOffset = 2.2f;
    [SerializeField] private float m_FadeSpeed = 2f;
    [SerializeField] private Color m_FullHealthColor = Color.green;
    [SerializeField] private Color m_LowHealthColor = Color.red;
    [SerializeField] private float m_LowHealthThreshold = 0.3f;

    private Transform m_Target;
    private Camera m_Camera;
    private float m_HideTimer;
    private CanvasGroup m_CanvasGroup;
    private bool m_IsVisible;
    private float m_CurrentHealth = 1f;
    private float m_TargetHealth = 1f;

    private void Awake()
    {
        m_Camera = Camera.main;

        // Get or add CanvasGroup for fading
        if (m_CanvasGroup == null)
        {
            m_CanvasGroup = GetComponent<CanvasGroup>();
            if (m_CanvasGroup == null)
                m_CanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void LateUpdate()
    {
        if (m_Target == null)
        {
            Destroy(gameObject);
            return;
        }

        // Update camera reference if needed
        if (m_Camera == null)
        {
            m_Camera = Camera.main;
            if (m_Camera == null)
            {
                // Try to find any camera
                m_Camera = FindFirstObjectByType<Camera>();
            }
        }

        if (m_Camera == null)
        {
            Debug.LogWarning("[ZombieHealthBar] No camera found!");
            return;
        }

        // Position above target
        transform.position = m_Target.position + Vector3.up * m_HeightOffset;

        // Billboard - face camera
        transform.rotation = Quaternion.LookRotation(transform.position - m_Camera.transform.position);

        // Smooth health bar fill
        if (m_FillImage != null)
        {
            m_CurrentHealth = Mathf.Lerp(m_CurrentHealth, m_TargetHealth, Time.deltaTime * 10f);
            m_FillImage.fillAmount = m_CurrentHealth;

            // Color based on health
            if (m_CurrentHealth <= m_LowHealthThreshold)
                m_FillImage.color = m_LowHealthColor;
            else
                m_FillImage.color = Color.Lerp(m_LowHealthColor, m_FullHealthColor, (m_CurrentHealth - m_LowHealthThreshold) / (1f - m_LowHealthThreshold));
        }

        // Handle visibility timer
        if (m_IsVisible)
        {
            m_HideTimer -= Time.deltaTime;
            if (m_HideTimer <= 0)
            {
                m_IsVisible = false;
            }

            // Fade in
            m_CanvasGroup.alpha = Mathf.MoveTowards(m_CanvasGroup.alpha, 1f, Time.deltaTime * m_FadeSpeed);
        }
        else
        {
            // Fade out
            m_CanvasGroup.alpha = Mathf.MoveTowards(m_CanvasGroup.alpha, 0f, Time.deltaTime * m_FadeSpeed);
        }
    }

    /// <summary>
    /// Initialize the health bar with a target to follow
    /// </summary>
    public void Initialize(Transform target)
    {
        m_Target = target;
    }

    /// <summary>
    /// Update the health bar (0-1 range)
    /// </summary>
    public void SetHealth(float normalizedHealth)
    {
        m_TargetHealth = Mathf.Clamp01(normalizedHealth);
        Debug.Log($"[ZombieHealthBar] SetHealth({normalizedHealth:F2}) on {m_Target?.name ?? "null"}, alpha={m_CanvasGroup?.alpha ?? -1}");
        Show();
    }

    /// <summary>
    /// Show the health bar and reset hide timer
    /// </summary>
    public void Show()
    {
        m_IsVisible = true;
        m_HideTimer = m_ShowDuration;
    }

    /// <summary>
    /// Immediately hide the health bar
    /// </summary>
    public void Hide()
    {
        m_IsVisible = false;
    }

    /// <summary>
    /// Create a health bar instance for a zombie
    /// </summary>
    public static ZombieHealthBar Create(Transform target)
    {
        // Create canvas
        GameObject healthBarObj = new GameObject("ZombieHealthBar");
        healthBarObj.layer = LayerMask.NameToLayer("Default"); // Ensure camera can see it

        Canvas canvas = healthBarObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;

        // Size in world units - 0.8m wide, 0.1m tall
        RectTransform canvasRect = healthBarObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(80f, 10f);
        canvasRect.localScale = Vector3.one * 0.01f; // 80 * 0.01 = 0.8 world units

        // Background with border
        GameObject bgObj = new GameObject("Background");
        bgObj.layer = healthBarObj.layer;
        bgObj.transform.SetParent(healthBarObj.transform, false);

        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.9f);

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Border
        GameObject borderObj = new GameObject("Border");
        borderObj.layer = healthBarObj.layer;
        borderObj.transform.SetParent(healthBarObj.transform, false);

        Image borderImage = borderObj.AddComponent<Image>();
        borderImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

        RectTransform borderRect = borderObj.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(2f, 2f);
        borderRect.offsetMax = new Vector2(-2f, -2f);

        // Fill (health)
        GameObject fillObj = new GameObject("Fill");
        fillObj.layer = healthBarObj.layer;
        fillObj.transform.SetParent(healthBarObj.transform, false);

        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = Color.green; // Start green (full health)
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.fillAmount = 1f;

        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(4f, 4f);
        fillRect.offsetMax = new Vector2(-4f, -4f);

        // Add CanvasGroup BEFORE ZombieHealthBar so Awake() finds it
        CanvasGroup canvasGroup = healthBarObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f; // Start visible

        // Add health bar component (Awake will find the CanvasGroup we just added)
        ZombieHealthBar healthBar = healthBarObj.AddComponent<ZombieHealthBar>();
        healthBar.m_FillImage = fillImage;
        healthBar.m_BackgroundImage = bgImage;
        healthBar.m_Canvas = canvas;
        healthBar.m_IsVisible = true; // Start visible
        healthBar.m_HideTimer = 5f; // Show for 5 seconds initially
        healthBar.Initialize(target);

        Debug.Log($"[ZombieHealthBar] Created health bar for {target.name} at scale {healthBarObj.transform.localScale}, position {healthBarObj.transform.position}");

        return healthBar;
    }
}
