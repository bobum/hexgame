class_name GameUI
extends Control
## Main game UI panel with map controls and info display
## Matches the lil-gui panel from web version with collapsible folders

signal regenerate_requested(width: int, height: int, seed_val: int)
signal random_seed_requested
signal end_turn_requested
signal spawn_land_requested(count: int)
signal spawn_naval_requested(count: int)
signal spawn_ai_requested(land: int, naval: int)
signal clear_units_requested
signal noise_param_changed(param: String, value: float)

@onready var panel: PanelContainer = $Panel
@onready var scroll: ScrollContainer = $Panel/ScrollContainer
@onready var main_vbox: VBoxContainer = $Panel/ScrollContainer/VBox


# Input controls
var map_width_spin: SpinBox
var map_height_spin: SpinBox
var seed_spin: SpinBox

# Dynamic labels
var fps_label: Label
var cell_count_label: Label
var hovered_label: Label
var draw_calls_label: Label
var triangles_label: Label
var turn_label: Label
var unit_count_label: Label

# Sliders
var noise_scale_slider: HSlider
var octaves_slider: HSlider
var persistence_slider: HSlider
var lacunarity_slider: HSlider
var sea_level_slider: HSlider
var mountain_level_slider: HSlider
var river_slider: HSlider
var flow_speed_slider: HSlider

var main_node: Node3D


func _ready() -> void:
	_build_ui()


func _process(_delta: float) -> void:
	# Update FPS
	if fps_label:
		fps_label.text = "FPS: %d" % Engine.get_frames_per_second()

	# Update render stats
	if draw_calls_label:
		var draw_calls = Performance.get_monitor(Performance.RENDER_TOTAL_DRAW_CALLS_IN_FRAME)
		draw_calls_label.text = "Draw Calls: %d" % draw_calls
	if triangles_label:
		var primitives = Performance.get_monitor(Performance.RENDER_TOTAL_PRIMITIVES_IN_FRAME)
		triangles_label.text = "Triangles: %d" % primitives


func _build_ui() -> void:
	# Title with instance ID for debugging
	var instance_id = "%04X" % (randi() % 0xFFFF)
	var title = Label.new()
	title.text = "HexGame [%s]" % instance_id
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_font_size_override("font_size", 16)
	main_vbox.add_child(title)

	# Create collapsible folders
	_create_map_folder()
	_create_terrain_folder()
	_create_rivers_folder()
	_create_units_folder()
	_create_turn_folder()
	_create_info_folder()
	_create_controls_folder()


func _create_folder(title: String, open: bool = true) -> VBoxContainer:
	var container = VBoxContainer.new()
	container.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	main_vbox.add_child(container)

	# Content container (indented)
	var content = VBoxContainer.new()
	content.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	var margin = MarginContainer.new()
	margin.name = title.replace(" ", "") + "Margin"
	margin.add_theme_constant_override("margin_left", 12)
	margin.add_child(content)

	# Header button with arrow
	var header = Button.new()
	header.text = ("▼ " if open else "▶ ") + title
	header.alignment = HORIZONTAL_ALIGNMENT_LEFT
	header.flat = true
	header.add_theme_font_size_override("font_size", 14)
	header.pressed.connect(func(): _toggle_folder(header, margin, title))
	container.add_child(header)

	container.add_child(margin)
	margin.visible = open

	# Separator
	var sep = HSeparator.new()
	container.add_child(sep)

	return content


func _toggle_folder(header: Button, margin: MarginContainer, title: String) -> void:
	margin.visible = not margin.visible
	var arrow = "▼ " if margin.visible else "▶ "
	header.text = arrow + title


func _create_map_folder() -> void:
	var content = _create_folder("Map Generation", true)

	# Grid for width/height/seed
	var grid = GridContainer.new()
	grid.columns = 2
	grid.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	content.add_child(grid)

	# Width
	var width_label = Label.new()
	width_label.text = "Width:"
	grid.add_child(width_label)
	map_width_spin = SpinBox.new()
	map_width_spin.min_value = 10
	map_width_spin.max_value = 80
	map_width_spin.value = 32
	map_width_spin.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	grid.add_child(map_width_spin)

	# Height
	var height_label = Label.new()
	height_label.text = "Height:"
	grid.add_child(height_label)
	map_height_spin = SpinBox.new()
	map_height_spin.min_value = 10
	map_height_spin.max_value = 60
	map_height_spin.value = 32
	map_height_spin.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	grid.add_child(map_height_spin)

	# Seed
	var seed_label = Label.new()
	seed_label.text = "Seed:"
	grid.add_child(seed_label)
	seed_spin = SpinBox.new()
	seed_spin.min_value = 1
	seed_spin.max_value = 99999
	seed_spin.value = randi() % 100000
	seed_spin.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	grid.add_child(seed_spin)

	# Buttons
	var btn_hbox = HBoxContainer.new()
	btn_hbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	content.add_child(btn_hbox)

	var regen_btn = Button.new()
	regen_btn.text = "Regenerate"
	regen_btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	regen_btn.pressed.connect(_on_regenerate_pressed)
	btn_hbox.add_child(regen_btn)

	var random_btn = Button.new()
	random_btn.text = "Random"
	random_btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	random_btn.pressed.connect(_on_random_seed_pressed)
	btn_hbox.add_child(random_btn)


func _create_terrain_folder() -> void:
	var content = _create_folder("Terrain", true)

	# Noise Scale (0.01 - 0.1)
	noise_scale_slider = _add_slider(content, "Scale", 0.01, 0.1, 0.02, 0.001, _on_noise_scale_changed)

	# Octaves (1 - 8)
	octaves_slider = _add_slider(content, "Octaves", 1, 8, 4, 1, _on_octaves_changed)

	# Persistence (0.1 - 0.9)
	persistence_slider = _add_slider(content, "Persist", 0.1, 0.9, 0.5, 0.05, _on_persistence_changed)

	# Lacunarity (1.5 - 3.0)
	lacunarity_slider = _add_slider(content, "Lacunar", 1.5, 3.0, 2.0, 0.1, _on_lacunarity_changed)

	# Sea Level (0.0 - 0.8)
	sea_level_slider = _add_slider(content, "Sea Level", 0.0, 0.8, 0.35, 0.01, _on_sea_level_changed)

	# Mountain Level (0.5 - 1.0)
	mountain_level_slider = _add_slider(content, "Mountains", 0.5, 1.0, 0.75, 0.01, _on_mountain_level_changed)


func _create_rivers_folder() -> void:
	var content = _create_folder("Rivers", true)

	# River percentage (0.0 - 0.3)
	river_slider = _add_slider(content, "Density", 0.0, 0.3, 0.1, 0.01, _on_river_changed)

	# Flow Speed (0.5 - 3.0)
	flow_speed_slider = _add_slider(content, "Flow Speed", 0.5, 3.0, 1.5, 0.1, _on_flow_speed_changed)


func _create_units_folder() -> void:
	var content = _create_folder("Units", true)

	# Unit count label
	unit_count_label = Label.new()
	unit_count_label.text = "Land: 0  Naval: 0"
	content.add_child(unit_count_label)

	# Spawn buttons
	var spawn_hbox = HBoxContainer.new()
	spawn_hbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	content.add_child(spawn_hbox)

	var spawn_land_btn = Button.new()
	spawn_land_btn.text = "+10 Land"
	spawn_land_btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	spawn_land_btn.pressed.connect(_on_spawn_land_pressed)
	spawn_hbox.add_child(spawn_land_btn)

	var spawn_naval_btn = Button.new()
	spawn_naval_btn.text = "+5 Naval"
	spawn_naval_btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	spawn_naval_btn.pressed.connect(_on_spawn_naval_pressed)
	spawn_hbox.add_child(spawn_naval_btn)

	# AI spawn button
	var spawn_ai_btn = Button.new()
	spawn_ai_btn.text = "Spawn 10 AI"
	spawn_ai_btn.pressed.connect(_on_spawn_ai_pressed)
	content.add_child(spawn_ai_btn)

	# Clear button
	var clear_btn = Button.new()
	clear_btn.text = "Clear Units"
	clear_btn.pressed.connect(_on_clear_units_pressed)
	content.add_child(clear_btn)


func _create_turn_folder() -> void:
	var content = _create_folder("Turn", true)

	# Turn status label
	turn_label = Label.new()
	turn_label.text = "Turn 1 - Player (movement)"
	content.add_child(turn_label)

	# End Turn button
	var end_turn_btn = Button.new()
	end_turn_btn.text = "End Turn"
	end_turn_btn.pressed.connect(_on_end_turn_pressed)
	content.add_child(end_turn_btn)


func _create_info_folder() -> void:
	var content = _create_folder("Info", true)

	fps_label = Label.new()
	fps_label.text = "FPS: 60"
	content.add_child(fps_label)

	cell_count_label = Label.new()
	cell_count_label.text = "Cells: 1024"
	content.add_child(cell_count_label)

	hovered_label = Label.new()
	hovered_label.text = "Hovered: None"
	content.add_child(hovered_label)

	draw_calls_label = Label.new()
	draw_calls_label.text = "Draw Calls: 0"
	content.add_child(draw_calls_label)

	triangles_label = Label.new()
	triangles_label.text = "Triangles: 0"
	content.add_child(triangles_label)


func _create_controls_folder() -> void:
	var content = _create_folder("Controls", false)

	var controls_label = Label.new()
	controls_label.text = _get_controls_text()
	controls_label.add_theme_font_size_override("font_size", 11)
	content.add_child(controls_label)


func _add_slider(parent: VBoxContainer, label_text: String, min_val: float, max_val: float, default_val: float, step: float, callback: Callable) -> HSlider:
	var hbox = HBoxContainer.new()
	hbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	parent.add_child(hbox)

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
	slider.value_changed.connect(callback)
	hbox.add_child(slider)

	var value_label = Label.new()
	value_label.text = "%d" % int(default_val) if step >= 1 else "%.2f" % default_val
	value_label.custom_minimum_size.x = 40
	value_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	hbox.add_child(value_label)

	return slider


func set_main_node(node: Node3D) -> void:
	main_node = node
	_update_cell_count()


func set_seed(seed_val: int) -> void:
	if seed_spin:
		seed_spin.value = seed_val % 100000


func set_hovered_hex(q: int, r: int, terrain: String) -> void:
	if hovered_label:
		hovered_label.text = "Hovered: (%d, %d) %s" % [q, r, terrain]


func clear_hovered_hex() -> void:
	if hovered_label:
		hovered_label.text = "Hovered: None"


func set_turn_status(status: String) -> void:
	if turn_label:
		turn_label.text = status


func set_unit_counts(land: int, naval: int) -> void:
	if unit_count_label:
		unit_count_label.text = "Land: %d  Naval: %d" % [land, naval]


func _update_cell_count() -> void:
	if main_node and cell_count_label:
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


func _on_end_turn_pressed() -> void:
	end_turn_requested.emit()


func _on_spawn_land_pressed() -> void:
	spawn_land_requested.emit(10)


func _on_spawn_naval_pressed() -> void:
	spawn_naval_requested.emit(5)


func _on_spawn_ai_pressed() -> void:
	spawn_ai_requested.emit(5, 5)


func _on_clear_units_pressed() -> void:
	clear_units_requested.emit()


func _on_noise_scale_changed(value: float) -> void:
	_update_slider_label(noise_scale_slider, value)
	noise_param_changed.emit("noise_scale", value)


func _on_octaves_changed(value: float) -> void:
	_update_slider_label(octaves_slider, value, true)
	noise_param_changed.emit("octaves", value)


func _on_persistence_changed(value: float) -> void:
	_update_slider_label(persistence_slider, value)
	noise_param_changed.emit("persistence", value)


func _on_lacunarity_changed(value: float) -> void:
	_update_slider_label(lacunarity_slider, value)
	noise_param_changed.emit("lacunarity", value)


func _on_sea_level_changed(value: float) -> void:
	_update_slider_label(sea_level_slider, value)
	noise_param_changed.emit("sea_level", value)


func _on_mountain_level_changed(value: float) -> void:
	_update_slider_label(mountain_level_slider, value)
	noise_param_changed.emit("mountain_level", value)


func _on_river_changed(value: float) -> void:
	_update_slider_label(river_slider, value)
	noise_param_changed.emit("river_percentage", value)


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


func _get_controls_text() -> String:
	return """Camera:
WASD/Arrows: Pan
Q/E: Rotate
R/F: Tilt
Z/X: Up/Down
Scroll: Zoom
Middle: Pan
Right: Rotate

Selection:
Click: Select
Ctrl+Click: Add
Box: Multi-select

Map:
Space: New Map
G: Regenerate"""
