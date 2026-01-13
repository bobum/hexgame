class_name Pathfinder
extends RefCounted
## A* pathfinder for hex grids.
## Finds optimal paths considering terrain costs, elevation, and unit obstacles.
## Matches web/src/pathfinding/Pathfinder.ts

var grid: HexGrid
var unit_manager: UnitManager


func _init(p_grid: HexGrid, p_unit_manager: UnitManager = null) -> void:
	grid = p_grid
	unit_manager = p_unit_manager


## Find the optimal path between two cells using A* algorithm.
## Returns dictionary with path, cost, and reachable status.
func find_path(start: HexCell, end: HexCell, options: Dictionary = {}) -> Dictionary:
	var ignore_units: bool = options.get("ignore_units", false)
	var max_cost: float = options.get("max_cost", INF)
	var unit_type = options.get("unit_type", null)

	# Quick check: destination must be passable for this unit type
	var dest_passable: bool
	if unit_type != null:
		dest_passable = MovementCosts.is_passable_for_unit(end, unit_type)
	else:
		dest_passable = MovementCosts.is_passable(end)

	if not dest_passable:
		return {"path": [], "cost": INF, "reachable": false}

	# Quick check: destination can't have a unit (unless ignoring units)
	if not ignore_units and unit_manager != null:
		var unit_at_end = unit_manager.get_unit_at(end.q, end.r)
		if unit_at_end != null:
			return {"path": [], "cost": INF, "reachable": false}

	# Same cell - trivial path
	if start.q == end.q and start.r == end.r:
		return {"path": [start], "cost": 0, "reachable": true}

	var frontier = PriorityQueue.new()
	var came_from: Dictionary = {}  # cell_key -> HexCell
	var cost_so_far: Dictionary = {}  # cell_key -> float

	var start_key = _cell_key(start)
	var end_key = _cell_key(end)

	frontier.enqueue(start, 0)
	came_from[start_key] = null
	cost_so_far[start_key] = 0

	while not frontier.is_empty():
		var current: HexCell = frontier.dequeue()
		var current_key = _cell_key(current)

		# Found destination
		if current_key == end_key:
			return {
				"path": _reconstruct_path(came_from, start, end),
				"cost": cost_so_far[end_key],
				"reachable": true
			}

		# Explore neighbors
		for neighbor in grid.get_neighbors(current):
			# Skip if there's a unit (unless ignoring units)
			if not ignore_units and unit_manager != null:
				var unit_at_neighbor = unit_manager.get_unit_at(neighbor.q, neighbor.r)
				# Allow destination even if pathfinding toward a unit (for attack targeting)
				if unit_at_neighbor != null and _cell_key(neighbor) != end_key:
					continue

			# Calculate movement cost (domain-aware if unit type provided)
			var move_cost: float
			if unit_type != null:
				move_cost = MovementCosts.get_movement_cost_for_unit(current, neighbor, unit_type)
			else:
				move_cost = MovementCosts.get_movement_cost(current, neighbor)

			# Skip impassable terrain
			if not is_finite(move_cost):
				continue

			var new_cost = cost_so_far[current_key] + move_cost

			# Skip if exceeds max cost
			if new_cost > max_cost:
				continue

			var neighbor_key = _cell_key(neighbor)

			if not cost_so_far.has(neighbor_key) or new_cost < cost_so_far[neighbor_key]:
				cost_so_far[neighbor_key] = new_cost

				# A* priority = cost so far + heuristic estimate to goal
				var priority = new_cost + _heuristic(neighbor, end)
				frontier.enqueue(neighbor, priority)
				came_from[neighbor_key] = current

	# No path found
	return {"path": [], "cost": INF, "reachable": false}


## Get all cells reachable from a starting cell within a movement budget.
## Returns dictionary of cell -> movement cost.
func get_reachable_cells(start: HexCell, movement_points: float, options: Dictionary = {}) -> Dictionary:
	var ignore_units: bool = options.get("ignore_units", false)
	var unit_type = options.get("unit_type", null)

	var reachable: Dictionary = {}  # HexCell -> float
	var frontier = PriorityQueue.new()
	var cost_so_far: Dictionary = {}  # cell_key -> float

	var start_key = _cell_key(start)
	frontier.enqueue(start, 0)
	cost_so_far[start_key] = 0
	reachable[start] = 0

	while not frontier.is_empty():
		var current: HexCell = frontier.dequeue()
		var current_key = _cell_key(current)
		var current_cost = cost_so_far[current_key]

		for neighbor in grid.get_neighbors(current):
			# Skip if there's a unit (unless ignoring)
			if not ignore_units and unit_manager != null:
				var unit_at_neighbor = unit_manager.get_unit_at(neighbor.q, neighbor.r)
				if unit_at_neighbor != null:
					continue

			# Calculate movement cost (domain-aware if unit type provided)
			var move_cost: float
			if unit_type != null:
				move_cost = MovementCosts.get_movement_cost_for_unit(current, neighbor, unit_type)
			else:
				move_cost = MovementCosts.get_movement_cost(current, neighbor)

			# Skip impassable
			if not is_finite(move_cost):
				continue

			var new_cost = current_cost + move_cost

			# Skip if exceeds movement budget
			if new_cost > movement_points:
				continue

			var neighbor_key = _cell_key(neighbor)

			if not cost_so_far.has(neighbor_key) or new_cost < cost_so_far[neighbor_key]:
				cost_so_far[neighbor_key] = new_cost
				frontier.enqueue(neighbor, new_cost)
				reachable[neighbor] = new_cost

	return reachable


## Check if a path exists between two cells.
func has_path(start: HexCell, end: HexCell, ignore_units: bool = false) -> bool:
	var result = find_path(start, end, {"ignore_units": ignore_units})
	return result["reachable"]


## Get the movement cost between two adjacent cells.
func get_step_cost(from: HexCell, to: HexCell) -> float:
	# Check if adjacent
	var from_coords = HexCoordinates.new(from.q, from.r)
	var to_coords = HexCoordinates.new(to.q, to.r)

	if from_coords.distance_to(to_coords) != 1:
		return INF  # Not adjacent

	return MovementCosts.get_movement_cost(from, to)


## Heuristic function for A* - hex distance.
func _heuristic(a: HexCell, b: HexCell) -> float:
	var coords_a = HexCoordinates.new(a.q, a.r)
	var coords_b = HexCoordinates.new(b.q, b.r)
	return float(coords_a.distance_to(coords_b))


## Generate a unique key for a cell.
func _cell_key(cell: HexCell) -> String:
	return "%d,%d" % [cell.q, cell.r]


## Reconstruct the path from the came_from map.
func _reconstruct_path(came_from: Dictionary, start: HexCell, end: HexCell) -> Array[HexCell]:
	var path: Array[HexCell] = []
	var current: HexCell = end

	while current != null:
		path.push_front(current)
		var key = _cell_key(current)
		current = came_from.get(key)

	return path
