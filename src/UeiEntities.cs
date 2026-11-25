using System;
using System.Collections.Generic;
using UnityEngine;

// Descriptors share the acquisition scanner's canonical root-prefab reads.
// Keep names and sprites, never a prefab or an instantiated gameplay object.
internal static class UeiEntities
{
    internal sealed class Descriptor
    {
        internal string ResourcePath = string.Empty;
        internal string LocaleId = string.Empty;
        internal string Name = string.Empty;
        internal string Description = string.Empty;
        internal Sprite? Icon;
        internal int TraderVariant;
        internal UeiEntityVisuals.Snapshot Visuals = UeiEntityVisuals.Snapshot.Empty;
    }

    private static readonly Dictionary<string, Descriptor> ByPath = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<Descriptor> EntriesBacking = new();
    private static bool _dirty;
    private static float _lastPublish = -999f;

    internal static IReadOnlyList<Descriptor> Entries => EntriesBacking;
    internal static int Revision { get; private set; }

    internal static void Capture(string resourcePath, GameObject prefab, BuildingEntity? root)
    {
        // A source's child components are not independently spawnable resources.
        // Item roots already have entries and must retain their give behavior.
        if (root == null || prefab.GetComponent<Item>() != null || ByPath.ContainsKey(resourcePath)) return;
        TraderScript? trader = prefab.GetComponent<TraderScript>();
        SpriteRenderer? renderer = trader?.headRender ?? prefab.GetComponentInChildren<SpriteRenderer>(true);
        Descriptor descriptor = new()
        {
            ResourcePath = resourcePath,
            LocaleId = root.id ?? string.Empty,
            Name = string.IsNullOrWhiteSpace(root.fullName) ? prefab.name : root.fullName,
            Description = root.description ?? string.Empty,
            Icon = renderer != null ? renderer.sprite : null,
            TraderVariant = trader == null ? 0 : trader.character + 1,
        };
        try { descriptor.Visuals = UeiEntityVisuals.Capture(prefab, trader); }
        catch (Exception ex) { UeiPlugin.LogWarning("UEI entity appearance unavailable: " + resourcePath + ": " + ex.Message); }
        if (trader != null && trader.headSprites != null && trader.headSprites.Length > 0 && trader.headSprites[0] != null)
            descriptor.Icon = trader.headSprites[0];
        else if (descriptor.Icon == null && descriptor.Visuals.Parts.Length > 0)
            descriptor.Icon = descriptor.Visuals.Parts[0].Sprite;
        ByPath.Add(resourcePath, descriptor);
        EntriesBacking.Add(descriptor);
        _dirty = true;
    }

    internal static void Publish(float now, bool force = false)
    {
        if (!_dirty || (!force && now - _lastPublish < 0.75f)) return;
        _dirty = false;
        _lastPublish = now;
        Revision++;
    }

    internal static bool Contains(string resourcePath) => ByPath.ContainsKey(resourcePath);

    internal static bool TryGetVisuals(string resourcePath, out UeiEntityVisuals.Snapshot snapshot)
    {
        if (ByPath.TryGetValue(resourcePath, out Descriptor? descriptor) && descriptor != null && descriptor.Visuals.Parts.Length > 0)
        {
            snapshot = descriptor.Visuals;
            return true;
        }
        snapshot = UeiEntityVisuals.Snapshot.Empty;
        return false;
    }

    internal static UeiEntry BuildEntry(Descriptor descriptor)
    {
        string name = UeiI18n.BuildingText(descriptor.LocaleId, descriptor.Name);
        if (descriptor.TraderVariant > 0)
            name = UeiI18n.Format("entity.traderVariant", name, descriptor.TraderVariant);
        string description = UeiI18n.BuildingText(descriptor.LocaleId + "dsc", descriptor.Description);
        string category = UeiI18n.CategoryLabel(UeiCatalog.CategoryEntity);
        return new UeiEntry
        {
            Kind = UeiEntryKind.Entity,
            Id = descriptor.ResourcePath,
            EntryKey = UeiCatalog.EntryKey(UeiEntryKind.Entity, descriptor.ResourcePath),
            DisplayName = name,
            Description = description,
            CategoryKey = UeiCatalog.CategoryEntity,
            CategoryLabel = category,
            Icon = descriptor.Icon,
            SearchText = string.Join(" ", descriptor.ResourcePath, descriptor.LocaleId, descriptor.Name,
                name, description, UeiCatalog.CategoryEntity, category),
        };
    }
}
