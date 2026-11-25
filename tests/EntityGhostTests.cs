using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

internal static class EntityGhostTests
{
    internal static void Run(Action<bool, string> check)
    {
        GameObject prefab = new("ghost_fixture");
        prefab.Attach(new BuildingEntity { id = "ghost_fixture" });
        prefab.transform.position = new(1000, 2000, 0);
        prefab.transform.localScale = new(2, 3, 1);
        Material originalMaterial = new() { Marker = 31 };
        Sprite bodySprite = new();
        Sprite originalHead = new();
        Sprite neutralHead = new();
        Sprite neutralEyes = new();
        SpriteRenderer body = prefab.AddComponent<SpriteRenderer>();
        body.sprite = bodySprite;
        body.sharedMaterial = originalMaterial;
        body.color = new(.8f, .6f, .4f, .8f);
        body.drawMode = SpriteDrawMode.Tiled;
        body.size = new(4, 5);
        body.tileMode = SpriteTileMode.Adaptive;
        body.sortingOrder = 3;
        GameObject pivot = new("pivot");
        prefab.Children.Add(pivot);
        pivot.transform.localPosition = new(0, 2, 0);
        pivot.transform.localRotation = Quaternion.Euler(0, 0, 20);
        pivot.transform.localScale = new(-1, 1, 1);
        SortingGroup nested = pivot.AddComponent<SortingGroup>();
        nested.sortingLayerID = 7;
        nested.sortingOrder = 4;
        GameObject headObject = new("head");
        pivot.Children.Add(headObject);
        headObject.transform.localPosition = new(0, 1, 0);
        SpriteRenderer head = headObject.AddComponent<SpriteRenderer>();
        head.sprite = originalHead;
        head.sharedMaterial = originalMaterial;
        head.flipX = true;
        head.sortingOrder = 5;
        SpriteRenderer eyes = headObject.AddComponent<SpriteRenderer>();
        eyes.sprite = null;
        eyes.sortingOrder = 6;
        TraderScript trader = prefab.AddComponent<TraderScript>();
        trader.headRender = head;
        trader.eyes = eyes;
        trader.headSprites = [neutralHead, new()];
        trader.eyeSprites = [neutralEyes, new()];
        GameObject hidden = new("hidden");
        prefab.Children.Add(hidden);
        hidden.SetActive(false);
        hidden.AddComponent<SpriteRenderer>().sprite = new();
        SpriteRenderer disabled = prefab.AddComponent<SpriteRenderer>();
        disabled.sprite = new();
        disabled.enabled = false;

        int objectsBeforeCapture = GameObject.Created.Count;
        UeiEntities.Capture("ghost_fixture", prefab, prefab.GetComponent<BuildingEntity>());
        check(UeiEntities.TryGetVisuals("ghost_fixture", out var visual), "An entity's existing prefab scan must capture its appearance");
        check(visual.Parts.Length == 3 && visual.Nodes.Length == 3,
            "Composite sprite layers and ancestors must be captured, excluding disabled/hidden layers");
        check(visual.Nodes[0].Parent == -1 && visual.Nodes[0].Position.x == 0 && visual.Nodes[0].Position.y == 0
            && visual.Nodes[0].Scale.x == 2 && visual.Nodes[0].Scale.y == 3,
            "The snapshot must discard the prefab's parked world position and retain its actual root scale");
        check(visual.Nodes[1].Parent == 0 && visual.Nodes[1].Position.y == 2 && visual.Nodes[1].Scale.x == -1
            && visual.Nodes[1].Rotation.z == 20 && visual.Nodes[2].Parent == 1 && visual.Nodes[2].Position.y == 1,
            "Nested poses and mirrored/nonuniform scale must keep their hierarchy rather than flattening the pivot");
        check(ReferenceEquals(visual.Parts[1].Sprite, neutralHead) && ReferenceEquals(visual.Parts[2].Sprite, neutralEyes),
            "Trader neutral appearance must come from native sprite data without running its render callback");
        check(ReferenceEquals(head.sprite, originalHead) && eyes.sprite == null && originalMaterial.Marker == 31
            && GameObject.Created.Count == objectsBeforeCapture,
            "Appearance capture must not create objects or mutate source sprites/materials");
        foreach (Type type in new[] { typeof(UeiEntityVisuals.Node), typeof(UeiEntityVisuals.Part) })
            check(!type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Any(field => field.FieldType == typeof(GameObject) || field.FieldType == typeof(Transform)
                    || field.FieldType == typeof(SpriteRenderer)), "Cached appearance data must not retain prefab objects/components");

        UeiEntityGhost ghost = new();
        int factoryCalls = 0;
        var oldFactory = Utils.CreateHandler;
        Utils.CreateHandler = (_, _, _) => { factoryCalls++; return null; };
        try
        {
            int beforeGhost = GameObject.Created.Count;
            check(ghost.Show("ghost_fixture", new(-12, 34), true), "A known sprite appearance must produce a local world ghost");
            GameObject root = GameObject.Created.Last(obj => obj.name == "UEI.EntityGhost");
            var created = GameObject.Created.Skip(beforeGhost).ToList();
            check(factoryCalls == 0 && created.All(obj => obj.Components.All(component => component is SpriteRenderer || component is SortingGroup)),
                "The ghost must contain only visual components and never invoke a gameplay factory");
            check(root.transform.position.x == -12 && root.transform.position.y == 34 && root.transform.position.z == 0,
                "The ghost origin must be the selected world spawn position");
            GameObject rootVisual = root.Children.Single();
            check(rootVisual.transform.localScale.x == 2 && rootVisual.transform.localScale.y == 3
                && rootVisual.Children.Single().transform.localRotation.z == 20
                && rootVisual.Children.Single().transform.localScale.x == -1,
                "The rendered hierarchy must preserve prefab dimensions and nested mirrored poses");
            var rendered = created.SelectMany(obj => obj.Components).OfType<SpriteRenderer>().ToList();
            check(rendered.Count == 3 && rendered[0].drawMode == SpriteDrawMode.Tiled && rendered[0].size.x == 4
                && rendered[1].flipX && rendered[1].sortingOrder == 5,
                "Visual rendering must preserve sliced/tiled size, flipping and part order");
            check(Math.Abs(rendered[0].color.a - .36f) < .0001f && body.color.a == .8f
                && !ReferenceEquals(rendered[0].sharedMaterial, originalMaterial)
                && ReferenceEquals(rendered[0].sharedMaterial, rendered[1].sharedMaterial),
                "Preview opacity and shared material copies must not alter game assets");
            int afterGhost = GameObject.Created.Count;
            ghost.Show("ghost_fixture", new(18, -22), false);
            check(GameObject.Created.Count == afterGhost && root.transform.position.x == 18
                && Math.Abs(rendered[0].color.a - .16f) < .0001f,
                "Moving/dimming the preview must reuse its hierarchy without respawning objects");
            ghost.Hide();
            check(!root.activeSelf, "Cancellation must hide the world ghost immediately");
            ghost.Show("ghost_fixture", new(1, 2), true);
            check(root.activeSelf && GameObject.Created.Count == afterGhost, "Reselecting the same appearance must reuse a hidden ghost");
            check(!ghost.Show("missing", new(1, 2), true) && !root.activeSelf,
                "An unavailable appearance must hide stale geometry without a crosshair substitute");
            int materialsBefore = UnityEngine.Object.DestroyedMaterials.Count;
            ghost.Destroy();
            check(UnityEngine.Object.DestroyCalls.Any(call => ReferenceEquals(call.Instance, root))
                && UnityEngine.Object.DestroyedMaterials.Count == materialsBefore + 1,
                "Disposal must remove preview objects and copied materials while preserving source assets");
            check(!UnityEngine.Object.DestroyCalls.Any(call => ReferenceEquals(call.Instance, prefab)) && factoryCalls == 0,
                "Preview teardown must never destroy a prefab or create a real entity");
        }
        finally { ghost.Destroy(); Utils.CreateHandler = oldFactory; }
        Console.WriteLine("PASS: composite entity appearance snapshots, world ghost poses/scale/pivots, source isolation, reuse and cleanup (renderer API doubles only)");
    }
}
