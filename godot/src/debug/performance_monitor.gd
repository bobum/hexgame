class_name PerformanceMonitor
extends Control
## Real-time performance monitoring with FPS graph
## Matches web/src/utils/PerformanceMonitor.ts

# Graph dimensions
const GRAPH_WIDTH: int = 200
const GRAPH_HEIGHT: int = 60
const HISTORY_SIZE: int = 200

# Thresholds
const TARGET_FRAME_TIME: float = 16.67  # 60 fps
const WARNING_FRAME_TIME: float = 33.33  # 30 fps

# Performance data
var frame_times: Array[float] = []
var fps: float = 60.0
var avg_frame_time: float = 16.67
var max_frame_time: float = 0.0
var min_frame_time: float = 1000.0
var one_percent_low: float = 60.0  # 1% low FPS

# UI elements
var graph_rect: ColorRect
var graph_texture: TextureRect
var image: Image
var stats_label: Label
var visible_graph: bool = true


func _ready() -> void:
	_setup_ui()
	# Initialize frame time history
	for i in range(HISTORY_SIZE):
		frame_times.append(16.67)


func _setup_ui() -> void:
	# Container at bottom-left
	anchor_left = 0
	anchor_top = 1
	anchor_right = 0
	anchor_bottom = 1
	offset_left = 10
	offset_top = -80
	offset_right = GRAPH_WIDTH + 10
	offset_bottom = -10

	# Background
	graph_rect = ColorRect.new()
	graph_rect.color = Color(0, 0, 0, 0.7)
	graph_rect.size = Vector2(GRAPH_WIDTH, GRAPH_HEIGHT + 20)
	add_child(graph_rect)

	# Graph image
	image = Image.create(GRAPH_WIDTH, GRAPH_HEIGHT, false, Image.FORMAT_RGBA8)
	image.fill(Color(0, 0, 0, 0))

	var texture = ImageTexture.create_from_image(image)
	graph_texture = TextureRect.new()
	graph_texture.texture = texture
	graph_texture.position = Vector2(0, 20)
	graph_texture.size = Vector2(GRAPH_WIDTH, GRAPH_HEIGHT)
	add_child(graph_texture)

	# Stats label
	stats_label = Label.new()
	stats_label.position = Vector2(4, 2)
	stats_label.add_theme_font_size_override("font_size", 11)
	stats_label.add_theme_color_override("font_color", Color.WHITE)
	add_child(stats_label)


func _process(delta: float) -> void:
	record_frame(delta * 1000.0)  # Convert to ms
	_update_display()


func record_frame(delta_ms: float) -> void:
	# Add new frame time
	frame_times.append(delta_ms)
	if frame_times.size() > HISTORY_SIZE:
		frame_times.pop_front()

	# Calculate statistics
	var sum: float = 0.0
	max_frame_time = 0.0
	min_frame_time = 1000.0

	for t in frame_times:
		sum += t
		max_frame_time = max(max_frame_time, t)
		min_frame_time = min(min_frame_time, t)

	avg_frame_time = sum / frame_times.size()
	fps = 1000.0 / avg_frame_time if avg_frame_time > 0 else 60.0

	# Calculate 1% low (worst 1% of frames)
	var sorted_times = frame_times.duplicate()
	sorted_times.sort()
	var one_percent_index = int(sorted_times.size() * 0.99)
	var worst_one_percent = sorted_times[one_percent_index]
	one_percent_low = 1000.0 / worst_one_percent if worst_one_percent > 0 else 60.0


func _update_display() -> void:
	# Update stats label
	stats_label.text = "FPS: %d | Avg: %.1fms" % [int(fps), avg_frame_time]
	stats_label.text += "\n1%% Low: %d | Max: %.1fms" % [int(one_percent_low), max_frame_time]

	if not visible_graph:
		return

	# Clear image
	image.fill(Color(0, 0, 0, 0))

	# Draw threshold lines
	var target_y = int((1.0 - TARGET_FRAME_TIME / 50.0) * GRAPH_HEIGHT)
	var warning_y = int((1.0 - WARNING_FRAME_TIME / 50.0) * GRAPH_HEIGHT)

	# Draw dashed lines for thresholds
	for x in range(0, GRAPH_WIDTH, 4):
		if target_y >= 0 and target_y < GRAPH_HEIGHT:
			image.set_pixel(x, target_y, Color(0, 0.8, 0, 0.5))  # Green 60fps line
		if warning_y >= 0 and warning_y < GRAPH_HEIGHT:
			image.set_pixel(x, warning_y, Color(0.8, 0.8, 0, 0.5))  # Yellow 30fps line

	# Draw frame time bars
	var bar_width = 1
	for i in range(frame_times.size()):
		var t = frame_times[i]
		var x = i * GRAPH_WIDTH / HISTORY_SIZE
		var height = int(min(t / 50.0, 1.0) * GRAPH_HEIGHT)
		var y_start = GRAPH_HEIGHT - height

		# Color based on frame time
		var color: Color
		if t <= TARGET_FRAME_TIME:
			color = Color(0, 0.8, 0, 0.9)  # Green - good
		elif t <= WARNING_FRAME_TIME:
			color = Color(0.8, 0.8, 0, 0.9)  # Yellow - warning
		else:
			color = Color(0.8, 0, 0, 0.9)  # Red - bad

		# Draw vertical bar
		for y in range(y_start, GRAPH_HEIGHT):
			if x >= 0 and x < GRAPH_WIDTH and y >= 0 and y < GRAPH_HEIGHT:
				image.set_pixel(x, y, color)

	# Update texture
	var texture = ImageTexture.create_from_image(image)
	graph_texture.texture = texture


func toggle_graph() -> void:
	visible_graph = not visible_graph
	graph_texture.visible = visible_graph


func get_stats() -> Dictionary:
	return {
		"fps": fps,
		"avg_frame_time": avg_frame_time,
		"max_frame_time": max_frame_time,
		"min_frame_time": min_frame_time,
		"one_percent_low": one_percent_low
	}
