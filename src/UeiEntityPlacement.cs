using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Placement outlives the inventory panel. The preview never instantiates a prefab.
internal sealed class UeiEntityPlacement
{
    internal enum PlacementPhase { Idle, AwaitRelease, Aiming }

    private readonly Func<UeiEntry, Vector2, UeiPanel.CheatActionResult> _spawn;
    private readonly Action<string>? _reportStatus;
    private PlayerCamera? _owner;
    private Body? _body;
    private Canvas? _canvas;
    private Camera? _worldCamera;
    private TogetherNetworkState _networkState;
    private int _sceneHandle;
    private int _selectionFrame;
    private int _lastInputFrame = -1;
    private int _lastRaycastFrame = -1;
    private bool _pointerOverUi;
    private bool _selectionReleased;
    private bool _confirming;

    internal UeiEntityPlacement(Func<UeiEntry, Vector2, UeiPanel.CheatActionResult> spawn,
        Action<string>? reportStatus = null)
    {
        _spawn = spawn;
        _reportStatus = reportStatus;
    }

    internal UeiEntry? Entry { get; private set; }
    internal PlacementPhase Phase { get; private set; }
    internal bool IsActive => Entry != null;
    internal bool CanPlace { get; private set; }
    internal bool HasPosition { get; private set; }
    internal Vector2 Position { get; private set; }

    internal bool TryBegin(PlayerCamera? camera, UeiEntry entry)
    {
        if (IsActive || entry.Kind != UeiEntryKind.Entity || !UeiEntities.Contains(entry.Id)
            || !UeiWorldSpawning.IsSafeToken(entry.Id) || !UeiPanel.CheatEnabled()
            || !CanUseWorld(camera) || camera!.dragItem != null) return false;

        PlayerCamera owner = camera!;
        Entry = entry;
        _owner = owner;
        _body = owner.body;
        _canvas = owner.mainCanvas;
        _worldCamera = Camera.main;
        _networkState = UeiPanel.GetTogetherNetworkState(out _, out _, out _);
        _sceneHandle = SceneManager.GetActiveScene().handle;
        _selectionFrame = Time.frameCount;
        _lastInputFrame = _lastRaycastFrame = -1;
        _selectionReleased = !Input.GetKey(KeyCode.Mouse0) || Input.GetKeyUp(KeyCode.Mouse0);
        Phase = PlacementPhase.AwaitRelease;
        CanPlace = false;
        HasPosition = false;
        owner.timeHeldThrow = 0f;
        UeiRuntime.ReleaseTextInputFocus();
        UeiInput.CaptureWorldPointerInput();
        // Let the game's own radial-menu transition close the inventory.
        owner.radialOpen = false;
        UeiPanel.ClearNativeTooltip();
        _reportStatus?.Invoke(UeiI18n.Format("placement.started", entry.DisplayName));
        return true;
    }

    internal void Tick(PlayerCamera? camera)
    {
        if (!IsActive || _confirming) return;
        try
        {
            if (!CanUseWorld(camera) || camera != _owner || camera!.body != _body
                || camera.mainCanvas != _canvas || Camera.main != _worldCamera
                || SceneManager.GetActiveScene().handle != _sceneHandle || camera.radialOpen
                || !UeiPanel.CheatEnabled() || !UeiEntities.Contains(Entry!.Id)
                || UeiPanel.GetTogetherNetworkState(out _, out _, out _) != _networkState)
            {
                Cancel("placement.interrupted");
                return;
            }

            UpdateTarget();
            int frame = Time.frameCount;
            if (_lastInputFrame == frame) return;
            _lastInputFrame = frame;

            KeyCode pause = GetBindSafe("pause");
            if (Input.GetKeyDown(KeyCode.Escape)
                || (pause != KeyCode.None && pause != KeyCode.Mouse0 && Input.GetKeyDown(pause)))
            {
                UeiInput.CaptureNavigationKey(Input.GetKeyDown(KeyCode.Escape) ? KeyCode.Escape : pause);
                Cancel();
                return;
            }
            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                Cancel();
                return;
            }
            KeyCode inventory = GetBindSafe("toggleinventory");
            if (inventory != KeyCode.None && inventory != KeyCode.Mouse0
                && Input.GetKeyDown(inventory))
            {
                // Keyboard inventory navigation still reaches native HandleInput.
                Cancel();
                // Keep a mouse-bound inventory key captured; opening the same
                // native flag avoids also triggering another mouse-bound action.
                if (inventory >= KeyCode.Mouse2 && inventory <= KeyCode.Mouse6) camera.radialOpen = true;
                return;
            }

            if (frame <= _selectionFrame) return;
            if (Phase == PlacementPhase.AwaitRelease)
            {
                _selectionReleased |= !Input.GetKey(KeyCode.Mouse0) || Input.GetKeyUp(KeyCode.Mouse0);
                if (!_selectionReleased) return;
                Phase = PlacementPhase.Aiming;
            }
            if (!CanPlace || !Input.GetKeyDown(KeyCode.Mouse0)) return;

            UeiEntry entry = Entry!;
            Vector2 position = Position;
            UeiInput.CaptureWorldPointerInput();
            _confirming = true;
            UeiPanel.CheatActionResult result;
            try { result = _spawn(entry, position); }
            finally { _confirming = false; }
            if (result == UeiPanel.CheatActionResult.Failed)
            {
                Phase = PlacementPhase.AwaitRelease;
                _selectionReleased = false;
                Alert(UeiI18n.T("status.cheatEntityFailed"));
                return;
            }

            Finish();
            Alert(result == UeiPanel.CheatActionResult.RequestSent
                ? UeiI18n.T("status.cheatSpawnRequestSent")
                : UeiI18n.Format("status.cheatEntitySpawned", entry.DisplayName));
        }
        catch (Exception ex)
        {
            UeiPlugin.LogWarning("UEI entity placement failed: " + ex.Message);
            Cancel("placement.interrupted");
        }
    }

    internal void Cancel(string statusKey = "placement.cancelled")
    {
        if (!IsActive) return;
        Finish();
        Alert(UeiI18n.T(statusKey));
    }

    private void Finish()
    {
        // Retain the entire confirming/cancelling gesture after placement ends.
        UeiInput.CaptureWorldPointerInput();
        Entry = null;
        _owner = null;
        _body = null;
        _canvas = null;
        _worldCamera = null;
        Phase = PlacementPhase.Idle;
        CanPlace = false;
        HasPosition = false;
    }

    private void UpdateTarget()
    {
        CanPlace = false;
        HasPosition = false;
        Vector3 mouse = Input.mousePosition;
        if (!UeiWorldSpawning.IsFinite(mouse.x) || !UeiWorldSpawning.IsFinite(mouse.y)
            || mouse.x < 0f || mouse.y < 0f || mouse.x >= Screen.width || mouse.y >= Screen.height) return;
        Vector2 position = _worldCamera!.ScreenToWorldPoint(mouse);
        if (!UeiWorldSpawning.IsFinite(position.x) || !UeiWorldSpawning.IsFinite(position.y)) return;
        Position = position;
        HasPosition = true;
        if (_lastRaycastFrame != Time.frameCount)
        {
            _lastRaycastFrame = Time.frameCount;
            _pointerOverUi = false;
            foreach (var hit in UIUtil.GetEventSystemRaycastResults())
            {
                if (hit.module is GraphicRaycaster)
                {
                    _pointerOverUi = true;
                    break;
                }
            }
        }
        CanPlace = !_pointerOverUi && !UeiRuntime.IsPointerOverPanel && !UeiRuntime.IsTextInputFocused;
    }

    private static bool CanUseWorld(PlayerCamera? camera)
        => camera != null && camera == PlayerCamera.main && camera.body != null && camera.body.alive
            && camera.mainCanvas != null && Camera.main != null && Application.isFocused
            && !PauseHandler.paused && !(ConsoleScript.instance != null && ConsoleScript.instance.active)
            && !(MinigameBase.main != null && MinigameBase.main.currentMinigame)
            && !IsOpen(camera.craftingPanel) && !IsOpen(camera.tradeMenu) && !IsOpen(camera.woundView);

    private static bool IsOpen(GameObject? panel) => panel != null && panel.activeInHierarchy;

    private static KeyCode GetBindSafe(string name)
    {
        try { return KeyBinds.GetBind(name); }
        catch { return KeyCode.None; }
    }

    private void Alert(string message)
    {
        try { _reportStatus?.Invoke(message); } catch { }
        try { PlayerCamera.main?.DoAlert(message); } catch { }
    }
}
