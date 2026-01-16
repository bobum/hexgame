class_name SpatialHash
extends RefCounted
## Generic spatial hash grid for O(1) point lookups and efficient range queries.
## Used for finding units, features, or any spatially-located objects.
## Matches web/src/utils/SpatialHash.ts

var _buckets: Dictionary = {}  # "bx,bz" -> Array of items
var _item_positions: Dictionary = {}  # item -> {x, z}
var _cell_size: float

# Statistics
var _insert_count: int = 0
var _query_count: int = 0
var _peak_items: int = 0


## Create a spatial hash with the given cell size.
## Smaller cells = more memory but faster range queries.
## Recommended: cell size slightly larger than typical query radius.
func _init(cell_size: float = 2.0) -> void:
	_cell_size = cell_size


## Get the bucket key for a world position.
func _get_key(x: float, z: float) -> String:
	var bx = int(floor(x / _cell_size))
	var bz = int(floor(z / _cell_size))
	return "%d,%d" % [bx, bz]


## Get bucket coordinates for a world position.
func _get_bucket_coords(x: float, z: float) -> Vector2i:
	return Vector2i(int(floor(x / _cell_size)), int(floor(z / _cell_size)))


## Insert an item at the given position.
func insert(item: Variant, x: float, z: float) -> void:
	# Remove from old position if already tracked
	if _item_positions.has(item):
		remove(item)

	var key = _get_key(x, z)
	if not _buckets.has(key):
		_buckets[key] = []
	_buckets[key].append(item)
	_item_positions[item] = {"x": x, "z": z}

	_insert_count += 1
	_peak_items = max(_peak_items, _item_positions.size())


## Remove an item from the hash.
func remove(item: Variant) -> bool:
	if not _item_positions.has(item):
		return false

	var pos = _item_positions[item]
	var key = _get_key(pos["x"], pos["z"])

	if _buckets.has(key):
		var bucket = _buckets[key] as Array
		bucket.erase(item)
		if bucket.is_empty():
			_buckets.erase(key)

	_item_positions.erase(item)
	return true


## Update an item's position (remove + insert).
func update(item: Variant, x: float, z: float) -> void:
	insert(item, x, z)  # insert handles removal automatically


## Get all items at exact bucket position (O(1)).
func get_at(x: float, z: float) -> Array:
	_query_count += 1
	var key = _get_key(x, z)
	if _buckets.has(key):
		return _buckets[key].duplicate()
	return []


## Get the first item at a position, or null.
func get_first_at(x: float, z: float) -> Variant:
	_query_count += 1
	var key = _get_key(x, z)
	if _buckets.has(key):
		var bucket = _buckets[key] as Array
		if not bucket.is_empty():
			return bucket[0]
	return null


## Check if any item exists at position.
func has_at(x: float, z: float) -> bool:
	var key = _get_key(x, z)
	if _buckets.has(key):
		var bucket = _buckets[key] as Array
		return not bucket.is_empty()
	return false


## Query all items within a radius of the given point.
## Uses squared distance for efficiency.
func query_radius(x: float, z: float, radius: float) -> Array:
	_query_count += 1
	var results: Array = []
	var radius_sq = radius * radius

	# Calculate bucket range to check
	var min_b = _get_bucket_coords(x - radius, z - radius)
	var max_b = _get_bucket_coords(x + radius, z + radius)

	# Check all buckets in range
	for bx in range(min_b.x, max_b.x + 1):
		for bz in range(min_b.y, max_b.y + 1):
			var key = "%d,%d" % [bx, bz]
			if not _buckets.has(key):
				continue

			for item in _buckets[key]:
				var pos = _item_positions.get(item)
				if pos == null:
					continue

				var dx = pos["x"] - x
				var dz = pos["z"] - z
				if dx * dx + dz * dz <= radius_sq:
					results.append(item)

	return results


## Query all items within a rectangular area.
func query_rect(min_x: float, min_z: float, max_x: float, max_z: float) -> Array:
	_query_count += 1
	var results: Array = []

	var min_b = _get_bucket_coords(min_x, min_z)
	var max_b = _get_bucket_coords(max_x, max_z)

	for bx in range(min_b.x, max_b.x + 1):
		for bz in range(min_b.y, max_b.y + 1):
			var key = "%d,%d" % [bx, bz]
			if not _buckets.has(key):
				continue

			for item in _buckets[key]:
				var pos = _item_positions.get(item)
				if pos == null:
					continue

				if pos["x"] >= min_x and pos["x"] <= max_x and pos["z"] >= min_z and pos["z"] <= max_z:
					results.append(item)

	return results


## Get all items in the hash.
func get_all() -> Array:
	return _item_positions.keys()


## Get the position of an item.
func get_position(item: Variant) -> Variant:
	return _item_positions.get(item)


## Check if an item is in the hash.
func has(item: Variant) -> bool:
	return _item_positions.has(item)


## Clear all items.
func clear() -> void:
	_buckets.clear()
	_item_positions.clear()


## Get the number of items in the hash.
func get_size() -> int:
	return _item_positions.size()


## Get the number of active buckets.
func get_bucket_count() -> int:
	return _buckets.size()


## Get statistics for debugging.
func get_stats() -> Dictionary:
	return {
		"items": _item_positions.size(),
		"buckets": _buckets.size(),
		"insert_count": _insert_count,
		"query_count": _query_count,
		"peak_items": _peak_items,
		"cell_size": _cell_size,
	}


## Reset statistics counters.
func reset_stats() -> void:
	_insert_count = 0
	_query_count = 0
