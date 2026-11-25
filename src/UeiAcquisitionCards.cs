using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

// Presentation uses the typed routes, never a parse of localized source prose.
internal sealed class UeiAcquisitionCard
{
    private readonly List<UeiAcquisitionRoute> _routes = new();
    public UeiAcquisitionRoute Route { get; private set; }
    public IReadOnlyList<UeiAcquisitionRoute> Routes { get; }
    public readonly UeiAcquisitionItem? ViaContainer;
    public readonly double LiquidAmount;
    public readonly string Key;

    private UeiAcquisitionCard(string key, UeiAcquisitionRoute route, UeiAcquisitionItem? viaContainer, double liquidAmount)
    {
        Route = route;
        _routes.Add(route);
        Routes = _routes.AsReadOnly();
        ViaContainer = viaContainer;
        LiquidAmount = liquidAmount;
        Key = key;
    }

    public string Title
    {
        get
        {
            if (Route.Container != null) return UeiI18n.ItemText(Route.Container.Id, Route.Container.Name);
            UeiAcquisitionBuilding? building = Route.Building;
            if (building == null) return UeiI18n.T("acquisition.card.trader");
            string name = UeiI18n.BuildingText(building.Id, building.Name);
            return Routes.Count > 1 || string.IsNullOrWhiteSpace(building.ContextId) ? name
                : UeiI18n.Format("acquisition.context", name, UeiI18n.BuildingText(building.ContextId, building.ContextName));
        }
    }

    public string Method
    {
        get
        {
            string kind = Route.Kind switch
            {
                UeiAcquisitionRouteKind.FixedDrop => "independent",
                UeiAcquisitionRouteKind.AlwaysDrop => "guaranteed",
                UeiAcquisitionRouteKind.RandomPool => "randomPool",
                UeiAcquisitionRouteKind.FilledContainer => "initialContents",
                _ => "trade",
            };
            string label = UeiI18n.T("acquisition.card." + kind);
            return Route.Building == null ? label
                : UeiI18n.Format("acquisition.card.method", UeiI18n.T(Route.Building.Animal ? "acquisition.defeat" : "acquisition.break"), label);
        }
    }

    public string Facts
    {
        get
        {
            if (Route.Kind == UeiAcquisitionRouteKind.Trade) return UeiI18n.T("acquisition.card.randomStock");
            if (Route.Kind == UeiAcquisitionRouteKind.WeaponTrade) return UeiI18n.T("acquisition.card.weaponStock");
            if (Route.Kind == UeiAcquisitionRouteKind.FilledContainer) return UeiI18n.T("acquisition.card.amountVaries");
            string rolls = Route.Kind switch
            {
                UeiAcquisitionRouteKind.AlwaysDrop => UeiI18n.Format("acquisition.card.guaranteedCount", Route.Draws),
                UeiAcquisitionRouteKind.FixedDrop => UeiI18n.Format("acquisition.card.fixedRolls", Route.Draws, UeiAcquisitionText.Percent(Route.ProbabilityPerDraw)),
                _ => Route.ProbabilityKnown
                    ? UeiI18n.Format("acquisition.card.poolRolls", Route.Draws, UeiAcquisitionText.Percent(Route.AtLeastOnce))
                    : UeiI18n.Format("acquisition.card.poolUnknown", Route.Draws),
            };
            return rolls + "\n" + UeiAcquisitionText.Condition(Route);
        }
    }

    public string ResultBadge => ViaContainer != null || Route.Kind == UeiAcquisitionRouteKind.FilledContainer
        ? UeiI18n.Format("acquisition.card.defaultAmount", UeiAcquisitionText.Number(LiquidAmount))
        : Route.Kind == UeiAcquisitionRouteKind.AlwaysDrop ? "×" + Route.Draws.ToString(CultureInfo.InvariantCulture) : string.Empty;

    public string Tooltip
    {
        get
        {
            StringBuilder text = new(UeiAcquisitionText.Format(Route));
            if (Routes.Count > 1)
            {
                text.Append('\n').Append(UeiI18n.T("acquisition.card.mergedSources"));
                HashSet<string> labels = new(StringComparer.Ordinal);
                foreach (UeiAcquisitionRoute route in Routes)
                {
                    UeiAcquisitionBuilding? building = route.Building;
                    if (building == null) continue;
                    string name = UeiI18n.BuildingText(building.Id, building.Name);
                    string label = string.IsNullOrWhiteSpace(building.ContextId)
                        ? UeiI18n.Format("acquisition.card.standalone", name)
                        : UeiI18n.Format("acquisition.context", name, UeiI18n.BuildingText(building.ContextId, building.ContextName));
                    if (labels.Add(label)) text.Append("\n• ").Append(label);
                }
            }
            if (ViaContainer != null)
                text.Append('\n').Append(UeiI18n.Format("acquisition.container", UeiI18n.ItemText(ViaContainer.Id, ViaContainer.Name), UeiAcquisitionText.Number(LiquidAmount)));
            return text.ToString();
        }
    }

    public string SourceFallback => UeiI18n.T(Route.Container != null ? "acquisition.card.containerIcon"
        : Route.Building != null ? Route.Building.Animal ? "acquisition.card.entityIcon" : "acquisition.card.buildingIcon"
        : "acquisition.card.tradeIcon");

    public static List<UeiAcquisitionCard> Build(UeiAcquisitionIndex index, UeiEntryKind kind, string id)
    {
        List<UeiAcquisitionCard> cards = new();
        Dictionary<string, UeiAcquisitionCard> groups = new(StringComparer.Ordinal);
        foreach (UeiAcquisitionRoute route in index.Get(kind, id))
        {
            if (route.Kind == UeiAcquisitionRouteKind.FilledContainer && route.Container != null)
            {
                IReadOnlyList<UeiAcquisitionRoute> sources = index.Get(UeiEntryKind.Item, route.Container.Id);
                if (sources.Count > 0)
                {
                    foreach (UeiAcquisitionRoute source in sources)
                        AddCard(groups, cards, source, route.Container, route.Amount);
                    continue;
                }
            }
            AddCard(groups, cards, route);
        }
        // A newly discovered prefab variant must not reorder an existing group.
        cards.Sort((a, b) =>
        {
            int kindOrder = ((int)a.Route.Kind).CompareTo((int)b.Route.Kind);
            if (kindOrder != 0) return kindOrder;
            int sourceOrder = StringComparer.OrdinalIgnoreCase.Compare(
                a.Route.Building?.Id ?? a.Route.Container?.Id ?? string.Empty,
                b.Route.Building?.Id ?? b.Route.Container?.Id ?? string.Empty);
            return sourceOrder != 0 ? sourceOrder : StringComparer.Ordinal.Compare(a.Key, b.Key);
        });
        return cards;
    }

    private static void AddCard(Dictionary<string, UeiAcquisitionCard> groups, List<UeiAcquisitionCard> cards,
        UeiAcquisitionRoute route, UeiAcquisitionItem? viaContainer = null, double liquidAmount = 0)
    {
        if (route.Kind == UeiAcquisitionRouteKind.FilledContainer) liquidAmount = route.Amount;
        string key = CreateKey(route, viaContainer, liquidAmount, out bool canMerge);
        UeiAcquisitionCard? card;
        if (canMerge && groups.TryGetValue(key, out card))
        {
            card._routes.Add(route);
            // Prefer the standalone source for the title; every variant remains available for icon lookup and tooltips.
            if (card.Route.Building != null && !string.IsNullOrWhiteSpace(card.Route.Building.ContextId)
                && route.Building != null && string.IsNullOrWhiteSpace(route.Building.ContextId))
                card.Route = route;
            return;
        }
        if (!canMerge)
        {
            string originalKey = key;
            int record = 0;
            while (groups.ContainsKey(key))
                key = originalKey + ":record:" + (++record).ToString(CultureInfo.InvariantCulture);
        }
        card = new UeiAcquisitionCard(key, route, viaContainer, liquidAmount);
        groups.Add(key, card);
        cards.Add(card);
    }

    private static string CreateKey(UeiAcquisitionRoute route, UeiAcquisitionItem? viaContainer, double liquidAmount, out bool canMerge)
    {
        UeiAcquisitionBuilding? building = route.Building;
        bool knownRules = route.ProbabilityKnown && Finite(route.ProbabilityPerDraw)
            && Finite(route.ConditionMin) && Finite(route.ConditionMax) && Finite(route.Amount) && Finite(liquidAmount);
        canMerge = knownRules && (building == null || !string.IsNullOrWhiteSpace(building.Id))
            && (route.Container == null || !string.IsNullOrWhiteSpace(route.Container.Id))
            && (viaContainer == null || !string.IsNullOrWhiteSpace(viaContainer.Id));
        string source = building == null ? string.Empty
            : canMerge ? "id:" + NormalizeId(building.Id)
            : "prefab:" + building.Key;
        // Compare full precision, not the rounded text. Context and prefab paths do not split known, identical rules for the same ID.
        return route.Kind + ":" + KeyPart(source) + ":" + (building?.Animal == true ? "1" : "0")
            + ":" + KeyPart(NormalizeId(route.Container?.Id))
            + ":" + (route.ProbabilityKnown ? "1" : "0")
            + ":" + route.ProbabilityPerDraw.ToString("R", CultureInfo.InvariantCulture)
            + ":" + route.Draws.ToString(CultureInfo.InvariantCulture)
            + ":" + route.ConditionMin.ToString("R", CultureInfo.InvariantCulture)
            + ":" + route.ConditionMax.ToString("R", CultureInfo.InvariantCulture)
            + ":" + route.Amount.ToString("R", CultureInfo.InvariantCulture)
            + ":" + KeyPart(NormalizeId(viaContainer?.Id))
            + ":" + liquidAmount.ToString("R", CultureInfo.InvariantCulture);
    }

    private static string NormalizeId(string? id) => (id ?? string.Empty).ToLowerInvariant();
    private static string KeyPart(string value) => value.Length.ToString(CultureInfo.InvariantCulture) + ":" + value;
    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
