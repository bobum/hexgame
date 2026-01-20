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

    // Tutorial 6: River data
    private bool _hasIncomingRiver;
    private bool _hasOutgoingRiver;
    private HexDirection _incomingRiver;
    private HexDirection _outgoingRiver;

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

            ValidateRivers();
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

    // Tutorial 6: River properties

    public bool HasIncomingRiver => _hasIncomingRiver;
    public bool HasOutgoingRiver => _hasOutgoingRiver;
    public HexDirection IncomingRiver => _incomingRiver;
    public HexDirection OutgoingRiver => _outgoingRiver;

    public bool HasRiver => _hasIncomingRiver || _hasOutgoingRiver;

    public bool HasRiverBeginOrEnd => _hasIncomingRiver != _hasOutgoingRiver;

    /// <summary>
    /// Returns true if a river flows through the specified edge.
    /// </summary>
    public bool HasRiverThroughEdge(HexDirection direction)
    {
        return (_hasIncomingRiver && _incomingRiver == direction) ||
               (_hasOutgoingRiver && _outgoingRiver == direction);
    }

    /// <summary>
    /// Y position of the stream bed (channel floor).
    /// </summary>
    public float StreamBedY =>
        (_elevation + HexMetrics.StreamBedElevationOffset) * HexMetrics.ElevationStep;

    /// <summary>
    /// Y position of the river water surface.
    /// </summary>
    public float RiverSurfaceY =>
        (_elevation + HexMetrics.RiverSurfaceElevationOffset) * HexMetrics.ElevationStep;

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

    // Tutorial 6: River manipulation methods

    /// <summary>
    /// Sets an outgoing river in the specified direction.
    /// Rivers can only flow to cells at the same elevation or lower.
    /// </summary>
    public void SetOutgoingRiver(HexDirection direction)
    {
        if (_hasOutgoingRiver && _outgoingRiver == direction)
        {
            return;
        }

        HexCell neighbor = GetNeighbor(direction);
        if (!IsValidRiverDestination(neighbor))
        {
            return;
        }

        RemoveOutgoingRiver();
        if (_hasIncomingRiver && _incomingRiver == direction)
        {
            RemoveIncomingRiver();
        }

        _hasOutgoingRiver = true;
        _outgoingRiver = direction;
        RefreshSelfOnly();

        neighbor.RemoveIncomingRiver();
        neighbor._hasIncomingRiver = true;
        neighbor._incomingRiver = direction.Opposite();
        neighbor.RefreshSelfOnly();
    }

    /// <summary>
    /// Removes the outgoing river from this cell.
    /// </summary>
    public void RemoveOutgoingRiver()
    {
        if (!_hasOutgoingRiver)
        {
            return;
        }
        _hasOutgoingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(_outgoingRiver);
        neighbor._hasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    /// <summary>
    /// Removes the incoming river from this cell.
    /// </summary>
    public void RemoveIncomingRiver()
    {
        if (!_hasIncomingRiver)
        {
            return;
        }
        _hasIncomingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(_incomingRiver);
        neighbor._hasOutgoingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    /// <summary>
    /// Removes both incoming and outgoing rivers from this cell.
    /// </summary>
    public void RemoveRiver()
    {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }

    /// <summary>
    /// Checks if a neighbor is a valid destination for a river.
    /// Rivers can only flow to cells at the same elevation or lower.
    /// </summary>
    bool IsValidRiverDestination(HexCell neighbor)
    {
        return neighbor != null && _elevation >= neighbor.Elevation;
    }

    /// <summary>
    /// Validates rivers after elevation change.
    /// Removes rivers that would now flow uphill.
    /// </summary>
    void ValidateRivers()
    {
        if (_hasOutgoingRiver && !IsValidRiverDestination(GetNeighbor(_outgoingRiver)))
        {
            RemoveOutgoingRiver();
        }
        if (_hasIncomingRiver)
        {
            HexCell neighbor = GetNeighbor(_incomingRiver);
            if (neighbor != null && !neighbor.IsValidRiverDestination(this))
            {
                RemoveIncomingRiver();
            }
        }
    }

    /// <summary>
    /// Refreshes only this cell's chunk, not neighbors.
    /// Used by river operations to avoid unnecessary neighbor refreshes.
    /// </summary>
    void RefreshSelfOnly()
    {
        Chunk?.Refresh();
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
