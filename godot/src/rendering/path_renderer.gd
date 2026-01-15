class_name PathRenderer
extends Node3D
## Renders pathfinding visualization:
## - Reachable cells (highlighted hexes)
## - Path preview (line from unit to destination)
## Matches web/src/rendering/PathRenderer.ts

var grid: HexGrid

# Reachable cells visualization
var reachable_meshes: MultiMeshInstance3D
var max_reachable_instances: int = 500

# Path line visualization
var path_line: MeshInstance3D
var path_material: StandardMaterial3D

# Destination marker
var destination_marker: MeshInstance3D


func _init() -> void:
	_create_reachable_mesh()
	_create_path_material()
	_create_destination_marker()


func setup(p_grid: HexGrid) -> void:
	grid = p_grid


func _create_reachable_mesh() -> void:
	# Create a flat hexagon for highlighting
	var mesh = _build_hex_shape_mesh()

	var multimesh = MultiMesh.new()
	multimesh.transform_format = MultiMesh.TRANSFORM_3D
	multimesh.use_colors = true
	multimesh.mesh = mesh
	multimesh.instance_count = max_reachable_instances
	multimesh.visible_instance_count = 0

	reachable_meshes = MultiMeshInstance3D.new()
	reachable_meshes.multimesh = multimesh

	var material = StandardMaterial3D.new()
	material.albedo_color = Color(0.0, 1.0, 0.0, 0.5)
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.cull_mode = BaseMaterial3D.CULL_DISABLED
	material.no_depth_test = true  # Render on top of terrain
	material.vertex_color_use_as_albedo = true
	reachable_meshes.material_override = material

	add_child(reachable_meshes)


func _build_hex_shape_mesh() -> ArrayMesh:
	var st = SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)

	var radius = HexMetrics.OUTER_RADIUS * 0.9
	var corners: Array[Vector3] = []

	for i in range(6):
		var angle = (PI / 3.0) * i - PI / 6.0
		corners.append(Vector3(cos(angle) * radius, 0, sin(angle) * radius))

	# Build triangles from center
	var center = Vector3.ZERO
	st.set_normal(Vector3.UP)
	for i in range(6):
		st.add_vertex(center)
		st.add_vertex(corners[i])
		st.add_vertex(corners[(i + 1) % 6])

	return st.commit()


func _create_path_material() -> void:
	path_material = StandardMaterial3D.new()
	path_material.albedo_color = Color(0.0, 1.0, 0.0, 0.8)
	path_material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	path_material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED


func _create_destination_marker() -> void:
	# Create a ring mesh for destination
	var st = SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)

	var inner_radius = 0.6
	var outer_radius = 0.8

	for i in range(6):
		var angle1 = (PI / 3.0) * i - PI / 6.0
		var angle2 = (PI / 3.0) * ((i + 1) % 6) - PI / 6.0

		var inner1 = Vector3(cos(angle1) * inner_radius, 0, sin(angle1) * inner_radius)
		var outer1 = Vector3(cos(angle1) * outer_radius, 0, sin(angle1) * outer_radius)
		var inner2 = Vector3(cos(angle2) * inner_radius, 0, sin(angle2) * inner_radius)
		var outer2 = Vector3(cos(angle2) * outer_radius, 0, sin(angle2) * outer_radius)

		st.set_normal(Vector3.UP)
		st.add_vertex(inner1)
		st.add_vertex(outer1)
		st.add_vertex(outer2)

		st.add_vertex(inner1)
		st.add_vertex(outer2)
		st.add_vertex(inner2)

	destination_marker = MeshInstance3D.new()
	destination_marker.mesh = st.commit()

	var material = StandardMaterial3D.new()
	material.albedo_color = Color(1.0, 0.0, 0.0, 0.8)
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.cull_mode = BaseMaterial3D.CULL_DISABLED
	destination_marker.material_override = material
	destination_marker.visible = false

	add_child(destination_marker)


## Show reachable cells for a unit
func show_reachable_cells(reachable_cells: Dictionary) -> void:
	if reachable_meshes == null:
		return

	var mm = reachable_meshes.multimesh
	var index = 0

	for cell in reachable_cells.keys():
		if index >= max_reachable_instances:
			break

		var cost: float = reachable_cells[cell]
		var world_pos = HexCoordinates.new(cell.q, cell.r).to_world_position(0)

		# For water cells, render on water surface; for land, render on terrain
		var y_offset: float
		if cell.elevation < 0:
			y_offset = 0.1  # Just above water surface
		else:
			y_offset = cell.elevation * HexMetrics.ELEVATION_STEP + 0.15

		var transform = Transform3D()
		transform.origin = Vector3(world_pos.x, y_offset, world_pos.z)
		mm.set_instance_transform(index, transform)

		# Color based on movement cost (green = cheap, yellow = expensive)
		var t = min(cost / 4.0, 1.0)
		var color = Color.from_hsv(0.33 - t * 0.33, 0.8, 0.7, 0.5)
		mm.set_instance_color(index, color)

		index += 1

	mm.visible_instance_count = index


## Hide reachable cells
func hide_reachable_cells() -> void:
	if reachable_meshes and reachable_meshes.multimesh:
		reachable_meshes.multimesh.visible_instance_count = 0


## Show path preview from unit to destination
func show_path(path: Array) -> void:
	# Remove old path line
	if path_line:
		path_line.queue_free()
		path_line = null

	if path.size() < 2:
		hide_destination_marker()
		return

	# Create path points
	var points: PackedVector3Array = []
	for cell in path:
		var world_pos = HexCoordinates.new(cell.q, cell.r).to_world_position(0)
		var y_pos: float
		if cell.elevation < 0:
			y_pos = 0.15  # Above water surface
		else:
			y_pos = cell.elevation * HexMetrics.ELEVATION_STEP + 0.2
		points.append(Vector3(world_pos.x, y_pos, world_pos.z))

	# Create line mesh using ImmediateMesh
	var im = ImmediateMesh.new()
	im.surface_begin(Mesh.PRIMITIVE_LINE_STRIP)
	for point in points:
		im.surface_add_vertex(point)
	im.surface_end()

	path_line = MeshInstance3D.new()
	path_line.mesh = im
	path_line.material_override = path_material
	add_child(path_line)

	# Show destination marker at end of path
	var last_cell = path[path.size() - 1]
	show_destination_marker(last_cell)


## Hide path preview
func hide_path() -> void:
	if path_line:
		path_line.queue_free()
		path_line = null
	hide_destination_marker()


## Show destination marker at a cell
func show_destination_marker(cell: HexCell) -> void:
	if destination_marker == null:
		return

	var world_pos = HexCoordinates.new(cell.q, cell.r).to_world_position(0)
	var y_pos: float
	if cell.elevation < 0:
		y_pos = 0.12  # Above water surface
	else:
		y_pos = cell.elevation * HexMetrics.ELEVATION_STEP + 0.15
	destination_marker.position = Vector3(world_pos.x, y_pos, world_pos.z)
	destination_marker.visible = true


## Hide destination marker
func hide_destination_marker() -> void:
	if destination_marker:
		destination_marker.visible = false


## Update path line color (for valid/invalid paths)
func set_path_valid(valid: bool) -> void:
	if path_material:
		path_material.albedo_color = Color(0.0, 1.0, 0.0, 0.8) if valid else Color(1.0, 0.0, 0.0, 0.8)
