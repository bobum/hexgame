class_name Unit
extends RefCounted
## Runtime unit instance data.
## Matches web UnitData interface.

var id: int = 0
var type: UnitTypes.Type
var q: int = 0              # Hex position Q
var r: int = 0              # Hex position R
var health: int = 100
var max_health: int = 100
var movement: int = 2       # Current movement points this turn
var max_movement: int = 2   # Movement points per turn
var attack: int = 10
var defense: int = 8
var player_id: int = 0      # 0 = neutral, 1 = player, 2+ = AI
var has_moved: bool = false


func _init(unit_type: UnitTypes.Type = UnitTypes.Type.INFANTRY) -> void:
	type = unit_type
	_apply_stats()


func _apply_stats() -> void:
	var stats = UnitTypes.get_stats(type)
	health = stats["health"]
	max_health = stats["health"]
	movement = stats["movement"]
	max_movement = stats["movement"]
	attack = stats["attack"]
	defense = stats["defense"]


func reset_movement() -> void:
	movement = max_movement
	has_moved = false


func spend_movement(cost: int) -> bool:
	if movement < cost:
		return false
	movement -= cost
	has_moved = true
	return true


func can_move() -> bool:
	return movement > 0


func get_world_position() -> Vector3:
	var coords = HexCoordinates.new(q, r)
	return coords.to_world_position(0)


func get_type_name() -> String:
	return UnitTypes.get_name(type)


func get_domain() -> UnitTypes.Domain:
	return UnitTypes.get_domain(type)


func can_traverse_land() -> bool:
	return UnitTypes.can_traverse_land(type)


func can_traverse_water() -> bool:
	return UnitTypes.can_traverse_water(type)


## Reset unit state for reuse from object pool.
func reset_for_pool() -> void:
	id = 0
	type = UnitTypes.Type.INFANTRY
	q = 0
	r = 0
	health = 100
	max_health = 100
	movement = 2
	max_movement = 2
	attack = 10
	defense = 8
	player_id = 0
	has_moved = false


## Initialize unit with new values (used when acquiring from pool).
func init_with(unit_type: UnitTypes.Type, unit_q: int, unit_r: int, unit_player_id: int) -> void:
	type = unit_type
	q = unit_q
	r = unit_r
	player_id = unit_player_id
	has_moved = false
	_apply_stats()
