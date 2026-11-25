using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

internal static class Program
{
    private static int _checks;

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        _checks++;
    }

    public static int Main(string[] args)
    {
        try
        {
            MissingMods();
            LateLoadedMods();
            PermissionsAndRoutes();
            AcquisitionTests.Run(Check);
            NativeAcquisitionTests.Run(Check);
            AcquisitionSupplementTests.Run(Check);
            ResourceIndexTests.Run(Check, args.Length > 1 ? args[1] : null);
            NativeAcquisitionDriverTests.Run(Check);
            InputTests.Run(Check);
            PerformanceTests.Run(Check);
            WorldSpawningTests.Run(Check);
            EntityPlacementTests.Run(Check);
            EntityGhostTests.Run(Check);
            SourceGuards(args.Length > 0 ? args[0] : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../..")));
            Console.WriteLine($"PASS: {_checks} assertions (production interop/i18n/policy/acquisition/input/catalog/interaction + source guards; Unity/network runtime not simulated).");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void MissingMods()
    {
        // No fixture assembly has been loaded yet. Repeated catalog misses must
        // reuse a negative type-cache entry and the same assembly snapshot.
        Check(UeiOptionalTypes.Find("CUCoreLib.Registries.ItemRegistry") == null, "Fixture loaded too early");
        Check(!CuCoreLibHelper.IsCustomItem("vanilla"), "Missing CUCore should be optional");
        Check(!SexModHelper.IsRegistered("vanilla"), "Missing SexMod should be optional");
        Check(!RshLibHelper.IsCustomItem("vanilla"), "Missing RshLib should be optional");
        Check(CuCoreLibHelper.GetItemText("missing") == "", "Absent public item-info fallback");
        Check(CuCoreLibHelper.CreateInstance("cu", new Vector3(), 0f) == null, "Missing CUCore factory must be optional");
        Check(!CuCoreLibHelper.TryGetRunningNetworkState(out _), "Missing CUCore must allow native state fallback");
        Check(UeiI18n.ItemText("missing", "fallback") == "fallback", "Translation fallback without mods");
        UeiOptionalTypes.Find("NoSuchMod.Registry");
        object snapshot = typeof(UeiOptionalTypes).GetField("_assemblies", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        for (int i = 0; i < 1000; i++)
        {
            Check(UeiOptionalTypes.Find("NoSuchMod.Registry") == null, "Absent type unexpectedly resolved");
            CuCoreLibHelper.GetCustomItemSprite("vanilla");
            SexModHelper.GetCustomItemSprite("vanilla");
            RshLibHelper.IsCustomItem("vanilla");
        }
        Check(ReferenceEquals(snapshot, typeof(UeiOptionalTypes).GetField("_assemblies", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)), "Repeated misses refreshed assembly snapshot");
        var cache = (IDictionary)typeof(UeiOptionalTypes).GetField("Types", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        Check(cache.Contains("NoSuchMod.Registry") && cache["NoSuchMod.Registry"] == null, "Miss was not cached");
        Console.WriteLine("PASS: absent optional mods and negative discovery cache");
    }

    private static void LateLoadedMods()
    {
        int generation = UeiOptionalTypes.Generation;
        Assembly fixture = Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "UEI.OptionalMods.dll"));
        Check(UeiOptionalTypes.Generation > generation, "Assembly load did not invalidate missing-mod cache");
        Check(UeiOptionalTypes.Find("CUCoreLib.Registries.ItemRegistry") != null, "Late CUCore was not discovered");
        Check(CuCoreLibHelper.IsCustomItem("cu"), "Exact CUCore string/out CustomItemInfo binding");
        Check(!CuCoreLibHelper.IsCustomItem("vanilla"), "Vanilla misidentified as CUCore item");
        Check(CuCoreLibHelper.GetCustomItemSprite("cu") != null, "CUCore icon binding");
        Check(CuCoreLibHelper.TryGetItemInfo("cu", out ItemInfo? cuInfo) && cuInfo?.fullName == "模组物品", "Public TryGetItemInfo binding");
        GameObject? cuInstance = CuCoreLibHelper.CreateInstance("cu", new Vector3(2f, 3f, 0f), 15f);
        Check(cuInstance?.name == "cu" && cuInstance.WorldPosition.x == 2f && cuInstance.WorldPosition.y == 3f
            && cuInstance.Rotation == 15f && cuInstance.Condition == 1f, "CUCore public factory signature/nullable condition");
        Check(CuCoreLibHelper.CreateInstance("vanilla", new Vector3(), 0f) == null, "CUCore factory used for an unregistered item");
        Check(UeiOptionalTypes.StaticMethod(fixture.GetType("UnrelatedMod.InternalApi"), "HiddenType", typeof(bool)) == null, "Internal API type was accepted");
        Check(UeiOptionalTypes.StaticMethod(fixture.GetType("UnrelatedMod.PublicApi"), "HiddenMethod", typeof(bool)) == null, "Nonpublic API method was accepted");

        MethodInfo configure = fixture.GetType("CUCoreLib.Networking.MultiplayerApi")!.GetMethod("Configure")!;
        configure.Invoke(null, new object[] { false, true, true, false });
        Check(!CuCoreLibHelper.TryGetRunningNetworkState(out _), "Unavailable public bridge must use native fallback");
        configure.Invoke(null, new object[] { true, false, false, true });
        Check(!CuCoreLibHelper.TryGetRunningNetworkState(out _), "Inactive public bridge must not override active native state");
        configure.Invoke(null, new object[] { true, true, true, false });
        Check(CuCoreLibHelper.TryGetRunningNetworkState(out var role) && role == TogetherNetworkState.Client, "Public API client role");
        configure.Invoke(null, new object[] { true, true, false, true });
        Check(CuCoreLibHelper.TryGetRunningNetworkState(out role) && role == TogetherNetworkState.Server, "Public API server role");
        configure.Invoke(null, new object[] { true, true, false, false });
        Check(CuCoreLibHelper.TryGetRunningNetworkState(out role) && role == TogetherNetworkState.Unknown, "Missing role must fail closed");
        configure.Invoke(null, new object[] { true, true, true, true });
        Check(CuCoreLibHelper.TryGetRunningNetworkState(out role) && role == TogetherNetworkState.Unknown, "Conflicting roles must fail closed");

        Check(!RshLibHelper.IsCustomItem("rsh"), "Rsh registry should initially be null");
        Type rsh = fixture.GetType("RshLib.Plugin")!;
        rsh.GetMethod("Initialize")!.Invoke(null, null);
        Check(RshLibHelper.IsCustomItem("rsh$instance"), "Rsh late registry initialization or $ suffix lost");
        Check(RshLibHelper.GetCustomItemSprite("rsh$instance") != null, "Rsh icon support lost");
        rsh.GetMethod("Replace")!.Invoke(null, null);
        Check(!RshLibHelper.IsCustomItem("rsh") && RshLibHelper.IsCustomItem("replacement"), "Stale Rsh registry retained across replacement");
        var entries = new Dictionary<string, UeiEntry> { ["item:replacement"] = new() };
        Check(RshLibHelper.TryResolveKey("replacement$instance", UeiEntryKind.Item, entries) == "item:replacement", "Rsh recipe key fallback lost");

        Check(SexModHelper.IsRegistered("sex"), "Current GitLab SexMod registry not bound");
        Check(SexModHelper.GetCustomItemSprite("sex") != null, "SexMod icon not bound");
        GameObject? created = SexModHelper.CreateInstance("sex", new Vector2(1.25f, 2.5f), 3.5f);
        Check(created?.name == "sex" && created.Position.x == 1.25f && created.Position.y == 2.5f && created.Rotation == 3.5f, "Wrong SexMod factory overload or arguments");
        Check(SexModHelper.CreateInstance("vanilla", new Vector2(), 0) == null, "Factory invoked for an unregistered item");
        Check(!SexModHelper.IsRegistered("late_sex"), "Late registry initial state");
        fixture.GetType("ScavPrototypeSexMod.Registers.ItemRegistry")!.GetMethod("RegisterLate")!.Invoke(null, null);
        Check(SexModHelper.IsRegistered("late_sex"), "Per-item registration result was incorrectly cached");

        Check(UeiI18n.ItemText("cu", "fallback") == "模组物品", "CUCore item translation");
        Check(UeiI18n.ItemText("cudsc", "fallback") == "模组描述", "CUCore public item description fallback");
        // CUCore's overlay populates the public game locale dictionaries. UEI
        // reads those dictionaries through Locale APIs, without its internal loader.
        Locale.Other["categorycustom"] = "自定义类别";
        Locale.Other["cu_liquid"] = "模组液体";
        Locale.Other["other_liquid"] = "Other liquid";
        Check(UeiI18n.OtherText("categorycustom", "fallback") == "自定义类别", "CUCore other translation");
        Check(UeiI18n.LiquidText("cu_liquid", "fallback") == "模组液体", "CUCore liquid translation");
        Check(UeiI18n.LiquidText("other_liquid", "fallback") == "Other liquid", "Liquid-to-other fallback");
        Check(UeiI18n.ItemText("unknown_item", "fallback") == "fallback", "Item miss returned raw key");
        Check(UeiI18n.LiquidText("unknown_liquid", "fallback") == "fallback", "Liquid miss returned raw key");
        Locale.Items["cu"] = "Native overlay";
        Check(UeiI18n.ItemText("cu", "fallback") == "Native overlay", "Native translation precedence changed");
        Locale.Other["existing_liquid"] = "Existing translation";
        Check(UeiI18n.LiquidText("existing_liquid", "fallback") == "Existing translation", "Resolved liquid translation overwritten");
        Check((int)fixture.GetType("CUCoreLib.Registries.LocaleRegistry")!.GetField("Reads")!.GetValue(null)! == 0, "A translation lookup touched mutating LocaleRegistry.Get");
        Check((int)fixture.GetType("CUCoreLib.Helpers.LocaleLoader")!.GetField("Reads")!.GetValue(null)! == 0, "A translation lookup used internal LocaleLoader");
        Check((int)fixture.GetType("CUCoreLib.Registries.ItemRegistry")!.GetField("MissingInfoRequests")!.GetValue(null)! == 0, "Locale misses were passed to the item-info API");
        Check(UeiI18n.T("status.cheatHostRequired").Contains("host"), "English host-required status missing");
        UeiPlugin.LanguageMode.Value = "zh";
        Check(UeiI18n.T("status.cheatHostRequired").Contains("房主"), "Chinese host-required status missing");
        Console.WriteLine("PASS: late-loaded CUCore/SexMod/Rsh APIs, live registration, read-only translations");
    }

    private static void PermissionsAndRoutes()
    {
        var expected = new Dictionary<TogetherNetworkState, UeiGiveRoute[]>
        {
            // Index: neither registered/resource, resource only, custom only, custom+resource.
            [TogetherNetworkState.NotInstalled] = [UeiGiveRoute.Unavailable, UeiGiveRoute.LocalFactory, UeiGiveRoute.LocalFactory, UeiGiveRoute.LocalFactory],
            [TogetherNetworkState.Offline] = [UeiGiveRoute.Unavailable, UeiGiveRoute.LocalFactory, UeiGiveRoute.LocalFactory, UeiGiveRoute.LocalFactory],
            [TogetherNetworkState.Client] = [UeiGiveRoute.Unavailable, UeiGiveRoute.ServerCommand, UeiGiveRoute.HostRequired, UeiGiveRoute.HostRequired],
            [TogetherNetworkState.Server] = [UeiGiveRoute.Unavailable, UeiGiveRoute.ServerCommand, UeiGiveRoute.LocalFactory, UeiGiveRoute.LocalFactory],
            [TogetherNetworkState.Unknown] = [UeiGiveRoute.Unavailable, UeiGiveRoute.Unavailable, UeiGiveRoute.Unavailable, UeiGiveRoute.Unavailable],
        };
        foreach (var pair in expected)
            for (int i = 0; i < 4; i++)
                Check(UeiCheatPolicy.GiveRoute(pair.Key, (i & 2) != 0, (i & 1) != 0) == pair.Value[i], $"Wrong give route: {pair.Key}/{i}");

        bool?[] values = [null, false, true];
        foreach (bool? admin in values)
        foreach (bool? cheats in values)
        {
            Check(UeiCheatPolicy.HasTogetherAccess(TogetherNetworkState.Client, admin, cheats) == (admin == true), "Client grant must follow server-admin permission, not replicated cheats");
            Check(UeiCheatPolicy.HasTogetherAccess(TogetherNetworkState.Server, admin, cheats) == (cheats == true), "Host must enable sv_cheats");
            Check(!UeiCheatPolicy.HasTogetherAccess(TogetherNetworkState.Unknown, admin, cheats), "Unknown network state must deny access");
        }
        Check(UeiCheatPolicy.HasTogetherAccess(TogetherNetworkState.NotInstalled, null, null), "Absent Together blocks normal offline policy");
        Check(UeiCheatPolicy.HasTogetherAccess(TogetherNetworkState.Offline, false, false), "Installed but disconnected Together blocks offline policy");
        Check(UeiCheatPolicy.GiveRoute((TogetherNetworkState)999, true, true) == UeiGiveRoute.Unavailable, "Unrecognized future state must not create a local item");
        Console.WriteLine("PASS: host/client/offline/unknown permission and item routing matrix");
    }

    private static void SourceGuards(string root)
    {
        string runtime = File.ReadAllText(Path.Combine(root, "UeiRuntime.cs"));
        Check(runtime.Contains("[HarmonyPatch(typeof(GlobalDark), \"Update\")]"), "GlobalDark recovery hook removed");
        Check(runtime.Contains("UeiRuntime.TickStandalone(\"GlobalDark.Update\")"), "GlobalDark no longer drives recovery");
        Check(runtime.Contains("UeiPlugin.EnsureRuntime(source)"), "Recovery does not recreate runtime");
        Check(runtime.IndexOf("bool shouldShow = ShouldShow(camera, out string hideReason);", StringComparison.Ordinal) < runtime.IndexOf("if (_lastShownFrame == Time.frameCount)", StringComparison.Ordinal), "Visibility must be reevaluated before deduplication");
        Check(runtime.IndexOf("UeiNativeAcquisition.Tick();", StringComparison.Ordinal) < runtime.IndexOf("bool shouldShow = ShouldShow(camera, out string hideReason);", StringComparison.Ordinal), "Native discovery must continue while inventory is closed");
        string native = File.ReadAllText(Path.Combine(root, "UeiNativeAcquisition.cs"));
        Check(!native.Contains("Resources.LoadAll") && !native.Contains("Instantiate("), "Native acquisition must not bulk-load or instantiate gameplay objects");
        string catalog = File.ReadAllText(Path.Combine(root, "UeiCatalog.cs"));
        int refresh = catalog.IndexOf("private static void RefreshAcquisition", StringComparison.Ordinal);
        string sourceRefresh = catalog[refresh..catalog.IndexOf("private static void BuildCategories", refresh, StringComparison.Ordinal)];
        Check(!sourceRefresh.Contains("BuildRecipeLinks(") && !sourceRefresh.Contains("_revision++"), "Source completion must not rebuild recipes or invalidate the grid/search catalog");
        Check(sourceRefresh.Contains("out string supplement") && sourceRefresh.Contains("entry.AcquisitionSupplement = supplement;")
            && sourceRefresh.Contains("string.Equals(entry.AcquisitionSupplement, supplement, StringComparison.Ordinal)"),
            "Source revisions must cache and compare the supplement returned by acquisition composition");
        Check(!sourceRefresh.Contains("AcquisitionAnnotation ="), "Supplement filtering must not overwrite the raw acquisition annotation");
        string interop = File.ReadAllText(Path.Combine(root, "UeiModInterop.cs"));
        Check(!interop.Contains(".GetTypes("), "Optional mod discovery enumerates all types");
        Check(!interop.Contains("\"CUCoreLib.Registries.LocaleRegistry\""), "Mutating locale registry is bound");
        Check(!interop.Contains("\"CUCoreLib.Helpers.LocaleLoader\""), "Internal locale loader is bound");
        string panel = File.ReadAllText(Path.Combine(root, "UeiPanel.cs"));
        int acquisitionRefresh = panel.IndexOf("if (_acquisitionRevision != UeiCatalog.AcquisitionRevision)", StringComparison.Ordinal);
        string acquisitionBlock = panel[acquisitionRefresh..panel.IndexOf("if (_favoritesRevision", acquisitionRefresh, StringComparison.Ordinal)];
        Check(acquisitionBlock.Contains("_detailDirty = true;") && !acquisitionBlock.Contains("_dirty = true;"), "New acquisition text must refresh details without rebuilding the grid");
        Check(panel.Contains("bool hasSupplement = !string.IsNullOrWhiteSpace(_selectedEntry.AcquisitionSupplement);")
            && panel.Contains("_selectedEntry.AcquisitionSupplement, AcquisitionNotesTextHeight);")
            && !panel.Contains("_selectedEntry.AcquisitionAnnotation"),
            "Supplement visibility and content must use the same filtered cache rather than the raw annotation");
        Check(panel.Contains("if (_sourceSection == SourceSection.Supplement && !hasSupplement) _sourceSection = SourceSection.Acquisition;"),
            "A supplement removed by a late source update must return the selection to acquisition");
        int click = panel.IndexOf("CheatActionResult result = TryCheatGiveItem(entry);", StringComparison.Ordinal);
        string cheatClick = panel[click..panel.IndexOf("private void SelectEntry", click, StringComparison.Ordinal)];
        Check(cheatClick.Contains("_selectedEntry = null;") && !cheatClick.Contains("SelectEntry("), "Cheat give opens recipe detail");
        Check(panel.Contains("inst = SexModHelper.CreateInstance(entry.Id, pos, 0f);"), "SexMod factory fallback lost");
        Check(panel.Contains("? CuCoreLibHelper.CreateInstance(entry.Id, pos, 0f)"), "CUCore public factory is not the preferred local path");
        Check(panel.Contains("if (!IsAlive) return;") && runtime.Contains("_panel.IsAlive && _attachedCanvas == canvas"), "Destroyed UI must be skipped and recreated");
        string plugin = File.ReadAllText(Path.Combine(root, "Plugin.cs"));
        string version = Regex.Match(File.ReadAllText(Path.Combine(root, "UEI.csproj")), "<Version>([^<]+)</Version>").Groups[1].Value;
        string baseVersion = Regex.Match(plugin, "PluginVersionBase = \"([^\"]+)\"").Groups[1].Value;
        string suffix = Regex.Match(plugin, "PluginVersion = PluginVersionBase \\+ \"([^\"]+)\"").Groups[1].Value;
        Check(Regex.IsMatch(version, @"^\d+\.\d+\.\d+$") && version == baseVersion + suffix, "Plugin/project version must be matching x.x.x");
        Console.WriteLine("PASS: source guards for recovery, click behavior, and version consistency");
    }
}
