using Godot;

/// <summary>
/// Manages a chunk of cells and their mesh.
/// Ported from Catlike Coding Hex Map Tutorial 5.
/// </summary>
public partial class HexGridChunk : Node3D
{
    HexCell[] _cells = null!;
    HexMesh _hexMesh = null!;
    Node3D _labelContainer = null!;
    bool _needsRefresh;

    /// <summary>
    /// Initializes the chunk with its mesh and label container.
    /// Call this after instantiation and after the chunk is in the scene tree.
    /// </summary>
    public void Initialize(Material? material = null)
    {
        _cells = new HexCell[HexMetrics.ChunkSizeX * HexMetrics.ChunkSizeZ];

        // Create HexMesh child
        _hexMesh = new HexMesh();
        _hexMesh.Name = "HexMesh";
        if (material != null)
        {
            _hexMesh.MaterialOverride = material;
        }
        AddChild(_hexMesh);
        // Explicitly initialize - don't rely on deferred _Ready() timing
        _hexMesh.EnsureInitialized();

        // Create label container
        _labelContainer = new Node3D();
        _labelContainer.Name = "Labels";
        AddChild(_labelContainer);
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
            GD.Print($"CHUNK REFRESH: {Name} retriangulating {_cells.Length} cells");
            _hexMesh.Triangulate(_cells);
            _needsRefresh = false;
            SetProcess(false);
        }
    }
}
