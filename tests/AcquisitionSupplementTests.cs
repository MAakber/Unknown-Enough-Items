internal static class AcquisitionSupplementTests
{
    internal static void Run(Action<bool, string> check)
    {
        string uiLanguage = UeiPlugin.LanguageMode.Value;
        string gameLanguage = Locale.currentLangName;
        var buildingsBefore = new Dictionary<string, string>(Locale.Buildings);
        var itemsBefore = new Dictionary<string, string>(Locale.Items);
        try
        {
            SetGameLanguage(chinese: true);
            UeiPlugin.LanguageMode.Value = "zh";
            var items = new Dictionary<string, UeiAcquisitionItem>
            {
                ["bandage"] = new("bandage", "Bandage", 10),
                ["waterjar"] = new("waterjar", "Water jar", 3, contents: [new("water", 500)]),
                ["lemonadejar"] = new("lemonadejar", "Lemonade jar", 2, contents: [new("lemonade", 750)]),
                ["salinejar"] = new("salinejar", "Saline jar", 1, contents: [new("saline", 100)]),
                ["unpooled"] = new("unpooled", "Unpooled", 10),
                ["pistol"] = new("pistol", "Pistol", 0, true),
            };
            var pools = new Dictionary<string, string[]>
            {
                ["medical"] = ["bandage"],
                ["water"] = ["waterjar"],
            };
            var med = new UeiAcquisitionBuilding("medcrate:0", "medcrate", "Medical crate", 2, ["medical"]);
            var capsule = new UeiAcquisitionBuilding("dropcapsule:0", "dropcapsule", "Drop capsule",
                drops: [new("bandage", .4, .3, .9), new("waterjar", .5)]);
            var podChest = new UeiAcquisitionBuilding("lifepodchest:0", "lifepodchest", "Lifepod chest",
                always: [new("bandage", 1)]);
            var nestedMed = new UeiAcquisitionBuilding("lifepod:medcrate", "medcrate", "Medical crate",
                always: [new("bandage", 1)], contextId: "lifepod", contextName: "Lifepod");
            var crate = new UeiAcquisitionBuilding("containercrate:0", "containercrate", "Container crate",
                drops: [new("lemonadejar", .2)]);
            UeiAcquisitionSnapshot Snapshot(UeiAcquisitionBuilding[] sources)
                => new(items, pools, new[] { "water", "lemonade", "saline" }, sources);
            UeiAcquisitionIndex index = UeiAcquisitionIndex.Build(Snapshot([med, capsule, podChest, nestedMed, crate]));

            const string screenshotSources = "医疗箱/空投舱/维生舱箱子/交易/地面刷新";
            string annotation = UeiAcquisition.Extract("┃获取：" + screenshotSources);
            check(annotation == screenshotSources, "Supplement filtering must leave the original screenshot annotation available to the catalog");
            void Expect(string raw, string expected, string message)
                => check(UeiAcquisitionSupplement.Filter(index, UeiEntryKind.Item, "bandage", raw) == expected, message);

            Expect(annotation, "地面刷新", "Screenshot sources already present on cards must leave only ground spawns, including the verified 医疗箱 alias");
            Expect("医疗箱/空投舱/维生舱箱子/交易", "", "An annotation fully covered by acquisition cards must produce an empty supplement");
            Expect("医疗物资箱／空投舱、交易", "", "The live building name and alternate list separators must recognize the same actual sources");
            Expect(" medcrate / DROPcapsule / lifepodchest / Trade ", "", "Complete IDs and case-insensitive source names may be removed without fuzzy matching");
            Expect("Medical\tcrate/交易/地面刷新", "地面刷新", "Source-name whitespace may vary without discarding the unknown remainder");
            Expect("医疗箱/医疗箱子/medcrate_custom/交易/自定义商人", "医疗箱子/medcrate_custom/自定义商人",
                "Source prefixes and mod names must not be mistaken for the covered medical crate or trader");
            Expect("维生舱/医疗箱", "维生舱", "A child drop source must not cover a bare parent location annotation");
            Expect("  未知箱子（夜间）  ", "  未知箱子（夜间）  ", "When no source can be removed, preserve the original annotation byte for byte");
            Expect(" \t\n", "", "Whitespace-only annotations must not create a supplement tab");

            Conditions(check, Expect);
            string fullText = UeiAcquisitionText.Compose(index, UeiEntryKind.Item, "bandage", annotation,
                UeiAcquisitionLoadState.Ready, out string supplement);
            check(supplement == "地面刷新" && fullText.EndsWith("描述补充：\n地面刷新", StringComparison.Ordinal),
                "Composition must append exactly the filtered supplement returned for the UI cache");
            check(UeiAcquisitionText.Compose(index, UeiEntryKind.Item, "bandage", "医疗箱/交易", UeiAcquisitionLoadState.Ready)
                .Contains("描述补充：") == false, "A fully covered annotation must not leave an empty supplement heading in composed text");
            check(fullText == UeiAcquisitionText.Compose(index, UeiEntryKind.Item, "bandage", annotation, UeiAcquisitionLoadState.Ready),
                "Existing composition callers must share the filtered result with the catalog overload");

            var routesBefore = index.Get(UeiEntryKind.Item, "bandage").ToArray();
            var probabilitiesBefore = routesBefore.Select(route => (route.ProbabilityPerDraw, route.Draws, route.ConditionMin, route.ConditionMax)).ToArray();
            Expect(annotation, "地面刷新", "Filtering an already-read annotation must be repeatable");
            check(index.Get(UeiEntryKind.Item, "bandage").SequenceEqual(routesBefore)
                && index.Get(UeiEntryKind.Item, "bandage").Select(route => (route.ProbabilityPerDraw, route.Draws, route.ConditionMin, route.ConditionMax))
                    .SequenceEqual(probabilitiesBefore), "Supplement filtering must preserve native route identities, chances, draw counts and condition ranges");
            check(index.Get(UeiEntryKind.Item, "bandage").Single(route => ReferenceEquals(route.Building, capsule)).ProbabilityPerDraw == .4,
                "Filtering the description must retain the fixture's independent drop probability");

            var aliasOnlyByName = new UeiAcquisitionBuilding("mod:medical", "mod_medcrate", "医疗物资箱", drops: [new("bandage", .2)]);
            var unrelatedIndex = UeiAcquisitionIndex.Build(Snapshot([aliasOnlyByName]));
            check(UeiAcquisitionSupplement.Filter(unrelatedIndex, UeiEntryKind.Item, "bandage", "医疗箱/交易") == "医疗箱",
                "The verified 医疗箱 alias must be tied to medcrate's ID rather than a similar mod object's name");
            check(UeiAcquisitionSupplement.Filter(index, UeiEntryKind.Item, "unpooled", annotation) == annotation,
                "Sources and trade routes belonging to other items must not remove this item's fallback annotation");
            check(UeiAcquisitionSupplement.Filter(UeiAcquisitionIndex.Empty, UeiEntryKind.Item, "bandage", annotation) == annotation,
                "An unavailable native index must retain all annotated sources");
            check(UeiAcquisitionSupplement.Filter(index, UeiEntryKind.Item, "pistol", "Trade/Weapon trader/武器商人/Ground spawns") == "Ground spawns",
                "Actual special weapon stock may cover both generic and weapon-trader source labels");
            Expect("Trade/Weapon trader", "Weapon trader", "Generic trade eligibility must not claim a special weapon-trader source");

            SourceUpdates(check, annotation, () => Snapshot([med]), () => Snapshot([med, capsule, podChest]));
            Liquids(check, index);
            Languages(check, index, annotation);
        }
        finally
        {
            UeiPlugin.LanguageMode.Value = uiLanguage;
            Locale.currentLangName = gameLanguage;
            Locale.Buildings.Clear();
            foreach (var value in buildingsBefore) Locale.Buildings.Add(value.Key, value.Value);
            Locale.Items.Clear();
            foreach (var value in itemsBefore) Locale.Items.Add(value.Key, value.Value);
        }
        Console.WriteLine("PASS: acquisition supplement deduplication, preserved conditions, partial scans, liquid chains and localization");
    }

    private static void Conditions(Action<bool, string> check, Action<string, string, string> expect)
    {
        expect("医疗箱（仅阶段2，概率<5%）/空投舱/交易（商人等级3，价格12）/地面刷新",
            "医疗箱（仅阶段2，概率<5%）/交易（商人等级3，价格12）/地面刷新",
            "Additional stage, chance and price conditions must remain attached to their complete source names");
        expect("医疗箱（低层/高层）/交易/地面刷新", "医疗箱（低层/高层）/地面刷新",
            "A slash inside source qualifications must not split or discard those qualifications");
        expect("医疗箱；仅低层/交易", "医疗箱；仅低层", "A semicolon may attach a condition and must not be treated as a source-list separator");
        expect("Medical crate, only at night/Trade", "Medical crate, only at night", "A comma may attach a condition and must remain with its source");
        expect("Trade: 12 coins/Medical crate", "Trade: 12 coins", "Price notes after a colon must not be stripped with a trade label");
        expect("自定义箱[夜间/低层（阶段2/3）]/医疗箱/交易", "自定义箱[夜间/低层（阶段2/3）]",
            "Nested bracketed conditions must preserve their internal separators");
        expect("\"自定义箱/特殊交易\"/医疗箱", "\"自定义箱/特殊交易\"", "Quoted source text must retain slashes as part of that source");
        expect("自定义箱“夜间/低层”/医疗箱", "自定义箱“夜间/低层”", "Chinese quotes must protect source qualifications");
        expect("Trader's cache/交易", "Trader's cache", "An English possessive must not turn another source into a malformed quotation");
        expect("Medical crate/Trade\nOnly after stage 2", "Medical crate/Trade\nOnly after stage 2",
            "A standalone following-line condition must retain its entire source list for context");
        expect("Medical crate/（仅阶段2，概率<5%）/Trade", "Medical crate/（仅阶段2，概率<5%）/Trade",
            "A bracketed note separated by a slash must keep the sources it may qualify");
        expect("Medical crate/[night/low floors]/Trade", "Medical crate/[night/low floors]/Trade",
            "A separate bracketed qualification must retain both its internal slash and source context");
        expect("(Only after stage 2)\nMedical crate/Trade", "(Only after stage 2)\nMedical crate/Trade",
            "A bracketed condition before a source list must keep that following source context");
        expect("Medical crate\r\nDrop capsule\r\n(Only at night, chance <5%)",
            "Medical crate\r\nDrop capsule\r\n(Only at night, chance <5%)",
            "A final condition must keep the preceding multiline source block rather than becoming an orphaned note");
        expect("Medical crate\nTrade", "", "A multiline list containing only covered source names needs no supplement");
        expect("Medical crate\u2028Trade", "", "Unicode line separators in a fully covered source list must not keep an empty supplement");
        expect("未知箱子/Medical crate\n仅夜间/Trade", "未知箱子/Medical crate\n仅夜间",
            "A following-line note must keep even a source list mixing covered and unknown names");
        foreach (string malformed in new[]
        {
            "Medical crate(unknown/Trade", "Medical crate)/Trade", "Medical crate/[unknown)/Trade",
            "Medical crate/\"unknown/Trade", "Medical crate//Trade", "Medical crate/https://example.invalid/source",
        })
            expect(malformed, malformed, "Malformed grouping or ambiguous separators must retain the original annotation: " + malformed);
        check(UeiAcquisition.Extract("获取：医疗箱（低层/高层）/交易") == "医疗箱（低层/高层）/交易",
            "Source-list filtering must not alter the acquisition extractor's qualifier and slash preservation");
    }

    private static void SourceUpdates(Action<bool, string> check, string annotation,
        Func<UeiAcquisitionSnapshot> partialSnapshot, Func<UeiAcquisitionSnapshot> fullSnapshot)
    {
        var cache = new UeiAcquisitionIndexCache();
        check(cache.Ensure(1, 1, partialSnapshot), "The first partial source snapshot must populate the native index");
        string partial = UeiAcquisitionText.Compose(cache.Current, UeiEntryKind.Item, "bandage", annotation,
            UeiAcquisitionLoadState.Loading, out string partialSupplement);
        check(partialSupplement == "空投舱/维生舱箱子/地面刷新" && partial.Contains("正在查找获取途径"),
            "Loading scans must remove known duplicates while retaining sources not yet discovered");
        check(cache.Ensure(2, 1, fullSnapshot), "A source addition must invalidate the cached native snapshot");
        UeiAcquisitionText.Compose(cache.Current, UeiEntryKind.Item, "bandage", annotation,
            UeiAcquisitionLoadState.Ready, out string completeSupplement);
        check(completeSupplement == "地面刷新", "New source cards must remove only the newly covered annotations");
        check(!cache.Ensure(2, 1, () => throw new Exception("unchanged source index reread")), "Unchanged native revisions must reuse their index");
        check(cache.Ensure(3, 1, partialSnapshot), "A source removal must refresh the native index");
        string removed = UeiAcquisitionText.Compose(cache.Current, UeiEntryKind.Item, "bandage", annotation,
            UeiAcquisitionLoadState.Partial, out string restoredSupplement);
        check(restoredSupplement == partialSupplement && removed.Contains("部分获取信息读取失败"),
            "A removed or unreadable source must restore its raw annotation and retain the partial-data notice");
    }

    private static void Liquids(Action<bool, string> check, UeiAcquisitionIndex index)
    {
        check(UeiAcquisitionSupplement.Filter(index, UeiEntryKind.Liquid, "water", "水罐/空投舱/交易/地面刷新") == "地面刷新",
            "Liquid supplements must recognize their actual filled container and the same source chains displayed on cards");
        check(UeiAcquisitionSupplement.Filter(index, UeiEntryKind.Liquid, "water", "医疗箱/物资箱/柠檬水罐/空投舱/水罐/交易/地面刷新")
            == "医疗箱/物资箱/柠檬水罐/地面刷新", "A liquid must not borrow drop sources or names from unrelated containers");
        check(UeiAcquisitionSupplement.Filter(index, UeiEntryKind.Liquid, "saline", "Saline jar/Medical crate/Trade") == "Medical crate/Trade",
            "A container-only card must cover that container without inventing acquisition or trading routes for it");
        check(UeiAcquisitionCard.Build(index, UeiEntryKind.Liquid, "saline").Single().Route.Kind == UeiAcquisitionRouteKind.FilledContainer,
            "The retained container-only coverage must correspond to an actual displayed card");
        string raw = "水罐/空投舱/交易";
        check(UeiAcquisitionSupplement.Filter(index, UeiEntryKind.Liquid, "unknown", raw) == raw,
            "Unknown liquids must retain their complete annotation even when other liquids have those sources");
        string text = UeiAcquisitionText.Compose(index, UeiEntryKind.Liquid, "water", "水罐/空投舱/交易/地面刷新",
            UeiAcquisitionLoadState.Ready, out string supplement);
        check(supplement == "地面刷新" && text.Contains("500 mL") && text.Contains("→") && text.EndsWith("地面刷新", StringComparison.Ordinal),
            "Filtering a liquid supplement must retain the native default volume and container acquisition chain");
    }

    private static void Languages(Action<bool, string> check, UeiAcquisitionIndex index, string chineseAnnotation)
    {
        UeiPlugin.LanguageMode.Value = "en";
        check(UeiAcquisitionSupplement.Filter(index, UeiEntryKind.Item, "bandage", chineseAnnotation) == "地面刷新",
            "English UEI controls with Chinese game text must still remove the Chinese source duplicates");
        string englishUi = UeiAcquisitionText.Compose(index, UeiEntryKind.Item, "bandage", chineseAnnotation,
            UeiAcquisitionLoadState.Ready, out string chineseSupplement);
        check(englishUi.Contains("Description supplement:") && englishUi.Contains("医疗物资箱") && chineseSupplement == "地面刷新",
            "UI headings must follow UEI language while names and remaining annotations keep the game language");
        SetGameLanguage(chinese: false);
        const string englishAnnotation = "Medical crate/Drop capsule/Lifepod chest/Trade/Ground spawns";
        check(UeiAcquisitionSupplement.Filter(index, UeiEntryKind.Item, "bandage", englishAnnotation) == "Ground spawns",
            "English game source names must be filtered after a locale change without rebuilding typed routes");
        UeiPlugin.LanguageMode.Value = "zh";
        string chineseUi = UeiAcquisitionText.Compose(index, UeiEntryKind.Item, "bandage", englishAnnotation,
            UeiAcquisitionLoadState.Ready, out string englishSupplement);
        check(chineseUi.Contains("描述补充：") && chineseUi.Contains("Drop capsule") && englishSupplement == "Ground spawns",
            "Chinese UEI controls with English game text must retain only the English remainder");
        Locale.Buildings.Remove("dropcapsule");
        check(UeiAcquisitionSupplement.Filter(index, UeiEntryKind.Item, "bandage", "Drop capsule/dropcapsule/Ground spawns") == "Ground spawns",
            "Missing localization must still match the verified source's fallback name and complete ID");
        check(UeiAcquisitionSupplement.Filter(index, UeiEntryKind.Item, "bandage", "医疗箱/Medical crate") == "",
            "The verified medical-crate alias may match its actual source across UI and game language combinations");
    }

    private static void SetGameLanguage(bool chinese)
    {
        Locale.currentLangName = chinese ? "zh-CN" : "en";
        Locale.Buildings["medcrate"] = chinese ? "医疗物资箱" : "Medical crate";
        Locale.Buildings["dropcapsule"] = chinese ? "空投舱" : "Drop capsule";
        Locale.Buildings["lifepodchest"] = chinese ? "维生舱箱子" : "Lifepod chest";
        Locale.Buildings["lifepod"] = chinese ? "维生舱" : "Lifepod";
        Locale.Buildings["containercrate"] = chinese ? "物资箱" : "Container crate";
        Locale.Items["waterjar"] = chinese ? "水罐" : "Water jar";
        Locale.Items["lemonadejar"] = chinese ? "柠檬水罐" : "Lemonade jar";
    }
}
