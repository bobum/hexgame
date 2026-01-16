class_name HexCell
extends RefCounted
## Represents a single hex cell in the grid
## Matches web/src/types/index.ts HexCell interface

var q: int = 0  # Axial Q coordinate
var r: int = 0  # Axial R coordinate
var elevation: int = 0  # Height level
var terrain_type: TerrainType.Type = TerrainType.Type.PLAINS
var moisture: float = 0.0  # 0-1 moisture level
var temperature: float = 0.5  # 0-1 temperature

# River data
var has_river: bool = false
var river_directions: Array[int] = []  # Which edges have rivers

# Feature data
var has_road: bool = false
var features: Array = []  # Trees, rocks, etc. (Feature objects)


## Get world position of this cell's center
## All cells render at their actual elevation
func get_world_position() -> Vector3:
	var coords = HexCoordinates.new(q, r)
	return coords.to_world_position(elevation)


## Check if this cell is underwater (water is elevation 0-4, land is 5+)
func is_underwater() -> bool:
	return elevation < HexMetrics.LAND_MIN_ELEVATION


## Check if terrain is water type
func is_water() -> bool:
	return TerrainType.is_water(terrain_type)


## Get terrain color
func get_color() -> Color:
	return TerrainType.get_color(terrain_type)


## String representation
func _to_string() -> String:
	return "HexCell(%d, %d) elev=%d terrain=%s" % [q, r, elevation, TerrainType.Type.keys()[terrain_type]]
