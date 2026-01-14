class_name SelectionManager
extends Node
## Manages unit selection via click, ctrl+click, and box selection.
## Matches web selection behavior from main.ts

signal selection_changed(selected_ids: Array[int])

var unit_manager: UnitManager
var unit_renderer: UnitRenderer
var grid: HexGrid
var camera: Camera3D
var pathfinder: Pathfinder
var path_renderer: PathRenderer
var turn_manager: TurnManager

# Selected unit IDs
var selected_unit_ids: Dictionary = {}  # Set<int>

# Box selection state
var is_box_selecting: bool = false
var box_select_start: Vector2 = Vector2.ZERO

# Selection box visual (CanvasLayer)
var selection_box: ColorRect


func setup(p_unit_manager: UnitManager, p_unit_renderer: UnitRenderer, p_grid: HexGrid, p_camera: Camera3D, p_pathfinder: Pathfinder = null, p_path_renderer: PathRenderer = null, p_turn_manager: TurnManager = null) -> void:
	unit_manager = p_unit_manager
	unit_renderer = p_unit_renderer
	grid = p_grid
	camera = p_camera
	pathfinder = p_pathfinder
	path_renderer = p_path_renderer
	turn_manager = p_turn_manager


func _input(event: InputEvent) -> void:
	if unit_manager == null or camera == null:
		return

	# Mouse button events
	if event is InputEventMouseButton:
		var mb = event as InputEventMouseButton

		# Left click
		if mb.button_index == MOUSE_BUTTON_LEFT:
			if mb.pressed:
				# Shift+click starts box selection
				if mb.shift_pressed:
					_start_box_select(mb.position)
				# Regular click or ctrl+click handled on release
			else:
				# Release
				if is_box_selecting:
					_finish_box_select(mb.position, mb.ctrl_pressed)
				else:
					_handle_click(mb.position, mb.ctrl_pressed)

		# Right click - move selected unit
		elif mb.button_index == MOUSE_BUTTON_RIGHT and mb.pressed:
			_handle_right_click(mb.position)

	# Mouse motion for box selection
	elif event is InputEventMouseMotion and is_box_selecting:
		_update_selection_box(event.position)


func _handle_click(screen_pos: Vector2, ctrl_pressed: bool) -> void:
	# Raycast to find clicked unit
	var unit = _get_unit_at_screen_pos(screen_pos)

	if unit == null:
		if not ctrl_pressed:
			clear_selection()
		return

	# Only select player 1's units for now
	if unit.player_id != 1:
		if not ctrl_pressed:
			clear_selection()
		return

	if ctrl_pressed:
		# Toggle selection
		if selected_unit_ids.has(unit.id):
			selected_unit_ids.erase(unit.id)
		else:
			selected_unit_ids[unit.id] = true
	else:
		# Replace selection
		selected_unit_ids.clear()
		selected_unit_ids[unit.id] = true

	_update_selection_visuals()


func _handle_right_click(screen_pos: Vector2) -> void:
	# Need exactly one unit selected
	if selected_unit_ids.size() != 1:
		return

	var unit_id = selected_unit_ids.keys()[0]
	var unit = unit_manager.get_unit(unit_id)
	if unit == null:
		return

	# Check turn system - must be movement phase for current player's unit
	if turn_manager:
		if not turn_manager.can_move():
			print("Not in movement phase")
			return
		if not turn_manager.is_current_player_unit(unit.player_id):
			print("Not your turn")
			return
	else:
		# Fallback: only move player 1's units
		if unit.player_id != 1:
			return

	# Check if unit can move
	if unit.movement <= 0:
		print("Unit has no movement left")
		return

	# Raycast to find target hex
	var target_cell = _get_cell_at_screen_pos(screen_pos)
	if target_cell == null:
		return

	# Get current cell
	var start_cell = grid.get_cell(unit.q, unit.r)
	if start_cell == null:
		return

	# Use pathfinding if available
	if pathfinder != null:
		var result = pathfinder.find_path(start_cell, target_cell, {
			"unit_type": unit.type,
			"max_cost": float(unit.movement)
		})

		if result["reachable"]:
			var path: Array = result["path"]
			var cost: float = result["cost"]

			# Move along the path to the destination
			if path.size() >= 2:
				var end_cell = path[path.size() - 1]
				if unit_manager.move_unit(unit_id, end_cell.q, end_cell.r, int(ceil(cost))):
					print("Moved unit %d to (%d, %d) via %d cells, cost: %.1f" % [unit_id, end_cell.q, end_cell.r, path.size(), cost])
		else:
			print("No valid path to destination")
	else:
		# Fallback: Simple direct move (for testing)
		var is_water = target_cell.elevation < 0
		if is_water and not unit.can_traverse_water():
			return
		if not is_water and not unit.can_traverse_land():
			return

		if unit_manager.move_unit(unit_id, target_cell.q, target_cell.r, 1):
			print("Moved unit %d to (%d, %d)" % [unit_id, target_cell.q, target_cell.r])


func _start_box_select(screen_pos: Vector2) -> void:
	is_box_selecting = true
	box_select_start = screen_pos
	_show_selection_box()


func _finish_box_select(end_pos: Vector2, ctrl_pressed: bool) -> void:
	is_box_selecting = false
	_hide_selection_box()

	var min_x = min(box_select_start.x, end_pos.x)
	var max_x = max(box_select_start.x, end_pos.x)
	var min_y = min(box_select_start.y, end_pos.y)
	var max_y = max(box_select_start.y, end_pos.y)

	# Minimum drag distance to count as box select
	if max_x - min_x < 5 and max_y - min_y < 5:
		return

	if not ctrl_pressed:
		selected_unit_ids.clear()

	# Check each unit's screen position
	for unit in unit_manager.get_all_units():
		# Only select player 1's units
		if unit.player_id != 1:
			continue

		var world_pos = unit.get_world_position()
		var cell = grid.get_cell(unit.q, unit.r)
		if cell:
			world_pos.y = cell.elevation * HexMetrics.ELEVATION_STEP + 0.25

		# Project to screen
		if not camera.is_position_behind(world_pos):
			var screen_pos = camera.unproject_position(world_pos)
			if screen_pos.x >= min_x and screen_pos.x <= max_x and screen_pos.y >= min_y and screen_pos.y <= max_y:
				selected_unit_ids[unit.id] = true

	_update_selection_visuals()


func _update_selection_box(current_pos: Vector2) -> void:
	if selection_box == null:
		return

	var left = min(box_select_start.x, current_pos.x)
	var top = min(box_select_start.y, current_pos.y)
	var width = abs(current_pos.x - box_select_start.x)
	var height = abs(current_pos.y - box_select_start.y)

	selection_box.position = Vector2(left, top)
	selection_box.size = Vector2(width, height)


func _show_selection_box() -> void:
	if selection_box == null:
		# Create selection box
		var canvas_layer = CanvasLayer.new()
		canvas_layer.layer = 10
		add_child(canvas_layer)

		selection_box = ColorRect.new()
		selection_box.color = Color(0.3, 0.5, 0.9, 0.3)
		canvas_layer.add_child(selection_box)

	selection_box.visible = true
	selection_box.position = box_select_start
	selection_box.size = Vector2.ZERO


func _hide_selection_box() -> void:
	if selection_box:
		selection_box.visible = false


## Update path preview when hovering over a cell
func update_path_preview(target_cell: HexCell) -> void:
	if path_renderer == null or pathfinder == null:
		return

	# Only show path if exactly one unit selected
	if selected_unit_ids.size() != 1:
		path_renderer.hide_path()
		return

	var unit_id = selected_unit_ids.keys()[0]
	var unit = unit_manager.get_unit(unit_id)
	if unit == null:
		path_renderer.hide_path()
		return

	var start_cell = grid.get_cell(unit.q, unit.r)
	if start_cell == null:
		path_renderer.hide_path()
		return

	# Don't show path to current position
	if start_cell.q == target_cell.q and start_cell.r == target_cell.r:
		path_renderer.hide_path()
		return

	# Find path
	var result = pathfinder.find_path(start_cell, target_cell, {
		"unit_type": unit.type,
	})

	if result["reachable"] and result["path"].size() > 0:
		path_renderer.show_path(result["path"])
		# Color indicates if within movement range
		path_renderer.set_path_valid(result["cost"] <= unit.movement)
	else:
		path_renderer.hide_path()


## Clear path preview when not hovering
func clear_path_preview() -> void:
	if path_renderer:
		path_renderer.hide_path()


func _update_selection_visuals() -> void:
	if unit_renderer:
		var ids: Array[int] = []
		for id in selected_unit_ids.keys():
			ids.append(id)
		unit_renderer.set_selected_units(ids)

	# Show reachable cells for single selected unit
	if path_renderer and pathfinder:
		if selected_unit_ids.size() == 1:
			var unit_id = selected_unit_ids.keys()[0]
			var unit = unit_manager.get_unit(unit_id)
			if unit and unit.movement > 0:
				var start_cell = grid.get_cell(unit.q, unit.r)
				if start_cell:
					var reachable = pathfinder.get_reachable_cells(start_cell, float(unit.movement), {
						"unit_type": unit.type
					})
					path_renderer.show_reachable_cells(reachable)
			else:
				path_renderer.hide_reachable_cells()
		else:
			path_renderer.hide_reachable_cells()

	selection_changed.emit(get_selected_ids())


func _get_unit_at_screen_pos(screen_pos: Vector2) -> Unit:
	var cell = _get_cell_at_screen_pos(screen_pos)
	if cell == null:
		return null
	return unit_manager.get_unit_at(cell.q, cell.r)


func _get_cell_at_screen_pos(screen_pos: Vector2) -> HexCell:
	var ray_origin = camera.project_ray_origin(screen_pos)
	var ray_dir = camera.project_ray_normal(screen_pos)

	if abs(ray_dir.y) < 0.001:
		return null

	# Raycast against multiple elevation levels to find the actual terrain surface
	# Check from highest to lowest elevation to find the first valid intersection
	var best_cell: HexCell = null
	var best_distance: float = INF

	for elev in range(HexMetrics.MAX_ELEVATION, HexMetrics.MIN_ELEVATION - 1, -1):
		var plane_y = elev * HexMetrics.ELEVATION_STEP
		var t = (plane_y - ray_origin.y) / ray_dir.y

		if t <= 0:
			continue  # Behind camera

		var hit_point = ray_origin + ray_dir * t
		var coords = HexCoordinates.from_world_position(hit_point)
		var cell = grid.get_cell(coords.q, coords.r)

		if cell and cell.elevation == elev:
			# Found a cell at this elevation
			if t < best_distance:
				best_distance = t
				best_cell = cell
				break  # First valid hit from high to low is closest

	# Fallback to water level if no elevated cell found
	if best_cell == null:
		var t = -ray_origin.y / ray_dir.y
		if t > 0:
			var hit_point = ray_origin + ray_dir * t
			var coords = HexCoordinates.from_world_position(hit_point)
			best_cell = grid.get_cell(coords.q, coords.r)

	return best_cell


func clear_selection() -> void:
	selected_unit_ids.clear()
	_update_selection_visuals()


func get_selected_ids() -> Array[int]:
	var ids: Array[int] = []
	for id in selected_unit_ids.keys():
		ids.append(id)
	return ids


func get_selected_units() -> Array[Unit]:
	var units: Array[Unit] = []
	for id in selected_unit_ids.keys():
		var unit = unit_manager.get_unit(id)
		if unit:
			units.append(unit)
	return units


func has_selection() -> bool:
	return selected_unit_ids.size() > 0


func get_single_selected_unit() -> Unit:
	if selected_unit_ids.size() != 1:
		return null
	return unit_manager.get_unit(selected_unit_ids.keys()[0])
