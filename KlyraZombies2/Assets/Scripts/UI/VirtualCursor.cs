using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Virtual cursor controlled by gamepad right stick.
/// Simulates mouse input so all existing UIs work automatically.
/// </summary>
public class VirtualCursor : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] private float m_CursorSpeed = 1000f;
    [SerializeField] private float m_StickDeadzone = 0.15f;
    [SerializeField] private Color m_CursorColor = Color.white;
    [SerializeField] private float m_CursorSize = 32f;

    [Header("Input")]
    [SerializeField] private bool m_UseRightStick = true; // false = left stick

    // Singleton
    private static VirtualCursor s_Instance;
    public static VirtualCursor Instance => s_Instance;

    // UI elements
    private Canvas m_Canvas;
    private GameObject m_CursorObject;
    private RectTransform m_CursorRect;
    private Image m_CursorImage;

    // State
    private Vector2 m_CursorPosition;
    private bool m_IsActive = false;
    private PointerEventData m_PointerData;
    private List<RaycastResult> m_RaycastResults = new List<RaycastResult>();
    private GameObject m_CurrentHover;
    private GameObject m_CurrentPressed;
    private float m_ClickCooldown = 0f;

    private void Awake()
    {
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        s_Instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize cursor at screen center
        m_CursorPosition = new Vector2(Screen.width / 2f, Screen.height / 2f);

        // Subscribe to input mode changes
        InputModeManager.OnInputModeChanged += OnInputModeChanged;
    }

    private void Start()
    {
        CreateCursor();

        // Check initial mode
        if (InputModeManager.CurrentMode == InputModeManager.InputMode.Gamepad)
        {
            Activate();
        }
    }

    private void OnDestroy()
    {
        InputModeManager.OnInputModeChanged -= OnInputModeChanged;

        if (s_Instance == this)
            s_Instance = null;
    }

    private void OnInputModeChanged(InputModeManager.InputMode mode)
    {
        if (mode == InputModeManager.InputMode.Gamepad)
        {
            Activate();
        }
        else
        {
            Deactivate();
        }
    }

    private void CreateCursor()
    {
        // Create canvas for cursor
        var canvasObj = new GameObject("VirtualCursorCanvas");
        canvasObj.transform.SetParent(transform);
        m_Canvas = canvasObj.AddComponent<Canvas>();
        m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        m_Canvas.sortingOrder = 10000; // Always on top

        // Create cursor image
        m_CursorObject = new GameObject("Cursor");
        m_CursorObject.transform.SetParent(m_Canvas.transform, false);

        m_CursorRect = m_CursorObject.AddComponent<RectTransform>();
        m_CursorRect.sizeDelta = new Vector2(m_CursorSize, m_CursorSize);

        m_CursorImage = m_CursorObject.AddComponent<Image>();
        m_CursorImage.color = m_CursorColor;
        m_CursorImage.raycastTarget = false;

        // Create a simple cursor sprite (arrow shape)
        m_CursorImage.sprite = CreateCursorSprite();

        // Set pivot to top-left for proper positioning
        m_CursorRect.pivot = new Vector2(0f, 1f);

        m_CursorObject.SetActive(false);

        // Create pointer event data
        m_PointerData = new PointerEventData(EventSystem.current);
    }

    private Sprite CreateCursorSprite()
    {
        // Create a simple arrow cursor texture
        int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];

        // Clear to transparent
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(0, 0, 0, 0);

        // Draw arrow shape
        Color32 white = new Color32(255, 255, 255, 255);
        Color32 black = new Color32(0, 0, 0, 255);

        // Arrow points (top-left origin)
        int[] arrowX = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 8, 8, 8, 8, 9, 9, 9, 10, 10, 11 };
        int[] arrowY = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 3, 4, 5, 6, 7, 8, 9, 10, 11, 4, 5, 6, 7, 8, 9, 10, 11, 5, 6, 7, 8, 9, 10, 11, 6, 7, 8, 9, 10, 11, 7, 8, 9, 10, 11, 8, 9, 10, 11, 9, 10, 11, 10, 11, 11 };

        // Simplified: just draw a filled triangle
        for (int y = 0; y < 20; y++)
        {
            int width = y / 2 + 1;
            for (int x = 0; x < width && x < size; x++)
            {
                if (y < size)
                {
                    // White fill
                    pixels[(size - 1 - y) * size + x] = white;

                    // Black outline
                    if (x == 0 || x == width - 1 || y == 0 || y == 19)
                    {
                        pixels[(size - 1 - y) * size + x] = black;
                    }
                }
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0, 1), 100f);
    }

    public void Activate()
    {
        m_IsActive = true;

        if (m_CursorObject != null)
            m_CursorObject.SetActive(true);

        // Hide system cursor
        Cursor.visible = false;

        // Center cursor on activation
        m_CursorPosition = new Vector2(Screen.width / 2f, Screen.height / 2f);

        Debug.Log("[VirtualCursor] Activated");
    }

    public void Deactivate()
    {
        m_IsActive = false;

        if (m_CursorObject != null)
            m_CursorObject.SetActive(false);

        // Show system cursor
        Cursor.visible = true;

        // Clear any hover state
        if (m_CurrentHover != null)
        {
            ExecutePointerExit(m_CurrentHover);
            m_CurrentHover = null;
        }

        Debug.Log("[VirtualCursor] Deactivated");
    }

    private void Update()
    {
        if (!m_IsActive) return;
        if (Gamepad.current == null) return;

        // Update cooldown
        if (m_ClickCooldown > 0)
            m_ClickCooldown -= Time.unscaledDeltaTime;

        // Move cursor with stick
        MoveCursor();

        // Update visual position
        UpdateCursorVisual();

        // Update hover state
        UpdateHover();

        // Handle buttons
        HandleButtons();
    }

    private void MoveCursor()
    {
        var gamepad = Gamepad.current;
        if (gamepad == null) return;

        Vector2 stick = m_UseRightStick ? gamepad.rightStick.ReadValue() : gamepad.leftStick.ReadValue();

        // Apply deadzone
        if (stick.magnitude < m_StickDeadzone)
            return;

        // Move cursor
        m_CursorPosition += stick * m_CursorSpeed * Time.unscaledDeltaTime;

        // Clamp to screen
        m_CursorPosition.x = Mathf.Clamp(m_CursorPosition.x, 0, Screen.width);
        m_CursorPosition.y = Mathf.Clamp(m_CursorPosition.y, 0, Screen.height);
    }

    private void UpdateCursorVisual()
    {
        if (m_CursorRect == null) return;

        // Position cursor at screen position
        m_CursorRect.position = m_CursorPosition;
    }

    private void UpdateHover()
    {
        if (EventSystem.current == null) return;

        // Raycast to find what's under cursor
        m_PointerData.position = m_CursorPosition;
        m_RaycastResults.Clear();
        EventSystem.current.RaycastAll(m_PointerData, m_RaycastResults);

        GameObject newHover = null;
        if (m_RaycastResults.Count > 0)
        {
            newHover = m_RaycastResults[0].gameObject;
        }

        // Handle hover state change
        if (newHover != m_CurrentHover)
        {
            // Exit old hover
            if (m_CurrentHover != null)
            {
                ExecutePointerExit(m_CurrentHover);
            }

            // Enter new hover
            if (newHover != null)
            {
                ExecutePointerEnter(newHover);
            }

            m_CurrentHover = newHover;
        }
    }

    private void HandleButtons()
    {
        var gamepad = Gamepad.current;
        if (gamepad == null) return;

        // A button = Click
        if (gamepad.buttonSouth.wasPressedThisFrame && m_ClickCooldown <= 0)
        {
            SimulateClick();
            m_ClickCooldown = 0.1f; // Prevent double-clicks
        }

        if (gamepad.buttonSouth.wasReleasedThisFrame)
        {
            SimulateRelease();
        }

        // B button = Escape/Back
        if (gamepad.buttonEast.wasPressedThisFrame)
        {
            SimulateEscape();
        }

        // Triggers for scroll (optional)
        float scroll = 0;
        if (gamepad.rightTrigger.ReadValue() > 0.5f)
            scroll = -1f;
        else if (gamepad.leftTrigger.ReadValue() > 0.5f)
            scroll = 1f;

        if (scroll != 0)
        {
            SimulateScroll(scroll);
        }
    }

    private void SimulateClick()
    {
        if (m_CurrentHover == null) return;

        m_PointerData.position = m_CursorPosition;
        m_PointerData.button = PointerEventData.InputButton.Left;
        m_PointerData.pressPosition = m_CursorPosition;
        m_PointerData.pointerPressRaycast = m_RaycastResults.Count > 0 ? m_RaycastResults[0] : default;

        // Execute pointer down
        var pointerDownHandler = ExecuteEvents.GetEventHandler<IPointerDownHandler>(m_CurrentHover);
        if (pointerDownHandler != null)
        {
            ExecuteEvents.Execute(pointerDownHandler, m_PointerData, ExecuteEvents.pointerDownHandler);
        }

        m_CurrentPressed = m_CurrentHover;
        m_PointerData.pointerPress = m_CurrentPressed;

        // Also try to execute click immediately for buttons
        var clickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(m_CurrentHover);
        if (clickHandler != null)
        {
            ExecuteEvents.Execute(clickHandler, m_PointerData, ExecuteEvents.pointerClickHandler);
        }

        // Handle submit for selectables (buttons, etc.)
        var selectable = m_CurrentHover.GetComponent<Selectable>();
        if (selectable != null)
        {
            var submitHandler = ExecuteEvents.GetEventHandler<ISubmitHandler>(m_CurrentHover);
            if (submitHandler != null)
            {
                ExecuteEvents.Execute(submitHandler, m_PointerData, ExecuteEvents.submitHandler);
            }
        }

        // Handle drag begin
        var dragHandler = ExecuteEvents.GetEventHandler<IBeginDragHandler>(m_CurrentHover);
        if (dragHandler != null)
        {
            m_PointerData.dragging = true;
            m_PointerData.pointerDrag = dragHandler;
            ExecuteEvents.Execute(dragHandler, m_PointerData, ExecuteEvents.beginDragHandler);
        }
    }

    private void SimulateRelease()
    {
        m_PointerData.position = m_CursorPosition;

        // End drag if dragging
        if (m_PointerData.dragging && m_PointerData.pointerDrag != null)
        {
            ExecuteEvents.Execute(m_PointerData.pointerDrag, m_PointerData, ExecuteEvents.endDragHandler);

            // Drop on current hover
            if (m_CurrentHover != null)
            {
                var dropHandler = ExecuteEvents.GetEventHandler<IDropHandler>(m_CurrentHover);
                if (dropHandler != null)
                {
                    ExecuteEvents.Execute(dropHandler, m_PointerData, ExecuteEvents.dropHandler);
                }
            }

            m_PointerData.dragging = false;
            m_PointerData.pointerDrag = null;
        }

        // Pointer up
        if (m_CurrentPressed != null)
        {
            ExecuteEvents.Execute(m_CurrentPressed, m_PointerData, ExecuteEvents.pointerUpHandler);
            m_CurrentPressed = null;
        }

        m_PointerData.pointerPress = null;
    }

    private void SimulateEscape()
    {
        // Send cancel event to EventSystem
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
        {
            ExecuteEvents.Execute(EventSystem.current.currentSelectedGameObject,
                new BaseEventData(EventSystem.current), ExecuteEvents.cancelHandler);
        }

        // Also simulate Escape key for scripts using Input.GetKeyDown(KeyCode.Escape)
        // This is handled by UIInputActions.IsCancelPressed() which checks both
    }

    private void SimulateScroll(float delta)
    {
        if (m_CurrentHover == null) return;

        m_PointerData.scrollDelta = new Vector2(0, delta * 3f);

        var scrollHandler = ExecuteEvents.GetEventHandler<IScrollHandler>(m_CurrentHover);
        if (scrollHandler != null)
        {
            ExecuteEvents.Execute(scrollHandler, m_PointerData, ExecuteEvents.scrollHandler);
        }
    }

    private void ExecutePointerEnter(GameObject target)
    {
        m_PointerData.pointerEnter = target;
        ExecuteEvents.Execute(target, m_PointerData, ExecuteEvents.pointerEnterHandler);

        // Also highlight selectables
        var selectable = target.GetComponent<Selectable>();
        if (selectable != null)
        {
            selectable.OnPointerEnter(m_PointerData);
        }
    }

    private void ExecutePointerExit(GameObject target)
    {
        ExecuteEvents.Execute(target, m_PointerData, ExecuteEvents.pointerExitHandler);
        m_PointerData.pointerEnter = null;

        // Also unhighlight selectables
        var selectable = target.GetComponent<Selectable>();
        if (selectable != null)
        {
            selectable.OnPointerExit(m_PointerData);
        }
    }

    // Handle drag while button held
    private void LateUpdate()
    {
        if (!m_IsActive) return;
        if (!m_PointerData.dragging || m_PointerData.pointerDrag == null) return;

        m_PointerData.position = m_CursorPosition;
        ExecuteEvents.Execute(m_PointerData.pointerDrag, m_PointerData, ExecuteEvents.dragHandler);
    }

    /// <summary>
    /// Set cursor position programmatically
    /// </summary>
    public void SetPosition(Vector2 screenPosition)
    {
        m_CursorPosition = screenPosition;
    }

    /// <summary>
    /// Get current cursor position
    /// </summary>
    public Vector2 GetPosition()
    {
        return m_CursorPosition;
    }
}
