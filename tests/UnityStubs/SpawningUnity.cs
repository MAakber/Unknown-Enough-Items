namespace UnityEngine;

public struct Color(float r, float g, float b, float a = 1)
{
    public float r = r, g = g, b = b, a = a;
    public static Color white => new(1, 1, 1);
}
public static class Object
{
    public static readonly List<Material> DestroyedMaterials = [];
    public static void Destroy(Material material) => DestroyedMaterials.Add(material);
    public static readonly List<(GameObject Instance, float Delay)> DestroyCalls = [];
    public static void Destroy(GameObject instance) => DestroyCalls.Add((instance, 0));
    public static void Destroy(GameObject instance, float delay) => DestroyCalls.Add((instance, delay));
}
