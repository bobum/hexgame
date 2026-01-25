using Godot;
using System.Collections.Generic;

/// <summary>
/// Creates and manages a hexagonal grid using chunks.
/// Ported from Catlike Coding Hex Map Tutorial 5.
/// Tutorial 15: Added Edit/Navigation mode toggle and pathfinding.
/// </summary>
public partial class HexGrid : Node3D
{
    [Export] public int ChunkCountX = 4;
    [Export] public int ChunkCountZ = 3;
    [Export] public PackedScene? CellPrefab;
    [Export] public PackedScene? CellLabelPrefab;
    [Export] public Texture2D? NoiseSource;
    [Export] public Material? HexMaterial;

    private int _cellCountX;
    private int _cellCountZ;
    private HexCell[] _cells = null!;
    private HexGridChunk[] _chunks = null!;
    private bool _labelsVisible = true;

    // Tutorial 14: Number of terrain types available
    private const int TerrainTypeCount = 5;

    // Tutorial 15: Edit vs Navigation mode
    private bool _editMode = true;
    private HexCell? _selectedCell;

    // Tutorial 15: Priority queue for Dijkstra's algorithm
    private HexCellPriorityQueue _searchFrontier = new HexCellPriorityQueue();
    private int _searchFrontierPhase;

    public override void _Ready()
    {
        // Initialize noise texture for perturbation
        InitializeNoiseSource();

        // Tutorial 9: Initialize hash grid for feature placement
        HexMetrics.InitializeHashGrid(1234);

        _cellCountX = ChunkCountX * HexMetrics.ChunkSizeX;
        _cellCountZ = ChunkCountZ * HexMetrics.ChunkSizeZ;

        CreateChunks();
        CreateCells();

        // Tutorial 6: Generate test rivers for visual verification
        GenerateTestRivers();

        // Tutorial 7: Generate test roads for visual verification
        GenerateTestRoads();

        // Tutorial 8: Generate test water bodies for visual verification
        GenerateTestWater();

        // Tutorial 9: Generate test features for visual verification
        GenerateTestFeatures();

        // Tutorial 10: Generate test walls for visual verification
        GenerateTestWalls();

        // Tutorial 11: Generate test bridges for visual verification
        GenerateTestBridges();

        // Tutorial 11: Generate test special features for visual verification
        GenerateTestSpecialFeatures();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == Key.L)
            {
                _labelsVisible = !_labelsVisible;
                ShowUI(_labelsVisible);
            }
            // Tutorial 15: Toggle Edit/Navigation mode with E key
            else if (keyEvent.Keycode == Key.E)
            {
                _editMode = !_editMode;
                SetEditMode(_editMode);
                GD.Print($"Mode: {(_editMode ? "Edit" : "Navigation")}");
            }
        }
        // Tutorial 15: Handle mouse clicks for navigation mode
        else if (@event is InputEventMouseButton mouseEvent &&
                 mouseEvent.Pressed &&
                 mouseEvent.ButtonIndex == MouseButton.Left &&
                 !_editMode)
        {
            HandleNavigationClick(mouseEvent);
        }
    }

    /// <summary>
    /// Sets the grid to edit or navigation mode.
    /// Tutorial 15: Navigation mode shows grid overlay and enables pathfinding.
    /// </summary>
    private void SetEditMode(bool editMode)
    {
        _editMode = editMode;

        // Toggle grid overlay
        SetGridVisible(!editMode);

        if (editMode)
        {
            // Clear distance labels when returning to edit mode
            ClearDistances();
        }
    }

    /// <summary>
    /// Sets the grid overlay visibility.
    /// Tutorial 15.
    /// </summary>
    public void SetGridVisible(bool visible)
    {
        // Update terrain material on all chunks directly
        int updated = 0;
        for (int i = 0; i < _chunks.Length; i++)
        {
            var chunk = _chunks[i];
            // Find the HexMesh child and update its material
            foreach (var child in chunk.GetChildren())
            {
                if (child is MeshInstance3D meshInstance && child.Name == "HexMesh")
                {
                    if (meshInstance.MaterialOverride is ShaderMaterial shaderMat)
                    {
                        shaderMat.SetShaderParameter("show_grid", visible);
                        updated++;
                    }
                }
            }
        }
        GD.Print($"[GRID] show_grid = {visible} (updated {updated} chunks)");
    }

    /// <summary>
    /// Handles mouse clicks in navigation mode.
    /// Tutorial 15: Calculates distances from clicked cell.
    /// </summary>
    private void HandleNavigationClick(InputEventMouseButton mouseEvent)
    {
        // Get camera for raycasting
        var camera = GetViewport().GetCamera3D();
        if (camera == null) return;

        // Raycast from mouse position
        var from = camera.ProjectRayOrigin(mouseEvent.Position);
        var dir = camera.ProjectRayNormal(mouseEvent.Position);
        var to = from + dir * 1000f;

        var spaceState = GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        var result = spaceState.IntersectRay(query);

        if (result.Count > 0)
        {
            Vector3 hitPos = (Vector3)result["position"];
            HexCell? cell = GetCell(hitPos);
            if (cell != null)
            {
                _selectedCell = cell;
                FindDistancesTo(cell);
            }
        }
    }

    /// <summary>
    /// Calculates distances from all cells to the given target cell using Dijkstra's algorithm.
    /// Tutorial 15: Includes movement obstacles and terrain costs.
    /// Movement costs: Road = 1, Flat = 5, Slope = 10, +1 per feature level.
    /// </summary>
    public void FindDistancesTo(HexCell cell)
    {
        // Clear previous distances
        ClearDistances();

        // Cannot pathfind from underwater cell
        if (cell.IsUnderwater)
        {
            return;
        }

        // Tutorial 15: Use search frontier phase to track visited cells
        _searchFrontierPhase++;
        _searchFrontier.Clear();

        cell.Distance = 0;
        cell.SearchPhase = _searchFrontierPhase;
        _searchFrontier.Enqueue(cell);

        while (_searchFrontier.Count > 0)
        {
            HexCell current = _searchFrontier.Dequeue();

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell? neighbor = current.GetNeighbor(d);
                if (neighbor == null)
                {
                    continue;
                }

                // Tutorial 15: Calculate movement cost (handles all obstacle checks)
                int moveCost = GetMoveCost(current, neighbor, d);
                if (moveCost < 0)
                {
                    continue; // Impassable
                }

                int distance = current.Distance + moveCost;
                if (neighbor.SearchPhase < _searchFrontierPhase)
                {
                    // First time visiting this cell
                    neighbor.Distance = distance;
                    neighbor.SearchPhase = _searchFrontierPhase;
                    neighbor.SearchHeuristic = 0; // No A* heuristic for simple distances
                    _searchFrontier.Enqueue(neighbor);
                }
                else if (distance < neighbor.Distance)
                {
                    // Found a shorter path
                    int oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    _searchFrontier.Change(neighbor, oldPriority);
                }
            }
        }
    }

    /// <summary>
    /// Calculates the movement cost to travel from one cell to a neighbor.
    /// Tutorial 15: Road = 1, Flat = 5, Slope = 10, +1 per feature level.
    /// Roads/bridges allow crossing water and walls.
    /// </summary>
    private int GetMoveCost(HexCell fromCell, HexCell toCell, HexDirection direction)
    {
        // Roads (including bridges) bypass most obstacles
        if (fromCell.HasRoadThroughEdge(direction))
        {
            return 1;
        }

        // Without a road, check for obstacles
        HexEdgeType edgeType = fromCell.GetEdgeType(toCell);
        if (edgeType == HexEdgeType.Cliff)
        {
            return -1; // Impassable without road
        }

        // Underwater cells impassable without road/bridge
        if (toCell.IsUnderwater)
        {
            return -1;
        }

        // Walls block movement without road
        if (fromCell.Walled != toCell.Walled)
        {
            return -1;
        }

        // Normal terrain cost
        int moveCost = edgeType == HexEdgeType.Flat ? 5 : 10;
        // Add cost for features in destination cell
        moveCost += toCell.UrbanLevel + toCell.FarmLevel + toCell.PlantLevel;
        return moveCost;
    }

    /// <summary>
    /// Clears all distance values and resets labels.
    /// Tutorial 15.
    /// </summary>
    public void ClearDistances()
    {
        for (int i = 0; i < _cells.Length; i++)
        {
            _cells[i].Distance = int.MaxValue;
        }
    }

    private void CreateChunks()
    {
        _chunks = new HexGridChunk[ChunkCountX * ChunkCountZ];

        for (int z = 0, i = 0; z < ChunkCountZ; z++)
        {
            for (int x = 0; x < ChunkCountX; x++)
            {
                HexGridChunk chunk = new HexGridChunk();
                chunk.Name = $"Chunk_{x}_{z}";
                _chunks[i++] = chunk;
                AddChild(chunk);
                chunk.Initialize(HexMaterial);
            }
        }
    }

    private void CreateCells()
    {
        _cells = new HexCell[_cellCountZ * _cellCountX];

        for (int z = 0, i = 0; z < _cellCountZ; z++)
        {
            for (int x = 0; x < _cellCountX; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    private void CreateCell(int x, int z, int i)
    {
        Vector3 position;
        position.X = (x + z * 0.5f - z / 2) * (HexMetrics.InnerRadius * 2f);
        position.Y = 0f;
        position.Z = z * (HexMetrics.OuterRadius * 1.5f);

        HexCell cell = CellPrefab!.Instantiate<HexCell>();
        _cells[i] = cell;
        cell.Position = position;
        cell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);

        // Establish neighbor connections
        if (x > 0)
        {
            cell.SetNeighbor(HexDirection.W, _cells[i - 1]);
        }
        if (z > 0)
        {
            if ((z & 1) == 0) // Even row
            {
                cell.SetNeighbor(HexDirection.SE, _cells[i - _cellCountX]);
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, _cells[i - _cellCountX - 1]);
                }
            }
            else // Odd row
            {
                cell.SetNeighbor(HexDirection.SW, _cells[i - _cellCountX]);
                if (x < _cellCountX - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, _cells[i - _cellCountX + 1]);
                }
            }
        }

        // Create coordinate label
        Label3D? label = null;
        if (CellLabelPrefab != null)
        {
            label = CellLabelPrefab.Instantiate<Label3D>();
            label.Position = new Vector3(position.X, 0.1f, position.Z);
            label.Text = cell.Coordinates.ToStringOnSeparateLines();
            label.RotationDegrees = new Vector3(-90, 0, 0);
            cell.UiLabel = label;
        }

        AddCellToChunk(x, z, cell);

        // Set initial values AFTER adding to chunk so Refresh works
        // Tutorial 14: Assign terrain type by chunk to visualize chunk boundaries
        int chunkX = x / HexMetrics.ChunkSizeX;
        int chunkZ = z / HexMetrics.ChunkSizeZ;
        int chunkIndex = chunkX + chunkZ * ChunkCountX;
        cell.TerrainTypeIndex = chunkIndex % TerrainTypeCount;
        cell.Elevation = (x + z) % 4;
    }

    private void AddCellToChunk(int x, int z, HexCell cell)
    {
        int chunkX = x / HexMetrics.ChunkSizeX;
        int chunkZ = z / HexMetrics.ChunkSizeZ;
        HexGridChunk chunk = _chunks[chunkX + chunkZ * ChunkCountX];

        int localX = x - chunkX * HexMetrics.ChunkSizeX;
        int localZ = z - chunkZ * HexMetrics.ChunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.ChunkSizeX, cell);
    }

    public HexCell? GetCell(Vector3 position)
    {
        position = ToLocal(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        return GetCell(coordinates);
    }

    public HexCell? GetCell(HexCoordinates coordinates)
    {
        int z = coordinates.Z;
        if (z < 0 || z >= _cellCountZ)
        {
            return null;
        }
        int x = coordinates.X + z / 2;
        if (x < 0 || x >= _cellCountX)
        {
            return null;
        }
        return _cells[x + z * _cellCountX];
    }

    public void ShowUI(bool visible)
    {
        for (int i = 0; i < _chunks.Length; i++)
        {
            _chunks[i].ShowUI(visible);
        }
    }

    /// <summary>
    /// Gets a cell by offset coordinates (x, z).
    /// Returns null if coordinates are out of bounds.
    /// </summary>
    public HexCell? GetCellByOffset(int x, int z)
    {
        if (x < 0 || x >= _cellCountX || z < 0 || z >= _cellCountZ)
        {
            return null;
        }
        return _cells[x + z * _cellCountX];
    }

    /// <summary>
    /// Generates test rivers for visual verification.
    /// Call this after the grid is created.
    /// </summary>
    public void GenerateTestRivers()
    {
        TestRiverGenerator.GenerateTestPatterns(GetCellByOffset);
    }

    /// <summary>
    /// Generates test roads for visual verification.
    /// Call this after the grid and rivers are created.
    /// </summary>
    public void GenerateTestRoads()
    {
        TestRoadGenerator.GenerateTestPatterns(GetCellByOffset);
    }

    /// <summary>
    /// Generates test water bodies for visual verification.
    /// Call this after the grid is created.
    /// </summary>
    public void GenerateTestWater()
    {
        TestWaterGenerator.GenerateTestPatterns(GetCellByOffset);
    }

    /// <summary>
    /// Generates test terrain features for visual verification.
    /// Call this after the grid and other test patterns are created.
    /// </summary>
    public void GenerateTestFeatures()
    {
        TestFeatureGenerator.GenerateTestPatterns(GetCellByOffset);
    }

    /// <summary>
    /// Generates test walls for visual verification.
    /// Call this after the grid and other test patterns are created.
    /// </summary>
    public void GenerateTestWalls()
    {
        TestWallGenerator.GenerateTestPatterns(GetCellByOffset);
    }

    /// <summary>
    /// Generates test bridges for visual verification.
    /// Call this after rivers and roads are created.
    /// Tutorial 11.
    /// </summary>
    public void GenerateTestBridges()
    {
        TestBridgeGenerator.GenerateTestPatterns(GetCellByOffset);
    }

    /// <summary>
    /// Generates test special features for visual verification.
    /// Call this after the grid is created.
    /// Tutorial 11.
    /// </summary>
    public void GenerateTestSpecialFeatures()
    {
        TestSpecialFeatureGenerator.GenerateTestPatterns(GetCellByOffset);
    }

    private void InitializeNoiseSource()
    {
        if (NoiseSource != null)
        {
            HexMetrics.NoiseSource = NoiseSource.GetImage();
            GD.Print($"Noise texture from export: {HexMetrics.NoiseSource.GetWidth()}x{HexMetrics.NoiseSource.GetHeight()}");
        }
        else
        {
            // Load noise texture (same texture used in Catlike Coding tutorial)
            var texture = GD.Load<Texture2D>("res://assets/noise.png");
            if (texture != null)
            {
                HexMetrics.NoiseSource = texture.GetImage();
                GD.Print($"Noise texture loaded: {HexMetrics.NoiseSource.GetWidth()}x{HexMetrics.NoiseSource.GetHeight()}");
            }
            else
            {
                GD.PrintErr("CRITICAL: Failed to load noise texture - perturbation will NOT work!");
            }
        }

        // Verify noise is working
        if (HexMetrics.NoiseSource == null)
        {
            GD.PrintErr("CRITICAL: NoiseSource is null after initialization!");
        }
    }
}
