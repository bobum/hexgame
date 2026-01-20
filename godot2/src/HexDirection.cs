/// <summary>
/// Hex grid directions for pointy-top hexagons.
/// Ported exactly from Catlike Coding Hex Map Tutorial 2.
/// </summary>
public enum HexDirection
{
    NE, E, SE, SW, W, NW
}

/// <summary>
/// Extension methods for HexDirection.
/// Ported exactly from Catlike Coding Hex Map Tutorial 2.
/// </summary>
public static class HexDirectionExtensions
{
    public static HexDirection Opposite(this HexDirection direction)
    {
        return (int)direction < 3 ? (direction + 3) : (direction - 3);
    }

    public static HexDirection Previous(this HexDirection direction)
    {
        return direction == HexDirection.NE ? HexDirection.NW : (direction - 1);
    }

    public static HexDirection Next(this HexDirection direction)
    {
        return direction == HexDirection.NW ? HexDirection.NE : (direction + 1);
    }

    /// <summary>
    /// Returns the direction two steps counterclockwise.
    /// Used for gentle river curves. Tutorial 6.
    /// </summary>
    public static HexDirection Previous2(this HexDirection direction)
    {
        direction -= 2;
        return direction >= HexDirection.NE ? direction : (direction + 6);
    }

    /// <summary>
    /// Returns the direction two steps clockwise.
    /// Used for gentle river curves. Tutorial 6.
    /// </summary>
    public static HexDirection Next2(this HexDirection direction)
    {
        direction += 2;
        return direction <= HexDirection.NW ? direction : (direction - 6);
    }
}
