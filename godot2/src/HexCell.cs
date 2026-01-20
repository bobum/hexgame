using Godot;

/// <summary>
/// Represents a single hexagonal cell.
/// Ported from Catlike Coding Hex Map Tutorials 1-5.
/// </summary>
public partial class HexCell : Node3D
{
    public HexCoordinates Coordinates;

    /// <summary>
    /// Reference to the chunk this cell belongs to.
    /// Used for deferred mesh updates.
    /// </summary>
    public HexGridChunk Chunk = null!;

    /// <summary>
    /// Reference to the coordinate label for this cell.
    /// </summary>
    public Label3D? UiLabel;

    private HexCell[] _neighbors = new HexCell[6];

    // Tutorial 5: Initialize to MinValue so first set always triggers refresh
    private int _elevation = int.MinValue;
    private Color _color;

    public int Elevation
    {
        get => _elevation;
        set
        {
            if (_elevation == value) return;

            _elevation = value;
            Vector3 position = Position;
            position.Y = value * HexMetrics.ElevationStep;
            position.Y += (HexMetrics.SampleNoise(position).Y * 2f - 1f) *
                          HexMetrics.ElevationPerturbStrength;
            Position = position;

            // Update label position to match new elevation
            if (UiLabel != null)
            {
                Vector3 labelPos = UiLabel.Position;
                labelPos.Y = position.Y + 0.1f;
                UiLabel.Position = labelPos;
            }

            Refresh();
        }
    }

    public Color Color
    {
        get => _color;
        set
        {
            if (_color == value) return;
            _color = value;
            Refresh();
        }
    }

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

    /// <summary>
    /// Notifies this cell's chunk that it needs to refresh.
    /// Also notifies neighbor chunks if this cell is on a chunk boundary.
    /// </summary>
    void Refresh()
    {
        if (Chunk == null) return;

        Chunk.Refresh();

        // Refresh neighbor chunks when cell is on chunk boundary
        for (int i = 0; i < _neighbors.Length; i++)
        {
            HexCell neighbor = _neighbors[i];
            if (neighbor != null && neighbor.Chunk != Chunk)
            {
                neighbor.Chunk.Refresh();
            }
        }
    }

}
