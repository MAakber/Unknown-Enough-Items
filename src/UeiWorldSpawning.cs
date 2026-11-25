using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;

internal enum UeiWorldSpawnResult { Failed, Completed, Nearby }

internal static class UeiWorldSpawning
{
    internal const string LiquidCommand = "uei_liquid";
    internal const string BottleId = "craftingbottle";
    internal const float BottleLifetime = 300f;
    private static Command? _liquidCommand;
    private static float _lastRegistrationCheck = -999f;

    internal static bool IsSafeToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        foreach (char c in value)
            if (char.IsWhiteSpace(c) || char.IsControl(c) || c == '"' || c == '\'' || c == ';') return false;
        return true;
    }

    internal static string EntityCommand(string id, Vector2 position)
        => "spawn " + id + " " + position.x.ToString("R", CultureInfo.InvariantCulture)
            + "," + position.y.ToString("R", CultureInfo.InvariantCulture) + " 1 1";

    internal static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    internal static bool SpawnEntity(string id, Vector2 position)
    {
        if (!IsSafeToken(id) || !UeiEntities.Contains(id)
            || !IsFinite(position.x) || !IsFinite(position.y)) return false;
        Command? spawn = ConsoleScript.SearchExact("spawn");
        if (spawn == null) return false;
        // Use the registered native action, including installed mod wrappers,
        // placement bookkeeping, cheat checks and exception reporting.
        spawn.action(EntityCommand(id, position).Split(' '));
        return true;
    }

    internal static void EnsureCommands(bool immediate = false)
    {
        if (!immediate && Time.unscaledTime - _lastRegistrationCheck < 1f) return;
        _lastRegistrationCheck = Time.unscaledTime;
        if (ConsoleScript.instance == null || ConsoleScript.Commands == null
            || ConsoleScript.SearchExact("spawn") == null) return;
        Command? existing = ConsoleScript.SearchExact(LiquidCommand);
        if (existing != null) return;
        _liquidCommand ??= new Command(LiquidCommand, "UEI - give a liquid in a temporary bottle.",
            ExecuteLiquidCommand, null, ("string liquid_id", "Liquid to give to the command caller"));
        // This is the same registration list used by Together.Con.RegisterCommand.
        // Together's existing authenticated admin transport dispatches it on the host.
        ConsoleScript.Commands.Add(_liquidCommand);
    }

    internal static void ExecuteLiquidCommand(string[] args)
    {
        TogetherNetworkState state = UeiPanel.GetTogetherNetworkState(out _, out _, out _);
        if (state == TogetherNetworkState.Client || state == TogetherNetworkState.Unknown
            || !UeiPanel.HasCheatPermission()) throw new InvalidOperationException("UEI: cheats are unavailable.");
        if (args.Length != 2 || !IsSafeToken(args[1]) || !Liquids.LiquidExists(args[1]))
            throw new ArgumentException("UEI: invalid liquid.");
        Body? body = state == TogetherNetworkState.Server
            ? TogetherSpawnHelper.CommandCallerBody() : PlayerCamera.main?.body;
        if (body == null) throw new InvalidOperationException("UEI: command caller has no body.");
        UeiWorldSpawnResult result = GiveLiquid(body, args[1], state == TogetherNetworkState.Server);
        if (result == UeiWorldSpawnResult.Failed) throw new InvalidOperationException("UEI: liquid delivery failed.");
        TogetherSpawnHelper.Log(result == UeiWorldSpawnResult.Nearby
            ? UeiI18n.T("status.cheatSpawnedNearby") : UeiI18n.T("status.cheatLiquidGiven"));
    }

    internal static UeiWorldSpawnResult GiveLiquid(Body body, string id, bool networkServer = false)
    {
        if (body == null || !IsSafeToken(id) || !Liquids.LiquidExists(id)) return UeiWorldSpawnResult.Failed;
        GameObject? instance = null;
        try
        {
            // The original recipe path uses this same resource, fills it via
            // AddLiquid, attempts pickup, and removes the bottle after 300 seconds.
            instance = Utils.Create(BottleId, body.transform.position, 0f);
            if (instance == null) return UeiWorldSpawnResult.Failed;
            Item? item = instance.GetComponent<Item>();
            WaterContainerItem? container = instance.GetComponent<WaterContainerItem>();
            if (item == null || container == null || !(container.Capacity > 0f)
                || float.IsInfinity(container.Capacity))
            {
                UnityEngine.Object.Destroy(instance);
                return UeiWorldSpawnResult.Failed;
            }
            container.DrainAll();
            float added = container.AddLiquid(id, container.Capacity);
            if (!(added > 0f) || float.IsInfinity(added))
            {
                UnityEngine.Object.Destroy(instance);
                return UeiWorldSpawnResult.Failed;
            }
            UnityEngine.Object.Destroy(instance, BottleLifetime);
            try
            {
                if (networkServer) TogetherSpawnHelper.PickUp(body, item);
                else body.AutoPickUpItem(item);
            }
            catch (Exception ex)
            {
                // Keep a successfully filled bottle available at the player's
                // feet even if the inventory cannot accept it.
                UeiPlugin.LogWarning("UEI liquid pickup: " + ex.Message);
            }
            return body.GetAllItemsThorough().Contains(item) ? UeiWorldSpawnResult.Completed : UeiWorldSpawnResult.Nearby;
        }
        catch (Exception ex)
        {
            if (instance != null) UnityEngine.Object.Destroy(instance);
            UeiPlugin.LogWarning("UEI liquid delivery failed: " + ex.Message);
            return UeiWorldSpawnResult.Failed;
        }
    }

    internal static void InitializeTrader(TraderScript trader)
    {
        // World generation sets a movement range after instantiation; native
        // spawn does not. Preserve any range supplied by world generation/mods.
        if (trader.MoveRange.min != 0f || trader.MoveRange.max != 0f) return;
        TogetherNetworkState state = UeiPanel.GetTogetherNetworkState(out _, out _, out _);
        if (state == TogetherNetworkState.Client || state == TogetherNetworkState.Unknown) return;
        float x = trader.transform.position.x;
        trader.MoveRange = new RangeF(x - 5f, x + 5f);
        trader.farEnoughToMove = true;
    }
}

internal static class TogetherSpawnHelper
{
    private static int _generation = -1;
    private static MethodInfo? _caller;
    private static FieldInfo? _body;
    private static MethodInfo? _pickup;
    private static MethodInfo? _log;

    private static void Bind()
    {
        int generation = UeiOptionalTypes.Generation;
        if (_generation == generation) return;
        Type? player = UeiOptionalTypes.Find("Together.ScavPlayer");
        _caller = player == null ? null : UeiOptionalTypes.StaticMethod(UeiOptionalTypes.Find("Together.Con"),
            "GetCommandCallerPlayer", player);
        _body = player?.GetField("body", BindingFlags.Instance | BindingFlags.Public);
        if (_body?.FieldType != typeof(Body)) _body = null;
        _pickup = UeiOptionalTypes.StaticMethod(UeiOptionalTypes.Find("CasualtiesTogetherUtils.Util_BodyExtensions"),
            "ForceAutoPickUpItem", typeof(void), typeof(Body), typeof(Item));
        _log = UeiOptionalTypes.StaticMethod(UeiOptionalTypes.Find("Together.Con"), "LogToConsole", typeof(void), typeof(string));
        _generation = generation;
    }

    internal static Body? CommandCallerBody()
    {
        Bind();
        object? caller = _caller?.Invoke(null, null);
        return caller == null ? null : _body?.GetValue(caller) as Body;
    }

    internal static void PickUp(Body body, Item item)
    {
        Bind();
        if (_pickup == null) throw new MissingMethodException("Together server pickup is unavailable.");
        _pickup.Invoke(null, new object[] { body, item });
    }

    internal static void Log(string message)
    {
        Bind();
        if (_log != null) _log.Invoke(null, new object[] { message });
        else UeiPlugin.LogInfo(message);
    }
}
