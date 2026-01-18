using HexGame.Camera;
using HexGame.Core;
using HexGame.Generation;
using HexGame.Pathfinding;
using HexGame.Rendering;
using HexGame.Units;

namespace HexGame;

/// <summary>
/// Main entry point for HexGame - C# version.
/// Manages game initialization and main loop.
/// </summary>
public partial class Main : Node3D
{
    #region Node References

    private Node3D? _hexGridNode;
    private MapCamera? _camera;
    private DirectionalLight3D? _directionalLight;
    private WorldEnvironment? _worldEnvironment;

    #endregion

    #region Game Systems

    private HexGrid? _grid;
    private MapGenerator? _mapGenerator;
    private TerrainRenderer? _terrainRenderer;
    private FeatureRenderer? _featureRenderer;
    private WaterRenderer? _waterRenderer;
    private UnitManager? _unitManager;
    private UnitRenderer? _unitRenderer;
    private Pathfinder? _pathfinder;
    private MeshInstance3D? _groundPlane;

    #endregion

    #region Configuration

    [Export]
    public int MapWidth { get; set; } = 32;

    [Export]
    public int MapHeight { get; set; } = 32;

    private int _currentSeed;

    #endregion

    public override void _Ready()
    {
        GD.Print("HexGame (C#) starting...");

        // Uncap FPS
        DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
        Engine.MaxFps = 0;

        // Generate random seed
        _currentSeed = (int)GD.Randi();

        // Get node references
        SetupNodeReferences();

        // Initialize game
        InitializeGame();

        GD.Print("HexGame (C#) initialized successfully!");
    }

    private void SetupNodeReferences()
    {
        _hexGridNode = GetNodeOrNull<Node3D>("HexGrid");
        if (_hexGridNode == null)
        {
            _hexGridNode = new Node3D { Name = "HexGrid" };
            AddChild(_hexGridNode);
        }

        // Setup camera (direct port of map_camera.gd)
        _camera = new MapCamera { Name = "MapCamera" };
        AddChild(_camera);

        _directionalLight = GetNodeOrNull<DirectionalLight3D>("DirectionalLight3D");
        _worldEnvironment = GetNodeOrNull<WorldEnvironment>("WorldEnvironment");
    }

    private void InitializeGame()
    {
        // Initialize grid
        _grid = new HexGrid(MapWidth, MapHeight);
        _grid.Initialize();
        GD.Print($"Grid initialized: {MapWidth}x{MapHeight} cells");

        // Generate terrain
        _mapGenerator = new MapGenerator();
        _mapGenerator.Generate(_grid, _currentSeed);
        GD.Print($"Map generated with seed: {_currentSeed}");

        // Build and display terrain
        BuildTerrain();

        // Build features
        BuildFeatures();

        // Setup unit system
        SetupUnits();

        // Center camera on map
        CenterCamera();
    }

    #region Terrain Building

    private void BuildTerrain()
    {
        if (_hexGridNode == null || _grid == null) return;

        // Create terrain renderer
        _terrainRenderer = new TerrainRenderer(_grid);
        _hexGridNode.AddChild(_terrainRenderer);
        _terrainRenderer.Build();

        // Build ground plane
        BuildGroundPlane();

        // Build water
        BuildWater();
    }

    private void BuildGroundPlane()
    {
        if (_hexGridNode == null || _grid == null) return;

        // Remove existing ground plane
        _groundPlane?.QueueFree();
        _groundPlane = null;

        // Calculate map bounds
        var minCoords = new HexCoordinates(0, 0).ToWorldPosition(0);
        var maxCoords = new HexCoordinates(MapWidth - 1, MapHeight - 1).ToWorldPosition(0);

        float padding = 20f;
        float sizeX = maxCoords.X - minCoords.X + padding * 2;
        float sizeZ = maxCoords.Z - minCoords.Z + padding * 2;
        float centerX = (minCoords.X + maxCoords.X) / 2f;
        float centerZ = (minCoords.Z + maxCoords.Z) / 2f;
        float planeY = HexMetrics.MinElevation * HexMetrics.ElevationStep - 0.5f;

        // Create mesh
        _groundPlane = new MeshInstance3D();
        var planeMesh = new PlaneMesh
        {
            Size = new Vector2(sizeX, sizeZ)
        };
        _groundPlane.Mesh = planeMesh;

        // Create material
        var material = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.102f, 0.298f, 0.431f),
            Metallic = 0f,
            Roughness = 1f
        };
        _groundPlane.MaterialOverride = material;

        // Position
        _groundPlane.Position = new Vector3(centerX, planeY, centerZ);

        _hexGridNode.AddChild(_groundPlane);
    }

    private void BuildWater()
    {
        if (_hexGridNode == null || _grid == null) return;

        _waterRenderer?.QueueFree();
        _waterRenderer = null;

        _waterRenderer = new WaterRenderer(_grid);
        _hexGridNode.AddChild(_waterRenderer);
        _waterRenderer.Build();

        GD.Print("Water mesh added to scene");
    }

    private void BuildFeatures()
    {
        if (_hexGridNode == null || _grid == null) return;

        _featureRenderer?.QueueFree();
        _featureRenderer = null;

        _featureRenderer = new FeatureRenderer(_grid);
        _hexGridNode.AddChild(_featureRenderer);
        _featureRenderer.Build();
    }

    #endregion

    #region Unit System

    private void SetupUnits()
    {
        if (_hexGridNode == null || _grid == null) return;

        // Create unit manager
        _unitManager = new UnitManager(_grid);

        // Create unit renderer
        _unitRenderer = new UnitRenderer(_unitManager);
        _hexGridNode.AddChild(_unitRenderer);

        // Spawn test units
        for (int i = 0; i < 10; i++)
        {
            int q = (int)(GD.Randi() % (uint)MapWidth);
            int r = (int)(GD.Randi() % (uint)MapHeight);
            var cell = _grid.GetCell(q, r);
            if (cell != null && !cell.IsWater)
            {
                _unitManager.CreateUnit(UnitType.Infantry, q, r, 1);
            }
        }

        GD.Print($"Created {_unitManager.UnitCount} units");

        // Build unit meshes
        _unitRenderer.Build();

        // Setup pathfinder
        _pathfinder = new Pathfinder(_grid, _unitManager);
    }

    #endregion

    #region Camera

    private void CenterCamera()
    {
        if (_camera == null || _grid == null) return;

        var centerQ = MapWidth / 2;
        var centerR = MapHeight / 2;
        var centerCoords = new HexCoordinates(centerQ, centerR);
        var centerPos = centerCoords.ToWorldPosition(0);

        _camera.FocusOn(centerPos, 40f);
    }

    #endregion

    #region Process Loop

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        // Update water animation
        _waterRenderer?.UpdateAnimation(dt);
        if (_camera != null)
        {
            _waterRenderer?.UpdateVisibility(_camera);
        }

        // Update unit renderer
        _unitRenderer?.Update(delta);
        if (_camera != null)
        {
            _unitRenderer?.UpdateVisibility(_camera);
        }

        // Update terrain visibility
        if (_camera != null)
        {
            _terrainRenderer?.UpdateVisibility(_camera);
        }

        // Update feature visibility
        if (_camera != null)
        {
            _featureRenderer?.UpdateVisibility(_camera);
        }
    }

    #endregion

    #region Input

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            switch (keyEvent.Keycode)
            {
                case Key.Space:
                    GD.Print("Regenerating map with new seed...");
                    _currentSeed = (int)GD.Randi();
                    RegenerateMap();
                    break;

                case Key.G:
                    GD.Print("Regenerating map with same seed...");
                    RegenerateMap();
                    break;
            }
        }
    }

    #endregion

    #region Map Regeneration

    public void RegenerateMap()
    {
        CleanupRenderers();

        // Clear units
        _unitManager?.Clear();

        // Regenerate
        if (_mapGenerator != null && _grid != null)
        {
            _mapGenerator.Generate(_grid, _currentSeed);
            GD.Print($"Map generated with seed: {_currentSeed}");

            BuildTerrain();
            BuildFeatures();
            SetupUnits();
            CenterCamera();
        }
    }

    private void CleanupRenderers()
    {
        _terrainRenderer?.Dispose();
        _terrainRenderer?.QueueFree();
        _terrainRenderer = null;

        _waterRenderer?.Dispose();
        _waterRenderer?.QueueFree();
        _waterRenderer = null;

        _featureRenderer?.Dispose();
        _featureRenderer?.QueueFree();
        _featureRenderer = null;

        _groundPlane?.QueueFree();
        _groundPlane = null;

        _unitRenderer?.Dispose();
        _unitRenderer?.QueueFree();
        _unitRenderer = null;
    }

    #endregion

    #region Game Actions

    public HexCell? GetCellAtWorldPos(Vector3 worldPos)
    {
        if (_grid == null) return null;
        var coords = HexCoordinates.FromWorldPosition(worldPos);
        return _grid.GetCell(coords.Q, coords.R);
    }

    #endregion
}
