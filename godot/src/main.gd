extends Node3D
## Main entry point for HexGame
## Manages game initialization and main loop

@onready var hex_grid_node: Node3D = $HexGrid
@onready var camera: MapCamera = $MapCamera

var grid: HexGrid
var map_generator: MapGenerator
var mesh_instance: MeshInstance3D
var water_instance: MeshInstance3D
var water_material: ShaderMaterial
var river_renderer: EdgeRiverRenderer
var hex_hover: HexHover
var current_seed: int = 0
var game_ui: GameUI

# Unit system
var unit_manager: UnitManager
var unit_renderer: UnitRenderer
var selection_manager: SelectionManager
var pathfinder: Pathfinder
var path_renderer: PathRenderer
var turn_manager: TurnManager

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
	game_ui.end_turn_requested.connect(_on_end_turn)
	game_ui.spawn_land_requested.connect(_on_spawn_land)
	game_ui.spawn_naval_requested.connect(_on_spawn_naval)
	game_ui.clear_units_requested.connect(_on_clear_units)
	game_ui.noise_param_changed.connect(_on_noise_param_changed)


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

	# Setup hover system
	_setup_hover()

	# Setup unit system
	_setup_units()


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

	# Build water
	_build_water()

	# Build rivers
	_build_rivers()

	# Center camera on map
	_center_camera()


func _setup_hover() -> void:
	hex_hover = HexHover.new()
	add_child(hex_hover)
	hex_hover.setup(grid, camera)

	# Connect hover signals to UI
	hex_hover.cell_hovered.connect(_on_cell_hovered)
	hex_hover.cell_unhovered.connect(_on_cell_unhovered)


func _on_cell_hovered(cell: HexCell) -> void:
	if game_ui:
		var terrain_name = TerrainType.get_terrain_name(cell.terrain_type)
		game_ui.set_hovered_hex(cell.q, cell.r, terrain_name)
	# Update path preview when hovering
	if selection_manager:
		selection_manager.update_path_preview(cell)


func _on_cell_unhovered() -> void:
	if game_ui:
		game_ui.clear_hovered_hex()
	# Clear path preview
	if selection_manager:
		selection_manager.clear_path_preview()


func _on_selection_changed(selected_ids: Array[int]) -> void:
	print("Selection changed: %d units selected" % selected_ids.size())
	# Could update UI here to show selected unit info


func _on_spawn_land(count: int) -> void:
	if unit_manager:
		var spawned = unit_manager.spawn_mixed_units(count, 0, 1)
		print("Spawned %d land units" % spawned["land"])
		if unit_renderer:
			unit_renderer.setup(unit_manager, grid)
			unit_renderer.build()
		_update_unit_counts()


func _on_spawn_naval(count: int) -> void:
	if unit_manager:
		var spawned = unit_manager.spawn_mixed_units(0, count, 1)
		print("Spawned %d naval units" % spawned["naval"])
		if unit_renderer:
			unit_renderer.setup(unit_manager, grid)
			unit_renderer.build()
		_update_unit_counts()


func _on_clear_units() -> void:
	if unit_manager:
		unit_manager.clear()
		print("Cleared all units")
		if unit_renderer:
			unit_renderer.setup(unit_manager, grid)
			unit_renderer.build()
		if selection_manager:
			selection_manager.clear_selection()
		_update_unit_counts()


func _update_unit_counts() -> void:
	if game_ui and unit_manager:
		var counts = unit_manager.get_unit_counts()
		game_ui.set_unit_counts(counts["land"], counts["naval"])


func _on_noise_param_changed(param: String, value: float) -> void:
	if map_generator:
		match param:
			"noise_scale":
				map_generator.noise_scale = value
			"octaves":
				map_generator.octaves = int(value)
			"sea_level":
				map_generator.sea_level = value
			"mountain_level":
				map_generator.mountain_level = value
			"river_percentage":
				map_generator.river_percentage = value
		# Regenerate with current settings
		_regenerate_map()


func _on_end_turn() -> void:
	if turn_manager:
		turn_manager.end_turn()
		_update_turn_display()
		print(turn_manager.get_status())


func _update_turn_display() -> void:
	if game_ui and turn_manager:
		game_ui.set_turn_status(turn_manager.get_status())


func _setup_units() -> void:
	# Create unit manager
	unit_manager = UnitManager.new(grid)

	# Create unit renderer
	unit_renderer = UnitRenderer.new()
	hex_grid_node.add_child(unit_renderer)
	unit_renderer.setup(unit_manager, grid)

	# Spawn some test units
	var spawned = unit_manager.spawn_mixed_units(10, 5, 1)
	print("Spawned %d land units, %d naval units" % [spawned["land"], spawned["naval"]])

	# Build unit meshes
	unit_renderer.build()

	# Setup pathfinder
	pathfinder = Pathfinder.new(grid, unit_manager)

	# Setup path renderer
	path_renderer = PathRenderer.new()
	hex_grid_node.add_child(path_renderer)
	path_renderer.setup(grid)

	# Setup turn manager (before selection manager so it can be passed)
	turn_manager = TurnManager.new(unit_manager)
	turn_manager.start_game()

	# Setup selection manager
	selection_manager = SelectionManager.new()
	add_child(selection_manager)
	selection_manager.setup(unit_manager, unit_renderer, grid, camera, pathfinder, path_renderer, turn_manager)
	selection_manager.selection_changed.connect(_on_selection_changed)

	_update_turn_display()
	_update_unit_counts()


func _build_water() -> void:
	var water_mesh = WaterRenderer.build_water_mesh(grid)
	if water_mesh == null:
		return

	water_instance = MeshInstance3D.new()
	water_instance.mesh = water_mesh

	# Create animated water material
	water_material = WaterRenderer.create_water_material()
	water_instance.material_override = water_material

	hex_grid_node.add_child(water_instance)
	print("Water mesh added to scene")


func _build_rivers() -> void:
	# Remove existing river renderer
	if river_renderer:
		river_renderer.queue_free()
		river_renderer = null

	river_renderer = EdgeRiverRenderer.new()
	hex_grid_node.add_child(river_renderer)
	river_renderer.setup(grid)
	river_renderer.build()


func _process(delta: float) -> void:
	# Update water animation
	if water_material:
		var current_time = water_material.get_shader_parameter("time")
		water_material.set_shader_parameter("time", current_time + delta)

	# Update unit renderer
	if unit_renderer:
		unit_renderer.update()

	# Update river animation
	if river_renderer:
		river_renderer.update(delta)


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
	# Remove old meshes
	if mesh_instance:
		mesh_instance.queue_free()
		mesh_instance = null
	if water_instance:
		water_instance.queue_free()
		water_instance = null
		water_material = null
	if river_renderer:
		river_renderer.queue_free()
		river_renderer = null

	# Clear units
	if unit_manager:
		unit_manager.clear()

	# Regenerate
	map_generator.generate(grid, current_seed)
	_build_terrain()

	# Update hover with new grid
	if hex_hover:
		hex_hover.setup(grid, camera)

	# Respawn units
	if unit_manager:
		var spawned = unit_manager.spawn_mixed_units(10, 5, 1)
		print("Respawned %d land, %d naval units" % [spawned["land"], spawned["naval"]])
		if unit_renderer:
			unit_renderer.setup(unit_manager, grid)
			unit_renderer.build()
		# Update pathfinder
		pathfinder = Pathfinder.new(grid, unit_manager)

		# Update turn manager
		turn_manager = TurnManager.new(unit_manager)
		turn_manager.start_game()

		if selection_manager:
			selection_manager.clear_selection()
			selection_manager.setup(unit_manager, unit_renderer, grid, camera, pathfinder, path_renderer, turn_manager)

		_update_turn_display()
		_update_unit_counts()


## Get the hex cell at a world position (for raycasting)
func get_cell_at_world_pos(world_pos: Vector3) -> HexCell:
	var coords = HexCoordinates.from_world_position(world_pos)
	return grid.get_cell(coords.q, coords.r)


## Regenerate with specific settings
func regenerate_with_settings(width: int, height: int, seed_val: int) -> void:
	map_width = width
	map_height = height
	current_seed = seed_val

	# Remove old meshes
	if mesh_instance:
		mesh_instance.queue_free()
		mesh_instance = null
	if water_instance:
		water_instance.queue_free()
		water_instance = null
		water_material = null
	if river_renderer:
		river_renderer.queue_free()
		river_renderer = null

	# Clear units
	if unit_manager:
		unit_manager.clear()

	# Reinitialize grid with new size
	grid = HexGrid.new(map_width, map_height)
	grid.initialize()

	# Generate and build
	map_generator.generate(grid, current_seed)
	_build_terrain()

	# Update hover with new grid
	if hex_hover:
		hex_hover.setup(grid, camera)

	# Setup new unit manager with new grid
	unit_manager = UnitManager.new(grid)
	var spawned = unit_manager.spawn_mixed_units(10, 5, 1)
	print("Spawned %d land, %d naval units" % [spawned["land"], spawned["naval"]])
	if unit_renderer:
		unit_renderer.setup(unit_manager, grid)
		unit_renderer.build()

	# Update pathfinder
	pathfinder = Pathfinder.new(grid, unit_manager)

	# Update turn manager
	turn_manager = TurnManager.new(unit_manager)
	turn_manager.start_game()

	if selection_manager:
		selection_manager.clear_selection()
		selection_manager.setup(unit_manager, unit_renderer, grid, camera, pathfinder, path_renderer, turn_manager)

	_update_turn_display()
	_update_unit_counts()
