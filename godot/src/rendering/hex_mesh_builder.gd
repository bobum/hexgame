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


var _corner_count: int = 0
var _edge_count: int = 0

## Build mesh for entire grid
func build_grid_mesh(grid: HexGrid) -> ArrayMesh:
	reset()
	_corner_count = 0
	_edge_count = 0

	for cell in grid.get_all_cells():
		build_cell(cell, grid)

	print("Built mesh: %d vertices, %d triangles" % [vertices.size(), vertices.size() / 3])
	print("Built %d edges, %d corners" % [_edge_count, _corner_count])
	return _create_mesh()


## Build geometry for a single cell (Catlike Coding pattern)
func build_cell(cell: HexCell, grid: HexGrid) -> void:
	var center = cell.get_world_position()
	var base_color = _vary_color(cell.get_color(), 0.08)

	# Gather all 6 neighbors and their colors
	var neighbors: Array[HexCell] = []
	var neighbor_colors: Array[Color] = []
	neighbors.resize(6)
	neighbor_colors.resize(6)

	for dir in range(6):
		var neighbor = grid.get_neighbor(cell, dir)
		neighbors[dir] = neighbor
		if neighbor:
			neighbor_colors[dir] = _vary_color(neighbor.get_color(), 0.08)
		else:
			neighbor_colors[dir] = base_color

	# Check if we're using full hexes (no gaps) or Catlike Coding style (gaps for edges)
	var use_full_hexes = HexMetrics.SOLID_FACTOR >= 0.99

	if use_full_hexes:
		# Simple mode: full hex tops with walls for elevation changes
		_build_full_hex(center, base_color)

		# Build walls for elevation drops
		for dir in range(6):
			var neighbor = neighbors[dir]
			var edge_index = _get_edge_index_for_direction(dir)

			if not neighbor:
				# Map edge - build cliff down
				var wall_height = (cell.elevation + 3) * HexMetrics.ELEVATION_STEP
				_build_cliff(center, edge_index, wall_height, base_color)
			elif cell.elevation > neighbor.elevation:
				# We're higher - build wall down to neighbor
				var wall_height = (cell.elevation - neighbor.elevation) * HexMetrics.ELEVATION_STEP
				_build_cliff(center, edge_index, wall_height, base_color)
	else:
		# Catlike Coding mode: solid center with edge/corner connections
		_build_top_face(center, base_color)

		# Build edges for each direction
		for dir in range(6):
			var neighbor = neighbors[dir]
			var edge_index = _get_edge_index_for_direction(dir)

			if not neighbor:
				# Map edge - build a cliff down
				var wall_height = (cell.elevation + 3) * HexMetrics.ELEVATION_STEP
				_build_cliff(center, edge_index, wall_height, base_color)
			else:
				var elevation_diff = cell.elevation - neighbor.elevation
				var neighbor_center = neighbor.get_world_position()
				var neighbor_color = neighbor_colors[dir]

				if elevation_diff == 1:
					# Single level slope - build terraces
					_build_terraced_slope(center, neighbor_center, edge_index, base_color, neighbor_color)
				elif elevation_diff > 1:
					# Multi-level cliff
					_build_flat_cliff(center, neighbor_center, edge_index, base_color, neighbor_color)
				elif elevation_diff == 0 and dir <= 2:
					# Same level - build flat edge bridge (only dirs 0-2 to avoid duplication)
					_build_flat_edge(center, neighbor_center, edge_index, base_color, neighbor_color)

			# Build corners (where three hexes meet)
			if dir <= 1:
				var prev_dir = (dir + 5) % 6
				var prev_neighbor = neighbors[prev_dir]
				if neighbor and prev_neighbor:
					_build_corner(cell, center, base_color, dir, neighbor, prev_neighbor)


## Vary a color slightly for visual interest
func _vary_color(color: Color, amount: float) -> Color:
	var variation = randf_range(-amount, amount)
	return Color(
		clampf(color.r + variation, 0.0, 1.0),
		clampf(color.g + variation, 0.0, 1.0),
		clampf(color.b + variation, 0.0, 1.0)
	)


func _get_edge_index_for_direction(dir: int) -> int:
	var dir_to_edge = [5, 4, 3, 2, 1, 0]
	return dir_to_edge[dir]


## Get the corner index of neighbor that touches the shared corner
## For dir=0: neighbor1 (NE) corner=2, neighbor2 (NW, prev_dir=5) corner=4
## For dir=1: neighbor1 (E) corner=1, neighbor2 (NE, prev_dir=0) corner=3
func _get_neighbor_corner_index(dir: int, is_prev_neighbor: bool = false) -> int:
	if dir == 0:
		return 4 if is_prev_neighbor else 2
	elif dir == 1:
		return 3 if is_prev_neighbor else 1
	elif dir == 5:  # prev_dir for dir=0
		return 4
	else:  # dir == 0 as prev_dir for dir=1
		return 3


## Build full hexagon at full radius (no solid/blend regions)
func _build_full_hex(center: Vector3, color: Color) -> void:
	for i in range(6):
		var c1 = corners[i]
		var c2 = corners[(i + 1) % 6]

		var v1 = center
		var v2 = Vector3(center.x + c1.x, center.y, center.z + c1.z)
		var v3 = Vector3(center.x + c2.x, center.y, center.z + c2.z)

		# CW winding for upward-facing in Godot
		_add_triangle(v1, v2, v3, color)


func _build_top_face(center: Vector3, color: Color) -> void:
	var solid = HexMetrics.SOLID_FACTOR

	for i in range(6):
		var c1 = corners[i]
		var c2 = corners[(i + 1) % 6]

		var v1 = center
		var v2 = Vector3(center.x + c1.x * solid, center.y, center.z + c1.z * solid)
		var v3 = Vector3(center.x + c2.x * solid, center.y, center.z + c2.z * solid)

		_add_triangle_with_colors(v1, color, v2, color, v3, color)


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
	_edge_count += 1
	var solid = HexMetrics.SOLID_FACTOR
	var c1 = corners[edge_index]
	var c2 = corners[(edge_index + 1) % 6]

	# Top edge: outer boundary of THIS cell's solid region (higher elevation)
	var top_left = Vector3(
		center.x + c1.x * solid,
		center.y,
		center.z + c1.z * solid
	)
	var top_right = Vector3(
		center.x + c2.x * solid,
		center.y,
		center.z + c2.z * solid
	)

	# The neighbor's edge that faces us is the opposite edge
	var opposite_edge = (edge_index + 3) % 6
	var opp_c1 = corners[opposite_edge]
	var opp_c2 = corners[(opposite_edge + 1) % 6]

	# Bottom edge: outer boundary of NEIGHBOR's solid region (lower elevation)
	# Note: corners are swapped to align the edges properly
	var bottom_left = Vector3(
		neighbor_center.x + opp_c2.x * solid,
		neighbor_center.y,
		neighbor_center.z + opp_c2.z * solid
	)
	var bottom_right = Vector3(
		neighbor_center.x + opp_c1.x * solid,
		neighbor_center.y,
		neighbor_center.z + opp_c1.z * solid
	)

	# Build terraces from top to bottom
	var v1 = top_left
	var v2 = top_right
	var c_1 = begin_color
	var c_2 = begin_color

	for step in range(1, HexMetrics.get_terrace_steps() + 1):
		# Interpolate to the next terrace level
		var v3 = HexMetrics.terrace_lerp(top_left, bottom_left, step)
		var v4 = HexMetrics.terrace_lerp(top_right, bottom_right, step)
		var c_3 = HexMetrics.terrace_color_lerp(begin_color, end_color, step)
		var c_4 = HexMetrics.terrace_color_lerp(begin_color, end_color, step)

		# Build quad for this terrace step
		var avg_color = Color(
			(c_1.r + c_2.r + c_3.r + c_4.r) / 4.0,
			(c_1.g + c_2.g + c_3.g + c_4.g) / 4.0,
			(c_1.b + c_2.b + c_3.b + c_4.b) / 4.0
		)

		# Two triangles for the quad
		_add_triangle(v1, v4, v3, avg_color)
		_add_triangle(v1, v2, v4, avg_color)

		# Move to next step
		v1 = v3
		v2 = v4
		c_1 = c_3
		c_2 = c_4


func _build_flat_cliff(center: Vector3, neighbor_center: Vector3, edge_index: int,
		color: Color, neighbor_color: Color) -> void:
	_edge_count += 1
	var solid = HexMetrics.SOLID_FACTOR
	var c1 = corners[edge_index]
	var c2 = corners[(edge_index + 1) % 6]

	# This cell's solid edge corners (higher elevation)
	var v1 = Vector3(center.x + c1.x * solid, center.y, center.z + c1.z * solid)
	var v2 = Vector3(center.x + c2.x * solid, center.y, center.z + c2.z * solid)

	# Neighbor's solid edge corners (lower elevation)
	var opposite_edge = (edge_index + 3) % 6
	var oc1 = corners[opposite_edge]
	var oc2 = corners[(opposite_edge + 1) % 6]

	# Note: oc2 aligns with v1, oc1 aligns with v2 (corners are swapped)
	var v3 = Vector3(neighbor_center.x + oc2.x * solid, neighbor_center.y, neighbor_center.z + oc2.z * solid)
	var v4 = Vector3(neighbor_center.x + oc1.x * solid, neighbor_center.y, neighbor_center.z + oc1.z * solid)

	var blend_color = color.lerp(neighbor_color, 0.5)
	_add_triangle(v1, v2, v4, blend_color)
	_add_triangle(v1, v4, v3, blend_color)


func _build_flat_edge(center: Vector3, neighbor_center: Vector3, edge_index: int,
		color: Color, neighbor_color: Color) -> void:
	_edge_count += 1
	var solid = HexMetrics.SOLID_FACTOR
	var c1 = corners[edge_index]
	var c2 = corners[(edge_index + 1) % 6]

	# This cell's solid edge corners
	var v1 = Vector3(center.x + c1.x * solid, center.y, center.z + c1.z * solid)
	var v2 = Vector3(center.x + c2.x * solid, center.y, center.z + c2.z * solid)

	# Neighbor's solid edge corners (opposite edge)
	var opposite_edge = (edge_index + 3) % 6
	var oc1 = corners[opposite_edge]
	var oc2 = corners[(opposite_edge + 1) % 6]

	# Note: oc2 aligns with v1, oc1 aligns with v2 (corners are swapped)
	var v3 = Vector3(neighbor_center.x + oc2.x * solid, neighbor_center.y, neighbor_center.z + oc2.z * solid)
	var v4 = Vector3(neighbor_center.x + oc1.x * solid, neighbor_center.y, neighbor_center.z + oc1.z * solid)

	var blend_color = color.lerp(neighbor_color, 0.5)

	# Build quad - CCW winding for upward-facing
	_add_triangle(v1, v2, v4, blend_color)
	_add_triangle(v1, v4, v3, blend_color)


## Build corner geometry where three hexes meet
func _build_corner(cell: HexCell, center: Vector3, color: Color, dir: int,
		neighbor1: HexCell, neighbor2: HexCell) -> void:
	_corner_count += 1
	var solid = HexMetrics.SOLID_FACTOR
	var edge_index = _get_edge_index_for_direction(dir)

	# The shared corner position P (at full radius) - where all three cells meet
	var corner_idx = (edge_index + 1) % 6
	var corner_offset = corners[corner_idx]
	var P = Vector3(center.x + corner_offset.x, 0, center.z + corner_offset.z)

	# Get neighbor centers
	var n1_center = neighbor1.get_world_position()
	var n2_center = neighbor2.get_world_position()

	# Calculate solid corner vertices for each cell
	# Each vertex is solid% of the way from cell center toward the shared corner P
	var v1 = Vector3(
		center.x + corner_offset.x * solid,
		center.y,
		center.z + corner_offset.z * solid
	)

	# For neighbors, calculate direction from their center to P, then scale by solid
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


## Add triangle with per-vertex colors (auto-corrects winding for upward normal)
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

	# Ensure upward-facing normal by reversing winding if needed
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
	# Use SurfaceTool to build the mesh properly
	var st = SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)

	# Add each triangle
	var num_triangles = vertices.size() / 3
	for i in range(num_triangles):
		var idx = i * 3
		st.set_color(colors[idx])
		st.add_vertex(vertices[idx])
		st.set_color(colors[idx + 1])
		st.add_vertex(vertices[idx + 1])
		st.set_color(colors[idx + 2])
		st.add_vertex(vertices[idx + 2])

	st.generate_normals()
	return st.commit()
