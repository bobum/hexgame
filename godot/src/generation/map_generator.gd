class_name MapGenerator
extends RefCounted
## Procedural map generation using noise
## Matches web/src/generation/MapGenerator.ts

var noise: FastNoiseLite
var grid: HexGrid

# Generation parameters
var sea_level: float = 0.35
var mountain_level: float = 0.75


func _init() -> void:
	noise = FastNoiseLite.new()
	noise.noise_type = FastNoiseLite.TYPE_SIMPLEX
	noise.frequency = 0.02
	noise.fractal_octaves = 4


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
