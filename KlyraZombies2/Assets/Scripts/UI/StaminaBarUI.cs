using UnityEngine;
using UnityEngine.UI;
using Opsive.Shared.Events;
using Opsive.UltimateCharacterController.Traits;

/// <summary>
/// Simple stamina bar UI that displays a Stamina attribute.
/// </summary>
public class StaminaBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider m_StaminaSlider;
    [SerializeField] private Image m_FillImage;
    [SerializeField] private GameObject m_Character;

    [Header("Settings")]
    [SerializeField] private string m_StaminaAttributeName = "Stamina";
    [SerializeField] private float m_SmoothSpeed = 10f;
    [SerializeField] private bool m_HideWhenFull = true;
    [SerializeField] private float m_HideDelay = 2f;

    [Header("Colors")]
    [SerializeField] private Color m_FullColor = Color.green;
    [SerializeField] private Color m_MidColor = Color.yellow;
    [SerializeField] private Color m_LowColor = Color.red;
    [SerializeField] private float m_LowThreshold = 0.25f;
    [SerializeField] private float m_MidThreshold = 0.5f;

    private AttributeManager m_AttributeManager;
    private Attribute m_StaminaAttribute;
    private CanvasGroup m_CanvasGroup;
    private float m_TargetValue;
    private float m_HideTimer;
    private bool m_IsVisible = true;

    private void Awake()
    {
        m_CanvasGroup = GetComponent<CanvasGroup>();
        if (m_CanvasGroup == null)
        {
            m_CanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (m_StaminaSlider == null)
        {
            m_StaminaSlider = GetComponentInChildren<Slider>();
        }

        if (m_FillImage == null && m_StaminaSlider != null)
        {
            m_FillImage = m_StaminaSlider.fillRect?.GetComponent<Image>();
        }
    }

    private void Start()
    {
        StartCoroutine(Initialize());
    }

    private System.Collections.IEnumerator Initialize()
    {
        // Wait for StaminaSystem to create the attribute
        yield return new WaitForSeconds(0.5f);

        // Find character if not assigned
        if (m_Character == null)
        {
            m_Character = GameObject.FindGameObjectWithTag("Player");
        }

        if (m_Character != null)
        {
            m_AttributeManager = m_Character.GetComponent<AttributeManager>();
            if (m_AttributeManager == null)
                m_AttributeManager = m_Character.GetComponentInChildren<AttributeManager>();
            if (m_AttributeManager == null)
                m_AttributeManager = m_Character.GetComponentInParent<AttributeManager>();

            if (m_AttributeManager != null)
            {
                // Retry a few times to find the attribute
                for (int i = 0; i < 10; i++)
                {
                    m_StaminaAttribute = m_AttributeManager.GetAttribute(m_StaminaAttributeName);
                    if (m_StaminaAttribute != null) break;
                    yield return new WaitForSeconds(0.1f);
                }

                if (m_StaminaAttribute != null)
                {
                    Debug.Log($"StaminaBarUI: Found Stamina attribute with value {m_StaminaAttribute.Value}");
                    EventHandler.RegisterEvent<Attribute>(m_AttributeManager.gameObject, "OnAttributeUpdateValue", OnAttributeUpdate);
                    UpdateBar(GetNormalizedValue());
                    gameObject.SetActive(true);
                }
            }
        }

        if (m_StaminaAttribute == null)
        {
            Debug.LogWarning("StaminaBarUI: Could not find Stamina attribute.");
            gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (m_AttributeManager != null)
        {
            EventHandler.UnregisterEvent<Attribute>(m_AttributeManager.gameObject, "OnAttributeUpdateValue", OnAttributeUpdate);
        }
    }

    private void Update()
    {
        // Smooth slider movement
        if (m_StaminaSlider != null && Mathf.Abs(m_StaminaSlider.value - m_TargetValue) > 0.001f)
        {
            m_StaminaSlider.value = Mathf.Lerp(m_StaminaSlider.value, m_TargetValue, Time.deltaTime * m_SmoothSpeed);
            UpdateColor(m_StaminaSlider.value);
        }

        // Handle visibility
        if (m_HideWhenFull)
        {
            if (m_TargetValue >= 0.99f)
            {
                m_HideTimer += Time.deltaTime;
                if (m_HideTimer >= m_HideDelay && m_IsVisible)
                {
                    SetVisible(false);
                }
            }
            else
            {
                m_HideTimer = 0f;
                if (!m_IsVisible)
                {
                    SetVisible(true);
                }
            }
        }
    }

    private void OnAttributeUpdate(Attribute attribute)
    {
        if (attribute.Name == m_StaminaAttributeName)
        {
            m_TargetValue = GetNormalizedValue();
        }
    }

    private float GetNormalizedValue()
    {
        if (m_StaminaAttribute == null) return 1f;
        return (m_StaminaAttribute.Value - m_StaminaAttribute.MinValue) /
               (m_StaminaAttribute.MaxValue - m_StaminaAttribute.MinValue);
    }

    private void UpdateBar(float value)
    {
        m_TargetValue = value;
        if (m_StaminaSlider != null)
        {
            m_StaminaSlider.value = value;
        }
        UpdateColor(value);
    }

    private void UpdateColor(float value)
    {
        if (m_FillImage == null) return;

        if (value <= m_LowThreshold)
        {
            m_FillImage.color = m_LowColor;
        }
        else if (value <= m_MidThreshold)
        {
            m_FillImage.color = Color.Lerp(m_LowColor, m_MidColor, (value - m_LowThreshold) / (m_MidThreshold - m_LowThreshold));
        }
        else
        {
            m_FillImage.color = Color.Lerp(m_MidColor, m_FullColor, (value - m_MidThreshold) / (1f - m_MidThreshold));
        }
    }

    private void SetVisible(bool visible)
    {
        m_IsVisible = visible;
        if (m_CanvasGroup != null)
        {
            m_CanvasGroup.alpha = visible ? 1f : 0f;
        }
    }
}
