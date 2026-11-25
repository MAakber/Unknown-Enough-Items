using UnityEngine;

internal static class UeiInput
{
    private static int _lastCaptureFrame = -1;
    private static int _capturedMouseButtons;
    private static int _releasedMouseButtons;
    private static KeyCode _capturedNavigationKey;
    private static int _navigationReleaseFrame = -1;

    internal static bool ShouldBlockWorldPointerInput
    {
        get
        {
            UpdatePointerCapture();
            return UeiRuntime.IsEntityPlacementActive || UeiRuntime.IsTextInputFocused
                || UeiRuntime.IsPointerOverPanel || _capturedMouseButtons != 0;
        }
    }

    internal static void PreparePlayerInput(PlayerCamera camera)
    {
        // Read placement confirmation/cancellation before native navigation.
        UeiRuntime.PrepareEntityPlacementInput(camera);
        UpdatePointerCapture();
        if (camera == null) return;

        if (UeiRuntime.IsTextInputFocused)
        {
            // A skipped key read must also end input latched in an earlier frame.
            if (camera.body != null)
            {
                camera.body.moveDir = Vector2.zero;
                camera.body.crouching = false;
            }
            EndJump(camera);
            camera.timeHeldThrow = 0f;

            if (NavigationKeyPressed("pause") || NavigationKeyPressed("toggleinventory"))
            {
                UeiRuntime.ReleaseTextInputFocus();
            }
        }
        else if (ShouldBlockWorldPointerInput && IsMouseKey(GetBindSafe("jump")))
        {
            EndJump(camera);
        }
    }

    internal static bool GetKey(KeyCode key) => !ShouldBlockKey(key) && Input.GetKey(key);
    internal static bool GetKeyDown(KeyCode key) => !ShouldBlockKey(key) && Input.GetKeyDown(key);
    internal static bool GetKeyUp(KeyCode key) => !ShouldBlockKey(key) && Input.GetKeyUp(key);

    internal static void CancelWorldDrag(PlayerCamera camera)
    {
        if (camera == null) return;
        if (camera.dragImage != null) camera.dragImage.enabled = false;
        camera.dragItem = null;
    }

    internal static void Reset()
    {
        _lastCaptureFrame = -1;
        _capturedMouseButtons = 0;
        _releasedMouseButtons = 0;
        _capturedNavigationKey = KeyCode.None;
        _navigationReleaseFrame = -1;
    }

    internal static void CaptureWorldPointerInput()
    {
        UpdatePointerCapture();
        for (int button = 0; button <= (int)KeyCode.Mouse6 - (int)KeyCode.Mouse0; button++)
        {
            KeyCode key = (KeyCode)((int)KeyCode.Mouse0 + button);
            if (!Input.GetKey(key) && !Input.GetKeyDown(key) && !Input.GetKeyUp(key)) continue;
            int bit = 1 << button;
            _capturedMouseButtons |= bit;
            if (Input.GetKeyUp(key)) _releasedMouseButtons |= bit;
        }
    }

    internal static void CaptureNavigationKey(KeyCode key)
    {
        _capturedNavigationKey = key;
        _navigationReleaseFrame = Input.GetKeyUp(key) ? Time.frameCount : -1;
    }

    private static bool ShouldBlockKey(KeyCode key)
    {
        if (_capturedNavigationKey != KeyCode.None)
        {
            int frame = Time.frameCount;
            if (_navigationReleaseFrame >= 0 && _navigationReleaseFrame < frame)
                _capturedNavigationKey = KeyCode.None;
            else if (Input.GetKeyUp(_capturedNavigationKey)
                || (!Input.GetKey(_capturedNavigationKey) && !Input.GetKeyDown(_capturedNavigationKey)))
                _navigationReleaseFrame = frame;
            if (key == _capturedNavigationKey) return true;
        }
        if (UeiRuntime.IsTextInputFocused) return true;
        return IsMouseKey(key) && ShouldBlockWorldPointerInput;
    }

    private static bool IsMouseKey(KeyCode key) => key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6;

    private static void UpdatePointerCapture()
    {
        int frame = Time.frameCount;
        if (_lastCaptureFrame == frame) return;
        _lastCaptureFrame = frame;
        _capturedMouseButtons &= ~_releasedMouseButtons;
        _releasedMouseButtons = 0;

        bool overPanel = UeiRuntime.IsEntityPlacementActive || UeiRuntime.IsPointerOverPanel || UeiRuntime.IsTextInputFocused;
        if (!overPanel && _capturedMouseButtons == 0) return;

        for (int button = 0; button <= (int)KeyCode.Mouse6 - (int)KeyCode.Mouse0; button++)
        {
            KeyCode key = (KeyCode)((int)KeyCode.Mouse0 + button);
            int bit = 1 << button;
            if ((_capturedMouseButtons & bit) != 0)
            {
                // Consume the release frame too, even if the pointer left UEI.
                if (!Input.GetKey(key) && !Input.GetKeyUp(key)) _capturedMouseButtons &= ~bit;
            }
            else if (overPanel && Input.GetKeyDown(key))
            {
                _capturedMouseButtons |= bit;
            }
            if ((_capturedMouseButtons & bit) != 0 && Input.GetKeyUp(key)) _releasedMouseButtons |= bit;
        }
    }

    private static bool NavigationKeyPressed(string bind)
    {
        KeyCode key = GetBindSafe(bind);
        // Text bindings remain text while editing; Escape/Tab and function keys
        // may dismiss search and are then processed by the original game method.
        return (key == KeyCode.Escape || key == KeyCode.Tab || (key >= KeyCode.F1 && key <= KeyCode.F15))
            && Input.GetKeyDown(key);
    }

    private static KeyCode GetBindSafe(string bind)
    {
        try { return KeyBinds.GetBind(bind); }
        catch { return KeyCode.None; }
    }

    private static void EndJump(PlayerCamera camera)
    {
        camera.wantsJumpInput = false;
        if (camera.body != null) camera.body.endedJump = true;
    }
}
