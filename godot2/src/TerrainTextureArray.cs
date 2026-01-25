using Godot;
using System.Collections.Generic;

/// <summary>
/// Creates and manages the terrain Texture2DArray for Tutorial 14.
/// Loads individual terrain textures and combines them into a texture array
/// for efficient GPU sampling in the terrain shader.
/// </summary>
public static class TerrainTextureArray
{
    private static Texture2DArray? _textureArray;
    private static bool _initialized;

    /// <summary>
    /// Terrain texture paths in index order:
    /// 0 = sand, 1 = grass, 2 = mud, 3 = stone, 4 = snow
    /// </summary>
    private static readonly string[] TexturePaths = new[]
    {
        "res://textures/terrain/sand.png",   // Index 0
        "res://textures/terrain/grass.png",  // Index 1
        "res://textures/terrain/mud.png",    // Index 2
        "res://textures/terrain/stone.png",  // Index 3
        "res://textures/terrain/snow.png"    // Index 4
    };

    /// <summary>
    /// Gets the terrain texture array, creating it if necessary.
    /// Returns null if textures couldn't be loaded.
    /// </summary>
    public static Texture2DArray? GetTextureArray()
    {
        if (!_initialized)
        {
            _initialized = true;
            _textureArray = CreateTextureArray();
        }
        return _textureArray;
    }

    /// <summary>
    /// Creates the Texture2DArray from individual terrain textures.
    /// Uses direct file loading to avoid dependency on Godot's import system.
    /// </summary>
    private static Texture2DArray? CreateTextureArray()
    {
        var images = new List<Image>();

        foreach (var path in TexturePaths)
        {
            // Convert res:// path to absolute path for direct loading
            var globalPath = ProjectSettings.GlobalizePath(path);

            var image = new Image();
            var loadError = image.Load(globalPath);
            if (loadError != Error.Ok)
            {
                GD.PrintErr($"[TerrainTextureArray] Failed to load image: {path} ({loadError})");
                return null;
            }

            images.Add(image);
            GD.Print($"[TerrainTextureArray] Loaded: {path} ({image.GetWidth()}x{image.GetHeight()}, {image.GetFormat()})");
        }

        // Verify all images have the same dimensions
        int width = images[0].GetWidth();
        int height = images[0].GetHeight();
        var format = images[0].GetFormat();

        for (int i = 1; i < images.Count; i++)
        {
            if (images[i].GetWidth() != width || images[i].GetHeight() != height)
            {
                GD.PrintErr($"[TerrainTextureArray] Texture size mismatch at index {i}: " +
                           $"expected {width}x{height}, got {images[i].GetWidth()}x{images[i].GetHeight()}");
                return null;
            }

            // Convert to same format if needed
            if (images[i].GetFormat() != format)
            {
                images[i].Convert(format);
            }
        }

        // Create the Texture2DArray
        var textureArray = new Texture2DArray();
        var imageArray = new Godot.Collections.Array<Image>();
        foreach (var img in images)
        {
            imageArray.Add(img);
        }

        var createError = textureArray.CreateFromImages(imageArray);
        if (createError != Error.Ok)
        {
            GD.PrintErr($"[TerrainTextureArray] Failed to create texture array: {createError}");
            return null;
        }

        GD.Print($"[TerrainTextureArray] Created texture array: {width}x{height}, {images.Count} layers");
        return textureArray;
    }
}
