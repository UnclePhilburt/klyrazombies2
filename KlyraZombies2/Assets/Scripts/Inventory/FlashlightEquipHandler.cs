using UnityEngine;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;

/// <summary>
/// Handles flashlight equip/unequip - enables a headlamp-style light when flashlight is equipped.
/// Attach to the player character.
/// </summary>
public class FlashlightEquipHandler : MonoBehaviour
{
    [Header("Light Settings")]
    [SerializeField] private float m_LightRange = 25f;
    [SerializeField] private float m_LightIntensity = 2f;
    [SerializeField] private float m_SpotAngle = 45f;
    [SerializeField] private float m_InnerSpotAngle = 25f;
    [SerializeField] private Color m_LightColor = new Color(1f, 0.95f, 0.85f);
    [SerializeField] private LightShadows m_Shadows = LightShadows.Soft;

    [Header("Attachment")]
    [SerializeField] private Transform m_AttachmentPoint; // Drag head bone or empty GameObject here
    [SerializeField] private Vector3 m_LightOffset = new Vector3(0f, 0.1f, 0.1f);
    [SerializeField] private Vector3 m_LightRotation = Vector3.zero;
    [SerializeField] private bool m_UseCameraFallback = true; // Fall back to camera if no attachment point

    [Header("Input")]
    [SerializeField] private KeyCode m_ToggleKey = KeyCode.F;

    private Light m_FlashlightLight;
    private GameObject m_LightObject;
    private Transform m_ActiveAttachment;
    private bool m_IsEquipped = false;

    private void Start()
    {
        // Determine attachment point
        if (m_AttachmentPoint != null)
        {
            m_ActiveAttachment = m_AttachmentPoint;
        }
        else if (m_UseCameraFallback)
        {
            m_ActiveAttachment = Camera.main?.transform;
        }

        // Create the light object (disabled by default)
        CreateFlashlightLight();

        // Check if flashlight is already equipped at start
        CheckForEquippedFlashlight();
    }

    private void OnDestroy()
    {
        if (m_LightObject != null)
            Destroy(m_LightObject);
    }

    private void CreateFlashlightLight()
    {
        m_LightObject = new GameObject("FlashlightLight");
        m_FlashlightLight = m_LightObject.AddComponent<Light>();

        m_FlashlightLight.type = LightType.Spot;
        m_FlashlightLight.range = m_LightRange;
        m_FlashlightLight.intensity = m_LightIntensity;
        m_FlashlightLight.spotAngle = m_SpotAngle;
        m_FlashlightLight.innerSpotAngle = m_InnerSpotAngle;
        m_FlashlightLight.color = m_LightColor;
        m_FlashlightLight.shadows = m_Shadows;

        // Parent to attachment point (head bone or camera)
        if (m_ActiveAttachment != null)
        {
            m_LightObject.transform.SetParent(m_ActiveAttachment);
            m_LightObject.transform.localPosition = m_LightOffset;
            m_LightObject.transform.localRotation = Quaternion.Euler(m_LightRotation);
        }

        // Start disabled
        m_LightObject.SetActive(false);
    }

    private void CheckForEquippedFlashlight()
    {
        // Try to find inventory on this object or player
        var inventory = GetComponent<Inventory>();
        if (inventory == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                inventory = player.GetComponent<Inventory>();
        }

        if (inventory == null) return;

        var equipCollection = inventory.GetItemCollection("Equippable");
        if (equipCollection == null) return;

        foreach (var itemStack in equipCollection.GetAllItemStacks())
        {
            if (itemStack?.Item == null) continue;
            if (IsFlashlightItem(itemStack.Item.name))
            {
                EnableFlashlight();
                return;
            }
        }
    }

    private bool IsFlashlightItem(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return false;

        string lowerName = itemName.ToLower();
        return lowerName.Contains("flashlight") || lowerName.Contains("torch") || lowerName.Contains("headlamp");
    }

    private void EnableFlashlight()
    {
        if (m_IsEquipped) return;

        m_IsEquipped = true;
        if (m_LightObject != null)
        {
            m_LightObject.SetActive(true);
            Debug.Log("[FlashlightEquipHandler] Flashlight enabled");
        }
    }

    private void DisableFlashlight()
    {
        if (!m_IsEquipped) return;

        m_IsEquipped = false;
        if (m_LightObject != null)
        {
            m_LightObject.SetActive(false);
            Debug.Log("[FlashlightEquipHandler] Flashlight disabled");
        }
    }

    private void Update()
    {
        // Toggle flashlight with key when equipped
        if (m_IsEquipped && Input.GetKeyDown(m_ToggleKey))
        {
            ToggleFlashlight();
        }
    }

    private void LateUpdate()
    {
        // Re-parent to attachment point if needed (in case it changes)
        if (m_IsEquipped && m_LightObject != null && m_ActiveAttachment != null)
        {
            if (m_LightObject.transform.parent != m_ActiveAttachment)
            {
                m_LightObject.transform.SetParent(m_ActiveAttachment);
                m_LightObject.transform.localPosition = m_LightOffset;
                m_LightObject.transform.localRotation = Quaternion.Euler(m_LightRotation);
            }
        }
    }

    /// <summary>
    /// Called by SimpleLootUI/SimpleInventoryUI when flashlight is equipped via UI.
    /// </summary>
    public void OnFlashlightEquipped()
    {
        EnableFlashlight();
    }

    /// <summary>
    /// Called by SimpleLootUI/SimpleInventoryUI when flashlight is unequipped via UI.
    /// </summary>
    public void OnFlashlightUnequipped()
    {
        DisableFlashlight();
    }

    /// <summary>
    /// Toggle flashlight on/off (for keybind).
    /// </summary>
    public void ToggleFlashlight()
    {
        if (!m_IsEquipped) return;

        if (m_LightObject != null)
        {
            bool newState = !m_LightObject.activeSelf;
            m_LightObject.SetActive(newState);
            Debug.Log($"[FlashlightEquipHandler] Flashlight toggled {(newState ? "ON" : "OFF")}");
        }
    }

    public bool IsFlashlightEquipped => m_IsEquipped;
    public bool IsFlashlightOn => m_LightObject != null && m_LightObject.activeSelf;
}
