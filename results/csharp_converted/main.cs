using Godot;
using Godot.Collections;

//# Main entry point for HexGame

//# Manages game initialization and main loop
[GlobalClass]
public partial class main : Godot.Node3D
{
	public const Godot.Resource ChunkedWaterRendererClass = /* preload has no equivalent, add a 'ResourcePreloader' Node in your scene */("res://src/rendering/chunked_water_renderer.gd");
	public const Godot.Resource ChunkedRiverRendererClass = /* preload has no equivalent, add a 'ResourcePreloader' Node in your scene */("res://src/rendering/chunked_river_renderer.gd");
	public const Godot.Resource ScreenshotCaptureClass = /* preload has no equivalent, add a 'ResourcePreloader' Node in your scene */("res://src/debug/screenshot_capture.gd");
	public const Godot.Resource PerformanceMonitorClass = /* preload has no equivalent, add a 'ResourcePreloader' Node in your scene */("res://src/debug/performance_monitor.gd");

	public Godot.Node3D HexGridNode;
	public Godot.MapCamera Camera;
	public Godot.DirectionalLight3D DirectionalLight;
	public Godot.WorldEnvironment WorldEnv;

	public Godot.HexGrid Grid;
	public Godot.MapGenerator MapGenerator;
	public Godot.ChunkedTerrainRenderer ChunkedTerrain;
	public Godot.FeatureRenderer FeatureRenderer;
	public Godot.Node3D ChunkedWater;
	// ChunkedWaterRenderer
	public Godot.Node3D ChunkedRivers;
	// ChunkedRiverRenderer
	public Godot.MeshInstance3D GroundPlane;
	// Prevents seeing through terrain at distance
	public Godot.HexHover HexHover;
	public int CurrentSeed = 0;
	public Godot.GameUI GameUi;


	// Unit system
	public Godot.UnitManager UnitManager;
	public Godot.UnitRenderer UnitRenderer;
	public Godot.SelectionManager SelectionManager;
	public Godot.Pathfinder Pathfinder;
	public Godot.PathRenderer PathRenderer;
	public Godot.TurnManager TurnManager;
	public Godot.Node ScreenshotCapture;
	// ScreenshotCapture
	public Godot.Control PerformanceMonitor;

	// PerformanceMonitor
	// Map settings
	public int MapWidth = 32;
	public int MapHeight = 32;


	// Async generation
	public bool UseAsyncGeneration = true;
	protected bool _AsyncGenerationPending = false;
	protected bool _AsyncNeedsNewUnits = false;

	// True when grid size changed
	// UI scene
	public const Godot.Resource GameUIScene = /* preload has no equivalent, add a 'ResourcePreloader' Node in your scene */("res://scenes/game_ui.tscn");


	public override void _Ready()
	{
		HexGridNode = GetNode("HexGrid");
		Camera = GetNode("MapCamera");
		DirectionalLight = GetNode("DirectionalLight3D");
		WorldEnv = GetNode("WorldEnvironment");
		GD.Print("HexGame starting...");


		// Uncap FPS (default VSync limits to monitor refresh rate)
		Godot.DisplayServer.WindowSetVsyncMode(Godot.DisplayServer.VSyncMode.VsyncDisabled);
		Godot.Engine.MaxFps = 0;

		// 0 = uncapped
		CurrentSeed = GD.Randi();
		_SetupUi();
		_InitializeGame();
		_SetupScreenshotCapture();
		_SetupPerformanceMonitor();
	}


	protected void _SetupUi()
	{

		// Create and add UI
		GameUi = GameUIScene.Instantiate();
		AddChild(GameUi);
		GameUi.SetMainNode(this);
		GameUi.SetSeed(CurrentSeed);


		// Connect signals
		GameUi.RegenerateRequested += _on_ui_regenerate;
		GameUi.RandomSeedRequested += _on_ui_random_seed;
		GameUi.EndTurnRequested += _on_end_turn;
		GameUi.SpawnLandRequested += _on_spawn_land;
		GameUi.SpawnNavalRequested += _on_spawn_naval;
		GameUi.SpawnAiRequested += _on_spawn_ai;
		GameUi.ClearUnitsRequested += _on_clear_units;
		GameUi.NoiseParamChanged += _on_noise_param_changed;
		GameUi.ShaderParamChanged += _on_shader_param_changed;
		GameUi.LightingParamChanged += _on_lighting_param_changed;
		GameUi.FogParamChanged += _on_fog_param_changed;
		GameUi.AsyncToggleChanged += _on_async_toggle_changed;
	}


	protected void _SetupScreenshotCapture()
	{
		ScreenshotCapture = ScreenshotCaptureClass.New();
		AddChild(ScreenshotCapture);
		ScreenshotCapture.Setup(Camera);
		GD.Print("Screenshot capture ready - Auto-capture enabled");
	}


	protected void _SetupPerformanceMonitor()
	{
		PerformanceMonitor = PerformanceMonitorClass.New();
		AddChild(PerformanceMonitor);
	}


	protected void _OnUiRegenerate(int width, int height, int seed_val)
	{
		RegenerateWithSettings(width, height, seed_val);
		GameUi.SetSeed(CurrentSeed);
	}


	protected void _OnUiRandomSeed()
	{
		CurrentSeed = GD.Randi();
		GameUi.SetSeed(CurrentSeed);
		_RegenerateMap();
	}


	protected void _InitializeGame()
	{

		// Initialize grid
		Grid = HexGrid.New(MapWidth, MapHeight);
		Grid.Initialize();
		GD.Print("Grid initialized: %dx%d cells" % new Array{MapWidth, MapHeight, });


		// Generate terrain
		MapGenerator = MapGenerator.New();
		MapGenerator.Generate(Grid, CurrentSeed);
		GD.Print("Map generated with seed: %d" % CurrentSeed);


		// Build and display terrain mesh
		_BuildTerrain();


		// Build features (trees, rocks)
		_BuildFeatures();


		// Setup hover system
		_SetupHover();


		// Setup unit system
		_SetupUnits();
	}


	protected void _BuildTerrain()
	{

		// Create chunked terrain renderer
		ChunkedTerrain = ChunkedTerrainRenderer.New();
		HexGridNode.AddChild(ChunkedTerrain);
		ChunkedTerrain.Build(Grid);


		// Build ground plane (prevents seeing through terrain at distance)
		_BuildGroundPlane();


		// Build water
		_BuildWater();


		// Build rivers
		_BuildRivers();


		// Center camera on map
		_CenterCamera();
	}


	protected void _SetupHover()
	{
		HexHover = HexHover.New();
		AddChild(HexHover);
		HexHover.Setup(Grid, Camera);


		// Connect hover signals to UI
		HexHover.CellHovered += _on_cell_hovered;
		HexHover.CellUnhovered += _on_cell_unhovered;
	}


	protected void _OnCellHovered(Godot.HexCell cell)
	{
		if(GameUi)
		{
			var terrain_name = TerrainType.GetTerrainName(cell.TerrainType);
			GameUi.SetHoveredHex(cell.Q, cell.R, terrain_name);
		}

		// Update path preview when hovering
		if(SelectionManager)
		{
			SelectionManager.UpdatePathPreview(cell);
		}
	}


	protected void _OnCellUnhovered()
	{
		if(GameUi)
		{
			GameUi.ClearHoveredHex();
		}

		// Clear path preview
		if(SelectionManager)
		{
			SelectionManager.ClearPathPreview();
		}
	}


	protected void _OnSelectionChanged(Array<int> selected_ids)
	{
		GD.Print("Selection changed: %d units selected" % selected_ids.Size());
	}


	// Could update UI here to show selected unit info
	protected void _OnSpawnLand(int count)
	{
		if(UnitManager)
		{
			var spawned = UnitManager.SpawnMixedUnits(count, 0, 1);
			GD.Print("Spawned %d land units" % spawned["land"]);
			if(UnitRenderer)
			{
				UnitRenderer.Setup(UnitManager, Grid);
				UnitRenderer.Build();
			}
			_UpdateUnitCounts();
		}
	}


	protected void _OnSpawnNaval(int count)
	{
		if(UnitManager)
		{
			var spawned = UnitManager.SpawnMixedUnits(0, count, 1);
			GD.Print("Spawned %d naval units" % spawned["naval"]);
			if(UnitRenderer)
			{
				UnitRenderer.Setup(UnitManager, Grid);
				UnitRenderer.Build();
			}
			_UpdateUnitCounts();
		}
	}


	protected void _OnSpawnAi(int land, int naval)
	{
		if(UnitManager)
		{
			var spawned = UnitManager.SpawnMixedUnits(land, naval, 2);
			// AI is player 2
			GD.Print("Spawned %d land, %d naval AI units" % new Array{spawned["land"], spawned["naval"], });
			if(UnitRenderer)
			{
				UnitRenderer.Setup(UnitManager, Grid);
				UnitRenderer.Build();
			}
			_UpdateUnitCounts();
		}
	}


	protected void _OnClearUnits()
	{
		if(UnitManager)
		{
			UnitManager.Clear();
			GD.Print("Cleared all units");
			if(UnitRenderer)
			{
				UnitRenderer.Setup(UnitManager, Grid);
				UnitRenderer.Build();
			}
			if(SelectionManager)
			{
				SelectionManager.ClearSelection();
			}
			_UpdateUnitCounts();
		}
	}


	protected void _UpdateUnitCounts()
	{
		if(GameUi && UnitManager)
		{
			var counts = UnitManager.GetUnitCounts();
			GameUi.SetUnitCounts(counts["land"], counts["naval"]);

			// Update pool stats
			var pool_stats = UnitManager.GetPoolStats();
			GameUi.SetPoolStats(pool_stats["active"], pool_stats["created"], pool_stats["reuse_rate"]);
		}
	}


	protected void _OnNoiseParamChanged(String param, double value)
	{

		// Handle flow_speed separately as it doesn't require map regeneration
		if(param == "flow_speed")
		{
			if(ChunkedRivers && ChunkedRivers.Material)
			{
				ChunkedRivers.Material.SetShaderParameter("flow_speed", value);
			}
			return ;
		}

		if(MapGenerator)
		{

			if(param == "noise_scale")
			{
				MapGenerator.NoiseScale = value;
			}
			if(param == "octaves")
			{
				MapGenerator.Octaves = Int(value);
			}
			if(param == "persistence")
			{
				MapGenerator.Persistence = value;
			}
			if(param == "lacunarity")
			{
				MapGenerator.Lacunarity = value;
			}
			if(param == "sea_level")
			{
				MapGenerator.SeaLevel = value;
			}
			if(param == "mountain_level")
			{
				MapGenerator.MountainLevel = value;
			}
			if(param == "river_percentage")
			{
				MapGenerator.RiverPercentage = value;
			}
		}

		// Regenerate with current settings
		_RegenerateMap();
	}
}


protected void _OnShaderParamChanged(String param, double value)
{
	if(ChunkedTerrain && ChunkedTerrain.TerrainMaterial)
	{
		ChunkedTerrain.TerrainMaterial.SetShaderParameter(param, value);
	}
}


protected void _OnLightingParamChanged(String param, double value)
{

	if(param == "ambient_energy")
	{
		if(WorldEnv && WorldEnv.Environment)
		{
			WorldEnv.Environment.AmbientLightEnergy = value;
		}
	}
	if(param == "light_energy")
	{
		if(DirectionalLight)
		{
			DirectionalLight.LightEnergy = value;
		}
	}
}
}


protected void _OnFogParamChanged(String param, double value)
{
if(WorldEnv && WorldEnv.Environment)
{

	if(param == "fog_near")
	{
		WorldEnv.Environment.FogDepthBegin = value;
	}
	if(param == "fog_far")
	{
		WorldEnv.Environment.FogDepthEnd = value;
	}
	if(param == "fog_density")
	{
		WorldEnv.Environment.FogLightEnergy = value;
	}
}
}
}


protected void _OnAsyncToggleChanged(bool enabled)
{
UseAsyncGeneration = enabled;
GD.Print("Async generation: %s" % (( enabled ? "enabled" : "disabled" )));
}


protected void _OnEndTurn()
{
if(TurnManager)
{
TurnManager.EndTurn();
_UpdateTurnDisplay();
GD.Print(TurnManager.GetStatus());
}
}


protected void _UpdateTurnDisplay()
{
if(GameUi && TurnManager)
{
GameUi.SetTurnStatus(TurnManager.GetStatus());
}
}


protected void _SetupUnits()
{

// Create unit manager
UnitManager = UnitManager.New(Grid);
UnitManager.SetupPool();
// Initialize object pooling
UnitManager.PrewarmPool(50);

// Pre-create units for faster spawning
// Create unit renderer
UnitRenderer = UnitRenderer.New();
HexGridNode.AddChild(UnitRenderer);
UnitRenderer.Setup(UnitManager, Grid);


// Spawn some test units
var spawned = UnitManager.SpawnMixedUnits(10, 5, 1);
GD.Print("Spawned %d land units, %d naval units" % new Array{spawned["land"], spawned["naval"], });


// Build unit meshes
UnitRenderer.Build();


// Setup pathfinder
Pathfinder = Pathfinder.New(Grid, UnitManager);


// Setup path renderer
PathRenderer = PathRenderer.New();
HexGridNode.AddChild(PathRenderer);
PathRenderer.Setup(Grid);


// Setup turn manager (before selection manager so it can be passed)
TurnManager = TurnManager.New(UnitManager);
TurnManager.StartGame();


// Setup selection manager
SelectionManager = SelectionManager.New();
AddChild(SelectionManager);
SelectionManager.Setup(UnitManager, UnitRenderer, Grid, Camera, Pathfinder, PathRenderer, TurnManager);
SelectionManager.SelectionChanged += _on_selection_changed;

_UpdateTurnDisplay();
_UpdateUnitCounts();
}


protected void _BuildFeatures()
{

// Remove old features - use remove_child + free for immediate cleanup
if(FeatureRenderer)
{
FeatureRenderer.Dispose();
HexGridNode.RemoveChild(FeatureRenderer);
FeatureRenderer.Free();
FeatureRenderer = null;
}

FeatureRenderer = FeatureRenderer.New();
HexGridNode.AddChild(FeatureRenderer);
FeatureRenderer.Build(Grid);
}


protected void _BuildWater()
{

// Remove old water
if(ChunkedWater)
{
ChunkedWater.Dispose();
ChunkedWater.QueueFree();
ChunkedWater = null;
}

ChunkedWater = ChunkedWaterRendererClass.New();
HexGridNode.AddChild(ChunkedWater);
ChunkedWater.Build(Grid);
GD.Print("Water mesh added to scene");
}


protected void _BuildRivers()
{

// Remove existing river renderer
if(ChunkedRivers)
{
ChunkedRivers.Dispose();
ChunkedRivers.QueueFree();
ChunkedRivers = null;
}

ChunkedRivers = ChunkedRiverRendererClass.New();
HexGridNode.AddChild(ChunkedRivers);
ChunkedRivers.Setup(Grid);
ChunkedRivers.Build();
}


protected void _BuildGroundPlane()
{

// Remove existing ground plane
if(GroundPlane)
{
GroundPlane.QueueFree();
GroundPlane = null;
}


// Calculate map bounds in world coordinates
var min_coords = HexCoordinates.New(0, 0).ToWorldPosition(0);
var max_coords = HexCoordinates.New(MapWidth - 1, MapHeight - 1).ToWorldPosition(0);


// Add padding around the map
var padding = 20.0;
var size_x = max_coords.X - min_coords.X + padding * 2;
var size_z = max_coords.Z - min_coords.Z + padding * 2;
var center_x = (min_coords.X + max_coords.X) / 2.0;
var center_z = (min_coords.Z + max_coords.Z) / 2.0;


// Position below minimum terrain elevation
var plane_y = HexMetrics.MIN_ELEVATION * HexMetrics.ELEVATION_STEP - 0.5;


// Create mesh
GroundPlane = MeshInstance3D.New();
var plane_mesh = PlaneMesh.New();
plane_mesh.Size = new Vector2(size_x, size_z);
GroundPlane.Mesh = plane_mesh;


// Create dark ocean material
var material = StandardMaterial3D.New();
material.AlbedoColor = new Color(0.102, 0.298, 0.431);
// 0x1a4c6e - matches Three.js
material.Metallic = 0.0;
material.Roughness = 1.0;
GroundPlane.MaterialOverride = material;


// Position the plane
GroundPlane.Position = new Vector3(center_x, plane_y, center_z);

HexGridNode.AddChild(GroundPlane);
}


public override void _Process(double delta)
{

// Check for async generation completion
if(_AsyncGenerationPending && MapGenerator && MapGenerator.IsGenerationComplete())
{
_FinishAsyncGeneration();
}


// Update water animation and visibility
if(ChunkedWater)
{
ChunkedWater.UpdateAnimation(delta);
if(Camera)
{
	ChunkedWater.Update(Camera);
}
}


// Update unit renderer
if(UnitRenderer)
{
UnitRenderer.Update();
if(Camera)
{
	UnitRenderer.UpdateVisibility(Camera);
}
}


// Update river animation and visibility
if(ChunkedRivers)
{
ChunkedRivers.UpdateAnimation(delta);
if(Camera)
{
	ChunkedRivers.Update(Camera);
}
}


// Update terrain visibility and LOD based on camera
if(ChunkedTerrain && Camera)
{
ChunkedTerrain.Update(Camera);
}


// Update feature visibility based on camera distance
if(FeatureRenderer && Camera)
{
FeatureRenderer.Update(Camera);
}
}


protected void _CenterCamera()
{

// Calculate map center
var center_q = MapWidth / 2;
var center_r = MapHeight / 2;
var center_coords = HexCoordinates.New(center_q, center_r);
var center_pos = center_coords.ToWorldPosition(0);


// Focus camera on map center
Camera.SetTarget(center_pos);
Camera.TargetDistance = 40.0;
Camera.TargetPitch = 50.0;
}


public override void _Input(Godot.InputEvent event)
{

// Regenerate map with Space key (R is now used for camera tilt)
if(event is Godot.InputEventKey)
{ && event.Pressed;;;//PANIC! <:> unexpected at Token(type=':', value=':', lineno=469, index=13694, end=13695)

{if(event.Keycode == KEY_SPACE)
	{
		GD.Print("Regenerating map with new seed...");
		CurrentSeed = GD.Randi();
		_RegenerateMap();
	}
	else if(event.Keycode == KEY_G)
	{
		GD.Print("Regenerating map with same seed...");
		_RegenerateMap();
	}
	else if(event.Keycode == KEY_P)
	{
		if(PerformanceMonitor)
		{
			PerformanceMonitor.ToggleGraph();


			// =============================================================================
			// MAP REGENERATION HELPERS (extracted to reduce duplication)

			// =============================================================================

		}
	}
}
}//# Cancel any pending async generation
protected void _CancelPendingGeneration()
{
if(_AsyncGenerationPending && MapGenerator)
{
	MapGenerator.CancelGeneration();
	_AsyncGenerationPending = false;
}
}


//# Cleanup all renderers and features
protected void _CleanupRenderers()
{
if(ChunkedTerrain)
{
	ChunkedTerrain.Dispose();
	ChunkedTerrain.QueueFree();
	ChunkedTerrain = null;
}
if(ChunkedWater)
{
	ChunkedWater.Dispose();
	ChunkedWater.QueueFree();
	ChunkedWater = null;
}
if(ChunkedRivers)
{
	ChunkedRivers.Dispose();
	ChunkedRivers.QueueFree();
	ChunkedRivers = null;
}
if(FeatureRenderer)
{
	FeatureRenderer.Dispose();
	FeatureRenderer.QueueFree();
	FeatureRenderer = null;
}
if(GroundPlane)
{
	GroundPlane.QueueFree();
	GroundPlane = null;
}
}


//# Setup systems after map generation (pathfinder, turn manager, selection)
protected void _SetupSystemsAfterBuild(bool create_new_unit_manager)
{

// Update hover with new grid
if(HexHover)
{
	HexHover.Setup(Grid, Camera);
}


// Create or reuse unit manager
if(create_new_unit_manager)
{
	UnitManager = UnitManager.New(Grid);
	UnitManager.SetupPool();
}


// Spawn units
var spawned = UnitManager.SpawnMixedUnits(10, 5, 1);
GD.Print("Spawned %d land, %d naval units" % new Array{spawned["land"], spawned["naval"], });


// Setup unit renderer
if(UnitRenderer)
{
	UnitRenderer.Setup(UnitManager, Grid);
	UnitRenderer.Build();
}


// Update pathfinder
Pathfinder = Pathfinder.New(Grid, UnitManager);


// Update turn manager
TurnManager = TurnManager.New(UnitManager);
TurnManager.StartGame();


// Update selection manager
if(SelectionManager)
{
	SelectionManager.ClearSelection();
	SelectionManager.Setup(UnitManager, UnitRenderer, Grid, Camera, Pathfinder, PathRenderer, TurnManager);
}

_UpdateTurnDisplay();
_UpdateUnitCounts();


// =============================================================================
// MAP REGENERATION

}
// =============================================================================
protected void _RegenerateMap()
{
_CancelPendingGeneration();
_CleanupRenderers();


// Clear units
if(UnitManager)
{
	UnitManager.Clear();
}


// Regenerate - async or sync based on setting
if(UseAsyncGeneration)
{
	_AsyncGenerationPending = true;
	_AsyncNeedsNewUnits = false;
	// Same grid, respawn units
	MapGenerator.GenerateAsync(Grid, CurrentSeed);
	GD.Print("Async map generation started with seed: %d" % CurrentSeed);
	if(GameUi)
	{
		GameUi.ShowGenerationStatus("Generating terrain...");
	}
}
else
{
	MapGenerator.Generate(Grid, CurrentSeed);
	GD.Print("Map generated (sync) with seed: %d" % CurrentSeed);
	_FinishMapBuild();
}
}


//# Called when async generation completes
protected void _FinishAsyncGeneration()
{
_AsyncGenerationPending = false;
var result = MapGenerator.FinishAsyncGeneration();
GD.Print("Map generated (async): worker=%dms, features=%dms" % new Array{result["worker_time"], result["feature_time"], });
if(GameUi)
{
	GameUi.HideGenerationStatus();
}

if(_AsyncNeedsNewUnits)
{
	_FinishMapBuildWithNewUnits();
}
else
{
	_FinishMapBuild();
}
}


//# Common map building after generation completes (same grid size)
protected void _FinishMapBuild()
{
_BuildTerrain();
_BuildFeatures();
_SetupSystemsAfterBuild(false);


// Reuse existing unit manager

}//# Map building when grid size changed (needs new unit manager)
protected void _FinishMapBuildWithNewUnits()
{
_BuildTerrain();
_BuildFeatures();
_SetupSystemsAfterBuild(true);


// Create new unit manager

}//# Get the hex cell at a world position (for raycasting)
public Godot.HexCell GetCellAtWorldPos(Vector3 world_pos)
{
var coords = HexCoordinates.FromWorldPosition(world_pos);
return Grid.GetCell(coords.Q, coords.R);
}


//# Regenerate with specific settings (width/height/seed from UI)
public void RegenerateWithSettings(int width, int height, int seed_val)
{
_CancelPendingGeneration();
_CleanupRenderers();

MapWidth = width;
MapHeight = height;
CurrentSeed = seed_val;


// Clear units
if(UnitManager)
{
	UnitManager.Clear();
}


// Reinitialize grid with new size
Grid = HexGrid.New(MapWidth, MapHeight);
Grid.Initialize();


// Generate - async or sync based on setting
if(UseAsyncGeneration)
{
	_AsyncGenerationPending = true;
	_AsyncNeedsNewUnits = true;
	// New grid size, need new unit manager
	MapGenerator.GenerateAsync(Grid, CurrentSeed);
	GD.Print("Async map generation started with seed: %d (size: %dx%d)" % new Array{CurrentSeed, MapWidth, MapHeight, });
	if(GameUi)
	{
		GameUi.ShowGenerationStatus("Generating terrain...");
	}
}
else
{
	MapGenerator.Generate(Grid, CurrentSeed);
	GD.Print("Map generated (sync) with seed: %d" % CurrentSeed);
	_FinishMapBuildWithNewUnits();
}
}

