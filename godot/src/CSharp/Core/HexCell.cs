namespace HexGame.Core;

/// <summary>
/// Represents a single hex cell in the grid with terrain, elevation, and feature data.
/// </summary>
public class HexCell
{
    #region Coordinates

    /// <summary>
    /// Axial Q coordinate (column).
    /// </summary>
    public int Q { get; set; }

    /// <summary>
    /// Axial R coordinate (row).
    /// </summary>
    public int R { get; set; }

    /// <summary>
    /// Gets the hex coordinates as a struct.
    /// </summary>
    public HexCoordinates Coordinates => new(Q, R);

    #endregion

    #region Terrain

    /// <summary>
    /// Elevation level of this cell.
    /// </summary>
    public int Elevation { get; set; }

    /// <summary>
    /// The terrain type (biome) of this cell.
    /// </summary>
    public TerrainType TerrainType { get; set; } = TerrainType.Plains;

    /// <summary>
    /// Moisture level (0-1) used for biome determination.
    /// </summary>
    public float Moisture { get; set; }

    /// <summary>
    /// Temperature value (0-1) used for biome determination.
    /// </summary>
    public float Temperature { get; set; } = 0.5f;

    #endregion

    #region Rivers

    /// <summary>
    /// Whether this cell has any river flowing through it.
    /// </summary>
    public bool HasRiver { get; set; }

    /// <summary>
    /// Directions where rivers flow (edge indices 0-5).
    /// </summary>
    public List<int> RiverDirections { get; } = new();

    #endregion

    #region Features

    /// <summary>
    /// Whether this cell has a road.
    /// </summary>
    public bool HasRoad { get; set; }

    /// <summary>
    /// Features on this cell (trees, rocks, etc.).
    /// </summary>
    public List<Feature> Features { get; } = new();

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the world position of this cell's center.
    /// </summary>
    public Vector3 GetWorldPosition()
    {
        return Coordinates.ToWorldPosition(Elevation);
    }

    /// <summary>
    /// Checks if this cell is underwater (below land minimum elevation).
    /// </summary>
    public bool IsUnderwater => Elevation < HexMetrics.LandMinElevation;

    /// <summary>
    /// Checks if the terrain type is a water type.
    /// </summary>
    public bool IsWater => TerrainType.IsWater();

    /// <summary>
    /// Gets the terrain color for this cell.
    /// </summary>
    public Color GetColor() => TerrainType.GetColor();

    #endregion

    /// <summary>
    /// Returns a string representation of the cell.
    /// </summary>
    public override string ToString()
    {
        return $"HexCell({Q}, {R}) elev={Elevation} terrain={TerrainType}";
    }
}

/// <summary>
/// Represents a feature (tree, rock, etc.) placed in a hex cell.
/// </summary>
public class Feature
{
    /// <summary>
    /// Feature type enumeration.
    /// </summary>
    public enum FeatureType
    {
        Tree,
        Rock
    }

    /// <summary>
    /// The type of feature.
    /// </summary>
    public FeatureType Type { get; }

    /// <summary>
    /// World position of the feature.
    /// </summary>
    public Vector3 Position { get; }

    /// <summary>
    /// Rotation angle in radians.
    /// </summary>
    public float Rotation { get; }

    /// <summary>
    /// Scale multiplier.
    /// </summary>
    public float Scale { get; }

    /// <summary>
    /// Creates a new feature.
    /// </summary>
    /// <param name="type">Feature type.</param>
    /// <param name="position">World position.</param>
    /// <param name="rotation">Rotation in radians.</param>
    /// <param name="scale">Scale multiplier.</param>
    public Feature(FeatureType type, Vector3 position, float rotation, float scale)
    {
        Type = type;
        Position = position;
        Rotation = rotation;
        Scale = scale;
    }
}
