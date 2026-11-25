using System.Globalization;
using System.Reflection;
using UnityEngine;

internal static class WorldSpawningTests
{
    internal static void Run(Action<bool, string> check)
    {
        Catalog(check);
        NativeSpawn(check);
        LiquidDelivery(check);
        ServerCommands(check);
        TraderInitialization(check);
        Console.WriteLine("PASS: production entity catalog, native spawn dispatch, temporary liquid bottles, caller routing and permissions (API doubles only)");
    }

    private static GameObject Entity(string path, string localeId, int? character = null)
    {
        GameObject prefab = new(path);
        prefab.Attach(new BuildingEntity { id = localeId });
        if (character.HasValue) prefab.Attach(new TraderScript { character = character.Value });
        return prefab;
    }

    private static void Capture(string path, GameObject prefab)
        => UeiEntities.Capture(path, prefab, prefab.GetComponent<BuildingEntity>());

    private static void Catalog(Action<bool, string> check)
    {
        UeiPlugin.LanguageMode.Value = "en";
        Locale.currentLangName = "en";
        Locale.Buildings["trader"] = "Merchant";
        Locale.Buildings["traderdsc"] = "A person to trade with.";
        GameObject first = Entity("trader1", "trader", 0);
        GameObject second = Entity("trader2", "trader", 1);
        Capture("trader1", first);
        Capture("trader2", second);
        Capture("trader1", first);
        GameObject sparse = Entity("sparse_icon_fixture", "fixture");
        sparse.Attach(new SpriteRenderer());
        GameObject visibleChild = new("visible_icon");
        Sprite visibleSprite = new();
        visibleChild.Attach(new SpriteRenderer { sprite = visibleSprite });
        sparse.Children.Add(visibleChild);
        Capture("sparse_icon_fixture", sparse);
        UeiEntities.Descriptor sparseDescriptor = UeiEntities.Entries.Single(x => x.ResourcePath == "sparse_icon_fixture");
        check(ReferenceEquals(sparseDescriptor.Icon, visibleSprite),
            "An entity must use an existing visible sprite when its first renderer has no sprite");
        check(UeiEntities.TryGetVisuals("sparse_icon_fixture", out var sparseVisual)
            && ReferenceEquals(sparseVisual.Parts[0].Sprite, visibleSprite),
            "Choosing an entity icon must reuse the already-captured visual data");
        GameObject itemRoot = Entity("item_duplicate", "box");
        itemRoot.Attach(new Item());
        Capture("item_duplicate", itemRoot);
        GameObject childOnly = new("child_only");
        childOnly.Children.Add(Entity("child", "box"));
        Capture("child_only", childOnly);
        GameObject indestructible = Entity("indestructible_fixture", "fixture");
        indestructible.GetComponent<BuildingEntity>()!.indestructible = true;
        Capture("indestructible_fixture", indestructible);
        UeiEntities.Publish(Time.unscaledTime, force: true);
        check(UeiEntities.Contains("trader1") && UeiEntities.Contains("trader2") && UeiEntities.Contains("indestructible_fixture"),
            "Entities must include roots without drops and indestructible roots, using each canonical resource path");
        check(!UeiEntities.Contains("item_duplicate") && !UeiEntities.Contains("child_only")
            && UeiEntities.Entries.Count(x => x.ResourcePath == "trader1") == 1,
            "Items, source-only children, and duplicate resource reads must not create extra entity entries");

        Item.GlobalItems = new(StringComparer.Ordinal) { ["trader1"] = new() { fullName = "Ordinary item", category = "tool" } };
        Liquids.Registry = new(StringComparer.Ordinal) { ["water"] = new() { localeName = "water" } };
        Recipes.recipes = [new() { result = new() { id = "trader1" }, items = [new() { specific = true, specificId = "trader1" }] }];
        UeiCatalog.ForceRebuild();
        check(UeiCatalog.EnsureBuilt(), "The actual production catalog must build with entity descriptors");
        check(UeiCatalog.CategoryKeys.Contains(UeiCatalog.CategoryEntity), "Entities must be a normal category page");
        UeiCatalog.TryGetEntry(UeiEntryKind.Entity, "trader1", out UeiEntry? entity);
        UeiCatalog.TryGetEntry(UeiEntryKind.Item, "trader1", out UeiEntry? item);
        check(entity != null && item != null && entity.EntryKey == "entity:trader1" && item.EntryKey == "item:trader1",
            "A shared item/entity ID must preserve distinct lookup and favorite identities");
        check(entity!.DisplayName == "Merchant 1" && entity.Description == "A person to trade with."
            && UeiCatalog.Entries.Any(x => x.DisplayName == "Merchant 2"), "Merchant variants must remain separate and use live building localization");
        check(UeiCatalog.GetProducedBy(entity).Count == 0 && UeiCatalog.GetUsedIn(entity).Count == 0
            && UeiCatalog.GetProducedBy(item!).Count == 1 && UeiCatalog.GetUsedIn(item!).Count == 1,
            "Entity IDs must never become item recipe results or ingredients");
        UeiCatalogView view = new();
        IReadOnlyList<UeiEntry> Query(string category, string search = "")
            => view.GetEntries(UeiCatalog.Entries, UeiCatalog.Revision, UeiFavorites.Revision, category, search);
        check(Query("All").Any(x => x.EntryKey == entity.EntryKey)
            && Query("Entity").All(x => x.Kind == UeiEntryKind.Entity)
            && Query("Entity", "trader2").Single().Id == "trader2", "All and Entities must share the searchable production entries");
        UeiFavorites.Toggle(entity.EntryKey);
        check(Query("Favorites").Any(x => ReferenceEquals(x, entity)), "Entity favorites must use the existing Favorites page");
        UeiFavorites.ClearAll();

        List<UeiRecipeLink> originalRecipes = UeiCatalog.GetProducedBy(item!);
        Capture("late_entity", Entity("late_entity", "fixture"));
        UeiEntities.Publish(Time.unscaledTime, force: true);
        UeiCatalog.EnsureBuilt();
        check(Query("Entity", "late_entity").Count == 1 && ReferenceEquals(originalRecipes, UeiCatalog.GetProducedBy(item!)),
            "Late entity discovery must refresh the grid without rebuilding existing item recipe links");
        Locale.currentLangName = "zh-CN";
        UeiPlugin.LanguageMode.Value = "zh";
        Locale.Buildings["trader"] = "商人";
        Locale.Buildings["traderdsc"] = "可以交易。";
        UeiCatalog.EnsureBuilt();
        check(Query("Entity", "商人").Any(x => x.Id == "trader1" && x.DisplayName == "商人 1")
            && UeiCatalog.CategoryLabel("Entity") == "实体", "Locale changes must refresh entity names, category labels and search text");
        UeiPlugin.LanguageMode.Value = "en";
        Locale.currentLangName = "en";
    }

    private static void NativeSpawn(Action<bool, string> check)
    {
        string[]? invoked = null;
        ConsoleScript.Commands = [new("spawn", "native", args => invoked = args, null)];
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new("de-DE");
            check(UeiWorldSpawning.SpawnEntity("trader1", new(12.5f, -3.25f)), "A known entity must execute the registered native spawn action");
            check(invoked!.SequenceEqual(new[] { "spawn", "trader1", "12.5,-3.25", "1", "1" }),
                "Native spawn arguments must identify the resource, explicit position and one entity, independent of decimal culture");
            invoked = null;
            check(!UeiWorldSpawning.SpawnEntity("unknown", new()) && invoked == null, "An unindexed object must not reach native spawn");
        }
        finally { CultureInfo.CurrentCulture = originalCulture; }
        foreach (string unsafeId in new[] { "", " ", "trader1\nheal", "trader1;heal", "a\tb", "a\"b", "a\u0001b" })
            check(!UeiWorldSpawning.IsSafeToken(unsafeId), "Spawn/liquid tokens must reject command separators and controls");
        foreach (TogetherNetworkState state in Enum.GetValues<TogetherNetworkState>())
        {
            UeiGiveRoute expected = state is TogetherNetworkState.Client or TogetherNetworkState.Server ? UeiGiveRoute.ServerCommand
                : state == TogetherNetworkState.Unknown ? UeiGiveRoute.Unavailable : UeiGiveRoute.LocalFactory;
            check(UeiCheatPolicy.WorldRoute(state) == expected, "Entity/liquid clients must use the server and unknown networking must reject the action");
        }
    }

    private static Body Player(string name)
    {
        GameObject player = new(name);
        Body body = new();
        player.Attach(body);
        player.transform.position = new(7, 8, 0);
        return body;
    }

    private static GameObject Bottle(float capacity = 1000)
    {
        GameObject bottle = new("craftingbottle");
        bottle.Attach(new Item());
        bottle.Attach(new WaterContainerItem { Capacity = capacity, stack = [new("old_liquid", 100)] });
        return bottle;
    }

    private static void LiquidDelivery(Action<bool, string> check)
    {
        Body player = Player("recipient");
        Liquids.Registry["water"] = new();
        GameObject? latest = null;
        int factories = 0;
        Utils.CreateHandler = (id, position, rotation) =>
        {
            check(id == "craftingbottle" && position.x == 7 && position.y == 8, "Liquid delivery must create the native bottle at the recipient");
            factories++;
            return latest = Bottle();
        };
        UnityEngine.Object.DestroyCalls.Clear();
        check(UeiWorldSpawning.GiveLiquid(player, "water") == UeiWorldSpawnResult.Completed
            && player.Inventory.Single() == latest!.GetComponent<Item>(), "The filled bottle must be delivered to the requested body");
        check(latest!.GetComponent<WaterContainerItem>()!.stack is [{ liquidId: "water", amount: 1000 }]
            && UnityEngine.Object.DestroyCalls.Contains((latest, 300)), "A bottle must contain only the selected liquid, fill to capacity and expire after five minutes");
        player.Pickup = _ => { };
        check(UeiWorldSpawning.GiveLiquid(player, "water") == UeiWorldSpawnResult.Nearby
            && latest!.GetComponent<WaterContainerItem>()!.stack.Single().amount == 1000,
            "Rejected inventory pickup must leave the full bottle nearby, without claiming inventory delivery");
        player.Pickup = _ => throw new InvalidOperationException("inventory rejected pickup");
        check(UeiWorldSpawning.GiveLiquid(player, "water") == UeiWorldSpawnResult.Nearby, "A pickup exception must retain the already-filled bottle");
        int before = factories;
        check(UeiWorldSpawning.GiveLiquid(player, "missing") == UeiWorldSpawnResult.Failed && factories == before,
            "Invalid liquid IDs must fail before creating an object");
        foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
        {
            Utils.CreateHandler = (_, _, _) => latest = Bottle(invalid);
            check(UeiWorldSpawning.GiveLiquid(player, "water") == UeiWorldSpawnResult.Failed
                && UnityEngine.Object.DestroyCalls.Contains((latest!, 0)), "Unusable container capacities must clean up the failed bottle");
        }
        Utils.CreateHandler = (_, _, _) => new GameObject("missing_components");
        check(UeiWorldSpawning.GiveLiquid(player, "water") == UeiWorldSpawnResult.Failed, "A malformed native bottle resource must fail cleanly");
        Utils.CreateHandler = (_, _, _) => null;
        check(UeiWorldSpawning.GiveLiquid(player, "water") == UeiWorldSpawnResult.Failed, "A missing factory result must not report success");
    }

    private static void ServerCommands(Action<bool, string> check)
    {
        UeiWorldSpawning.EnsureCommands(immediate: true);
        UeiWorldSpawning.EnsureCommands(immediate: true);
        check(ConsoleScript.Commands.Count(x => x.name == UeiWorldSpawning.LiquidCommand) == 1, "Liquid command registration must be idempotent");
        Command registered = ConsoleScript.SearchExact(UeiWorldSpawning.LiquidCommand)!;
        ConsoleScript.Commands = [ConsoleScript.SearchExact("spawn")!];
        UeiWorldSpawning.EnsureCommands(immediate: true);
        check(ReferenceEquals(registered, ConsoleScript.SearchExact(UeiWorldSpawning.LiquidCommand)), "A recreated game command list must regain the liquid handler");
        Body host = Player("host"), recipient = Player("remote_caller");
        PlayerCamera.main.body = host;
        Type callerType = UeiOptionalTypes.Find("Together.ScavPlayer")!;
        object caller = Activator.CreateInstance(callerType)!;
        callerType.GetField("body")!.SetValue(caller, recipient);
        Type con = UeiOptionalTypes.Find("Together.Con")!;
        con.GetField("Caller")!.SetValue(null, caller);
        Utils.CreateHandler = (_, _, _) => Bottle();
        UeiPanel.NetworkState = TogetherNetworkState.Server;
        UeiPanel.CheatAllowed = true;
        registered.action([UeiWorldSpawning.LiquidCommand, "water"]);
        check(recipient.Inventory.Count == 1 && host.Inventory.Count == 0, "Authenticated remote liquid requests must deliver to the command caller, never the host camera");
        int count = recipient.Inventory.Count;
        bool Throws(Action action)
        {
            try { action(); return false; } catch (InvalidOperationException) { return true; } catch (ArgumentException) { return true; }
        }
        UeiPanel.CheatAllowed = false;
        check(Throws(() => registered.action([UeiWorldSpawning.LiquidCommand, "water"])) && recipient.Inventory.Count == count,
            "The server command must recheck cheat permissions before changing inventory");
        UeiPanel.CheatAllowed = true;
        foreach (TogetherNetworkState state in new[] { TogetherNetworkState.Client, TogetherNetworkState.Unknown })
        {
            UeiPanel.NetworkState = state;
            check(Throws(() => registered.action([UeiWorldSpawning.LiquidCommand, "water"])) && recipient.Inventory.Count == count,
                "A client/unknown state must never execute the server liquid handler locally");
        }
        UeiPanel.NetworkState = TogetherNetworkState.Server;
        check(Throws(() => registered.action([UeiWorldSpawning.LiquidCommand, "water", "extra"]))
            && Throws(() => registered.action([UeiWorldSpawning.LiquidCommand, "missing"])), "Malformed server requests must reject extra arguments and unknown liquids");
        con.GetField("Caller")!.SetValue(null, null);
        check(Throws(() => registered.action([UeiWorldSpawning.LiquidCommand, "water"])) && host.Inventory.Count == 0,
            "An unresolved remote caller must not fall back to the host");
        UeiPanel.NetworkState = TogetherNetworkState.Offline;
    }

    private static void TraderInitialization(Action<bool, string> check)
    {
        GameObject instance = Entity("trader", "trader", 0);
        TraderScript trader = instance.GetComponent<TraderScript>()!;
        instance.transform.position = new(40, -10, 0);
        UeiPanel.NetworkState = TogetherNetworkState.Offline;
        UeiWorldSpawning.InitializeTrader(trader);
        check(trader.MoveRange.min == 35 && trader.MoveRange.max == 45 && trader.farEnoughToMove,
            "A manually spawned trader must receive a movement range around its actual position");
        trader.MoveRange = new(-10, 10);
        UeiWorldSpawning.InitializeTrader(trader);
        check(trader.MoveRange.min == -10 && trader.MoveRange.max == 10, "World-generation and mod-supplied trader ranges must remain intact");
        trader.MoveRange = new();
        UeiPanel.NetworkState = TogetherNetworkState.Client;
        UeiWorldSpawning.InitializeTrader(trader);
        check(trader.MoveRange.min == 0 && trader.MoveRange.max == 0, "Clients must not initialize authoritative trader state");
        UeiPanel.NetworkState = TogetherNetworkState.Offline;
    }
}
