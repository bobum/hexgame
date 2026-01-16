class_name UnitManager
extends RefCounted
## Manages all units in the game.
## Matches web/src/units/UnitManager.ts

var units: Dictionary = {}  # id -> Unit
var next_id: int = 1
var grid: HexGrid

# Spatial indexing for O(1) lookups
var _hex_positions: Dictionary = {}  # "q,r" -> Unit (for hex coordinate lookups)
var _spatial_hash: SpatialHash  # For world coordinate queries (radius, rect)

# Object pooling for units (reduces GC pressure)
var _unit_pool: ObjectPool

signal unit_created(unit: Unit)
signal unit_removed(unit_id: int)
signal unit_moved(unit: Unit, from_q: int, from_r: int)


func _init(p_grid: HexGrid) -> void:
	grid = p_grid
	# Cell size of 2.0 works well for hex grids (slightly larger than hex radius)
	_spatial_hash = SpatialHash.new(2.0)
	# Initialize unit pool (will be set up after _init completes)
	_unit_pool = null


## Set up object pooling (call after construction)
func setup_pool() -> void:
	_unit_pool = ObjectPool.new(
		Callable(self, "_create_unit_for_pool"),
		Callable(self, "_reset_unit_for_pool"),
		500  # Max pool size
	)


## Factory function for unit pool.
func _create_unit_for_pool() -> Unit:
	return Unit.new()


## Reset function for unit pool.
func _reset_unit_for_pool(unit: Unit) -> void:
	unit.reset_for_pool()


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

	# Acquire unit from pool (or create directly if pool not set up)
	var unit: Unit
	if _unit_pool:
		unit = _unit_pool.acquire() as Unit
	else:
		unit = Unit.new()
	unit.id = next_id
	next_id += 1
	unit.init_with(type, q, r, player_id)

	# Add to tracking
	units[unit.id] = unit

	# Add to spatial indexes
	var hex_key = "%d,%d" % [q, r]
	_hex_positions[hex_key] = unit
	var world_pos = HexCoordinates.new(q, r).to_world_position(cell.elevation)
	_spatial_hash.insert(unit, world_pos.x, world_pos.z)

	unit_created.emit(unit)

	return unit


## Remove a unit from the game.
func remove_unit(unit_id: int) -> bool:
	if not units.has(unit_id):
		return false

	var unit = units[unit_id] as Unit

	# Remove from spatial indexes
	var hex_key = "%d,%d" % [unit.q, unit.r]
	_hex_positions.erase(hex_key)
	_spatial_hash.remove(unit)

	units.erase(unit_id)

	# Release unit back to pool for reuse (if pool is set up)
	if _unit_pool:
		_unit_pool.release(unit)

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

	# Update spatial indexes (remove old, add new)
	var old_hex_key = "%d,%d" % [from_q, from_r]
	var new_hex_key = "%d,%d" % [to_q, to_r]
	_hex_positions.erase(old_hex_key)
	_hex_positions[new_hex_key] = unit

	# Update position
	unit.q = to_q
	unit.r = to_r

	# Update spatial hash with new world position
	var world_pos = HexCoordinates.new(to_q, to_r).to_world_position(cell.elevation)
	_spatial_hash.update(unit, world_pos.x, world_pos.z)

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


## Get unit at a specific hex (O(1) lookup via spatial index).
func get_unit_at(q: int, r: int) -> Unit:
	var hex_key = "%d,%d" % [q, r]
	return _hex_positions.get(hex_key)


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
	# Release all units back to pool (if pool is set up)
	if _unit_pool:
		for unit in units.values():
			_unit_pool.release(unit)
	units.clear()
	_hex_positions.clear()
	_spatial_hash.clear()
	next_id = 1


## Get units within a world-coordinate radius (for range attacks, area effects).
func get_units_in_radius(world_x: float, world_z: float, radius: float) -> Array[Unit]:
	var results: Array[Unit] = []
	for item in _spatial_hash.query_radius(world_x, world_z, radius):
		if item is Unit:
			results.append(item)
	return results


## Get units within a world-coordinate rectangle (for selection box).
func get_units_in_rect(min_x: float, min_z: float, max_x: float, max_z: float) -> Array[Unit]:
	var results: Array[Unit] = []
	for item in _spatial_hash.query_rect(min_x, min_z, max_x, max_z):
		if item is Unit:
			results.append(item)
	return results


## Get spatial hash statistics for debugging.
func get_spatial_stats() -> Dictionary:
	return _spatial_hash.get_stats()


## Get object pool statistics for debugging.
func get_pool_stats() -> Dictionary:
	if _unit_pool:
		return _unit_pool.get_stats()
	return {"available": 0, "active": 0, "created": 0, "reused": 0, "peak": 0, "reuse_rate": 0.0}


## Prewarm unit pool with objects.
func prewarm_pool(count: int) -> void:
	if _unit_pool:
		_unit_pool.prewarm(count)


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
