extends Node3D
## Main entry point for HexGame
## Manages game initialization and main loop

@onready var hex_grid: Node3D = $HexGrid
@onready var camera: Camera3D = $Camera3D

var grid: HexGrid
var map_generator: MapGenerator
var terrain_renderer: TerrainRenderer


func _ready() -> void:
	print("HexGame starting...")
	_initialize_game()


func _initialize_game() -> void:
	# Initialize core systems
	grid = HexGrid.new()
	map_generator = MapGenerator.new()
	terrain_renderer = TerrainRenderer.new()

	# Generate initial map
	_generate_map()


func _generate_map() -> void:
	# TODO: Implement map generation
	print("Generating map...")


func _process(_delta: float) -> void:
	# Main game loop
	pass


func _input(event: InputEvent) -> void:
	# Handle input
	pass
