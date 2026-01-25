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

    // Tutorial 8: Water meshes
    HexMesh _waterMesh = null!;
    HexMesh _waterShoreMesh = null!;
    HexMesh _estuariesMesh = null!;

    // Tutorial 9: Feature manager
    HexFeatureManager _features = null!;

    // Tutorial 6: River material loaded once
    private static Material? _riverMaterial;

    // Tutorial 7: Road material loaded once
    private static Material? _roadMaterial;

    // Tutorial 8: Water materials loaded once
    private static Material? _waterMaterial;
    private static Material? _waterShoreMaterial;
    private static Material? _estuaryMaterial;

    // Tutorial 14: Terrain material with texture array
    private static Material? _terrainMaterial;
    private static bool _terrainTexturesApplied;

    /// <summary>
    /// Initializes the chunk with its mesh and label container.
    /// Call this after instantiation and after the chunk is in the scene tree.
    /// </summary>
    public void Initialize(Material? material = null)
    {
        _cells = new HexCell[HexMetrics.ChunkSizeX * HexMetrics.ChunkSizeZ];

        // Create terrain HexMesh child
        // Tutorial 14: Enable terrain types for texture array support
        _hexMesh = new HexMesh();
        _hexMesh.Name = "HexMesh";
        _hexMesh.UseColors = true;  // Colors are now splat weights
        _hexMesh.UseUVCoordinates = false;
        _hexMesh.UseTerrainTypes = true;  // Tutorial 14
        LoadTerrainMaterial();
        if (_terrainMaterial != null)
        {
            _hexMesh.MaterialOverride = _terrainMaterial;
        }
        else if (material != null)
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
        LoadRoadMaterial();
        if (_roadMaterial != null)
        {
            _roadsMesh.MaterialOverride = _roadMaterial;
        }
        AddChild(_roadsMesh);
        _roadsMesh.EnsureInitialized();

        // Tutorial 8: Create water HexMesh child
        _waterMesh = new HexMesh();
        _waterMesh.Name = "WaterMesh";
        _waterMesh.UseColors = false;
        _waterMesh.UseUVCoordinates = false;
        LoadWaterMaterial();
        if (_waterMaterial != null)
        {
            _waterMesh.MaterialOverride = _waterMaterial;
        }
        AddChild(_waterMesh);
        _waterMesh.EnsureInitialized();

        // Tutorial 8: Create water shore HexMesh child
        _waterShoreMesh = new HexMesh();
        _waterShoreMesh.Name = "WaterShoreMesh";
        _waterShoreMesh.UseColors = false;
        _waterShoreMesh.UseUVCoordinates = true;
        LoadWaterShoreMaterial();
        if (_waterShoreMaterial != null)
        {
            _waterShoreMesh.MaterialOverride = _waterShoreMaterial;
        }
        AddChild(_waterShoreMesh);
        _waterShoreMesh.EnsureInitialized();

        // Tutorial 8: Create estuaries HexMesh child
        _estuariesMesh = new HexMesh();
        _estuariesMesh.Name = "EstuariesMesh";
        _estuariesMesh.UseColors = false;
        _estuariesMesh.UseUVCoordinates = true;
        _estuariesMesh.UseUV2Coordinates = true;
        LoadEstuaryMaterial();
        if (_estuaryMaterial != null)
        {
            _estuariesMesh.MaterialOverride = _estuaryMaterial;
        }
        AddChild(_estuariesMesh);
        _estuariesMesh.EnsureInitialized();

        // Tutorial 9: Create feature manager
        _features = new HexFeatureManager();
        _features.Name = "Features";
        AddChild(_features);
        _features.Initialize();

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
    /// Loads the water material if not already loaded.
    /// </summary>
    private static void LoadWaterMaterial()
    {
        if (_waterMaterial == null)
        {
            _waterMaterial = GD.Load<Material>("res://materials/water_material.tres");
        }
    }

    /// <summary>
    /// Loads the water shore material if not already loaded.
    /// </summary>
    private static void LoadWaterShoreMaterial()
    {
        if (_waterShoreMaterial == null)
        {
            _waterShoreMaterial = GD.Load<Material>("res://materials/water_shore_material.tres");
        }
    }

    /// <summary>
    /// Loads the estuary material if not already loaded.
    /// </summary>
    private static void LoadEstuaryMaterial()
    {
        if (_estuaryMaterial == null)
        {
            _estuaryMaterial = GD.Load<Material>("res://materials/estuary_material.tres");
        }
    }

    /// <summary>
    /// Loads the terrain material if not already loaded.
    /// Tutorial 14: Uses terrain shader with texture array.
    /// </summary>
    private static void LoadTerrainMaterial()
    {
        if (_terrainMaterial == null)
        {
            _terrainMaterial = GD.Load<Material>("res://materials/terrain_material.tres");
            if (_terrainMaterial != null)
            {
                GD.Print("[TERRAIN] Terrain material loaded successfully");
            }
            else
            {
                GD.PrintErr("[TERRAIN] Failed to load terrain material - falling back to default");
            }
        }

        // Tutorial 14: Load and assign the terrain texture array (separate from material loading)
        if (_terrainMaterial != null && !_terrainTexturesApplied)
        {
            var textureArray = TerrainTextureArray.GetTextureArray();
            if (textureArray != null && _terrainMaterial is ShaderMaterial shaderMat)
            {
                shaderMat.SetShaderParameter("terrain_textures", textureArray);
                shaderMat.SetShaderParameter("use_textures", true);
                shaderMat.SetShaderParameter("debug_mode", 0);
                _terrainTexturesApplied = true;
                GD.Print("[TERRAIN] Texture array assigned and textures enabled");
            }
            else if (textureArray == null)
            {
                GD.Print("[TERRAIN] Using color fallback (texture array failed to load)");
                _terrainTexturesApplied = true; // Don't retry
            }
            else
            {
                GD.Print("[TERRAIN] Using color fallback (material is not ShaderMaterial)");
                _terrainTexturesApplied = true;
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
            // Tutorial 9: Clear features before triangulation
            _features.Clear();

            _hexMesh.Triangulate(
                _cells,
                _riversMesh,
                _roadsMesh,
                _waterMesh,
                _waterShoreMesh,
                _estuariesMesh,
                _features
            );

            // Tutorial 9: Apply features after triangulation
            _features.Apply();

            _needsRefresh = false;
            SetProcess(false);
        }
    }
}
