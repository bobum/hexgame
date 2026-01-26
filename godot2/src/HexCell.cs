using Godot;

/// <summary>
/// Represents a single hexagonal cell.
/// Ported from Catlike Coding Hex Map Tutorials 1-5.
/// Tutorial 16: Added cell highlighting for pathfinding visualization.
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

    // Tutorial 16: Highlight sprite for pathfinding visualization
    private Sprite3D? _highlight;
    private static Material? _highlightMaterial;

    private HexCell[] _neighbors = new HexCell[6];

    // Tutorial 5: Initialize to MinValue so first set always triggers refresh
    private int _elevation = int.MinValue;

    // Tutorial 14: Terrain type replaces color
    private int _terrainTypeIndex;

    // Tutorial 6: River data
    private bool _hasIncomingRiver;
    private bool _hasOutgoingRiver;
    private HexDirection _incomingRiver;
    private HexDirection _outgoingRiver;

    // Tutorial 7: Road data
    private bool[] _roads = new bool[6];

    // Tutorial 8: Water data
    private int _waterLevel;

    // Tutorial 9: Feature level data
    private int _urbanLevel;
    private int _farmLevel;
    private int _plantLevel;

    // Tutorial 10: Wall data
    private bool _walled;

    // Tutorial 11: Special feature data
    private int _specialIndex;

    // Tutorial 15: Pathfinding data
    // Initialize to MaxValue so BFS can detect unvisited cells
    private int _distance = int.MaxValue;
    public HexCell? PathFrom { get; set; }
    public int SearchHeuristic { get; set; }
    public int SearchPhase { get; set; }

    /// <summary>
    /// Distance from the selected cell. Used for pathfinding visualization.
    /// Tutorial 15: Setting this updates the cell's label.
    /// </summary>
    public int Distance
    {
        get => _distance;
        set
        {
            _distance = value;
            UpdateDistanceLabel();
        }
    }

    /// <summary>
    /// Combined priority for A* pathfinding.
    /// Tutorial 15: Distance + heuristic for priority queue ordering.
    /// </summary>
    public int SearchPriority => _distance + SearchHeuristic;

    /// <summary>
    /// Next cell in the priority queue linked list.
    /// Tutorial 15: Used for efficient priority queue implementation.
    /// </summary>
    public HexCell? NextWithSamePriority { get; set; }

    /// <summary>
    /// Updates the cell label to show distance value.
    /// Tutorial 15.
    /// </summary>
    void UpdateDistanceLabel()
    {
        if (UiLabel != null)
        {
            UiLabel.Text = _distance == int.MaxValue ? "" : _distance.ToString();
        }
    }

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
            ValidateRoads();
            Refresh();
        }
    }

    /// <summary>
    /// Index into terrain texture array (0 = sand, 1 = grass, 2 = mud, 3 = stone, 4 = snow).
    /// Tutorial 14: Replaces Color property.
    /// </summary>
    public int TerrainTypeIndex
    {
        get => _terrainTypeIndex;
        set
        {
            if (_terrainTypeIndex == value) return;
            _terrainTypeIndex = value;
            Refresh();
        }
    }

    // Tutorial 8: Water properties

    public int WaterLevel
    {
        get => _waterLevel;
        set
        {
            if (_waterLevel == value) return;
            _waterLevel = value;
            ValidateRivers();
            Refresh();
        }
    }

    /// <summary>
    /// Returns true if this cell is underwater (water level > elevation).
    /// </summary>
    public bool IsUnderwater => _waterLevel > _elevation;

    /// <summary>
    /// Y position of the water surface.
    /// </summary>
    public float WaterSurfaceY =>
        (_waterLevel + HexMetrics.WaterElevationOffset) * HexMetrics.ElevationStep;

    // Tutorial 9: Feature properties

    /// <summary>
    /// Urban feature density level (0-3). 0 = none.
    /// </summary>
    public int UrbanLevel
    {
        get => _urbanLevel;
        set
        {
            if (_urbanLevel != value)
            {
                _urbanLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    /// <summary>
    /// Farm feature density level (0-3). 0 = none.
    /// </summary>
    public int FarmLevel
    {
        get => _farmLevel;
        set
        {
            if (_farmLevel != value)
            {
                _farmLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    /// <summary>
    /// Plant feature density level (0-3). 0 = none.
    /// </summary>
    public int PlantLevel
    {
        get => _plantLevel;
        set
        {
            if (_plantLevel != value)
            {
                _plantLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    // Tutorial 10: Wall properties

    /// <summary>
    /// Whether this cell is surrounded by walls.
    /// Walls are placed on the edges between walled and non-walled cells.
    /// </summary>
    public bool Walled
    {
        get => _walled;
        set
        {
            if (_walled != value)
            {
                _walled = value;
                Refresh();
            }
        }
    }

    // Tutorial 11: Special feature properties

    /// <summary>
    /// Index of special feature (0 = none, 1 = castle, 2 = ziggurat, 3 = megaflora).
    /// Tutorial 11.
    /// </summary>
    public int SpecialIndex
    {
        get => _specialIndex;
        set
        {
            if (_specialIndex != value)
            {
                _specialIndex = value;
                // Tutorial 11: Special features remove existing roads
                if (value > 0)
                {
                    RemoveRoads();
                }
                RefreshSelfOnly();
            }
        }
    }

    /// <summary>
    /// Returns true if this cell has a special feature.
    /// Tutorial 11.
    /// </summary>
    public bool IsSpecial => _specialIndex > 0;

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
    /// Uses WaterElevationOffset as rivers and water share the same surface offset.
    /// </summary>
    public float RiverSurfaceY =>
        (_elevation + HexMetrics.WaterElevationOffset) * HexMetrics.ElevationStep;

    /// <summary>
    /// Gets the direction of a river begin or end.
    /// Returns incoming direction if has incoming, otherwise outgoing.
    /// </summary>
    public HexDirection RiverBeginOrEndDirection =>
        _hasIncomingRiver ? _incomingRiver : _outgoingRiver;

    // Tutorial 7: Road properties

    /// <summary>
    /// Returns true if a road exists through the specified edge.
    /// </summary>
    public bool HasRoadThroughEdge(HexDirection direction)
    {
        return _roads[(int)direction];
    }

    /// <summary>
    /// Returns true if this cell has any roads.
    /// </summary>
    public bool HasRoads
    {
        get
        {
            for (int i = 0; i < _roads.Length; i++)
            {
                if (_roads[i]) return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Gets the absolute elevation difference to a neighbor.
    /// Returns int.MaxValue if neighbor is null.
    /// </summary>
    public int GetElevationDifference(HexDirection direction)
    {
        HexCell neighbor = GetNeighbor(direction);
        if (neighbor == null) return int.MaxValue;
        int difference = _elevation - neighbor.Elevation;
        return difference >= 0 ? difference : -difference;
    }

    /// <summary>
    /// Adds a road in the specified direction if valid.
    /// Roads cannot be placed where rivers exist, where elevation difference > 1,
    /// or in cells with special features (Tutorial 11).
    /// </summary>
    public void AddRoad(HexDirection direction)
    {
        if (!_roads[(int)direction] &&
            !HasRiverThroughEdge(direction) &&
            !IsSpecial &&  // Tutorial 11: No roads in special cells
            GetElevationDifference(direction) <= 1)
        {
            SetRoad((int)direction, true);
        }
    }

    /// <summary>
    /// Removes all roads from this cell.
    /// </summary>
    public void RemoveRoads()
    {
        for (int i = 0; i < _neighbors.Length; i++)
        {
            if (_roads[i])
            {
                SetRoad(i, false);
            }
        }
    }

    /// <summary>
    /// Sets a road in the specified direction, updating both cells.
    /// </summary>
    private void SetRoad(int index, bool state)
    {
        _roads[index] = state;
        HexCell neighbor = _neighbors[index];
        if (neighbor != null)
        {
            neighbor._roads[(int)((HexDirection)index).Opposite()] = state;
            neighbor.RefreshSelfOnly();
        }
        RefreshSelfOnly();
    }

    /// <summary>
    /// Validates roads after elevation change.
    /// Removes roads where elevation difference is now > 1.
    /// </summary>
    private void ValidateRoads()
    {
        for (int i = 0; i < _neighbors.Length; i++)
        {
            if (_roads[i] && GetElevationDifference((HexDirection)i) > 1)
            {
                SetRoad(i, false);
            }
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

        // Tutorial 11: Rivers override special features
        _specialIndex = 0;

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
    /// Rivers can flow to cells at the same elevation or lower,
    /// or into water bodies at matching water level.
    /// </summary>
    bool IsValidRiverDestination(HexCell neighbor)
    {
        return neighbor != null &&
            (_elevation >= neighbor.Elevation || _waterLevel == neighbor.Elevation);
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

    // Tutorial 16: Cell highlighting methods

    /// <summary>
    /// Enables the cell highlight with the specified color.
    /// Tutorial 16: Used for pathfinding visualization (blue=start, red=end, white=path).
    /// </summary>
    public void EnableHighlight(Color color)
    {
        if (_highlight == null)
        {
            CreateHighlight();
        }

        if (_highlight != null)
        {
            // Set the highlight color via shader parameter
            if (_highlight.MaterialOverride is ShaderMaterial shaderMat)
            {
                shaderMat.SetShaderParameter("highlight_color", color);
            }
            _highlight.Visible = true;
        }
    }

    /// <summary>
    /// Disables the cell highlight.
    /// Tutorial 16.
    /// </summary>
    public void DisableHighlight()
    {
        if (_highlight != null)
        {
            _highlight.Visible = false;
        }
    }

    /// <summary>
    /// Creates the highlight sprite for this cell.
    /// Tutorial 16: Lazily created on first EnableHighlight call.
    /// </summary>
    private void CreateHighlight()
    {
        // Load material if not already loaded
        if (_highlightMaterial == null)
        {
            _highlightMaterial = GD.Load<Material>("res://materials/highlight_material.tres");
        }

        if (_highlightMaterial == null)
        {
            GD.PrintErr("[HexCell] Failed to load highlight material");
            return;
        }

        _highlight = new Sprite3D();
        _highlight.Name = "Highlight";

        // Load the texture
        var texture = GD.Load<Texture2D>("res://textures/cell-outline.png");
        if (texture != null)
        {
            _highlight.Texture = texture;
        }

        // Each cell needs its own material instance to have independent colors
        _highlight.MaterialOverride = (Material)_highlightMaterial.Duplicate();

        // Position at cell center, slightly above terrain to avoid z-fighting
        // The sprite is positioned relative to the cell's position
        _highlight.Position = new Vector3(0, 0.01f, 0);

        // Rotate to lay flat on the ground (face up)
        _highlight.RotationDegrees = new Vector3(-90, 0, 0);

        // Scale to match hex cell size using PixelSize
        // PixelSize controls how many world units each pixel occupies
        _highlight.PixelSize = 0.2f; // Adjust for proper world scale

        _highlight.Visible = false;
        AddChild(_highlight);
    }

}
