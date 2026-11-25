public class ItemInfo
{
    public string fullName = string.Empty;
    public string description = string.Empty;
    public string category = string.Empty;
    public int value;
    public bool untradeable;
    public float weight;
    public string tags = string.Empty;
    public List<CraftingQuality> qualities = [];
    public string[] GetTags() => tags.Split(',');
}

public class LiquidItemInfo : ItemInfo
{
    public float capacity;
    public List<LiquidStack>? defaultContents;
}
public sealed class LiquidStack(string id, float amount)
{
    public string liquidId = id;
    public float amount = amount;
}
public class Item : UnityEngine.MonoBehaviour
{
    public float condition;
    public static Dictionary<string, ItemInfo> GlobalItems = new(StringComparer.Ordinal);
    public static ItemInfo GetItem(string id) => GlobalItems[id];
}
public static class ItemLootPool
{
    public static Dictionary<string, List<string>>? pool;
}
public sealed class LiquidType
{
    public string localeName = string.Empty;
    public UnityEngine.Color color = UnityEngine.Color.white;
    public bool injectable;
    public bool healthUsable;
    public List<CraftingQuality> qualities = [];
}
public static class Liquids
{
    public static Dictionary<string, LiquidType> Registry = new(StringComparer.Ordinal);
    public static List<string> DangerList = [];
    public static bool LiquidExists(string id) => Registry.ContainsKey(id);
}
public sealed class ItemDrop
{
    public string id = string.Empty;
    public float chance;
    public float conditionMin = 1;
    public float conditionMax = 1;
}
public sealed class BuildingEntity : UnityEngine.MonoBehaviour
{
    public ItemDrop[] itemsDropOnDestroy = [];
    public ItemDrop[] alwaysDrop = [];
    public string[] itemCategoriesToAdd = [];
    public float dropChanceMultiplier = 1;
    public int guaranteedDropAmount;
    public string id = string.Empty;
    public string fullName = string.Empty;
    public string description = string.Empty;
    public bool animal;
    public bool indestructible;
}
