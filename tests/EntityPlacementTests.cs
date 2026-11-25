using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal static class EntityPlacementTests
{
    private const string EntityId = "uei_placement_fixture";
    private static int _frame = 10000;
    private static readonly UeiEntry Entry = new()
    {
        Kind = UeiEntryKind.Entity, Id = EntityId, EntryKey = "entity:" + EntityId, DisplayName = "Placement fixture",
    };

    private sealed class Scenario
    {
        internal readonly PlayerCamera Camera;
        internal readonly UeiEntityPlacement Placement;
        internal int Calls;
        internal Vector2 LastPosition;
        internal UeiPanel.CheatActionResult Result = UeiPanel.CheatActionResult.Completed;
        internal readonly List<string[]> Commands = [];

        internal Scenario(TogetherNetworkState network = TogetherNetworkState.Offline)
        {
            UeiRuntime.Placement?.Cancel();
            UeiRuntime.Placement = null;
            UeiInput.Reset();
            UeiRuntime.IsTextInputFocused = UeiRuntime.IsPointerOverPanel = false;
            UeiPanel.CheatAllowed = UeiPanel.CheatConfigured = true;
            UeiPanel.NetworkState = network;
            PauseHandler.paused = false;
            MinigameBase.main.currentMinigame = null;
            ConsoleScript.instance.active = false;
            UIUtil.Hits.Clear();
            UIUtil.RaycastCalls = 0;
            SceneManager.ActiveHandle = 1;
            Application.isFocused = true;
            UnityEngine.Camera.main = new();
            Input.mousePosition = new(128f, 128f, 0f);
            Camera = PlayerCamera.main = new() { radialOpen = true };
            ConsoleScript.Commands = [new("spawn", "native", args => Commands.Add((string[])args.Clone()), null)];
            Placement = new((entry, position) =>
            {
                Calls++;
                LastPosition = position;
                if (Result != UeiPanel.CheatActionResult.Completed) return Result;
                return UeiWorldSpawning.SpawnEntity(entry.Id, position)
                    ? UeiPanel.CheatActionResult.Completed : UeiPanel.CheatActionResult.Failed;
            });
            UeiRuntime.Placement = Placement;
            Step(up: [KeyCode.Mouse0]);
        }

        internal void Begin(Action<bool, string> check)
            => check(Placement.TryBegin(Camera, Entry), "A valid entity must enter placement");

        internal void Tick() => UeiInput.PreparePlayerInput(Camera);
    }

    private static void Step(KeyCode[]? held = null, KeyCode[]? down = null, KeyCode[]? up = null)
        => Input.SetFrame(++_frame, held, down, up);

    internal static void Run(Action<bool, string> check)
    {
        GameObject prefab = new(EntityId);
        prefab.Attach(new BuildingEntity { id = EntityId });
        UeiEntities.Capture(EntityId, prefab, prefab.GetComponent<BuildingEntity>());
        try
        {
            SelectionAndCoordinates(check);
            ReleasedSelection(check);
            BlockedTargets(check);
            InvalidSelections(check);
            Cancellation(check);
            SessionCleanup(check);
            RetryAndRequests(check);
            ReentrantDispatch(check);
        }
        finally
        {
            UeiRuntime.Placement?.Cancel();
            UeiRuntime.Placement = null;
            UeiInput.Reset();
            UeiPanel.CheatAllowed = UeiPanel.CheatConfigured = true;
            UeiPanel.NetworkState = TogetherNetworkState.Offline;
            Application.isFocused = true;
            PauseHandler.paused = false;
            MinigameBase.main.currentMinigame = null;
            ConsoleScript.instance.active = false;
            UIUtil.Hits.Clear();
            KeyBinds.Binds["pause"] = KeyCode.Escape;
            KeyBinds.Binds["toggleinventory"] = KeyCode.Tab;
        }
        Console.WriteLine("PASS: entity placement selection/release, mouse coordinates, gesture capture, cancellation, lifecycle/permission cleanup and one-shot dispatch (API doubles only)");
    }

    private static void SelectionAndCoordinates(Action<bool, string> check)
    {
        Scenario s = new();
        UeiRuntime.IsTextInputFocused = true;
        s.Camera.timeHeldThrow = 0.8f;
        s.Camera.body.moveDir = new(1f, 0f);
        s.Camera.wantsJumpInput = true;
        Step(held: [KeyCode.Mouse0], down: [KeyCode.Mouse0]);
        s.Begin(check);
        s.Tick();
        s.Placement.Tick(s.Camera);
        check(s.Placement.IsActive && !s.Camera.radialOpen && s.Calls == 0,
            "Selecting must close the native inventory without spawning or consuming a same-frame confirmation");
        check(!UeiRuntime.IsTextInputFocused && s.Camera.timeHeldThrow == 0f,
            "Selection must release search focus and end a previous throw charge");
        Step(held: [KeyCode.Mouse0, KeyCode.A]);
        s.Tick();
        check(s.Calls == 0 && s.Placement.Phase == UeiEntityPlacement.PlacementPhase.AwaitRelease,
            "Dragging the selecting press into the world must not arm or spawn an entity");
        check(UeiInput.GetKey(KeyCode.A) && s.Camera.body.moveDir.x == 1f && s.Camera.wantsJumpInput,
            "Placement must preserve ordinary keyboard movement and jump state");
        check(!UeiInput.GetKey(KeyCode.Mouse0) && UeiInput.ShouldBlockWorldPointerInput,
            "Placement owns world mouse input even outside UEI");
        Step(up: [KeyCode.Mouse0]);
        s.Tick();
        check(s.Calls == 0 && s.Placement.Phase == UeiEntityPlacement.PlacementPhase.Aiming,
            "The selecting release must only arm placement");
        check(!UeiInput.GetKeyUp(KeyCode.Mouse0), "The selecting release must not reach world actions");

        UnityEngine.Camera.main!.Project = mouse => new(mouse.x * 2f - 7f, mouse.y / 4f - 100f, 12f);
        Input.mousePosition = new(41f, 60f, 0f);
        Step(held: [KeyCode.Mouse0], down: [KeyCode.Mouse0]);
        s.Tick();
        s.Placement.Tick(s.Camera);
        s.Placement.Tick(s.Camera);
        check(s.Calls == 1 && !s.Placement.IsActive && s.LastPosition.x == 75f && s.LastPosition.y == -85f,
            "A new press must submit the camera-projected world point once across all recovery hooks");
        check(s.Commands.Single().SequenceEqual(new[] { "spawn", EntityId, "75,-85", "1", "1" }),
            "Local placement must retain the selected point, including negative coordinates, in the native command");
        check(!UeiInput.GetKeyDown(KeyCode.Mouse0) && UeiInput.ShouldBlockWorldPointerInput,
            "The confirming press must remain captured after placement completes");
        Step(held: [KeyCode.Mouse0]);
        s.Tick();
        check(s.Calls == 1 && !UeiInput.GetKey(KeyCode.Mouse0), "Holding confirmation must neither respawn nor attack");
        Step(up: [KeyCode.Mouse0]);
        check(!UeiInput.GetKeyUp(KeyCode.Mouse0), "The confirming release must remain captured");
        Step(held: [KeyCode.Mouse0], down: [KeyCode.Mouse0]);
        check(UeiInput.GetKeyDown(KeyCode.Mouse0) && !UeiInput.ShouldBlockWorldPointerInput,
            "The next independent click must restore native world input");
    }

    private static void ReleasedSelection(Action<bool, string> check)
    {
        Scenario s = new();
        s.Begin(check);
        s.Tick();
        check(s.Calls == 0, "A pointer-click callback's release frame must never spawn");
        Step(held: [KeyCode.Mouse0], down: [KeyCode.Mouse0]);
        s.Tick();
        check(s.Calls == 1, "A released UI selection must accept the very next press without requiring an extra neutral frame");
    }

    private static void BlockedTargets(Action<bool, string> check)
    {
        Scenario s = new();
        s.Begin(check);
        UIUtil.Hits.Add(new() { module = new GraphicRaycaster() });
        Step(held: [KeyCode.Mouse0], down: [KeyCode.Mouse0]);
        s.Tick();
        s.Placement.Tick(s.Camera);
        check(s.Calls == 0 && s.Placement.IsActive && !s.Placement.CanPlace,
            "Clicking a game UI surface must leave placement pending without dispatch");
        check(UIUtil.RaycastCalls == 1, "Repeated recovery hooks must share one UI raycast sample per frame");
        UIUtil.Hits.Clear();
        UIUtil.Hits.Add(new() { module = new object() });
        Step(up: [KeyCode.Mouse0]);
        s.Tick();
        Step(held: [KeyCode.Mouse0], down: [KeyCode.Mouse0]);
        s.Tick();
        check(s.Calls == 1, "World/physics raycasts must not be mistaken for an obstructing UI surface");

        foreach (Vector3 mouse in new[] { new Vector3(-1f, 50f, 0f), new Vector3(20f, -1f, 0f),
            new Vector3(Screen.width, 50f, 0f), new Vector3(20f, Screen.height, 0f), new Vector3(float.NaN, 30f, 0f) })
        {
            s = new();
            s.Begin(check);
            Input.mousePosition = mouse;
            Step(held: [KeyCode.Mouse0], down: [KeyCode.Mouse0]);
            s.Tick();
            check(s.Calls == 0 && s.Placement.IsActive && !s.Placement.CanPlace,
                "Out-of-window or invalid screen positions must never become spawn commands");
        }
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            s = new();
            s.Begin(check);
            UnityEngine.Camera.main!.Project = _ => new(invalid, 0f, 0f);
            Step(held: [KeyCode.Mouse0], down: [KeyCode.Mouse0]);
            s.Tick();
            check(s.Calls == 0 && !s.Placement.CanPlace, "Non-finite camera projections must not dispatch");
            check(!UeiWorldSpawning.SpawnEntity(EntityId, new(invalid, 0f)) && s.Commands.Count == 0,
                "The native dispatch boundary must independently reject non-finite world coordinates");
        }
    }

    private static void InvalidSelections(Action<bool, string> check)
    {
        foreach (UeiEntry invalid in new[] { new UeiEntry { Kind = UeiEntryKind.Item, Id = EntityId },
            new UeiEntry { Kind = UeiEntryKind.Liquid, Id = "water" }, new UeiEntry { Kind = UeiEntryKind.Entity, Id = "missing" } })
        {
            Scenario s = new();
            check(!s.Placement.TryBegin(s.Camera, invalid) && s.Camera.radialOpen && s.Calls == 0,
                "A non-entity or unknown resource must not close inventory or enter placement");
        }
        Scenario drag = new();
        drag.Camera.dragItem = new object();
        check(!drag.Placement.TryBegin(drag.Camera, Entry) && drag.Camera.radialOpen,
            "Selection must not discard an inventory item being dragged");
        foreach (Action disable in new Action[] { () => UeiPanel.CheatConfigured = false,
            () => UeiPanel.CheatAllowed = false, () => PauseHandler.paused = true, () => Application.isFocused = false,
            () => MinigameBase.main.currentMinigame = new() })
        {
            Scenario s = new();
            disable();
            check(!s.Placement.TryBegin(s.Camera, Entry) && s.Camera.radialOpen,
                "Unavailable cheats or gameplay must be rejected before inventory changes");
        }
    }

    private static void Cancellation(Action<bool, string> check)
    {
        Scenario s = new();
        s.Begin(check);
        Step(held: [KeyCode.Mouse1], down: [KeyCode.Mouse1]);
        s.Tick();
        check(!s.Placement.IsActive && s.Calls == 0 && !UeiInput.GetKeyDown(KeyCode.Mouse1),
            "Right-click cancellation must not trigger another world action");
        Step(up: [KeyCode.Mouse1]);
        check(!UeiInput.GetKeyUp(KeyCode.Mouse1), "The cancelling right-button release must remain captured");
        Step(held: [KeyCode.Mouse1], down: [KeyCode.Mouse1]);
        check(UeiInput.GetKeyDown(KeyCode.Mouse1), "A later independent right click must work normally");

        foreach (KeyCode pause in new[] { KeyCode.Escape, KeyCode.F1 })
        {
            s = new();
            KeyBinds.Binds["pause"] = pause;
            s.Begin(check);
            Step(held: [pause], down: [pause]);
            s.Tick();
            check(!s.Placement.IsActive && !UeiInput.GetKeyDown(pause) && !PauseHandler.paused,
                "Cancellation must run before native pause handling, including a rebound pause key");
            Step(held: [pause]);
            check(!UeiInput.GetKey(pause), "The held cancel key must not leak after placement ends");
            Step(up: [pause]);
            check(!UeiInput.GetKeyUp(pause), "The cancelling navigation-key release must remain captured");
            Step(held: [pause], down: [pause]);
            check(UeiInput.GetKeyDown(pause), "A later navigation-key press must reach the game again");
        }
        KeyBinds.Binds["pause"] = KeyCode.Escape;
        foreach (KeyCode inventory in new[] { KeyCode.Tab, KeyCode.D, KeyCode.Mouse2 })
        {
            s = new();
            KeyBinds.Binds["toggleinventory"] = inventory;
            s.Begin(check);
            Step(held: [inventory], down: [inventory]);
            s.Tick();
            check(!s.Placement.IsActive && s.Calls == 0
                && (inventory == KeyCode.Mouse2
                    ? s.Camera.radialOpen && !UeiInput.GetKeyDown(inventory) && UeiInput.ShouldBlockWorldPointerInput
                    : UeiInput.GetKeyDown(inventory)),
                "Inventory cancellation must preserve keyboard navigation and safely open mouse-bound inventory without another world action");
        }
        KeyBinds.Binds["toggleinventory"] = KeyCode.Tab;
    }

    private static void SessionCleanup(Action<bool, string> check)
    {
        var changes = new (string Name, Action<PlayerCamera> Change)[]
        {
            ("death", camera => camera.body.alive = false),
            ("new body", camera => camera.body = new()),
            ("new player camera", _ => PlayerCamera.main = new()),
            ("new canvas", camera => camera.mainCanvas = new()),
            ("new world camera", _ => UnityEngine.Camera.main = new()),
            ("scene change", _ => SceneManager.ActiveHandle++),
            ("focus loss", _ => Application.isFocused = false),
            ("pause", _ => PauseHandler.paused = true),
            ("minigame opened", _ => MinigameBase.main.currentMinigame = new()),
            ("cheat switch off", _ => UeiPanel.CheatConfigured = false),
            ("permission revoked", _ => UeiPanel.CheatAllowed = false),
            ("network session changed", _ => UeiPanel.NetworkState = TogetherNetworkState.Unknown),
            ("console opened", _ => ConsoleScript.instance.active = true),
            ("crafting opened", camera => camera.craftingPanel = new("craft")),
            ("trade opened", camera => camera.tradeMenu = new("trade")),
            ("medical opened", camera => camera.woundView = new("medical")),
            ("inventory reopened", camera => camera.radialOpen = true),
        };
        foreach (var change in changes)
        {
            Scenario s = new();
            s.Begin(check);
            Step();
            s.Tick();
            change.Change(s.Camera);
            s.Placement.Tick(s.Camera);
            check(!s.Placement.IsActive && s.Calls == 0 && !UeiRuntime.IsEntityPlacementActive,
                "Session changes must cancel even after an earlier tick in the same frame: " + change.Name);
        }
    }

    private static void RetryAndRequests(Action<bool, string> check)
    {
        Scenario s = new() { Result = UeiPanel.CheatActionResult.Failed };
        s.Begin(check);
        Step(held: [KeyCode.Mouse0], down: [KeyCode.Mouse0]);
        s.Tick();
        s.Placement.Tick(s.Camera);
        check(s.Calls == 1 && s.Placement.IsActive, "A dispatch failure must stay pending without duplicate retries");
        Step(held: [KeyCode.Mouse0]);
        s.Tick();
        check(s.Calls == 1, "A held failed confirmation must not automatically retry");
        Step(up: [KeyCode.Mouse0]);
        s.Tick();
        s.Result = UeiPanel.CheatActionResult.Completed;
        Step(held: [KeyCode.Mouse0], down: [KeyCode.Mouse0]);
        s.Tick();
        check(s.Calls == 2 && !s.Placement.IsActive, "A new independent press may retry a failed spawn");

        foreach (TogetherNetworkState network in new[] { TogetherNetworkState.Client, TogetherNetworkState.Server })
        {
            s = new(network) { Result = UeiPanel.CheatActionResult.RequestSent };
            s.Begin(check);
            UnityEngine.Camera.main!.Project = _ => new(-12.5f, 31.25f, 0f);
            Step(held: [KeyCode.Mouse0], down: [KeyCode.Mouse0]);
            s.Tick();
            s.Placement.Tick(s.Camera);
            check(s.Calls == 1 && !s.Placement.IsActive && s.Commands.Count == 0
                && s.LastPosition.x == -12.5f && s.LastPosition.y == 31.25f,
                "A network submission must consume one selected point and never also instantiate locally");
            check(s.Camera.Alerts.Last() == UeiI18n.T("status.cheatSpawnRequestSent"),
                "A void network submission must report a request rather than confirmed generation");
        }
    }

    private static void ReentrantDispatch(Action<bool, string> check)
    {
        Scenario s = new();
        int calls = 0;
        UeiEntityPlacement placement = null!;
        placement = new((_, _) =>
        {
            calls++;
            placement.Tick(s.Camera);
            return UeiPanel.CheatActionResult.Completed;
        });
        UeiRuntime.Placement = placement;
        check(placement.TryBegin(s.Camera, Entry), "Reentrant-dispatch fixture must enter placement");
        Step(held: [KeyCode.Mouse0], down: [KeyCode.Mouse0]);
        s.Tick();
        check(calls == 1 && !placement.IsActive, "A spawn callback that reenters runtime must not duplicate generation");

        s = new();
        placement = new((_, _) => throw new InvalidOperationException("dispatch failed"));
        UeiRuntime.Placement = placement;
        check(placement.TryBegin(s.Camera, Entry), "Exception fixture must enter placement");
        Step(held: [KeyCode.Mouse0], down: [KeyCode.Mouse0]);
        s.Tick();
        check(!placement.IsActive && !UeiRuntime.IsEntityPlacementActive && !UeiInput.GetKeyDown(KeyCode.Mouse0),
            "Unexpected dispatch exceptions must clean up placement while retaining click capture");
    }
}
