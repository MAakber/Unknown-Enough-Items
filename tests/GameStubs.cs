internal sealed class Setting<T>(T value) { public T Value = value; }
internal static class UeiPlugin
{
    public static Setting<string> LanguageMode = new("en");
    public static int FavoriteSaveCalls;
    public static void SaveFavorites() => FavoriteSaveCalls++;
    public static void LogWarning(string message) { }
    public static void LogInfo(string message) { }
}
internal static class Locale
{
    public static string currentLangName = "en";
    public static readonly Dictionary<string, string> Items = new();
    public static readonly Dictionary<string, string> Other = new();
    public static readonly Dictionary<string, string> Buildings = new();
    public static string GetItem(string key) => Items.GetValueOrDefault(key, key);
    public static string GetOther(string key) => Other.GetValueOrDefault(key, key);
    public static string GetBuilding(string key) => Buildings.GetValueOrDefault(key, key);
}

internal static class UeiRuntime
{
    internal static UeiEntityPlacement? Placement;
    internal static bool IsEntityPlacementActive => Placement?.IsActive ?? false;
    internal static void PrepareEntityPlacementInput(PlayerCamera camera)
    {
        if (camera == PlayerCamera.main) Placement?.Tick(camera);
    }
    public static bool IsTextInputFocused;
    public static bool IsPointerOverPanel;
    public static void ReleaseTextInputFocus() => IsTextInputFocused = false;
}

// Tooltip sink only; this does not execute or emulate Unity's UI system.
internal static class UeiPanel
{
    internal enum CheatActionResult { Failed, Completed, SpawnedNearby, RequestSent, HostRequired }
    internal static (string Name, string Description)? Tooltip;
    internal static int TooltipSetCalls;
    internal static void SetNativeTooltip(string name, string description)
    {
        Tooltip = (name, description);
        TooltipSetCalls++;
    }
    internal static void ClearNativeTooltip() => Tooltip = null;
    internal static TogetherNetworkState NetworkState = TogetherNetworkState.Offline;
    internal static bool CheatAllowed = true;
    internal static bool CheatConfigured = true;
    internal static bool CheatEnabled() => CheatConfigured && CheatAllowed;
    internal static bool HasCheatPermission() => CheatAllowed;
    internal static TogetherNetworkState GetTogetherNetworkState(out Type? con, out Type? net, out Type? mp)
    {
        con = net = mp = null;
        return NetworkState;
    }
}
internal static class KeyBinds
{
    public static readonly Dictionary<string, UnityEngine.KeyCode> Binds = new()
    {
        ["pause"] = UnityEngine.KeyCode.Escape,
        ["toggleinventory"] = UnityEngine.KeyCode.Tab,
        ["jump"] = UnityEngine.KeyCode.Space,
    };
    public static UnityEngine.KeyCode GetBind(string key) => Binds.GetValueOrDefault(key);
}
internal sealed class TestDragImage { public bool enabled; }
internal sealed class PlayerCamera
{
    public static PlayerCamera main = new();
    public Body body = new();
    public UnityEngine.Canvas mainCanvas = new();
    public bool radialOpen;
    public UnityEngine.GameObject? craftingPanel;
    public UnityEngine.GameObject? tradeMenu;
    public UnityEngine.GameObject? woundView;
    public readonly List<string> Alerts = [];
    public void DoAlert(string text, bool important = false) => Alerts.Add(text);
    public bool wantsJumpInput;
    public float timeHeldThrow;
    public object? dragItem;
    public TestDragImage dragImage = new();
}

internal static class PauseHandler { public static bool paused; }
internal sealed class Minigame
{
    public static implicit operator bool(Minigame? value) => value != null;
}
internal sealed class MinigameBase
{
    public static MinigameBase main = new();
    public Minigame? currentMinigame = null;
}
internal static class UIUtil
{
    internal static readonly List<UnityEngine.EventSystems.RaycastResult> Hits = [];
    internal static int RaycastCalls;
    public static List<UnityEngine.EventSystems.RaycastResult> GetEventSystemRaycastResults(UnityEngine.Vector2? pos = null)
    {
        RaycastCalls++;
        return Hits;
    }
}
