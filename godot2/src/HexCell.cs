using Godot;

/// <summary>
/// Represents a single hexagonal cell.
/// Ported exactly from Catlike Coding Hex Map Tutorials 1-4.
/// </summary>
public partial class HexCell : Node3D
{
    public HexCoordinates Coordinates;
    public Color Color;

    private int _elevation;

    public int Elevation
    {
        get => _elevation;
        set
        {
            _elevation = value;
            Vector3 position = Position;
            position.Y = value * HexMetrics.ElevationStep;
            position.Y += (HexMetrics.SampleNoise(position).Y * 2f - 1f) *
                          HexMetrics.ElevationPerturbStrength;
            Position = position;
        }
    }

    private HexCell[] _neighbors = new HexCell[6];

    public HexCell GetNeighbor(HexDirection direction)
    {
        return _neighbors[(int)direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        _neighbors[(int)direction] = cell;
        cell._neighbors[(int)direction.Opposite()] = this;
    }

    /// <summary>
    /// Gets the edge type between this cell and its neighbor in the given direction.
    /// </summary>
    /// <returns>The edge type, or Cliff if no neighbor exists (map edge).</returns>
    public HexEdgeType GetEdgeType(HexDirection direction)
    {
        HexCell neighbor = GetNeighbor(direction);
        if (neighbor == null)
        {
            return HexEdgeType.Cliff;
        }
        return HexMetrics.GetEdgeType(_elevation, neighbor.Elevation);
    }

    /// <summary>
    /// Gets the edge type between this cell and another cell.
    /// </summary>
    public HexEdgeType GetEdgeType(HexCell otherCell)
    {
        return HexMetrics.GetEdgeType(_elevation, otherCell.Elevation);
    }
}
