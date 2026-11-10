using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

internal sealed class UeiRuntime : MonoBehaviour
{
    private static UeiRuntime? _active;

    private Canvas? _attachedCanvas;
    private UeiPanel? _panel;
    private bool _loggedWaitingForData;
    private bool _loggedRuntimeError;
    private bool _loggedPlayerCameraReady;
    private bool _loggedFirstShow;
    private bool _loggedUpdateActive;
    private int _lastTickFrame = -1;
    private int _lastInventoryUseShortcutFrame = -1;
    private string _lastHideReason = string.Empty;
    private string _lastTickSource = "none";

    internal static bool ShouldBlockPlayerCameraInput
    {
        get
        {
            try
            {
                UeiPanel? panel = _active?._panel;
                return panel != null && (panel.IsTextInputFocused || panel.IsPointerOverPanel);
            }
            catch { return false; }
        }
    }

    private void Awake()
    {
        if (_active != null && _active != this)
        {
            Destroy(gameObject);
            return;
        }

        _active = this;
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }
        UeiPlugin.LogInfo("UEI runtime Awake.");
    }

    private void OnEnable()
    {
        UeiPlugin.LogInfo("UEI runtime OnEnable.");
    }

    private void Start()
    {
        UeiPlugin.LogInfo("UEI runtime Start.");
    }

    private void OnDisable()
    {
        UeiPlugin.LogInfo("UEI runtime OnDisable.");
    }

    private void OnDestroy()
    {
        UeiPlugin.LogInfo("UEI runtime OnDestroy.");

        if (_active == this)
        {
            _active = null;
        }

        UeiPlugin.ClearRuntime(this);
        _panel?.Destroy();
        _panel = null;
        _attachedCanvas = null;
    }

    private void LateUpdate()
    {
        if (!_loggedUpdateActive)
        {
            _loggedUpdateActive = true;
            UeiPlugin.LogInfo("UEI runtime LateUpdate active.");
        }

        Tick("UeiRuntime.LateUpdate");
    }

    internal static string BuildDebugOverlayText()
    {
        UeiRuntime? runtime = _active;
        PlayerCamera? camera = GetPlayerCameraSafe();
        string hideReason = camera == null ? "no camera" : string.Empty;
        bool shouldShow = camera != null && ShouldShow(camera, out hideReason);
        bool radialActive = false;
        Vector3 radialScale = Vector3.zero;
        if (camera != null)
        {
            TryGetRadialMenuState(camera, out radialActive, out radialScale);
        }

        StringBuilder sb = new();
        sb.AppendLine("UEI DEBUG");
        sb.AppendLine("runtime=" + (runtime != null ? "1" : "0") + " source=" + (runtime?._lastTickSource ?? "none"));
        sb.AppendLine("shouldShow=" + (shouldShow ? "1" : "0") + " reason=" + (shouldShow ? "shown" : hideReason));

        if (camera == null)
        {
            sb.AppendLine("camera=0 body=0 canvas=0");
        }
        else
        {
            sb.AppendLine(
                "camera=1 body=" + (camera.body != null ? "1" : "0")
                + " canvas=" + (camera.mainCanvas != null ? "1" : "0")
                + " console=" + (IsConsoleOpen() ? "1" : "0"));
            sb.AppendLine(
                "radialOpen=" + (camera.radialOpen ? "1" : "0")
                + " radialActive=" + (radialActive ? "1" : "0")
                + " medical=" + (IsMedicalPanelOpen(camera) ? "1" : "0")
                + " craft=" + (IsActive(camera.craftingPanel) ? "1" : "0")
                + " trade=" + (IsActive(camera.tradeMenu) ? "1" : "0"));
            sb.AppendLine("radialScale=" + radialScale.x.ToString("0.00") + "," + radialScale.y.ToString("0.00") + "," + radialScale.z.ToString("0.00"));
        }

        sb.AppendLine("hideReason=" + (runtime?._lastHideReason ?? "none"));
        sb.AppendLine("panel=" + (runtime?._panel?.DescribeVisualState() ?? "none"));
        return sb.ToString();
    }

    private void Tick(string source, PlayerCamera? cameraOverride = null)
    {
        int frame = Time.frameCount;
        if (_lastTickFrame == frame)
        {
            return;
        }

        _lastTickFrame = frame;
        _lastTickSource = source;

        try
        {
            TickSafe(source, cameraOverride);
        }
        catch (Exception ex)
        {
            if (!_loggedRuntimeError)
            {
                _loggedRuntimeError = true;
                UeiPlugin.LogError($"UEI runtime failed via {source}: {ex}");
            }

            _panel?.Hide();
        }
    }

    private void TickSafe(string source, PlayerCamera? cameraOverride)
    {
        PlayerCamera? camera = cameraOverride ?? GetPlayerCameraSafe();
        if (camera == null || camera.mainCanvas == null || camera.body == null)
        {
            LogHideReason("waiting for PlayerCamera/mainCanvas/body");
            _panel?.Hide();
            return;
        }

        if (!_loggedPlayerCameraReady)
        {
            _loggedPlayerCameraReady = true;
            UeiPlugin.LogInfo("UEI found PlayerCamera, main canvas, and player body.");
        }

        EnsurePanel(camera.mainCanvas);

        bool shouldShow = ShouldShow(camera, out string hideReason);
        if (!shouldShow)
        {
            LogHideReason(hideReason);
            _panel?.Hide();
            return;
        }

        if (!_loggedFirstShow)
        {
            _loggedFirstShow = true;
            UeiPlugin.LogInfo("UEI display condition met; showing panel.");
        }

        if (!UeiCatalog.EnsureBuilt())
        {
            if (!_loggedWaitingForData)
            {
                _loggedWaitingForData = true;
                UeiPlugin.LogInfo("UEI is waiting for item, liquid, and recipe data.");
            }

            _panel?.ShowLoading(camera);
            return;
        }

        _loggedWaitingForData = false;
        _panel?.Show(camera);
        HandleInventoryUseShortcut(camera);
    }

    private void HandleInventoryUseShortcut(PlayerCamera camera)
    {
        if (_panel == null || _panel.IsTextInputFocused || _lastInventoryUseShortcutFrame == Time.frameCount)
        {
            return;
        }

        if (!Input.GetKeyDown(KeyCode.U) || camera.dragItem != null)
        {
            return;
        }

        try
        {
            List<RaycastResult> uiCasts = UIUtil.GetEventSystemRaycastResults();
            foreach (RaycastResult uiCast in uiCasts)
            {
                if (!uiCast.gameObject.TryGetComponent<InvButton>(out InvButton button) || !button.Overlaps(uiCasts))
                {
                    continue;
                }

                Item item = button.GetItem();
                if (item == null)
                {
                    continue;
                }

                _lastInventoryUseShortcutFrame = Time.frameCount;
                if (_panel.ShowItemUses(item))
                {
                    camera.PlayUISound(PlayerCamera.UISoundType.MiniClick);
                }
                return;
            }
        }
        catch (Exception ex)
        {
            _lastInventoryUseShortcutFrame = Time.frameCount;
            UeiPlugin.LogWarning($"UEI inventory use shortcut failed: {ex.Message}");
        }
    }

    private void EnsurePanel(Canvas canvas)
    {
        if (_panel != null && _attachedCanvas == canvas)
        {
            return;
        }

        _panel?.Destroy();
        _attachedCanvas = canvas;
        _panel = new UeiPanel(canvas);
        UeiPlugin.LogInfo("UEI panel attached to the main canvas.");
    }

    private void LogHideReason(string reason)
    {
        if (_lastHideReason == reason)
        {
            return;
        }

        _lastHideReason = reason;
        UeiPlugin.LogInfo("UEI hidden: " + reason + ".");
    }

    private static PlayerCamera? GetPlayerCameraSafe()
    {
        try { return PlayerCamera.main; }
        catch { return null; }
    }

    private static bool ShouldShow(PlayerCamera camera, out string hideReason)
    {
        if (IsMedicalPanelOpen(camera))
        {
            hideReason = "medical panel is open";
            return false;
        }

        if (!IsTargetInventoryUiOpen(camera))
        {
            hideReason = "radial inventory is closed";
            return false;
        }

        if (IsActive(camera.craftingPanel))
        {
            hideReason = "crafting panel is open";
            return false;
        }

        if (IsActive(camera.tradeMenu))
        {
            hideReason = "trade panel is open";
            return false;
        }

        if (IsConsoleOpen())
        {
            hideReason = "console is open";
            return false;
        }

        hideReason = string.Empty;
        return true;
    }

    private static bool IsTargetInventoryUiOpen(PlayerCamera camera)
    {
        return IsRadialInventoryOpen(camera);
    }

    private static bool IsRadialInventoryOpen(PlayerCamera camera)
    {
        if (camera.radialOpen)
        {
            return true;
        }

        return TryGetRadialMenuState(camera, out bool active, out Vector3 scale)
            && active
            && scale.sqrMagnitude > 0.0025f;
    }

    private static bool TryGetRadialMenuState(PlayerCamera camera, out bool active, out Vector3 scale)
    {
        try
        {
            RectTransform? radialMenu = camera.radialMenu;
            active = radialMenu != null && radialMenu.gameObject.activeInHierarchy;
            scale = radialMenu != null ? radialMenu.localScale : Vector3.zero;
            return true;
        }
        catch
        {
            active = false;
            scale = Vector3.zero;
            return false;
        }
    }

    private static bool IsMedicalPanelOpen(PlayerCamera camera)
    {
        try
        {
            return camera.woundView != null && camera.woundView.activeInHierarchy;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsActive(GameObject? go)
    {
        try { return go != null && go.activeInHierarchy; }
        catch { return false; }
    }

    private static bool IsConsoleOpen()
    {
        try { return ConsoleScript.instance != null && ConsoleScript.instance.active; }
        catch { return false; }
    }
}

[HarmonyPatch(typeof(PlayerCamera), "HandleInput")]
internal static class UeiPlayerCameraInputPatch
{
    private static bool Prefix()
    {
        return !UeiRuntime.ShouldBlockPlayerCameraInput;
    }
}
