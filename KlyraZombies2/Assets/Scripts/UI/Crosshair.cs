using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple dot crosshair that stays in the center of the screen.
/// Auto-creates itself in gameplay scenes.
/// </summary>
public class Crosshair : MonoBehaviour
{
    [Header("Crosshair Settings")]
    [SerializeField] private float m_Size = 4f;
    [SerializeField] private Color m_Color = Color.white;
    [SerializeField] private Color m_HighlightColor = new Color(0.3f, 0.7f, 1f, 1f);

    private Image m_Image;
    private static Crosshair s_Instance;
    private static bool s_SceneHandlerRegistered = false;

    public static Crosshair Instance => s_Instance;

    /// <summary>
    /// Auto-creates the crosshair when a gameplay scene loads.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        // Register for scene loads to handle scene transitions
        if (!s_SceneHandlerRegistered)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            s_SceneHandlerRegistered = true;
        }

        TryCreateCrosshair();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Small delay to let scene objects initialize (like CharacterSpawner spawning player)
        if (s_Instance == null)
        {
            // Use a coroutine helper to delay the check
            var helper = new GameObject("CrosshairInitHelper").AddComponent<CrosshairInitHelper>();
            helper.Initialize();
        }
    }

    private static void TryCreateCrosshair()
    {
        // Don't create if already exists
        if (s_Instance != null) return;

        // Check if there's a player in the scene (indicates gameplay scene)
        if (GameObject.FindWithTag("Player") != null || FindFirstObjectByType<CharacterSpawner>() != null)
        {
            CreateCrosshair();
        }
    }

    private static void CreateCrosshair()
    {
        // Find or create canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("HUD Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        // Create crosshair object
        GameObject crosshairObj = new GameObject("Crosshair");
        crosshairObj.transform.SetParent(canvas.transform, false);

        RectTransform rect = crosshairObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(6, 6);

        crosshairObj.AddComponent<Crosshair>();
    }

    /// <summary>
    /// Set crosshair to highlight color (when looking at interactable)
    /// </summary>
    public void SetHighlighted(bool highlighted)
    {
        if (m_Image != null)
        {
            m_Image.color = highlighted ? m_HighlightColor : m_Color;
        }
    }

    private void Awake()
    {
        s_Instance = this;

        // Create the crosshair image if not already set up
        m_Image = GetComponent<Image>();
        if (m_Image == null)
        {
            m_Image = gameObject.AddComponent<Image>();
        }

        // Create a simple circle sprite
        m_Image.sprite = CreateCircleSprite();
        m_Image.color = m_Color;
        m_Image.raycastTarget = false;

        // Set size
        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(m_Size, m_Size);
        }
    }

    private Sprite CreateCircleSprite()
    {
        // Create a small white circle texture
        int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        float center = size / 2f;
        float radius = size / 2f - 1;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= radius)
                {
                    float alpha = Mathf.Clamp01((radius - dist) / 2f); // Soft edge
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
    }

    private void OnDestroy()
    {
        if (s_Instance == this)
        {
            s_Instance = null;
        }
    }

    /// <summary>
    /// Make TryCreateCrosshair accessible for the helper
    /// </summary>
    public static void EnsureExists()
    {
        TryCreateCrosshair();
    }
}

/// <summary>
/// Helper to delay crosshair creation until player spawns
/// </summary>
public class CrosshairInitHelper : MonoBehaviour
{
    private float m_Timer = 0f;
    private const float MAX_WAIT = 2f;

    public void Initialize()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        m_Timer += Time.deltaTime;

        // Check periodically if player exists
        if (GameObject.FindWithTag("Player") != null || m_Timer > MAX_WAIT)
        {
            Crosshair.EnsureExists();
            Destroy(gameObject);
        }
    }
}
