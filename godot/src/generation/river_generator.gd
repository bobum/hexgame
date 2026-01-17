class_name RiverGenerator
extends RefCounted
## Generates rivers flowing from high elevation to water.
## Uses steepest descent algorithm - rivers can ONLY flow downhill.
## Matches web/src/generation/RiverGenerator.ts

var grid: HexGrid
var river_percentage: float = 0.1
var seed_val: int = 0

# Minimum number of edges for a valid river
const MIN_RIVER_LENGTH: int = 3

# Simple seeded random
var _rng: RandomNumberGenerator


func _init(p_grid: HexGrid) -> void:
	grid = p_grid
	_rng = RandomNumberGenerator.new()


## Generate rivers on the map.
func generate(p_seed: int = 0, p_river_percentage: float = 0.1) -> void:
	seed_val = p_seed if p_seed != 0 else randi()
	river_percentage = p_river_percentage

	# Use different seed offset for rivers to avoid correlation with terrain
	_rng.seed = seed_val + 7777

	# Clear existing rivers
	for cell in grid.get_all_cells():
		cell.river_directions.clear()
		cell.has_river = false

	# Count land cells for budget calculation
	var land_cells: Array[HexCell] = []
	for cell in grid.get_all_cells():
		if cell.elevation >= HexMetrics.SEA_LEVEL:
			land_cells.append(cell)

	if land_cells.is_empty():
		return

	# Calculate river budget based on percentage
	var river_budget = int(land_cells.size() * river_percentage)

	# Find potential river sources (high elevation + moisture)
	var sources = _find_river_sources(land_cells)

	# Generate rivers until budget exhausted or no more sources
	var attempts = 0
	var max_attempts = sources.size() * 2

	while river_budget > 0 and not sources.is_empty() and attempts < max_attempts:
		attempts += 1

		# Pick a random source (weighted toward better candidates)
		var source_index = _pick_weighted_source(sources)
		var source = sources[source_index]

		# Try to create a river from this source
		var river_length = _trace_river(source)

		if river_length > 0:
			river_budget -= river_length
			# Remove used source
			sources.remove_at(source_index)
		else:
			# Source didn't work, remove it
			sources.remove_at(source_index)


## Find cells that are good river sources.
## High elevation + high moisture = good source.
func _find_river_sources(land_cells: Array[HexCell]) -> Array[HexCell]:
	var sources: Array[HexCell] = []
	var elevation_range = HexMetrics.MAX_ELEVATION - HexMetrics.SEA_LEVEL

	for cell in land_cells:
		# Skip cells already with rivers
		if not cell.river_directions.is_empty():
			continue

		# Skip cells adjacent to water (too close to ocean)
		if _is_adjacent_to_water(cell):
			continue

		# Skip cells adjacent to existing rivers
		if _is_adjacent_to_river(cell):
			continue

		# Calculate source fitness score
		var elevation_factor = float(cell.elevation - HexMetrics.SEA_LEVEL) / elevation_range
		var score = cell.moisture * elevation_factor

		# Add to sources with weighting
		if score > 0.25:
			sources.append(cell)

	return sources


## Pick a source using weighted random selection.
func _pick_weighted_source(sources: Array[HexCell]) -> int:
	var elevation_range = HexMetrics.MAX_ELEVATION - HexMetrics.SEA_LEVEL

	# Build weighted selection list
	var weights: Array[float] = []
	var total_weight: float = 0.0

	for cell in sources:
		var elevation_factor = float(cell.elevation - HexMetrics.SEA_LEVEL) / elevation_range
		var score = cell.moisture * elevation_factor

		# Higher score = more weight
		var weight: float = 1.0
		if score > 0.75:
			weight = 4.0
		elif score > 0.5:
			weight = 2.0

		weights.append(weight)
		total_weight += weight

	# Random selection
	var pick = _rng.randf() * total_weight
	for i in range(weights.size()):
		pick -= weights[i]
		if pick <= 0:
			return i

	return sources.size() - 1


## Trace a river from source to water using steepest descent.
## Rivers can ONLY flow downhill. If stuck or too short, discard entirely.
## Returns the length of the river created (0 if discarded).
func _trace_river(source: HexCell) -> int:
	var current = source
	var visited: Dictionary = {}

	# Track cells we add river segments to, so we can remove them if river is too short
	var river_cells: Array[Dictionary] = []

	while current.elevation >= HexMetrics.SEA_LEVEL:
		var key = "%d,%d" % [current.q, current.r]
		if visited.has(key):
			break  # Avoid loops
		visited[key] = true

		# Find best direction to flow (strictly downhill only)
		var flow_dir = _find_flow_direction(current)

		if flow_dir < 0:
			# Can't flow anywhere - dead end, stop here
			break

		# Get the neighbor in that direction
		var neighbor = grid.get_neighbor(current, flow_dir)
		if not neighbor:
			break

		# Record this segment (don't add to cell yet)
		river_cells.append({"cell": current, "direction": flow_dir})

		# Check if neighbor already has a river (merge point)
		if not neighbor.river_directions.is_empty():
			break

		# Check if we reached water
		if neighbor.elevation < HexMetrics.SEA_LEVEL:
			break

		# Move to next cell
		current = neighbor

		# Safety limit
		if river_cells.size() > 100:
			break

	# Check minimum length - discard if too short
	if river_cells.size() < MIN_RIVER_LENGTH:
		return 0  # River too short, don't create it

	# River is long enough - actually add the segments to cells
	for entry in river_cells:
		var cell: HexCell = entry["cell"]
		var direction: int = entry["direction"]
		cell.river_directions.append(direction)
		cell.has_river = true

	return river_cells.size()


## Find the best direction for water to flow from a cell.
## ONLY allows strictly downhill flow (no flat terrain).
## Prefers steepest descent, with randomness for variety.
func _find_flow_direction(cell: HexCell) -> int:
	var candidates: Array[Dictionary] = []

	for dir in range(6):
		var neighbor = grid.get_neighbor(cell, dir)
		if not neighbor:
			continue

		# Calculate elevation difference (positive = downhill)
		var elevation_diff = cell.elevation - neighbor.elevation

		# ONLY allow strictly downhill (elevation must decrease)
		if elevation_diff <= 0:
			continue

		# Weight based on steepness - steeper = more likely
		var weight = 1.0 + elevation_diff * 3.0

		candidates.append({"dir": dir, "weight": weight})

	if candidates.is_empty():
		return -1

	# Weighted random selection
	var total_weight: float = 0.0
	for c in candidates:
		total_weight += c["weight"]

	var pick = _rng.randf() * total_weight

	for candidate in candidates:
		pick -= candidate["weight"]
		if pick <= 0:
			return candidate["dir"]

	return candidates[candidates.size() - 1]["dir"]


## Check if cell is adjacent to water.
func _is_adjacent_to_water(cell: HexCell) -> bool:
	for dir in range(6):
		var neighbor = grid.get_neighbor(cell, dir)
		if neighbor and neighbor.elevation < HexMetrics.SEA_LEVEL:
			return true
	return false


## Check if cell is adjacent to an existing river.
func _is_adjacent_to_river(cell: HexCell) -> bool:
	for dir in range(6):
		var neighbor = grid.get_neighbor(cell, dir)
		if neighbor and not neighbor.river_directions.is_empty():
			return true
	return false
