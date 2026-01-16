class_name ScreenshotCapture
extends Node
## Automatic screenshot capture for AI-assisted visual debugging
## Saves screenshots to a fixed location for external analysis

# Screenshot output path - save to project root for easy access
const SCREENSHOT_PATH = "C:/projects/hexgame/godot/debug_screenshot.png"

# Fixed camera settings for consistent screenshots - match target images
const DEBUG_CAMERA_DISTANCE: float = 30.0
const DEBUG_CAMERA_PITCH: float = 45.0
const DEBUG_CAMERA_YAW: float = 30.0

# Auto-capture settings
const AUTO_CAPTURE_DELAY: float = 2.0  # Set to -1 to disable auto-capture
const AUTO_QUIT_AFTER_CAPTURE: bool = false
const DEBUG_SEED: int = 12345  # Fixed seed for consistent screenshots

# Reference to map camera
var camera: MapCamera = null
var auto_capture_timer: float = -1.0
var has_auto_captured: bool = false

# Signals
signal screenshot_taken(path: String)
signal camera_positioned()


func _ready() -> void:
	print("[ScreenshotCapture] Ready - Auto-capture in %.1f seconds" % AUTO_CAPTURE_DELAY)
	auto_capture_timer = AUTO_CAPTURE_DELAY


func _process(delta: float) -> void:
	if auto_capture_timer > 0:
		auto_capture_timer -= delta
		if auto_capture_timer <= 0 and not has_auto_captured:
			has_auto_captured = true
			_do_auto_capture()


func _do_auto_capture() -> void:
	print("[ScreenshotCapture] Starting auto-capture sequence...")
	var main = get_parent()

	# Use fixed seed for consistent screenshots
	if main and main.has_method("regenerate_with_settings"):
		print("[ScreenshotCapture] Using fixed seed: %d" % DEBUG_SEED)
		main.regenerate_with_settings(main.map_width, main.map_height, DEBUG_SEED)
		# Wait for regeneration to complete
		await get_tree().create_timer(1.0).timeout

	# Calculate map center
	var center = Vector3.ZERO
	if main and "grid" in main and main.grid:
		var center_q = main.map_width / 2
		var center_r = main.map_height / 2
		var center_coords = HexCoordinates.new(center_q, center_r)
		center = center_coords.to_world_position(0)

	# Position camera and capture
	var path = await capture_debug_screenshot(center)

	if path != "" and AUTO_QUIT_AFTER_CAPTURE:
		print("[ScreenshotCapture] Screenshot complete, quitting...")
		await get_tree().create_timer(0.5).timeout
		get_tree().quit()


## Setup with camera reference
func setup(map_camera: MapCamera) -> void:
	camera = map_camera


## Take a screenshot and save to the fixed path
func capture_screenshot() -> String:
	# Wait for the frame to render
	await RenderingServer.frame_post_draw

	# Get the viewport image
	var viewport = get_viewport()
	var image = viewport.get_texture().get_image()

	# Save to project directory
	var error = image.save_png(SCREENSHOT_PATH)
	if error == OK:
		print("[ScreenshotCapture] Screenshot saved to: %s" % SCREENSHOT_PATH)
		screenshot_taken.emit(SCREENSHOT_PATH)
		return SCREENSHOT_PATH
	else:
		push_error("[ScreenshotCapture] Failed to save screenshot: %s" % error)
		return ""


## Position camera at a fixed debug position for consistent screenshots
func position_camera_for_debug(center_target: Vector3 = Vector3.ZERO) -> void:
	if not camera:
		push_error("[ScreenshotCapture] No camera set up")
		return

	# Set fixed camera position
	camera.set_target(center_target)
	camera.target_distance = DEBUG_CAMERA_DISTANCE
	camera.target_pitch = DEBUG_CAMERA_PITCH
	camera.target_yaw = DEBUG_CAMERA_YAW

	# Also set current values to snap immediately
	camera.distance = DEBUG_CAMERA_DISTANCE
	camera.pitch = DEBUG_CAMERA_PITCH
	camera.yaw = DEBUG_CAMERA_YAW

	print("[ScreenshotCapture] Camera positioned at debug view (dist=%.1f, pitch=%.1f, yaw=%.1f)" % [
		DEBUG_CAMERA_DISTANCE, DEBUG_CAMERA_PITCH, DEBUG_CAMERA_YAW
	])
	camera_positioned.emit()


## Position camera and take screenshot in one call
func capture_debug_screenshot(center_target: Vector3 = Vector3.ZERO) -> String:
	position_camera_for_debug(center_target)

	# Wait a couple frames for camera to update and scene to render
	await get_tree().process_frame
	await get_tree().process_frame

	return await capture_screenshot()


## Get the expected screenshot path for external tools
static func get_screenshot_path() -> String:
	return SCREENSHOT_PATH


## Check if a screenshot exists
static func screenshot_exists() -> bool:
	return FileAccess.file_exists(SCREENSHOT_PATH)


func _input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed:
		# F12 - Take screenshot with current camera position
		if event.keycode == KEY_F12:
			print("[ScreenshotCapture] Capturing screenshot...")
			capture_screenshot()
		# F11 - Position camera at debug view and take screenshot
		elif event.keycode == KEY_F11:
			print("[ScreenshotCapture] Setting debug camera and capturing...")
			# Calculate map center if we have access to it
			var main = get_parent()
			var center = Vector3.ZERO
			if main and "grid" in main and main.grid:
				var center_q = main.map_width / 2
				var center_r = main.map_height / 2
				var center_coords = HexCoordinates.new(center_q, center_r)
				center = center_coords.to_world_position(0)
			capture_debug_screenshot(center)
