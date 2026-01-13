extends Node3D
## Main entry point for HexGame
## Manages game initialization and main loop

@onready var hex_grid_node: Node3D = $HexGrid
@onready var camera: MapCamera = $MapCamera

var grid: HexGrid
var map_generator: MapGenerator
var mesh_instance: MeshInstance3D
var current_seed: int = 0
var game_ui: GameUI

# Map settings
var map_width: int = 32
var map_height: int = 32

# UI scene
const GameUIScene = preload("res://scenes/game_ui.tscn")


func _ready() -> void:
	print("HexGame starting...")
	current_seed = randi()
	_setup_ui()
	_initialize_game()


func _setup_ui() -> void:
	# Create and add UI
	game_ui = GameUIScene.instantiate()
	add_child(game_ui)
	game_ui.set_main_node(self)
	game_ui.set_seed(current_seed)

	# Connect signals
	game_ui.regenerate_requested.connect(_on_ui_regenerate)
	game_ui.random_seed_requested.connect(_on_ui_random_seed)


func _on_ui_regenerate(width: int, height: int, seed_val: int) -> void:
	regenerate_with_settings(width, height, seed_val)
	game_ui.set_seed(current_seed)


func _on_ui_random_seed() -> void:
	current_seed = randi()
	game_ui.set_seed(current_seed)
	_regenerate_map()


func _initialize_game() -> void:
	# Initialize grid
	grid = HexGrid.new(map_width, map_height)
	grid.initialize()
	print("Grid initialized: %dx%d cells" % [map_width, map_height])

	# Generate terrain
	map_generator = MapGenerator.new()
	map_generator.generate(grid, current_seed)
	print("Map generated with seed: %d" % current_seed)

	# Build and display terrain mesh
	_build_terrain()


func _build_terrain() -> void:
	# Create mesh builder and generate geometry
	var builder = HexMeshBuilder.new()
	var mesh = builder.build_grid_mesh(grid)

	# Create mesh instance
	mesh_instance = MeshInstance3D.new()
	mesh_instance.mesh = mesh

	# Create material with vertex colors
	var material = StandardMaterial3D.new()
	material.vertex_color_use_as_albedo = true
	material.shading_mode = BaseMaterial3D.SHADING_MODE_PER_VERTEX
	material.cull_mode = BaseMaterial3D.CULL_DISABLED  # Render both sides
	mesh_instance.material_override = material

	# Add to scene
	hex_grid_node.add_child(mesh_instance)
	print("Terrain mesh added to scene")

	# Center camera on map
	_center_camera()


func _center_camera() -> void:
	# Calculate map center
	var center_q = map_width / 2
	var center_r = map_height / 2
	var center_coords = HexCoordinates.new(center_q, center_r)
	var center_pos = center_coords.to_world_position(0)

	# Focus camera on map center
	camera.set_target(center_pos)
	camera.target_distance = 40.0
	camera.target_pitch = 50.0


func _input(event: InputEvent) -> void:
	# Regenerate map with Space key (R is now used for camera tilt)
	if event is InputEventKey and event.pressed:
		if event.keycode == KEY_SPACE:
			print("Regenerating map with new seed...")
			current_seed = randi()
			_regenerate_map()
		elif event.keycode == KEY_G:
			print("Regenerating map with same seed...")
			_regenerate_map()


func _regenerate_map() -> void:
	# Remove old mesh
	if mesh_instance:
		mesh_instance.queue_free()
		mesh_instance = null

	# Regenerate
	map_generator.generate(grid, current_seed)
	_build_terrain()


## Get the hex cell at a world position (for raycasting)
func get_cell_at_world_pos(world_pos: Vector3) -> HexCell:
	var coords = HexCoordinates.from_world_position(world_pos)
	return grid.get_cell(coords.q, coords.r)


## Regenerate with specific settings
func regenerate_with_settings(width: int, height: int, seed_val: int) -> void:
	map_width = width
	map_height = height
	current_seed = seed_val

	# Remove old mesh
	if mesh_instance:
		mesh_instance.queue_free()
		mesh_instance = null

	# Reinitialize grid with new size
	grid = HexGrid.new(map_width, map_height)
	grid.initialize()

	# Generate and build
	map_generator.generate(grid, current_seed)
	_build_terrain()
