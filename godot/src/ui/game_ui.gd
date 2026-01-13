class_name GameUI
extends Control
## Main game UI panel with map controls and info display
## Matches the lil-gui panel from web version

signal regenerate_requested(width: int, height: int, seed_val: int)
signal random_seed_requested
signal end_turn_requested
signal spawn_land_requested(count: int)
signal spawn_naval_requested(count: int)
signal clear_units_requested
signal noise_param_changed(param: String, value: float)

@onready var panel: PanelContainer = $Panel
@onready var map_width_spin: SpinBox = $Panel/VBox/MapSection/GridContainer/WidthSpin
@onready var map_height_spin: SpinBox = $Panel/VBox/MapSection/GridContainer/HeightSpin
@onready var seed_spin: SpinBox = $Panel/VBox/MapSection/GridContainer/SeedSpin
@onready var fps_label: Label = $Panel/VBox/InfoSection/FPSLabel
@onready var cell_count_label: Label = $Panel/VBox/InfoSection/CellCountLabel
@onready var hovered_label: Label = $Panel/VBox/InfoSection/HoveredLabel
@onready var controls_label: Label = $Panel/VBox/ControlsSection/ControlsLabel

# Render stats (added dynamically)
var draw_calls_label: Label
var triangles_label: Label

# Turn system UI elements (added dynamically)
var turn_section: VBoxContainer
var turn_label: Label
var end_turn_button: Button

# Units section UI elements
var units_section: VBoxContainer
var unit_count_label: Label

# Noise/Terrain section UI elements
var terrain_section: VBoxContainer
var noise_scale_slider: HSlider
var octaves_slider: HSlider
var persistence_slider: HSlider
var lacunarity_slider: HSlider
var sea_level_slider: HSlider
var mountain_level_slider: HSlider
var river_slider: HSlider

# River settings section
var river_section: VBoxContainer
var flow_speed_slider: HSlider

var main_node: Node3D


func _ready() -> void:
	# Set default values
	map_width_spin.value = 32
	map_height_spin.value = 32
	seed_spin.value = randi() % 100000

	# Setup controls help text
	controls_label.text = _get_controls_text()

	# Create dynamic UI sections
	_create_units_section()
	_create_terrain_section()
	_create_river_section()
	_create_turn_section()
	_create_render_stats()


func _process(_delta: float) -> void:
	# Update FPS
	fps_label.text = "FPS: %d" % Engine.get_frames_per_second()

	# Update render stats
	if draw_calls_label:
		var draw_calls = Performance.get_monitor(Performance.RENDER_TOTAL_DRAW_CALLS_IN_FRAME)
		draw_calls_label.text = "Draw Calls: %d" % draw_calls
	if triangles_label:
		var primitives = Performance.get_monitor(Performance.RENDER_TOTAL_PRIMITIVES_IN_FRAME)
		triangles_label.text = "Triangles: %d" % primitives


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


func _create_turn_section() -> void:
	# Find the VBox in the panel
	var vbox = $Panel/VBox

	# Create turn section container
	turn_section = VBoxContainer.new()
	turn_section.name = "TurnSection"

	# Add separator
	var separator = HSeparator.new()
	turn_section.add_child(separator)

	# Add header label
	var header = Label.new()
	header.text = "Turn Info"
	header.add_theme_font_size_override("font_size", 14)
	turn_section.add_child(header)

	# Add turn status label
	turn_label = Label.new()
	turn_label.text = "Turn 1 - Player (movement)"
	turn_section.add_child(turn_label)

	# Add End Turn button
	end_turn_button = Button.new()
	end_turn_button.text = "End Turn"
	end_turn_button.pressed.connect(_on_end_turn_pressed)
	turn_section.add_child(end_turn_button)

	# Insert before controls section
	var controls_section = vbox.get_node("ControlsSection")
	var controls_idx = controls_section.get_index()
	vbox.add_child(turn_section)
	vbox.move_child(turn_section, controls_idx)


func _on_end_turn_pressed() -> void:
	end_turn_requested.emit()


func set_turn_status(status: String) -> void:
	if turn_label:
		turn_label.text = status


func _create_units_section() -> void:
	# Find the VBox in the panel
	var vbox = $Panel/VBox

	# Create units section container
	units_section = VBoxContainer.new()
	units_section.name = "UnitsSection"

	# Add separator
	var separator = HSeparator.new()
	units_section.add_child(separator)

	# Add header label
	var header = Label.new()
	header.text = "Units"
	header.add_theme_font_size_override("font_size", 14)
	units_section.add_child(header)

	# Add unit count label
	unit_count_label = Label.new()
	unit_count_label.text = "Land: 0  Naval: 0"
	units_section.add_child(unit_count_label)

	# Add spawn buttons container
	var spawn_hbox = HBoxContainer.new()
	spawn_hbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL

	var spawn_land_btn = Button.new()
	spawn_land_btn.text = "+10 Land"
	spawn_land_btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	spawn_land_btn.pressed.connect(_on_spawn_land_pressed)
	spawn_hbox.add_child(spawn_land_btn)

	var spawn_naval_btn = Button.new()
	spawn_naval_btn.text = "+10 Naval"
	spawn_naval_btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	spawn_naval_btn.pressed.connect(_on_spawn_naval_pressed)
	spawn_hbox.add_child(spawn_naval_btn)

	units_section.add_child(spawn_hbox)

	# Add clear button
	var clear_btn = Button.new()
	clear_btn.text = "Clear Units"
	clear_btn.pressed.connect(_on_clear_units_pressed)
	units_section.add_child(clear_btn)

	# Insert after MapSection
	var map_section = vbox.get_node("MapSection")
	var map_idx = map_section.get_index()
	vbox.add_child(units_section)
	vbox.move_child(units_section, map_idx + 1)


func _on_spawn_land_pressed() -> void:
	spawn_land_requested.emit(10)


func _on_spawn_naval_pressed() -> void:
	spawn_naval_requested.emit(5)


func _on_clear_units_pressed() -> void:
	clear_units_requested.emit()


func set_unit_counts(land: int, naval: int) -> void:
	if unit_count_label:
		unit_count_label.text = "Land: %d  Naval: %d" % [land, naval]


func _create_terrain_section() -> void:
	# Find the VBox in the panel
	var vbox = $Panel/VBox

	# Create terrain section container
	terrain_section = VBoxContainer.new()
	terrain_section.name = "TerrainSection"

	# Add separator
	var separator = HSeparator.new()
	terrain_section.add_child(separator)

	# Add header label
	var header = Label.new()
	header.text = "Terrain Generation"
	header.add_theme_font_size_override("font_size", 14)
	terrain_section.add_child(header)

	# Noise Scale slider (0.01 - 0.1)
	terrain_section.add_child(_create_labeled_slider("Scale", 0.01, 0.1, 0.02, 0.001, "_on_noise_scale_changed"))
	noise_scale_slider = terrain_section.get_child(terrain_section.get_child_count() - 1).get_child(1)

	# Octaves slider (1 - 8)
	terrain_section.add_child(_create_labeled_slider("Octaves", 1, 8, 4, 1, "_on_octaves_changed"))
	octaves_slider = terrain_section.get_child(terrain_section.get_child_count() - 1).get_child(1)

	# Persistence slider (0.1 - 0.9)
	terrain_section.add_child(_create_labeled_slider("Persist", 0.1, 0.9, 0.5, 0.05, "_on_persistence_changed"))
	persistence_slider = terrain_section.get_child(terrain_section.get_child_count() - 1).get_child(1)

	# Lacunarity slider (1.5 - 3.0)
	terrain_section.add_child(_create_labeled_slider("Lacunar", 1.5, 3.0, 2.0, 0.1, "_on_lacunarity_changed"))
	lacunarity_slider = terrain_section.get_child(terrain_section.get_child_count() - 1).get_child(1)

	# Sea Level slider (0.0 - 0.8)
	terrain_section.add_child(_create_labeled_slider("Sea Level", 0.0, 0.8, 0.35, 0.01, "_on_sea_level_changed"))
	sea_level_slider = terrain_section.get_child(terrain_section.get_child_count() - 1).get_child(1)

	# Mountain Level slider (0.5 - 1.0)
	terrain_section.add_child(_create_labeled_slider("Mountains", 0.5, 1.0, 0.75, 0.01, "_on_mountain_level_changed"))
	mountain_level_slider = terrain_section.get_child(terrain_section.get_child_count() - 1).get_child(1)

	# River percentage slider (0.0 - 0.3)
	terrain_section.add_child(_create_labeled_slider("Rivers", 0.0, 0.3, 0.1, 0.01, "_on_river_changed"))
	river_slider = terrain_section.get_child(terrain_section.get_child_count() - 1).get_child(1)

	# Insert after UnitsSection
	var units_idx = units_section.get_index()
	vbox.add_child(terrain_section)
	vbox.move_child(terrain_section, units_idx + 1)


func _create_labeled_slider(label_text: String, min_val: float, max_val: float, default_val: float, step: float, callback: String) -> HBoxContainer:
	var hbox = HBoxContainer.new()
	hbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL

	var label = Label.new()
	label.text = label_text
	label.custom_minimum_size.x = 70
	hbox.add_child(label)

	var slider = HSlider.new()
	slider.min_value = min_val
	slider.max_value = max_val
	slider.value = default_val
	slider.step = step
	slider.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	slider.value_changed.connect(Callable(self, callback))
	hbox.add_child(slider)

	var value_label = Label.new()
	value_label.text = "%.2f" % default_val if step < 1 else "%d" % int(default_val)
	value_label.custom_minimum_size.x = 40
	value_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	hbox.add_child(value_label)

	return hbox


func _on_noise_scale_changed(value: float) -> void:
	_update_slider_label(noise_scale_slider, value)
	noise_param_changed.emit("noise_scale", value)


func _on_octaves_changed(value: float) -> void:
	_update_slider_label(octaves_slider, value, true)
	noise_param_changed.emit("octaves", value)


func _on_sea_level_changed(value: float) -> void:
	_update_slider_label(sea_level_slider, value)
	noise_param_changed.emit("sea_level", value)


func _on_mountain_level_changed(value: float) -> void:
	_update_slider_label(mountain_level_slider, value)
	noise_param_changed.emit("mountain_level", value)


func _on_river_changed(value: float) -> void:
	_update_slider_label(river_slider, value)
	noise_param_changed.emit("river_percentage", value)


func _on_persistence_changed(value: float) -> void:
	_update_slider_label(persistence_slider, value)
	noise_param_changed.emit("persistence", value)


func _on_lacunarity_changed(value: float) -> void:
	_update_slider_label(lacunarity_slider, value)
	noise_param_changed.emit("lacunarity", value)


func _on_flow_speed_changed(value: float) -> void:
	_update_slider_label(flow_speed_slider, value)
	noise_param_changed.emit("flow_speed", value)


func _update_slider_label(slider: HSlider, value: float, is_int: bool = false) -> void:
	if slider and slider.get_parent():
		var parent = slider.get_parent()
		if parent.get_child_count() > 2:
			var label = parent.get_child(2) as Label
			if label:
				label.text = "%d" % int(value) if is_int else "%.2f" % value


func _create_render_stats() -> void:
	# Add render stats to InfoSection
	var info_section = $Panel/VBox/InfoSection

	draw_calls_label = Label.new()
	draw_calls_label.text = "Draw Calls: 0"
	info_section.add_child(draw_calls_label)

	triangles_label = Label.new()
	triangles_label.text = "Triangles: 0"
	info_section.add_child(triangles_label)


func _create_river_section() -> void:
	# Find the VBox in the panel
	var vbox = $Panel/VBox

	# Create river settings section
	river_section = VBoxContainer.new()
	river_section.name = "RiverSection"

	# Add separator
	var separator = HSeparator.new()
	river_section.add_child(separator)

	# Add header label
	var header = Label.new()
	header.text = "River Settings"
	header.add_theme_font_size_override("font_size", 14)
	river_section.add_child(header)

	# Flow Speed slider (0.5 - 3.0)
	river_section.add_child(_create_labeled_slider("Flow Spd", 0.5, 3.0, 1.5, 0.1, "_on_flow_speed_changed"))
	flow_speed_slider = river_section.get_child(river_section.get_child_count() - 1).get_child(1)

	# Insert after TerrainSection
	var terrain_idx = terrain_section.get_index()
	vbox.add_child(river_section)
	vbox.move_child(river_section, terrain_idx + 1)
