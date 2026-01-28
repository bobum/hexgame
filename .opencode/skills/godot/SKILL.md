---
name: godot
description: Comprehensive reference for Godot Engine 4.x development including GDScript syntax, nodes, signals, scenes, 2D/3D development, UI, physics, animation, and best practices.
license: MIT
compatibility: opencode
metadata:
  version: "1.0"
  godot-version: "4.x"
---

# Godot Engine Development

Comprehensive reference for Godot Engine 4.x development with GDScript and C#.

## Usage

```
/godot [topic]
```

Topics: `gdscript`, `nodes`, `signals`, `scenes`, `2d`, `3d`, `ui`, `physics`, `animation`, `resources`, `style`

---

## GDScript Fundamentals

### Variable Declaration

```gdscript
# Dynamic typing
var health
var player_name = "Hero"

# Static typing (recommended - 2x faster)
var health: int = 100
var speed: float = 5.0
var is_alive: bool = true
var display_name: String = "Player"

# Type inference with :=
var damage := 25  # Inferred as int
var velocity := Vector2.ZERO  # Inferred as Vector2

# Constants
const MAX_HEALTH := 100
const GRAVITY := 980.0
const DIRECTIONS := [Vector2.UP, Vector2.DOWN, Vector2.LEFT, Vector2.RIGHT]

# Enums
enum State { IDLE, WALKING, RUNNING, JUMPING }
enum { NORTH, EAST, SOUTH, WEST }  # Anonymous enum
```

### Data Types

| Type | Description | Example |
|------|-------------|---------|
| `int` | Integer | `42`, `-10`, `0xFF` |
| `float` | Floating point | `3.14`, `1e6` |
| `bool` | Boolean | `true`, `false` |
| `String` | Text | `"Hello"`, `'World'` |
| `Vector2` | 2D vector | `Vector2(10, 20)` |
| `Vector3` | 3D vector | `Vector3(1, 2, 3)` |
| `Array` | Dynamic array | `[1, "two", 3.0]` |
| `Dictionary` | Key-value map | `{"key": "value"}` |
| `Color` | RGBA color | `Color.RED`, `Color(1, 0, 0)` |
| `NodePath` | Path to node | `^"../Player"` |
| `StringName` | Interned string | `&"action_name"` |

### Typed Collections

```gdscript
var inventory: Array[Item] = []
var scores: Array[int] = [100, 200, 300]
var settings: Dictionary[String, bool] = {"sound": true, "music": false}
```

### Functions

```gdscript
# Basic function
func take_damage(amount: int) -> void:
    health -= amount
    if health <= 0:
        die()

# Function with return value
func calculate_damage(base: int, multiplier: float = 1.0) -> int:
    return int(base * multiplier)

# Static function (callable without instance)
static func clamp_health(value: int) -> int:
    return clampi(value, 0, MAX_HEALTH)

# Lambda/anonymous function
var double := func(x: int) -> int: return x * 2
```

### Control Flow

```gdscript
# Conditionals
if health <= 0:
    die()
elif health < 25:
    show_warning()
else:
    continue_playing()

# Ternary
var status := "alive" if health > 0 else "dead"

# Match (pattern matching)
match state:
    State.IDLE:
        play_idle_animation()
    State.WALKING, State.RUNNING:
        play_move_animation()
    _:
        pass  # Default case

# Loops
for i in range(10):
    print(i)

for item in inventory:
    item.use()

for key in dictionary:
    print(key, dictionary[key])

while is_running:
    process_frame()

# Loop control
for i in range(100):
    if i == 50:
        break
    if i % 2 == 0:
        continue
    print(i)
```

### Classes and Inheritance

```gdscript
# File: player.gd
class_name Player
extends CharacterBody2D

# Exported properties (visible in Inspector)
@export var max_health: int = 100
@export var speed: float = 200.0
@export_range(0, 100) var armor: int = 0
@export_file("*.tscn") var death_scene: String
@export_group("Combat")
@export var damage: int = 10

# Signals
signal health_changed(new_health: int)
signal died

# Private convention (underscore prefix)
var _current_health: int

# Called when node enters scene tree
func _ready() -> void:
    _current_health = max_health

# Called every frame
func _process(delta: float) -> void:
    pass

# Called at fixed physics rate (default 60/sec)
func _physics_process(delta: float) -> void:
    move_and_slide()

# Handle input events
func _input(event: InputEvent) -> void:
    if event.is_action_pressed("jump"):
        jump()

# Unhandled input (after _input and GUI)
func _unhandled_input(event: InputEvent) -> void:
    pass
```

### Inner Classes

```gdscript
class_name Inventory

class Slot:
    var item: Item
    var count: int = 0

    func is_empty() -> bool:
        return item == null or count <= 0

var slots: Array[Slot] = []
```

---

## Signals

### Defining and Emitting

```gdscript
# Define signal
signal health_changed(old_value: int, new_value: int)
signal died
signal item_collected(item: Item, position: Vector2)

# Emit signal
func take_damage(amount: int) -> void:
    var old_health := health
    health -= amount
    health_changed.emit(old_health, health)

    if health <= 0:
        died.emit()
```

### Connecting Signals

```gdscript
# In code
func _ready() -> void:
    # Connect to method
    health_changed.connect(_on_health_changed)

    # Connect with bind (extra arguments)
    button.pressed.connect(_on_button_pressed.bind("save"))

    # One-shot connection (disconnects after first emission)
    died.connect(_on_died, CONNECT_ONE_SHOT)

    # Deferred connection (calls at end of frame)
    ready.connect(_on_ready, CONNECT_DEFERRED)

func _on_health_changed(old_val: int, new_val: int) -> void:
    update_health_bar()

# Lambda connection
button.pressed.connect(func(): print("Button pressed!"))

# Disconnect
health_changed.disconnect(_on_health_changed)
```

### Built-in Signals

```gdscript
# Node signals
tree_entered  # When added to scene tree
tree_exiting  # When about to leave scene tree
ready         # When node and children are ready

# Timer
$Timer.timeout.connect(_on_timeout)

# Area2D
$Area2D.body_entered.connect(_on_body_entered)
$Area2D.area_entered.connect(_on_area_entered)

# Button
$Button.pressed.connect(_on_button_pressed)

# AnimationPlayer
$AnimationPlayer.animation_finished.connect(_on_animation_finished)
```

---

## Scene Tree and Nodes

### Node References

```gdscript
# Get node by path (relative)
var player := $Player
var sprite := $Player/Sprite2D
var health_bar := $UI/HealthBar

# Get node by path (absolute)
var game_manager := get_node("/root/GameManager")

# Type-safe node access
@onready var player: CharacterBody2D = $Player
@onready var sprite: Sprite2D = $Player/Sprite2D

# Find nodes
var enemies := get_tree().get_nodes_in_group("enemies")
var first_enemy := get_tree().get_first_node_in_group("enemies")

# Get parent/children
var parent := get_parent()
var children := get_children()
var child_count := get_child_count()
```

### Node Manipulation

```gdscript
# Create node
var sprite := Sprite2D.new()
sprite.texture = preload("res://icon.svg")
add_child(sprite)

# Load and instance scene
var enemy_scene := preload("res://scenes/enemy.tscn")
var enemy := enemy_scene.instantiate()
add_child(enemy)

# Remove node
enemy.queue_free()  # Safe removal at end of frame
remove_child(enemy)  # Remove from tree but don't delete

# Reparent
sprite.reparent(new_parent)

# Reorder children
move_child(child, 0)  # Move to first position
```

### Groups

```gdscript
# Add to group
add_to_group("enemies")
add_to_group("damageable")

# Check group membership
if is_in_group("enemies"):
    pass

# Call method on all group members
get_tree().call_group("enemies", "take_damage", 10)

# Get all nodes in group
var all_enemies := get_tree().get_nodes_in_group("enemies")

# Remove from group
remove_from_group("enemies")
```

### Scene Management

```gdscript
# Change scene
get_tree().change_scene_to_file("res://scenes/level2.tscn")

# Change scene (packed)
var next_scene := preload("res://scenes/level2.tscn")
get_tree().change_scene_to_packed(next_scene)

# Reload current scene
get_tree().reload_current_scene()

# Quit game
get_tree().quit()

# Pause
get_tree().paused = true
# Node must have process_mode = PROCESS_MODE_ALWAYS to run while paused
```

---

## Input Handling

### Input Map Actions

```gdscript
# Check action state
if Input.is_action_pressed("move_right"):
    velocity.x = speed

if Input.is_action_just_pressed("jump"):
    jump()

if Input.is_action_just_released("fire"):
    stop_firing()

# Get action strength (for analog input)
var horizontal := Input.get_axis("move_left", "move_right")
var direction := Input.get_vector("move_left", "move_right", "move_up", "move_down")
```

### Input Events

```gdscript
func _input(event: InputEvent) -> void:
    # Keyboard
    if event is InputEventKey:
        if event.pressed and event.keycode == KEY_ESCAPE:
            pause_game()

    # Mouse button
    if event is InputEventMouseButton:
        if event.button_index == MOUSE_BUTTON_LEFT and event.pressed:
            shoot()

    # Mouse motion
    if event is InputEventMouseMotion:
        rotate_camera(event.relative)

    # Action-based (recommended)
    if event.is_action_pressed("jump"):
        jump()
        get_viewport().set_input_as_handled()
```

### Mouse Position

```gdscript
# Global mouse position
var mouse_pos := get_global_mouse_position()

# Local mouse position
var local_mouse := get_local_mouse_position()

# Viewport mouse position
var viewport_mouse := get_viewport().get_mouse_position()
```

---

## 2D Development

### CharacterBody2D Movement

```gdscript
extends CharacterBody2D

@export var speed := 200.0
@export var jump_velocity := -400.0
@export var gravity := 980.0

func _physics_process(delta: float) -> void:
    # Gravity
    if not is_on_floor():
        velocity.y += gravity * delta

    # Jump
    if Input.is_action_just_pressed("jump") and is_on_floor():
        velocity.y = jump_velocity

    # Horizontal movement
    var direction := Input.get_axis("move_left", "move_right")
    velocity.x = direction * speed

    move_and_slide()
```

### Sprite Animation

```gdscript
@onready var anim_sprite: AnimatedSprite2D = $AnimatedSprite2D

func update_animation() -> void:
    if velocity.x != 0:
        anim_sprite.play("walk")
        anim_sprite.flip_h = velocity.x < 0
    else:
        anim_sprite.play("idle")
```

### Area2D Collision

```gdscript
extends Area2D

signal item_collected(item)

func _ready() -> void:
    body_entered.connect(_on_body_entered)

func _on_body_entered(body: Node2D) -> void:
    if body.is_in_group("player"):
        item_collected.emit(self)
        queue_free()
```

### TileMap

```gdscript
@onready var tilemap: TileMap = $TileMap

func _ready() -> void:
    # Get tile at position
    var cell := tilemap.local_to_map(position)
    var tile_data := tilemap.get_cell_tile_data(0, cell)

    # Set tile
    tilemap.set_cell(0, cell, source_id, atlas_coords)

    # Clear tile
    tilemap.erase_cell(0, cell)
```

---

## 3D Development

### CharacterBody3D Movement

```gdscript
extends CharacterBody3D

@export var speed := 5.0
@export var jump_velocity := 4.5
@export var mouse_sensitivity := 0.002

var gravity: float = ProjectSettings.get_setting("physics/3d/default_gravity")

func _physics_process(delta: float) -> void:
    # Gravity
    if not is_on_floor():
        velocity.y -= gravity * delta

    # Jump
    if Input.is_action_just_pressed("jump") and is_on_floor():
        velocity.y = jump_velocity

    # Movement
    var input_dir := Input.get_vector("move_left", "move_right", "move_forward", "move_back")
    var direction := (transform.basis * Vector3(input_dir.x, 0, input_dir.y)).normalized()

    if direction:
        velocity.x = direction.x * speed
        velocity.z = direction.z * speed
    else:
        velocity.x = move_toward(velocity.x, 0, speed)
        velocity.z = move_toward(velocity.z, 0, speed)

    move_and_slide()

func _input(event: InputEvent) -> void:
    if event is InputEventMouseMotion and Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
        rotate_y(-event.relative.x * mouse_sensitivity)
        $Camera3D.rotate_x(-event.relative.y * mouse_sensitivity)
        $Camera3D.rotation.x = clamp($Camera3D.rotation.x, -PI/2, PI/2)
```

### Raycasting

```gdscript
# Using RayCast3D node
@onready var raycast: RayCast3D = $RayCast3D

func check_raycast() -> void:
    if raycast.is_colliding():
        var collider := raycast.get_collider()
        var point := raycast.get_collision_point()
        var normal := raycast.get_collision_normal()

# Direct physics query
func raycast_from_camera() -> void:
    var camera := get_viewport().get_camera_3d()
    var mouse_pos := get_viewport().get_mouse_position()

    var from := camera.project_ray_origin(mouse_pos)
    var to := from + camera.project_ray_normal(mouse_pos) * 1000

    var space_state := get_world_3d().direct_space_state
    var query := PhysicsRayQueryParameters3D.create(from, to)
    var result := space_state.intersect_ray(query)

    if result:
        print("Hit: ", result.collider, " at ", result.position)
```

---

## UI Development

### Control Nodes

```gdscript
# Button
@onready var button: Button = $Button
button.pressed.connect(_on_pressed)
button.text = "Click Me"
button.disabled = true

# Label
@onready var label: Label = $Label
label.text = "Score: %d" % score

# LineEdit
@onready var input: LineEdit = $LineEdit
input.text_changed.connect(_on_text_changed)
input.text_submitted.connect(_on_text_submitted)

# ProgressBar
@onready var health_bar: ProgressBar = $HealthBar
health_bar.value = health
health_bar.max_value = max_health

# TextureRect
@onready var icon: TextureRect = $Icon
icon.texture = preload("res://icon.svg")
```

### Container Layout

```gdscript
# VBoxContainer - vertical layout
# HBoxContainer - horizontal layout
# GridContainer - grid layout
# MarginContainer - add margins
# CenterContainer - center children
# PanelContainer - styled background

# Size flags
control.size_flags_horizontal = Control.SIZE_EXPAND_FILL
control.size_flags_vertical = Control.SIZE_SHRINK_CENTER
```

### Custom Drawing

```gdscript
extends Control

func _draw() -> void:
    # Rectangle
    draw_rect(Rect2(0, 0, 100, 50), Color.RED)

    # Circle
    draw_circle(Vector2(50, 50), 25, Color.BLUE)

    # Line
    draw_line(Vector2(0, 0), Vector2(100, 100), Color.WHITE, 2.0)

    # Text
    draw_string(ThemeDB.fallback_font, Vector2(10, 20), "Hello")

# Force redraw
func update_visual() -> void:
    queue_redraw()
```

---

## Animation

### AnimationPlayer

```gdscript
@onready var anim_player: AnimationPlayer = $AnimationPlayer

func _ready() -> void:
    anim_player.animation_finished.connect(_on_animation_finished)

func play_animation(name: String) -> void:
    anim_player.play(name)

func _on_animation_finished(anim_name: StringName) -> void:
    if anim_name == "attack":
        anim_player.play("idle")

# Control playback
anim_player.pause()
anim_player.stop()
anim_player.seek(0.5)  # Jump to time
anim_player.speed_scale = 2.0  # Double speed
anim_player.play_backwards("walk")
```

### Tweens

```gdscript
# Create tween
var tween := create_tween()

# Property animation
tween.tween_property(sprite, "position", Vector2(100, 100), 0.5)
tween.tween_property(sprite, "modulate:a", 0.0, 0.3)

# Chaining
tween.tween_property(sprite, "position:x", 200, 0.5)
tween.tween_property(sprite, "position:y", 200, 0.5)  # Plays after x

# Parallel
tween.set_parallel(true)
tween.tween_property(sprite, "position", target, 0.5)
tween.tween_property(sprite, "rotation", PI, 0.5)  # Plays simultaneously

# Easing
tween.set_ease(Tween.EASE_OUT)
tween.set_trans(Tween.TRANS_BOUNCE)

# Callbacks
tween.tween_callback(queue_free)
tween.tween_callback(func(): print("Done!"))

# Delays
tween.tween_interval(0.5)  # Wait 0.5 seconds

# Loop
tween.set_loops(3)  # Loop 3 times
tween.set_loops()   # Loop forever
```

---

## Resources

### Loading Resources

```gdscript
# Preload (compile-time, recommended for known resources)
const PLAYER_SCENE := preload("res://scenes/player.tscn")
const ICON := preload("res://icon.svg")

# Load (runtime)
var texture := load("res://textures/enemy.png")
var scene := load("res://scenes/level_%d.tscn" % level_num)

# Async loading
ResourceLoader.load_threaded_request("res://large_scene.tscn")

func _process(_delta: float) -> void:
    var status := ResourceLoader.load_threaded_get_status("res://large_scene.tscn")
    if status == ResourceLoader.THREAD_LOAD_LOADED:
        var scene := ResourceLoader.load_threaded_get("res://large_scene.tscn")
```

### Custom Resources

```gdscript
# weapon_data.gd
class_name WeaponData
extends Resource

@export var name: String
@export var damage: int
@export var fire_rate: float
@export var icon: Texture2D

# Usage
@export var weapon: WeaponData

func attack() -> void:
    deal_damage(weapon.damage)
```

### Saving/Loading

```gdscript
# Save
func save_game() -> void:
    var save_data := {
        "health": health,
        "position": {"x": position.x, "y": position.y},
        "inventory": inventory
    }
    var file := FileAccess.open("user://save.json", FileAccess.WRITE)
    file.store_string(JSON.stringify(save_data))

# Load
func load_game() -> void:
    if not FileAccess.file_exists("user://save.json"):
        return
    var file := FileAccess.open("user://save.json", FileAccess.READ)
    var save_data: Dictionary = JSON.parse_string(file.get_as_text())
    health = save_data.health
    position = Vector2(save_data.position.x, save_data.position.y)
```

---

## Autoloads (Singletons)

### Setting Up

Project Settings > Autoload > Add script

```gdscript
# game_manager.gd (autoload as "GameManager")
extends Node

signal score_changed(new_score: int)

var score: int = 0:
    set(value):
        score = value
        score_changed.emit(score)

var current_level: int = 1

func reset_game() -> void:
    score = 0
    current_level = 1
```

### Using Autoloads

```gdscript
# From any script
func collect_coin() -> void:
    GameManager.score += 10

func _ready() -> void:
    GameManager.score_changed.connect(_on_score_changed)
```

---

## GDScript Style Guide

### Naming Conventions

| Type | Convention | Example |
|------|------------|---------|
| Classes | PascalCase | `PlayerController` |
| Nodes | PascalCase | `HealthBar` |
| Functions | snake_case | `calculate_damage()` |
| Variables | snake_case | `player_health` |
| Constants | SCREAMING_SNAKE | `MAX_SPEED` |
| Signals | snake_case (past tense) | `health_changed` |
| Enums | PascalCase | `State.IDLE` |
| Private | _underscore prefix | `_internal_value` |

### Code Order

```gdscript
class_name MyClass
extends BaseClass

# 1. Signals
signal health_changed

# 2. Enums
enum State { IDLE, RUNNING }

# 3. Constants
const MAX_HEALTH := 100

# 4. Exported variables
@export var speed: float = 5.0

# 5. Public variables
var health: int = 100

# 6. Private variables
var _internal_state: State

# 7. @onready variables
@onready var sprite: Sprite2D = $Sprite2D

# 8. Built-in virtual methods
func _ready() -> void:
    pass

func _process(delta: float) -> void:
    pass

# 9. Public methods
func take_damage(amount: int) -> void:
    pass

# 10. Private methods
func _update_state() -> void:
    pass

# 11. Signal callbacks
func _on_button_pressed() -> void:
    pass
```

### Type Hints (Always Use)

```gdscript
# Variables
var health: int = 100
var velocity: Vector2 = Vector2.ZERO

# Functions
func calculate_damage(base: int, multiplier: float = 1.0) -> int:
    return int(base * multiplier)

# Arrays and Dictionaries
var items: Array[Item] = []
var scores: Dictionary[String, int] = {}
```

---

## Common Patterns

### State Machine

```gdscript
enum State { IDLE, WALK, JUMP, ATTACK }

var current_state: State = State.IDLE

func _physics_process(delta: float) -> void:
    match current_state:
        State.IDLE:
            process_idle(delta)
        State.WALK:
            process_walk(delta)
        State.JUMP:
            process_jump(delta)
        State.ATTACK:
            process_attack(delta)

func change_state(new_state: State) -> void:
    exit_state(current_state)
    current_state = new_state
    enter_state(new_state)
```

### Object Pooling

```gdscript
var bullet_pool: Array[Bullet] = []

func get_bullet() -> Bullet:
    for bullet in bullet_pool:
        if not bullet.active:
            bullet.active = true
            return bullet

    var new_bullet := BULLET_SCENE.instantiate()
    bullet_pool.append(new_bullet)
    add_child(new_bullet)
    return new_bullet
```

### Component System

```gdscript
# health_component.gd
class_name HealthComponent
extends Node

signal died
signal health_changed(new_health: int)

@export var max_health: int = 100
var current_health: int

func _ready() -> void:
    current_health = max_health

func take_damage(amount: int) -> void:
    current_health = max(0, current_health - amount)
    health_changed.emit(current_health)
    if current_health <= 0:
        died.emit()
```

---

## Debugging

```gdscript
# Print debugging
print("Value: ", value)
prints("Multiple", "values", "with", "spaces")
printt("Tab", "separated")
print_rich("[color=red]Error:[/color] Something went wrong")

# Assertions (debug builds only)
assert(health >= 0, "Health cannot be negative")

# Breakpoints
breakpoint  # Pauses execution in debugger

# Stack trace
print_stack()

# Performance
var start := Time.get_ticks_msec()
# ... code to measure ...
print("Took %d ms" % (Time.get_ticks_msec() - start))
```

---

## Godot 4.x with C#

### C# Differences

```csharp
// Node references
[Export] public float Speed = 200f;
private Sprite2D _sprite;

public override void _Ready()
{
    _sprite = GetNode<Sprite2D>("Sprite2D");
}

// Signals
[Signal] public delegate void HealthChangedEventHandler(int newHealth);

// Emit
EmitSignal(SignalName.HealthChanged, health);

// Connect
button.Pressed += OnButtonPressed;
```

### When to Use C#

- Large-scale projects benefiting from strong typing
- Existing C# codebase or team experience
- Need for specific .NET libraries
- Performance-critical code (marginal improvement over typed GDScript)

### When to Use GDScript

- Rapid prototyping
- Tight Godot integration (easier signal/node access)
- Smaller projects
- Learning Godot

---

## External Resources

- [Official Documentation](https://docs.godotengine.org/en/stable/)
- [GDScript Reference](https://docs.godotengine.org/en/stable/tutorials/scripting/gdscript/gdscript_basics.html)
- [Class Reference](https://docs.godotengine.org/en/stable/classes/index.html)
- [GDQuest Tutorials](https://www.gdquest.com/)
