using UnityEngine;

internal static class InputTests
{
    internal static void Run(Action<bool, string> check)
    {
        UeiInput.Reset();
        UeiRuntime.IsTextInputFocused = false;
        UeiRuntime.IsPointerOverPanel = true;
        PlayerCamera camera = new()
        {
            wantsJumpInput = true,
            timeHeldThrow = 0.4f,
        };
        camera.body.moveDir = new Vector2(1f, 0f);
        camera.body.crouching = true;
        Input.SetFrame(1, held: new[] { KeyCode.D, KeyCode.Space }, down: new[] { KeyCode.Tab });
        UeiInput.PreparePlayerInput(camera);
        check(UeiInput.GetKey(KeyCode.D) && UeiInput.GetKey(KeyCode.Space), "Panel hover must preserve movement and jump keys");
        check(UeiInput.GetKeyDown(KeyCode.Tab), "Panel hover must preserve inventory toggle");
        check(camera.body.moveDir.x == 1f && camera.wantsJumpInput && camera.body.crouching, "Hover must not clear gameplay state");
        check(UeiInput.ShouldBlockWorldPointerInput, "Hover must block world pointer actions");

        Input.SetFrame(2, held: new[] { KeyCode.Mouse0 }, down: new[] { KeyCode.Mouse0 });
        check(!UeiInput.GetKeyDown(KeyCode.Mouse0) && !UeiInput.GetKey(KeyCode.Mouse0), "A UEI click must not start an attack");
        UeiRuntime.IsPointerOverPanel = false;
        Input.SetFrame(3, held: new[] { KeyCode.Mouse0, KeyCode.A });
        check(!UeiInput.GetKey(KeyCode.Mouse0) && UeiInput.GetKey(KeyCode.A), "Leaving UEI with its mouse button held must preserve click capture and keyboard movement");
        Input.SetFrame(4, up: new[] { KeyCode.Mouse0 });
        check(!UeiInput.GetKeyUp(KeyCode.Mouse0) && UeiInput.ShouldBlockWorldPointerInput, "A captured click must consume its release outside UEI");
        Input.SetFrame(5, held: new[] { KeyCode.Mouse0 }, down: new[] { KeyCode.Mouse0 });
        check(UeiInput.GetKeyDown(KeyCode.Mouse0) && !UeiInput.ShouldBlockWorldPointerInput, "A new world click after release must work");

        UeiInput.Reset();
        UeiRuntime.IsTextInputFocused = true;
        Input.SetFrame(6, held: new[] { KeyCode.W, KeyCode.Space }, down: new[] { KeyCode.W });
        UeiInput.PreparePlayerInput(camera);
        check(!UeiInput.GetKey(KeyCode.W) && !UeiInput.GetKeyDown(KeyCode.W), "Typing must not trigger gameplay keys");
        check(camera.body.moveDir.x == 0f && camera.body.moveDir.y == 0f && !camera.body.crouching, "Typing must clear previously latched movement");
        check(!camera.wantsJumpInput && camera.body.endedJump && camera.timeHeldThrow == 0f, "Typing must end a previously latched jump and throw charge");
        check(UeiInput.ShouldBlockWorldPointerInput, "Search focus must block world interaction even outside the panel");

        Input.SetFrame(7, down: new[] { KeyCode.Escape });
        UeiInput.PreparePlayerInput(camera);
        check(!UeiRuntime.IsTextInputFocused && UeiInput.GetKeyDown(KeyCode.Escape), "Escape must dismiss search and reach the original navigation logic");
        UeiRuntime.IsTextInputFocused = true;
        Input.SetFrame(8, down: new[] { KeyCode.Tab });
        UeiInput.PreparePlayerInput(camera);
        check(!UeiRuntime.IsTextInputFocused && UeiInput.GetKeyDown(KeyCode.Tab), "Inventory toggle must dismiss search and reach the original game");
        KeyBinds.Binds["toggleinventory"] = KeyCode.D;
        UeiRuntime.IsTextInputFocused = true;
        Input.SetFrame(9, down: new[] { KeyCode.D });
        UeiInput.PreparePlayerInput(camera);
        check(UeiRuntime.IsTextInputFocused && !UeiInput.GetKeyDown(KeyCode.D), "A text key rebound to inventory must stay text while editing");
        KeyBinds.Binds["toggleinventory"] = KeyCode.Tab;

        UeiRuntime.IsTextInputFocused = false;
        UeiRuntime.IsPointerOverPanel = true;
        KeyBinds.Binds["jump"] = KeyCode.Mouse1;
        camera.wantsJumpInput = true;
        Input.SetFrame(10, held: new[] { KeyCode.Mouse1 });
        UeiInput.PreparePlayerInput(camera);
        check(!camera.wantsJumpInput && camera.body.endedJump, "A mouse-bound jump must not remain latched over UEI");
        KeyBinds.Binds["jump"] = KeyCode.Space;

        camera.dragItem = new object();
        camera.dragImage.enabled = true;
        UeiInput.CancelWorldDrag(camera);
        check(camera.dragItem == null && !camera.dragImage.enabled, "Releasing on UEI must cancel the drag without world actions");
        UeiInput.Reset();
        UeiRuntime.IsPointerOverPanel = false;
        Input.SetFrame(11, held: new[] { KeyCode.Mouse1 }, down: new[] { KeyCode.Mouse1 });
        check(UeiInput.GetKeyDown(KeyCode.Mouse1) && !UeiInput.ShouldBlockWorldPointerInput, "Runtime recreation must not keep old pointer capture");
        Console.WriteLine("PASS: hover movement, text focus, navigation, captured clicks, rebound keys, and drag cancellation");
    }
}
