using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Static helper class that abstracts UI input for both keyboard/mouse and gamepad.
/// Checks both New Input System gamepad and legacy Input for keyboard.
/// </summary>
public static class UIInputActions
{
    // Deadzone for analog stick navigation
    private const float STICK_DEADZONE = 0.5f;

    // Track previous frame's stick state to detect "press" vs "held"
    private static Vector2 s_LastStickInput;
    private static float s_LastNavigateTime;
    private static float s_NavigateRepeatDelay = 0.4f;
    private static float s_NavigateRepeatRate = 0.15f;

    /// <summary>
    /// Returns true on the frame Submit is pressed (A button, Enter, or Space)
    /// </summary>
    public static bool IsSubmitPressed()
    {
        // Gamepad A button
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
            return true;

        // Keyboard
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            return true;

        return false;
    }

    /// <summary>
    /// Returns true on the frame Cancel is pressed (B button, Escape)
    /// </summary>
    public static bool IsCancelPressed()
    {
        // Gamepad B button
        if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
            return true;

        // Keyboard
        if (Input.GetKeyDown(KeyCode.Escape))
            return true;

        return false;
    }

    /// <summary>
    /// Returns true on the frame Quick Transfer is pressed (LB/L1)
    /// </summary>
    public static bool IsQuickTransferPressed()
    {
        // Gamepad LB
        if (Gamepad.current != null && Gamepad.current.leftShoulder.wasPressedThisFrame)
            return true;

        // Keyboard alternative - Shift+Click handled elsewhere, Q for quick transfer
        if (Input.GetKeyDown(KeyCode.Q))
            return true;

        return false;
    }

    /// <summary>
    /// Returns true on the frame Equip is pressed (RB/R1)
    /// </summary>
    public static bool IsEquipPressed()
    {
        // Gamepad RB
        if (Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame)
            return true;

        // Keyboard alternative
        if (Input.GetKeyDown(KeyCode.E))
            return true;

        return false;
    }

    /// <summary>
    /// Returns true on the frame Open Panel is pressed (Start button, Tab)
    /// </summary>
    public static bool IsOpenPanelPressed()
    {
        // Gamepad Start
        if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
            return true;

        // Keyboard
        if (Input.GetKeyDown(KeyCode.Tab))
            return true;

        return false;
    }

    /// <summary>
    /// Returns true on the frame Interact is pressed (Y button, F key)
    /// Used for opening/closing loot containers
    /// </summary>
    public static bool IsInteractPressed()
    {
        // Gamepad Y button (or B for closing)
        if (Gamepad.current != null)
        {
            if (Gamepad.current.buttonNorth.wasPressedThisFrame)
                return true;
        }

        // Keyboard
        if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.E))
            return true;

        return false;
    }

    /// <summary>
    /// Returns navigation input with repeat handling for held directions.
    /// Returns Vector2.zero when no navigation, otherwise cardinal direction.
    /// </summary>
    public static Vector2 GetNavigationWithRepeat()
    {
        Vector2 rawInput = GetRawNavigation();

        // Convert to cardinal direction
        Vector2 cardinalInput = Vector2.zero;
        if (Mathf.Abs(rawInput.x) > Mathf.Abs(rawInput.y))
        {
            if (rawInput.x > STICK_DEADZONE) cardinalInput = Vector2.right;
            else if (rawInput.x < -STICK_DEADZONE) cardinalInput = Vector2.left;
        }
        else
        {
            if (rawInput.y > STICK_DEADZONE) cardinalInput = Vector2.up;
            else if (rawInput.y < -STICK_DEADZONE) cardinalInput = Vector2.down;
        }

        // Handle repeat timing
        bool wasActive = s_LastStickInput != Vector2.zero;
        bool isActive = cardinalInput != Vector2.zero;

        if (!isActive)
        {
            s_LastStickInput = Vector2.zero;
            return Vector2.zero;
        }

        // Direction changed or first press
        if (cardinalInput != s_LastStickInput)
        {
            s_LastStickInput = cardinalInput;
            s_LastNavigateTime = Time.unscaledTime;
            return cardinalInput;
        }

        // Same direction held - check repeat timing
        float elapsed = Time.unscaledTime - s_LastNavigateTime;
        if (!wasActive)
        {
            // First press
            s_LastNavigateTime = Time.unscaledTime;
            return cardinalInput;
        }
        else if (elapsed > s_NavigateRepeatDelay)
        {
            // Repeating
            float repeatElapsed = elapsed - s_NavigateRepeatDelay;
            int repeatCount = Mathf.FloorToInt(repeatElapsed / s_NavigateRepeatRate);
            float expectedTime = s_NavigateRepeatDelay + (repeatCount + 1) * s_NavigateRepeatRate;

            if (elapsed >= s_NavigateRepeatDelay + repeatCount * s_NavigateRepeatRate &&
                elapsed < s_NavigateRepeatDelay + (repeatCount + 1) * s_NavigateRepeatRate)
            {
                // Only return input on the frame we cross a repeat threshold
                if (Time.unscaledTime - s_LastNavigateTime >= expectedTime - s_NavigateRepeatRate)
                {
                    return cardinalInput;
                }
            }
        }

        return Vector2.zero;
    }

    /// <summary>
    /// Returns raw navigation input (D-pad, left stick, or arrow keys)
    /// </summary>
    public static Vector2 GetRawNavigation()
    {
        Vector2 input = Vector2.zero;

        // Gamepad D-pad and left stick
        if (Gamepad.current != null)
        {
            Vector2 dpad = Gamepad.current.dpad.ReadValue();
            Vector2 stick = Gamepad.current.leftStick.ReadValue();

            // Prefer D-pad if pressed, otherwise use stick
            if (dpad.sqrMagnitude > 0.1f)
                input = dpad;
            else if (stick.sqrMagnitude > STICK_DEADZONE * STICK_DEADZONE)
                input = stick;
        }

        // Keyboard arrow keys (add to gamepad input)
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            input.y = 1f;
        else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            input.y = -1f;

        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            input.x = 1f;
        else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            input.x = -1f;

        return input;
    }

    /// <summary>
    /// Returns true if navigation was just pressed this frame (no repeat)
    /// </summary>
    public static bool IsNavigateUpPressed()
    {
        if (Gamepad.current != null)
        {
            if (Gamepad.current.dpad.up.wasPressedThisFrame)
                return true;
        }
        return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
    }

    public static bool IsNavigateDownPressed()
    {
        if (Gamepad.current != null)
        {
            if (Gamepad.current.dpad.down.wasPressedThisFrame)
                return true;
        }
        return Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
    }

    public static bool IsNavigateLeftPressed()
    {
        if (Gamepad.current != null)
        {
            if (Gamepad.current.dpad.left.wasPressedThisFrame)
                return true;
        }
        return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
    }

    public static bool IsNavigateRightPressed()
    {
        if (Gamepad.current != null)
        {
            if (Gamepad.current.dpad.right.wasPressedThisFrame)
                return true;
        }
        return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
    }

    /// <summary>
    /// Returns true if a gamepad is connected and was used recently
    /// </summary>
    public static bool IsGamepadActive()
    {
        return Gamepad.current != null && InputModeManager.CurrentMode == InputModeManager.InputMode.Gamepad;
    }

    /// <summary>
    /// Returns true if any gamepad button or stick was used this frame
    /// </summary>
    public static bool AnyGamepadInputThisFrame()
    {
        if (Gamepad.current == null) return false;

        var gp = Gamepad.current;

        // Check buttons
        if (gp.buttonSouth.wasPressedThisFrame ||
            gp.buttonEast.wasPressedThisFrame ||
            gp.buttonWest.wasPressedThisFrame ||
            gp.buttonNorth.wasPressedThisFrame ||
            gp.startButton.wasPressedThisFrame ||
            gp.selectButton.wasPressedThisFrame ||
            gp.leftShoulder.wasPressedThisFrame ||
            gp.rightShoulder.wasPressedThisFrame ||
            gp.leftTrigger.wasPressedThisFrame ||
            gp.rightTrigger.wasPressedThisFrame)
            return true;

        // Check sticks and dpad
        if (gp.leftStick.ReadValue().sqrMagnitude > STICK_DEADZONE * STICK_DEADZONE ||
            gp.rightStick.ReadValue().sqrMagnitude > STICK_DEADZONE * STICK_DEADZONE ||
            gp.dpad.ReadValue().sqrMagnitude > 0.1f)
            return true;

        return false;
    }

    /// <summary>
    /// Returns true if any keyboard or mouse input was used this frame
    /// </summary>
    public static bool AnyKeyboardMouseInputThisFrame()
    {
        // Mouse movement or clicks
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            return true;

        if (Mathf.Abs(Input.GetAxis("Mouse X")) > 0.1f || Mathf.Abs(Input.GetAxis("Mouse Y")) > 0.1f)
            return true;

        // Any key press
        if (Input.anyKeyDown && !AnyGamepadInputThisFrame())
            return true;

        return false;
    }
}
