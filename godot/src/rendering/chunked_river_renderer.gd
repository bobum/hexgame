class_name ChunkedRiverRenderer
extends Node3D
## Renders rivers using chunked meshes for distance culling
## Rivers follow hex edges, rendered per chunk

const CHUNK_SIZE: float = 16.0
const MAX_RENDER_DISTANCE: float = 50.0
const RIVER_WIDTH: float = 0.15
const HEIGHT_OFFSET: float = 0.02

var grid: HexGrid
var material: ShaderMaterial
var time: float = 0.0

# Chunk storage
var chunks: Dictionary = {}

# Direction to corner mapping
const DIRECTION_TO_CORNERS: Array[Vector2i] = [
	Vector2i(5, 0), Vector2i(4, 5), Vector2i(3, 4),
	Vector2i(2, 3), Vector2i(1, 2), Vector2i(0, 1),
]


class RiverChunk:
	var mesh_instance: MeshInstance3D
	var chunk_x: int = 0
	var chunk_z: int = 0
	var center: Vector3 = Vector3.ZERO


func _get_chunk_key(cx: int, cz: int) -> String:
	return "%d,%d" % [cx, cz]


func _get_cell_chunk_coords(cell: HexCell) -> Vector2i:
	var coords = HexCoordinates.new(cell.q, cell.r)
	var world_pos = coords.to_world_position(0)
	return Vector2i(int(floor(world_pos.x / CHUNK_SIZE)), int(floor(world_pos.z / CHUNK_SIZE)))


func _get_chunk_center(cx: int, cz: int) -> Vector3:
	return Vector3((cx + 0.5) * CHUNK_SIZE, 0, (cz + 0.5) * CHUNK_SIZE)


func setup(p_grid: HexGrid) -> void:
	grid = p_grid


func build() -> void:
	dispose()
	material = _create_river_material()

	# Build incoming rivers map
	var incoming_rivers: Dictionary = {}
	for cell in grid.get_all_cells():
		for dir in cell.river_directions:
			var neighbor = grid.get_neighbor(cell, dir)
			if neighbor:
				var key = "%d,%d" % [neighbor.q, neighbor.r]
				if not incoming_rivers.has(key):
					incoming_rivers[key] = []
				incoming_rivers[key].append(HexDirection.opposite(dir))

	# Group river cells by chunk
	var chunk_river_cells: Dictionary = {}
	for cell in grid.get_all_cells():
		var outgoing = cell.river_directions
		var incoming_key = "%d,%d" % [cell.q, cell.r]
		var incoming = incoming_rivers.get(incoming_key, [])

		if outgoing.is_empty() and incoming.is_empty():
			continue

		var chunk_coords = _get_cell_chunk_coords(cell)
		var key = _get_chunk_key(chunk_coords.x, chunk_coords.y)

		if not chunks.has(key):
			var new_chunk = RiverChunk.new()
			new_chunk.chunk_x = chunk_coords.x
			new_chunk.chunk_z = chunk_coords.y
			new_chunk.center = _get_chunk_center(chunk_coords.x, chunk_coords.y)
			chunks[key] = new_chunk

		if not chunk_river_cells.has(key):
			chunk_river_cells[key] = []
		chunk_river_cells[key].append({
			"cell": cell,
			"outgoing": outgoing,
			"incoming": incoming
		})

	# Build mesh for each chunk
	var total_verts = 0
	for key in chunks:
		var chunk: RiverChunk = chunks[key]
		var river_data = chunk_river_cells.get(key, [])
		if river_data.size() > 0:
			var mesh = _build_river_mesh(river_data)
			if mesh:
				chunk.mesh_instance = MeshInstance3D.new()
				chunk.mesh_instance.mesh = mesh
				chunk.mesh_instance.material_override = material
				chunk.mesh_instance.name = "River_%s" % key
				add_child(chunk.mesh_instance)
				total_verts += mesh.get_faces().size()

	print("Built river mesh: %d vertices" % total_verts)


func _build_river_mesh(river_data: Array) -> ArrayMesh:
	var vertices: PackedVector3Array = PackedVector3Array()
	var uvs: PackedVector2Array = PackedVector2Array()
	var indices: PackedInt32Array = PackedInt32Array()
	var vertex_index: int = 0

	var corners = HexMetrics.get_corners()
	var half_width = RIVER_WIDTH
	var rendered_edges: Dictionary = {}

	for data in river_data:
		var cell: HexCell = data["cell"]
		var outgoing: Array = data["outgoing"]
		var incoming: Array = data["incoming"]

		var center_pos = cell.get_world_position()
		var world_corners: Array[Vector3] = []
		for c in corners:
			world_corners.append(Vector3(center_pos.x + c.x, 0, center_pos.z + c.z))

		# Draw outgoing river edges
		for out_dir in outgoing:
			var neighbor = grid.get_neighbor(cell, out_dir)
			if not neighbor:
				continue

			var edge_key = _get_edge_key(cell.q, cell.r, neighbor.q, neighbor.r)
			if rendered_edges.has(edge_key):
				continue
			rendered_edges[edge_key] = true

			var corner_pair = DIRECTION_TO_CORNERS[out_dir]
			var c1 = world_corners[corner_pair.x]
			var c2 = world_corners[corner_pair.y]

			var edge_dx = c2.x - c1.x
			var edge_dz = c2.z - c1.z
			var edge_len = sqrt(edge_dx * edge_dx + edge_dz * edge_dz)
			var perp_x = -edge_dz / edge_len * half_width
			var perp_z = edge_dx / edge_len * half_width

			var high_y = max(cell.elevation, neighbor.elevation) * HexMetrics.ELEVATION_STEP + HEIGHT_OFFSET
			var low_y = min(cell.elevation, neighbor.elevation) * HexMetrics.ELEVATION_STEP + HEIGHT_OFFSET

			# Horizontal quad
			vertices.append(Vector3(c1.x - perp_x, high_y, c1.z - perp_z))
			vertices.append(Vector3(c1.x + perp_x, high_y, c1.z + perp_z))
			vertices.append(Vector3(c2.x + perp_x, high_y, c2.z + perp_z))
			vertices.append(Vector3(c2.x - perp_x, high_y, c2.z - perp_z))
			uvs.append(Vector2(0, 0)); uvs.append(Vector2(1, 0))
			uvs.append(Vector2(1, 1)); uvs.append(Vector2(0, 1))
			indices.append(vertex_index); indices.append(vertex_index + 1); indices.append(vertex_index + 2)
			indices.append(vertex_index); indices.append(vertex_index + 2); indices.append(vertex_index + 3)
			vertex_index += 4

			# Waterfall if downhill
			if cell.elevation > neighbor.elevation:
				var neighbor_has_river = not neighbor.river_directions.is_empty() or neighbor.elevation < HexMetrics.WATER_LEVEL
				if neighbor_has_river:
					var edge_norm_x = edge_dx / edge_len * half_width
					var edge_norm_z = edge_dz / edge_len * half_width
					vertices.append(Vector3(c2.x - edge_norm_x, high_y, c2.z - edge_norm_z))
					vertices.append(Vector3(c2.x + edge_norm_x, high_y, c2.z + edge_norm_z))
					vertices.append(Vector3(c2.x + edge_norm_x, low_y, c2.z + edge_norm_z))
					vertices.append(Vector3(c2.x - edge_norm_x, low_y, c2.z - edge_norm_z))
					uvs.append(Vector2(0, 0)); uvs.append(Vector2(1, 0))
					uvs.append(Vector2(1, 1)); uvs.append(Vector2(0, 1))
					indices.append(vertex_index); indices.append(vertex_index + 1); indices.append(vertex_index + 2)
					indices.append(vertex_index); indices.append(vertex_index + 2); indices.append(vertex_index + 3)
					vertex_index += 4

		# Connecting paths for through-flow
		if not incoming.is_empty() and not outgoing.is_empty():
			for in_dir in incoming:
				for out_dir in outgoing:
					var in_corners = DIRECTION_TO_CORNERS[in_dir]
					var out_corners = DIRECTION_TO_CORNERS[out_dir]
					var path = _find_corner_path(in_corners.x, out_corners.x)

					if path.size() >= 2:
						var y = cell.elevation * HexMetrics.ELEVATION_STEP + HEIGHT_OFFSET
						for i in range(path.size() - 1):
							var p1 = world_corners[path[i]]
							var p2 = world_corners[path[i + 1]]
							var seg_dx = p2.x - p1.x
							var seg_dz = p2.z - p1.z
							var seg_len = sqrt(seg_dx * seg_dx + seg_dz * seg_dz)
							if seg_len < 0.001:
								continue
							var s_perp_x = -seg_dz / seg_len * half_width
							var s_perp_z = seg_dx / seg_len * half_width

							vertices.append(Vector3(p1.x - s_perp_x, y, p1.z - s_perp_z))
							vertices.append(Vector3(p1.x + s_perp_x, y, p1.z + s_perp_z))
							vertices.append(Vector3(p2.x + s_perp_x, y, p2.z + s_perp_z))
							vertices.append(Vector3(p2.x - s_perp_x, y, p2.z - s_perp_z))
							uvs.append(Vector2(0, 0)); uvs.append(Vector2(1, 0))
							uvs.append(Vector2(1, 1)); uvs.append(Vector2(0, 1))
							indices.append(vertex_index); indices.append(vertex_index + 1); indices.append(vertex_index + 2)
							indices.append(vertex_index); indices.append(vertex_index + 2); indices.append(vertex_index + 3)
							vertex_index += 4

	if vertices.is_empty():
		return null

	var arrays = []
	arrays.resize(Mesh.ARRAY_MAX)
	arrays[Mesh.ARRAY_VERTEX] = vertices
	arrays[Mesh.ARRAY_TEX_UV] = uvs
	arrays[Mesh.ARRAY_INDEX] = indices

	var mesh = ArrayMesh.new()
	mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, arrays)
	return mesh


func _find_corner_path(start: int, end: int) -> Array[int]:
	if start == end:
		return [start]
	var cw: Array[int] = [start]
	var curr = start
	while curr != end and cw.size() <= 6:
		curr = (curr + 1) % 6
		cw.append(curr)
	var ccw: Array[int] = [start]
	curr = start
	while curr != end and ccw.size() <= 6:
		curr = (curr + 5) % 6
		ccw.append(curr)
	return cw if cw.size() <= ccw.size() else ccw


func _get_edge_key(q1: int, r1: int, q2: int, r2: int) -> String:
	if q1 < q2 or (q1 == q2 and r1 < r2):
		return "%d,%d-%d,%d" % [q1, r1, q2, r2]
	return "%d,%d-%d,%d" % [q2, r2, q1, r1]


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
		var chunk: RiverChunk = chunks[key]
		if not chunk.mesh_instance:
			continue
		var dx = chunk.center.x - view_center.x
		var dz = chunk.center.z - view_center.z
		chunk.mesh_instance.visible = (dx * dx + dz * dz) <= max_dist_sq


func update_animation(delta: float) -> void:
	time += delta
	if material:
		material.set_shader_parameter("time", time)


func _create_river_material() -> ShaderMaterial:
	var mat = ShaderMaterial.new()
	mat.shader = _create_river_shader()
	mat.set_shader_parameter("river_color", Color(0.176, 0.545, 0.788))
	mat.set_shader_parameter("river_color_deep", Color(0.102, 0.361, 0.557))
	mat.set_shader_parameter("flow_speed", 1.5)
	mat.set_shader_parameter("time", 0.0)
	return mat


func _create_river_shader() -> Shader:
	var shader = Shader.new()
	shader.code = """
shader_type spatial;
render_mode blend_mix, cull_disabled, depth_draw_opaque;

uniform vec3 river_color : source_color = vec3(0.176, 0.545, 0.788);
uniform vec3 river_color_deep : source_color = vec3(0.102, 0.361, 0.557);
uniform float flow_speed = 1.5;
uniform float time = 0.0;

varying vec2 world_uv;

float noise(vec2 p) {
	return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

void vertex() {
	world_uv = UV;
}

void fragment() {
	vec2 flow_uv = world_uv;
	flow_uv.y -= time * flow_speed;
	float ripple = noise(flow_uv * 10.0 + time) + noise(flow_uv * 5.0 - time * 0.5) * 0.5;
	ripple = ripple * 0.15;
	float color_mix = sin(VERTEX.x * 0.5 + VERTEX.z * 0.3) * 0.5 + 0.5;
	vec3 base_color = mix(river_color, river_color_deep, color_mix * 0.3);
	vec3 color = base_color + vec3(ripple * 0.2);
	float edge_fade = smoothstep(0.0, 0.15, world_uv.x) * smoothstep(1.0, 0.85, world_uv.x);
	ALBEDO = color;
	ALPHA = 0.85 * edge_fade;
}
"""
	return shader


func dispose() -> void:
	for key in chunks:
		var chunk: RiverChunk = chunks[key]
		if chunk.mesh_instance:
			chunk.mesh_instance.queue_free()
	chunks.clear()
