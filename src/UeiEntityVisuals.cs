using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Only asset references and copied pose data survive the existing prefab scan.
internal static class UeiEntityVisuals
{
    internal sealed class Node
    {
        internal int Parent;
        internal Vector3 Position;
        internal Quaternion Rotation;
        internal Vector3 Scale;
        internal int Layer;
        internal bool SortingGroup;
        internal int SortingLayer;
        internal int SortingOrder;
        internal bool SortAtRoot;
    }

    internal sealed class Part
    {
        internal int Node;
        internal Sprite Sprite = null!;
        internal Material? Material;
        internal Color Color;
        internal bool FlipX;
        internal bool FlipY;
        internal SpriteDrawMode DrawMode;
        internal Vector2 Size;
        internal SpriteTileMode TileMode;
        internal float AdaptiveThreshold;
        internal SpriteSortPoint SortPoint;
        internal int SortingLayer;
        internal int SortingOrder;
    }

    internal sealed class Snapshot
    {
        internal static readonly Snapshot Empty = new(Array.Empty<Node>(), Array.Empty<Part>());
        internal readonly Node[] Nodes;
        internal readonly Part[] Parts;
        internal Snapshot(Node[] nodes, Part[] parts) { Nodes = nodes; Parts = parts; }
    }

    private static readonly List<SpriteRenderer> Renderers = new();

    internal static Snapshot Capture(GameObject prefab, TraderScript? trader)
    {
        Renderers.Clear();
        try
        {
            prefab.GetComponentsInChildren(true, Renderers);
            var nodes = new List<Node>();
            var parts = new List<Part>();
            var indices = new Dictionary<Transform, int>();
            Transform root = prefab.transform;

            int AddNode(Transform transform)
            {
                if (indices.TryGetValue(transform, out int existing)) return existing;
                int parent = transform == root ? -1 : AddNode(transform.parent != null ? transform.parent : root);
                SortingGroup? group = transform.GetComponent<SortingGroup>();
                int index = nodes.Count;
                indices.Add(transform, index);
                nodes.Add(new Node
                {
                    Parent = parent,
                    // Native spawn supplies its position and zero rotation;
                    // retain root scale and every child's local pose/pivot.
                    Position = transform == root ? Vector3.zero : transform.localPosition,
                    Rotation = transform == root ? Quaternion.identity : transform.localRotation,
                    Scale = transform.localScale,
                    Layer = transform.gameObject.layer,
                    SortingGroup = group != null && group.enabled,
                    SortingLayer = group != null ? group.sortingLayerID : 0,
                    SortingOrder = group != null ? group.sortingOrder : 0,
                    SortAtRoot = group != null && group.sortAtRoot,
                });
                return index;
            }

            foreach (SpriteRenderer renderer in Renderers)
            {
                if (renderer == null || !renderer.enabled) continue;
                bool visible = true;
                for (Transform? parent = renderer.transform; parent != root && parent != null; parent = parent.parent)
                    if (!parent.gameObject.activeSelf) { visible = false; break; }
                if (!visible) continue;
                Sprite? sprite = renderer.sprite;
                // Trader OnWillRenderObject selects these neutral sprites.
                // Copy the data; do not run Awake/Start or its render callback.
                if (trader != null && renderer == trader.headRender && trader.headSprites != null && trader.headSprites.Length > 0)
                    sprite = trader.headSprites[0];
                if (trader != null && renderer == trader.eyes && trader.eyeSprites != null && trader.eyeSprites.Length > 0)
                    sprite = trader.eyeSprites[0];
                if (sprite == null) continue;
                parts.Add(new Part
                {
                    Node = AddNode(renderer.transform), Sprite = sprite, Material = renderer.sharedMaterial,
                    Color = renderer.color, FlipX = renderer.flipX, FlipY = renderer.flipY,
                    DrawMode = renderer.drawMode, Size = renderer.size, TileMode = renderer.tileMode,
                    AdaptiveThreshold = renderer.adaptiveModeThreshold, SortPoint = renderer.spriteSortPoint,
                    SortingLayer = renderer.sortingLayerID, SortingOrder = renderer.sortingOrder,
                });
            }
            return parts.Count == 0 ? Snapshot.Empty : new Snapshot(nodes.ToArray(), parts.ToArray());
        }
        finally { Renderers.Clear(); }
    }
}
