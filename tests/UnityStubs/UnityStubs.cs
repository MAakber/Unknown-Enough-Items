namespace UnityEngine;

// Test doubles for reflection and translation tests; no Unity runtime behavior.
public sealed class Sprite { }
public sealed class SpriteRenderer : MonoBehaviour
{
    public Sprite? sprite;
    public Material? sharedMaterial;
    public Color color = Color.white;
    public bool flipX, flipY;
    public SpriteDrawMode drawMode;
    public Vector2 size;
    public SpriteTileMode tileMode;
    public float adaptiveModeThreshold;
    public SpriteSortPoint spriteSortPoint;
    public SpriteMaskInteraction maskInteraction;
    public int sortingLayerID, sortingOrder;
}
public sealed class Transform(GameObject owner)
{
    public GameObject gameObject => owner;
    public Vector3 position;
    public Vector3 localPosition;
    public Quaternion localRotation = Quaternion.identity;
    public Vector3 localScale = Vector3.one;
    public Transform? parent;
    public T? GetComponent<T>() where T : class => owner.GetComponent<T>();
    public void SetParent(Transform? value, bool worldPositionStays = true)
    {
        parent?.gameObject.Children.Remove(owner);
        parent = value;
        if (value != null && !value.gameObject.Children.Contains(owner)) value.gameObject.Children.Add(owner);
    }
}
public class MonoBehaviour
{
    public bool enabled = true;
    public GameObject gameObject = null!;
    public Transform transform => gameObject.transform;
    public T? GetComponent<T>() where T : class => gameObject.GetComponent<T>();
}
public struct Vector2(float x, float y)
{
    public static Vector2 zero => new(0f, 0f);
    public float x = x;
    public float y = y;
}
public struct Vector3(float x, float y, float z)
{
    public static Vector3 zero => new(0, 0, 0);
    public static Vector3 one => new(1, 1, 1);
    public static implicit operator Vector2(Vector3 v) => new(v.x, v.y);
    public static implicit operator Vector3(Vector2 v) => new(v.x, v.y, 0);
    public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
    public float x = x;
    public float y = y;
    public float z = z;
}
public struct Quaternion
{
    public static Quaternion identity => new();
    public float z;
    public static Quaternion Euler(float x, float y, float z) => new() { z = z };
}
public sealed class GameObject
{
    public static readonly List<GameObject> Created = [];
    public GameObject(string name)
    {
        this.name = name;
        transform = new(this);
        Children = new(this);
        Created.Add(this);
    }
    public bool activeSelf = true;
    public bool activeInHierarchy => activeSelf;
    public void SetActive(bool value) => activeSelf = value;
    public string name;
    public int layer;
    public readonly Transform transform;
    public readonly List<object> Components = new();
    public readonly ChildObjects Children;
    public Vector2 Position;
    public Vector3 WorldPosition;
    public float Rotation;
    public float? Condition;
    public void Attach(MonoBehaviour component) { component.gameObject = this; Components.Add(component); }
    public T AddComponent<T>() where T : MonoBehaviour, new() { T component = new(); Attach(component); return component; }
    public T? GetComponent<T>() where T : class => Components.OfType<T>().FirstOrDefault();
    public T? GetComponentInChildren<T>(bool includeInactive) where T : class
        => GetComponent<T>() ?? Children.Select(child => child.GetComponentInChildren<T>(includeInactive)).FirstOrDefault(value => value != null);
    public void GetComponentsInChildren<T>(bool includeInactive, List<T> target) where T : class
    {
        target.AddRange(Components.OfType<T>());
        foreach (GameObject child in Children) child.GetComponentsInChildren(includeInactive, target);
    }
}
public sealed class ChildObjects(GameObject owner) : List<GameObject>
{
    public new void Add(GameObject child) { child.transform.parent = owner.transform; base.Add(child); }
}
// Controllable API-boundary doubles for scheduler tests only. They do not
// reproduce Unity asset deserialization, object lifetime or frame timings.
public sealed class ResourceRequest
{
    public bool isDone;
    private object? _asset;
    public int AssetReads;
    public object? asset { get { AssetReads++; return _asset; } set { _asset = value; } }
}
public static class Resources
{
    public static Func<string, ResourceRequest> LoadHandler = _ => throw new InvalidOperationException("No resource test handler");
    public static ResourceRequest LoadAsync<T>(string path) => LoadHandler(path);
    public static Func<string, object?> SyncLoadHandler = _ => null;
    public static T? Load<T>(string path) where T : class => SyncLoadHandler(path) as T;
}
public enum SystemLanguage { English, Chinese, ChineseSimplified, ChineseTraditional }
public static class Application
{
    public static bool isFocused = true;
    public static SystemLanguage systemLanguage = SystemLanguage.English;
    public static string dataPath = string.Empty;
}
public static class PlayerPrefs { public static string GetString(string key) => string.Empty; }

public enum KeyCode
{
    None = 0, Tab = 9, Escape = 27, Space = 32, A = 97, D = 100, Q = 113, W = 119,
    F1 = 282, F15 = 296, Mouse0 = 323, Mouse1 = 324, Mouse2 = 325,
    Mouse3 = 326, Mouse4 = 327, Mouse5 = 328, Mouse6 = 329,
}
public static class Time { public static int frameCount; public static float unscaledTime; }
public static class Input
{
    public static Vector3 mousePosition = new(128, 128, 0);
    private static readonly HashSet<KeyCode> Held = new();
    private static readonly HashSet<KeyCode> Down = new();
    private static readonly HashSet<KeyCode> Up = new();
    public static bool GetKey(KeyCode key) => Held.Contains(key);
    public static bool GetKeyDown(KeyCode key) => Down.Contains(key);
    public static bool GetKeyUp(KeyCode key) => Up.Contains(key);
    public static void SetFrame(int frame, KeyCode[]? held = null, KeyCode[]? down = null, KeyCode[]? up = null)
    {
        Time.frameCount = frame;
        Held.Clear(); Down.Clear(); Up.Clear();
        if (held != null) Held.UnionWith(held);
        if (down != null) Down.UnionWith(down);
        if (up != null) Up.UnionWith(up);
    }
}
