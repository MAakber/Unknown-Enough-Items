internal static class NativeAcquisitionTests
{
    internal static void Run(Action<bool, string> check)
    {
        var items = new Dictionary<string, UeiAcquisitionItem>(StringComparer.Ordinal)
        {
            ["bandage"] = new("bandage", "Dressing", 10),
            ["lemonade"] = new("lemonade", "Lemonade jar", 2, contents: [new("lemonade", 225), new("lemonade", 525), new("unknown_liquid", 50)]),
            ["waterjar"] = new("waterjar", "Water jar", 3, contents: [new("water", 500)]),
            ["food"] = new("food", "Food", 1),
            ["drug"] = new("drug", "Drug", 2),
            ["bloodbaghuman"] = new("bloodbaghuman", "Blood bag", 3),
            ["untradeable"] = new("untradeable", "Untradeable", 10, true),
            ["free"] = new("free", "Free", 0),
            ["pistol"] = new("pistol", "Pistol", 0, true),
            ["unpooled"] = new("unpooled", "Not in an actual pool", 10),
            ["empty"] = new("empty", "Empty container", 1, contents: [new("lemonade", 0), new("water", -1)]),
        };
        var pools = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["water"] = ["lemonade", "waterjar"],
            ["medical"] = ["bandage", "untradeable"],
            ["drug"] = ["drug", "free"],
            ["food"] = ["food", "food", "bandage"],
        };
        string[] weighted = ["drug", "drug", "water", "medical", "medical", "medical", "medical", "medical"];
        UeiAcquisitionBuilding med = new("medcrate:0", "medcrate", "Medical crate", 1, weighted,
            drops: [new("bloodbaghuman", .05, .5, 1), new("bandage", 0), new("unpooled", double.NaN)], multiplier: .67);
        weighted[0] = "food";
        check(med.Categories[0] == "drug", "Source descriptors must copy category weights rather than retain mutable prefab arrays");
        UeiAcquisitionBuilding capsule = new("dropcapsule:0", "dropcapsule", "Drop capsule", 2, ["food", "water", "food"]);
        UeiAcquisitionBuilding variant = new("lifepod:1", "medcrate", "Medical crate", 2, ["water"], contextId: "lifepod", contextName: "Life pod");
        UeiAcquisitionBuilding always = new("guaranteed:0", "guaranteed", "Guaranteed", always: [new("unpooled", 0, .3, .9), new("unpooled", 0, .3, .9)]);
        UeiAcquisitionBuilding repeated = new("independent:0", "independent", "Independent", drops: [new("unpooled", .25), new("unpooled", .25)]);
        UeiAcquisitionBuilding inert = new("inert:0", "inert", "Indestructible", 1, ["water"], drops: [new("bandage", 1)], always: [new("food", 1)], indestructible: true);
        var buildings = new[] { med, capsule, variant, always, repeated, inert };
        UeiAcquisitionSnapshot Snapshot() => new(items, pools, new[] { "lemonade", "water" }, buildings);
        UeiAcquisitionIndex index = UeiAcquisitionIndex.Build(Snapshot());
        var medLemonade = index.Get(UeiEntryKind.Item, "lemonade").Single(x => ReferenceEquals(x.Building, med));
        var medBandage = index.Get(UeiEntryKind.Item, "bandage").Single(x => ReferenceEquals(x.Building, med));
        check(Close(medLemonade.ProbabilityPerDraw, 1d / 16) && Close(medBandage.ProbabilityPerDraw, 5d / 16), "Duplicate category weights must match the game's category-array sampling");
        var capsuleBandage = index.Get(UeiEntryKind.Item, "bandage").Single(x => ReferenceEquals(x.Building, capsule));
        check(Close(capsuleBandage.ProbabilityPerDraw, 2d / 9) && Close(capsuleBandage.AtLeastOnce, 1 - Math.Pow(7d / 9, 2)), "Duplicate pool members and multiple draws must preserve the exact random selection weights");
        check(medLemonade.ProbabilityPerDraw < 1 && medLemonade.Draws == 1, "A guaranteed random draw must not be presented as a guaranteed specific item");
        var blood = index.Get(UeiEntryKind.Item, "bloodbaghuman").Single();
        check(blood.Kind == UeiAcquisitionRouteKind.FixedDrop && Close(blood.ProbabilityPerDraw, .0335)
            && blood.ConditionMin == .5 && blood.ConditionMax == 1, "Fixed drops must apply the multiplier and preserve condition ranges");
        check(!index.Get(UeiEntryKind.Item, "unpooled").Any(x => ReferenceEquals(x.Building, med)), "NaN and zero-probability fixed drops must not become acquisition routes");
        var alwaysRoute = index.Get(UeiEntryKind.Item, "unpooled").Single(x => x.Kind == UeiAcquisitionRouteKind.AlwaysDrop);
        check(alwaysRoute.Draws == 2 && alwaysRoute.ProbabilityPerDraw == 1 && alwaysRoute.ConditionMin == .3, "alwaysDrop ignores chance and must retain repeated guaranteed drops");
        var independent = index.Get(UeiEntryKind.Item, "unpooled").Single(x => x.Kind == UeiAcquisitionRouteKind.FixedDrop);
        check(independent.Draws == 2 && Close(independent.AtLeastOnce, .4375), "Repeated fixed entries must remain independent rolls");
        check(!items.Keys.SelectMany(id => index.Get(UeiEntryKind.Item, id)).Any(x => ReferenceEquals(x.Building, inert)), "Indestructible sources must not advertise their serialized drops");
        check(index.Get(UeiEntryKind.Item, "lemonade").Count(x => x.Building?.Id == "medcrate") == 2,
            "Same-ID canonical and nested variants must stay separate rather than combine their probabilities");
        check(index.Get(UeiEntryKind.Item, "lemonade").Any(x => x.Kind == UeiAcquisitionRouteKind.Trade), "Actual eligible trader-pool membership must produce a random trade route");
        check(index.Get(UeiEntryKind.Item, "untradeable").Any(x => x.Kind == UeiAcquisitionRouteKind.RandomPool)
            && !index.Get(UeiEntryKind.Item, "untradeable").Any(x => x.Kind == UeiAcquisitionRouteKind.Trade), "Untradeable filtering applies to traders, without removing building loot");
        check(!index.Get(UeiEntryKind.Item, "free").Any(x => x.Kind == UeiAcquisitionRouteKind.Trade)
            && !index.Get(UeiEntryKind.Item, "unpooled").Any(x => x.Kind == UeiAcquisitionRouteKind.Trade), "Zero value and absent actual-pool membership must not imply trader stock");
        check(index.Get(UeiEntryKind.Item, "pistol").Single().Kind == UeiAcquisitionRouteKind.WeaponTrade,
            "The shipped special weapon-stock path bypasses the general trader filter");
        check(index.Get(UeiEntryKind.Item, "lemonade").All(x => x.Kind != UeiAcquisitionRouteKind.FilledContainer)
            && index.Get(UeiEntryKind.Liquid, "lemonade").Single().Kind == UeiAcquisitionRouteKind.FilledContainer,
            "An item and liquid with the same ID must not share a source bucket");
        check(index.Get(UeiEntryKind.Liquid, "lemonade").Single().Amount == 750, "Duplicate default-content stacks should be summed per liquid");
        check(index.Get(UeiEntryKind.Liquid, "unknown_liquid").Count == 0 && index.Get(UeiEntryKind.Liquid, "water").Count == 1,
            "Unknown liquids, empty containers and negative default amounts must not create sources");
        check(ReferenceEquals(index.Get(UeiEntryKind.Item, "bandage"), index.Get(UeiEntryKind.Item, "BANDAGE")), "Reverse-source queries must reuse immutable route views");

        var incomplete = new UeiAcquisitionBuilding("brokenpool:0", "brokenpool", "Broken pool", 2, ["water", "missing"]);
        var incompleteIndex = UeiAcquisitionIndex.Build(new(items, pools, new[] { "lemonade" }, new[] { incomplete }));
        check(!incompleteIndex.Get(UeiEntryKind.Item, "lemonade").Single(x => x.Kind == UeiAcquisitionRouteKind.RandomPool).ProbabilityKnown,
            "Missing pool slots must not be discarded to inflate a displayed probability");
        Text(check, index, incompleteIndex);
        Cards(check, index, incompleteIndex);
        AcquisitionCardMergeTests.Run(check);
        Cache(check, Snapshot, () => buildings, items, pools);
        Console.WriteLine("PASS: native loot weights, independent drops, variants, trade rules, typed liquid sources, text and cache invalidation");
    }

    private static void Cards(Action<bool, string> check, UeiAcquisitionIndex index, UeiAcquisitionIndex incomplete)
    {
        string language = UeiPlugin.LanguageMode.Value;
        bool hadMedicalName = Locale.Buildings.TryGetValue("medcrate", out string? medicalName);
        try
        {
            UeiPlugin.LanguageMode.Value = "en";
            Locale.Buildings["medcrate"] = "Medical crate";
            var jars = UeiAcquisitionCard.Build(index, UeiEntryKind.Item, "lemonade");
            check(jars.Count == index.Get(UeiEntryKind.Item, "lemonade").Count
                && jars.All(card => card.ViaContainer == null), "Item cards must preserve direct typed routes without borrowing liquid contents");
            var random = jars.Single(card => card.Route.Building?.Id == "dropcapsule");
            check(random.Facts.Contains("Random drops: 2") && random.Facts.Contains("30.56%")
                && random.ResultBadge == string.Empty, "Random draws must show the at-least-once chance and never an output quantity");
            var blood = UeiAcquisitionCard.Build(index, UeiEntryKind.Item, "bloodbaghuman").Single();
            check(blood.Facts.Contains("3.35% each") && blood.Facts.Contains("50%–100%"),
                "Compact blood-bag cards must retain the independent 3.35% chance and exact condition range");
            var repeated = UeiAcquisitionCard.Build(index, UeiEntryKind.Item, "unpooled");
            check(repeated.Single(card => card.Route.Kind == UeiAcquisitionRouteKind.AlwaysDrop).ResultBadge == "×2",
                "Only guaranteed item drops may use the output-count badge");
            check(repeated.Single(card => card.Route.Kind == UeiAcquisitionRouteKind.FixedDrop).Facts.Contains("Drop chances: 2 · 25% each"),
                "Repeated independent drops must not substitute the combined chance for each roll's probability");
            var liquid = UeiAcquisitionCard.Build(index, UeiEntryKind.Liquid, "lemonade");
            check(liquid.Count == jars.Count && liquid.All(card => card.ViaContainer?.Id == "lemonade"
                && card.LiquidAmount == 750 && card.ResultBadge == "Default 750 mL"),
                "Liquid cards must expand the container's acquisition routes while keeping a default volume rather than a promised drop amount");
            check(liquid.All(card => jars.Any(jar => ReferenceEquals(jar.Route, card.Route))),
                "Liquid chains must use the actual container routes rather than recalculate or merge their probabilities");
            check(liquid.All(card => card.Tooltip.Contains("actual amount depends on spawn condition")),
                "Every expanded liquid source must retain the default-content condition in its tooltip");
            check(jars.Select(card => card.Key).Distinct().Count() == jars.Count
                && liquid.Select(card => card.Key).Distinct().Count() == liquid.Count,
                "Canonical and child variants need distinct stable card keys for source refreshes");
            var unknown = UeiAcquisitionCard.Build(incomplete, UeiEntryKind.Item, "lemonade")
                .Single(card => card.Route.Kind == UeiAcquisitionRouteKind.RandomPool);
            check(unknown.Facts.Contains("Chance unknown") && !unknown.Facts.Split('\n')[0].Contains("%") && unknown.ResultBadge == string.Empty,
                "Incomplete loot pools must not show a fabricated percentage or guaranteed item count");
            var containerOnly = UeiAcquisitionIndex.Build(new(
                new Dictionary<string, UeiAcquisitionItem> { ["jar"] = new("jar", "Jar", 1, contents: [new("water", 500)]) },
                new Dictionary<string, string[]>(), new[] { "water" }, Array.Empty<UeiAcquisitionBuilding>()));
            var initial = UeiAcquisitionCard.Build(containerOnly, UeiEntryKind.Liquid, "water").Single();
            check(initial.Route.Kind == UeiAcquisitionRouteKind.FilledContainer && initial.ViaContainer == null
                && initial.Title == "Jar" && initial.ResultBadge == "Default 500 mL",
                "A filled container must remain a visible source even before any container acquisition routes are known");
            check(UeiAcquisitionCard.Build(UeiAcquisitionIndex.Empty, UeiEntryKind.Item, "absent").Count == 0,
                "Missing acquisition data must not produce a placeholder acquisition route");
            string[] keys = jars.Select(card => card.Key).ToArray();
            UeiPlugin.LanguageMode.Value = "zh";
            Locale.Buildings["medcrate"] = "医疗箱";
            var chinese = UeiAcquisitionCard.Build(index, UeiEntryKind.Item, "lemonade");
            check(chinese.Select(card => card.Key).SequenceEqual(keys), "UI and game-language changes must preserve source-card identity");
            check(chinese.Any(card => card.Title == "医疗箱" && card.Method == "破坏 · 随机掉落")
                && chinese.Single(card => card.Route.Building?.Id == "dropcapsule").Facts.Contains("随机掉落 2 次 · 获得概率 30.56%"),
                "Source-card methods and facts follow UEI language while source names use game localization");
            check(UeiAcquisitionCard.Build(index, UeiEntryKind.Liquid, "lemonade").All(card => card.ResultBadge == "默认 750 mL"),
                "Default liquid-volume badges must be localized without changing their amount");
        }
        finally
        {
            UeiPlugin.LanguageMode.Value = language;
            if (hadMedicalName) Locale.Buildings["medcrate"] = medicalName!;
            else Locale.Buildings.Remove("medcrate");
        }
        Console.WriteLine("PASS: compact source-card probabilities, typed liquid chains, stable identities and localization");
    }

    private static void Text(Action<bool, string> check, UeiAcquisitionIndex index, UeiAcquisitionIndex incomplete)
    {
        string language = UeiPlugin.LanguageMode.Value;
        try
        {
            UeiPlugin.LanguageMode.Value = "en";
            Locale.Buildings["medcrate"] = "Medical crate";
            Locale.Buildings["lifepod"] = "Life pod";
            string native = UeiAcquisitionText.Compose(index, UeiEntryKind.Item, "lemonade", UeiAcquisition.Extract("A 0.75L jar."), UeiAcquisitionLoadState.Ready);
            check(native.Contains("Medical crate") && native.Contains("Trade: traders may stock this item") && !native.Contains("Description supplement"), "Official English descriptions need no annotations for native acquisition data");
            check(native.Contains("inside Life pod") && native.Contains("chance to get this item at least once"), "Nested variants and random eligibility must be explicit in English");
            string liquid = UeiAcquisitionText.Compose(index, UeiEntryKind.Liquid, "lemonade", "", UeiAcquisitionLoadState.Ready);
            check(liquid.Contains("Lemonade jar") && liquid.Contains("750 mL") && liquid.Contains("→ Break"), "Liquid sources must identify their filled container and that container's acquisition routes");
            string annotation = UeiAcquisition.Extract("获取：棘背兽长老（仅特定阶段，概率<5%）");
            string supplemented = UeiAcquisitionText.Compose(index, UeiEntryKind.Item, "lemonade", annotation, UeiAcquisitionLoadState.Ready);
            check(supplemented.IndexOf("Medical crate", StringComparison.Ordinal) < supplemented.IndexOf("Description supplement:", StringComparison.Ordinal)
                && supplemented.EndsWith(annotation, StringComparison.Ordinal), "Native data must lead while preserving the description's complete supplementary conditions");
            check(UeiAcquisitionText.Compose(UeiAcquisitionIndex.Empty, UeiEntryKind.Item, "absent", "", UeiAcquisitionLoadState.Ready) == "",
                "Completed scans with no known sources should let the existing empty state render");
            check(UeiAcquisitionText.Compose(UeiAcquisitionIndex.Empty, UeiEntryKind.Item, "absent", "", UeiAcquisitionLoadState.Loading).Contains("Looking for item sources"),
                "An unfinished scan must not be presented as missing acquisition info");
            check(UeiAcquisitionText.Compose(index, UeiEntryKind.Item, "lemonade", "", UeiAcquisitionLoadState.Partial).Contains("incomplete"),
                "Partially failed scans must keep known sources and disclose incomplete coverage");
            check(UeiAcquisitionText.Compose(UeiAcquisitionIndex.Empty, UeiEntryKind.Item, "absent", annotation, UeiAcquisitionLoadState.Unavailable).Contains(annotation),
                "Index failure must preserve annotation fallback");
            check(!UeiAcquisitionText.Compose(incomplete, UeiEntryKind.Item, "lemonade", "", UeiAcquisitionLoadState.Ready).Contains("at least once"),
                "Invalid pool configurations must not advertise an exact success probability");
            Locale.Buildings["medcrate"] = "医疗箱";
            check(UeiAcquisitionText.Compose(index, UeiEntryKind.Item, "lemonade", "", UeiAcquisitionLoadState.Ready).Contains("Break 医疗箱"),
                "Source names must use the live game locale without rebuilding the raw index");
            UeiPlugin.LanguageMode.Value = "zh";
            check(UeiAcquisitionText.Compose(index, UeiEntryKind.Item, "bloodbaghuman", "", UeiAcquisitionLoadState.Ready).Contains("3.35%")
                && UeiAcquisitionText.Compose(index, UeiEntryKind.Item, "bloodbaghuman", "", UeiAcquisitionLoadState.Ready).Contains("50%–100%"),
                "Chinese native-source text must preserve probability and condition details");
            check(UeiI18n.BuildingText("missing_building", "Fallback") == "Fallback" && UeiI18n.BuildingText("", "Fallback") == "Fallback",
                "Unresolved building locale keys need safe readable fallbacks");
        }
        finally { UeiPlugin.LanguageMode.Value = language; Locale.Buildings.Clear(); }
    }

    private static void Cache(Action<bool, string> check, Func<UeiAcquisitionSnapshot> snapshot,
        Func<UeiAcquisitionBuilding[]> buildings, Dictionary<string, UeiAcquisitionItem> items, Dictionary<string, string[]> pools)
    {
        UeiAcquisitionIndexCache cache = new();
        int reads = 0;
        UeiAcquisitionSnapshot Read() { reads++; return snapshot(); }
        check(cache.Ensure(1, 11, Read), "An acquisition cache must build once when data arrives");
        var first = cache.Current;
        for (int i = 0; i < 100; i++) check(!cache.Ensure(1, 11, Read), "Stable source and registry revisions must reuse the index");
        check(reads == 1 && ReferenceEquals(first, cache.Current), "Warm searches and recipe/locale changes must not reread source snapshots");
        pools["water"] = ["waterjar", "waterjar"];
        check(cache.Ensure(1, 12, Read) && !cache.Current.Get(UeiEntryKind.Item, "lemonade").Any(x => x.Kind == UeiAcquisitionRouteKind.RandomPool),
            "Same-count actual-pool replacement must remove stale loot eligibility");
        items["bandage"] = new("bandage", "Changed", 10, true);
        check(cache.Ensure(1, 13, Read) && !cache.Current.Get(UeiEntryKind.Item, "bandage").Any(x => x.Kind == UeiAcquisitionRouteKind.Trade),
            "Same-count item field/registry replacement must update trade filtering");
        check(cache.Ensure(2, 13, Read) && reads == 4, "Published source revisions must rebuild even if registries stay unchanged");
        var prior = cache.Current;
        bool failed = false;
        try { cache.Ensure(3, 13, () => throw new InvalidOperationException("Transient registry change")); }
        catch (InvalidOperationException) { failed = true; }
        check(failed && ReferenceEquals(prior, cache.Current), "A failed snapshot must retain the last complete index");
        check(cache.Ensure(3, 13, () => new(items, pools, new[] { "lemonade" }, buildings())), "A failed snapshot must be retryable without a new revision");
    }

    private static bool Close(double a, double b) => Math.Abs(a - b) < 1e-12;
}
