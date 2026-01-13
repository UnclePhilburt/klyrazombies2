using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages grid-based controller navigation for UI.
/// Supports multiple zones (equipment, inventory, container) with D-pad/stick navigation.
/// </summary>
public class UIControllerNavigator : MonoBehaviour
{
    [System.Serializable]
    public class NavigationZone
    {
        public string Name;
        public List<RectTransform> Slots = new List<RectTransform>();
        public int Columns = 1;
        public int SelectedIndex = 0;
        public bool WrapHorizontal = false;
        public bool WrapVertical = false;
    }

    [Header("Navigation Settings")]
    [SerializeField] private float m_NavigateRepeatDelay = 0.4f;
    [SerializeField] private float m_NavigateRepeatRate = 0.12f;
    [SerializeField] private float m_StickDeadzone = 0.5f;

    [Header("Visual Settings")]
    [SerializeField] private Color m_HighlightColor = new Color(1f, 0.84f, 0f, 1f); // Gold
    [SerializeField] private Color m_HeldItemColor = new Color(0f, 1f, 1f, 0.8f); // Cyan
    [SerializeField] private float m_HighlightBorderWidth = 3f;
    [SerializeField] private float m_PulseSpeed = 2f;
    [SerializeField] private float m_PulseMinAlpha = 0.6f;

    // Navigation state
    private List<NavigationZone> m_Zones = new List<NavigationZone>();
    private int m_CurrentZoneIndex = 0;
    private bool m_IsActive = false;

    // Input timing
    private Vector2 m_LastInputDirection;
    private float m_LastNavigateTime;
    private bool m_WaitingForRepeat;

    // Selection highlight
    private GameObject m_HighlightObject;
    private Image m_HighlightBorder;
    private RectTransform m_HighlightRect;
    private Canvas m_ParentCanvas;

    // Held item state (for select-then-act pattern)
    private int m_HeldItemZone = -1;
    private int m_HeldItemSlot = -1;
    private GameObject m_HeldHighlight;

    // Events
    public event Action<int, int> OnSelectionChanged; // zoneIndex, slotIndex
    public event Action<int, int> OnSlotSubmit; // zoneIndex, slotIndex
    public event Action<int, int> OnSlotQuickTransfer; // zoneIndex, slotIndex
    public event Action<int, int> OnSlotEquip; // zoneIndex, slotIndex
    public event Action OnCancel;
    public event Action<int> OnZoneChanged; // newZoneIndex

    // Public properties
    public int CurrentZoneIndex => m_CurrentZoneIndex;
    public int SelectedSlotIndex => m_Zones.Count > 0 && m_CurrentZoneIndex < m_Zones.Count
        ? m_Zones[m_CurrentZoneIndex].SelectedIndex : -1;
    public bool IsActive => m_IsActive;
    public bool HasHeldItem => m_HeldItemZone >= 0;
    public int HeldItemZone => m_HeldItemZone;
    public int HeldItemSlot => m_HeldItemSlot;

    private void Awake()
    {
        // Subscribe to input mode changes
        InputModeManager.OnInputModeChanged += OnInputModeChanged;
    }

    private void OnDestroy()
    {
        InputModeManager.OnInputModeChanged -= OnInputModeChanged;

        if (m_HighlightObject != null)
            Destroy(m_HighlightObject);
        if (m_HeldHighlight != null)
            Destroy(m_HeldHighlight);
    }

    private void OnInputModeChanged(InputModeManager.InputMode mode)
    {
        bool shouldBeActive = mode == InputModeManager.InputMode.Gamepad;

        if (shouldBeActive && !m_IsActive)
        {
            Activate();
        }
        else if (!shouldBeActive && m_IsActive)
        {
            Deactivate();
        }
    }

    private void Update()
    {
        if (!m_IsActive) return;
        if (m_Zones.Count == 0) return;

        // Handle navigation input
        HandleNavigation();

        // Handle action buttons
        HandleActions();

        // Update highlight visual
        UpdateHighlightVisual();
    }

    #region Public API

    /// <summary>
    /// Register a navigation zone (call this when UI is created)
    /// </summary>
    public int RegisterZone(string name, List<RectTransform> slots, int columns, bool wrapH = false, bool wrapV = false)
    {
        var zone = new NavigationZone
        {
            Name = name,
            Slots = new List<RectTransform>(slots),
            Columns = columns,
            SelectedIndex = 0,
            WrapHorizontal = wrapH,
            WrapVertical = wrapV
        };

        m_Zones.Add(zone);
        return m_Zones.Count - 1;
    }

    /// <summary>
    /// Clear all zones (call when UI closes)
    /// </summary>
    public void ClearZones()
    {
        m_Zones.Clear();
        m_CurrentZoneIndex = 0;
        ClearHeldItem();
    }

    /// <summary>
    /// Update slots in an existing zone (for dynamic slot counts)
    /// </summary>
    public void UpdateZoneSlots(int zoneIndex, List<RectTransform> slots)
    {
        if (zoneIndex >= 0 && zoneIndex < m_Zones.Count)
        {
            m_Zones[zoneIndex].Slots = new List<RectTransform>(slots);
            // Clamp selection if needed
            if (m_Zones[zoneIndex].SelectedIndex >= slots.Count)
            {
                m_Zones[zoneIndex].SelectedIndex = Mathf.Max(0, slots.Count - 1);
            }
        }
    }

    /// <summary>
    /// Set which zone is active for navigation
    /// </summary>
    public void SetActiveZone(int zoneIndex)
    {
        if (zoneIndex >= 0 && zoneIndex < m_Zones.Count && zoneIndex != m_CurrentZoneIndex)
        {
            m_CurrentZoneIndex = zoneIndex;
            OnZoneChanged?.Invoke(m_CurrentZoneIndex);
            UpdateHighlightPosition();
        }
    }

    /// <summary>
    /// Navigate to specific slot
    /// </summary>
    public void NavigateToSlot(int zoneIndex, int slotIndex)
    {
        if (zoneIndex >= 0 && zoneIndex < m_Zones.Count)
        {
            var zone = m_Zones[zoneIndex];
            if (slotIndex >= 0 && slotIndex < zone.Slots.Count)
            {
                m_CurrentZoneIndex = zoneIndex;
                zone.SelectedIndex = slotIndex;
                OnSelectionChanged?.Invoke(m_CurrentZoneIndex, slotIndex);
                UpdateHighlightPosition();
            }
        }
    }

    /// <summary>
    /// Get currently selected slot transform
    /// </summary>
    public RectTransform GetSelectedSlot()
    {
        if (m_Zones.Count == 0 || m_CurrentZoneIndex >= m_Zones.Count)
            return null;

        var zone = m_Zones[m_CurrentZoneIndex];
        if (zone.SelectedIndex >= 0 && zone.SelectedIndex < zone.Slots.Count)
            return zone.Slots[zone.SelectedIndex];

        return null;
    }

    /// <summary>
    /// Activate controller navigation (show highlight)
    /// </summary>
    public void Activate()
    {
        m_IsActive = true;
        EnsureHighlightCreated();
        UpdateHighlightPosition();

        if (m_HighlightObject != null)
            m_HighlightObject.SetActive(true);
    }

    /// <summary>
    /// Deactivate controller navigation (hide highlight)
    /// </summary>
    public void Deactivate()
    {
        m_IsActive = false;

        if (m_HighlightObject != null)
            m_HighlightObject.SetActive(false);
    }

    /// <summary>
    /// Mark an item as "held" for select-then-act pattern
    /// </summary>
    public void SetHeldItem(int zoneIndex, int slotIndex)
    {
        m_HeldItemZone = zoneIndex;
        m_HeldItemSlot = slotIndex;
        UpdateHeldHighlight();
    }

    /// <summary>
    /// Clear held item state
    /// </summary>
    public void ClearHeldItem()
    {
        m_HeldItemZone = -1;
        m_HeldItemSlot = -1;

        if (m_HeldHighlight != null)
            m_HeldHighlight.SetActive(false);
    }

    /// <summary>
    /// Set the parent canvas (needed for highlight positioning)
    /// </summary>
    public void SetParentCanvas(Canvas canvas)
    {
        m_ParentCanvas = canvas;
    }

    #endregion

    #region Navigation Logic

    private void HandleNavigation()
    {
        Vector2 input = GetNavigationInput();

        if (input == Vector2.zero)
        {
            m_LastInputDirection = Vector2.zero;
            m_WaitingForRepeat = false;
            return;
        }

        // Convert to cardinal direction
        Vector2 direction = GetCardinalDirection(input);

        // Check if this is a new direction or repeat
        bool isNewDirection = direction != m_LastInputDirection;
        float timeSinceLastNav = Time.unscaledTime - m_LastNavigateTime;

        bool shouldNavigate = false;

        if (isNewDirection)
        {
            shouldNavigate = true;
            m_WaitingForRepeat = true;
        }
        else if (m_WaitingForRepeat)
        {
            if (timeSinceLastNav >= m_NavigateRepeatDelay)
            {
                shouldNavigate = true;
                m_WaitingForRepeat = false;
            }
        }
        else
        {
            if (timeSinceLastNav >= m_NavigateRepeatRate)
            {
                shouldNavigate = true;
            }
        }

        if (shouldNavigate)
        {
            m_LastInputDirection = direction;
            m_LastNavigateTime = Time.unscaledTime;
            Navigate(direction);
        }
    }

    private Vector2 GetNavigationInput()
    {
        Vector2 input = Vector2.zero;

        // Gamepad
        if (UnityEngine.InputSystem.Gamepad.current != null)
        {
            var gp = UnityEngine.InputSystem.Gamepad.current;
            Vector2 dpad = gp.dpad.ReadValue();
            Vector2 stick = gp.leftStick.ReadValue();

            if (dpad.sqrMagnitude > 0.1f)
                input = dpad;
            else if (stick.sqrMagnitude > m_StickDeadzone * m_StickDeadzone)
                input = stick;
        }

        // Keyboard (add to input)
        if (Input.GetKey(KeyCode.UpArrow)) input.y = 1f;
        else if (Input.GetKey(KeyCode.DownArrow)) input.y = -1f;
        if (Input.GetKey(KeyCode.RightArrow)) input.x = 1f;
        else if (Input.GetKey(KeyCode.LeftArrow)) input.x = -1f;

        return input;
    }

    private Vector2 GetCardinalDirection(Vector2 input)
    {
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
        {
            return input.x > 0 ? Vector2.right : Vector2.left;
        }
        else
        {
            return input.y > 0 ? Vector2.up : Vector2.down;
        }
    }

    private void Navigate(Vector2 direction)
    {
        if (m_Zones.Count == 0 || m_CurrentZoneIndex >= m_Zones.Count)
            return;

        var zone = m_Zones[m_CurrentZoneIndex];
        if (zone.Slots.Count == 0) return;

        int currentIndex = zone.SelectedIndex;
        int columns = zone.Columns;
        int rows = Mathf.CeilToInt((float)zone.Slots.Count / columns);

        int col = currentIndex % columns;
        int row = currentIndex / columns;

        int newCol = col;
        int newRow = row;

        // Handle direction
        if (direction == Vector2.right)
        {
            newCol++;
            if (newCol >= columns)
            {
                if (zone.WrapHorizontal)
                    newCol = 0;
                else
                {
                    // Try switching to next zone
                    TrySwitchZone(1);
                    return;
                }
            }
        }
        else if (direction == Vector2.left)
        {
            newCol--;
            if (newCol < 0)
            {
                if (zone.WrapHorizontal)
                    newCol = columns - 1;
                else
                {
                    // Try switching to previous zone
                    TrySwitchZone(-1);
                    return;
                }
            }
        }
        else if (direction == Vector2.up)
        {
            newRow--;
            if (newRow < 0)
            {
                if (zone.WrapVertical)
                    newRow = rows - 1;
                else
                    return; // Can't go up
            }
        }
        else if (direction == Vector2.down)
        {
            newRow++;
            if (newRow >= rows)
            {
                if (zone.WrapVertical)
                    newRow = 0;
                else
                    return; // Can't go down
            }
        }

        int newIndex = newRow * columns + newCol;

        // Clamp to valid slots
        if (newIndex >= 0 && newIndex < zone.Slots.Count)
        {
            zone.SelectedIndex = newIndex;
            OnSelectionChanged?.Invoke(m_CurrentZoneIndex, newIndex);
            UpdateHighlightPosition();
        }
    }

    private void TrySwitchZone(int direction)
    {
        int newZone = m_CurrentZoneIndex + direction;

        if (newZone >= 0 && newZone < m_Zones.Count)
        {
            m_CurrentZoneIndex = newZone;
            OnZoneChanged?.Invoke(m_CurrentZoneIndex);
            UpdateHighlightPosition();
        }
    }

    #endregion

    #region Action Handling

    private void HandleActions()
    {
        // Submit (A button)
        if (UIInputActions.IsSubmitPressed())
        {
            var zone = m_Zones[m_CurrentZoneIndex];
            OnSlotSubmit?.Invoke(m_CurrentZoneIndex, zone.SelectedIndex);
        }

        // Cancel (B button)
        if (UIInputActions.IsCancelPressed())
        {
            if (HasHeldItem)
            {
                ClearHeldItem();
            }
            else
            {
                OnCancel?.Invoke();
            }
        }

        // Quick Transfer (LB)
        if (UIInputActions.IsQuickTransferPressed())
        {
            var zone = m_Zones[m_CurrentZoneIndex];
            OnSlotQuickTransfer?.Invoke(m_CurrentZoneIndex, zone.SelectedIndex);
        }

        // Equip (RB)
        if (UIInputActions.IsEquipPressed())
        {
            var zone = m_Zones[m_CurrentZoneIndex];
            OnSlotEquip?.Invoke(m_CurrentZoneIndex, zone.SelectedIndex);
        }
    }

    #endregion

    #region Visual Highlight

    private void EnsureHighlightCreated()
    {
        if (m_HighlightObject != null) return;

        // Find canvas
        if (m_ParentCanvas == null)
            m_ParentCanvas = GetComponentInParent<Canvas>();

        if (m_ParentCanvas == null) return;

        // Create highlight object
        m_HighlightObject = new GameObject("ControllerHighlight");
        m_HighlightObject.transform.SetParent(m_ParentCanvas.transform, false);

        m_HighlightRect = m_HighlightObject.AddComponent<RectTransform>();

        // Create border image
        m_HighlightBorder = m_HighlightObject.AddComponent<Image>();
        m_HighlightBorder.color = m_HighlightColor;
        m_HighlightBorder.raycastTarget = false;

        // Create border sprite (outline)
        m_HighlightBorder.sprite = CreateBorderSprite();
        m_HighlightBorder.type = Image.Type.Sliced;
        m_HighlightBorder.pixelsPerUnitMultiplier = 1f;

        // Set to render on top
        var canvas = m_HighlightObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000;

        m_HighlightObject.SetActive(false);
    }

    private Sprite CreateBorderSprite()
    {
        // Create a simple border texture
        int size = 32;
        int border = 4;
        var tex = new Texture2D(size, size);
        var pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isBorder = x < border || x >= size - border || y < border || y >= size - border;
                pixels[y * size + x] = isBorder ? Color.white : Color.clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
    }

    private void UpdateHighlightPosition()
    {
        if (m_HighlightObject == null || !m_IsActive) return;

        var slot = GetSelectedSlot();
        if (slot == null)
        {
            m_HighlightObject.SetActive(false);
            return;
        }

        m_HighlightObject.SetActive(true);

        // Position highlight over slot
        m_HighlightRect.position = slot.position;
        m_HighlightRect.sizeDelta = slot.sizeDelta + new Vector2(m_HighlightBorderWidth * 2, m_HighlightBorderWidth * 2);
    }

    private void UpdateHighlightVisual()
    {
        if (m_HighlightBorder == null) return;

        // Pulse effect
        float pulse = (Mathf.Sin(Time.unscaledTime * m_PulseSpeed * Mathf.PI) + 1f) / 2f;
        float alpha = Mathf.Lerp(m_PulseMinAlpha, 1f, pulse);

        Color color = m_HighlightColor;
        color.a = alpha;
        m_HighlightBorder.color = color;
    }

    private void UpdateHeldHighlight()
    {
        if (m_HeldItemZone < 0 || m_HeldItemSlot < 0)
        {
            if (m_HeldHighlight != null)
                m_HeldHighlight.SetActive(false);
            return;
        }

        // Create held highlight if needed
        if (m_HeldHighlight == null && m_ParentCanvas != null)
        {
            m_HeldHighlight = new GameObject("HeldItemHighlight");
            m_HeldHighlight.transform.SetParent(m_ParentCanvas.transform, false);

            var rect = m_HeldHighlight.AddComponent<RectTransform>();
            var img = m_HeldHighlight.AddComponent<Image>();
            img.color = m_HeldItemColor;
            img.sprite = CreateBorderSprite();
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;

            var canvas = m_HeldHighlight.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 999;
        }

        // Position over held slot
        if (m_HeldItemZone < m_Zones.Count)
        {
            var zone = m_Zones[m_HeldItemZone];
            if (m_HeldItemSlot < zone.Slots.Count)
            {
                var slot = zone.Slots[m_HeldItemSlot];
                var rect = m_HeldHighlight.GetComponent<RectTransform>();
                rect.position = slot.position;
                rect.sizeDelta = slot.sizeDelta + new Vector2(m_HighlightBorderWidth * 2, m_HighlightBorderWidth * 2);
                m_HeldHighlight.SetActive(true);
            }
        }
    }

    #endregion
}
