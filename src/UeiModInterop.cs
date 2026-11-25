using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using UnityEngine;

// Cache misses as well as hits. Optional mods loaded later invalidate the cache,
// without enumerating every type in every assembly for each catalog entry.
internal static class UeiOptionalTypes
{
    private static readonly object Gate = new();
    private static readonly Dictionary<string, Type?> Types = new(StringComparer.Ordinal);
    private static Assembly[] _assemblies = Array.Empty<Assembly>();
    private static int _generation;
    private static int _cachedGeneration = -1;

    static UeiOptionalTypes()
    {
        AppDomain.CurrentDomain.AssemblyLoad += (_, _) => Interlocked.Increment(ref _generation);
    }

    internal static int Generation => Volatile.Read(ref _generation);

    internal static Type? Find(string fullName)
    {
        lock (Gate)
        {
            int generation = Generation;
            if (_cachedGeneration != generation)
            {
                Types.Clear();
                _assemblies = AppDomain.CurrentDomain.GetAssemblies();
                _cachedGeneration = generation;
            }

            if (Types.TryGetValue(fullName, out Type? cached))
            {
                return cached;
            }

            Type? found = null;
            foreach (Assembly assembly in _assemblies)
            {
                try { found = assembly.GetType(fullName, throwOnError: false); }
                catch { continue; }
                if (found != null) break;
            }

            Types[fullName] = found;
            return found;
        }
    }

    internal static MethodInfo? StaticMethod(Type? type, string name, Type returnType, params Type[] parameters)
    {
        if (type?.IsVisible != true) return null;
        try
        {
            MethodInfo? method = type.GetMethod(name, BindingFlags.Public | BindingFlags.Static,
                null, parameters, null);
            return method?.ReturnType == returnType ? method : null;
        }
        catch { return null; }
    }
}

internal static class RshLibHelper
{
    private static int _generation = -1;
    private static FieldInfo? _registryField;
    private static FieldInfo? _spriteField;

    private static IDictionary? Registry()
    {
        int generation = UeiOptionalTypes.Generation;
        if (_generation != generation)
        {
            _registryField = UeiOptionalTypes.Find("RshLib.Plugin")?.GetField("itemRegistry",
                BindingFlags.Public | BindingFlags.Static);
            _spriteField = UeiOptionalTypes.Find("RshLib.RshItem")?.GetField("sprite",
                BindingFlags.Public | BindingFlags.Instance);
            _generation = generation;
        }

        // Read the live field, not a cached null/dictionary: registration can occur
        // after assembly loading, and a mod may replace its registry on a new run.
        return _registryField?.GetValue(null) as IDictionary;
    }

    public static Sprite? GetCustomItemSprite(string itemId)
    {
        try
        {
            object? item = Registry()?[GetBaseId(itemId)];
            return item == null ? null : _spriteField?.GetValue(item) as Sprite;
        }
        catch { return null; }
    }

    public static bool IsCustomItem(string itemId)
    {
        try { return Registry()?.Contains(GetBaseId(itemId)) == true; }
        catch { return false; }
    }

    public static string GetBaseId(string id)
    {
        if (string.IsNullOrEmpty(id)) return id;
        int dollarIndex = id.IndexOf('$');
        return dollarIndex >= 0 ? id.Substring(0, dollarIndex) : id;
    }

    public static string TryResolveKey(string fullId, UeiEntryKind kind, Dictionary<string, UeiEntry> entriesByKey)
    {
        string entryKey = UeiCatalog.EntryKey(kind, fullId);
        if (entriesByKey.ContainsKey(entryKey)) return entryKey;
        string baseId = GetBaseId(fullId);
        string baseKey = UeiCatalog.EntryKey(kind, baseId);
        return baseId != fullId && entriesByKey.ContainsKey(baseKey) ? baseKey : entryKey;
    }
}

internal static class CuCoreLibHelper
{
    private static int _generation = -1;
    private static MethodInfo? _tryGetIcon;
    private static MethodInfo? _tryGetCustomInfo;
    private static MethodInfo? _tryGetItemInfo;
    private static MethodInfo? _instantiate;
    private static MethodInfo? _isAvailable;
    private static MethodInfo? _isRunning;
    private static MethodInfo? _isClient;
    private static MethodInfo? _isServer;

    private static void EnsureBindings()
    {
        int generation = UeiOptionalTypes.Generation;
        if (_generation == generation) return;

        Type? registry = UeiOptionalTypes.Find("CUCoreLib.Registries.ItemRegistry");
        _tryGetIcon = UeiOptionalTypes.StaticMethod(registry, "TryGetIcon", typeof(bool),
            typeof(string), typeof(Sprite).MakeByRefType());
        Type? info = UeiOptionalTypes.Find("CUCoreLib.Data.CustomItemInfo");
        _tryGetCustomInfo = info == null ? null : UeiOptionalTypes.StaticMethod(registry, "TryGetCustomInfo",
            typeof(bool), typeof(string), info.MakeByRefType());
        _tryGetItemInfo = UeiOptionalTypes.StaticMethod(registry, "TryGetItemInfo", typeof(bool),
            typeof(string), typeof(ItemInfo).MakeByRefType());
        _instantiate = UeiOptionalTypes.StaticMethod(UeiOptionalTypes.Find("CUCoreLib.Helpers.CustomInstantiate"),
            "InstantiateReturn", typeof(GameObject), typeof(string), typeof(Vector3), typeof(Quaternion), typeof(float?));
        Type? multiplayer = UeiOptionalTypes.Find("CUCoreLib.Networking.MultiplayerApi");
        _isAvailable = UeiOptionalTypes.StaticMethod(multiplayer, "get_IsAvailable", typeof(bool));
        _isRunning = UeiOptionalTypes.StaticMethod(multiplayer, "get_IsRunning", typeof(bool));
        _isClient = UeiOptionalTypes.StaticMethod(multiplayer, "get_IsClient", typeof(bool));
        _isServer = UeiOptionalTypes.StaticMethod(multiplayer, "get_IsServer", typeof(bool));
        _generation = generation;
    }

    public static Sprite? GetCustomItemSprite(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return null;
        EnsureBindings();
        if (_tryGetIcon == null) return null;
        try
        {
            object?[] args = { itemId, null };
            return _tryGetIcon?.Invoke(null, args) is true ? args[1] as Sprite : null;
        }
        catch { return null; }
    }

    public static bool IsCustomItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return false;
        EnsureBindings();
        if (_tryGetCustomInfo == null) return false;
        try { return _tryGetCustomInfo?.Invoke(null, new object?[] { itemId, null }) is true; }
        catch { return false; }
    }

    public static bool TryGetItemInfo(string itemId, out ItemInfo? info)
    {
        info = null;
        if (string.IsNullOrWhiteSpace(itemId)) return false;
        EnsureBindings();
        if (_tryGetItemInfo == null) return false;
        try
        {
            object?[] args = { itemId, null };
            if (_tryGetItemInfo.Invoke(null, args) is not true) return false;
            info = args[1] as ItemInfo;
            return info != null;
        }
        catch { return false; }
    }

    public static string GetItemText(string localeKey)
    {
        if (string.IsNullOrWhiteSpace(localeKey)) return string.Empty;
        // CUCore's locale overlay is already merged into the game's public Locale
        // dictionaries. ItemInfo supplies a public-data fallback when a key is absent.
        // Do not bind internal LocaleLoader or mutating LocaleRegistry.Get.
        // TryGetItemInfo logs misses and can inject registered entries. Probe the
        // read-only registration API first, rather than sending locale keys to it.
        if (IsCustomItem(localeKey) && TryGetItemInfo(localeKey, out ItemInfo? info))
            return info!.fullName ?? string.Empty;
        if (localeKey.EndsWith("dsc", StringComparison.Ordinal)
            && IsCustomItem(localeKey.Substring(0, localeKey.Length - 3))
            && TryGetItemInfo(localeKey.Substring(0, localeKey.Length - 3), out ItemInfo? descriptionInfo))
            return descriptionInfo!.description ?? string.Empty;
        return string.Empty;
    }

    public static GameObject? CreateInstance(string itemId, Vector3 position, float rotation)
    {
        if (!IsCustomItem(itemId) || _instantiate == null) return null;
        try
        {
            return _instantiate.Invoke(null, new object?[] { itemId, position, Quaternion.Euler(0f, 0f, rotation), 1f }) as GameObject;
        }
        catch (Exception ex)
        {
            UeiPlugin.LogWarning($"UEI CUCoreLib factory failed for {itemId}: {ex.Message}");
            return null;
        }
    }

    public static bool TryGetRunningNetworkState(out TogetherNetworkState state)
    {
        EnsureBindings();
        state = TogetherNetworkState.Unknown;
        // A missing/uninitialized bridge is not proof that multiplayer is offline.
        // Let the caller fall back to the native network state in that case.
        if (ReadBool(_isAvailable) != true || ReadBool(_isRunning) != true) return false;
        bool? client = ReadBool(_isClient);
        bool? server = ReadBool(_isServer);
        if (client == true && server == false) state = TogetherNetworkState.Client;
        else if (server == true && client == false) state = TogetherNetworkState.Server;
        return true;
    }

    private static bool? ReadBool(MethodInfo? getter)
    {
        try { return getter?.Invoke(null, null) is bool value ? value : null; }
        catch { return null; }
    }
}

internal static class SexModHelper
{
    private static int _generation = -1;
    private static MethodInfo? _isRegistered;
    private static MethodInfo? _getIcon;
    private static MethodInfo? _createInstance;

    private static void EnsureBindings()
    {
        int generation = UeiOptionalTypes.Generation;
        if (_generation == generation) return;

        Type? registry = UeiOptionalTypes.Find("ScavPrototypeSexMod.Registers.ItemRegistry");
        _isRegistered = UeiOptionalTypes.StaticMethod(registry, "IsRegistered", typeof(bool), typeof(string));
        _getIcon = UeiOptionalTypes.StaticMethod(registry, "GetIcon", typeof(Sprite), typeof(string));
        _createInstance = UeiOptionalTypes.StaticMethod(registry, "CreateInstance", typeof(GameObject),
            typeof(string), typeof(Vector2), typeof(float));
        _generation = generation;
    }

    public static bool IsRegistered(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return false;
        EnsureBindings();
        if (_isRegistered == null) return false;
        try { return _isRegistered?.Invoke(null, new object?[] { itemId }) is true; }
        catch { return false; }
    }

    public static Sprite? GetCustomItemSprite(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return null;
        EnsureBindings();
        if (_getIcon == null) return null;
        try { return _getIcon?.Invoke(null, new object?[] { itemId }) as Sprite; }
        catch { return null; }
    }

    public static GameObject? CreateInstance(string itemId, Vector2 position, float rotation)
    {
        if (!IsRegistered(itemId)) return null;
        try { return _createInstance?.Invoke(null, new object?[] { itemId, position, rotation }) as GameObject; }
        catch (Exception ex)
        {
            UeiPlugin.LogWarning($"UEI SexMod factory failed for {itemId}: {ex.Message}");
            return null;
        }
    }
}
