extends Node3D
## Main entry point for HexGame
## Manages game initialization and main loop

@onready var hex_grid_node: Node3D = $HexGrid
@onready var camera: Camera3D = $Camera3D

var grid: HexGrid
var map_generator: MapGenerator
var mesh_instance: MeshInstance3D

# Map settings
const MAP_WIDTH: int = 32
const MAP_HEIGHT: int = 32


func _ready() -> void:
	print("HexGame starting...")
	_initialize_game()


func _initialize_game() -> void:
	# Initialize grid
	grid = HexGrid.new(MAP_WIDTH, MAP_HEIGHT)
	grid.initialize()
	print("Grid initialized: %dx%d cells" % [MAP_WIDTH, MAP_HEIGHT])

	# Generate terrain
	map_generator = MapGenerator.new()
	map_generator.generate(grid, randi())
	print("Map generated")

	# Build and display terrain mesh
	_build_terrain()


func _build_terrain() -> void:
	# Create mesh builder and generate geometry
	var builder = HexMeshBuilder.new()
	var mesh = builder.build_grid_mesh(grid)
	print("Mesh built: %d vertices" % [mesh.get_surface_count()])

	# Create mesh instance
	mesh_instance = MeshInstance3D.new()
	mesh_instance.mesh = mesh

	# Create material with vertex colors
	var material = StandardMaterial3D.new()
	material.vertex_color_use_as_albedo = true
	material.shading_mode = BaseMaterial3D.SHADING_MODE_PER_VERTEX
	mesh_instance.material_override = material

	# Add to scene
	hex_grid_node.add_child(mesh_instance)
	print("Terrain mesh added to scene")

	# Center camera on map
	_center_camera()


func _center_camera() -> void:
	# Calculate map center
	var center_q = MAP_WIDTH / 2
	var center_r = MAP_HEIGHT / 2
	var center_coords = HexCoordinates.new(center_q, center_r)
	var center_pos = center_coords.to_world_position(0)

	# Position camera to look at center
	var cam_height = 20.0
	var cam_distance = 20.0
	camera.position = Vector3(center_pos.x, cam_height, center_pos.z + cam_distance)
	camera.look_at(center_pos, Vector3.UP)


func _process(_delta: float) -> void:
	# Camera controls
	_handle_camera_input(_delta)


func _handle_camera_input(delta: float) -> void:
	var move_speed = 15.0 * delta
	var zoom_speed = 20.0 * delta

	# WASD movement
	if Input.is_key_pressed(KEY_W):
		camera.position.z -= move_speed
	if Input.is_key_pressed(KEY_S):
		camera.position.z += move_speed
	if Input.is_key_pressed(KEY_A):
		camera.position.x -= move_speed
	if Input.is_key_pressed(KEY_D):
		camera.position.x += move_speed

	# Zoom with Q/E
	if Input.is_key_pressed(KEY_Q):
		camera.position.y += zoom_speed
	if Input.is_key_pressed(KEY_E):
		camera.position.y -= zoom_speed
		camera.position.y = max(5.0, camera.position.y)  # Min height


func _input(event: InputEvent) -> void:
	# Regenerate map with R key
	if event is InputEventKey and event.pressed and event.keycode == KEY_R:
		print("Regenerating map...")
		_regenerate_map()


func _regenerate_map() -> void:
	# Remove old mesh
	if mesh_instance:
		mesh_instance.queue_free()
		mesh_instance = null

	# Regenerate
	map_generator.generate(grid, randi())
	_build_terrain()
