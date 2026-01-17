class_name EdgeRiverRenderer
extends Node3D
## Renders rivers as edge-based meshes with animated flow.
## Rivers follow hex boundaries, tracing along edges between hexes.
## Matches web/src/rendering/EdgeRiverRenderer.ts

var grid: HexGrid
var mesh_instance: MeshInstance3D
var material: ShaderMaterial
var time: float = 0.0

# River rendering constants
const RIVER_WIDTH: float = 0.15
const HEIGHT_OFFSET: float = 0.02

# Direction to corner indices mapping (same as web version)
# Each direction maps to the two corners that form that edge
const DIRECTION_TO_CORNERS: Array[Vector2i] = [
	Vector2i(5, 0),  # NE: corners 5->0
	Vector2i(4, 5),  # E:  corners 4->5
	Vector2i(3, 4),  # SE: corners 3->4
	Vector2i(2, 3),  # SW: corners 2->3
	Vector2i(1, 2),  # W:  corners 1->2
	Vector2i(0, 1),  # NW: corners 0->1
]


func setup(p_grid: HexGrid) -> void:
	grid = p_grid


func build() -> void:
	# Remove existing mesh
	if mesh_instance:
		mesh_instance.queue_free()
		mesh_instance = null

	var vertices: PackedVector3Array = PackedVector3Array()
	var uvs: PackedVector2Array = PackedVector2Array()
	var indices: PackedInt32Array = PackedInt32Array()
	var vertex_index: int = 0

	var corners = HexMetrics.get_corners()
	var half_width = RIVER_WIDTH

	# Build a map of incoming river directions for each cell
	var incoming_rivers: Dictionary = {}  # "q,r" -> Array of directions
	for cell in grid.get_all_cells():
		for dir in cell.river_directions:
			var neighbor = grid.get_neighbor(cell, dir)
			if neighbor:
				var key = "%d,%d" % [neighbor.q, neighbor.r]
				if not incoming_rivers.has(key):
					incoming_rivers[key] = []
				incoming_rivers[key].append(HexDirection.opposite(dir))

	# Track rendered edges to avoid duplicates
	var rendered_edges: Dictionary = {}

	# Process each cell that has rivers
	for cell in grid.get_all_cells():
		var outgoing = cell.river_directions
		var incoming_key = "%d,%d" % [cell.q, cell.r]
		var incoming: Array = incoming_rivers.get(incoming_key, [])

		if outgoing.is_empty() and incoming.is_empty():
			continue

		var center_pos = cell.get_world_position()

		# Get world positions of all 6 corners for this cell
		var world_corners: Array[Vector3] = []
		for c in corners:
			world_corners.append(Vector3(
				center_pos.x + c.x,
				0,
				center_pos.z + c.z
			))

		# For each outgoing river, draw the edge quad
		for out_dir in outgoing:
			var neighbor = grid.get_neighbor(cell, out_dir)
			if not neighbor:
				continue

			var edge_key = _get_edge_key(cell.q, cell.r, neighbor.q, neighbor.r)
			if rendered_edges.has(edge_key):
				continue
			rendered_edges[edge_key] = true

			# Get the corners for this edge
			var corner_pair = DIRECTION_TO_CORNERS[out_dir]
			var c1 = world_corners[corner_pair.x]
			var c2 = world_corners[corner_pair.y]

			# Calculate edge midpoint and perpendicular
			var edge_dx = c2.x - c1.x
			var edge_dz = c2.z - c1.z
			var edge_len = sqrt(edge_dx * edge_dx + edge_dz * edge_dz)
			var perp_x = -edge_dz / edge_len * half_width
			var perp_z = edge_dx / edge_len * half_width

			# Y positions for both cells
			var high_y = max(cell.elevation, neighbor.elevation) * HexMetrics.ELEVATION_STEP + HEIGHT_OFFSET
			var low_y = min(cell.elevation, neighbor.elevation) * HexMetrics.ELEVATION_STEP + HEIGHT_OFFSET
			var elevation_diff = cell.elevation - neighbor.elevation

			# Draw horizontal quad along the edge (at higher elevation)
			vertices.append(Vector3(c1.x - perp_x, high_y, c1.z - perp_z))
			vertices.append(Vector3(c1.x + perp_x, high_y, c1.z + perp_z))
			vertices.append(Vector3(c2.x + perp_x, high_y, c2.z + perp_z))
			vertices.append(Vector3(c2.x - perp_x, high_y, c2.z - perp_z))

			uvs.append(Vector2(0, 0))
			uvs.append(Vector2(1, 0))
			uvs.append(Vector2(1, 1))
			uvs.append(Vector2(0, 1))

			indices.append(vertex_index)
			indices.append(vertex_index + 1)
			indices.append(vertex_index + 2)
			indices.append(vertex_index)
			indices.append(vertex_index + 2)
			indices.append(vertex_index + 3)
			vertex_index += 4

			# If river flows downhill, draw waterfall
			if elevation_diff > 0:
				var neighbor_has_river = not neighbor.river_directions.is_empty() or \
					neighbor.elevation < HexMetrics.SEA_LEVEL

				if neighbor_has_river:
					# Waterfall width along edge direction
					var edge_norm_x = edge_dx / edge_len * half_width
					var edge_norm_z = edge_dz / edge_len * half_width

					# Draw waterfall quad
					vertices.append(Vector3(c2.x - edge_norm_x, high_y, c2.z - edge_norm_z))
					vertices.append(Vector3(c2.x + edge_norm_x, high_y, c2.z + edge_norm_z))
					vertices.append(Vector3(c2.x + edge_norm_x, low_y, c2.z + edge_norm_z))
					vertices.append(Vector3(c2.x - edge_norm_x, low_y, c2.z - edge_norm_z))

					uvs.append(Vector2(0, 0))
					uvs.append(Vector2(1, 0))
					uvs.append(Vector2(1, 1))
					uvs.append(Vector2(0, 1))

					indices.append(vertex_index)
					indices.append(vertex_index + 1)
					indices.append(vertex_index + 2)
					indices.append(vertex_index)
					indices.append(vertex_index + 2)
					indices.append(vertex_index + 3)
					vertex_index += 4

		# If this cell has both incoming and outgoing rivers, draw connecting path
		if not incoming.is_empty() and not outgoing.is_empty():
			for in_dir in incoming:
				for out_dir in outgoing:
					# Get corners for incoming and outgoing edges
					var in_corners = DIRECTION_TO_CORNERS[in_dir]
					var out_corners = DIRECTION_TO_CORNERS[out_dir]

					# Find path from incoming edge START to outgoing edge START
					var path_corners = _find_corner_path(in_corners.x, out_corners.x)

					if path_corners.size() >= 2:
						var y = cell.elevation * HexMetrics.ELEVATION_STEP + HEIGHT_OFFSET

						# Draw quads along the corner path
						for i in range(path_corners.size() - 1):
							var c_idx1 = path_corners[i]
							var c_idx2 = path_corners[i + 1]
							var p1 = world_corners[c_idx1]
							var p2 = world_corners[c_idx2]

							# Calculate perpendicular for this segment
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

							uvs.append(Vector2(0, 0))
							uvs.append(Vector2(1, 0))
							uvs.append(Vector2(1, 1))
							uvs.append(Vector2(0, 1))

							indices.append(vertex_index)
							indices.append(vertex_index + 1)
							indices.append(vertex_index + 2)
							indices.append(vertex_index)
							indices.append(vertex_index + 2)
							indices.append(vertex_index + 3)
							vertex_index += 4

	if vertices.is_empty():
		print("No river geometry to build")
		return

	# Create mesh
	var arrays = []
	arrays.resize(Mesh.ARRAY_MAX)
	arrays[Mesh.ARRAY_VERTEX] = vertices
	arrays[Mesh.ARRAY_TEX_UV] = uvs
	arrays[Mesh.ARRAY_INDEX] = indices

	var mesh = ArrayMesh.new()
	mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, arrays)

	# Create mesh instance
	mesh_instance = MeshInstance3D.new()
	mesh_instance.mesh = mesh
	mesh_instance.material_override = _create_river_material()
	add_child(mesh_instance)

	print("Built river mesh: %d vertices" % vertices.size())


func _create_river_material() -> ShaderMaterial:
	material = ShaderMaterial.new()
	material.shader = _create_river_shader()
	material.set_shader_parameter("river_color", Color(0.176, 0.545, 0.788))
	material.set_shader_parameter("river_color_deep", Color(0.102, 0.361, 0.557))
	material.set_shader_parameter("flow_speed", 1.5)
	material.set_shader_parameter("time", 0.0)
	return material


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

// Simple noise function for water shimmer
float noise(vec2 p) {
	return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

void vertex() {
	world_uv = UV;
}

void fragment() {
	// Flow animation - scroll UV over time
	vec2 flow_uv = world_uv;
	flow_uv.y -= time * flow_speed;

	// Create ripple effect using noise
	float ripple = noise(flow_uv * 10.0 + time);
	ripple += noise(flow_uv * 5.0 - time * 0.5) * 0.5;
	ripple = ripple * 0.15;

	// Color variation based on position
	float color_mix = sin(VERTEX.x * 0.5 + VERTEX.z * 0.3) * 0.5 + 0.5;
	vec3 base_color = mix(river_color, river_color_deep, color_mix * 0.3);

	// Add ripple brightness variation
	vec3 color = base_color + vec3(ripple * 0.2);

	// Edge fade for smoother blending
	float edge_fade = smoothstep(0.0, 0.15, world_uv.x) * smoothstep(1.0, 0.85, world_uv.x);

	ALBEDO = color;
	ALPHA = 0.85 * edge_fade;
}
"""
	return shader


## Find the shortest path of corner indices from start to end.
func _find_corner_path(start_corner: int, end_corner: int) -> Array[int]:
	if start_corner == end_corner:
		return [start_corner]

	# Try clockwise path
	var cw_path: Array[int] = [start_corner]
	var current = start_corner
	while current != end_corner:
		current = (current + 1) % 6
		cw_path.append(current)
		if cw_path.size() > 6:
			break

	# Try counterclockwise path
	var ccw_path: Array[int] = [start_corner]
	current = start_corner
	while current != end_corner:
		current = (current + 5) % 6
		ccw_path.append(current)
		if ccw_path.size() > 6:
			break

	# Return shorter path
	return cw_path if cw_path.size() <= ccw_path.size() else ccw_path


## Create unique key for an edge between two cells.
func _get_edge_key(q1: int, r1: int, q2: int, r2: int) -> String:
	if q1 < q2 or (q1 == q2 and r1 < r2):
		return "%d,%d-%d,%d" % [q1, r1, q2, r2]
	return "%d,%d-%d,%d" % [q2, r2, q1, r1]


## Update river animation.
func update(delta: float) -> void:
	time += delta
	if material:
		material.set_shader_parameter("time", time)
