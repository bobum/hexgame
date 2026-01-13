class_name TerrainRenderer
extends RefCounted
## Renders hex terrain using Godot's mesh system
## Matches web/src/rendering/ChunkedTerrainRenderer.ts

var grid: HexGrid
var mesh_instance: MeshInstance3D
var hex_mesh_builder: HexMeshBuilder


func _init() -> void:
	hex_mesh_builder = HexMeshBuilder.new()


## Build terrain mesh for entire grid
func build(hex_grid: HexGrid, parent: Node3D) -> void:
	grid = hex_grid

	# Create mesh instance if needed
	if mesh_instance == null:
		mesh_instance = MeshInstance3D.new()
		parent.add_child(mesh_instance)

	# Build the mesh
	var mesh = hex_mesh_builder.build_grid_mesh(grid)
	mesh_instance.mesh = mesh

	# Create material
	var material = StandardMaterial3D.new()
	material.vertex_color_use_as_albedo = true
	material.shading_mode = BaseMaterial3D.SHADING_MODE_PER_VERTEX
	mesh_instance.material_override = material


## Update specific cells (for dynamic changes)
func update_cells(_cells: Array[HexCell]) -> void:
	# TODO: Implement partial updates
	pass


## Dispose of resources
func dispose() -> void:
	if mesh_instance:
		mesh_instance.queue_free()
		mesh_instance = null
