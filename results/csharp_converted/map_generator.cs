using Godot;
using Godot.Collections;


//# Procedural map generation using noise
//# Matches web/src/generation/MapGenerator.ts

//# Supports both sync and async (threaded) generation
[GlobalClass]
public partial class MapGenerator : Godot.RefCounted
{
	public const Godot.Resource FeatureClass = /* preload has no equivalent, add a 'ResourcePreloader' Node in your scene */("res://src/core/feature.gd");


	// Signals for async generation progress
	[Signal]
	public delegate void GenerationStartedEventHandler();
	[Signal]
	public delegate void GenerationProgressEventHandler(String phase, double percent);
	[Signal]
	public delegate void GenerationCompletedEventHandler(bool success, double worker_time_ms, double feature_time_ms);

	public FastNoiseLite Noise;
	public Godot.HexGrid Grid;
	public Godot.RiverGenerator RiverGenerator;


	// Threading support
	protected Godot.Thread _Thread;
	protected bool _IsGenerating = false;
	protected int _PendingSeed = 0;


	// Generation parameters
	public double SeaLevel = 0.35;
	public double MountainLevel = 0.75;
	public double RiverPercentage = 0.1;


	// Noise parameters (exposed for UI)
	public double NoiseScale
	{
		set
		{
			_NoiseScale = value;
			if(Noise)
			{
				Noise.Frequency = value;
			}
		}
		get { return _NoiseScale; }
	}
	private double _NoiseScale = 0.02;


	public int Octaves
	{
		set
		{
			_Octaves = value;
			if(Noise)
			{
				Noise.FractalOctaves = value;
			}
		}
		get { return _Octaves; }
	}
	private int _Octaves = 4;


	public double Persistence
	{
		set
		{
			_Persistence = value;
			if(Noise)
			{
				Noise.FractalGain = value;
			}
		}
		get { return _Persistence; }
	}
	private double _Persistence = 0.5;


	public double Lacunarity
	{
		set
		{
			_Lacunarity = value;
			if(Noise)
			{
				Noise.FractalLacunarity = value;
			}
		}
		get { return _Lacunarity; }
	}
	private double _Lacunarity = 2.0;


	public double LandPercentage = 0.5;


	// Inverse of sea_level essentially
	public override void _Init()
	{
		Noise = FastNoiseLite.New();
		Noise.NoiseType = FastNoiseLite.TYPE_SIMPLEX;
		Noise.Frequency = 0.02;
		Noise.FractalOctaves = 4;
		Noise.FractalGain = 0.5;
		Noise.FractalLacunarity = 2.0;
	}


	//# Generate a new map
	public void Generate(Godot.HexGrid hex_grid, int seed_val = 0)
	{
		Grid = hex_grid;
		Noise.Seed = ( seed_val != 0 ? seed_val : GD.Randi() );


		// Generate elevation
		_GenerateElevation();


		// Generate moisture
		_GenerateMoisture();


		// Assign biomes
		_AssignBiomes();


		// Generate rivers
		_GenerateRivers(seed_val);


		// Generate features (trees, rocks)
		_GenerateFeatures(seed_val);
	}


	protected void _GenerateElevation()
	{
		foreach(HexCell cell in Grid.GetAllCells())
		{
			var world_pos = cell.GetWorldPosition();
			var noise_val = (Noise.GetNoise2d(world_pos.X, world_pos.Z) + 1.0) / 2.0;


			// Convert to elevation using sea level system
			// Water: 0-4 (SEA_LEVEL), Land: 5-13 (LAND_MIN_ELEVATION to MAX_ELEVATION)
			if(noise_val < SeaLevel)
			{

				// Underwater: elevation 0 to SEA_LEVEL (water is 0-4)
				// Use roundi() so coastal water gets elevation 4, ensuring 1-level terrace to land
				var normalized = noise_val / SeaLevel;
				cell.Elevation = Mathf.RoundToInt(normalized * HexMetrics.SEA_LEVEL);
			}
			else
			{

				// Land: elevation LAND_MIN_ELEVATION to MAX_ELEVATION (5-13)
				var normalized = (noise_val - SeaLevel) / (1.0 - SeaLevel);
				var land_range = HexMetrics.MAX_ELEVATION - HexMetrics.LAND_MIN_ELEVATION;
				cell.Elevation = HexMetrics.LAND_MIN_ELEVATION + Int(normalized * land_range);
			}
		}
	}


	protected void _GenerateMoisture()
	{
		var moisture_noise = FastNoiseLite.New();
		moisture_noise.NoiseType = FastNoiseLite.TYPE_SIMPLEX;
		moisture_noise.Seed = Noise.Seed + 1000;
		moisture_noise.Frequency = 0.03;

		foreach(HexCell cell in Grid.GetAllCells())
		{
			var world_pos = cell.GetWorldPosition();
			cell.Moisture = (moisture_noise.GetNoise2d(world_pos.X, world_pos.Z) + 1.0) / 2.0;
		}
	}


	protected void _AssignBiomes()
	{
		foreach(HexCell cell in Grid.GetAllCells())
		{
			cell.TerrainType = _GetBiome(cell.Elevation, cell.Moisture);
		}
	}


	protected TerrainType.Type _GetBiome(int elevation, double moisture)
	{

		// Water - below land minimum (elevation 0-4)
		if(elevation < HexMetrics.LAND_MIN_ELEVATION)
		{
			if(elevation < HexMetrics.SEA_LEVEL - 2)
			{
				return TerrainType.Type.OCEAN;
			}
			// Deep water (0-1)
			return TerrainType.Type.COAST;

			// Shallow water (2-4)

		}// Height above land minimum for land biomes
		var height_above_sea = elevation - HexMetrics.LAND_MIN_ELEVATION;


		// High elevation
		if(height_above_sea >= 6)
		{
			return TerrainType.Type.SNOW;
		}
		if(height_above_sea >= 4)
		{
			return TerrainType.Type.MOUNTAINS;
		}


		// Land biomes based on moisture
		if(moisture < 0.2)
		{
			return TerrainType.Type.DESERT;
		}
		else if(moisture < 0.4)
		{
			if(height_above_sea >= 2)
			{
				return TerrainType.Type.HILLS;
			}
			return TerrainType.Type.SAVANNA;
		}
		else if(moisture < 0.6)
		{
			return TerrainType.Type.PLAINS;
		}
		else if(moisture < 0.8)
		{
			return TerrainType.Type.FOREST;
		}
		else
		{
			return TerrainType.Type.JUNGLE;
		}
	}


	protected void _GenerateRivers(int seed_val)
	{
		if(RiverPercentage <= 0)
		{
			return ;
		}

		RiverGenerator = RiverGenerator.New(Grid);
		RiverGenerator.Generate(seed_val, RiverPercentage);


		// Count rivers for debugging
		var river_count = 0;
		foreach(HexCell cell in Grid.GetAllCells())
		{
			river_count += cell.RiverDirections.Size();
		}
		if(river_count > 0)
		{
			GD.Print("Generated %d river segments" % river_count);
		}
	}


	protected void _GenerateFeatures(int seed_val)
	{
		var rng = RandomNumberGenerator.New();
		rng.Seed = seed_val + 2000;


		// Clear existing features from all cells before generating new ones
		foreach(HexCell cell in Grid.GetAllCells())
		{
			cell.Features.Clear();
		}

		var tree_count = 0;
		var rock_count = 0;

		foreach(HexCell cell in Grid.GetAllCells())
		{

			// Skip water cells
			if(cell.IsUnderwater())
			{
				continue;
			}


			// Skip cells with rivers
			if(cell.HasRiver)
			{
				continue;
			}


			// Feature density based on terrain type
			var tree_chance = 0.0;
			var rock_chance = 0.0;


			if(cell.TerrainType == TerrainType.Type.FOREST)
			{
				tree_chance = 0.7;
				rock_chance = 0.1;
			}
			if(cell.TerrainType == TerrainType.Type.JUNGLE)
			{
				tree_chance = 0.85;
				rock_chance = 0.05;
			}
			if(cell.TerrainType == TerrainType.Type.PLAINS)
			{
				tree_chance = 0.15;
				rock_chance = 0.1;
			}
			if(cell.TerrainType == TerrainType.Type.SAVANNA)
			{
				tree_chance = 0.1;
				rock_chance = 0.15;
			}
			if(cell.TerrainType == TerrainType.Type.HILLS)
			{
				tree_chance = 0.2;
				rock_chance = 0.3;
			}
			if(cell.TerrainType == TerrainType.Type.MOUNTAINS)
			{
				tree_chance = 0.05;
				rock_chance = 0.4;
			}
			if(cell.TerrainType == TerrainType.Type.DESERT)
			{
				rock_chance = 0.2;
			}
			if(cell.TerrainType == TerrainType.Type.SNOW)
			{
				rock_chance = 0.15;
			}
		}


		// Generate features
		var center = cell.GetWorldPosition();


		// Try to place trees
		if(tree_chance > 0 && rng.Randf() < tree_chance)
		{
			var num_trees = rng.RandiRange(1, 3);
			foreach(int _i in GD.Range(num_trees))
			{


				var offset = new Vector3(, rng.RandfRange( - 0.3, 0.3), 0, rng.RandfRange( - 0.3, 0.3));


				var feature = FeatureClass.New(, FeatureClass.Type.TREE, center + offset, rng.Randf() * Mathf.Tau, rng.RandfRange(0.8, 1.2));
				cell.Features.Append(feature);
				tree_count += 1;
			}
		}


		// Try to place rocks
		if(rock_chance > 0 && rng.Randf() < rock_chance)
		{
			var num_rocks = rng.RandiRange(1, 2);
			foreach(int _i in GD.Range(num_rocks))
			{


				var offset = new Vector3(, rng.RandfRange( - 0.35, 0.35), 0, rng.RandfRange( - 0.35, 0.35));


				var feature = FeatureClass.New(, FeatureClass.Type.ROCK, center + offset, rng.Randf() * Mathf.Tau, rng.RandfRange(0.6, 1.4));
				cell.Features.Append(feature);
				rock_count += 1;
			}
		}
	}

	if(tree_count > 0 || rock_count > 0)
	{
		GD.Print("Generated %d trees, %d rocks" % new Array{tree_count, rock_count, });


		// =============================================================================
		// ASYNC GENERATION METHODS

		// =============================================================================
		//# Start async generation in background thread
		//# Call is_generation_complete() in _process() to check when done

	}
}//# Then call finish_async_generation() to apply results
public void GenerateAsync(Godot.HexGrid hex_grid, int seed_val = 0)
{
	if(_IsGenerating)
	{
		GD.PushError("MapGenerator: Generation already in progress");
		return ;
	}

	Grid = hex_grid;
	_PendingSeed = ( seed_val != 0 ? seed_val : GD.Randi() );
	_IsGenerating = true;

	EmitSignal("GenerationStarted");
	EmitSignal("GenerationProgress", "terrain", 0.0);


	// Prepare input data for thread (no objects, just primitives)
	var input = new Dictionary{
			{"width", Grid.Width},
			{"height", Grid.Height},
			{"seed", _PendingSeed},
			{"noise_scale", NoiseScale},
			{"octaves", Octaves},
			{"persistence", Persistence},
			{"lacunarity", Lacunarity},
			{"sea_level", SeaLevel},
			};


	// Start background thread
	_Thread = Thread.New();
	_Thread.Start(_thread_generate_terrain.Bind(input));


	//# Thread worker function - generates terrain data only (no grid access)

}//# Returns Dictionary with cells array and timing info
protected Dictionary _ThreadGenerateTerrain(Dictionary input)
{
	var start_time = Godot.Time.GetTicksMsec();


	// Create noise generators in thread (thread-safe)
	var elev_noise = FastNoiseLite.New();
	elev_noise.NoiseType = FastNoiseLite.TYPE_SIMPLEX;
	elev_noise.Seed = input["seed"];
	elev_noise.Frequency = input["noise_scale"];
	elev_noise.FractalOctaves = input["octaves"];
	elev_noise.FractalGain = input["persistence"];
	elev_noise.FractalLacunarity = input["lacunarity"];

	var moist_noise = FastNoiseLite.New();
	moist_noise.NoiseType = FastNoiseLite.TYPE_SIMPLEX;
	moist_noise.Seed = input["seed"] + 1000;
	moist_noise.Frequency = 0.03;

	var cells = new Array{};
	var width = input["width"];
	var height = input["height"];
	var sea_lvl = input["sea_level"];


	// Generate all cells
	foreach(int r in GD.Range(height))
	{
		foreach(int q in GD.Range(width))
		{

			// Calculate world position (same as HexCoordinates.to_world_position)
			var x = (q + r * 0.5) * (HexMetrics.INNER_RADIUS * 2.0);
			var z = r * (HexMetrics.OUTER_RADIUS * 1.5);


			// Elevation from noise - using sea level system
			// Water: 0-4 (SEA_LEVEL), Land: 5-13 (LAND_MIN_ELEVATION to MAX_ELEVATION)
			var noise_val = (elev_noise.GetNoise2d(x, z) + 1.0) / 2.0;
			var elevation;
			if(noise_val < sea_lvl)
			{

				// Underwater: elevation 0 to SEA_LEVEL (water is 0-4)
				// Use roundi() so coastal water gets elevation 4, ensuring 1-level terrace to land
				var normalized = noise_val / sea_lvl;
				elevation = Mathf.RoundToInt(normalized * HexMetrics.SEA_LEVEL);
			}
			else
			{

				// Land: elevation LAND_MIN_ELEVATION to MAX_ELEVATION (5-13)
				var normalized = (noise_val - sea_lvl) / (1.0 - sea_lvl);
				var land_range = HexMetrics.MAX_ELEVATION - HexMetrics.LAND_MIN_ELEVATION;
				elevation = HexMetrics.LAND_MIN_ELEVATION + Int(normalized * land_range);
			}


			// Moisture from noise
			var moisture = (moist_noise.GetNoise2d(x, z) + 1.0) / 2.0;


			// Determine terrain type (static function for thread safety)
			var terrain_type = _GetBiomeForThread(elevation, moisture);

			cells.Append(new Dictionary{
							{"q", q},
							{"r", r},
							{"elevation", elevation},
							{"moisture", moisture},
							{"terrain_type", terrain_type},
							});
		}
	}

	return new Dictionary{
			{"cells", cells},
			{"worker_time", Godot.Time.GetTicksMsec() - start_time},
			};
}


//# Static biome function for thread (avoids instance access)
protected static int _GetBiomeForThread(int elevation, double moisture)
{

	// Water - below land minimum (elevation 0-4)
	if(elevation < HexMetrics.LAND_MIN_ELEVATION)
	{
		if(elevation < HexMetrics.SEA_LEVEL - 2)
		{
			return TerrainType.Type.OCEAN;
		}
		// Deep water (0-1)
		return TerrainType.Type.COAST;

		// Shallow water (2-4)

	}// Height above land minimum for land biomes
	var height_above_sea = elevation - HexMetrics.LAND_MIN_ELEVATION;


	// High elevation
	if(height_above_sea >= 6)
	{
		return TerrainType.Type.SNOW;
	}
	if(height_above_sea >= 4)
	{
		return TerrainType.Type.MOUNTAINS;
	}


	// Land biomes based on moisture
	if(moisture < 0.2)
	{
		return TerrainType.Type.DESERT;
	}
	else if(moisture < 0.4)
	{
		if(height_above_sea >= 2)
		{
			return TerrainType.Type.HILLS;
		}
		return TerrainType.Type.SAVANNA;
	}
	else if(moisture < 0.6)
	{
		return TerrainType.Type.PLAINS;
	}
	else if(moisture < 0.8)
	{
		return TerrainType.Type.FOREST;
	}
	else
	{
		return TerrainType.Type.JUNGLE;
	}
}


//# Check if async generation thread has completed
public bool IsGenerationComplete()
{
	return _Thread != null && !_Thread.IsAlive();
}


//# Check if generation is in progress
public bool IsGenerating()
{
	return _IsGenerating;


	//# Finish async generation - MUST be called from main thread

}//# Applies thread results to grid, then runs rivers/features on main thread
public Dictionary FinishAsyncGeneration()
{
	if(!_Thread)
	{
		return new Dictionary{{"worker_time", 0},{"feature_time", 0},};
	}


	// Wait for thread and get results
	var result = _Thread.WaitToFinish();
	_Thread = null;

	EmitSignal("GenerationProgress", "applying", 0.3);


	// Apply terrain data to grid (main thread, safe to access grid)
	foreach(Variant cell_data in result["cells"])
	{
		var cell = Grid.GetCell(cell_data["q"], cell_data["r"]);
		if(cell)
		{
			cell.Elevation = cell_data["elevation"];
			cell.Moisture = cell_data["moisture"];
			cell.TerrainType = cell_data["terrain_type"];
		}
	}

	EmitSignal("GenerationProgress", "rivers", 0.5);


	// Generate rivers on main thread (requires grid traversal)
	var feature_start = Godot.Time.GetTicksMsec();
	_GenerateRivers(_PendingSeed);

	EmitSignal("GenerationProgress", "features", 0.75);


	// Generate features on main thread
	_GenerateFeatures(_PendingSeed);

	var feature_time = Godot.Time.GetTicksMsec() - feature_start;

	_IsGenerating = false;
	EmitSignal("GenerationProgress", "complete", 1.0);
	EmitSignal("GenerationCompleted", true, result["worker_time"], feature_time);

	return new Dictionary{
			{"worker_time", result["worker_time"]},
			{"feature_time", feature_time},
			};
}


//# Cancel ongoing generation (cleanup)
public void CancelGeneration()
{
	if(_Thread && _Thread.IsAlive())
	{

		// Can't actually cancel thread, but we can wait and discard results
		_Thread.WaitToFinish();
	}
	_Thread = null;
	_IsGenerating = false;
}

