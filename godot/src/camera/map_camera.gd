class_name MapCamera
extends Camera3D
## Orbital camera with mouse and keyboard controls
## Matches web/src/camera/MapCamera.ts

# Camera configuration
@export var min_zoom: float = 5.0
@export var max_zoom: float = 80.0
@export var min_pitch: float = 20.0
@export var max_pitch: float = 85.0
@export var pan_speed: float = 0.5
@export var rotate_speed: float = 0.3
@export var zoom_speed: float = 0.1
@export var keyboard_pan_speed: float = 20.0
@export var keyboard_rotate_speed: float = 60.0

# Orbital camera state
var target: Vector3 = Vector3.ZERO
var distance: float = 30.0
var pitch: float = 45.0  # Degrees
var yaw: float = 0.0     # Degrees

# Target values for smooth interpolation
var target_distance: float = 30.0
var target_pitch: float = 45.0
var target_yaw: float = 0.0
var target_position: Vector3 = Vector3.ZERO

# Input state
var is_panning: bool = false
var is_rotating: bool = false
var last_mouse_pos: Vector2 = Vector2.ZERO

# Smoothing factor
var smoothing: float = 10.0


func _ready() -> void:
	target_position = target
	_update_camera_position()


func _process(delta: float) -> void:
	_handle_keyboard_input(delta)
	_apply_smoothing(delta)
	_update_camera_position()


func _unhandled_input(event: InputEvent) -> void:
	# Mouse wheel zoom
	if event is InputEventMouseButton:
		var mb = event as InputEventMouseButton
		if mb.pressed:
			if mb.button_index == MOUSE_BUTTON_WHEEL_UP:
				target_distance *= 0.9
				target_distance = clamp(target_distance, min_zoom, max_zoom)
			elif mb.button_index == MOUSE_BUTTON_WHEEL_DOWN:
				target_distance *= 1.1
				target_distance = clamp(target_distance, min_zoom, max_zoom)
			# Middle mouse button - start panning
			elif mb.button_index == MOUSE_BUTTON_MIDDLE:
				is_panning = true
				last_mouse_pos = mb.position
			# Right mouse button - start rotating
			elif mb.button_index == MOUSE_BUTTON_RIGHT:
				is_rotating = true
				last_mouse_pos = mb.position
		else:
			# Button released
			if mb.button_index == MOUSE_BUTTON_MIDDLE:
				is_panning = false
			elif mb.button_index == MOUSE_BUTTON_RIGHT:
				is_rotating = false

	# Mouse motion for pan/rotate
	if event is InputEventMouseMotion:
		var mm = event as InputEventMouseMotion
		if is_panning:
			_handle_pan(mm.relative)
		if is_rotating:
			_handle_rotate(mm.relative)


func _handle_keyboard_input(delta: float) -> void:
	var move_dir = Vector3.ZERO
	var rotate_dir = 0.0
	var pitch_dir = 0.0
	var vertical_dir = 0.0

	# WASD / Arrow keys for panning
	if Input.is_key_pressed(KEY_W) or Input.is_key_pressed(KEY_UP):
		move_dir.z -= 1
	if Input.is_key_pressed(KEY_S) or Input.is_key_pressed(KEY_DOWN):
		move_dir.z += 1
	if Input.is_key_pressed(KEY_A) or Input.is_key_pressed(KEY_LEFT):
		move_dir.x -= 1
	if Input.is_key_pressed(KEY_D) or Input.is_key_pressed(KEY_RIGHT):
		move_dir.x += 1

	# Q/E for rotation
	if Input.is_key_pressed(KEY_Q):
		rotate_dir -= 1
	if Input.is_key_pressed(KEY_E):
		rotate_dir += 1

	# R/F for tilt (pitch)
	if Input.is_key_pressed(KEY_R):
		pitch_dir -= 1
	if Input.is_key_pressed(KEY_F):
		pitch_dir += 1

	# Z/X for vertical movement
	if Input.is_key_pressed(KEY_Z):
		vertical_dir += 1
	if Input.is_key_pressed(KEY_X):
		vertical_dir -= 1

	# Apply movement relative to camera yaw
	if move_dir.length() > 0:
		move_dir = move_dir.normalized()
		var yaw_rad = deg_to_rad(yaw)
		var forward = Vector3(sin(yaw_rad), 0, cos(yaw_rad))
		var right = Vector3(cos(yaw_rad), 0, -sin(yaw_rad))
		var movement = (forward * move_dir.z + right * move_dir.x) * keyboard_pan_speed * delta
		target_position += movement

	# Apply rotation
	if rotate_dir != 0:
		target_yaw += rotate_dir * keyboard_rotate_speed * delta

	# Apply pitch
	if pitch_dir != 0:
		target_pitch += pitch_dir * keyboard_rotate_speed * delta
		target_pitch = clamp(target_pitch, min_pitch, max_pitch)

	# Apply vertical movement
	if vertical_dir != 0:
		target_position.y += vertical_dir * keyboard_pan_speed * delta
		target_position.y = clamp(target_position.y, 0, 30)


func _handle_pan(delta: Vector2) -> void:
	# Pan speed scales with distance
	var scale = distance * pan_speed * 0.01
	var yaw_rad = deg_to_rad(yaw)

	# Calculate movement in world space relative to camera orientation
	var forward = Vector3(sin(yaw_rad), 0, cos(yaw_rad))
	var right = Vector3(cos(yaw_rad), 0, -sin(yaw_rad))

	target_position -= right * delta.x * scale
	target_position -= forward * delta.y * scale


func _handle_rotate(delta: Vector2) -> void:
	# Horizontal drag rotates yaw
	target_yaw -= delta.x * rotate_speed

	# Vertical drag adjusts pitch
	target_pitch += delta.y * rotate_speed
	target_pitch = clamp(target_pitch, min_pitch, max_pitch)


func _apply_smoothing(delta: float) -> void:
	var t = 1.0 - pow(0.001, delta * smoothing)

	target = target.lerp(target_position, t)
	distance = lerp(distance, target_distance, t)
	pitch = lerp(pitch, target_pitch, t)
	yaw = lerp(yaw, target_yaw, t)


func _update_camera_position() -> void:
	var pitch_rad = deg_to_rad(pitch)
	var yaw_rad = deg_to_rad(yaw)

	# Calculate camera position on orbital sphere around target
	var offset = Vector3(
		sin(yaw_rad) * cos(pitch_rad) * distance,
		sin(pitch_rad) * distance,
		cos(yaw_rad) * cos(pitch_rad) * distance
	)

	global_position = target + offset
	look_at(target, Vector3.UP)


## Set the camera target (what it looks at)
func set_target(new_target: Vector3) -> void:
	target_position = new_target
	target = new_target


## Get current target position
func get_target() -> Vector3:
	return target


## Focus on a specific world position
func focus_on(world_pos: Vector3, new_distance: float = -1) -> void:
	target_position = world_pos
	if new_distance > 0:
		target_distance = clamp(new_distance, min_zoom, max_zoom)
