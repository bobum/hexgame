using HexGame.Camera;
using HexGame.Core;
using HexGame.Debug;
using HexGame.GameState;
using HexGame.Generation;
using HexGame.Interaction;
using HexGame.Pathfinding;
using HexGame.Rendering;
using HexGame.UI;
using HexGame.Units;

namespace HexGame;

/// <summary>
/// Main entry point for HexGame - C# version.
/// Direct port of main.gd with full feature parity.
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
    private ChunkedTerrainRenderer? _chunkedTerrain;
    private FeatureRenderer? _featureRenderer;
    private WaterRenderer? _chunkedWater;
    private ChunkedRiverRenderer? _chunkedRivers;
    private MeshInstance3D? _groundPlane;
    private HexHover? _hexHover;
    private GameUI? _gameUI;

    // Unit system
    private UnitManager? _unitManager;
    private UnitRenderer? _unitRenderer;
    private SelectionManager? _selectionManager;
    private Pathfinder? _pathfinder;
    private PathRenderer? _pathRenderer;
    private TurnManager? _turnManager;
    private PerformanceMonitor? _performanceMonitor;

    #endregion

    #region Configuration

    [Export]
    public int MapWidth { get; set; } = 32;

    [Export]
    public int MapHeight { get; set; } = 32;

    private int _currentSeed;

    // Async generation
    private bool _useAsyncGeneration = true;
    private bool _asyncGenerationPending;
    private bool _asyncNeedsNewUnits;

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

        // Setup UI
        SetupUI();

        // Initialize game
        InitializeGame();

        // Setup performance monitor
        SetupPerformanceMonitor();

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

        // Setup camera
        _camera = new MapCamera { Name = "MapCamera" };
        AddChild(_camera);

        _directionalLight = GetNodeOrNull<DirectionalLight3D>("DirectionalLight3D");
        _worldEnvironment = GetNodeOrNull<WorldEnvironment>("WorldEnvironment");
    }

    private void SetupUI()
    {
        _gameUI = new GameUI();
        AddChild(_gameUI);
        _gameUI.SetMainNode(this);
        _gameUI.SetSeed(_currentSeed);

        // Connect signals using Godot's Connect method for proper interop
        _gameUI.Connect(GameUI.SignalName.RegenerateRequested, Callable.From<int, int, int>(OnUIRegenerate));
        _gameUI.Connect(GameUI.SignalName.RandomSeedRequested, Callable.From(OnUIRandomSeed));
        _gameUI.Connect(GameUI.SignalName.EndTurnRequested, Callable.From(OnEndTurn));
        _gameUI.Connect(GameUI.SignalName.SpawnLandRequested, Callable.From<int>(OnSpawnLand));
        _gameUI.Connect(GameUI.SignalName.SpawnNavalRequested, Callable.From<int>(OnSpawnNaval));
        _gameUI.Connect(GameUI.SignalName.SpawnAiRequested, Callable.From<int, int>(OnSpawnAi));
        _gameUI.Connect(GameUI.SignalName.ClearUnitsRequested, Callable.From(OnClearUnits));
        _gameUI.Connect(GameUI.SignalName.NoiseParamChanged, Callable.From<string, float>(OnNoiseParamChanged));
        _gameUI.Connect(GameUI.SignalName.ShaderParamChanged, Callable.From<string, float>(OnShaderParamChanged));
        _gameUI.Connect(GameUI.SignalName.LightingParamChanged, Callable.From<string, float>(OnLightingParamChanged));
        _gameUI.Connect(GameUI.SignalName.FogParamChanged, Callable.From<string, float>(OnFogParamChanged));
        _gameUI.Connect(GameUI.SignalName.WaterParamChanged, Callable.From<string, float>(OnWaterParamChanged));
        _gameUI.Connect(GameUI.SignalName.AsyncToggleChanged, Callable.From<bool>(OnAsyncToggleChanged));
    }

    private void SetupPerformanceMonitor()
    {
        _performanceMonitor = new PerformanceMonitor();
        AddChild(_performanceMonitor);
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

        // Setup hover system
        SetupHover();

        // Setup unit system
        SetupUnits();

        // Center camera on map
        CenterCamera();
    }

    #region Terrain Building

    private void BuildTerrain()
    {
        if (_hexGridNode == null || _grid == null) return;

        // Create chunked terrain renderer
        _chunkedTerrain = new ChunkedTerrainRenderer();
        _hexGridNode.AddChild(_chunkedTerrain);
        _chunkedTerrain.Build(_grid);

        // Build ground plane
        BuildGroundPlane();

        // Build water
        BuildWater();

        // Build rivers
        BuildRivers();
    }

    private void BuildGroundPlane()
    {
        if (_hexGridNode == null || _grid == null) return;

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

        _groundPlane = new MeshInstance3D();
        var planeMesh = new PlaneMesh
        {
            Size = new Vector2(sizeX, sizeZ)
        };
        _groundPlane.Mesh = planeMesh;

        var material = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.102f, 0.298f, 0.431f),
            Metallic = 0f,
            Roughness = 1f
        };
        _groundPlane.MaterialOverride = material;
        _groundPlane.Position = new Vector3(centerX, planeY, centerZ);

        _hexGridNode.AddChild(_groundPlane);
    }

    private void BuildWater()
    {
        if (_hexGridNode == null || _grid == null) return;

        _chunkedWater?.Dispose();
        _chunkedWater?.QueueFree();
        _chunkedWater = null;

        _chunkedWater = new WaterRenderer(_grid);
        _hexGridNode.AddChild(_chunkedWater);
        _chunkedWater.Build();

        GD.Print("Water mesh added to scene");
    }

    private void BuildRivers()
    {
        if (_hexGridNode == null || _grid == null) return;

        _chunkedRivers?.Dispose();
        _chunkedRivers?.QueueFree();
        _chunkedRivers = null;

        _chunkedRivers = new ChunkedRiverRenderer();
        _hexGridNode.AddChild(_chunkedRivers);
        _chunkedRivers.Setup(_grid);
        _chunkedRivers.Build();
    }

    private void BuildFeatures()
    {
        if (_hexGridNode == null || _grid == null) return;

        _featureRenderer?.Dispose();
        _featureRenderer?.QueueFree();
        _featureRenderer = null;

        _featureRenderer = new FeatureRenderer(_grid);
        _hexGridNode.AddChild(_featureRenderer);
        _featureRenderer.Build();
    }

    #endregion

    #region Hover System

    private void SetupHover()
    {
        if (_grid == null || _camera == null) return;

        _hexHover = new HexHover();
        AddChild(_hexHover);
        _hexHover.Setup(_grid, _camera);

        // Connect hover signals
        _hexHover.CellHovered += OnCellHovered;
        _hexHover.CellUnhovered += OnCellUnhovered;
    }

    private void OnCellHovered(HexCell cell)
    {
        if (_gameUI != null)
        {
            var terrainName = cell.TerrainType.GetDisplayName();
            _gameUI.SetHoveredHex(cell.Q, cell.R, terrainName);
        }

        // Update path preview
        _selectionManager?.UpdatePathPreview(cell);
    }

    private void OnCellUnhovered()
    {
        _gameUI?.ClearHoveredHex();
        _selectionManager?.ClearPathPreview();
    }

    #endregion

    #region Unit System

    private void SetupUnits()
    {
        if (_hexGridNode == null || _grid == null)
        {
            GD.Print("SetupUnits: _hexGridNode or _grid is null!");
            return;
        }

        GD.Print("SetupUnits: Creating UnitManager...");
        // Create unit manager
        _unitManager = new UnitManager(_grid);

        GD.Print("SetupUnits: Creating UnitRenderer...");
        // Create unit renderer
        _unitRenderer = new UnitRenderer(_unitManager);
        _unitRenderer.Setup(_grid);
        _hexGridNode.AddChild(_unitRenderer);

        GD.Print("SetupUnits: Spawning units using UnitManager.SpawnMixedUnits...");
        // Use UnitManager's spawn method instead of our own
        var spawned = _unitManager.SpawnMixedUnits(10, 5, 1);
        GD.Print($"Created {spawned.Land} land, {spawned.Naval} naval units (total in manager: {_unitManager.UnitCount})");

        // Build unit meshes
        _unitRenderer.Build();

        // Setup pathfinder
        _pathfinder = new Pathfinder(_grid, _unitManager);

        // Setup path renderer
        _pathRenderer = new PathRenderer();
        _hexGridNode.AddChild(_pathRenderer);
        _pathRenderer.Setup(_grid);

        // Setup turn manager
        _turnManager = new TurnManager(_unitManager);
        _turnManager.StartGame();

        // Setup selection manager
        _selectionManager = new SelectionManager();
        AddChild(_selectionManager);
        _selectionManager.Setup(_unitManager, _unitRenderer, _grid, _camera!, _pathfinder, _pathRenderer, _turnManager);
        _selectionManager.SelectionChanged += OnSelectionChanged;

        UpdateTurnDisplay();
        UpdateUnitCounts();
    }

    private (int Land, int Naval) SpawnMixedUnits(int landCount, int navalCount, int playerId)
    {
        if (_unitManager == null || _grid == null) return (0, 0);

        int landSpawned = 0;
        int navalSpawned = 0;

        // Spawn land units
        for (int i = 0; i < landCount; i++)
        {
            int q = (int)(GD.Randi() % (uint)MapWidth);
            int r = (int)(GD.Randi() % (uint)MapHeight);
            var cell = _grid.GetCell(q, r);
            if (cell != null && !cell.IsWater)
            {
                _unitManager.CreateUnit(UnitType.Infantry, q, r, playerId);
                landSpawned++;
            }
        }

        // Spawn naval units
        for (int i = 0; i < navalCount; i++)
        {
            int q = (int)(GD.Randi() % (uint)MapWidth);
            int r = (int)(GD.Randi() % (uint)MapHeight);
            var cell = _grid.GetCell(q, r);
            if (cell != null && cell.IsWater)
            {
                _unitManager.CreateUnit(UnitType.Galley, q, r, playerId);
                navalSpawned++;
            }
        }

        return (landSpawned, navalSpawned);
    }

    private void OnSelectionChanged(int[] selectedIds)
    {
        GD.Print($"Selection changed: {selectedIds.Length} units selected");
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

        // Check for async generation completion
        if (_asyncGenerationPending && _mapGenerator != null && _mapGenerator.IsGenerationComplete())
        {
            FinishAsyncGeneration();
        }

        // Update water animation and visibility
        _chunkedWater?.UpdateAnimation(dt);
        if (_camera != null)
        {
            _chunkedWater?.UpdateVisibility(_camera);
        }

        // Update river animation and visibility
        _chunkedRivers?.UpdateAnimation(dt);
        if (_camera != null)
        {
            _chunkedRivers?.Update(_camera);
        }

        // Update unit renderer
        _unitRenderer?.Update();
        if (_camera != null)
        {
            _unitRenderer?.UpdateVisibility(_camera);
        }

        // Update terrain visibility and LOD
        if (_camera != null)
        {
            _chunkedTerrain?.Update(_camera);
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

                case Key.P:
                    _performanceMonitor?.ToggleGraph();
                    break;
            }
        }
    }

    #endregion

    #region UI Signal Handlers

    private void OnUIRegenerate(int width, int height, int seedVal)
    {
        RegenerateWithSettings(width, height, seedVal);
        _gameUI?.SetSeed(_currentSeed);
    }

    private void OnUIRandomSeed()
    {
        _currentSeed = (int)GD.Randi();
        _gameUI?.SetSeed(_currentSeed);
        RegenerateMap();
    }

    private void OnEndTurn()
    {
        if (_turnManager != null)
        {
            _turnManager.EndTurn();
            UpdateTurnDisplay();
            GD.Print(_turnManager.GetStatus());
        }
    }

    private void OnSpawnLand(int count)
    {
        GD.Print($"OnSpawnLand received with count={count}");
        if (_unitManager == null) { GD.Print("OnSpawnLand: _unitManager is null!"); return; }
        var spawned = _unitManager.SpawnMixedUnits(count, 0, 1);
        GD.Print($"Spawned {spawned.Land} land units");
        _unitRenderer?.Build();
        UpdateUnitCounts();
    }

    private void OnSpawnNaval(int count)
    {
        GD.Print($"OnSpawnNaval received with count={count}");
        if (_unitManager == null) { GD.Print("OnSpawnNaval: _unitManager is null!"); return; }
        var spawned = _unitManager.SpawnMixedUnits(0, count, 1);
        GD.Print($"Spawned {spawned.Naval} naval units");
        _unitRenderer?.Build();
        UpdateUnitCounts();
    }

    private void OnSpawnAi(int land, int naval)
    {
        if (_unitManager == null) return;
        var spawned = _unitManager.SpawnMixedUnits(land, naval, 2);
        GD.Print($"Spawned {spawned.Land} land, {spawned.Naval} naval AI units");
        _unitRenderer?.Build();
        UpdateUnitCounts();
    }

    private void OnClearUnits()
    {
        _unitManager?.Clear();
        GD.Print("Cleared all units");
        _unitRenderer?.Build();
        _selectionManager?.ClearSelection();
        UpdateUnitCounts();
    }

    private void OnNoiseParamChanged(string param, float value)
    {
        if (_mapGenerator == null) return;

        // Handle flow_speed separately
        if (param == "flow_speed")
        {
            // TODO: Update river material flow speed
            return;
        }

        switch (param)
        {
            case "noise_scale":
                _mapGenerator.NoiseScale = value;
                break;
            case "octaves":
                _mapGenerator.Octaves = (int)value;
                break;
            case "persistence":
                _mapGenerator.Persistence = value;
                break;
            case "lacunarity":
                _mapGenerator.Lacunarity = value;
                break;
            case "sea_level":
                _mapGenerator.SeaLevel = value;
                break;
            case "mountain_level":
                _mapGenerator.MountainLevel = value;
                break;
            case "river_percentage":
                _mapGenerator.RiverPercentage = value;
                break;
        }

        RegenerateMap();
    }

    private void OnShaderParamChanged(string param, float value)
    {
        _chunkedTerrain?.SetShaderParameter(param, value);
    }

    private void OnLightingParamChanged(string param, float value)
    {
        switch (param)
        {
            case "ambient_energy":
                if (_worldEnvironment?.Environment != null)
                {
                    _worldEnvironment.Environment.AmbientLightEnergy = value;
                }
                break;
            case "light_energy":
                if (_directionalLight != null)
                {
                    _directionalLight.LightEnergy = value;
                }
                break;
        }
    }

    private void OnFogParamChanged(string param, float value)
    {
        if (_worldEnvironment?.Environment == null) return;

        switch (param)
        {
            case "fog_near":
                _worldEnvironment.Environment.FogDepthBegin = value;
                break;
            case "fog_far":
                _worldEnvironment.Environment.FogDepthEnd = value;
                break;
            case "fog_density":
                _worldEnvironment.Environment.FogLightEnergy = value;
                break;
        }
    }

    private void OnWaterParamChanged(string param, float value)
    {
        if (_chunkedWater == null) return;

        switch (param)
        {
            case "height_offset":
                _chunkedWater.SetHeightOffset(value);
                break;
        }
    }

    private void OnAsyncToggleChanged(bool enabled)
    {
        _useAsyncGeneration = enabled;
        GD.Print($"Async generation: {(enabled ? "enabled" : "disabled")}");
    }

    #endregion

    #region Turn System

    private void UpdateTurnDisplay()
    {
        if (_gameUI != null && _turnManager != null)
        {
            _gameUI.SetTurnStatus(_turnManager.GetStatus());
        }
    }

    private void UpdateUnitCounts()
    {
        if (_gameUI == null || _unitManager == null) return;

        var counts = _unitManager.GetUnitCounts();
        _gameUI.SetUnitCounts(counts.Land, counts.Naval);
    }

    #endregion

    #region Map Regeneration

    public void RegenerateMap()
    {
        CancelPendingGeneration();
        CleanupRenderers();

        // Clear units
        _unitManager?.Clear();

        // Regenerate - async or sync based on setting
        if (_mapGenerator != null && _grid != null)
        {
            if (_useAsyncGeneration)
            {
                _asyncGenerationPending = true;
                _asyncNeedsNewUnits = false; // Same grid, respawn units
                _mapGenerator.GenerateAsync(_grid, _currentSeed);
                GD.Print($"Async map generation started with seed: {_currentSeed}");
                _gameUI?.ShowGenerationStatus("Generating terrain...");
            }
            else
            {
                _mapGenerator.Generate(_grid, _currentSeed);
                GD.Print($"Map generated (sync) with seed: {_currentSeed}");
                FinishMapBuild();
            }
        }
    }

    private void CancelPendingGeneration()
    {
        if (_asyncGenerationPending && _mapGenerator != null)
        {
            _mapGenerator.CancelGeneration();
            _asyncGenerationPending = false;
        }
    }

    private void FinishAsyncGeneration()
    {
        _asyncGenerationPending = false;
        var result = _mapGenerator!.FinishAsyncGeneration();
        GD.Print($"Map generated (async): worker={result.WorkerTimeMs}ms, features={result.FeatureTimeMs}ms");
        _gameUI?.HideGenerationStatus();

        if (_asyncNeedsNewUnits)
        {
            FinishMapBuildWithNewUnits();
        }
        else
        {
            FinishMapBuild();
        }
    }

    private void FinishMapBuild()
    {
        BuildTerrain();
        BuildFeatures();
        SetupSystemsAfterBuild(false); // Reuse existing unit manager
        CenterCamera();
    }

    private void FinishMapBuildWithNewUnits()
    {
        BuildTerrain();
        BuildFeatures();
        SetupSystemsAfterBuild(true); // Create new unit manager
        CenterCamera();
    }

    public void RegenerateWithSettings(int width, int height, int seedVal)
    {
        CancelPendingGeneration();
        CleanupRenderers();

        MapWidth = width;
        MapHeight = height;
        _currentSeed = seedVal;

        // Clear units
        _unitManager?.Clear();

        // Reinitialize grid with new size
        _grid = new HexGrid(MapWidth, MapHeight);
        _grid.Initialize();

        // Generate - async or sync based on setting
        if (_mapGenerator != null)
        {
            if (_useAsyncGeneration)
            {
                _asyncGenerationPending = true;
                _asyncNeedsNewUnits = true; // New grid size, need new unit manager
                _mapGenerator.GenerateAsync(_grid, _currentSeed);
                GD.Print($"Async map generation started with seed: {_currentSeed} (size: {MapWidth}x{MapHeight})");
                _gameUI?.ShowGenerationStatus("Generating terrain...");
            }
            else
            {
                _mapGenerator.Generate(_grid, _currentSeed);
                GD.Print($"Map generated (sync) with seed: {_currentSeed}");
                FinishMapBuildWithNewUnits();
            }
        }
    }

    private void SetupSystemsAfterBuild(bool createNewUnitManager)
    {
        if (_grid == null || _camera == null) return;

        // Update hover with new grid
        _hexHover?.Setup(_grid, _camera);

        // Create or reuse unit manager
        if (createNewUnitManager)
        {
            _unitManager = new UnitManager(_grid);
        }

        // Spawn units
        var spawned = SpawnMixedUnits(10, 5, 1);
        GD.Print($"Spawned {spawned.Land} land, {spawned.Naval} naval units");

        // Setup unit renderer
        _unitRenderer?.Build();

        // Update pathfinder
        _pathfinder = new Pathfinder(_grid, _unitManager!);

        // Update turn manager
        _turnManager = new TurnManager(_unitManager!);
        _turnManager.StartGame();

        // Update selection manager
        _selectionManager?.ClearSelection();
        _selectionManager?.Setup(_unitManager!, _unitRenderer!, _grid, _camera, _pathfinder, _pathRenderer, _turnManager);

        UpdateTurnDisplay();
        UpdateUnitCounts();
    }

    private void CleanupRenderers()
    {
        _chunkedTerrain?.Dispose();
        _chunkedTerrain?.QueueFree();
        _chunkedTerrain = null;

        _chunkedWater?.Dispose();
        _chunkedWater?.QueueFree();
        _chunkedWater = null;

        _chunkedRivers?.Dispose();
        _chunkedRivers?.QueueFree();
        _chunkedRivers = null;

        _featureRenderer?.Dispose();
        _featureRenderer?.QueueFree();
        _featureRenderer = null;

        _groundPlane?.QueueFree();
        _groundPlane = null;
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
