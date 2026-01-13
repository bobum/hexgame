class_name PriorityQueue
extends RefCounted
## Bucket-based priority queue optimized for A* pathfinding.
## Uses integer buckets for fast O(1) enqueue and amortized O(1) dequeue.
## Matches web/src/pathfinding/PriorityQueue.ts

var buckets: Dictionary = {}  # int -> Array
var min_priority: float = INF
var _size: int = 0
var precision: int = 10  # Multiplier for priorities to handle decimals


func _init(p_precision: int = 10) -> void:
	precision = p_precision


## Add an item with the given priority (lower = higher priority)
func enqueue(item: Variant, priority: float) -> void:
	var bucket_key = int(floor(priority * precision))

	if not buckets.has(bucket_key):
		buckets[bucket_key] = []

	buckets[bucket_key].append(item)

	if bucket_key < min_priority:
		min_priority = bucket_key

	_size += 1


## Remove and return the item with lowest priority
func dequeue() -> Variant:
	if _size == 0:
		return null

	# Find the minimum bucket with items
	while min_priority < INF:
		if buckets.has(min_priority) and buckets[min_priority].size() > 0:
			_size -= 1
			return buckets[min_priority].pop_front()

		# Bucket is empty, clean it up and find next
		buckets.erase(min_priority)
		min_priority = _find_min_priority()

	return null


## Peek at the item with lowest priority without removing it
func peek() -> Variant:
	if _size == 0:
		return null

	if buckets.has(min_priority) and buckets[min_priority].size() > 0:
		return buckets[min_priority][0]

	return null


## Check if the queue is empty
func is_empty() -> bool:
	return _size == 0


## Clear all items from the queue
func clear() -> void:
	buckets.clear()
	min_priority = INF
	_size = 0


## Get the number of items in the queue
func get_size() -> int:
	return _size


## Find the minimum priority bucket key
func _find_min_priority() -> float:
	var min_val = INF
	for key in buckets.keys():
		if key < min_val:
			min_val = key
	return min_val
