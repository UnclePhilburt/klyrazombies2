using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple dot crosshair that stays in the center of the screen.
/// </summary>
public class Crosshair : MonoBehaviour
{
    [Header("Crosshair Settings")]
    [SerializeField] private float m_Size = 4f;
    [SerializeField] private Color m_Color = Color.white;
    [SerializeField] private Color m_HighlightColor = new Color(0.3f, 0.7f, 1f, 1f);

    private Image m_Image;
    private static Crosshair s_Instance;

    public static Crosshair Instance => s_Instance;

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
}
