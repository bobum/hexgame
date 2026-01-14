class_name UnitRenderer
extends Node3D
## Renders units using instanced meshes for performance.
## Matches web/src/units/UnitRenderer.ts

# Player colors for land units
const PLAYER_COLORS_LAND: Array[Color] = [
	Color(0.267, 0.533, 1.0),   # Player 1: Blue
	Color(1.0, 0.267, 0.267),   # Player 2: Red
	Color(0.267, 1.0, 0.267),   # Player 3: Green
	Color(1.0, 0.533, 0.267),   # Player 4: Orange
]

# Player colors for naval units (yellow tones)
const PLAYER_COLORS_NAVAL: Array[Color] = [
	Color(1.0, 1.0, 0.267),     # Player 1: Yellow
	Color(1.0, 0.8, 0.0),       # Player 2: Gold
	Color(0.8, 1.0, 0.267),     # Player 3: Lime Yellow
	Color(1.0, 0.667, 0.0),     # Player 4: Amber
]

# Amphibious units (cyan tones)
const PLAYER_COLORS_AMPHIBIOUS: Array[Color] = [
	Color(0.267, 1.0, 1.0),     # Player 1: Cyan
	Color(0.0, 0.8, 0.8),       # Player 2: Teal
	Color(0.267, 0.8, 1.0),     # Player 3: Sky Blue
	Color(0.0, 1.0, 0.8),       # Player 4: Aqua
]

const SELECTED_COLOR: Color = Color(1.0, 1.0, 1.0)  # White for selected

var unit_manager: UnitManager
var grid: HexGrid

# MultiMesh instances per unit type
var multimeshes: Dictionary = {}  # UnitTypes.Type -> MultiMeshInstance3D

# Instance index -> unit ID mapping per type
var unit_id_maps: Dictionary = {}  # UnitTypes.Type -> Array[int]

# Selected unit IDs
var selected_unit_ids: Dictionary = {}  # Set<int>

var needs_rebuild: bool = true

# Cached meshes
var _infantry_mesh: Mesh
var _cavalry_mesh: Mesh
var _archer_mesh: Mesh
var _galley_mesh: Mesh
var _warship_mesh: Mesh
var _marine_mesh: Mesh


func _init() -> void:
	# Pre-create meshes
	_infantry_mesh = _create_infantry_mesh()
	_cavalry_mesh = _create_cavalry_mesh()
	_archer_mesh = _create_archer_mesh()
	_galley_mesh = _create_galley_mesh()
	_warship_mesh = _create_warship_mesh()
	_marine_mesh = _create_marine_mesh()


func setup(p_unit_manager: UnitManager, p_grid: HexGrid) -> void:
	unit_manager = p_unit_manager
	grid = p_grid

	# Disconnect old signals if reconnecting
	if unit_manager.unit_created.is_connected(_on_unit_created):
		unit_manager.unit_created.disconnect(_on_unit_created)
	if unit_manager.unit_removed.is_connected(_on_unit_removed):
		unit_manager.unit_removed.disconnect(_on_unit_removed)
	if unit_manager.unit_moved.is_connected(_on_unit_moved):
		unit_manager.unit_moved.disconnect(_on_unit_moved)

	# Connect signals
	unit_manager.unit_created.connect(_on_unit_created)
	unit_manager.unit_removed.connect(_on_unit_removed)
	unit_manager.unit_moved.connect(_on_unit_moved)


func _on_unit_created(_unit: Unit) -> void:
	needs_rebuild = true


func _on_unit_removed(_unit_id: int) -> void:
	needs_rebuild = true


func _on_unit_moved(_unit: Unit, _from_q: int, _from_r: int) -> void:
	needs_rebuild = true


func build() -> void:
	_clear_meshes()

	var units = unit_manager.get_all_units()

	# Sort units by type
	var by_type: Dictionary = {}
	for unit_type in [UnitTypes.Type.INFANTRY, UnitTypes.Type.CAVALRY, UnitTypes.Type.ARCHER,
					  UnitTypes.Type.GALLEY, UnitTypes.Type.WARSHIP, UnitTypes.Type.MARINE]:
		by_type[unit_type] = []
		unit_id_maps[unit_type] = []

	for unit in units:
		by_type[unit.type].append(unit)

	# Create multimeshes for each type
	_create_type_multimesh(UnitTypes.Type.INFANTRY, by_type[UnitTypes.Type.INFANTRY], _infantry_mesh, PLAYER_COLORS_LAND)
	_create_type_multimesh(UnitTypes.Type.CAVALRY, by_type[UnitTypes.Type.CAVALRY], _cavalry_mesh, PLAYER_COLORS_LAND)
	_create_type_multimesh(UnitTypes.Type.ARCHER, by_type[UnitTypes.Type.ARCHER], _archer_mesh, PLAYER_COLORS_LAND)
	_create_type_multimesh(UnitTypes.Type.GALLEY, by_type[UnitTypes.Type.GALLEY], _galley_mesh, PLAYER_COLORS_NAVAL)
	_create_type_multimesh(UnitTypes.Type.WARSHIP, by_type[UnitTypes.Type.WARSHIP], _warship_mesh, PLAYER_COLORS_NAVAL)
	_create_type_multimesh(UnitTypes.Type.MARINE, by_type[UnitTypes.Type.MARINE], _marine_mesh, PLAYER_COLORS_AMPHIBIOUS)

	needs_rebuild = false


func _clear_meshes() -> void:
	for mm in multimeshes.values():
		if mm:
			mm.queue_free()
	multimeshes.clear()
	unit_id_maps.clear()


func _create_type_multimesh(unit_type: UnitTypes.Type, units: Array, mesh: Mesh, colors: Array[Color]) -> void:
	if units.is_empty():
		multimeshes[unit_type] = null
		return

	var multimesh = MultiMesh.new()
	multimesh.transform_format = MultiMesh.TRANSFORM_3D
	multimesh.use_colors = true
	multimesh.mesh = mesh
	multimesh.instance_count = units.size()

	var unit_ids: Array[int] = []

	for i in range(units.size()):
		var unit: Unit = units[i]
		unit_ids.append(unit.id)

		# Get position
		var cell = grid.get_cell(unit.q, unit.r)
		var world_pos = unit.get_world_position()
		var elevation = cell.elevation if cell else 0

		# Naval units float on water surface, land units on terrain
		# Amphibious units use water surface when in water, terrain when on land
		var is_on_water = cell != null and cell.elevation < HexMetrics.WATER_LEVEL
		if UnitTypes.is_naval(unit.type) or (UnitTypes.is_amphibious(unit.type) and is_on_water):
			world_pos.y = 0.1  # Water surface + slight offset
		else:
			world_pos.y = elevation * HexMetrics.ELEVATION_STEP + 0.25

		# Create transform
		var transform = Transform3D()
		transform.origin = world_pos
		multimesh.set_instance_transform(i, transform)

		# Set color based on player
		var color = colors[unit.player_id % colors.size()]
		multimesh.set_instance_color(i, color)

	unit_id_maps[unit_type] = unit_ids

	var instance = MultiMeshInstance3D.new()
	instance.multimesh = multimesh

	# Create material
	var material = StandardMaterial3D.new()
	material.vertex_color_use_as_albedo = true
	material.shading_mode = BaseMaterial3D.SHADING_MODE_PER_VERTEX
	instance.material_override = material

	add_child(instance)
	multimeshes[unit_type] = instance


## Infantry: Simple cylinder shape
func _create_infantry_mesh() -> Mesh:
	var mesh = CylinderMesh.new()
	mesh.top_radius = 0.15
	mesh.bottom_radius = 0.18
	mesh.height = 0.5
	mesh.radial_segments = 8
	return mesh


## Cavalry: Box shape (horse-like)
func _create_cavalry_mesh() -> Mesh:
	var mesh = BoxMesh.new()
	mesh.size = Vector3(0.5, 0.35, 0.25)
	return mesh


## Archer: Cone shape (pointed)
func _create_archer_mesh() -> Mesh:
	var mesh = CylinderMesh.new()
	mesh.top_radius = 0.0
	mesh.bottom_radius = 0.15
	mesh.height = 0.5
	mesh.radial_segments = 6
	return mesh


## Galley: Small boat (elongated box)
func _create_galley_mesh() -> Mesh:
	var mesh = BoxMesh.new()
	mesh.size = Vector3(0.6, 0.2, 0.25)
	return mesh


## Warship: Larger boat
func _create_warship_mesh() -> Mesh:
	var mesh = BoxMesh.new()
	mesh.size = Vector3(0.7, 0.3, 0.35)
	return mesh


## Marine: Similar to infantry but distinctive
func _create_marine_mesh() -> Mesh:
	var mesh = CylinderMesh.new()
	mesh.top_radius = 0.12
	mesh.bottom_radius = 0.2
	mesh.height = 0.45
	mesh.radial_segments = 6
	return mesh


func set_selected_units(ids: Array[int]) -> void:
	selected_unit_ids.clear()
	for id in ids:
		selected_unit_ids[id] = true
	_apply_selection_colors()


func _apply_selection_colors() -> void:
	_update_type_colors(UnitTypes.Type.INFANTRY, PLAYER_COLORS_LAND)
	_update_type_colors(UnitTypes.Type.CAVALRY, PLAYER_COLORS_LAND)
	_update_type_colors(UnitTypes.Type.ARCHER, PLAYER_COLORS_LAND)
	_update_type_colors(UnitTypes.Type.GALLEY, PLAYER_COLORS_NAVAL)
	_update_type_colors(UnitTypes.Type.WARSHIP, PLAYER_COLORS_NAVAL)
	_update_type_colors(UnitTypes.Type.MARINE, PLAYER_COLORS_AMPHIBIOUS)


func _update_type_colors(unit_type: UnitTypes.Type, colors: Array[Color]) -> void:
	var instance = multimeshes.get(unit_type)
	if instance == null:
		return

	var unit_ids = unit_id_maps.get(unit_type, [])
	if unit_ids.is_empty():
		return

	var mm = instance.multimesh
	for i in range(unit_ids.size()):
		var unit_id = unit_ids[i]
		var unit = unit_manager.get_unit(unit_id)
		if unit == null:
			continue

		var color: Color
		if selected_unit_ids.has(unit_id):
			color = SELECTED_COLOR
		else:
			color = colors[unit.player_id % colors.size()]

		mm.set_instance_color(i, color)


func update() -> void:
	if needs_rebuild:
		build()


func mark_dirty() -> void:
	needs_rebuild = true
