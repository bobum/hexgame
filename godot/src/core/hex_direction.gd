class_name HexDirection
extends RefCounted
## Hex direction utilities for flat-topped hexes
## Matches web/src/core/HexDirection.ts

enum Direction {
	NE = 0,  # Northeast
	E = 1,   # East
	SE = 2,  # Southeast
	SW = 3,  # Southwest
	W = 4,   # West
	NW = 5   # Northwest
}

# Axial coordinate offsets for each direction (q, r)
# Must match web/src/core/HexDirection.ts DirectionOffsets
const OFFSETS: Array[Vector2i] = [
	Vector2i(1, 0),    # NE: q+1, r+0
	Vector2i(1, -1),   # E:  q+1, r-1
	Vector2i(0, -1),   # SE: q+0, r-1
	Vector2i(-1, 0),   # SW: q-1, r+0
	Vector2i(-1, 1),   # W:  q-1, r+1
	Vector2i(0, 1)     # NW: q+0, r+1
]


## Get the offset for a direction
static func get_offset(direction: int) -> Vector2i:
	return OFFSETS[direction % 6]


## Get the opposite direction
static func opposite(direction: int) -> int:
	return (direction + 3) % 6


## Get the next direction (clockwise)
static func next(direction: int) -> int:
	return (direction + 1) % 6


## Get the previous direction (counter-clockwise)
static func previous(direction: int) -> int:
	return (direction + 5) % 6
