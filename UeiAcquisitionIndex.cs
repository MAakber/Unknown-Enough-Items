using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

internal enum UeiAcquisitionLoadState { Loading, Ready, Partial, Unavailable }
internal enum UeiAcquisitionRouteKind { FixedDrop, AlwaysDrop, RandomPool, Trade, WeaponTrade, FilledContainer }

internal readonly struct UeiAcquisitionDrop
{
    public readonly string Id;
    public readonly double Chance;
    public readonly double ConditionMin;
    public readonly double ConditionMax;
    public UeiAcquisitionDrop(string id, double chance, double conditionMin = 1, double conditionMax = 1)
    {
        Id = id;
        Chance = chance;
        ConditionMin = conditionMin;
        ConditionMax = conditionMax;
    }
}

internal readonly struct UeiAcquisitionLiquidContent
{
    public readonly string Id;
    public readonly double Amount;
    public UeiAcquisitionLiquidContent(string id, double amount) { Id = id; Amount = amount; }
}

internal sealed class UeiAcquisitionItem
{
    public readonly string Id;
    public readonly string Name;
    public readonly double Value;
    public readonly bool Untradeable;
    public readonly IReadOnlyList<UeiAcquisitionLiquidContent> Contents;
    public UeiAcquisitionItem(string id, string name, double value, bool untradeable = false,
        UeiAcquisitionLiquidContent[]? contents = null)
    {
        Id = id;
        Name = name;
        Value = value;
        Untradeable = untradeable;
        Contents = Array.AsReadOnly(contents == null ? Array.Empty<UeiAcquisitionLiquidContent>() : (UeiAcquisitionLiquidContent[])contents.Clone());
    }
}

internal sealed class UeiAcquisitionBuilding
{
    public readonly string Key;
    public readonly string Id;
    public readonly string Name;
    public readonly string ContextId;
    public readonly string ContextName;
    public readonly bool Animal;
    public readonly bool Indestructible;
    public readonly double Multiplier;
    public readonly int RandomDraws;
    public readonly IReadOnlyList<string> Categories;
    public readonly IReadOnlyList<UeiAcquisitionDrop> Drops;
    public readonly IReadOnlyList<UeiAcquisitionDrop> Always;
    public UeiAcquisitionBuilding(string key, string id, string name, int randomDraws = 0,
        string[]? categories = null, UeiAcquisitionDrop[]? drops = null, UeiAcquisitionDrop[]? always = null,
        double multiplier = 1, bool indestructible = false, bool animal = false,
        string contextId = "", string contextName = "")
    {
        Key = key;
        Id = id;
        Name = name;
        ContextId = contextId;
        ContextName = contextName;
        Animal = animal;
        Indestructible = indestructible;
        Multiplier = multiplier;
        RandomDraws = randomDraws;
        Categories = Array.AsReadOnly(categories == null ? Array.Empty<string>() : (string[])categories.Clone());
        Drops = Array.AsReadOnly(drops == null ? Array.Empty<UeiAcquisitionDrop>() : (UeiAcquisitionDrop[])drops.Clone());
        Always = Array.AsReadOnly(always == null ? Array.Empty<UeiAcquisitionDrop>() : (UeiAcquisitionDrop[])always.Clone());
    }
}

internal sealed class UeiAcquisitionSnapshot
{
    public readonly IReadOnlyDictionary<string, UeiAcquisitionItem> Items;
    public readonly IReadOnlyDictionary<string, string[]> Pools;
    public readonly IReadOnlyCollection<string> Liquids;
    public readonly IReadOnlyList<UeiAcquisitionBuilding> Buildings;
    public UeiAcquisitionSnapshot(IReadOnlyDictionary<string, UeiAcquisitionItem> items,
        IReadOnlyDictionary<string, string[]> pools, IReadOnlyCollection<string> liquids,
        IReadOnlyList<UeiAcquisitionBuilding> buildings)
    {
        Items = items;
        Pools = pools;
        Liquids = liquids;
        Buildings = buildings;
    }
}

internal sealed class UeiAcquisitionRoute
{
    public readonly UeiAcquisitionRouteKind Kind;
    public readonly UeiAcquisitionBuilding? Building;
    public readonly UeiAcquisitionItem? Container;
    public readonly double ProbabilityPerDraw;
    public readonly bool ProbabilityKnown;
    public readonly int Draws;
    public readonly double ConditionMin;
    public readonly double ConditionMax;
    public readonly double Amount;
    public double AtLeastOnce => 1 - Math.Pow(1 - ProbabilityPerDraw, Draws);
    public UeiAcquisitionRoute(UeiAcquisitionRouteKind kind, UeiAcquisitionBuilding? building = null,
        UeiAcquisitionItem? container = null, double probability = 1, int draws = 1,
        double conditionMin = 1, double conditionMax = 1, bool probabilityKnown = true, double amount = 0)
    {
        Kind = kind;
        Building = building;
        Container = container;
        ProbabilityPerDraw = probability;
        ProbabilityKnown = probabilityKnown;
        Draws = draws;
        ConditionMin = conditionMin;
        ConditionMax = conditionMax;
        Amount = amount;
    }
}

// Pure reverse index. Sources stay separate by canonical prefab and child
// variant; duplicate category names and duplicate pool members remain weights.
internal sealed class UeiAcquisitionIndex
{
    // Verified against the full game's TraderScript.GenerateSingleItemList and
    // GenerateInventory. These are selection rules, not a copied Wiki item table.
    internal static readonly string[] TraderCategories = { "medical", "food", "water", "tool", "drug", "container", "utility", "custom" };
    internal static readonly string[] WeaponTraderItems = { "smallmagazine", "riflemagazine", "boxof12gauge", "pistol", "rifle", "shotgun" };
    private readonly Dictionary<string, List<UeiAcquisitionRoute>> _routes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<UeiAcquisitionRoute>> _views = new(StringComparer.OrdinalIgnoreCase);
    public static UeiAcquisitionIndex Empty { get; } = new();

    public IReadOnlyList<UeiAcquisitionRoute> Get(UeiEntryKind kind, string id)
        => _views.TryGetValue(UeiCatalog.EntryKey(kind, id), out IReadOnlyList<UeiAcquisitionRoute>? routes)
            ? routes : Array.Empty<UeiAcquisitionRoute>();

    public static UeiAcquisitionIndex Build(UeiAcquisitionSnapshot snapshot)
    {
        UeiAcquisitionIndex index = new();
        foreach (UeiAcquisitionBuilding building in snapshot.Buildings)
        {
            if (building.Indestructible) continue;
            foreach (UeiAcquisitionDrop drop in building.Drops)
            {
                double probability = Math.Min(1, Math.Max(0, drop.Chance * building.Multiplier));
                if (probability > 0 && !double.IsNaN(probability) && !string.IsNullOrEmpty(drop.Id) && snapshot.Items.ContainsKey(drop.Id))
                    index.AddFixed(drop, building, probability, UeiAcquisitionRouteKind.FixedDrop);
            }
            foreach (UeiAcquisitionDrop drop in building.Always)
            {
                if (!string.IsNullOrEmpty(drop.Id) && snapshot.Items.ContainsKey(drop.Id))
                    index.AddFixed(drop, building, 1, UeiAcquisitionRouteKind.AlwaysDrop);
            }
            index.AddPool(building, snapshot);
        }

        HashSet<string> tradeCandidates = new(StringComparer.OrdinalIgnoreCase);
        foreach (string category in TraderCategories)
        {
            if (!snapshot.Pools.TryGetValue(category, out string[]? pool)) continue;
            foreach (string id in pool)
            {
                if (!string.IsNullOrEmpty(id) && snapshot.Items.TryGetValue(id, out UeiAcquisitionItem? item)
                    && item.Value > 0 && !item.Untradeable) tradeCandidates.Add(id);
            }
        }
        foreach (string id in tradeCandidates) index.Add(UeiEntryKind.Item, id, new(UeiAcquisitionRouteKind.Trade));
        foreach (string id in WeaponTraderItems)
        {
            // This extra inventory path bypasses the general trader's value and
            // untradeable filter in the shipped code; do not impose that filter.
            if (snapshot.Items.ContainsKey(id)) index.Add(UeiEntryKind.Item, id, new(UeiAcquisitionRouteKind.WeaponTrade));
        }

        HashSet<string> liquids = new(snapshot.Liquids, StringComparer.OrdinalIgnoreCase);
        foreach (UeiAcquisitionItem container in snapshot.Items.Values)
        {
            Dictionary<string, double> amounts = new(StringComparer.OrdinalIgnoreCase);
            foreach (UeiAcquisitionLiquidContent content in container.Contents)
            {
                if (!string.IsNullOrEmpty(content.Id) && liquids.Contains(content.Id) && content.Amount > 0
                    && !double.IsInfinity(content.Amount))
                    amounts[content.Id] = (amounts.TryGetValue(content.Id, out double previous) ? previous : 0) + content.Amount;
            }
            foreach (KeyValuePair<string, double> amount in amounts)
                index.Add(UeiEntryKind.Liquid, amount.Key, new(UeiAcquisitionRouteKind.FilledContainer, container: container, amount: amount.Value));
        }
        foreach (List<UeiAcquisitionRoute> routes in index._routes.Values)
            routes.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(SortKey(a), SortKey(b)));
        foreach (KeyValuePair<string, List<UeiAcquisitionRoute>> routes in index._routes)
            index._views.Add(routes.Key, routes.Value.AsReadOnly());
        return index;
    }

    private static string SortKey(UeiAcquisitionRoute route)
        => ((int)route.Kind).ToString(CultureInfo.InvariantCulture) + ":" + (route.Building?.Key ?? route.Container?.Id ?? string.Empty);

    private void AddFixed(UeiAcquisitionDrop drop, UeiAcquisitionBuilding building, double probability, UeiAcquisitionRouteKind kind)
    {
        List<UeiAcquisitionRoute> routes = Bucket(UeiEntryKind.Item, drop.Id);
        for (int i = 0; i < routes.Count; i++)
        {
            UeiAcquisitionRoute route = routes[i];
            if (route.Kind == kind && ReferenceEquals(route.Building, building) && route.ProbabilityPerDraw == probability
                && route.ConditionMin == drop.ConditionMin && route.ConditionMax == drop.ConditionMax)
            {
                routes[i] = new(kind, building, probability: probability, draws: route.Draws + 1,
                    conditionMin: drop.ConditionMin, conditionMax: drop.ConditionMax);
                return;
            }
        }
        routes.Add(new(kind, building, probability: probability, conditionMin: drop.ConditionMin, conditionMax: drop.ConditionMax));
    }

    private void AddPool(UeiAcquisitionBuilding building, UeiAcquisitionSnapshot snapshot)
    {
        if (building.RandomDraws <= 0 || building.Categories.Count == 0) return;
        Dictionary<string, double> perDraw = new(StringComparer.OrdinalIgnoreCase);
        bool complete = true;
        foreach (string category in building.Categories)
        {
            if (string.IsNullOrEmpty(category) || !snapshot.Pools.TryGetValue(category, out string[]? pool) || pool.Length == 0)
            {
                complete = false;
                continue;
            }
            double weight = 1d / building.Categories.Count / pool.Length;
            foreach (string id in pool)
            {
                if (string.IsNullOrEmpty(id) || !snapshot.Items.ContainsKey(id)) { complete = false; continue; }
                perDraw[id] = (perDraw.TryGetValue(id, out double prior) ? prior : 0) + weight;
            }
        }
        foreach (KeyValuePair<string, double> entry in perDraw)
        {
            double probability = Math.Min(1, entry.Value);
            Add(UeiEntryKind.Item, entry.Key, new(UeiAcquisitionRouteKind.RandomPool, building,
                probability: probability, draws: building.RandomDraws, probabilityKnown: complete));
        }
    }

    private void Add(UeiEntryKind kind, string id, UeiAcquisitionRoute route) => Bucket(kind, id).Add(route);
    private List<UeiAcquisitionRoute> Bucket(UeiEntryKind kind, string id)
    {
        string key = UeiCatalog.EntryKey(kind, id);
        if (!_routes.TryGetValue(key, out List<UeiAcquisitionRoute>? routes))
        {
            routes = new();
            _routes.Add(key, routes);
        }
        return routes;
    }
}

internal sealed class UeiAcquisitionIndexCache
{
    private bool _built;
    private int _sourceRevision;
    private ulong _registryStamp;
    public UeiAcquisitionIndex Current { get; private set; } = UeiAcquisitionIndex.Empty;
    public bool Ensure(int sourceRevision, ulong registryStamp, Func<UeiAcquisitionSnapshot> readSnapshot)
    {
        if (_built && _sourceRevision == sourceRevision && _registryStamp == registryStamp) return false;
        UeiAcquisitionIndex next = UeiAcquisitionIndex.Build(readSnapshot());
        Current = next;
        _sourceRevision = sourceRevision;
        _registryStamp = registryStamp;
        _built = true;
        return true;
    }
}

internal static class UeiAcquisitionText
{
    public static string Compose(UeiAcquisitionIndex index, UeiEntryKind kind, string id, string annotation, UeiAcquisitionLoadState state)
        => Compose(index, kind, id, annotation, state, out _);

    public static string Compose(UeiAcquisitionIndex index, UeiEntryKind kind, string id, string annotation,
        UeiAcquisitionLoadState state, out string supplement)
    {
        supplement = UeiAcquisitionSupplement.Filter(index, kind, id, annotation);
        StringBuilder text = new();
        foreach (UeiAcquisitionRoute route in index.Get(kind, id))
        {
            Append(text, Format(route));
            if (route.Kind == UeiAcquisitionRouteKind.FilledContainer && route.Container != null)
            {
                foreach (UeiAcquisitionRoute containerRoute in index.Get(UeiEntryKind.Item, route.Container.Id))
                    Append(text, "  → " + Format(containerRoute));
            }
        }
        if (!string.IsNullOrWhiteSpace(supplement))
        {
            Append(text, UeiI18n.T("acquisition.supplement"));
            Append(text, supplement);
        }
        if (state == UeiAcquisitionLoadState.Loading) Append(text, UeiI18n.T("acquisition.loading"));
        else if (state == UeiAcquisitionLoadState.Partial || state == UeiAcquisitionLoadState.Unavailable)
            Append(text, UeiI18n.T("acquisition.incomplete"));
        return text.ToString();
    }

    public static string Format(UeiAcquisitionRoute route)
    {
        if (route.Kind == UeiAcquisitionRouteKind.Trade) return UeiI18n.T("acquisition.trade");
        if (route.Kind == UeiAcquisitionRouteKind.WeaponTrade) return UeiI18n.T("acquisition.weaponTrade");
        if (route.Kind == UeiAcquisitionRouteKind.FilledContainer && route.Container != null)
            return UeiI18n.Format("acquisition.container", UeiI18n.ItemText(route.Container.Id, route.Container.Name), Number(route.Amount));
        UeiAcquisitionBuilding? building = route.Building;
        if (building == null) return string.Empty;
        string name = UeiI18n.BuildingText(building.Id, building.Name);
        if (!string.IsNullOrWhiteSpace(building.ContextId))
            name = UeiI18n.Format("acquisition.context", name, UeiI18n.BuildingText(building.ContextId, building.ContextName));
        string trigger = UeiI18n.T(building.Animal ? "acquisition.defeat" : "acquisition.break");
        string condition = Condition(route);
        if (route.Kind == UeiAcquisitionRouteKind.AlwaysDrop)
            return UeiI18n.Format("acquisition.always", trigger, name, route.Draws, condition);
        if (route.Kind == UeiAcquisitionRouteKind.FixedDrop)
            return UeiI18n.Format("acquisition.fixed", trigger, name, Percent(route.ProbabilityPerDraw), route.Draws, condition);
        return route.ProbabilityKnown
            ? UeiI18n.Format("acquisition.pool", trigger, name, route.Draws, Percent(route.AtLeastOnce), condition)
            : UeiI18n.Format("acquisition.poolUnknown", trigger, name, route.Draws);
    }

    public static string Condition(UeiAcquisitionRoute route)
        => double.IsNaN(route.ConditionMin) || double.IsNaN(route.ConditionMax)
            || double.IsInfinity(route.ConditionMin) || double.IsInfinity(route.ConditionMax)
            ? UeiI18n.T("acquisition.conditionUnknown")
            : route.ConditionMin == route.ConditionMax ? UeiI18n.Format("acquisition.conditionSingle", Percent(route.ConditionMin))
            : UeiI18n.Format("acquisition.condition", Percent(route.ConditionMin), Percent(route.ConditionMax));

    public static string Number(double number) => number.ToString("0.##", CultureInfo.InvariantCulture);
    public static string Percent(double fraction) => fraction > 0 && fraction < 0.0001 ? "<0.01%" : Number(fraction * 100) + "%";
    private static void Append(StringBuilder text, string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;
        if (text.Length > 0) text.Append('\n');
        text.Append(line);
    }
}
