using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

internal static class UeiFavorites
{
    private static readonly HashSet<string> Favorites = new(StringComparer.OrdinalIgnoreCase);

    public static void Load(string serialized)
    {
        Favorites.Clear();
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return;
        }

        foreach (string raw in serialized.Split('|'))
        {
            string key = raw.Trim();
            if (!string.IsNullOrEmpty(key))
            {
                Favorites.Add(key);
            }
        }
    }

    public static bool Contains(string entryKey)
    {
        return Favorites.Contains(entryKey);
    }

    public static int Count => Favorites.Count;

    public static void Toggle(string entryKey)
    {
        if (string.IsNullOrWhiteSpace(entryKey))
        {
            return;
        }

        if (!Favorites.Remove(entryKey))
        {
            Favorites.Add(entryKey);
        }

        UeiPlugin.SaveFavorites();
    }

    public static void ClearAll()
    {
        if (Favorites.Count == 0)
        {
            return;
        }

        Favorites.Clear();
        UeiPlugin.SaveFavorites();
    }

    public static string Serialize()
    {
        return string.Join("|", Favorites.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
    }
}

internal static class UeiCatalog
{
    public const string CategoryAll = "All";
    public const string CategoryFavorites = "Favorites";
    public const string CategoryLiquid = "Liquid";

    private static readonly List<UeiEntry> EntriesBacking = new();
    private static readonly List<string> CategoryKeysBacking = new();
    private static readonly Dictionary<string, UeiEntry> EntriesByKey = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, List<UeiRecipeLink>> ProducedByLinks = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, List<UeiRecipeLink>> UsedInLinks = new(StringComparer.OrdinalIgnoreCase);

    private static int _itemCount = -1;
    private static int _liquidCount = -1;
    private static int _recipeCount = -1;
    private static int _playerInt = -1;
    private static int _madeRecipeCount = -1;
    private static int _specialKnownCount = -1;
    private static string _localeName = string.Empty;
    private static int _revision;
    private static float _lastRecipeFlagCheck = -999f;

    public static IReadOnlyList<UeiEntry> Entries => EntriesBacking;
    public static IReadOnlyList<string> CategoryKeys => CategoryKeysBacking;
    public static int Revision => _revision;

    public static void ForceRebuild()
    {
        _itemCount = -1;
        _liquidCount = -1;
        _recipeCount = -1;
        _playerInt = -1;
        _madeRecipeCount = -1;
        _specialKnownCount = -1;
        _localeName = string.Empty;
        _lastRecipeFlagCheck = -999f;
        _revision++;
    }

    public static bool EnsureBuilt()
    {
        if (!IsRuntimeDataReady())
        {
            return false;
        }

        int itemCount = Item.GlobalItems.Count;
        int liquidCount = Liquids.Registry.Count;
        int recipeCount = Recipes.recipes.Count;
        int playerInt = PlayerCamera.main.body.skills.INT;
        string localeName = SafeString(() => Locale.currentLangName, string.Empty);
        bool countsUnchanged = itemCount == _itemCount
            && liquidCount == _liquidCount
            && recipeCount == _recipeCount
            && playerInt == _playerInt
            && string.Equals(localeName, _localeName, StringComparison.Ordinal)
            && EntriesBacking.Count > 0;
        if (countsUnchanged && Time.unscaledTime - _lastRecipeFlagCheck < 1f)
        {
            return true;
        }

        int madeRecipeCount = 0;
        int specialKnownCount = 0;
        foreach (Recipe recipe in Recipes.recipes)
        {
            if (recipe.hasMadeBefore) madeRecipeCount++;
            if (recipe.specialKnown) specialKnownCount++;
        }
        _lastRecipeFlagCheck = Time.unscaledTime;
        if (countsUnchanged
            && madeRecipeCount == _madeRecipeCount
            && specialKnownCount == _specialKnownCount)
        {
            return true;
        }

        Build(itemCount, liquidCount, recipeCount, playerInt, madeRecipeCount, specialKnownCount, localeName);
        return true;
    }

    public static string EntryKey(UeiEntryKind kind, string id)
    {
        return (kind == UeiEntryKind.Item ? "item:" : "liquid:") + id;
    }

    public static bool TryGetEntry(UeiEntryKind kind, string id, out UeiEntry? entry)
    {
        string key = EntryKey(kind, id);
        if (EntriesByKey.TryGetValue(key, out entry))
        {
            return true;
        }

        string baseId = RshLibHelper.GetBaseId(id);
        if (baseId != id)
        {
            return EntriesByKey.TryGetValue(EntryKey(kind, baseId), out entry);
        }

        entry = null;
        return false;
    }

    public static List<UeiRecipeLink> GetProducedBy(UeiEntry entry)
    {
        return ProducedByLinks.TryGetValue(entry.EntryKey, out List<UeiRecipeLink>? links)
            ? links
            : new List<UeiRecipeLink>();
    }

    public static List<UeiRecipeLink> GetUsedIn(UeiEntry entry)
    {
        return UsedInLinks.TryGetValue(entry.EntryKey, out List<UeiRecipeLink>? links)
            ? links
            : new List<UeiRecipeLink>();
    }

    public static string CategoryLabel(string categoryKey)
    {
        return UeiI18n.CategoryLabel(categoryKey);
    }

    public static bool IsDangerousLiquid(string id)
    {
        return Liquids.DangerList != null && Liquids.DangerList.Contains(id);
    }

    private static bool IsRuntimeDataReady()
    {
        try
        {
            return Item.GlobalItems != null
                && Item.GlobalItems.Count > 0
                && Liquids.Registry != null
                && Liquids.Registry.Count > 0
                && Recipes.recipes != null
                && Recipes.recipes.Count > 0
                && PlayerCamera.main != null
                && PlayerCamera.main.body != null;
        }
        catch
        {
            return false;
        }
    }

    private static void Build(int itemCount, int liquidCount, int recipeCount, int playerInt, int madeRecipeCount, int specialKnownCount, string localeName)
    {
        EntriesBacking.Clear();
        EntriesByKey.Clear();
        ProducedByLinks.Clear();
        UsedInLinks.Clear();

        Sprite? droplet = SafeLoadDroplet();
        foreach (KeyValuePair<string, ItemInfo> kv in Item.GlobalItems.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            UeiEntry entry = BuildItemEntry(kv.Key, kv.Value);
            EntriesBacking.Add(entry);
            EntriesByKey[entry.EntryKey] = entry;
        }

        foreach (KeyValuePair<string, LiquidType> kv in Liquids.Registry.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            UeiEntry entry = BuildLiquidEntry(kv.Key, kv.Value, droplet);
            EntriesBacking.Add(entry);
            EntriesByKey[entry.EntryKey] = entry;
        }

        BuildCategories();
        BuildRecipeLinks();

        _itemCount = itemCount;
        _liquidCount = liquidCount;
        _recipeCount = recipeCount;
        _playerInt = playerInt;
        _madeRecipeCount = madeRecipeCount;
        _specialKnownCount = specialKnownCount;
        _localeName = localeName;
        _revision++;
        UeiPlugin.LogInfo($"UEI catalog built: {EntriesBacking.Count} entries, {Recipes.recipes.Count} recipes.");
    }

    private static UeiEntry BuildItemEntry(string id, ItemInfo info)
    {
        if (CuCoreLibHelper.TryGetItemInfo(id, out ItemInfo? registeredInfo) && registeredInfo != null)
        {
            info = registeredInfo;
        }
        string name = UeiI18n.ItemText(id, string.Empty);
        if (string.IsNullOrWhiteSpace(name))
        {
            name = SafeString(() => info.fullName, id);
        }

        string desc = UeiI18n.ItemText(id + "dsc", string.Empty);
        if (string.IsNullOrWhiteSpace(desc))
        {
            desc = SafeString(() => info.description, string.Empty);
        }

        string category = string.IsNullOrWhiteSpace(info.category) ? "other" : info.category.Trim();
        UeiEntry entry = new()
        {
            Kind = UeiEntryKind.Item,
            Id = id,
            EntryKey = EntryKey(UeiEntryKind.Item, id),
            DisplayName = name,
            Description = desc,
            CategoryKey = category,
            CategoryLabel = CategoryLabel(category),
            Icon = SafeLoadItemSprite(id),
            IconColor = Color.white,
            ItemInfo = info,
        };

        AddTags(entry, info);
        AddQualities(entry, info.qualities);
        entry.SearchText = BuildSearchText(entry);
        return entry;
    }

    private static UeiEntry BuildLiquidEntry(string id, LiquidType info, Sprite? droplet)
    {
        string name = UeiI18n.LiquidText(id, string.Empty);
        if (string.IsNullOrWhiteSpace(name) || name == id)
        {
            string localeName = SafeString(() => info.localeName, id);
            name = UeiI18n.LiquidText(localeName, localeName);
        }

        UeiEntry entry = new()
        {
            Kind = UeiEntryKind.Liquid,
            Id = id,
            EntryKey = EntryKey(UeiEntryKind.Liquid, id),
            DisplayName = name,
            Description = UeiI18n.LiquidText(id + "dsc", string.Empty),
            CategoryKey = CategoryLiquid,
            CategoryLabel = CategoryLabel(CategoryLiquid),
            Icon = droplet,
            IconColor = info.color,
            LiquidInfo = info,
        };

        if (info.injectable)
        {
            entry.Tags.Add("injectable");
        }
        if (info.healthUsable)
        {
            entry.Tags.Add("health");
        }
        if (IsDangerousLiquid(id))
        {
            entry.Tags.Add("danger");
        }
        AddQualities(entry, info.qualities);
        entry.SearchText = BuildSearchText(entry);
        return entry;
    }

    private static void BuildCategories()
    {
        CategoryKeysBacking.Clear();
        CategoryKeysBacking.Add(CategoryAll);
        CategoryKeysBacking.Add(CategoryFavorites);

        foreach (string category in EntriesBacking
            .Where(x => x.Kind == UeiEntryKind.Item)
            .Select(x => x.CategoryKey)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            CategoryKeysBacking.Add(category);
        }

        CategoryKeysBacking.Add(CategoryLiquid);
    }

    private static void BuildRecipeLinks()
    {
        for (int i = 0; i < Recipes.recipes.Count; i++)
        {
            Recipe recipe = Recipes.recipes[i];
            AddProducedLink(recipe, i);
            AddUsedLinks(recipe, i);
        }
    }

    private static void AddProducedLink(Recipe recipe, int recipeIndex)
    {
        if (recipe.result == null || string.IsNullOrWhiteSpace(recipe.result.id))
        {
            return;
        }

        string fullId = recipe.result.id;
        UeiEntryKind kind = recipe.result.isLiquid ? UeiEntryKind.Liquid : UeiEntryKind.Item;
        string key = RshLibHelper.TryResolveKey(fullId, kind, EntriesByKey);
        if (!EntriesByKey.ContainsKey(key) && !EntriesByKey.ContainsKey(EntryKey(kind, fullId)))
        {
            return;
        }

        if (!EntriesByKey.ContainsKey(key))
        {
            key = EntryKey(kind, fullId);
        }

        GetOrCreate(ProducedByLinks, key).Add(NewRecipeLink(recipe, recipeIndex, UeiRecipeRelation.ProducedBy));
    }

    private static void AddUsedLinks(Recipe recipe, int recipeIndex)
    {
        if (recipe.items == null)
        {
            return;
        }

        HashSet<string> added = new(StringComparer.OrdinalIgnoreCase);
        foreach (RecipeItem item in recipe.items)
        {
            foreach (UeiEntry entry in EntriesBacking)
            {
                if (!RecipeItemUsesEntry(item, entry) || !added.Add(entry.EntryKey))
                {
                    continue;
                }

                GetOrCreate(UsedInLinks, entry.EntryKey).Add(NewRecipeLink(recipe, recipeIndex, UeiRecipeRelation.UsedIn));
            }
        }
    }

    private static bool RecipeItemUsesEntry(RecipeItem item, UeiEntry entry)
    {
        bool wantsLiquid = item.isLiquid;
        if (wantsLiquid != (entry.Kind == UeiEntryKind.Liquid))
        {
            return false;
        }

        bool specific = item.specific || !string.IsNullOrWhiteSpace(item.specificId);
        if (specific)
        {
            string specificId = item.specificId ?? string.Empty;
            if (string.Equals(specificId, entry.Id, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string specificBase = RshLibHelper.GetBaseId(specificId);
            return string.Equals(specificBase, entry.Id, StringComparison.OrdinalIgnoreCase);
        }

        if (item.quality == null || string.IsNullOrWhiteSpace(item.quality.id))
        {
            return false;
        }

        if (entry.Kind == UeiEntryKind.Liquid)
        {
            return entry.Qualities.Any(q => string.Equals(q.id, item.quality.id, StringComparison.OrdinalIgnoreCase)
                && q.amount > 0f);
        }

        return entry.Qualities.Any(q => string.Equals(q.id, item.quality.id, StringComparison.OrdinalIgnoreCase)
            && q.amount >= item.quality.amount);
    }

    private static UeiRecipeLink NewRecipeLink(Recipe recipe, int recipeIndex, UeiRecipeRelation relation)
    {
        return new UeiRecipeLink
        {
            RecipeIndex = recipeIndex,
            Relation = relation,
            Visible = SafeBool(() => recipe.visible, true),
            HasMadeBefore = recipe.hasMadeBefore,
        };
    }

    private static List<UeiRecipeLink> GetOrCreate(Dictionary<string, List<UeiRecipeLink>> map, string key)
    {
        if (!map.TryGetValue(key, out List<UeiRecipeLink>? links))
        {
            links = new List<UeiRecipeLink>();
            map[key] = links;
        }

        return links;
    }

    private static void AddTags(UeiEntry entry, ItemInfo info)
    {
        try
        {
            string[]? tags = info.GetTags();
            if (tags != null)
            {
                foreach (string tag in tags)
                {
                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        entry.Tags.Add(tag.Trim());
                    }
                }
            }
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(info.tags))
            {
                foreach (string tag in info.tags.Split(','))
                {
                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        entry.Tags.Add(tag.Trim());
                    }
                }
            }
        }
    }

    private static void AddQualities(UeiEntry entry, List<CraftingQuality>? qualities)
    {
        if (qualities == null)
        {
            return;
        }

        foreach (CraftingQuality quality in qualities)
        {
            if (quality != null && !string.IsNullOrWhiteSpace(quality.id))
            {
                entry.Qualities.Add(quality);
            }
        }
    }

    private static string BuildSearchText(UeiEntry entry)
    {
        StringBuilder sb = new();
        sb.Append(entry.Id).Append(' ');
        sb.Append(entry.EntryKey).Append(' ');
        sb.Append(entry.DisplayName).Append(' ');
        sb.Append(entry.Description).Append(' ');
        sb.Append(entry.CategoryKey).Append(' ');
        sb.Append(entry.CategoryLabel).Append(' ');
        foreach (string tag in entry.Tags)
        {
            sb.Append(tag).Append(' ');
        }
        foreach (CraftingQuality quality in entry.Qualities)
        {
            sb.Append(quality.id).Append(' ');
            sb.Append(SafeString(() => quality.LocaleName, string.Empty)).Append(' ');
        }
        return sb.ToString();
    }

    private static Sprite? SafeLoadItemSprite(string id)
    {
        Sprite? customSprite = CuCoreLibHelper.GetCustomItemSprite(id)
            ?? RshLibHelper.GetCustomItemSprite(id)
            ?? SexModHelper.GetCustomItemSprite(id);
        if (customSprite != null)
        {
            return customSprite;
        }

        try
        {
            GameObject prefab = Resources.Load<GameObject>(id);
            if (prefab == null)
            {
                return null;
            }

            SpriteRenderer renderer = prefab.GetComponent<SpriteRenderer>();
            return renderer != null ? renderer.sprite : null;
        }
        catch
        {
            return null;
        }
    }

    private static Sprite? SafeLoadDroplet()
    {
        try { return Resources.Load<Sprite>("Sprites/droplet"); }
        catch { return null; }
    }

    private static string SafeString(Func<string> getter, string fallback)
    {
        try
        {
            string value = getter();
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
        catch
        {
            return fallback;
        }
    }

    private static bool SafeBool(Func<bool> getter, bool fallback)
    {
        try { return getter(); }
        catch { return fallback; }
    }
}
