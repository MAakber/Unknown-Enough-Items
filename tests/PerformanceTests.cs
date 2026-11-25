using System.Collections;
using System.Reflection;
using UnityEngine.EventSystems;

internal static class PerformanceTests
{
    internal static void Run(Action<bool, string> check)
    {
        CatalogQueries(check);
        ReboundInteractions(check);
        Console.WriteLine("PASS: catalog query reuse, favorite/locale invalidation, rebound clicks, and hovered tooltip refresh");
    }

    private static void CatalogQueries(Action<bool, string> check)
    {
        UeiFavorites.Load("item:c");
        UeiEntry apple = Entry("a", "Apple", "food", "Food", "Apple red food");
        UeiEntry berry = Entry("b", "Berry", "food", "Food", "Berry blue food");
        UeiEntry axe = Entry("c", "Axe", "tools", "Tools", "Axe tools");
        UeiEntry water = Entry("water", "Water", "liquid", "Liquid", "Water liquid", UeiEntryKind.Liquid);
        CountingEntries source = new(water, berry, axe, apple);
        UeiCatalogView view = new();
        IReadOnlyList<UeiEntry> Query(string category = "All", string search = "", int revision = 17)
            => view.GetEntries(source, revision, UeiFavorites.Revision, category, search);

        check(Keys(Query()) == "c,a,b,water", "Listing order must keep favorites first and the existing kind/category/name order");
        IReadOnlyList<UeiEntry> reused = Query();
        for (int i = 0; i < 100; i++) _ = Query("ALL");
        check(ReferenceEquals(reused, Query()) && source.Enumerations == 1, "Unchanged queries must reuse their result without rereading the source catalog");
        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) _ = view.GetEntries(source, 17, UeiFavorites.Revision, "All", "");
        long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        check(allocated == 0, "Repeated warmed catalog queries should allocate no managed memory");
        check(Keys(Query("FoOd", "  APPLE  ")) == "a", "Category/search matching must remain case insensitive and trim search whitespace");
        check(Keys(Query("Liquid")) == "water", "Liquid filtering must exclude items");
        check(Keys(Query("Favorites")) == "c", "Favorite filtering must use current favorite membership");
        check(Query("All", "does not exist").Count == 0, "An unmatched query must clear prior results");
        check(Keys(Query()) == "c,a,b,water" && source.Enumerations == 1, "Changing search/category must reuse sorted catalog data");

        int priorCount = UeiFavorites.Count;
        int priorRevision = UeiFavorites.Revision;
        UeiFavorites.Toggle("item:c");
        UeiFavorites.Toggle("item:b");
        check(UeiFavorites.Count == priorCount && UeiFavorites.Revision != priorRevision, "Replacing one favorite must invalidate even when the count is unchanged");
        check(Keys(Query("Favorites")) == "b" && source.Enumerations == 2, "Favorite cache must reflect a same-count membership replacement");
        check(Keys(Query()) == "b,a,c,water", "Favorite changes must update the all-items sort too");

        UeiFavorites.Load(" ITEM:A | item:a | liquid:water ");
        check(UeiFavorites.Count == 2 && UeiFavorites.Contains("item:A"), "Saved favorites must keep case-insensitive membership and deduplicate keys");
        check(Keys(Query("Favorites")) == "a,water", "Reloaded favorites must invalidate previous cached membership");
        check(string.Equals(UeiFavorites.Serialize(), "ITEM:A|liquid:water", StringComparison.OrdinalIgnoreCase), "Favorites must preserve their existing stable serialized format");
        UeiFavorites.ClearAll();
        check(Query("Favorites").Count == 0, "Clearing favorites must clear the cached favorites listing");
        priorRevision = UeiFavorites.Revision;
        int priorSaves = UeiPlugin.FavoriteSaveCalls;
        UeiFavorites.ClearAll();
        UeiFavorites.Toggle(" ");
        check(UeiFavorites.Revision == priorRevision && UeiPlugin.FavoriteSaveCalls == priorSaves, "No-op favorite actions must not resave or invalidate the catalog");

        apple.DisplayName = "Zulu";
        apple.SearchText = "苹果 红色 食物";
        check(Keys(Query(revision: 18)) == "b,a,c,water", "A catalog revision must refresh sorting even when entry count and source object are unchanged");
        check(Keys(Query(search: "苹果", revision: 18)) == "a", "A locale/catalog revision must refresh searchable metadata");
        CountingEntries replacement = new(Entry("replacement", "Replacement", "food", "Food", "new"));
        check(Keys(view.GetEntries(replacement, 18, UeiFavorites.Revision, "All", "")) == "replacement", "A replacement source must not keep stale entry references");
        check(view.GetEntries(Array.Empty<UeiEntry>(), 19, UeiFavorites.Revision, "All", "").Count == 0, "An empty replacement catalog must clear the prior listing");
        Console.WriteLine($"PASS: 1,000 repeated warmed catalog queries allocated {allocated} bytes (offline helper only)");
    }

    private static void ReboundInteractions(Action<bool, string> check)
    {
        UeiEntry first = Entry("a", "First", "food", "Food", "first");
        UeiEntry second = Entry("b", "Second", "food", "Food", "second");
        List<UeiEntry> left = new();
        List<UeiEntry> right = new();
        UeiEntryClickHandler click = new();
        click.Init(left.Add, right.Add);
        PointerEventData leftEvent = new() { button = PointerEventData.InputButton.Left };
        PointerEventData rightEvent = new() { button = PointerEventData.InputButton.Right };
        click.Bind(first);
        click.OnPointerClick(leftEvent);
        click.Bind(second);
        click.OnPointerClick(leftEvent);
        click.OnPointerClick(rightEvent);
        check(left.SequenceEqual(new[] { first, second }) && right.SequenceEqual(new[] { second }), "A reused cell's left/right actions must use the newly bound entry");
        click.Bind(null);
        click.OnPointerClick(leftEvent);
        click.OnPointerClick(rightEvent);
        check(left.Count == 2 && right.Count == 1, "An empty/loading cell must ignore clicks");
        UeiEntry localized = Entry("a", "更新后", "food", "食物", "中文");
        click.Bind(localized);
        click.OnPointerClick(leftEvent);
        check(ReferenceEquals(left[^1], localized), "A refreshed catalog's same-key entry must replace the old bound reference");
        click.OnPointerClick(new PointerEventData { button = PointerEventData.InputButton.Middle });
        check(left.Count == 3 && right.Count == 1, "Middle-click must not become an item action");

        UeiPanel.Tooltip = null;
        UeiPanel.TooltipSetCalls = 0;
        UeiTooltipRelay relay = new();
        relay.Init("First", "first description");
        check(UeiPanel.Tooltip == null, "Binding a non-hovered cell must not show its tooltip");
        relay.OnPointerEnter(leftEvent);
        check(UeiPanel.Tooltip == ("First", "first description"), "Pointer entry must show the bound cell's tooltip");
        relay.Init("Second", "second description");
        check(UeiPanel.Tooltip == ("Second", "second description"), "Paging under a stationary pointer must refresh the tooltip immediately");
        int before = UeiPanel.TooltipSetCalls;
        relay.Init("Second", "second description");
        check(UeiPanel.TooltipSetCalls == before, "Unchanged binding must not rewrite the hovered tooltip");
        relay.OnPointerExit(leftEvent);
        check(UeiPanel.Tooltip == null, "Pointer exit must clear the UEI tooltip");
        relay.OnPointerEnter(leftEvent);
        typeof(UeiTooltipRelay).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(relay, null);
        relay.Init("Replacement", "replacement description");
        relay.OnPointerMove(leftEvent);
        check(UeiPanel.Tooltip == null, "Disabling an empty/loading cell must clear its tooltip and reset hover state");
    }

    private static UeiEntry Entry(string id, string name, string category, string label, string search,
        UeiEntryKind kind = UeiEntryKind.Item) => new()
    {
        Id = id, EntryKey = UeiCatalog.EntryKey(kind, id), Kind = kind, DisplayName = name,
        CategoryKey = category, CategoryLabel = label, SearchText = search,
    };

    private static string Keys(IReadOnlyList<UeiEntry> entries) => string.Join(",", entries.Select(e => e.Id));

    private sealed class CountingEntries(params UeiEntry[] entries) : IReadOnlyList<UeiEntry>
    {
        internal int Enumerations;
        public int Count => entries.Length;
        public UeiEntry this[int index] => entries[index];
        public IEnumerator<UeiEntry> GetEnumerator()
        {
            Enumerations++;
            return ((IEnumerable<UeiEntry>)entries).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
