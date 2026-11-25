using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

internal sealed class UeiRuntime : MonoBehaviour
{
    private static UeiRuntime? _active;

    private Canvas? _attachedCanvas;
    private UeiPanel? _panel;
    private readonly UeiEntityPlacement _placement = new(UeiPanel.TryCheatSpawnEntity, ReportPlacementStatus);
    private readonly UeiEntityGhost _ghost = new();
    private bool _loggedWaitingForData;
    private bool _loggedRuntimeError;
    private bool _loggedPlayerCameraReady;
    private bool _loggedFirstShow;
    private bool _loggedUpdateActive;
    private bool _loggedLateUpdateActive;
    private bool _loggedPatchTickActive;
    private int _lastInventoryUseShortcutFrame = -1;
    private string _lastHideReason = string.Empty;
    private string _lastTickSource = "none";
    private int _lastShownFrame = -1;

    internal static bool IsEntityPlacementActive => _active?._placement.IsActive ?? false;

    internal static bool BeginEntityPlacement(UeiEntry entry)
    {
        if (_active == null) UeiPlugin.EnsureRuntime("entity placement");
        UeiRuntime? runtime = _active;
        if (runtime == null || !runtime._placement.TryBegin(GetPlayerCameraSafe(), entry)) return false;
        runtime._panel?.Hide();
        runtime._lastShownFrame = -1;
        return true;
    }

    internal static void PrepareEntityPlacementInput(PlayerCamera camera)
    {
        if (camera == PlayerCamera.main) _active?._placement.Tick(camera);
    }

    private static void ReportPlacementStatus(string message) => _active?._panel?.SetPlacementStatus(message);

    internal static bool IsTextInputFocused
    {
        get
        {
            try
            {
                return _active?._panel?.IsTextInputFocused ?? false;
            }
            catch { return false; }
        }
    }

    internal static bool IsPointerOverPanel
    {
        get
        {
            try { return _active?._panel?.IsPointerOverPanel ?? false; }
            catch { return false; }
        }
    }

    internal static void ReleaseTextInputFocus()
    {
        try { _active?._panel?.ReleaseTextInputFocus(); }
        catch { }
    }

    private void Awake()
    {
        if (_active != null && _active != this)
        {
            Destroy(gameObject);
            return;
        }

        _active = this;
        DontDestroyOnLoad(gameObject);
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
        _placement.Cancel();
        _ghost.Hide();
        _panel?.HidePlacementPreview();
        UeiPlugin.LogInfo("UEI runtime OnDisable.");
    }

    private void OnApplicationFocus(bool focused)
    {
        if (focused) return;
        _placement.Cancel("placement.interrupted");
        _ghost.Hide();
        _panel?.HidePlacementPreview();
        UeiInput.Reset();
    }

    private void OnDestroy()
    {
        bool releaseSharedIcons = _active == this;
        _placement.Cancel();
        _ghost.Destroy();
        UeiPlugin.LogInfo("UEI runtime OnDestroy.");

        if (_active == this)
        {
            _active = null;
            UeiInput.Reset();
        }

        UeiPlugin.ClearRuntime(this);
        _panel?.Destroy();
        _panel = null;
        _attachedCanvas = null;
        if (releaseSharedIcons) UeiMissingIcon.Release();
    }

    private void Update()
    {
        if (!_loggedUpdateActive)
        {
            _loggedUpdateActive = true;
            UeiPlugin.LogInfo("UEI runtime Update active.");
        }

        Tick("UeiRuntime.Update");
    }

    private void LateUpdate()
    {
        if (!_loggedLateUpdateActive)
        {
            _loggedLateUpdateActive = true;
            UeiPlugin.LogInfo("UEI runtime LateUpdate active.");
        }

        Tick("UeiRuntime.LateUpdate");
    }

    internal static void TickStandalone(string source)
    {
        if (_active == null)
        {
            UeiPlugin.EnsureRuntime(source);
        }

        _active?.Tick(source);
    }

    internal static void TickFromPlayerCamera(PlayerCamera camera, string source)
    {
        if (_active == null)
        {
            UeiPlugin.EnsureRuntime(source);
        }

        UeiRuntime? runtime = _active;
        if (runtime == null)
        {
            return;
        }

        if (!runtime._loggedPatchTickActive)
        {
            runtime._loggedPatchTickActive = true;
            UeiPlugin.LogInfo("UEI PlayerCamera hook active.");
        }

        runtime.Tick(source, camera);
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

            _placement.Cancel("placement.interrupted");
            _ghost.Hide();
            _panel?.HidePlacementPreview();
            _panel?.Hide();
        }
    }

    private void TickSafe(string source, PlayerCamera? cameraOverride)
    {
        UeiNativeAcquisition.Tick();
        UeiWorldSpawning.EnsureCommands();
        // Placement must keep running after inventory/UEI closes, and belongs
        // to the local player even when another camera invokes a recovery hook.
        _placement.Tick(GetPlayerCameraSafe());
        PlayerCamera? camera = _placement.IsActive ? GetPlayerCameraSafe() : cameraOverride ?? GetPlayerCameraSafe();
        if (camera == null || camera.mainCanvas == null || camera.body == null)
        {
            string reason = "waiting for PlayerCamera/mainCanvas/body";
            LogHideReason(reason);
            _lastShownFrame = -1;
            _ghost.Hide();
            _panel?.HidePlacementPreview();
            _panel?.Hide();
            return;
        }

        if (!_loggedPlayerCameraReady)
        {
            _loggedPlayerCameraReady = true;
            UeiPlugin.LogInfo("UEI found PlayerCamera, main canvas, and player body.");
        }

        EnsurePanel(camera.mainCanvas);

        if (_placement.IsActive)
        {
            _lastShownFrame = -1;
            _panel?.Hide();
            bool appearance = false;
            if (_placement.HasPosition) appearance = _ghost.Show(_placement.Entry!.Id, _placement.Position, _placement.CanPlace);
            else _ghost.Hide();
            _panel?.ShowPlacementPreview(_placement.Entry!, _placement.CanPlace, appearance);
            return;
        }
        _ghost.Hide();
        _panel?.HidePlacementPreview();

        bool shouldShow = ShouldShow(camera, out string hideReason);
        if (!shouldShow)
        {
            LogHideReason(hideReason);
            _lastShownFrame = -1;
            _panel?.Hide();
            return;
        }

        // Keep every recovery hook and reevaluate visibility on every call: an
        // input hook may open/close the inventory after Update in the same frame.
        // Only catalog/grid work is deduplicated while visibility stays unchanged.
        if (_lastShownFrame == Time.frameCount)
        {
            if (!_loggedWaitingForData)
            {
                HandleInventoryUseShortcut(camera);
            }
            return;
        }
        _lastShownFrame = Time.frameCount;

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
        if (_panel != null && _panel.IsAlive && _attachedCanvas == canvas)
        {
            return;
        }

        _panel?.Destroy();
        _attachedCanvas = canvas;
        _panel = new UeiPanel(canvas);
        _lastShownFrame = -1;
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
    private static readonly MethodInfo NativeGetKey = AccessTools.Method(typeof(Input), nameof(Input.GetKey), new[] { typeof(KeyCode) });
    private static readonly MethodInfo NativeGetKeyDown = AccessTools.Method(typeof(Input), nameof(Input.GetKeyDown), new[] { typeof(KeyCode) });
    private static readonly MethodInfo NativeGetKeyUp = AccessTools.Method(typeof(Input), nameof(Input.GetKeyUp), new[] { typeof(KeyCode) });
    private static readonly MethodInfo RoutedGetKey = AccessTools.Method(typeof(UeiInput), nameof(UeiInput.GetKey));
    private static readonly MethodInfo RoutedGetKeyDown = AccessTools.Method(typeof(UeiInput), nameof(UeiInput.GetKeyDown));
    private static readonly MethodInfo RoutedGetKeyUp = AccessTools.Method(typeof(UeiInput), nameof(UeiInput.GetKeyUp));

    private static void Prefix(PlayerCamera __instance)
    {
        UeiInput.PreparePlayerInput(__instance);
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        int replaced = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            MethodInfo? replacement = instruction.operand is MethodInfo method
                ? Equals(method, NativeGetKey) ? RoutedGetKey
                : Equals(method, NativeGetKeyDown) ? RoutedGetKeyDown
                : Equals(method, NativeGetKeyUp) ? RoutedGetKeyUp
                : null
                : null;
            if (replacement != null)
            {
                // Mutate the existing instruction so branch labels and exception
                // blocks stay attached to the same call site.
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
                replaced++;
            }
            yield return instruction;
        }

        if (replaced == 0) throw new InvalidOperationException("UEI could not find PlayerCamera KeyCode input reads to route.");
        UeiPlugin.LogInfo($"UEI input routing patched: {replaced} key reads.");
    }

    private static void Postfix(PlayerCamera __instance)
    {
        UeiRuntime.TickFromPlayerCamera(__instance, "PlayerCamera.HandleInput");
    }
}

[HarmonyPatch(typeof(PlayerCamera), "HandleAttacks")]
internal static class UeiPlayerCameraAttackPatch
{
    private static bool Prefix() => !UeiInput.ShouldBlockWorldPointerInput;
}

[HarmonyPatch(typeof(PlayerCamera), "HandleStartDragging")]
internal static class UeiPlayerCameraStartDragPatch
{
    private static bool Prefix() => !UeiInput.ShouldBlockWorldPointerInput;
}

[HarmonyPatch(typeof(PlayerCamera), "TryPerformWorldActions")]
internal static class UeiPlayerCameraWorldActionPatch
{
    private static bool Prefix() => !UeiInput.ShouldBlockWorldPointerInput;
}

[HarmonyPatch(typeof(PlayerCamera), "HandleBlockPlaceGhost")]
internal static class UeiPlayerCameraPlaceGhostPatch
{
    private static bool Prefix(PlayerCamera __instance)
    {
        if (!UeiRuntime.IsEntityPlacementActive) return true;
        if (__instance.placeSquare != null) __instance.placeSquare.enabled = false;
        return false;
    }
}

[HarmonyPatch(typeof(PlayerCamera), "HandleReleaseDragging")]
internal static class UeiPlayerCameraReleaseDragPatch
{
    private static bool Prefix(PlayerCamera __instance)
    {
        if (!UeiInput.ShouldBlockWorldPointerInput) return true;
        UeiInput.CancelWorldDrag(__instance);
        return false;
    }
}

[HarmonyPatch(typeof(PlayerCamera), "HandleRadialMenu")]
internal static class UeiPlayerCameraRadialPatch
{
    private static void Postfix(PlayerCamera __instance)
    {
        UeiRuntime.TickFromPlayerCamera(__instance, "PlayerCamera.HandleRadialMenu");
    }
}

[HarmonyPatch(typeof(PlayerCamera), "HandleWoundView")]
internal static class UeiPlayerCameraWoundPatch
{
    private static void Postfix(PlayerCamera __instance)
    {
        UeiRuntime.TickFromPlayerCamera(__instance, "PlayerCamera.HandleWoundView");
    }
}

[HarmonyPatch(typeof(GlobalDark), "Update")]
internal static class UeiGlobalDarkUpdatePatch
{
    private static void Postfix()
    {
        UeiRuntime.TickStandalone("GlobalDark.Update");
    }
}

internal sealed class UeiProbeListener : MonoBehaviour
{
    private bool _loggedUpdate;
    private bool _loggedOnGui;

    private void Awake()
    {
        UeiPlugin.LogInfo("UEI probe Awake.");
    }

    private void OnEnable()
    {
        UeiPlugin.LogInfo("UEI probe OnEnable.");
    }

    private void Start()
    {
        UeiPlugin.LogInfo("UEI probe Start.");
    }

    private void OnDisable()
    {
        UeiPlugin.LogInfo("UEI probe OnDisable.");
    }

    private void OnDestroy()
    {
        UeiPlugin.LogInfo("UEI probe OnDestroy.");
        UeiPlugin.ClearProbe(this);
    }

    private void Update()
    {
        if (!_loggedUpdate)
        {
            _loggedUpdate = true;
            UeiPlugin.LogInfo("UEI probe Update active.");
        }

        UeiRuntime.TickStandalone("UeiProbeListener.Update");
    }

    private void OnGUI()
    {
        if (!_loggedOnGui)
        {
            _loggedOnGui = true;
            UeiPlugin.LogInfo("UEI probe OnGUI active; visual debug overlay disabled.");
        }
    }
}


// Natural generation supplies a range before Start; native console spawning
// leaves the prefab's zero range. Initialize it before Together publishes it.
[HarmonyPatch(typeof(TraderScript), "Start")]
internal static class UeiTraderSpawnInitializationPatch
{
    private static void Prefix(TraderScript __instance) => UeiWorldSpawning.InitializeTrader(__instance);
}
