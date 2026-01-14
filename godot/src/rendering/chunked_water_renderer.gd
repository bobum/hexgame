class_name ChunkedWaterRenderer
extends Node3D
## Renders water surface using chunked meshes for distance culling
## Matches terrain chunking for consistent visibility

const CHUNK_SIZE: float = 16.0
const MAX_RENDER_DISTANCE: float = 50.0  # Match terrain culling distance
const WATER_LEVEL: float = -0.05  # Slightly below terrain to avoid z-fighting
const DEEP_COLOR: Color = Color(0.102, 0.298, 0.431)  # 0x1a4c6e
const SHALLOW_COLOR: Color = Color(0.176, 0.545, 0.788)  # 0x2d8bc9

var water_material: ShaderMaterial

# Chunk storage: key -> WaterChunk
var chunks: Dictionary = {}


class WaterChunk:
	var mesh_instance: MeshInstance3D
	var chunk_x: int = 0
	var chunk_z: int = 0
	var center: Vector3 = Vector3.ZERO


func _init() -> void:
	water_material = _create_water_material()


func _get_chunk_key(cx: int, cz: int) -> String:
	return "%d,%d" % [cx, cz]


func _get_cell_chunk_coords(cell: HexCell) -> Vector2i:
	var coords = HexCoordinates.new(cell.q, cell.r)
	var world_pos = coords.to_world_position(0)
	var cx = int(floor(world_pos.x / CHUNK_SIZE))
	var cz = int(floor(world_pos.z / CHUNK_SIZE))
	return Vector2i(cx, cz)


func _get_chunk_center(cx: int, cz: int) -> Vector3:
	return Vector3((cx + 0.5) * CHUNK_SIZE, 0, (cz + 0.5) * CHUNK_SIZE)


## Build water meshes from grid
func build(grid: HexGrid) -> void:
	dispose()

	# Group water cells by chunk
	var chunk_cells: Dictionary = {}  # key -> Array of cells

	for cell in grid.get_all_cells():
		if cell.elevation < 0:  # Underwater
			var chunk_coords = _get_cell_chunk_coords(cell)
			var key = _get_chunk_key(chunk_coords.x, chunk_coords.y)

			if not chunks.has(key):
				var new_chunk = WaterChunk.new()
				new_chunk.chunk_x = chunk_coords.x
				new_chunk.chunk_z = chunk_coords.y
				new_chunk.center = _get_chunk_center(chunk_coords.x, chunk_coords.y)
				chunks[key] = new_chunk

			if not chunk_cells.has(key):
				chunk_cells[key] = []
			chunk_cells[key].append(cell)

	# Build mesh for each chunk
	var total_cells = 0

	for key in chunks:
		var chunk: WaterChunk = chunks[key]
		var cells = chunk_cells.get(key, [])

		if cells.size() > 0:
			var mesh = _build_water_mesh(cells)
			chunk.mesh_instance = MeshInstance3D.new()
			chunk.mesh_instance.mesh = mesh
			chunk.mesh_instance.material_override = water_material
			chunk.mesh_instance.name = "Water_%s" % key
			add_child(chunk.mesh_instance)
			total_cells += cells.size()

	print("Built water mesh: %d cells" % total_cells)


func _build_water_mesh(cells: Array) -> ArrayMesh:
	var st = SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	var corners = HexMetrics.get_corners()

	for cell in cells:
		var center = cell.get_world_position()
		center.y = WATER_LEVEL

		# Color based on depth
		var depth_factor = clampf(float(-cell.elevation) / 3.0, 0.0, 1.0)
		var color = SHALLOW_COLOR.lerp(DEEP_COLOR, depth_factor)

		# Build hexagonal water surface
		for i in range(6):
			var c1 = corners[i]
			var c2 = corners[(i + 1) % 6]

			st.set_normal(Vector3.UP)
			st.set_color(color)
			st.add_vertex(center)
			st.set_color(color)
			st.add_vertex(Vector3(center.x + c1.x, center.y, center.z + c1.z))
			st.set_color(color)
			st.add_vertex(Vector3(center.x + c2.x, center.y, center.z + c2.z))

	return st.commit()


## Update visibility based on camera - hide chunks beyond render distance
func update(camera: Camera3D) -> void:
	if not camera:
		return

	var camera_pos = camera.global_position
	var forward = -camera.global_transform.basis.z
	var view_center: Vector3

	if forward.y < -0.01:
		var t = -camera_pos.y / forward.y
		view_center = camera_pos + forward * t
	else:
		view_center = Vector3(camera_pos.x, 0, camera_pos.z)

	var max_dist_sq = MAX_RENDER_DISTANCE * MAX_RENDER_DISTANCE

	for key in chunks:
		var chunk: WaterChunk = chunks[key]
		if not chunk.mesh_instance:
			continue

		var dx = chunk.center.x - view_center.x
		var dz = chunk.center.z - view_center.z
		var dist_sq = dx * dx + dz * dz

		chunk.mesh_instance.visible = dist_sq <= max_dist_sq


## Update water animation
func update_animation(delta: float) -> void:
	if water_material:
		var current_time = water_material.get_shader_parameter("time")
		water_material.set_shader_parameter("time", current_time + delta)


func _create_water_material() -> ShaderMaterial:
	var shader = Shader.new()
	shader.code = """
shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_disabled;

uniform float time = 0.0;
uniform float wave_amplitude = 0.03;
uniform float wave_frequency = 2.0;
uniform float alpha = 0.85;

void vertex() {
	// Subtle wave animation
	VERTEX.y += sin(VERTEX.x * wave_frequency + time) * wave_amplitude;
	VERTEX.y += sin(VERTEX.z * wave_frequency * 0.8 + time * 0.8) * wave_amplitude;
}

void fragment() {
	// Use vertex color for base
	ALBEDO = COLOR.rgb;

	// Add subtle color variation based on position
	float variation = sin(VERTEX.x * 0.5 + time * 0.5) * 0.1;
	ALBEDO = mix(ALBEDO, ALBEDO * 1.2, variation);

	// Transparency
	ALPHA = alpha;

	// Slight roughness for water look
	ROUGHNESS = 0.3;
	METALLIC = 0.0;
}
"""

	var material = ShaderMaterial.new()
	material.shader = shader
	material.set_shader_parameter("time", 0.0)
	material.set_shader_parameter("wave_amplitude", 0.03)
	material.set_shader_parameter("wave_frequency", 2.0)
	material.set_shader_parameter("alpha", 0.85)

	return material


func dispose() -> void:
	for key in chunks:
		var chunk: WaterChunk = chunks[key]
		if chunk.mesh_instance:
			chunk.mesh_instance.queue_free()
	chunks.clear()
