class_name Feature
extends RefCounted
## Represents a decorative feature (tree, rock) on a hex cell

enum Type {
	NONE,
	TREE,
	ROCK
}

var type: Type = Type.NONE
var position: Vector3 = Vector3.ZERO
var rotation: float = 0.0
var scale: float = 1.0


func _init(feature_type: Type = Type.NONE, pos: Vector3 = Vector3.ZERO, rot: float = 0.0, feature_scale: float = 1.0) -> void:
	type = feature_type
	position = pos
	rotation = rot
	scale = feature_scale
