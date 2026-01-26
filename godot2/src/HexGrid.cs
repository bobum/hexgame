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

    /// <summary>
    /// When true, generates test patterns (rivers, roads, water, features, etc.) at startup.
    /// Set to false for production or when loading saved maps.
    /// </summary>
    [Export] public bool GenerateTestData = true;

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

    // Tutorial 15/16: Priority queue for pathfinding
    private HexCellPriorityQueue _searchFrontier = new HexCellPriorityQueue();
    private int _searchFrontierPhase;

    // Tutorial 16: Two-point pathfinding
    private HexCell? _searchFromCell;
    private HexCell? _searchToCell;
    private bool _currentPathExists;

    // Tutorial 16: Ground plane for navigation raycasting
    private StaticBody3D? _groundPlane;

    // Debug: On-screen label for debugging
    private Label? _debugLabel;

    // Tutorial 16: Cell highlighting using hex outline texture
    // Blue = start cell, Red = destination cell, White = path cells
    private MeshInstance3D? _startHighlight;
    private MeshInstance3D? _endHighlight;
    private List<MeshInstance3D> _pathHighlights = new();

    // Tutorial 16: Cached hex mesh and material for highlights (shared across instances)
    private static ArrayMesh? _hexHighlightMesh;
    private static ShaderMaterial? _highlightBaseMaterial;

    public override void _Ready()
    {
        // Initialize noise texture for perturbation
        InitializeNoiseSource();

        // Tutorial 9: Initialize hash grid for feature placement
        HexMetrics.InitializeHashGrid(1234);

        // Pre-load all materials and prefabs before creating chunks for better performance
        HexGridChunk.PreloadMaterials();
        HexFeatureManager.PreloadPrefabs();

        _cellCountX = ChunkCountX * HexMetrics.ChunkSizeX;
        _cellCountZ = ChunkCountZ * HexMetrics.ChunkSizeZ;

        CreateChunks();

        // Suppress chunk refreshes during batch initialization
        // This prevents ~1800+ redundant mesh rebuilds during setup
        SetChunkRefreshSuppression(true);

        CreateCells();

        if (GenerateTestData)
        {
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

        // Re-enable refreshes and trigger one final refresh per chunk
        SetChunkRefreshSuppression(false);
        RefreshAllChunks();

        // Tutorial 16: Create ground plane for navigation raycasting
        CreateGroundPlane();

        // Debug: Create on-screen debug label
        CreateDebugLabel();
    }

    /// <summary>
    /// Creates an invisible ground plane for navigation mode raycasting.
    /// Tutorial 16: Required for mouse picking in pathfinding.
    /// </summary>
    private void CreateGroundPlane()
    {
        _groundPlane = new StaticBody3D();
        _groundPlane.Name = "NavigationGroundPlane";
        AddChild(_groundPlane);

        var collisionShape = new CollisionShape3D();
        var shape = new WorldBoundaryShape3D();
        shape.Plane = new Plane(Vector3.Up, 0f);
        collisionShape.Shape = shape;
        _groundPlane.AddChild(collisionShape);

        GD.Print("[HexGrid] Navigation ground plane created for raycasting");
    }

    /// <summary>
    /// Creates an on-screen debug label for visual feedback.
    /// </summary>
    private void CreateDebugLabel()
    {
        var canvas = new CanvasLayer();
        canvas.Name = "DebugCanvas";
        AddChild(canvas);

        _debugLabel = new Label();
        _debugLabel.Name = "DebugLabel";
        _debugLabel.Position = new Vector2(10, 10);
        _debugLabel.Size = new Vector2(600, 200);
        _debugLabel.AddThemeColorOverride("font_color", new Color(1, 1, 0)); // Yellow
        _debugLabel.AddThemeFontSizeOverride("font_size", 18);
        _debugLabel.Text = "Debug: Ready (Press E for Nav mode, then Shift+Click)";
        canvas.AddChild(_debugLabel);
    }

    /// <summary>
    /// Updates the on-screen debug label.
    /// </summary>
    private void DebugLog(string message)
    {
        GD.Print(message);
        if (_debugLabel != null)
        {
            _debugLabel.Text = message;
        }
    }

    /// <summary>
    /// Sets the refresh suppression state for all chunks.
    /// Used during batch initialization to prevent redundant mesh rebuilds.
    /// </summary>
    private void SetChunkRefreshSuppression(bool suppress)
    {
        for (int i = 0; i < _chunks.Length; i++)
        {
            _chunks[i].SuppressRefresh = suppress;
        }
    }

    /// <summary>
    /// Triggers a refresh on all chunks. Called once after batch initialization.
    /// </summary>
    private void RefreshAllChunks()
    {
        for (int i = 0; i < _chunks.Length; i++)
        {
            _chunks[i].Refresh();
        }
    }

    public override void _Input(InputEvent @event)
    {
        // Debug: Log all mouse button events
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            DebugLog($"Click! editMode={_editMode}, shift={mb.ShiftPressed}");
        }

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
                DebugLog($"Mode: {(_editMode ? "EDIT" : "NAVIGATION")} - Now {(_editMode ? "press E again" : "Shift+Click to set start")}");
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
    /// Tutorial 15/16: Navigation mode shows grid overlay and enables pathfinding.
    /// </summary>
    private void SetEditMode(bool editMode)
    {
        _editMode = editMode;

        // Toggle grid overlay
        SetGridVisible(!editMode);

        if (editMode)
        {
            // Returning to edit mode
            ClearPath();
            _searchFromCell = null;
            _searchToCell = null;
            // Restore coordinate labels
            RestoreCoordinateLabels();
            // Hide start/end highlights
            if (_startHighlight != null) _startHighlight.Visible = false;
            if (_endHighlight != null) _endHighlight.Visible = false;
        }
        else
        {
            // Entering navigation mode - clear coordinate labels (distances will fill them)
            ClearDistances();
        }
    }

    /// <summary>
    /// Restores coordinate labels on all cells.
    /// Called when returning to edit mode.
    /// </summary>
    private void RestoreCoordinateLabels()
    {
        for (int i = 0; i < _cells.Length; i++)
        {
            var cell = _cells[i];
            if (cell.UiLabel != null)
            {
                cell.UiLabel.Text = cell.Coordinates.ToStringOnSeparateLines();
            }
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
        HexDebug.PrintMaterial($"[GRID] show_grid = {visible} (updated {updated} chunks)");
    }

    /// <summary>
    /// Handles mouse clicks in navigation mode.
    /// Tutorial 16: Shift+Click sets start cell, regular click sets destination.
    /// </summary>
    private void HandleNavigationClick(InputEventMouseButton mouseEvent)
    {
        DebugLog($"NavClick: Shift={mouseEvent.ShiftPressed}");

        // Get camera for raycasting
        var camera = GetViewport().GetCamera3D();
        if (camera == null)
        {
            DebugLog("ERROR: No camera!");
            return;
        }

        // Raycast from mouse position
        var from = camera.ProjectRayOrigin(mouseEvent.Position);
        var dir = camera.ProjectRayNormal(mouseEvent.Position);
        var to = from + dir * 1000f;

        var spaceState = GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        var result = spaceState.IntersectRay(query);

        if (result.Count == 0)
        {
            DebugLog("Raycast hit nothing!");
            return;
        }

        Vector3 hitPos = (Vector3)result["position"];
        HexCell? cell = GetCell(hitPos);
        if (cell == null)
        {
            DebugLog($"No cell at {hitPos}");
            return;
        }

        // Tutorial 16: Shift+Click for start cell, regular click for destination
        bool shiftHeld = mouseEvent.ShiftPressed;

        if (shiftHeld)
        {
            // Set start cell
            DebugLog($"START: {cell.Coordinates}");
            _searchFromCell = cell;

            // Create/move start highlight using cell's stored position (has correct elevation)
            if (_startHighlight == null)
            {
                _startHighlight = CreateHighlightHex(new Color(0, 0, 1)); // Blue
                AddChild(_startHighlight);
            }
            Vector3 startPos = cell.Position;
            _startHighlight.GlobalPosition = new Vector3(startPos.X, startPos.Y + 0.1f, startPos.Z);
            _startHighlight.Visible = true;

            if (_searchToCell != null)
            {
                FindPath(_searchFromCell, _searchToCell);
            }
        }
        else if (_searchFromCell != null && _searchFromCell != cell)
        {
            // Set destination cell
            DebugLog($"END: {cell.Coordinates}");
            _searchToCell = cell;

            // Create/move end highlight using cell's stored position (has correct elevation)
            if (_endHighlight == null)
            {
                _endHighlight = CreateHighlightHex(new Color(1, 0, 0)); // Red
                AddChild(_endHighlight);
            }
            Vector3 endPos = cell.Position;
            _endHighlight.GlobalPosition = new Vector3(endPos.X, endPos.Y + 0.1f, endPos.Z);
            _endHighlight.Visible = true;

            FindPath(_searchFromCell, _searchToCell);
        }
        else
        {
            DebugLog($"Click ignored - Shift+Click first to set start");
        }
    }

    /// <summary>
    /// Creates a hex-shaped mesh for highlighting cells.
    /// Tutorial 16: Uses the cell-outline texture with proper UV mapping.
    /// </summary>
    private MeshInstance3D CreateHighlightHex(Color color)
    {
        // Ensure hex mesh is created (once, shared by all highlights)
        if (_hexHighlightMesh == null)
        {
            _hexHighlightMesh = CreateHexHighlightMesh();
        }

        // Load base material if not already loaded
        if (_highlightBaseMaterial == null)
        {
            _highlightBaseMaterial = GD.Load<ShaderMaterial>("res://materials/highlight_material.tres");
        }

        var meshInstance = new MeshInstance3D();
        meshInstance.Mesh = _hexHighlightMesh;

        // Duplicate material so each highlight can have its own color
        var material = (ShaderMaterial)_highlightBaseMaterial.Duplicate();
        material.SetShaderParameter("highlight_color", color);
        meshInstance.MaterialOverride = material;

        // Rotate to lie flat on the ground (hex mesh is created in XZ plane)
        meshInstance.Visible = false;
        return meshInstance;
    }

    /// <summary>
    /// Creates a flat hexagonal mesh with UV coordinates for the outline texture.
    /// Tutorial 16: Matches Catlike Coding's hex cell outline approach.
    /// </summary>
    private static ArrayMesh CreateHexHighlightMesh()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        // Use slightly smaller radius so outline fits within cell
        float radius = HexMetrics.OuterRadius * 0.95f;
        var center = Vector3.Zero;

        // Create 6 triangles forming a hexagon
        // Hex corners are at 30, 90, 150, 210, 270, 330 degrees (pointy-top hex)
        for (int i = 0; i < 6; i++)
        {
            // Angles for pointy-top hex (starts at 30 degrees = PI/6)
            float angle1 = Mathf.Pi / 6f + (Mathf.Pi / 3f) * i;
            float angle2 = Mathf.Pi / 6f + (Mathf.Pi / 3f) * (i + 1);

            var corner1 = new Vector3(
                Mathf.Cos(angle1) * radius,
                0,
                Mathf.Sin(angle1) * radius
            );
            var corner2 = new Vector3(
                Mathf.Cos(angle2) * radius,
                0,
                Mathf.Sin(angle2) * radius
            );

            // UV coordinates map the hex to the texture
            // Center of texture is (0.5, 0.5), corners map to hex edges
            var uvCenter = new Vector2(0.5f, 0.5f);
            var uv1 = new Vector2(
                0.5f + 0.5f * Mathf.Cos(angle1),
                0.5f + 0.5f * Mathf.Sin(angle1)
            );
            var uv2 = new Vector2(
                0.5f + 0.5f * Mathf.Cos(angle2),
                0.5f + 0.5f * Mathf.Sin(angle2)
            );

            // Add triangle (center, corner1, corner2)
            st.SetNormal(Vector3.Up);
            st.SetUV(uvCenter);
            st.AddVertex(center);

            st.SetNormal(Vector3.Up);
            st.SetUV(uv1);
            st.AddVertex(corner1);

            st.SetNormal(Vector3.Up);
            st.SetUV(uv2);
            st.AddVertex(corner2);
        }

        return st.Commit();
    }

    // Tutorial 16: Two-point pathfinding methods

    /// <summary>
    /// Finds a path between two cells using A* algorithm.
    /// Tutorial 16: Highlights start (blue), end (red), and path (white).
    /// </summary>
    public void FindPath(HexCell fromCell, HexCell toCell)
    {
        // Clear any previous path
        ClearPath();

        DebugLog($"FindPath: {fromCell.Coordinates} -> {toCell.Coordinates}");

        // Run A* search
        _currentPathExists = Search(fromCell, toCell);

        // Show the path if found
        if (_currentPathExists)
        {
            // Debug: Check PathFrom chain
            DebugLog($"Path exists! toCell.PathFrom = {toCell.PathFrom?.Coordinates}");
            int pathLength = ShowPathWithBoxes();
            DebugLog($"PATH: {pathLength} cells, {_pathHighlights.Count} boxes");
        }
        else
        {
            DebugLog($"NO PATH: {fromCell.Coordinates} to {toCell.Coordinates}");
        }
    }

    /// <summary>
    /// Performs A* search from start to destination.
    /// Tutorial 16: Uses SearchHeuristic for informed search.
    /// </summary>
    /// <returns>True if a path was found, false otherwise.</returns>
    private bool Search(HexCell fromCell, HexCell toCell)
    {
        _searchFrontierPhase++;
        _searchFrontier.Clear();

        fromCell.Distance = 0;
        fromCell.SearchPhase = _searchFrontierPhase;
        _searchFrontier.Enqueue(fromCell);

        int explored = 0;
        while (_searchFrontier.Count > 0)
        {
            HexCell current = _searchFrontier.Dequeue();
            explored++;

            // Early exit when destination reached
            if (current == toCell)
            {
                DebugLog($"Search: found after {explored} cells");
                return true;
            }

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell? neighbor = current.GetNeighbor(d);
                if (neighbor == null)
                {
                    continue;
                }

                // Skip cells already fully processed in this search
                if (neighbor.SearchPhase > _searchFrontierPhase)
                {
                    continue;
                }

                // Calculate movement cost
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
                    neighbor.PathFrom = current;
                    neighbor.SearchHeuristic =
                        neighbor.Coordinates.DistanceTo(toCell.Coordinates);
                    neighbor.SearchPhase = _searchFrontierPhase;
                    _searchFrontier.Enqueue(neighbor);
                }
                else if (distance < neighbor.Distance)
                {
                    // Found a shorter path to this cell
                    int oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    _searchFrontier.Change(neighbor, oldPriority);
                }
            }
        }

        DebugLog($"Search: exhausted after {explored} cells, no path");
        return false; // No path found
    }

    /// <summary>
    /// Highlights the found path using visible boxes.
    /// Returns the path length.
    /// </summary>
    private int ShowPathWithBoxes()
    {
        if (_searchToCell == null || _searchFromCell == null)
        {
            DebugLog("ShowPath: null cells!");
            return 0;
        }

        int pathLength = 0;

        // Backtrace from destination to start, creating white hex highlights
        HexCell? current = _searchToCell.PathFrom;
        DebugLog($"ShowPath: starting from PathFrom={current?.Coordinates}");

        while (current != null && current != _searchFromCell)
        {
            DebugLog($"ShowPath: hex at {current.Coordinates}");
            // Create a white hex highlight at this cell's position
            var hex = CreateHighlightHex(new Color(1, 1, 1)); // White
            AddChild(hex);

            // Use cell.Position directly - it has correct elevation with noise
            Vector3 cellPos = current.Position;
            hex.GlobalPosition = new Vector3(cellPos.X, cellPos.Y + 0.1f, cellPos.Z);
            hex.Visible = true;

            _pathHighlights.Add(hex);
            pathLength++;
            current = current.PathFrom;

            // Safety check to prevent infinite loops
            if (pathLength > 100)
            {
                DebugLog("ShowPath: too many steps, breaking");
                break;
            }
        }

        DebugLog($"ShowPath: created {pathLength} hexes");
        return pathLength + 2; // +2 for start and end cells
    }

    /// <summary>
    /// Clears the current path visualization.
    /// </summary>
    private void ClearPath()
    {
        // Clear path hex highlights
        foreach (var hex in _pathHighlights)
        {
            hex.QueueFree();
        }
        _pathHighlights.Clear();

        _currentPathExists = false;
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
            HexDebug.PrintMaterial($"Noise texture from export: {HexMetrics.NoiseSource.GetWidth()}x{HexMetrics.NoiseSource.GetHeight()}");
        }
        else
        {
            // Load noise texture (same texture used in Catlike Coding tutorial)
            var texture = GD.Load<Texture2D>("res://assets/noise.png");
            if (texture != null)
            {
                HexMetrics.NoiseSource = texture.GetImage();
                HexDebug.PrintMaterial($"Noise texture loaded: {HexMetrics.NoiseSource.GetWidth()}x{HexMetrics.NoiseSource.GetHeight()}");
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
