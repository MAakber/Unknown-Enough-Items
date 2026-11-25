using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

internal static class UeiNativeAcquisition
{
    private static readonly List<UeiAcquisitionBuilding> Buildings = new();
    private static readonly Dictionary<string, Sprite> BuildingIcons = new(StringComparer.Ordinal);
    private static readonly List<BuildingEntity> Components = new();
    private static readonly UeiAcquisitionIndexCache Cache = new();
    private static readonly Func<UeiAcquisitionSnapshot> ReadSnapshot = CaptureSnapshot;
    private static Task<UeiResourceIndex.Result>? _resourceTask;
    private static string[]? _paths;
    private static ResourceRequest? _request;
    private static string _requestPath = string.Empty;
    private static bool _started;
    private static bool _finished;
    private static bool _hadLoadFailure;
    private static bool _loggedLoadFailure;
    private static bool _loggedRegistryFailure;
    private static bool _sourcesDirty;
    private static bool _hasRegistry;
    private static bool _registryHealthy;
    private static int _nextPath;
    private static int _sourceRevision;
    private static int _lastTickFrame = -1;
    private static float _lastRegistryCheck = -999f;
    private static float _lastPublish = -999f;
    private static ulong _registryStamp;
    private static ulong _entryStamp;

    public static int Revision { get; private set; }
    public static int RegistryRevision { get; private set; }
    public static UeiAcquisitionLoadState State { get; private set; } = UeiAcquisitionLoadState.Loading;
    public static UeiAcquisitionIndex Index => Cache.Current;
    public static Sprite? GetBuildingIcon(string key) => BuildingIcons.TryGetValue(key, out Sprite? sprite) ? sprite : null;

    // All recovery hooks may tick in one frame. Metadata IO runs on a worker;
    // Unity requests and prefab reads stay here on the main thread. At most one
    // request completes and one starts per frame, including with inventory shut.
    public static void Tick()
    {
        if (_lastTickFrame == Time.frameCount) return;
        _lastTickFrame = Time.frameCount;
        if (Item.GlobalItems == null || Item.GlobalItems.Count == 0 || Liquids.Registry == null) return;
        float now = Time.unscaledTime;
        RefreshRegistry(now);
        if (!_started)
        {
            _started = true;
            string dataDirectory = Application.dataPath;
            _resourceTask = Task.Run(() => UeiResourceIndex.Read(dataDirectory));
            UeiPlugin.LogInfo("UEI acquisition: reading the installed game's resource index.");
        }
        if (_resourceTask != null)
        {
            if (!_resourceTask.IsCompleted) return;
            try
            {
                UeiResourceIndex.Result result = _resourceTask.GetAwaiter().GetResult();
                _paths = result.Paths;
                _hadLoadFailure = result.UnavailableFiles.Length > 0;
                if (_hadLoadFailure)
                    UeiPlugin.LogWarning("UEI acquisition resource files unavailable: " + string.Join(", ", result.UnavailableFiles));
                UeiPlugin.LogInfo($"UEI acquisition: queued {_paths.Length} canonical GameObject resources.");
            }
            catch (Exception ex)
            {
                _finished = true;
                SetState(UeiAcquisitionLoadState.Unavailable);
                UeiPlugin.LogWarning("UEI acquisition resource index unavailable: " + ex.Message);
            }
            finally { _resourceTask = null; }
        }
        if (_finished)
        {
            Publish(now);
            UpdateFinishedState();
            return;
        }
        if (_paths == null) return;

        if (_request != null)
        {
            if (!_request.isDone) { Publish(now); return; }
            try
            {
                if (_request.asset is GameObject prefab) CapturePrefab(_requestPath, prefab);
                else RecordLoadFailure(_requestPath, "The resource did not return a GameObject.");
            }
            catch (Exception ex) { RecordLoadFailure(_requestPath, ex.Message); }
            finally
            {
                Components.Clear(); // Do not retain loaded prefab references.
                _request = null;
            }
        }
        if (_nextPath < _paths.Length)
        {
            _requestPath = _paths[_nextPath++];
            try
            {
                _request = Resources.LoadAsync<GameObject>(_requestPath);
                if (_request == null) RecordLoadFailure(_requestPath, "No resource request was returned.");
            }
            catch (Exception ex) { RecordLoadFailure(_requestPath, ex.Message); }
        }
        if (_nextPath == _paths.Length && _request == null)
        {
            _finished = true;
            Publish(now, force: true);
            UpdateFinishedState();
            UeiPlugin.LogInfo($"UEI acquisition: cached {Buildings.Count} building/child source variants; state={State}.");
        }
        else Publish(now);
    }

    private static void CapturePrefab(string resourcePath, GameObject prefab)
    {
        Components.Clear();
        prefab.GetComponentsInChildren(true, Components);
        BuildingEntity? root = prefab.GetComponent<BuildingEntity>();
        UeiEntities.Capture(resourcePath, prefab, root);
        for (int i = 0; i < Components.Count; i++)
        {
            BuildingEntity building = Components[i];
            if (building == null || building.indestructible) continue;
            UeiAcquisitionDrop[] drops = CopyDrops(building.itemsDropOnDestroy);
            UeiAcquisitionDrop[] always = CopyDrops(building.alwaysDrop);
            string[] categories = building.itemCategoriesToAdd ?? Array.Empty<string>();
            if (drops.Length == 0 && always.Length == 0 && (building.guaranteedDropAmount <= 0 || categories.Length == 0)) continue;
            bool child = building.transform != prefab.transform;
            string key = resourcePath + ":" + i.ToString(CultureInfo.InvariantCulture);
            Buildings.Add(new UeiAcquisitionBuilding(
                key, building.id ?? string.Empty,
                FallbackName(building.fullName, building.gameObject.name), building.guaranteedDropAmount,
                categories, drops, always, building.dropChanceMultiplier, animal: building.animal,
                contextId: child ? FallbackName(root?.id, resourcePath) : string.Empty,
                contextName: child ? FallbackName(root?.fullName, prefab.name) : string.Empty));
            // Reuse the prefab already read by this scan. Retain only its sprite,
            // without another Resources.Load or a GameObject reference.
            SpriteRenderer? renderer = building.gameObject.GetComponent<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null) BuildingIcons[key] = renderer.sprite;
            _sourcesDirty = true;
        }
    }

    private static UeiAcquisitionDrop[] CopyDrops(ItemDrop[]? drops)
    {
        if (drops == null || drops.Length == 0) return Array.Empty<UeiAcquisitionDrop>();
        List<UeiAcquisitionDrop> copied = new(drops.Length);
        foreach (ItemDrop drop in drops)
            if (drop != null && !string.IsNullOrWhiteSpace(drop.id))
                copied.Add(new(drop.id, drop.chance, drop.conditionMin, drop.conditionMax));
        return copied.ToArray();
    }

    private static string FallbackName(string? name, string fallback)
        => string.IsNullOrWhiteSpace(name) ? fallback : name!;

    private static void Publish(float now, bool force = false)
    {
        UeiEntities.Publish(now, force);
        if (!_sourcesDirty || !_hasRegistry || (!force && now - _lastPublish < 0.75f)) return;
        _lastPublish = now;
        try
        {
            int nextRevision = _sourceRevision + 1;
            if (Cache.Ensure(nextRevision, _registryStamp, ReadSnapshot)) Revision++;
            _sourceRevision = nextRevision;
            _sourcesDirty = false;
        }
        catch (Exception ex) { LogRegistryFailure(ex); }
    }

    private static void RefreshRegistry(float now)
    {
        if (now - _lastRegistryCheck < 1f) return;
        _lastRegistryCheck = now;
        try
        {
            ulong stamp = RegistryStamp(out ulong entryStamp);
            if (Cache.Ensure(_sourceRevision, stamp, ReadSnapshot)) Revision++;
            if (!_hasRegistry || _entryStamp != entryStamp) RegistryRevision++;
            _entryStamp = entryStamp;
            _registryStamp = stamp;
            _hasRegistry = true;
            _registryHealthy = true;
            _loggedRegistryFailure = false;
        }
        catch (Exception ex)
        {
            LogRegistryFailure(ex);
        }
    }

    private static UeiAcquisitionSnapshot CaptureSnapshot()
    {
        Dictionary<string, UeiAcquisitionItem> items = new(Item.GlobalItems.Comparer);
        foreach (KeyValuePair<string, ItemInfo> pair in Item.GlobalItems)
        {
            // Use the same getter as the game's trader selection.
            ItemInfo info = Item.GetItem(pair.Key);
            List<UeiAcquisitionLiquidContent> contents = new();
            if (info is LiquidItemInfo liquid && liquid.defaultContents != null)
                foreach (LiquidStack stack in liquid.defaultContents)
                    if (stack != null) contents.Add(new(stack.liquidId ?? string.Empty, stack.amount));
            items.Add(pair.Key, new(pair.Key, FallbackName(info.fullName, pair.Key), info.value, info.untradeable, contents.ToArray()));
        }
        Dictionary<string, string[]> pools = ItemLootPool.pool == null
            ? new(StringComparer.Ordinal) : new(ItemLootPool.pool.Comparer);
        if (ItemLootPool.pool != null)
            foreach (KeyValuePair<string, List<string>> pool in ItemLootPool.pool)
                pools.Add(pool.Key, pool.Value == null ? Array.Empty<string>() : pool.Value.ToArray());
        return new(items, pools, new List<string>(Liquids.Registry.Keys), Buildings.ToArray());
    }

    // Poll registry content once per second, without allocations or prefab IO.
    // Counts alone miss same-size pool/registry replacements and field edits.
    private static ulong RegistryStamp(out ulong entryStamp)
    {
        ulong hash = 14695981039346656037UL;
        entryStamp = hash;
        Mix(ref hash, RuntimeHelpers.GetHashCode(Item.GlobalItems));
        Mix(ref hash, Item.GlobalItems.Count);
        Mix(ref entryStamp, RuntimeHelpers.GetHashCode(Item.GlobalItems));
        Mix(ref entryStamp, Item.GlobalItems.Count);
        foreach (KeyValuePair<string, ItemInfo> pair in Item.GlobalItems)
        {
            ItemInfo info = Item.GetItem(pair.Key);
            Mix(ref hash, pair.Key);
            Mix(ref hash, RuntimeHelpers.GetHashCode(info));
            Mix(ref hash, info.fullName);
            Mix(ref hash, info.value.GetHashCode());
            Mix(ref hash, info.untradeable ? 1 : 0);
            Mix(ref entryStamp, pair.Key);
            Mix(ref entryStamp, RuntimeHelpers.GetHashCode(info));
            Mix(ref entryStamp, info.category);
            Mix(ref entryStamp, info.fullName);
            Mix(ref entryStamp, info.description);
            if (info is LiquidItemInfo liquid)
            {
                Mix(ref hash, liquid.capacity.GetHashCode());
                Mix(ref hash, liquid.defaultContents?.Count ?? -1);
                if (liquid.defaultContents != null)
                    foreach (LiquidStack stack in liquid.defaultContents)
                    {
                        Mix(ref hash, stack?.liquidId);
                        Mix(ref hash, stack?.amount.GetHashCode() ?? 0);
                    }
            }
            else Mix(ref hash, -1);
        }
        Mix(ref hash, ItemLootPool.pool == null ? 0 : RuntimeHelpers.GetHashCode(ItemLootPool.pool));
        if (ItemLootPool.pool != null)
        {
            Mix(ref hash, ItemLootPool.pool.Count);
            foreach (KeyValuePair<string, List<string>> pool in ItemLootPool.pool)
            {
                Mix(ref hash, pool.Key);
                Mix(ref hash, pool.Value?.Count ?? -1);
                if (pool.Value != null) foreach (string id in pool.Value) Mix(ref hash, id);
            }
        }
        Mix(ref hash, RuntimeHelpers.GetHashCode(Liquids.Registry));
        Mix(ref hash, Liquids.Registry.Count);
        Mix(ref entryStamp, RuntimeHelpers.GetHashCode(Liquids.Registry));
        Mix(ref entryStamp, Liquids.Registry.Count);
        foreach (KeyValuePair<string, LiquidType> liquid in Liquids.Registry)
        {
            Mix(ref hash, liquid.Key);
            Mix(ref entryStamp, liquid.Key);
            Mix(ref entryStamp, RuntimeHelpers.GetHashCode(liquid.Value));
        }
        return hash;
    }

    private static void UpdateFinishedState()
    {
        if (_paths == null) SetState(UeiAcquisitionLoadState.Unavailable);
        else if (!_registryHealthy) SetState(UeiAcquisitionLoadState.Partial);
        else if (_sourcesDirty || !_hasRegistry || ItemLootPool.pool == null || ItemLootPool.pool.Count == 0)
            SetState(UeiAcquisitionLoadState.Loading);
        else SetState(_hadLoadFailure ? UeiAcquisitionLoadState.Partial : UeiAcquisitionLoadState.Ready);
    }

    private static void LogRegistryFailure(Exception ex)
    {
        _registryHealthy = false;
        SetState(UeiAcquisitionLoadState.Partial);
        if (_loggedRegistryFailure) return;
        _loggedRegistryFailure = true;
        UeiPlugin.LogWarning("UEI acquisition is waiting for stable item/loot registries: " + ex.Message);
    }

    private static void Mix(ref ulong hash, int value) { unchecked { hash = (hash ^ (uint)value) * 1099511628211UL; } }
    private static void Mix(ref ulong hash, string? value)
    {
        Mix(ref hash, value?.Length ?? -1);
        if (value != null) foreach (char character in value) Mix(ref hash, character);
    }
    private static void SetState(UeiAcquisitionLoadState state)
    {
        if (State == state) return;
        State = state;
        Revision++;
    }
    private static void RecordLoadFailure(string path, string reason)
    {
        _hadLoadFailure = true;
        if (_loggedLoadFailure) return;
        _loggedLoadFailure = true;
        UeiPlugin.LogWarning("UEI acquisition could not read resource " + path + ": " + reason + " (further failures suppressed).");
    }
}
