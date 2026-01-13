class_name HexMeshBuilder
extends RefCounted
## Builds hex mesh geometry with terraced slopes
## Matches web/src/rendering/HexMeshBuilder.ts

var vertices: PackedVector3Array
var colors: PackedColorArray
var indices: PackedInt32Array
var vertex_index: int = 0

# Pre-calculated corner offsets
var corners: Array[Vector3]


func _init() -> void:
	corners = HexMetrics.get_corners()
	reset()


func reset() -> void:
	vertices = PackedVector3Array()
	colors = PackedColorArray()
	indices = PackedInt32Array()
	vertex_index = 0


## Build mesh for entire grid
func build_grid_mesh(grid: HexGrid) -> ArrayMesh:
	reset()

	for cell in grid.get_all_cells():
		build_cell(cell, grid)

	return _create_mesh()


## Build geometry for a single cell
func build_cell(cell: HexCell, grid: HexGrid) -> void:
	var center = cell.get_world_position()
	var base_color = cell.get_color()

	# Build solid center hexagon
	_build_top_face(center, base_color)

	# Build edge connections
	for dir in range(6):
		var neighbor = grid.get_neighbor(cell, dir)
		var edge_index = _get_edge_index_for_direction(dir)

		if neighbor == null:
			# Edge of map - cliff
			var wall_height = (cell.elevation - HexMetrics.MIN_ELEVATION) * HexMetrics.ELEVATION_STEP
			if wall_height > 0:
				_build_cliff(center, edge_index, wall_height, base_color)
		else:
			var elevation_diff = cell.elevation - neighbor.elevation
			var neighbor_center = neighbor.get_world_position()
			var neighbor_color = neighbor.get_color()

			if elevation_diff == 1:
				# Single level - terraced slope
				_build_terraced_slope(center, neighbor_center, edge_index, base_color, neighbor_color)
			elif elevation_diff > 1:
				# Multi-level cliff
				_build_flat_cliff(center, neighbor_center, edge_index, base_color, neighbor_color)
			elif elevation_diff == 0 and dir <= 2:
				# Same level - flat bridge
				_build_flat_edge(center, neighbor_center, edge_index, base_color, neighbor_color)

		# Build corners (directions 0 and 1 only)
		if dir <= 1:
			var prev_dir = (dir + 5) % 6
			var prev_neighbor = grid.get_neighbor(cell, prev_dir)
			if neighbor and prev_neighbor:
				_build_corner(cell, center, base_color, dir, neighbor, prev_neighbor, grid)


func _get_edge_index_for_direction(dir: int) -> int:
	var dir_to_edge = [5, 4, 3, 2, 1, 0]
	return dir_to_edge[dir]


func _build_top_face(center: Vector3, color: Color) -> void:
	var solid = HexMetrics.SOLID_FACTOR

	for i in range(6):
		var c1 = corners[i]
		var c2 = corners[(i + 1) % 6]

		var v1 = center
		var v2 = Vector3(center.x + c1.x * solid, center.y, center.z + c1.z * solid)
		var v3 = Vector3(center.x + c2.x * solid, center.y, center.z + c2.z * solid)

		_add_triangle(v1, v2, v3, color)


func _build_cliff(center: Vector3, edge_index: int, height: float, color: Color) -> void:
	var wall_color = color.darkened(0.35)
	var c1 = corners[edge_index]
	var c2 = corners[(edge_index + 1) % 6]

	var top_left = Vector3(center.x + c1.x, center.y, center.z + c1.z)
	var top_right = Vector3(center.x + c2.x, center.y, center.z + c2.z)
	var bottom_left = Vector3(top_left.x, center.y - height, top_left.z)
	var bottom_right = Vector3(top_right.x, center.y - height, top_right.z)

	_add_triangle(top_left, bottom_right, bottom_left, wall_color)
	_add_triangle(top_left, top_right, bottom_right, wall_color)


func _build_terraced_slope(center: Vector3, neighbor_center: Vector3, edge_index: int,
		begin_color: Color, end_color: Color) -> void:
	var solid = HexMetrics.SOLID_FACTOR
	var c1 = corners[edge_index]
	var c2 = corners[(edge_index + 1) % 6]

	var top_left = Vector3(center.x + c1.x * solid, center.y, center.z + c1.z * solid)
	var top_right = Vector3(center.x + c2.x * solid, center.y, center.z + c2.z * solid)

	var opposite_edge = (edge_index + 3) % 6
	var oc1 = corners[opposite_edge]
	var oc2 = corners[(opposite_edge + 1) % 6]

	var bottom_left = Vector3(neighbor_center.x + oc2.x * solid, neighbor_center.y, neighbor_center.z + oc2.z * solid)
	var bottom_right = Vector3(neighbor_center.x + oc1.x * solid, neighbor_center.y, neighbor_center.z + oc1.z * solid)

	# Build terraces
	var v1 = top_left
	var v2 = top_right
	var c_current = begin_color

	for step in range(1, HexMetrics.get_terrace_steps() + 1):
		var v3 = HexMetrics.terrace_lerp(top_left, bottom_left, step)
		var v4 = HexMetrics.terrace_lerp(top_right, bottom_right, step)
		var c_next = HexMetrics.terrace_color_lerp(begin_color, end_color, step)

		var avg_color = c_current.lerp(c_next, 0.5)
		_add_triangle(v1, v4, v3, avg_color)
		_add_triangle(v1, v2, v4, avg_color)

		v1 = v3
		v2 = v4
		c_current = c_next


func _build_flat_cliff(center: Vector3, neighbor_center: Vector3, edge_index: int,
		color: Color, neighbor_color: Color) -> void:
	var solid = HexMetrics.SOLID_FACTOR
	var c1 = corners[edge_index]
	var c2 = corners[(edge_index + 1) % 6]

	var v1 = Vector3(center.x + c1.x * solid, center.y, center.z + c1.z * solid)
	var v2 = Vector3(center.x + c2.x * solid, center.y, center.z + c2.z * solid)

	var opposite_edge = (edge_index + 3) % 6
	var oc1 = corners[opposite_edge]
	var oc2 = corners[(opposite_edge + 1) % 6]

	var v3 = Vector3(neighbor_center.x + oc2.x * solid, neighbor_center.y, neighbor_center.z + oc2.z * solid)
	var v4 = Vector3(neighbor_center.x + oc1.x * solid, neighbor_center.y, neighbor_center.z + oc1.z * solid)

	var blend_color = color.lerp(neighbor_color, 0.5)
	_add_triangle(v1, v2, v4, blend_color)
	_add_triangle(v1, v4, v3, blend_color)


func _build_flat_edge(center: Vector3, neighbor_center: Vector3, edge_index: int,
		color: Color, neighbor_color: Color) -> void:
	var solid = HexMetrics.SOLID_FACTOR
	var c1 = corners[edge_index]
	var c2 = corners[(edge_index + 1) % 6]

	var v1 = Vector3(center.x + c1.x * solid, center.y, center.z + c1.z * solid)
	var v2 = Vector3(center.x + c2.x * solid, center.y, center.z + c2.z * solid)

	var opposite_edge = (edge_index + 3) % 6
	var oc1 = corners[opposite_edge]
	var oc2 = corners[(opposite_edge + 1) % 6]

	var v3 = Vector3(neighbor_center.x + oc2.x * solid, neighbor_center.y, neighbor_center.z + oc2.z * solid)
	var v4 = Vector3(neighbor_center.x + oc1.x * solid, neighbor_center.y, neighbor_center.z + oc1.z * solid)

	var blend_color = color.lerp(neighbor_color, 0.5)
	_add_triangle(v1, v2, v4, blend_color)
	_add_triangle(v1, v4, v3, blend_color)


func _build_corner(_cell: HexCell, _center: Vector3, _color: Color, _dir: int,
		_neighbor1: HexCell, _neighbor2: HexCell, _grid: HexGrid) -> void:
	# TODO: Implement full corner triangulation with terraces
	# This is a placeholder - full implementation matches web version
	pass


func _add_triangle(v1: Vector3, v2: Vector3, v3: Vector3, color: Color) -> void:
	# Check winding and correct if needed
	var edge1 = v2 - v1
	var edge2 = v3 - v1
	var normal = edge1.cross(edge2)

	if normal.y < 0:
		# Reverse winding
		vertices.append(v1)
		vertices.append(v3)
		vertices.append(v2)
	else:
		vertices.append(v1)
		vertices.append(v2)
		vertices.append(v3)

	colors.append(color)
	colors.append(color)
	colors.append(color)

	indices.append(vertex_index)
	indices.append(vertex_index + 1)
	indices.append(vertex_index + 2)
	vertex_index += 3


func _create_mesh() -> ArrayMesh:
	var mesh = ArrayMesh.new()
	var arrays = []
	arrays.resize(Mesh.ARRAY_MAX)
	arrays[Mesh.ARRAY_VERTEX] = vertices
	arrays[Mesh.ARRAY_COLOR] = colors
	arrays[Mesh.ARRAY_INDEX] = indices

	mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, arrays)

	# Generate normals
	var st = SurfaceTool.new()
	st.create_from(mesh, 0)
	st.generate_normals()
	return st.commit()
