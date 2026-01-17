namespace HexGame.Core;

/// <summary>
/// Immutable axial hex coordinates (q, r) with cube coordinate conversions.
/// </summary>
/// <remarks>
/// Uses the axial coordinate system where q is the column and r is the row.
/// Cube coordinates (x, y, z) are derived: x = q, z = r, y = -q - r.
/// </remarks>
public readonly struct HexCoordinates : IEquatable<HexCoordinates>
{
    /// <summary>
    /// Column coordinate (axial q).
    /// </summary>
    public int Q { get; }

    /// <summary>
    /// Row coordinate (axial r).
    /// </summary>
    public int R { get; }

    /// <summary>
    /// Cube coordinate X (same as Q).
    /// </summary>
    public int X => Q;

    /// <summary>
    /// Cube coordinate Y (derived: -Q - R).
    /// </summary>
    public int Y => -Q - R;

    /// <summary>
    /// Cube coordinate Z (same as R).
    /// </summary>
    public int Z => R;

    /// <summary>
    /// Creates new hex coordinates.
    /// </summary>
    /// <param name="q">Column coordinate.</param>
    /// <param name="r">Row coordinate.</param>
    public HexCoordinates(int q, int r)
    {
        Q = q;
        R = r;
    }

    /// <summary>
    /// Converts hex coordinates to world position at the specified elevation.
    /// </summary>
    /// <param name="elevation">The elevation level.</param>
    /// <returns>World position as Vector3.</returns>
    public Vector3 ToWorldPosition(int elevation = 0)
    {
        float x = (Q + R * 0.5f) * (HexMetrics.InnerRadius * 2f);
        float z = R * (HexMetrics.OuterRadius * 1.5f);
        float y = elevation * HexMetrics.ElevationStep;
        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Creates hex coordinates from a world position.
    /// </summary>
    /// <param name="position">World position.</param>
    /// <returns>The nearest hex coordinates.</returns>
    public static HexCoordinates FromWorldPosition(Vector3 position)
    {
        float qFloat = position.X / (HexMetrics.InnerRadius * 2f);
        float rFloat = position.Z / (HexMetrics.OuterRadius * 1.5f);
        qFloat -= rFloat * 0.5f;

        // Round to nearest hex
        int qInt = Mathf.RoundToInt(qFloat);
        int rInt = Mathf.RoundToInt(rFloat);

        return new HexCoordinates(qInt, rInt);
    }

    /// <summary>
    /// Calculates the distance to another hex in hex steps.
    /// </summary>
    /// <param name="other">The target hex coordinates.</param>
    /// <returns>Distance in hex steps.</returns>
    public int DistanceTo(HexCoordinates other)
    {
        int dx = Math.Abs(X - other.X);
        int dy = Math.Abs(Y - other.Y);
        int dz = Math.Abs(Z - other.Z);
        return (dx + dy + dz) / 2;
    }

    /// <summary>
    /// Gets the neighbor coordinates in the specified direction.
    /// </summary>
    /// <param name="direction">The direction to the neighbor.</param>
    /// <returns>Neighbor hex coordinates.</returns>
    public HexCoordinates GetNeighbor(HexDirection direction)
    {
        var offset = direction.GetOffset();
        return new HexCoordinates(Q + offset.X, R + offset.Y);
    }

    /// <summary>
    /// Gets all 6 neighbor coordinates.
    /// </summary>
    /// <returns>Array of 6 neighbor coordinates.</returns>
    public HexCoordinates[] GetNeighbors()
    {
        var neighbors = new HexCoordinates[6];
        for (int i = 0; i < 6; i++)
        {
            neighbors[i] = GetNeighbor((HexDirection)i);
        }
        return neighbors;
    }

    #region Equality

    public bool Equals(HexCoordinates other) => Q == other.Q && R == other.R;

    public override bool Equals(object? obj) => obj is HexCoordinates other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Q, R);

    public static bool operator ==(HexCoordinates left, HexCoordinates right) => left.Equals(right);

    public static bool operator !=(HexCoordinates left, HexCoordinates right) => !left.Equals(right);

    #endregion

    /// <summary>
    /// Returns a string representation of the coordinates.
    /// </summary>
    public override string ToString() => $"({Q}, {R})";

    /// <summary>
    /// Creates a key string suitable for dictionary lookups.
    /// </summary>
    public string ToKey() => $"{Q},{R}";
}
