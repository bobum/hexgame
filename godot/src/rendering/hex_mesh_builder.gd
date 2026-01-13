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
				_build_corner(cell, center, base_color, dir, neighbor, prev_neighbor)


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


## Build corner geometry where three hexes meet
func _build_corner(cell: HexCell, center: Vector3, color: Color, dir: int,
		neighbor1: HexCell, neighbor2: HexCell) -> void:
	var solid = HexMetrics.SOLID_FACTOR
	var edge_index = _get_edge_index_for_direction(dir)

	# The shared corner position P (at full radius)
	var corner_idx = (edge_index + 1) % 6
	var corner_offset = corners[corner_idx]
	var P = Vector3(center.x + corner_offset.x, 0, center.z + corner_offset.z)

	# Get neighbor centers
	var n1_center = neighbor1.get_world_position()
	var n2_center = neighbor2.get_world_position()

	# Calculate solid corner vertices for each cell
	var v1 = Vector3(
		center.x + corner_offset.x * solid,
		center.y,
		center.z + corner_offset.z * solid
	)
	var v2 = Vector3(
		n1_center.x + (P.x - n1_center.x) * solid,
		n1_center.y,
		n1_center.z + (P.z - n1_center.z) * solid
	)
	var v3 = Vector3(
		n2_center.x + (P.x - n2_center.x) * solid,
		n2_center.y,
		n2_center.z + (P.z - n2_center.z) * solid
	)

	# Get colors
	var c1 = color
	var c2 = neighbor1.get_color()
	var c3 = neighbor2.get_color()

	# Get elevations
	var e1 = cell.elevation
	var e2 = neighbor1.elevation
	var e3 = neighbor2.elevation

	# Sort by elevation to find bottom, left, right (Catlike Coding approach)
	if e1 <= e2:
		if e1 <= e3:
			# e1 is lowest - current cell is bottom
			_triangulate_corner(v1, c1, e1, v2, c2, e2, v3, c3, e3)
		else:
			# e3 is lowest - neighbor2 is bottom, rotate CCW
			_triangulate_corner(v3, c3, e3, v1, c1, e1, v2, c2, e2)
	else:
		if e2 <= e3:
			# e2 is lowest - neighbor1 is bottom, rotate CW
			_triangulate_corner(v2, c2, e2, v3, c3, e3, v1, c1, e1)
		else:
			# e3 is lowest - neighbor2 is bottom, rotate CCW
			_triangulate_corner(v3, c3, e3, v1, c1, e1, v2, c2, e2)


## Triangulate corner with bottom vertex first, then left and right
func _triangulate_corner(
		bottom: Vector3, bottom_color: Color, bottom_elev: int,
		left: Vector3, left_color: Color, left_elev: int,
		right: Vector3, right_color: Color, right_elev: int) -> void:

	var left_edge_type = _get_edge_type(bottom_elev, left_elev)
	var right_edge_type = _get_edge_type(bottom_elev, right_elev)

	if left_edge_type == "slope":
		if right_edge_type == "slope":
			# Check if left and right are at same elevation (SSF case)
			var top_edge_type = _get_edge_type(left_elev, right_elev)
			if top_edge_type == "flat":
				_triangulate_corner_ssf(bottom, bottom_color, left, left_color, right, right_color)
			else:
				_triangulate_corner_terraces(bottom, bottom_color, left, left_color, right, right_color)
		elif right_edge_type == "cliff":
			_triangulate_corner_terraces_cliff(
				bottom, bottom_color, bottom_elev,
				left, left_color, left_elev,
				right, right_color, right_elev
			)
		else:
			# Slope-Flat: terraces fan from left
			_triangulate_corner_terraces(left, left_color, right, right_color, bottom, bottom_color)

	elif left_edge_type == "cliff":
		if right_edge_type == "slope":
			_triangulate_corner_cliff_terraces(
				bottom, bottom_color, bottom_elev,
				left, left_color, left_elev,
				right, right_color, right_elev
			)
		elif right_edge_type == "cliff":
			var top_edge_type = _get_edge_type(left_elev, right_elev)
			if top_edge_type == "slope":
				if left_elev < right_elev:
					_triangulate_corner_ccsr(bottom, bottom_color, left, left_color, right, right_color)
				else:
					_triangulate_corner_ccsl(bottom, bottom_color, left, left_color, right, right_color)
			else:
				_add_triangle_with_colors(bottom, bottom_color, left, left_color, right, right_color)
		else:
			_add_triangle_with_colors(bottom, bottom_color, left, left_color, right, right_color)

	else:
		# Left is flat
		if right_edge_type == "slope":
			_triangulate_corner_terraces(right, right_color, bottom, bottom_color, left, left_color)
		else:
			_add_triangle_with_colors(bottom, bottom_color, left, left_color, right, right_color)


func _get_edge_type(e1: int, e2: int) -> String:
	var diff = abs(e1 - e2)
	if diff == 0:
		return "flat"
	elif diff == 1:
		return "slope"
	else:
		return "cliff"


## Slope-Slope corner: terraced fan from bottom
func _triangulate_corner_terraces(
		bottom: Vector3, bottom_color: Color,
		left: Vector3, left_color: Color,
		right: Vector3, right_color: Color) -> void:

	var v3 = HexMetrics.terrace_lerp(bottom, left, 1)
	var v4 = HexMetrics.terrace_lerp(bottom, right, 1)
	var c3 = HexMetrics.terrace_color_lerp(bottom_color, left_color, 1)
	var c4 = HexMetrics.terrace_color_lerp(bottom_color, right_color, 1)

	_add_triangle_with_colors(bottom, bottom_color, v3, c3, v4, c4)

	for i in range(2, HexMetrics.get_terrace_steps()):
		var v1 = v3
		var v2 = v4
		var c1 = c3
		var c2 = c4
		v3 = HexMetrics.terrace_lerp(bottom, left, i)
		v4 = HexMetrics.terrace_lerp(bottom, right, i)
		c3 = HexMetrics.terrace_color_lerp(bottom_color, left_color, i)
		c4 = HexMetrics.terrace_color_lerp(bottom_color, right_color, i)
		_add_quad_with_colors(v1, c1, v2, c2, v3, c3, v4, c4)

	_add_quad_with_colors(v3, c3, v4, c4, left, left_color, right, right_color)


## Slope-Cliff corner: terraces on left, boundary triangles to cliff
func _triangulate_corner_terraces_cliff(
		bottom: Vector3, bottom_color: Color, bottom_elev: int,
		left: Vector3, left_color: Color, left_elev: int,
		right: Vector3, right_color: Color, right_elev: int) -> void:

	var b = 1.0 / (right_elev - bottom_elev)
	var boundary = bottom.lerp(right, b)
	var boundary_color = bottom_color.lerp(right_color, b)

	_triangulate_boundary_triangle(bottom, bottom_color, left, left_color, boundary, boundary_color)

	if _get_edge_type(left_elev, right_elev) == "slope":
		_triangulate_boundary_triangle(left, left_color, right, right_color, boundary, boundary_color)
	else:
		_add_triangle_with_colors(left, left_color, right, right_color, boundary, boundary_color)


## Cliff-Slope corner: boundary triangles on cliff, terraces on right
func _triangulate_corner_cliff_terraces(
		bottom: Vector3, bottom_color: Color, bottom_elev: int,
		left: Vector3, left_color: Color, left_elev: int,
		right: Vector3, right_color: Color, right_elev: int) -> void:

	var b = 1.0 / (left_elev - bottom_elev)
	var boundary = bottom.lerp(left, b)
	var boundary_color = bottom_color.lerp(left_color, b)

	_triangulate_boundary_triangle(bottom, bottom_color, right, right_color, boundary, boundary_color)

	if _get_edge_type(left_elev, right_elev) == "slope":
		_triangulate_boundary_triangle(right, right_color, left, left_color, boundary, boundary_color)
	else:
		_add_triangle_with_colors(right, right_color, left, left_color, boundary, boundary_color)


## SSF: Slope-Slope-Flat - both slopes, flat on top
func _triangulate_corner_ssf(
		bottom: Vector3, bottom_color: Color,
		left: Vector3, left_color: Color,
		right: Vector3, right_color: Color) -> void:

	var v3 = HexMetrics.terrace_lerp(bottom, left, 1)
	var c3 = HexMetrics.terrace_color_lerp(bottom_color, left_color, 1)
	var v4 = HexMetrics.terrace_lerp(bottom, right, 1)
	var c4 = HexMetrics.terrace_color_lerp(bottom_color, right_color, 1)

	_add_triangle_with_colors(bottom, bottom_color, v3, c3, v4, c4)

	for i in range(2, HexMetrics.get_terrace_steps() + 1):
		var v3prev = v3
		var c3prev = c3
		var v4prev = v4
		var c4prev = c4

		v3 = HexMetrics.terrace_lerp(bottom, left, i)
		c3 = HexMetrics.terrace_color_lerp(bottom_color, left_color, i)
		v4 = HexMetrics.terrace_lerp(bottom, right, i)
		c4 = HexMetrics.terrace_color_lerp(bottom_color, right_color, i)

		_add_triangle_with_colors(v3prev, c3prev, v3, c3, v4prev, c4prev)
		_add_triangle_with_colors(v3, c3, v4, c4, v4prev, c4prev)

	_add_triangle_with_colors(v3, c3, left, left_color, v4, c4)
	_add_triangle_with_colors(left, left_color, right, right_color, v4, c4)


## CCSR: Cliff-Cliff with Slope, Right higher
func _triangulate_corner_ccsr(
		bottom: Vector3, bottom_color: Color,
		left: Vector3, left_color: Color,
		right: Vector3, right_color: Color) -> void:

	var right_cliff_height = right.y - bottom.y
	var left_height = left.y - bottom.y
	var b = left_height / right_cliff_height

	var boundary = bottom.lerp(right, b)
	var boundary_color = bottom_color.lerp(right_color, b)

	_add_triangle_with_colors(bottom, bottom_color, left, left_color, boundary, boundary_color)
	_triangulate_boundary_triangle(left, left_color, right, right_color, boundary, boundary_color)


## CCSL: Cliff-Cliff with Slope, Left higher
func _triangulate_corner_ccsl(
		bottom: Vector3, bottom_color: Color,
		left: Vector3, left_color: Color,
		right: Vector3, right_color: Color) -> void:

	var left_cliff_height = left.y - bottom.y
	var right_height = right.y - bottom.y
	var b = right_height / left_cliff_height

	var boundary = bottom.lerp(left, b)
	var boundary_color = bottom_color.lerp(left_color, b)

	_add_triangle_with_colors(bottom, bottom_color, boundary, boundary_color, right, right_color)
	_triangulate_boundary_triangle(right, right_color, left, left_color, boundary, boundary_color)


## Boundary triangle: fan from begin toward left, all pointing to boundary
func _triangulate_boundary_triangle(
		begin: Vector3, begin_color: Color,
		left: Vector3, left_color: Color,
		boundary: Vector3, boundary_color: Color) -> void:

	var v2 = HexMetrics.terrace_lerp(begin, left, 1)
	var c2 = HexMetrics.terrace_color_lerp(begin_color, left_color, 1)

	_add_triangle_with_colors(begin, begin_color, v2, c2, boundary, boundary_color)

	for i in range(2, HexMetrics.get_terrace_steps()):
		var v1 = v2
		var c1 = c2
		v2 = HexMetrics.terrace_lerp(begin, left, i)
		c2 = HexMetrics.terrace_color_lerp(begin_color, left_color, i)
		_add_triangle_with_colors(v1, c1, v2, c2, boundary, boundary_color)

	_add_triangle_with_colors(v2, c2, left, left_color, boundary, boundary_color)


## Add triangle with per-vertex colors (auto-corrects winding)
func _add_triangle_with_colors(
		v1: Vector3, c1: Color,
		v2: Vector3, c2: Color,
		v3: Vector3, c3: Color) -> void:

	var edge1 = v2 - v1
	var edge2 = v3 - v1
	var normal = edge1.cross(edge2)

	var avg_color = Color(
		(c1.r + c2.r + c3.r) / 3.0,
		(c1.g + c2.g + c3.g) / 3.0,
		(c1.b + c2.b + c3.b) / 3.0
	)

	if normal.y < 0:
		_add_triangle(v1, v3, v2, avg_color)
	else:
		_add_triangle(v1, v2, v3, avg_color)


## Add quad with per-vertex colors
func _add_quad_with_colors(
		v1: Vector3, c1: Color,
		v2: Vector3, c2: Color,
		v3: Vector3, c3: Color,
		v4: Vector3, c4: Color) -> void:

	var avg_color = Color(
		(c1.r + c2.r + c3.r + c4.r) / 4.0,
		(c1.g + c2.g + c3.g + c4.g) / 4.0,
		(c1.b + c2.b + c3.b + c4.b) / 4.0
	)

	_add_triangle_with_colors(v1, avg_color, v2, avg_color, v3, avg_color)
	_add_triangle_with_colors(v2, avg_color, v4, avg_color, v3, avg_color)


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
