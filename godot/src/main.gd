extends Node3D
## Main entry point for HexGame
## Manages game initialization and main loop

const ChunkedWaterRendererClass = preload("res://src/rendering/chunked_water_renderer.gd")
const ChunkedRiverRendererClass = preload("res://src/rendering/chunked_river_renderer.gd")
const ScreenshotCaptureClass = preload("res://src/debug/screenshot_capture.gd")
const PerformanceMonitorClass = preload("res://src/debug/performance_monitor.gd")

@onready var hex_grid_node: Node3D = $HexGrid
@onready var camera: MapCamera = $MapCamera
@onready var directional_light: DirectionalLight3D = $DirectionalLight3D
@onready var world_env: WorldEnvironment = $WorldEnvironment

var grid: HexGrid
var map_generator: MapGenerator
var chunked_terrain: ChunkedTerrainRenderer
var feature_renderer: FeatureRenderer
var chunked_water: Node3D  # ChunkedWaterRenderer
var chunked_rivers: Node3D  # ChunkedRiverRenderer
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
var screenshot_capture: Node  # ScreenshotCapture
var performance_monitor: Control  # PerformanceMonitor

# Map settings
var map_width: int = 32
var map_height: int = 32

# Async generation
var use_async_generation: bool = true
var _async_generation_pending: bool = false
var _async_needs_new_units: bool = false  # True when grid size changed

# UI scene
const GameUIScene = preload("res://scenes/game_ui.tscn")


func _ready() -> void:
	print("HexGame starting...")

	# Uncap FPS (default VSync limits to monitor refresh rate)
	DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_DISABLED)
	Engine.max_fps = 0  # 0 = uncapped

	current_seed = randi()
	_setup_ui()
	_initialize_game()
	_setup_screenshot_capture()
	_setup_performance_monitor()


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
	game_ui.spawn_ai_requested.connect(_on_spawn_ai)
	game_ui.clear_units_requested.connect(_on_clear_units)
	game_ui.noise_param_changed.connect(_on_noise_param_changed)
	game_ui.shader_param_changed.connect(_on_shader_param_changed)
	game_ui.lighting_param_changed.connect(_on_lighting_param_changed)
	game_ui.fog_param_changed.connect(_on_fog_param_changed)
	game_ui.async_toggle_changed.connect(_on_async_toggle_changed)


func _setup_screenshot_capture() -> void:
	screenshot_capture = ScreenshotCaptureClass.new()
	add_child(screenshot_capture)
	screenshot_capture.setup(camera)
	print("Screenshot capture ready - Auto-capture enabled")


func _setup_performance_monitor() -> void:
	performance_monitor = PerformanceMonitorClass.new()
	add_child(performance_monitor)


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

	# Build features (trees, rocks)
	_build_features()

	# Setup hover system
	_setup_hover()

	# Setup unit system
	_setup_units()


func _build_terrain() -> void:
	# Create chunked terrain renderer
	chunked_terrain = ChunkedTerrainRenderer.new()
	hex_grid_node.add_child(chunked_terrain)
	chunked_terrain.build(grid)

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


func _on_spawn_ai(land: int, naval: int) -> void:
	if unit_manager:
		var spawned = unit_manager.spawn_mixed_units(land, naval, 2)  # AI is player 2
		print("Spawned %d land, %d naval AI units" % [spawned["land"], spawned["naval"]])
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
		# Update pool stats
		var pool_stats = unit_manager.get_pool_stats()
		game_ui.set_pool_stats(pool_stats["active"], pool_stats["created"], pool_stats["reuse_rate"])


func _on_noise_param_changed(param: String, value: float) -> void:
	# Handle flow_speed separately as it doesn't require map regeneration
	if param == "flow_speed":
		if chunked_rivers and chunked_rivers.material:
			chunked_rivers.material.set_shader_parameter("flow_speed", value)
		return

	if map_generator:
		match param:
			"noise_scale":
				map_generator.noise_scale = value
			"octaves":
				map_generator.octaves = int(value)
			"persistence":
				map_generator.persistence = value
			"lacunarity":
				map_generator.lacunarity = value
			"sea_level":
				map_generator.sea_level = value
			"mountain_level":
				map_generator.mountain_level = value
			"river_percentage":
				map_generator.river_percentage = value
		# Regenerate with current settings
		_regenerate_map()


func _on_shader_param_changed(param: String, value: float) -> void:
	if chunked_terrain and chunked_terrain.terrain_material:
		chunked_terrain.terrain_material.set_shader_parameter(param, value)


func _on_lighting_param_changed(param: String, value: float) -> void:
	match param:
		"ambient_energy":
			if world_env and world_env.environment:
				world_env.environment.ambient_light_energy = value
		"light_energy":
			if directional_light:
				directional_light.light_energy = value


func _on_fog_param_changed(param: String, value: float) -> void:
	if world_env and world_env.environment:
		match param:
			"fog_near":
				world_env.environment.fog_depth_begin = value
			"fog_far":
				world_env.environment.fog_depth_end = value
			"fog_density":
				world_env.environment.fog_light_energy = value


func _on_async_toggle_changed(enabled: bool) -> void:
	use_async_generation = enabled
	print("Async generation: %s" % ("enabled" if enabled else "disabled"))


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
	unit_manager.setup_pool()  # Initialize object pooling
	unit_manager.prewarm_pool(50)  # Pre-create units for faster spawning

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


func _build_features() -> void:
	# Remove old features
	if feature_renderer:
		feature_renderer.queue_free()
		feature_renderer = null

	feature_renderer = FeatureRenderer.new()
	hex_grid_node.add_child(feature_renderer)
	feature_renderer.build(grid)


func _build_water() -> void:
	# Remove old water
	if chunked_water:
		chunked_water.dispose()
		chunked_water.queue_free()
		chunked_water = null

	chunked_water = ChunkedWaterRendererClass.new()
	hex_grid_node.add_child(chunked_water)
	chunked_water.build(grid)
	print("Water mesh added to scene")


func _build_rivers() -> void:
	# Remove existing river renderer
	if chunked_rivers:
		chunked_rivers.dispose()
		chunked_rivers.queue_free()
		chunked_rivers = null

	chunked_rivers = ChunkedRiverRendererClass.new()
	hex_grid_node.add_child(chunked_rivers)
	chunked_rivers.setup(grid)
	chunked_rivers.build()


func _process(delta: float) -> void:
	# Check for async generation completion
	if _async_generation_pending and map_generator and map_generator.is_generation_complete():
		_finish_async_generation()

	# Update water animation and visibility
	if chunked_water:
		chunked_water.update_animation(delta)
		if camera:
			chunked_water.update(camera)

	# Update unit renderer
	if unit_renderer:
		unit_renderer.update()
		if camera:
			unit_renderer.update_visibility(camera)

	# Update river animation and visibility
	if chunked_rivers:
		chunked_rivers.update_animation(delta)
		if camera:
			chunked_rivers.update(camera)

	# Update terrain visibility and LOD based on camera
	if chunked_terrain and camera:
		chunked_terrain.update(camera)

	# Update feature visibility based on camera distance
	if feature_renderer and camera:
		feature_renderer.update(camera)


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
		elif event.keycode == KEY_P:
			if performance_monitor:
				performance_monitor.toggle_graph()


func _regenerate_map() -> void:
	# Cancel any pending async generation
	if _async_generation_pending and map_generator:
		map_generator.cancel_generation()
		_async_generation_pending = false

	# Remove old terrain
	if chunked_terrain:
		chunked_terrain.dispose()
		chunked_terrain.queue_free()
		chunked_terrain = null
	if chunked_water:
		chunked_water.dispose()
		chunked_water.queue_free()
		chunked_water = null
	if chunked_rivers:
		chunked_rivers.dispose()
		chunked_rivers.queue_free()
		chunked_rivers = null
	if feature_renderer:
		feature_renderer.dispose()
		feature_renderer.queue_free()
		feature_renderer = null

	# Clear units
	if unit_manager:
		unit_manager.clear()

	# Regenerate - async or sync based on setting
	if use_async_generation:
		_async_generation_pending = true
		_async_needs_new_units = false  # Same grid, respawn units
		map_generator.generate_async(grid, current_seed)
		print("Async map generation started with seed: %d" % current_seed)
		if game_ui:
			game_ui.show_generation_status("Generating terrain...")
	else:
		map_generator.generate(grid, current_seed)
		print("Map generated (sync) with seed: %d" % current_seed)
		_finish_map_build()


## Called when async generation completes
func _finish_async_generation() -> void:
	_async_generation_pending = false
	var result = map_generator.finish_async_generation()
	print("Map generated (async): worker=%dms, features=%dms" % [result["worker_time"], result["feature_time"]])
	if game_ui:
		game_ui.hide_generation_status()

	if _async_needs_new_units:
		_finish_map_build_with_new_units()
	else:
		_finish_map_build()


## Common map building after generation completes
func _finish_map_build() -> void:
	_build_terrain()
	_build_features()

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


## Map building when grid size changed (needs new unit manager)
func _finish_map_build_with_new_units() -> void:
	_build_terrain()
	_build_features()

	# Update hover with new grid
	if hex_hover:
		hex_hover.setup(grid, camera)

	# Setup new unit manager with new grid
	unit_manager = UnitManager.new(grid)
	unit_manager.setup_pool()
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


## Get the hex cell at a world position (for raycasting)
func get_cell_at_world_pos(world_pos: Vector3) -> HexCell:
	var coords = HexCoordinates.from_world_position(world_pos)
	return grid.get_cell(coords.q, coords.r)


## Regenerate with specific settings
func regenerate_with_settings(width: int, height: int, seed_val: int) -> void:
	# Cancel any pending async generation
	if _async_generation_pending and map_generator:
		map_generator.cancel_generation()
		_async_generation_pending = false

	map_width = width
	map_height = height
	current_seed = seed_val

	# Remove old terrain
	if chunked_terrain:
		chunked_terrain.dispose()
		chunked_terrain.queue_free()
		chunked_terrain = null
	if chunked_water:
		chunked_water.dispose()
		chunked_water.queue_free()
		chunked_water = null
	if chunked_rivers:
		chunked_rivers.dispose()
		chunked_rivers.queue_free()
		chunked_rivers = null
	if feature_renderer:
		feature_renderer.dispose()
		feature_renderer.queue_free()
		feature_renderer = null

	# Clear units
	if unit_manager:
		unit_manager.clear()

	# Reinitialize grid with new size
	grid = HexGrid.new(map_width, map_height)
	grid.initialize()

	# Generate - async or sync based on setting
	if use_async_generation:
		_async_generation_pending = true
		_async_needs_new_units = true  # New grid size, need new unit manager
		map_generator.generate_async(grid, current_seed)
		print("Async map generation started with seed: %d (size: %dx%d)" % [current_seed, map_width, map_height])
		if game_ui:
			game_ui.show_generation_status("Generating terrain...")
	else:
		map_generator.generate(grid, current_seed)
		print("Map generated (sync) with seed: %d" % current_seed)
		_finish_map_build_with_new_units()
