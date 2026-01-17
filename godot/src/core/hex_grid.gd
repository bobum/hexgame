class_name HexGrid
extends RefCounted
## Manages the hex grid data structure
## Matches web/src/core/HexGrid.ts

var width: int = 0
var height: int = 0
var cells: Dictionary = {}  # Key: Vector2i(q, r) -> HexCell


func _init(w: int = 64, h: int = 64) -> void:
	width = w
	height = h


## Initialize grid with empty cells
func initialize() -> void:
	cells.clear()
	for r in range(height):
		for q in range(width):
			var cell = HexCell.new()
			cell.q = q
			cell.r = r
			cell.elevation = 0
			cell.terrain_type = TerrainType.Type.PLAINS
			cells[Vector2i(q, r)] = cell


## Get cell at coordinates
func get_cell(q: int, r: int) -> HexCell:
	var key = Vector2i(q, r)
	return cells.get(key)


## Set cell at coordinates
func set_cell(q: int, r: int, cell: HexCell) -> void:
	cells[Vector2i(q, r)] = cell


## Get neighbor of a cell in given direction
func get_neighbor(cell: HexCell, direction: int) -> HexCell:
	var offset = HexDirection.get_offset(direction)
	return get_cell(cell.q + offset.x, cell.r + offset.y)


## Get all valid neighbors of a cell
func get_neighbors(cell: HexCell) -> Array[HexCell]:
	var neighbors: Array[HexCell] = []
	for dir in range(6):
		var neighbor = get_neighbor(cell, dir)
		if neighbor != null:
			neighbors.append(neighbor)
	return neighbors


## Check if coordinates are within bounds
func is_valid(q: int, r: int) -> bool:
	return q >= 0 and q < width and r >= 0 and r < height


## Get all cells as array
func get_all_cells() -> Array[HexCell]:
	var result: Array[HexCell] = []
	for cell in cells.values():
		result.append(cell)
	return result


