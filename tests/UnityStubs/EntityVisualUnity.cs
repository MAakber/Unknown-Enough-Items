namespace UnityEngine
{
    public sealed class Material
    {
        public int Marker;
        public Material() { }
        public Material(Material source) { Marker = source.Marker; }
    }
    public enum SpriteDrawMode { Simple, Sliced, Tiled }
    public enum SpriteTileMode { Continuous, Adaptive }
    public enum SpriteSortPoint { Center, Pivot }
    public enum SpriteMaskInteraction { None, VisibleInsideMask, VisibleOutsideMask }
    public readonly struct SortingLayer(int layerID, int sortValue)
    {
        public int id => layerID;
        public int value => sortValue;
        public static SortingLayer[] layers = [new(0, 0), new(7, 100)];
    }
}
namespace UnityEngine.Rendering
{
    public sealed class SortingGroup : UnityEngine.MonoBehaviour
    {
        public int sortingLayerID;
        public int sortingOrder;
        public bool sortAtRoot;
    }
}
