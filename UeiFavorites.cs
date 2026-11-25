using System;
using System.Collections.Generic;
using System.Linq;

internal static class UeiFavorites
{
    private static readonly HashSet<string> Favorites = new(StringComparer.OrdinalIgnoreCase);

    public static int Count => Favorites.Count;
    public static int Revision { get; private set; }

    public static void Load(string serialized)
    {
        Favorites.Clear();
        Revision++;
        if (string.IsNullOrWhiteSpace(serialized)) return;

        foreach (string raw in serialized.Split('|'))
        {
            string key = raw.Trim();
            if (!string.IsNullOrEmpty(key)) Favorites.Add(key);
        }
    }

    public static bool Contains(string entryKey) => Favorites.Contains(entryKey);

    public static void Toggle(string entryKey)
    {
        if (string.IsNullOrWhiteSpace(entryKey)) return;
        if (!Favorites.Remove(entryKey)) Favorites.Add(entryKey);
        Revision++;
        UeiPlugin.SaveFavorites();
    }

    public static void ClearAll()
    {
        if (Favorites.Count == 0) return;
        Favorites.Clear();
        Revision++;
        UeiPlugin.SaveFavorites();
    }

    public static string Serialize() => string.Join("|", Favorites.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
}
