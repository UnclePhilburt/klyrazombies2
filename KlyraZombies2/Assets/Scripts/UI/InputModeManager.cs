using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Detects and tracks whether the player is using keyboard/mouse or gamepad.
/// Singleton that persists across scenes.
/// </summary>
public class InputModeManager : MonoBehaviour
{
    public enum InputMode
    {
        KeyboardMouse,
        Gamepad
    }

    // Singleton
    private static InputModeManager s_Instance;
    public static InputModeManager Instance
    {
        get
        {
            if (s_Instance == null)
            {
                // Try to find existing
                s_Instance = FindFirstObjectByType<InputModeManager>();

                // Create if not found
                if (s_Instance == null)
                {
                    var go = new GameObject("InputModeManager");
                    s_Instance = go.AddComponent<InputModeManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return s_Instance;
        }
    }

    // Current mode (static for easy access)
    private static InputMode s_CurrentMode = InputMode.KeyboardMouse;
    public static InputMode CurrentMode => s_CurrentMode;

    // Event fired when mode changes
    public static event Action<InputMode> OnInputModeChanged;

    // Settings
    [SerializeField] private float m_StickDeadzone = 0.3f;

    private void Awake()
    {
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_Instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize based on whether a gamepad is connected
        if (Gamepad.current != null)
        {
            // Start in keyboard mode but ready to switch
            s_CurrentMode = InputMode.KeyboardMouse;
        }

        // Create VirtualCursor if it doesn't exist
        if (VirtualCursor.Instance == null)
        {
            var cursorObj = new GameObject("VirtualCursor");
            cursorObj.AddComponent<VirtualCursor>();
            DontDestroyOnLoad(cursorObj);
        }
    }

    private void Update()
    {
        DetectInputMode();
    }

    private void DetectInputMode()
    {
        InputMode newMode = s_CurrentMode;

        // Check for gamepad input
        if (HasGamepadInput())
        {
            newMode = InputMode.Gamepad;
        }
        // Check for keyboard/mouse input
        else if (HasKeyboardMouseInput())
        {
            newMode = InputMode.KeyboardMouse;
        }

        // Fire event if mode changed
        if (newMode != s_CurrentMode)
        {
            s_CurrentMode = newMode;
            Debug.Log($"[InputModeManager] Switched to {s_CurrentMode}");

            // Update cursor visibility
            UpdateCursor();

            // Fire event
            OnInputModeChanged?.Invoke(s_CurrentMode);
        }
    }

    private bool HasGamepadInput()
    {
        if (Gamepad.current == null) return false;

        var gp = Gamepad.current;

        // Check any button press
        if (gp.buttonSouth.wasPressedThisFrame ||
            gp.buttonEast.wasPressedThisFrame ||
            gp.buttonWest.wasPressedThisFrame ||
            gp.buttonNorth.wasPressedThisFrame ||
            gp.startButton.wasPressedThisFrame ||
            gp.selectButton.wasPressedThisFrame ||
            gp.leftShoulder.wasPressedThisFrame ||
            gp.rightShoulder.wasPressedThisFrame ||
            gp.leftTrigger.wasPressedThisFrame ||
            gp.rightTrigger.wasPressedThisFrame ||
            gp.leftStickButton.wasPressedThisFrame ||
            gp.rightStickButton.wasPressedThisFrame)
            return true;

        // Check D-pad
        if (gp.dpad.up.wasPressedThisFrame ||
            gp.dpad.down.wasPressedThisFrame ||
            gp.dpad.left.wasPressedThisFrame ||
            gp.dpad.right.wasPressedThisFrame)
            return true;

        // Check sticks with deadzone
        if (gp.leftStick.ReadValue().sqrMagnitude > m_StickDeadzone * m_StickDeadzone ||
            gp.rightStick.ReadValue().sqrMagnitude > m_StickDeadzone * m_StickDeadzone)
            return true;

        return false;
    }

    private bool HasKeyboardMouseInput()
    {
        // Mouse movement
        if (Mathf.Abs(Input.GetAxisRaw("Mouse X")) > 0.05f ||
            Mathf.Abs(Input.GetAxisRaw("Mouse Y")) > 0.05f)
            return true;

        // Mouse buttons
        if (Input.GetMouseButtonDown(0) ||
            Input.GetMouseButtonDown(1) ||
            Input.GetMouseButtonDown(2))
            return true;

        // Any keyboard key (but not during gamepad use)
        if (Input.anyKeyDown)
        {
            // Exclude gamepad-mapped keys if gamepad is connected
            // This is a simple heuristic - any key press counts as keyboard
            return true;
        }

        return false;
    }

    private void UpdateCursor()
    {
        // Only manage cursor when in menus (check if any UI is open)
        // For now, let individual UIs handle cursor visibility
        // This just sets a baseline

        if (s_CurrentMode == InputMode.Gamepad)
        {
            // Hide cursor in gamepad mode (UI will show selection highlight instead)
            // But only if a menu is open - gameplay handles its own cursor
        }
        else
        {
            // Show cursor in keyboard/mouse mode for menus
        }
    }

    /// <summary>
    /// Force a mode switch (useful for testing)
    /// </summary>
    public static void ForceMode(InputMode mode)
    {
        if (s_CurrentMode != mode)
        {
            s_CurrentMode = mode;
            OnInputModeChanged?.Invoke(s_CurrentMode);
        }
    }

    /// <summary>
    /// Check if we should use gamepad-style navigation
    /// </summary>
    public static bool UseGamepadNavigation => s_CurrentMode == InputMode.Gamepad;
}
