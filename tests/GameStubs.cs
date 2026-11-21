internal enum UeiEntryKind { Item, Liquid }
internal sealed class UeiEntry { }
internal static class UeiCatalog
{
    public const string CategoryAll = "All";
    public const string CategoryFavorites = "Favorites";
    public const string CategoryLiquid = "Liquid";
    public static string EntryKey(UeiEntryKind kind, string id) => (kind == UeiEntryKind.Item ? "item:" : "liquid:") + id;
}
internal sealed class Setting<T>(T value) { public T Value = value; }
internal static class UeiPlugin
{
    public static Setting<string> LanguageMode = new("en");
    public static void LogWarning(string message) { }
}
internal static class Locale
{
    public static string currentLangName = "en";
    public static readonly Dictionary<string, string> Items = new();
    public static readonly Dictionary<string, string> Other = new();
    public static string GetItem(string key) => Items.GetValueOrDefault(key, key);
    public static string GetOther(string key) => Other.GetValueOrDefault(key, key);
}
