using UnityEngine;

// Display-only asset: catalog entries retain their real sprite or null.
internal static class UeiMissingIcon
{
    private static Sprite? _sprite;
    private static Texture2D? _texture;

    internal static Sprite Sprite
    {
        get
        {
            if (_sprite != null) return _sprite;
            const int size = 16;
            _texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "UEI.MissingIcon",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            Color black = new(0f, 0f, 0f, 1f);
            Color purple = new(0.97f, 0f, 0.97f, 1f);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = ((x / 8 + y / 8) & 1) == 0 ? black : purple;
            _texture.SetPixels(pixels);
            _texture.Apply(false, true);
            _sprite = UnityEngine.Sprite.Create(_texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f, 0, SpriteMeshType.FullRect);
            _sprite.name = "UEI.MissingIcon";
            _sprite.hideFlags = HideFlags.HideAndDontSave;
            return _sprite;
        }
    }

    internal static void Release()
    {
        if (_sprite != null) Object.Destroy(_sprite);
        if (_texture != null) Object.Destroy(_texture);
        _sprite = null;
        _texture = null;
    }
}
