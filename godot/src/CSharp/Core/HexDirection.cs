namespace HexGame.Core;

/// <summary>
/// Hex direction enumeration for flat-topped hexes.
/// </summary>
public enum HexDirection
{
    /// <summary>Northeast direction.</summary>
    NE = 0,
    /// <summary>East direction.</summary>
    E = 1,
    /// <summary>Southeast direction.</summary>
    SE = 2,
    /// <summary>Southwest direction.</summary>
    SW = 3,
    /// <summary>West direction.</summary>
    W = 4,
    /// <summary>Northwest direction.</summary>
    NW = 5
}

/// <summary>
/// Extension methods for HexDirection to provide navigation utilities.
/// </summary>
public static class HexDirectionExtensions
{
    /// <summary>
    /// Axial coordinate offsets for each direction (q, r).
    /// </summary>
    private static readonly Vector2I[] Offsets =
    {
        new(1, 0),   // NE: q+1, r+0
        new(1, -1),  // E:  q+1, r-1
        new(0, -1),  // SE: q+0, r-1
        new(-1, 0),  // SW: q-1, r+0
        new(-1, 1),  // W:  q-1, r+1
        new(0, 1)    // NW: q+0, r+1
    };

    /// <summary>
    /// Gets the axial coordinate offset for a direction.
    /// </summary>
    /// <param name="direction">The direction.</param>
    /// <returns>The offset as (q, r) in a Vector2I.</returns>
    public static Vector2I GetOffset(this HexDirection direction)
    {
        return Offsets[(int)direction % 6];
    }

    /// <summary>
    /// Gets the opposite direction.
    /// </summary>
    /// <param name="direction">The direction.</param>
    /// <returns>The opposite direction.</returns>
    public static HexDirection Opposite(this HexDirection direction)
    {
        return (HexDirection)(((int)direction + 3) % 6);
    }

    /// <summary>
    /// Gets the next direction (clockwise).
    /// </summary>
    /// <param name="direction">The direction.</param>
    /// <returns>The next clockwise direction.</returns>
    public static HexDirection Next(this HexDirection direction)
    {
        return (HexDirection)(((int)direction + 1) % 6);
    }

    /// <summary>
    /// Gets the previous direction (counter-clockwise).
    /// </summary>
    /// <param name="direction">The direction.</param>
    /// <returns>The previous counter-clockwise direction.</returns>
    public static HexDirection Previous(this HexDirection direction)
    {
        return (HexDirection)(((int)direction + 5) % 6);
    }
}
