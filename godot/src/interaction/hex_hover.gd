class_name HexHover
extends Node3D
## Handles hex hover detection and highlighting
## Matches web hover system

const HIGHLIGHT_COLOR: Color = Color(1.0, 0.9, 0.2, 0.8)  # Yellow
const HIGHLIGHT_HEIGHT: float = 0.1  # Slightly above terrain
const RING_WIDTH: float = 0.08  # Width of highlight ring

var highlight_mesh: MeshInstance3D
var current_cell: HexCell = null
var grid: HexGrid
var camera: Camera3D

# Callback for UI updates
signal cell_hovered(cell: HexCell)
signal cell_unhovered()


func _ready() -> void:
	_create_highlight_mesh()


func setup(p_grid: HexGrid, p_camera: Camera3D) -> void:
	assert(p_grid != null, "HexHover requires HexGrid")
	assert(p_camera != null, "HexHover requires Camera3D")

	grid = p_grid
	camera = p_camera


func _create_highlight_mesh() -> void:
	var mesh = _build_hex_ring_mesh()
	highlight_mesh = MeshInstance3D.new()
	highlight_mesh.mesh = mesh

	var material = StandardMaterial3D.new()
	material.albedo_color = HIGHLIGHT_COLOR
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.cull_mode = BaseMaterial3D.CULL_DISABLED
	highlight_mesh.material_override = material

	highlight_mesh.visible = false
	add_child(highlight_mesh)


func _build_hex_ring_mesh() -> ArrayMesh:
	var st = SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)

	var corners = HexMetrics.get_corners()
	var inner_scale = 1.0 - RING_WIDTH

	for i in range(6):
		var c1 = corners[i]
		var c2 = corners[(i + 1) % 6]

		# Outer corners
		var outer1 = Vector3(c1.x, 0, c1.z)
		var outer2 = Vector3(c2.x, 0, c2.z)

		# Inner corners (scaled down)
		var inner1 = Vector3(c1.x * inner_scale, 0, c1.z * inner_scale)
		var inner2 = Vector3(c2.x * inner_scale, 0, c2.z * inner_scale)

		# Build quad for this edge of the ring
		st.set_normal(Vector3.UP)
		st.add_vertex(outer1)
		st.add_vertex(inner1)
		st.add_vertex(outer2)

		st.add_vertex(outer2)
		st.add_vertex(inner1)
		st.add_vertex(inner2)

	return st.commit()


func _input(event: InputEvent) -> void:
	if event is InputEventMouseMotion:
		_update_hover()


func _update_hover() -> void:
	if not camera or not grid:
		return

	var mouse_pos = get_viewport().get_mouse_position()
	var ray_origin = camera.project_ray_origin(mouse_pos)
	var ray_dir = camera.project_ray_normal(mouse_pos)

	# Raycast to find intersection with ground plane (y = 0) or terrain
	var cell = _raycast_to_hex(ray_origin, ray_dir)

	if cell != current_cell:
		current_cell = cell
		if cell:
			_show_highlight(cell)
			cell_hovered.emit(cell)
		else:
			_hide_highlight()
			cell_unhovered.emit()


func _raycast_to_hex(origin: Vector3, direction: Vector3) -> HexCell:
	# Cast to sea level plane (where water and most terrain interaction happens)
	if abs(direction.y) < 0.001:
		return null

	# Sea level Y position
	var sea_level_y = HexMetrics.SEA_LEVEL * HexMetrics.ELEVATION_STEP

	# Cast to sea level plane to get XZ position
	var t = (sea_level_y - origin.y) / direction.y

	if t <= 0:
		return null

	var hit_point = origin + direction * t
	return _get_cell_at_position(hit_point)


func _get_cell_at_position(world_pos: Vector3) -> HexCell:
	var coords = HexCoordinates.from_world_position(world_pos)
	return grid.get_cell(coords.q, coords.r)


func _show_highlight(cell: HexCell) -> void:
	var world_pos = cell.get_world_position()
	highlight_mesh.position = Vector3(world_pos.x, world_pos.y + HIGHLIGHT_HEIGHT, world_pos.z)
	highlight_mesh.visible = true


func _hide_highlight() -> void:
	highlight_mesh.visible = false
	current_cell = null


func get_hovered_cell() -> HexCell:
	return current_cell
