internal static class AcquisitionCardMergeTests
{
    internal static void Run(Action<bool, string> check)
    {
        string language = UeiPlugin.LanguageMode.Value;
        var names = new Dictionary<string, string>(Locale.Buildings);
        var items = new Dictionary<string, UeiAcquisitionItem>
        {
            ["target"] = new("target", "Target", 1),
            ["other"] = new("other", "Other", 1),
            ["jara"] = new("jara", "Jar A", 1, contents: [new("drink", 750)]),
            ["jarb"] = new("jarb", "Jar B", 1, contents: [new("drink", 750)]),
        };
        var pools = new Dictionary<string, string[]>
        {
            ["supplies"] = new[] { "target" }.Concat(Enumerable.Repeat("other", 171)).ToArray(),
            ["half"] = ["target", "other"],
            ["jars"] = ["jara", "jarb"],
        };
        UeiAcquisitionIndex Index(params UeiAcquisitionBuilding[] sources)
            => UeiAcquisitionIndex.Build(new(items, pools, new[] { "drink" }, sources));
        List<UeiAcquisitionCard> Cards(params UeiAcquisitionBuilding[] sources)
            => UeiAcquisitionCard.Build(Index(sources), UeiEntryKind.Item, "target");
        UeiAcquisitionBuilding canonical = new("z-crate:0", "supplycrate", "Supply crate", 1, ["supplies"]);
        UeiAcquisitionBuilding nested = new("a-bigcontainer:1", "supplycrate", "Supply crate", 1, ["supplies"],
            contextId: "bigcontainer", contextName: "BigContainer");
        try
        {
            UeiPlugin.LanguageMode.Value = "en";
            Locale.Buildings["supplycrate"] = "Supply crate";
            Locale.Buildings["bigcontainer"] = "BigContainer";
            var index = Index(canonical, nested);
            var merged = UeiAcquisitionCard.Build(index, UeiEntryKind.Item, "target").Single();
            check(index.Get(UeiEntryKind.Item, "target").Count == 2 && merged.Routes.Count == 2,
                "Merging cards must retain both original canonical and nested routes in the raw index");
            check(merged.Routes.All(route => index.Get(UeiEntryKind.Item, "target").Contains(route))
                && ReferenceEquals(merged.Route.Building, canonical),
                "Merged cards must use original routes and prefer the canonical source for presentation");
            check(merged.Title == "Supply crate" && !merged.Title.Contains("BigContainer"),
                "Identical crate variants need one readable title without the internal parent name");
            check(merged.Route.Draws == 1 && Math.Abs(merged.Route.ProbabilityPerDraw - 1d / 172) < 1e-12
                && merged.Facts.Contains("Random drops: 1 · Item chance: 0.58%") && merged.Facts.Contains("condition 100%")
                && merged.ResultBadge == string.Empty,
                "Merging two 0.58% crate variants must not add probabilities, draws or item counts");
            check(merged.Tooltip.Contains("Found in these places (same drops):") && merged.Tooltip.Contains("Supply crate (on its own)")
                && merged.Tooltip.Contains("inside BigContainer"),
                "Merged tooltips must disclose canonical and nested source contexts");
            check(merged.Routes is IList<UeiAcquisitionRoute> view && view.IsReadOnly,
                "Merged source views must not allow callers to remove original routes");
            check(Cards(nested).Single().Key == merged.Key,
                "A late canonical variant must keep the existing nested group's page anchor");
            UeiAcquisitionBuilding otherId = new("m-other:0", "othercrate", "Supply crate", 1, ["supplies"]);
            check(Cards(canonical, otherId).Count == 2,
                "Different source IDs must not merge just because the localized names and chances match");
            check(Cards(nested, otherId).Select(card => card.Key).SequenceEqual(Cards(canonical, nested, otherId).Select(card => card.Key)),
                "Adding a matching prefab variant must not reorder existing card groups");

            UeiAcquisitionBuilding Fixed(string key, double probability, double min = 1, double max = 1,
                bool animal = false, string id = "supplycrate")
                => new(key, id, "Supply crate", drops: [new("target", probability, min, max)], animal: animal);
            var rounded = Cards(Fixed("rounded-a", .00580), Fixed("rounded-b", .00581));
            check(rounded.Count == 2 && rounded.All(card => card.Facts.Contains("0.58%")),
                "Different exact probabilities must remain separate even when their displayed percentages match");
            check(Cards(Fixed("min-a", .5, .5), Fixed("min-b", .5, .6)).Count == 2,
                "Different minimum conditions must remain separate");
            check(Cards(Fixed("max-a", .5, .5, 1), Fixed("max-b", .5, .5, .9)).Count == 2,
                "Different maximum conditions must remain separate");
            UeiAcquisitionBuilding twoRolls = new("two-rolls", "supplycrate", "Supply crate",
                drops: [new("target", .5), new("target", .5)]);
            check(Cards(Fixed("one-roll", .5), twoRolls).Count == 2,
                "One and two independent rolls must not merge into a single card");
            UeiAcquisitionBuilding randomHalf = new("random-half", "supplycrate", "Supply crate", 1, ["half"]);
            check(Cards(Fixed("fixed-half", .5), randomHalf).Count == 2,
                "Independent drops and random pool draws must remain separate even with equal probabilities");
            UeiAcquisitionBuilding guaranteedA = new("always-a", "supplycrate", "Supply crate", always: [new("target", 0)]);
            UeiAcquisitionBuilding guaranteedB = new("always-b", "supplycrate", "Supply crate", always: [new("target", 0)],
                contextId: "bigcontainer", contextName: "BigContainer");
            var guaranteed = Cards(guaranteedA, guaranteedB).Single();
            check(guaranteed.Routes.Count == 2 && guaranteed.Route.Draws == 1 && guaranteed.ResultBadge == "×1",
                "Two matching guaranteed sources must not become a guaranteed two-item drop");
            check(Cards(guaranteedA, Fixed("certain-independent", 1)).Count == 2,
                "Guaranteed and independent drops must retain their distinct methods");
            UeiAcquisitionBuilding twoGuaranteed = new("two-always", "supplycrate", "Supply crate",
                always: [new("target", 0), new("target", 0)]);
            check(Cards(guaranteedA, twoGuaranteed).Count == 2,
                "Different guaranteed quantities must remain separate");
            check(Cards(Fixed("break", .5), Fixed("defeat", .5, animal: true)).Count == 2,
                "Break and defeat sources must remain separate");
            check(Cards(Fixed("anonymous-a", .5, id: ""), Fixed("anonymous-b", .5, id: "")).Count == 2,
                "Unknown source IDs must keep distinct prefab identities");
            check(Cards(Fixed("unknown-condition-a", .5, double.NaN), Fixed("unknown-condition-b", .5, double.NaN)).Count == 2,
                "Unknown durability must not be treated as proof that source rules match");
            UeiAcquisitionBuilding repeatedUnknown = new("repeated-unknown", "supplycrate", "Supply crate",
                drops: [new("target", .5, double.NaN), new("target", .5, double.NaN)]);
            var unknownRecords = Cards(repeatedUnknown);
            check(unknownRecords.Count == 2 && unknownRecords.All(card => card.Routes.Count == 1)
                && unknownRecords.Select(card => card.Key).Distinct().Count() == 2,
                "Repeated incomplete records in the same prefab must remain separate with distinct card identities");
            UeiAcquisitionBuilding unknownA = new("unknown-pool-a", "supplycrate", "Supply crate", 1, ["supplies", "missing"]);
            UeiAcquisitionBuilding unknownB = new("unknown-pool-b", "supplycrate", "Supply crate", 1, ["supplies", "missing"]);
            check(Cards(unknownA, unknownB).Count == 2,
                "Incomplete loot probabilities must keep separate prefab identities");

            UeiAcquisitionBuilding jarRoot = new("jar-root", "supplycrate", "Supply crate", 1, ["jars"]);
            UeiAcquisitionBuilding jarChild = new("jar-child", "supplycrate", "Supply crate", 1, ["jars"],
                contextId: "bigcontainer", contextName: "BigContainer");
            var drinks = UeiAcquisitionCard.Build(Index(jarRoot, jarChild), UeiEntryKind.Liquid, "drink");
            check(drinks.Count == 2 && drinks.All(card => card.Routes.Count == 2 && card.ResultBadge == "Default 750 mL")
                && drinks.Select(card => card.ViaContainer?.Id).Order().SequenceEqual(new[] { "jara", "jarb" }),
                "Liquid cards must merge matching prefab variants while keeping distinct filled containers");
            check(drinks.All(card => card.Tooltip.Contains("inside BigContainer")
                && card.Tooltip.Contains("actual amount depends on spawn condition")),
                "Merged liquid tooltips must preserve the source context and default-content condition");
            string jarKey = drinks.Single(card => card.ViaContainer?.Id == "jara").Key;
            items["jara"] = new("jara", "Jar A", 1, contents: [new("drink", 500)]);
            var changedVolume = UeiAcquisitionCard.Build(Index(jarRoot, jarChild), UeiEntryKind.Liquid, "drink")
                .Single(card => card.ViaContainer?.Id == "jara");
            check(changedVolume.Key != jarKey && changedVolume.ResultBadge == "Default 500 mL",
                "Changing a container's default liquid amount must update its card identity and volume");

            UeiPlugin.LanguageMode.Value = "zh";
            Locale.Buildings["supplycrate"] = "物资箱";
            var chinese = Cards(canonical, nested).Single();
            check(chinese.Key == merged.Key && chinese.Title == "物资箱"
                && chinese.Tooltip.Contains("出现位置（掉落相同）：") && chinese.Tooltip.Contains("物资箱（单独出现）")
                && chinese.Tooltip.Contains("BigContainer") && chinese.Facts.Contains("随机掉落 1 次 · 获得概率 0.58%"),
                "Merged cards must retain identity, conditions and source context across UI and game localization changes");
        }
        finally
        {
            UeiPlugin.LanguageMode.Value = language;
            Locale.Buildings.Clear();
            foreach (var name in names) Locale.Buildings.Add(name.Key, name.Value);
        }
        Console.WriteLine("PASS: matching source-card merging, exact rule boundaries, preserved contexts, stable pages and liquid chains");
    }
}
