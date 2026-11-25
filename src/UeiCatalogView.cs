using System;
using System.Collections.Generic;
using System.Linq;

internal sealed class UeiCatalogView
{
    private IReadOnlyList<UeiEntry>? _source;
    private readonly List<UeiEntry> _sorted = new();
    private readonly List<UeiEntry> _filtered = new();
    private int _catalogRevision = -1;
    private int _favoritesRevision = -1;
    private string _category = string.Empty;
    private string _search = string.Empty;

    internal IReadOnlyList<UeiEntry> GetEntries(IReadOnlyList<UeiEntry> source, int catalogRevision,
        int favoritesRevision, string category, string search)
    {
        string normalizedSearch = (search ?? string.Empty).Trim();
        bool orderChanged = !ReferenceEquals(source, _source)
            || catalogRevision != _catalogRevision || favoritesRevision != _favoritesRevision;
        if (!orderChanged
            && string.Equals(category, _category, StringComparison.OrdinalIgnoreCase)
            && string.Equals(normalizedSearch, _search, StringComparison.OrdinalIgnoreCase))
        {
            return _filtered;
        }

        if (orderChanged)
        {
            _sorted.Clear();
            _sorted.AddRange(source
                .OrderByDescending(x => x.IsFavorite)
                .ThenBy(x => x.Kind)
                .ThenBy(x => x.CategoryLabel, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase));
        }

        _filtered.Clear();
        foreach (UeiEntry entry in _sorted)
        {
            if (!MatchesCategory(entry, category)) continue;
            if (normalizedSearch.Length > 0
                && entry.SearchText.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) < 0) continue;
            _filtered.Add(entry);
        }

        _source = source;
        _catalogRevision = catalogRevision;
        _favoritesRevision = favoritesRevision;
        _category = category;
        _search = normalizedSearch;
        return _filtered;
    }

    private static bool MatchesCategory(UeiEntry entry, string category)
    {
        if (string.Equals(category, UeiCatalog.CategoryAll, StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(category, UeiCatalog.CategoryFavorites, StringComparison.OrdinalIgnoreCase)) return entry.IsFavorite;
        if (string.Equals(category, UeiCatalog.CategoryLiquid, StringComparison.OrdinalIgnoreCase)) return entry.Kind == UeiEntryKind.Liquid;
        if (string.Equals(category, UeiCatalog.CategoryEntity, StringComparison.OrdinalIgnoreCase)) return entry.Kind == UeiEntryKind.Entity;
        return entry.Kind == UeiEntryKind.Item && string.Equals(entry.CategoryKey, category, StringComparison.OrdinalIgnoreCase);
    }
}
