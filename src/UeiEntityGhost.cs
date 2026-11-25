using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// A local visual-only hierarchy, with no colliders, scripts, items or networking.
internal sealed class UeiEntityGhost
{
    private GameObject? _root;
    private UeiEntityVisuals.Snapshot? _snapshot;
    private UeiEntityVisuals.Snapshot? _failedSnapshot;
    private SpriteRenderer[] _renderers = System.Array.Empty<SpriteRenderer>();
    private readonly Dictionary<Material, Material> _materials = new();

    internal bool Show(string resourcePath, Vector2 position, bool canPlace)
    {
        if (!UeiWorldSpawning.IsFinite(position.x) || !UeiWorldSpawning.IsFinite(position.y)
            || !UeiEntities.TryGetVisuals(resourcePath, out UeiEntityVisuals.Snapshot snapshot))
        {
            Hide();
            return false;
        }
        if (ReferenceEquals(_failedSnapshot, snapshot)) { Hide(); return false; }
        try
        {
            if (_root == null || !ReferenceEquals(_snapshot, snapshot)) Rebuild(snapshot);
            _root!.transform.position = new Vector3(position.x, position.y, 0f);
            for (int i = 0; i < _renderers.Length; i++)
            {
                Color color = snapshot.Parts[i].Color;
                color.a *= canPlace ? 0.45f : 0.2f;
                _renderers[i].color = color;
            }
            _root.SetActive(true);
            return true;
        }
        catch (System.Exception ex)
        {
            Destroy();
            _failedSnapshot = snapshot;
            UeiPlugin.LogWarning("UEI visual preview unavailable: " + resourcePath + ": " + ex.Message);
            return false;
        }
    }

    internal void Hide()
    {
        if (_root != null) _root.SetActive(false);
    }

    internal void Destroy()
    {
        Hide();
        if (_root != null) UnityEngine.Object.Destroy(_root);
        foreach (Material material in _materials.Values) if (material != null) UnityEngine.Object.Destroy(material);
        _materials.Clear();
        _root = null;
        _snapshot = null;
        _failedSnapshot = null;
        _renderers = System.Array.Empty<SpriteRenderer>();
    }

    private void Rebuild(UeiEntityVisuals.Snapshot snapshot)
    {
        Destroy();
        _root = new GameObject("UEI.EntityGhost");
        _root.SetActive(false);
        SortingGroup foreground = _root.AddComponent<SortingGroup>();
        int layer = 0;
        int value = int.MinValue;
        foreach (SortingLayer sorting in SortingLayer.layers)
            if (sorting.value > value) { value = sorting.value; layer = sorting.id; }
        foreground.sortingLayerID = layer;
        foreground.sortingOrder = short.MaxValue;

        var nodes = new Transform[snapshot.Nodes.Length];
        for (int i = 0; i < nodes.Length; i++)
        {
            UeiEntityVisuals.Node node = snapshot.Nodes[i];
            GameObject visual = new("Visual." + i);
            visual.layer = node.Layer;
            visual.transform.SetParent(node.Parent < 0 ? _root.transform : nodes[node.Parent], false);
            visual.transform.localPosition = node.Position;
            visual.transform.localRotation = node.Rotation;
            visual.transform.localScale = node.Scale;
            if (node.SortingGroup)
            {
                SortingGroup group = visual.AddComponent<SortingGroup>();
                group.sortingLayerID = node.SortingLayer;
                group.sortingOrder = node.SortingOrder;
                group.sortAtRoot = false; // Keep every part inside the preview's foreground group.
            }
            nodes[i] = visual.transform;
        }
        _renderers = new SpriteRenderer[snapshot.Parts.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            UeiEntityVisuals.Part part = snapshot.Parts[i];
            SpriteRenderer renderer = nodes[part.Node].gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = part.Sprite;
            renderer.flipX = part.FlipX;
            renderer.flipY = part.FlipY;
            renderer.drawMode = part.DrawMode;
            renderer.size = part.Size;
            renderer.tileMode = part.TileMode;
            renderer.adaptiveModeThreshold = part.AdaptiveThreshold;
            renderer.spriteSortPoint = part.SortPoint;
            renderer.maskInteraction = SpriteMaskInteraction.None;
            renderer.sortingLayerID = part.SortingLayer;
            renderer.sortingOrder = part.SortingOrder;
            if (part.Material != null)
            {
                if (!_materials.TryGetValue(part.Material, out Material? material))
                {
                    material = new Material(part.Material);
                    _materials.Add(part.Material, material);
                }
                renderer.sharedMaterial = material;
            }
            _renderers[i] = renderer;
        }
        _snapshot = snapshot;
    }
}
