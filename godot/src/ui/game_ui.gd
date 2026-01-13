class_name GameUI
extends Control
## Main game UI panel with map controls and info display
## Matches the lil-gui panel from web version

signal regenerate_requested(width: int, height: int, seed_val: int)
signal random_seed_requested

@onready var panel: PanelContainer = $Panel
@onready var map_width_spin: SpinBox = $Panel/VBox/MapSection/GridContainer/WidthSpin
@onready var map_height_spin: SpinBox = $Panel/VBox/MapSection/GridContainer/HeightSpin
@onready var seed_spin: SpinBox = $Panel/VBox/MapSection/GridContainer/SeedSpin
@onready var fps_label: Label = $Panel/VBox/InfoSection/FPSLabel
@onready var cell_count_label: Label = $Panel/VBox/InfoSection/CellCountLabel
@onready var hovered_label: Label = $Panel/VBox/InfoSection/HoveredLabel
@onready var controls_label: Label = $Panel/VBox/ControlsSection/ControlsLabel

var main_node: Node3D


func _ready() -> void:
	# Set default values
	map_width_spin.value = 32
	map_height_spin.value = 32
	seed_spin.value = randi() % 100000

	# Setup controls help text
	controls_label.text = _get_controls_text()


func _process(_delta: float) -> void:
	# Update FPS
	fps_label.text = "FPS: %d" % Engine.get_frames_per_second()


func set_main_node(node: Node3D) -> void:
	main_node = node
	_update_cell_count()


func set_seed(seed_val: int) -> void:
	seed_spin.value = seed_val % 100000


func set_hovered_hex(q: int, r: int, terrain: String) -> void:
	hovered_label.text = "Hovered: (%d, %d) %s" % [q, r, terrain]


func clear_hovered_hex() -> void:
	hovered_label.text = "Hovered: None"


func _update_cell_count() -> void:
	if main_node and main_node.has_method("get"):
		var width = main_node.map_width if "map_width" in main_node else 32
		var height = main_node.map_height if "map_height" in main_node else 32
		cell_count_label.text = "Cells: %d" % (width * height)


func _on_regenerate_pressed() -> void:
	var width = int(map_width_spin.value)
	var height = int(map_height_spin.value)
	var seed_val = int(seed_spin.value)
	regenerate_requested.emit(width, height, seed_val)
	_update_cell_count()


func _on_random_seed_pressed() -> void:
	seed_spin.value = randi() % 100000
	random_seed_requested.emit()


func _get_controls_text() -> String:
	return """Camera Controls:
WASD/Arrows: Pan
Q/E: Rotate
R/F: Tilt
Z/X: Up/Down
Mouse Wheel: Zoom
Middle Drag: Pan
Right Drag: Rotate

Map Controls:
Space: New Map
G: Regenerate"""
