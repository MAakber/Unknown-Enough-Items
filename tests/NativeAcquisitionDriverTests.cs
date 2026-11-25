using System.Diagnostics;
using System.Reflection;
using UnityEngine;

// Execute the production coordinator with controllable API-boundary doubles.
// This checks scheduling/cache/state behavior, not Unity resource loading.
internal static class NativeAcquisitionDriverTests
{
    internal static void Run(Action<bool, string> check)
    {
        string directory = Path.Combine(Path.GetTempPath(), "UEI.Acquisition.Driver.Tests-" + Guid.NewGuid().ToString("N"));
        string[] keys = Enumerable.Range(0, 12).Select(i => "prefab" + i.ToString("D2")).ToArray();
        ResourceIndexTests.WriteDriverFixture(directory, keys);
        string originalPath = Application.dataPath;
        Func<string, ResourceRequest> originalHandler = Resources.LoadHandler;
        var prefabs = keys.ToDictionary(key => key, key => new GameObject(key));
        BuildingEntity root = new()
        {
            id = "medcrate", guaranteedDropAmount = 1, itemCategoriesToAdd = ["water"],
            itemsDropOnDestroy = [new() { id = "bandage", chance = .5f, conditionMin = .5f }],
        };
        prefabs[keys[0]].Attach(root);
        Sprite rootIcon = new();
        prefabs[keys[0]].Attach(new SpriteRenderer { sprite = rootIcon });
        GameObject child = new("child");
        child.Attach(new BuildingEntity { id = "childcrate", alwaysDrop = [new() { id = "bandage", chance = 0 }] });
        Sprite childIcon = new();
        child.Attach(new SpriteRenderer { sprite = childIcon });
        prefabs[keys[1]].Children.Add(child);
        prefabs[keys[2]].Attach(new BuildingEntity { id = "trader" });
        prefabs[keys[2]].Attach(new TraderScript { character = 0 });
        prefabs[keys[3]].Attach(new BuildingEntity { id = "indestructible", indestructible = true });
        prefabs[keys[4]].Attach(new BuildingEntity { id = "item_root" });
        prefabs[keys[4]].Attach(new Item());
        var calls = new List<string>();
        var requests = new List<ResourceRequest>();
        int thread = Environment.CurrentManagedThreadId;
        bool wrongThread = false;
        Resources.LoadHandler = path =>
        {
            wrongThread |= Environment.CurrentManagedThreadId != thread;
            calls.Add(path);
            var request = new ResourceRequest { asset = prefabs[path] };
            requests.Add(request);
            return request;
        };
        Item.GlobalItems = new(StringComparer.Ordinal)
        {
            ["bandage"] = new() { fullName = "Dressing", category = "medical", value = 10 },
            ["lemonade"] = new LiquidItemInfo { fullName = "Lemonade jar", category = "water", value = 2, capacity = 750,
                defaultContents = [new("lemonade", 750)] },
        };
        ItemLootPool.pool = null;
        Liquids.Registry = new(StringComparer.Ordinal) { ["lemonade"] = new() };
        Application.dataPath = directory;
        Time.frameCount = 50000;
        Time.unscaledTime = 0;
        try
        {
            bool boundedRequests = true;
            bool deduplicatedFrames = true;
            void Step(float delta = .1f)
            {
                Time.frameCount++;
                Time.unscaledTime += delta;
                int before = calls.Count;
                UeiNativeAcquisition.Tick();
                boundedRequests &= calls.Count - before <= 1;
                int after = calls.Count;
                UeiNativeAcquisition.Tick();
                UeiNativeAcquisition.Tick();
                deduplicatedFrames &= calls.Count == after;
            }

            Stopwatch wait = Stopwatch.StartNew();
            while (calls.Count == 0 && wait.Elapsed < TimeSpan.FromSeconds(5)) { Step(); Thread.Sleep(1); }
            check(calls.Count == 1, "Metadata IO should eventually queue the first resource without a player camera or open inventory");
            for (int i = 0; i < 5; i++) Step();
            check(calls.Count == 1 && requests[0].AssetReads == 0, "Pending requests must not be read synchronously or advance the queue");
            for (int i = 0; i < keys.Length + 2; i++)
            {
                requests[^1].isDone = true;
                Step();
            }
            check(calls.SequenceEqual(keys) && !wrongThread, "Canonical paths must load once, in order, through the main-thread API boundary");
            check(boundedRequests && deduplicatedFrames, "One frame may start at most one request, even with multiple recovery hooks");
            check(UeiEntities.Contains(keys[0]) && UeiEntities.Contains(keys[2]) && UeiEntities.Contains(keys[3])
                && !UeiEntities.Contains(keys[1]) && !UeiEntities.Contains(keys[4]),
                "The shared scan must discover independent root entities without drop filters or duplicate item/child entries");
            check(UeiNativeAcquisition.State == UeiAcquisitionLoadState.Loading, "Finishing prefab discovery must still wait for the actual loot pool");
            check(UeiNativeAcquisition.Index.Get(UeiEntryKind.Item, "bandage").Count(x => x.Building != null) == 2,
                "The coordinator must copy direct and nested source fields, including alwaysDrop with chance zero");
            ItemLootPool.pool = new(StringComparer.Ordinal) { ["water"] = ["lemonade"], ["medical"] = ["bandage"] };
            Step(1.1f);
            check(UeiNativeAcquisition.State == UeiAcquisitionLoadState.Ready
                && UeiNativeAcquisition.Index.Get(UeiEntryKind.Item, "lemonade").Any(x => x.Kind == UeiAcquisitionRouteKind.RandomPool),
                "Late loot-pool initialization must remove the loading marker and build the actual sources");
            int registryRevision = UeiNativeAcquisition.RegistryRevision;
            int sourceRevision = UeiNativeAcquisition.Revision;
            ItemLootPool.pool["water"] = ["bandage"];
            Step(1.1f);
            check(UeiNativeAcquisition.Revision > sourceRevision && UeiNativeAcquisition.RegistryRevision == registryRevision
                && !UeiNativeAcquisition.Index.Get(UeiEntryKind.Item, "lemonade").Any(x => x.Kind == UeiAcquisitionRouteKind.RandomPool),
                "A same-count pool edit must refresh sources without invalidating the item/recipe catalog");
            Item.GlobalItems["bandage"].untradeable = true;
            Step(1.1f);
            check(!UeiNativeAcquisition.Index.Get(UeiEntryKind.Item, "bandage").Any(x => x.Kind == UeiAcquisitionRouteKind.Trade)
                && UeiNativeAcquisition.RegistryRevision == registryRevision, "A trade-filter change should refresh sources without replacing entry metadata");
            Item.GlobalItems["bandage"].category = "changed_category";
            Step(1.1f);
            check(UeiNativeAcquisition.RegistryRevision > registryRevision, "A same-count entry/category edit must invalidate catalog metadata");
            registryRevision = UeiNativeAcquisition.RegistryRevision;
            Item.GlobalItems = new(Item.GlobalItems, StringComparer.Ordinal);
            Step(1.1f);
            check(UeiNativeAcquisition.RegistryRevision > registryRevision && calls.Count == keys.Length,
                "A same-count registry replacement must rebuild entry metadata while reusing prefab descriptors");
            ((LiquidItemInfo)Item.GlobalItems["lemonade"]).defaultContents![0].amount = 500;
            Step(1.1f);
            check(UeiNativeAcquisition.Index.Get(UeiEntryKind.Liquid, "lemonade").Single().Amount == 500,
                "Same-count default liquid content edits must invalidate the filled-container source");
            UeiAcquisitionIndex validIndex = UeiNativeAcquisition.Index;
            ItemInfo validInfo = Item.GlobalItems["bandage"];
            Item.GlobalItems["bandage"] = null!;
            Step(1.1f);
            check(UeiNativeAcquisition.State == UeiAcquisitionLoadState.Partial && ReferenceEquals(validIndex, UeiNativeAcquisition.Index),
                "A transient registry failure must preserve the last complete index and flag incomplete coverage");
            Item.GlobalItems["bandage"] = validInfo;
            Step(1.1f);
            check(UeiNativeAcquisition.State == UeiAcquisitionLoadState.Ready && calls.Count == keys.Length,
                "Registry recovery must clear incomplete coverage while retaining the completed prefab cache");
            int reads = requests.Sum(x => x.AssetReads);
            check(ReferenceEquals(UeiNativeAcquisition.GetBuildingIcon(keys[0] + ":0"), rootIcon)
                && ReferenceEquals(UeiNativeAcquisition.GetBuildingIcon(keys[1] + ":0"), childIcon)
                && UeiNativeAcquisition.GetBuildingIcon("missing") == null,
                "Source icons must preserve each canonical/child sprite and return no invented icon for an unknown source");
            for (int i = 0; i < 10; i++) Step();
            check(calls.Count == keys.Length && requests.Sum(x => x.AssetReads) == reads,
                "Completed acquisition queries and registry polling must not reload or reread prefabs");

            // Allocation check covers this isolated .NET coordinator and test
            // boundary only, not Unity native work or actual frame performance.
            for (int i = 0; i < 100; i++) { Time.frameCount++; Time.unscaledTime += .01f; UeiNativeAcquisition.Tick(); }
            long beforeAllocation = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) { Time.frameCount++; Time.unscaledTime += .01f; UeiNativeAcquisition.Tick(); }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - beforeAllocation;
            check(allocated == 0, "The warmed isolated coordinator should not allocate or rescan while nothing changes");
            Console.WriteLine($"PASS: bounded source scheduling, late pool readiness, registry changes and cache reuse; 1,000 warm coordinator ticks allocated {allocated} bytes (API doubles only)");
        }
        finally
        {
            Application.dataPath = originalPath;
            Resources.LoadHandler = originalHandler;
            string resolved = Path.GetFullPath(directory);
            if (Path.GetDirectoryName(resolved) != Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)
                || !Path.GetFileName(resolved).StartsWith("UEI.Acquisition.Driver.Tests-", StringComparison.Ordinal))
                throw new InvalidOperationException("Refusing to clean up a path outside the driver-test directory.");
            Directory.Delete(resolved, recursive: true);
        }
    }
}
