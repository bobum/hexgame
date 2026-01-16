class_name MapGenerator
extends RefCounted
## Procedural map generation using noise
## Matches web/src/generation/MapGenerator.ts
## Supports both sync and async (threaded) generation

const FeatureClass = preload("res://src/core/feature.gd")

# Signals for async generation progress
signal generation_started()
signal generation_progress(phase: String, percent: float)
signal generation_completed(success: bool, worker_time_ms: float, feature_time_ms: float)

var noise: FastNoiseLite
var grid: HexGrid
var river_generator: RiverGenerator

# Threading support
var _thread: Thread
var _is_generating: bool = false
var _pending_seed: int = 0

# Generation parameters
var sea_level: float = 0.35
var mountain_level: float = 0.75
var river_percentage: float = 0.1

# Noise parameters (exposed for UI)
var noise_scale: float = 0.02:
	set(val):
		noise_scale = val
		if noise:
			noise.frequency = val

var octaves: int = 4:
	set(val):
		octaves = val
		if noise:
			noise.fractal_octaves = val

var persistence: float = 0.5:
	set(val):
		persistence = val
		if noise:
			noise.fractal_gain = val

var lacunarity: float = 2.0:
	set(val):
		lacunarity = val
		if noise:
			noise.fractal_lacunarity = val

var land_percentage: float = 0.5  # Inverse of sea_level essentially


func _init() -> void:
	noise = FastNoiseLite.new()
	noise.noise_type = FastNoiseLite.TYPE_SIMPLEX
	noise.frequency = 0.02
	noise.fractal_octaves = 4
	noise.fractal_gain = 0.5
	noise.fractal_lacunarity = 2.0


## Generate a new map
func generate(hex_grid: HexGrid, seed_val: int = 0) -> void:
	grid = hex_grid
	noise.seed = seed_val if seed_val != 0 else randi()

	# Generate elevation
	_generate_elevation()

	# Generate moisture
	_generate_moisture()

	# Assign biomes
	_assign_biomes()

	# Generate rivers
	_generate_rivers(seed_val)

	# Generate features (trees, rocks)
	_generate_features(seed_val)


func _generate_elevation() -> void:
	for cell in grid.get_all_cells():
		var world_pos = cell.get_world_position()
		var noise_val = (noise.get_noise_2d(world_pos.x, world_pos.z) + 1.0) / 2.0

		# Convert to elevation
		if noise_val < sea_level:
			cell.elevation = HexMetrics.MIN_ELEVATION
		else:
			var normalized = (noise_val - sea_level) / (1.0 - sea_level)
			cell.elevation = int(normalized * HexMetrics.MAX_ELEVATION)


func _generate_moisture() -> void:
	var moisture_noise = FastNoiseLite.new()
	moisture_noise.noise_type = FastNoiseLite.TYPE_SIMPLEX
	moisture_noise.seed = noise.seed + 1000
	moisture_noise.frequency = 0.03

	for cell in grid.get_all_cells():
		var world_pos = cell.get_world_position()
		cell.moisture = (moisture_noise.get_noise_2d(world_pos.x, world_pos.z) + 1.0) / 2.0


func _assign_biomes() -> void:
	for cell in grid.get_all_cells():
		cell.terrain_type = _get_biome(cell.elevation, cell.moisture)


func _get_biome(elevation: int, moisture: float) -> TerrainType.Type:
	# Water
	if elevation < HexMetrics.WATER_LEVEL:
		if elevation < HexMetrics.WATER_LEVEL - 1:
			return TerrainType.Type.OCEAN
		return TerrainType.Type.COAST

	# High elevation
	if elevation >= 6:
		return TerrainType.Type.SNOW
	if elevation >= 4:
		return TerrainType.Type.MOUNTAINS

	# Land biomes based on moisture
	if moisture < 0.2:
		return TerrainType.Type.DESERT
	elif moisture < 0.4:
		if elevation >= 2:
			return TerrainType.Type.HILLS
		return TerrainType.Type.SAVANNA
	elif moisture < 0.6:
		return TerrainType.Type.PLAINS
	elif moisture < 0.8:
		return TerrainType.Type.FOREST
	else:
		return TerrainType.Type.JUNGLE


func _generate_rivers(seed_val: int) -> void:
	if river_percentage <= 0:
		return

	river_generator = RiverGenerator.new(grid)
	river_generator.generate(seed_val, river_percentage)

	# Count rivers for debugging
	var river_count = 0
	for cell in grid.get_all_cells():
		river_count += cell.river_directions.size()
	if river_count > 0:
		print("Generated %d river segments" % river_count)


func _generate_features(seed_val: int) -> void:
	var rng = RandomNumberGenerator.new()
	rng.seed = seed_val + 2000

	# Clear existing features from all cells before generating new ones
	for cell in grid.get_all_cells():
		cell.features.clear()

	var tree_count = 0
	var rock_count = 0

	for cell in grid.get_all_cells():
		# Skip water cells
		if cell.is_underwater():
			continue

		# Skip cells with rivers
		if cell.has_river:
			continue

		# Feature density based on terrain type
		var tree_chance = 0.0
		var rock_chance = 0.0

		match cell.terrain_type:
			TerrainType.Type.FOREST:
				tree_chance = 0.7
				rock_chance = 0.1
			TerrainType.Type.JUNGLE:
				tree_chance = 0.85
				rock_chance = 0.05
			TerrainType.Type.PLAINS:
				tree_chance = 0.15
				rock_chance = 0.1
			TerrainType.Type.SAVANNA:
				tree_chance = 0.1
				rock_chance = 0.15
			TerrainType.Type.HILLS:
				tree_chance = 0.2
				rock_chance = 0.3
			TerrainType.Type.MOUNTAINS:
				tree_chance = 0.05
				rock_chance = 0.4
			TerrainType.Type.DESERT:
				rock_chance = 0.2
			TerrainType.Type.SNOW:
				rock_chance = 0.15

		# Generate features
		var center = cell.get_world_position()

		# Try to place trees
		if tree_chance > 0 and rng.randf() < tree_chance:
			var num_trees = rng.randi_range(1, 3)
			for _i in range(num_trees):
				var offset = Vector3(
					rng.randf_range(-0.3, 0.3),
					0,
					rng.randf_range(-0.3, 0.3)
				)
				var feature = FeatureClass.new(
					FeatureClass.Type.TREE,
					center + offset,
					rng.randf() * TAU,
					rng.randf_range(0.8, 1.2)
				)
				cell.features.append(feature)
				tree_count += 1

		# Try to place rocks
		if rock_chance > 0 and rng.randf() < rock_chance:
			var num_rocks = rng.randi_range(1, 2)
			for _i in range(num_rocks):
				var offset = Vector3(
					rng.randf_range(-0.35, 0.35),
					0,
					rng.randf_range(-0.35, 0.35)
				)
				var feature = FeatureClass.new(
					FeatureClass.Type.ROCK,
					center + offset,
					rng.randf() * TAU,
					rng.randf_range(0.6, 1.4)
				)
				cell.features.append(feature)
				rock_count += 1

	if tree_count > 0 or rock_count > 0:
		print("Generated %d trees, %d rocks" % [tree_count, rock_count])


# =============================================================================
# ASYNC GENERATION METHODS
# =============================================================================

## Start async generation in background thread
## Call is_generation_complete() in _process() to check when done
## Then call finish_async_generation() to apply results
func generate_async(hex_grid: HexGrid, seed_val: int = 0) -> void:
	if _is_generating:
		push_error("MapGenerator: Generation already in progress")
		return

	grid = hex_grid
	_pending_seed = seed_val if seed_val != 0 else randi()
	_is_generating = true

	generation_started.emit()
	generation_progress.emit("terrain", 0.0)

	# Prepare input data for thread (no objects, just primitives)
	var input = {
		"width": grid.width,
		"height": grid.height,
		"seed": _pending_seed,
		"noise_scale": noise_scale,
		"octaves": octaves,
		"persistence": persistence,
		"lacunarity": lacunarity,
		"sea_level": sea_level,
	}

	# Start background thread
	_thread = Thread.new()
	_thread.start(_thread_generate_terrain.bind(input))


## Thread worker function - generates terrain data only (no grid access)
## Returns Dictionary with cells array and timing info
func _thread_generate_terrain(input: Dictionary) -> Dictionary:
	var start_time = Time.get_ticks_msec()

	# Create noise generators in thread (thread-safe)
	var elev_noise = FastNoiseLite.new()
	elev_noise.noise_type = FastNoiseLite.TYPE_SIMPLEX
	elev_noise.seed = input["seed"]
	elev_noise.frequency = input["noise_scale"]
	elev_noise.fractal_octaves = input["octaves"]
	elev_noise.fractal_gain = input["persistence"]
	elev_noise.fractal_lacunarity = input["lacunarity"]

	var moist_noise = FastNoiseLite.new()
	moist_noise.noise_type = FastNoiseLite.TYPE_SIMPLEX
	moist_noise.seed = input["seed"] + 1000
	moist_noise.frequency = 0.03

	var cells: Array[Dictionary] = []
	var width = input["width"]
	var height = input["height"]
	var sea_lvl = input["sea_level"]

	# Generate all cells
	for r in range(height):
		for q in range(width):
			# Calculate world position (same as HexCoordinates.to_world_position)
			var x = (q + r * 0.5) * (HexMetrics.INNER_RADIUS * 2.0)
			var z = r * (HexMetrics.OUTER_RADIUS * 1.5)

			# Elevation from noise
			var noise_val = (elev_noise.get_noise_2d(x, z) + 1.0) / 2.0
			var elevation: int
			if noise_val < sea_lvl:
				elevation = HexMetrics.MIN_ELEVATION
			else:
				var normalized = (noise_val - sea_lvl) / (1.0 - sea_lvl)
				elevation = int(normalized * HexMetrics.MAX_ELEVATION)

			# Moisture from noise
			var moisture = (moist_noise.get_noise_2d(x, z) + 1.0) / 2.0

			# Determine terrain type (static function for thread safety)
			var terrain_type = _get_biome_for_thread(elevation, moisture)

			cells.append({
				"q": q,
				"r": r,
				"elevation": elevation,
				"moisture": moisture,
				"terrain_type": terrain_type
			})

	return {
		"cells": cells,
		"worker_time": Time.get_ticks_msec() - start_time
	}


## Static biome function for thread (avoids instance access)
static func _get_biome_for_thread(elevation: int, moisture: float) -> int:
	# Water
	if elevation < HexMetrics.WATER_LEVEL:
		if elevation < HexMetrics.WATER_LEVEL - 1:
			return TerrainType.Type.OCEAN
		return TerrainType.Type.COAST

	# High elevation
	if elevation >= 6:
		return TerrainType.Type.SNOW
	if elevation >= 4:
		return TerrainType.Type.MOUNTAINS

	# Land biomes based on moisture
	if moisture < 0.2:
		return TerrainType.Type.DESERT
	elif moisture < 0.4:
		if elevation >= 2:
			return TerrainType.Type.HILLS
		return TerrainType.Type.SAVANNA
	elif moisture < 0.6:
		return TerrainType.Type.PLAINS
	elif moisture < 0.8:
		return TerrainType.Type.FOREST
	else:
		return TerrainType.Type.JUNGLE


## Check if async generation thread has completed
func is_generation_complete() -> bool:
	return _thread != null and not _thread.is_alive()


## Check if generation is in progress
func is_generating() -> bool:
	return _is_generating


## Finish async generation - MUST be called from main thread
## Applies thread results to grid, then runs rivers/features on main thread
func finish_async_generation() -> Dictionary:
	if not _thread:
		return {"worker_time": 0, "feature_time": 0}

	# Wait for thread and get results
	var result = _thread.wait_to_finish()
	_thread = null

	generation_progress.emit("applying", 0.3)

	# Apply terrain data to grid (main thread, safe to access grid)
	for cell_data in result["cells"]:
		var cell = grid.get_cell(cell_data["q"], cell_data["r"])
		if cell:
			cell.elevation = cell_data["elevation"]
			cell.moisture = cell_data["moisture"]
			cell.terrain_type = cell_data["terrain_type"]

	generation_progress.emit("rivers", 0.5)

	# Generate rivers on main thread (requires grid traversal)
	var feature_start = Time.get_ticks_msec()
	_generate_rivers(_pending_seed)

	generation_progress.emit("features", 0.75)

	# Generate features on main thread
	_generate_features(_pending_seed)

	var feature_time = Time.get_ticks_msec() - feature_start

	_is_generating = false
	generation_progress.emit("complete", 1.0)
	generation_completed.emit(true, result["worker_time"], feature_time)

	return {
		"worker_time": result["worker_time"],
		"feature_time": feature_time
	}


## Cancel ongoing generation (cleanup)
func cancel_generation() -> void:
	if _thread and _thread.is_alive():
		# Can't actually cancel thread, but we can wait and discard results
		_thread.wait_to_finish()
	_thread = null
	_is_generating = false
