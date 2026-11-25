using UnityEngine;

// Controllable game boundaries, not Unity physics or network simulation.
public sealed class Skills { public int INT = 10; }
public sealed class Body : MonoBehaviour
{
    public bool alive = true;
    public Vector2 moveDir;
    public bool crouching;
    public bool endedJump;
    public Skills skills = new();
    public readonly List<Item> Inventory = [];
    public Action<Item>? Pickup;
    public void AutoPickUpItem(Item item)
    {
        if (Pickup != null) Pickup(item);
        else Inventory.Add(item);
    }
    public List<Item> GetAllItemsThorough() => Inventory;
}

public sealed class CraftingQuality
{
    public string id = string.Empty;
    public float amount;
    public string LocaleName => id;
}
public sealed class RecipeItem
{
    public bool isLiquid;
    public bool specific;
    public string specificId = string.Empty;
    public CraftingQuality? quality;
}
public sealed class RecipeResult
{
    public string id = string.Empty;
    public bool isLiquid;
}
public sealed class Recipe
{
    public bool visible = true;
    public bool hasMadeBefore;
    public bool specialKnown;
    public RecipeResult? result;
    public List<RecipeItem>? items;
}
public static class Recipes { public static List<Recipe> recipes = []; }

public sealed class WaterContainerItem : MonoBehaviour
{
    public float Capacity = 1000;
    public List<LiquidStack> stack = [];
    public void DrainAll() => stack.Clear();
    public float AddLiquid(string id, float amount)
    {
        float added = Math.Min(Capacity - stack.Sum(x => x.amount), amount);
        if (added > 0) stack.Add(new(id, added));
        return added;
    }
}
public struct RangeF(float min, float max)
{
    public float min = min;
    public float max = max;
}
public sealed class TraderScript : MonoBehaviour
{
    public RangeF MoveRange;
    public bool farEnoughToMove;
    public int character;
    public SpriteRenderer? headRender;
    public SpriteRenderer? eyes;
    public Sprite[]? headSprites;
    public Sprite[]? eyeSprites;
}
public static class Utils
{
    public static Func<string, Vector2, float, GameObject?> CreateHandler = (_, _, _) => null;
    public static GameObject? Create(string id, Vector2 position, float rotation) => CreateHandler(id, position, rotation);
}
public sealed class Command(string name, string description, Command.Action action,
    Dictionary<int, List<string>>? autofill, params (string, string)[] args)
{
    public delegate void Action(string[] args);
    public string name = name;
    public string description = description;
    public Dictionary<int, List<string>>? argAutofill = autofill;
    public (string, string)[] argDescription = args;
    public Action action = action;
}
public sealed class ConsoleScript
{
    public bool active;
    public static ConsoleScript instance = new();
    public static List<Command> Commands = [];
    public readonly List<string> Logs = [];
    public static Command? SearchExact(string name) => Commands.FirstOrDefault(command => command.name == name);
    public void LogToConsole(string text) => Logs.Add(text);
}
