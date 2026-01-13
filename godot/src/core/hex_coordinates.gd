class_name HexCoordinates
extends RefCounted
## Axial hex coordinates (q, r) with cube coordinate conversions
## Matches web/src/core/HexCoordinates.ts

var q: int  # Column (axial)
var r: int  # Row (axial)


func _init(q_val: int = 0, r_val: int = 0) -> void:
	q = q_val
	r = r_val


## Cube coordinate X (derived from axial)
func get_x() -> int:
	return q


## Cube coordinate Y (derived from axial)
func get_y() -> int:
	return -q - r


## Cube coordinate Z (derived from axial)
func get_z() -> int:
	return r


## Convert to world position at given elevation
func to_world_position(elevation: int = 0) -> Vector3:
	var x = (q + r * 0.5) * (HexMetrics.INNER_RADIUS * 2.0)
	var z = r * (HexMetrics.OUTER_RADIUS * 1.5)
	var y = elevation * HexMetrics.ELEVATION_STEP
	return Vector3(x, y, z)


## Create from world position
static func from_world_position(position: Vector3) -> HexCoordinates:
	var q_float = position.x / (HexMetrics.INNER_RADIUS * 2.0)
	var r_float = position.z / (HexMetrics.OUTER_RADIUS * 1.5)
	q_float -= r_float * 0.5

	# Round to nearest hex
	var q_int = roundi(q_float)
	var r_int = roundi(r_float)

	return HexCoordinates.new(q_int, r_int)


## Calculate distance to another hex (in hex steps)
func distance_to(other: HexCoordinates) -> int:
	var dx = abs(get_x() - other.get_x())
	var dy = abs(get_y() - other.get_y())
	var dz = abs(get_z() - other.get_z())
	return (dx + dy + dz) / 2


## Get neighbor in given direction
func get_neighbor(direction: int) -> HexCoordinates:
	var offsets = HexDirection.get_offset(direction)
	return HexCoordinates.new(q + offsets.x, r + offsets.y)


## String representation
func _to_string() -> String:
	return "(%d, %d)" % [q, r]


## Equality check
func equals(other: HexCoordinates) -> bool:
	return q == other.q and r == other.r
