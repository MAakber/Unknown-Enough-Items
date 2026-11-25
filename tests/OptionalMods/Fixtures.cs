using UnityEngine;

namespace RshLib
{
    public sealed class RshItem { public Sprite sprite = new(); }
    public static class Plugin
    {
        public static Dictionary<string, RshItem>? itemRegistry;
        public static void Initialize() => itemRegistry = new() { ["rsh"] = new() };
        public static void Replace() => itemRegistry = new() { ["replacement"] = new() };
    }
}

namespace CUCoreLib.Data { public sealed class CustomItemInfo : ItemInfo { } }

namespace CUCoreLib.Registries
{
    public static class ItemRegistry
    {
        public static int MissingInfoRequests;
        public static bool TryGetIcon(string id, out Sprite? icon)
        {
            icon = id == "cu" ? new Sprite() : null;
            return icon != null;
        }

        // Decoy overload: binding only by name/out-argument would be incorrect.
        public static bool TryGetCustomInfo(string id, out int info) => throw new Exception("Wrong overload");
        public static bool TryGetCustomInfo(string id, out Data.CustomItemInfo? info)
        {
            info = id == "cu" ? new Data.CustomItemInfo() : null;
            return info != null;
        }

        public static bool TryGetItemInfo(string id, out int info) => throw new Exception("Wrong item-info overload");
        public static bool TryGetItemInfo(string id, out ItemInfo? info)
        {
            info = id == "cu" ? new Data.CustomItemInfo { fullName = "模组物品", description = "模组描述" } : null;
            if (info == null) MissingInfoRequests++;
            return info != null;
        }
    }

    public static class LocaleRegistry
    {
        public static int Reads;
        public static string Get(string category, string key, string? fallback = null)
        {
            Reads++;
            throw new Exception("UEI must not call the mutating locale registry");
        }
    }
}

namespace CUCoreLib.Helpers
{
    internal static class LocaleLoader
    {
        public static int Reads;
        public static string GetLocalizedText(string category, string key, string? fallback = null)
        {
            Reads++;
            throw new Exception("UEI must not bind the internal locale loader");
        }
    }

    public static class CustomInstantiate
    {
        public static GameObject InstantiateReturn(string id, Vector2 pos, float rot) => throw new Exception("Wrong CUCore factory overload");
        public static GameObject? InstantiateReturn(string id, Vector3 position, Quaternion rotation, float? condition = null)
            => id == "cu" ? new GameObject(id) { WorldPosition = position, Rotation = rotation.z, Condition = condition } : null;
    }
}

namespace CUCoreLib.Networking
{
    public static class MultiplayerApi
    {
        public static bool IsAvailable { get; private set; }
        public static bool IsRunning { get; private set; }
        public static bool IsClient { get; private set; }
        public static bool IsServer { get; private set; }
        public static void Configure(bool available, bool running, bool client, bool server)
        {
            IsAvailable = available;
            IsRunning = running;
            IsClient = client;
            IsServer = server;
        }
    }
}

namespace ScavPrototypeSexMod.Registers
{
    public static class ItemRegistry
    {
        private static readonly HashSet<string> Registered = new() { "sex" };
        public static bool IsRegistered(string id) => Registered.Contains(id);
        public static void RegisterLate() => Registered.Add("late_sex");
        public static Sprite? GetIcon(string id) => IsRegistered(id) ? new Sprite() : null;
        public static GameObject CreateInstance(string id, Transform parent) => throw new Exception("Wrong factory overload");
        public static GameObject? CreateInstance(string id, Vector2 pos, float rot)
            => IsRegistered(id) ? new GameObject(id) { Position = pos, Rotation = rot } : null;
    }
}

namespace UnrelatedMod
{
    internal static class InternalApi
    {
        public static bool HiddenType() => true;
    }
    public static class PublicApi
    {
        private static bool HiddenMethod() => true;
    }
    // Same short name: discovery must use the verified full type name.
    public static class ItemRegistry
    {
        public static bool IsRegistered(string id) => throw new Exception("Wrong registry");
    }
}


namespace Together
{
    public sealed class ScavPlayer { public Body? body; }
    public static class Con
    {
        public static ScavPlayer? Caller;
        public static ScavPlayer? GetCommandCallerPlayer() => Caller;
    }
}
namespace CasualtiesTogetherUtils
{
    public static class Util_BodyExtensions
    {
        public static void ForceAutoPickUpItem(Body body, Item item) => body.AutoPickUpItem(item);
    }
}
