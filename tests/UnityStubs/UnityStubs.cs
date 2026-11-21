namespace UnityEngine;

// Test doubles for reflection and translation tests; no Unity runtime behavior.
public sealed class Sprite { }
public sealed class Transform { }
public struct Vector2(float x, float y)
{
    public float x = x;
    public float y = y;
}
public struct Vector3(float x, float y, float z)
{
    public float x = x;
    public float y = y;
    public float z = z;
}
public struct Quaternion
{
    public float z;
    public static Quaternion Euler(float x, float y, float z) => new() { z = z };
}
public sealed class GameObject(string name)
{
    public string name = name;
    public Vector2 Position;
    public Vector3 WorldPosition;
    public float Rotation;
    public float? Condition;
}
public enum SystemLanguage { English, Chinese, ChineseSimplified, ChineseTraditional }
public static class Application { public static SystemLanguage systemLanguage = SystemLanguage.English; }
public static class PlayerPrefs { public static string GetString(string key) => string.Empty; }
