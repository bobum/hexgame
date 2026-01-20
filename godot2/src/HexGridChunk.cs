using Godot;

/// <summary>
/// Manages a chunk of cells and their mesh.
/// Ported from Catlike Coding Hex Map Tutorial 5-6.
/// </summary>
public partial class HexGridChunk : Node3D
{
    HexCell[] _cells = null!;
    HexMesh _hexMesh = null!;
    HexMesh _riversMesh = null!;
    HexMesh _roadsMesh = null!;
    Node3D _labelContainer = null!;
    bool _needsRefresh;

    // Tutorial 6: River material loaded once
    private static Material? _riverMaterial;

    // Tutorial 7: Road material loaded once
    private static Material? _roadMaterial;

    /// <summary>
    /// Initializes the chunk with its mesh and label container.
    /// Call this after instantiation and after the chunk is in the scene tree.
    /// </summary>
    public void Initialize(Material? material = null)
    {
        _cells = new HexCell[HexMetrics.ChunkSizeX * HexMetrics.ChunkSizeZ];

        // Create terrain HexMesh child
        _hexMesh = new HexMesh();
        _hexMesh.Name = "HexMesh";
        _hexMesh.UseColors = true;
        _hexMesh.UseUVCoordinates = false;
        if (material != null)
        {
            _hexMesh.MaterialOverride = material;
        }
        AddChild(_hexMesh);
        _hexMesh.EnsureInitialized();

        // Tutorial 6: Create rivers HexMesh child
        _riversMesh = new HexMesh();
        _riversMesh.Name = "RiversMesh";
        _riversMesh.UseColors = false;
        _riversMesh.UseUVCoordinates = true;
        LoadRiverMaterial();
        if (_riverMaterial != null)
        {
            _riversMesh.MaterialOverride = _riverMaterial;
        }
        AddChild(_riversMesh);
        _riversMesh.EnsureInitialized();

        // Tutorial 7: Create roads HexMesh child
        _roadsMesh = new HexMesh();
        _roadsMesh.Name = "RoadsMesh";
        _roadsMesh.UseColors = false;
        _roadsMesh.UseUVCoordinates = true;
        // Use simple solid color material for debugging - brown roads
        var roadMat = new StandardMaterial3D();
        roadMat.AlbedoColor = new Color(0.6f, 0.4f, 0.2f);  // Brown
        roadMat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        roadMat.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        _roadsMesh.MaterialOverride = roadMat;
        GD.Print("[ROAD] Using simple brown StandardMaterial3D for roads");
        AddChild(_roadsMesh);
        _roadsMesh.EnsureInitialized();

        // Create label container
        _labelContainer = new Node3D();
        _labelContainer.Name = "Labels";
        AddChild(_labelContainer);
    }

    /// <summary>
    /// Loads the river material if not already loaded.
    /// </summary>
    private static void LoadRiverMaterial()
    {
        if (_riverMaterial == null)
        {
            _riverMaterial = GD.Load<Material>("res://materials/river_material.tres");
        }
    }

    /// <summary>
    /// Loads the road material if not already loaded.
    /// </summary>
    private static void LoadRoadMaterial()
    {
        if (_roadMaterial == null)
        {
            _roadMaterial = GD.Load<Material>("res://materials/road_material.tres");
            if (_roadMaterial != null)
            {
                GD.Print("[ROAD] Road material loaded successfully");
            }
            else
            {
                GD.PrintErr("[ROAD] FAILED to load road material!");
            }
        }
    }

    /// <summary>
    /// Adds a cell to this chunk at the specified local index.
    /// </summary>
    public void AddCell(int index, HexCell cell)
    {
        _cells[index] = cell;
        cell.Chunk = this;

        // Add label to this chunk's label container
        if (cell.UiLabel != null)
        {
            _labelContainer.AddChild(cell.UiLabel);
        }
    }

    /// <summary>
    /// Shows or hides the coordinate labels for all cells in this chunk.
    /// </summary>
    public void ShowUI(bool visible)
    {
        _labelContainer.Visible = visible;
    }

    /// <summary>
    /// Marks this chunk for mesh refresh on next frame.
    /// Uses Godot's SetProcess pattern as equivalent to Unity's LateUpdate.
    /// </summary>
    public void Refresh()
    {
        _needsRefresh = true;
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        if (_needsRefresh)
        {
            // Count cells with roads for debug
            int roadCells = 0;
            foreach (var cell in _cells) { if (cell?.HasRoads == true) roadCells++; }
            GD.Print($"CHUNK REFRESH: {Name} retriangulating {_cells.Length} cells, {roadCells} with roads");
            _hexMesh.Triangulate(_cells, _riversMesh, _roadsMesh);
            GD.Print($"CHUNK REFRESH: {Name} DONE - roads mesh vertices: {_roadsMesh.VertexCount}");
            _needsRefresh = false;
            SetProcess(false);
        }
    }
}
