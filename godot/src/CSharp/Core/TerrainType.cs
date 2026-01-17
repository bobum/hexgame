namespace HexGame.Core;

/// <summary>
/// Terrain type enumeration with associated colors and properties.
/// </summary>
public enum TerrainType
{
    Ocean,
    Coast,
    Plains,
    Forest,
    Hills,
    Mountains,
    Snow,
    Desert,
    Tundra,
    Jungle,
    Savanna,
    Taiga
}

/// <summary>
/// Extension methods for TerrainType providing colors and utility functions.
/// </summary>
public static class TerrainTypeExtensions
{
    /// <summary>
    /// Terrain colors - stylized low-poly palette.
    /// </summary>
    private static readonly Dictionary<TerrainType, Color> Colors = new()
    {
        { TerrainType.Ocean, new Color(0.102f, 0.298f, 0.431f) },     // 0x1a4c6e - Deep blue
        { TerrainType.Coast, new Color(0.176f, 0.545f, 0.788f) },     // 0x2d8bc9 - Light blue
        { TerrainType.Plains, new Color(0.52f, 0.75f, 0.28f) },       // Brighter grass green
        { TerrainType.Forest, new Color(0.180f, 0.490f, 0.196f) },    // 0x2e7d32 - Dark green
        { TerrainType.Hills, new Color(0.553f, 0.431f, 0.388f) },     // 0x8d6e63 - Brown
        { TerrainType.Mountains, new Color(0.459f, 0.459f, 0.459f) }, // 0x757575 - Gray
        { TerrainType.Snow, new Color(0.925f, 0.937f, 0.945f) },      // 0xeceff1 - White
        { TerrainType.Desert, new Color(0.902f, 0.784f, 0.431f) },    // 0xe6c86e - Sand yellow
        { TerrainType.Tundra, new Color(0.565f, 0.643f, 0.682f) },    // 0x90a4ae - Blue-gray
        { TerrainType.Jungle, new Color(0.106f, 0.369f, 0.125f) },    // 0x1b5e20 - Deep green
        { TerrainType.Savanna, new Color(0.773f, 0.659f, 0.333f) },   // 0xc5a855 - Golden brown
        { TerrainType.Taiga, new Color(0.290f, 0.388f, 0.365f) }      // 0x4a635d - Dark teal-green
    };

    /// <summary>
    /// Gets the display color for a terrain type.
    /// </summary>
    /// <param name="terrain">The terrain type.</param>
    /// <returns>The associated color.</returns>
    public static Color GetColor(this TerrainType terrain)
    {
        return Colors.TryGetValue(terrain, out var color) ? color : Colors.White;
    }

    /// <summary>
    /// Checks if the terrain type is a water type.
    /// </summary>
    /// <param name="terrain">The terrain type.</param>
    /// <returns>True if ocean or coast.</returns>
    public static bool IsWater(this TerrainType terrain)
    {
        return terrain is TerrainType.Ocean or TerrainType.Coast;
    }

    /// <summary>
    /// Gets the display name for a terrain type.
    /// </summary>
    /// <param name="terrain">The terrain type.</param>
    /// <returns>Human-readable name.</returns>
    public static string GetDisplayName(this TerrainType terrain)
    {
        return terrain switch
        {
            TerrainType.Ocean => "Ocean",
            TerrainType.Coast => "Coast",
            TerrainType.Plains => "Plains",
            TerrainType.Forest => "Forest",
            TerrainType.Hills => "Hills",
            TerrainType.Mountains => "Mountains",
            TerrainType.Snow => "Snow",
            TerrainType.Desert => "Desert",
            TerrainType.Tundra => "Tundra",
            TerrainType.Jungle => "Jungle",
            TerrainType.Savanna => "Savanna",
            TerrainType.Taiga => "Taiga",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Gets the base movement cost for traversing this terrain.
    /// </summary>
    /// <param name="terrain">The terrain type.</param>
    /// <returns>Movement cost in movement points.</returns>
    public static int GetMovementCost(this TerrainType terrain)
    {
        return terrain switch
        {
            TerrainType.Ocean => 1,
            TerrainType.Coast => 1,
            TerrainType.Plains => 1,
            TerrainType.Savanna => 1,
            TerrainType.Desert => 2,
            TerrainType.Forest => 2,
            TerrainType.Jungle => 3,
            TerrainType.Taiga => 2,
            TerrainType.Tundra => 2,
            TerrainType.Hills => 2,
            TerrainType.Mountains => 3,
            TerrainType.Snow => 2,
            _ => 1
        };
    }
}
