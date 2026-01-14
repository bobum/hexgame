class_name MovementCosts
extends RefCounted
## Movement cost calculations for pathfinding.
## Matches web/src/pathfinding/MovementCosts.ts

const RIVER_CROSSING_COST: float = 1.0

## Base movement cost for each terrain type (land units).
## Lower = easier to traverse. INF = impassable.
const LAND_TERRAIN_COSTS: Dictionary = {
	TerrainType.Type.PLAINS: 1.0,
	TerrainType.Type.COAST: 1.0,
	TerrainType.Type.DESERT: 1.0,
	TerrainType.Type.SAVANNA: 1.0,
	TerrainType.Type.FOREST: 1.5,
	TerrainType.Type.TAIGA: 1.5,
	TerrainType.Type.JUNGLE: 2.0,
	TerrainType.Type.TUNDRA: 1.5,
	TerrainType.Type.HILLS: 2.0,
	TerrainType.Type.SNOW: 2.5,
	TerrainType.Type.MOUNTAINS: INF,  # Impassable
	TerrainType.Type.OCEAN: INF,      # Impassable for land units
}

## Base movement cost for each terrain type (naval units).
const NAVAL_TERRAIN_COSTS: Dictionary = {
	TerrainType.Type.OCEAN: 1.0,      # Open water - easy sailing
	TerrainType.Type.COAST: 1.5,      # Coastal waters - slightly harder
	TerrainType.Type.PLAINS: INF,
	TerrainType.Type.DESERT: INF,
	TerrainType.Type.SAVANNA: INF,
	TerrainType.Type.FOREST: INF,
	TerrainType.Type.TAIGA: INF,
	TerrainType.Type.JUNGLE: INF,
	TerrainType.Type.TUNDRA: INF,
	TerrainType.Type.HILLS: INF,
	TerrainType.Type.SNOW: INF,
	TerrainType.Type.MOUNTAINS: INF,
}


## Calculate the movement cost for a LAND unit to move from one cell to an adjacent cell.
static func get_land_movement_cost(from: HexCell, to: HexCell) -> float:
	# Water is impassable for land units (elevation < 0)
	if to.elevation < 0:
		return INF

	# Get base terrain cost
	var cost: float = LAND_TERRAIN_COSTS.get(to.terrain_type, INF)

	# If base terrain is impassable, return early
	if not is_finite(cost):
		return INF

	# Elevation difference penalty
	var elev_diff = to.elevation - from.elevation

	# Cliffs (2+ elevation difference) are impassable
	if abs(elev_diff) >= 2:
		return INF

	# Climbing penalty - going uphill costs more
	if elev_diff > 0:
		cost += elev_diff * 0.5

	# TODO: River crossing penalty (when rivers are implemented)
	# if crosses_river(from, to):
	#     cost += RIVER_CROSSING_COST

	return cost


## Calculate the movement cost for a NAVAL unit to move from one cell to an adjacent cell.
static func get_naval_movement_cost(_from: HexCell, to: HexCell) -> float:
	# Naval units can move on water (elevation < 0) or Ocean/Coast terrain
	var is_water_cell = to.elevation < 0 or \
						to.terrain_type == TerrainType.Type.OCEAN or \
						to.terrain_type == TerrainType.Type.COAST

	if not is_water_cell:
		return INF

	# Get base terrain cost for naval
	var cost: float = NAVAL_TERRAIN_COSTS.get(to.terrain_type, INF)

	# If terrain type isn't in naval costs but cell is water, use default cost
	if not is_finite(cost) and to.elevation < 0:
		cost = 1.0

	return cost


## Calculate the movement cost based on unit type (domain-aware).
static func get_movement_cost_for_unit(from: HexCell, to: HexCell, unit_type: UnitTypes.Type) -> float:
	var domain = UnitTypes.get_domain(unit_type)

	if domain == UnitTypes.Domain.NAVAL:
		return get_naval_movement_cost(from, to)

	if domain == UnitTypes.Domain.AMPHIBIOUS:
		# Amphibious units can use either cost, pick the better one
		var land_cost = get_land_movement_cost(from, to)
		var naval_cost = get_naval_movement_cost(from, to)
		return min(land_cost, naval_cost)

	# Default: land movement
	return get_land_movement_cost(from, to)


## Legacy function - assumes land unit
static func get_movement_cost(from: HexCell, to: HexCell) -> float:
	return get_land_movement_cost(from, to)


## Check if a cell is passable for land units.
static func is_passable_for_land(cell: HexCell) -> bool:
	if cell.elevation < 0:
		return false
	if cell.terrain_type == TerrainType.Type.MOUNTAINS:
		return false
	if cell.terrain_type == TerrainType.Type.OCEAN:
		return false
	return true


## Check if a cell is passable for naval units.
static func is_passable_for_naval(cell: HexCell) -> bool:
	# Naval can go on water (elevation < 0) or Ocean/Coast terrain
	if cell.elevation < 0:
		return true
	if cell.terrain_type == TerrainType.Type.OCEAN:
		return true
	if cell.terrain_type == TerrainType.Type.COAST:
		return true
	return false


## Check if a cell is passable for a specific unit type.
static func is_passable_for_unit(cell: HexCell, unit_type: UnitTypes.Type) -> bool:
	var domain = UnitTypes.get_domain(unit_type)

	if domain == UnitTypes.Domain.NAVAL:
		return is_passable_for_naval(cell)

	if domain == UnitTypes.Domain.AMPHIBIOUS:
		return is_passable_for_land(cell) or is_passable_for_naval(cell)

	return is_passable_for_land(cell)


## Check if a cell is passable at all (legacy - assumes land)
static func is_passable(cell: HexCell) -> bool:
	return is_passable_for_land(cell)


## Check if movement between two adjacent cells is possible.
static func can_move_between(from: HexCell, to: HexCell) -> bool:
	return is_finite(get_movement_cost(from, to))


## Check if movement between two adjacent cells is possible for a specific unit type.
static func can_move_between_for_unit(from: HexCell, to: HexCell, unit_type: UnitTypes.Type) -> bool:
	return is_finite(get_movement_cost_for_unit(from, to, unit_type))
