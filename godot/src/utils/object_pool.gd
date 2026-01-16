class_name ObjectPool
extends RefCounted
## Generic object pool for reusing objects instead of creating/destroying.
## Reduces garbage collection pressure for frequently used objects.
## Matches web/src/utils/ObjectPool.ts

var _available: Array = []
var _active: Dictionary = {}  # item -> true (used as Set)
var _factory: Callable
var _reset: Callable
var _max_size: int

# Statistics
var _created: int = 0
var _reused: int = 0
var _peak: int = 0


## Create an object pool.
## @param factory - Callable to create new objects: func() -> Object
## @param reset - Callable to reset an object before reuse: func(obj) -> void
## @param max_size - Maximum pool size (prevents unbounded growth)
func _init(factory: Callable, reset: Callable = Callable(), max_size: int = 1000) -> void:
	_factory = factory
	_reset = reset
	_max_size = max_size


## Acquire an object from the pool (or create new if empty).
func acquire() -> Variant:
	var obj: Variant

	if _available.size() > 0:
		obj = _available.pop_back()
		_reused += 1
	else:
		obj = _factory.call()
		_created += 1

	_active[obj] = true
	_peak = max(_peak, _active.size())

	return obj


## Release an object back to the pool.
func release(obj: Variant) -> void:
	if not _active.has(obj):
		push_warning("ObjectPool: releasing object not from this pool")
		return

	_active.erase(obj)

	if _reset.is_valid():
		_reset.call(obj)

	# Only keep up to max_size in the pool
	if _available.size() < _max_size:
		_available.append(obj)


## Release all active objects back to the pool.
func release_all() -> void:
	for obj in _active.keys():
		if _reset.is_valid():
			_reset.call(obj)
		if _available.size() < _max_size:
			_available.append(obj)
	_active.clear()


## Pre-warm the pool with objects.
func prewarm(count: int) -> void:
	for i in range(count):
		if _available.size() >= _max_size:
			break
		_available.append(_factory.call())
		_created += 1


## Clear the pool entirely.
func clear() -> void:
	_available.clear()
	_active.clear()


## Get pool statistics.
func get_stats() -> Dictionary:
	var total = _created + _reused
	return {
		"available": _available.size(),
		"active": _active.size(),
		"created": _created,
		"reused": _reused,
		"peak": _peak,
		"reuse_rate": float(_reused) / total if total > 0 else 0.0,
	}


## Get the number of active objects.
func get_active_count() -> int:
	return _active.size()


## Get the number of available objects.
func get_available_count() -> int:
	return _available.size()


## Reset statistics counters.
func reset_stats() -> void:
	_created = 0
	_reused = 0
	_peak = 0
