class_name UnitManager
extends RefCounted
## Manages all units in the game.
## Matches web/src/units/UnitManager.ts

var units: Dictionary = {}  # id -> Unit
var next_id: int = 1
var grid: HexGrid

signal unit_created(unit: Unit)
signal unit_removed(unit_id: int)
signal unit_moved(unit: Unit, from_q: int, from_r: int)


func _init(p_grid: HexGrid) -> void:
	grid = p_grid


## Create a new unit at the specified hex.
func create_unit(type: UnitTypes.Type, q: int, r: int, player_id: int) -> Unit:
	# Check if hex is valid
	var cell = grid.get_cell(q, r)
	if cell == null:
		return null

	# Check domain compatibility
	var is_water = cell.elevation < 0
	if is_water and not UnitTypes.can_traverse_water(type):
		return null  # Land unit can't be placed on water
	if not is_water and not UnitTypes.can_traverse_land(type):
		return null  # Naval unit can't be placed on land

	# Check if hex is already occupied
	if get_unit_at(q, r) != null:
		return null

	# Create unit
	var unit = Unit.new(type)
	unit.id = next_id
	next_id += 1
	unit.q = q
	unit.r = r
	unit.player_id = player_id

	# Add to tracking
	units[unit.id] = unit
	unit_created.emit(unit)

	return unit


## Remove a unit from the game.
func remove_unit(unit_id: int) -> bool:
	if not units.has(unit_id):
		return false

	units.erase(unit_id)
	unit_removed.emit(unit_id)
	return true


## Move a unit to a new hex.
func move_unit(unit_id: int, to_q: int, to_r: int, movement_cost: int = -1) -> bool:
	var unit = units.get(unit_id)
	if unit == null:
		return false

	# Check destination is valid
	var cell = grid.get_cell(to_q, to_r)
	if cell == null:
		return false

	# Check domain compatibility
	var is_water = cell.elevation < 0
	if is_water and not unit.can_traverse_water():
		return false
	if not is_water and not unit.can_traverse_land():
		return false

	# Check not occupied
	if get_unit_at(to_q, to_r) != null:
		return false

	# Check movement cost if provided
	if movement_cost >= 0:
		if unit.movement < movement_cost:
			return false
		unit.spend_movement(movement_cost)

	# Store old position
	var from_q = unit.q
	var from_r = unit.r

	# Update position
	unit.q = to_q
	unit.r = to_r

	unit_moved.emit(unit, from_q, from_r)
	return true


## Reset movement for all units (called at start of turn).
func reset_all_movement() -> void:
	for unit in units.values():
		unit.reset_movement()


## Reset movement for units of a specific player.
func reset_player_movement(player_id: int) -> void:
	for unit in units.values():
		if unit.player_id == player_id:
			unit.reset_movement()


## Get unit at a specific hex.
func get_unit_at(q: int, r: int) -> Unit:
	for unit in units.values():
		if unit.q == q and unit.r == r:
			return unit
	return null


## Get unit by ID.
func get_unit(id: int) -> Unit:
	return units.get(id)


## Get all units.
func get_all_units() -> Array[Unit]:
	var result: Array[Unit] = []
	for unit in units.values():
		result.append(unit)
	return result


## Get units for a specific player.
func get_player_units(player_id: int) -> Array[Unit]:
	var result: Array[Unit] = []
	for unit in units.values():
		if unit.player_id == player_id:
			result.append(unit)
	return result


## Get unit count.
func get_unit_count() -> int:
	return units.size()


## Get counts by domain (land vs naval).
func get_unit_counts() -> Dictionary:
	var land = 0
	var naval = 0
	for unit in units.values():
		if UnitTypes.get_domain(unit.type) == UnitTypes.Domain.NAVAL:
			naval += 1
		else:
			land += 1
	return {"land": land, "naval": naval}


## Clear all units.
func clear() -> void:
	units.clear()
	next_id = 1


## Spawn random land units for testing.
func spawn_random_units(count: int, player_id: int = 1) -> int:
	var land_cells: Array = []
	for cell in grid.get_all_cells():
		if cell.elevation >= 0:
			land_cells.append(cell)

	var spawned = 0
	var land_types = UnitTypes.get_land_types()

	for i in range(count):
		if land_cells.is_empty():
			break
		var idx = randi() % land_cells.size()
		var cell = land_cells[idx]
		var type = land_types[randi() % land_types.size()]

		if create_unit(type, cell.q, cell.r, player_id):
			spawned += 1
			land_cells.remove_at(idx)

	return spawned


## Spawn random naval units for testing.
func spawn_random_naval_units(count: int, player_id: int = 1) -> int:
	var water_cells: Array = []
	for cell in grid.get_all_cells():
		if cell.elevation < 0:
			water_cells.append(cell)

	var spawned = 0
	var naval_types = UnitTypes.get_naval_types()

	for i in range(count):
		if water_cells.is_empty():
			break
		var idx = randi() % water_cells.size()
		var cell = water_cells[idx]
		var type = naval_types[randi() % naval_types.size()]

		if create_unit(type, cell.q, cell.r, player_id):
			spawned += 1
			water_cells.remove_at(idx)

	return spawned


## Spawn a mix of land and naval units for testing.
func spawn_mixed_units(land_count: int, naval_count: int, player_id: int = 1) -> Dictionary:
	return {
		"land": spawn_random_units(land_count, player_id),
		"naval": spawn_random_naval_units(naval_count, player_id),
	}
