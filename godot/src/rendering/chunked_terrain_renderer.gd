class_name ChunkedTerrainRenderer
extends Node3D
## Renders hex terrain using a chunk-based system with LOD and culling
## Matches web/src/rendering/ChunkedTerrainRenderer.ts

const CHUNK_SIZE: float = 16.0

# Distance culling - chunks beyond this are hidden (works with fog)
const MAX_RENDER_DISTANCE: float = 60.0  # Beyond fog end

# LOD distance thresholds (matching Three.js LODDistances)
const LOD_HIGH_TO_MEDIUM: float = 30.0
const LOD_MEDIUM_TO_LOW: float = 60.0

# Reference zoom distance for LOD scaling (default camera distance)
const REFERENCE_ZOOM: float = 30.0

# Base Y level for skirts - below minimum terrain elevation
const SKIRT_BASE_Y: float = HexMetrics.MIN_ELEVATION * HexMetrics.ELEVATION_STEP - 1.0

# Material for all terrain meshes
var terrain_material: ShaderMaterial
var terrain_shader: Shader

# Chunk storage: key -> TerrainChunk
var chunks: Dictionary = {}

# Stats
var total_chunk_count: int = 0


class TerrainChunk:
	var mesh_high: MeshInstance3D    # Full detail
	var mesh_medium: MeshInstance3D  # Simplified
	var mesh_low: MeshInstance3D     # Very simple
	var mesh_skirt: MeshInstance3D   # Always-visible boundary skirt
	var cells: Array[HexCell] = []
	var chunk_x: int = 0
	var chunk_z: int = 0
	var center: Vector3 = Vector3.ZERO


func _init() -> void:
	# Load terrain shader with depth bias to prevent z-fighting
	terrain_shader = load("res://src/rendering/terrain_shader.gdshader")
	terrain_material = ShaderMaterial.new()
	terrain_material.shader = terrain_shader
	terrain_material.set_shader_parameter("depth_bias", 0.001)


func _get_chunk_key(cx: int, cz: int) -> String:
	return "%d,%d" % [cx, cz]


func _get_cell_chunk_coords(cell: HexCell) -> Vector2i:
	var coords = HexCoordinates.new(cell.q, cell.r)
	var world_pos = coords.to_world_position(0)
	var cx = int(floor(world_pos.x / CHUNK_SIZE))
	var cz = int(floor(world_pos.z / CHUNK_SIZE))
	return Vector2i(cx, cz)


func _get_chunk_center(cx: int, cz: int) -> Vector3:
	return Vector3((cx + 0.5) * CHUNK_SIZE, 0, (cz + 0.5) * CHUNK_SIZE)


## Build terrain from grid
func build(grid: HexGrid) -> void:
	dispose()

	# Group cells into chunks
	for cell in grid.get_all_cells():
		var chunk_coords = _get_cell_chunk_coords(cell)
		var key = _get_chunk_key(chunk_coords.x, chunk_coords.y)

		if not chunks.has(key):
			var new_chunk = TerrainChunk.new()
			new_chunk.chunk_x = chunk_coords.x
			new_chunk.chunk_z = chunk_coords.y
			new_chunk.center = _get_chunk_center(chunk_coords.x, chunk_coords.y)
			chunks[key] = new_chunk

		chunks[key].cells.append(cell)

	# Build meshes for all chunks
	for key in chunks:
		var chunk: TerrainChunk = chunks[key]
		_build_chunk_meshes(chunk, grid)

	total_chunk_count = chunks.size()
	print("Built %d terrain chunks" % total_chunk_count)


func _build_chunk_meshes(chunk: TerrainChunk, grid: HexGrid) -> void:
	if chunk.cells.is_empty():
		return

	# HIGH detail - full hex with terraces
	var builder_high = HexMeshBuilder.new()
	for cell in chunk.cells:
		builder_high.build_cell(cell, grid)
	var mesh_high = builder_high._create_mesh()

	chunk.mesh_high = MeshInstance3D.new()
	chunk.mesh_high.mesh = mesh_high
	chunk.mesh_high.material_override = terrain_material
	chunk.mesh_high.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	chunk.mesh_high.name = "Chunk_%d_%d_HIGH" % [chunk.chunk_x, chunk.chunk_z]
	add_child(chunk.mesh_high)

	# MEDIUM detail - flat hexes
	var mesh_medium = _build_flat_hex_mesh(chunk.cells)
	chunk.mesh_medium = MeshInstance3D.new()
	chunk.mesh_medium.mesh = mesh_medium
	chunk.mesh_medium.material_override = terrain_material
	chunk.mesh_medium.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	chunk.mesh_medium.name = "Chunk_%d_%d_MED" % [chunk.chunk_x, chunk.chunk_z]
	chunk.mesh_medium.visible = false
	add_child(chunk.mesh_medium)

	# LOW detail - simple quads (even simpler)
	var mesh_low = _build_simple_quad_mesh(chunk.cells)
	chunk.mesh_low = MeshInstance3D.new()
	chunk.mesh_low.mesh = mesh_low
	chunk.mesh_low.material_override = terrain_material
	chunk.mesh_low.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	chunk.mesh_low.name = "Chunk_%d_%d_LOW" % [chunk.chunk_x, chunk.chunk_z]
	chunk.mesh_low.visible = false
	add_child(chunk.mesh_low)

	# SKIRT disabled for now - was causing visual issues
	# TODO: Implement proper edge-only skirts that don't block terrain view


## Build chunk boundary skirt - walls around the outer edges of chunk
func _build_chunk_boundary_skirt(cells: Array[HexCell], chunk_x: int, chunk_z: int) -> ArrayMesh:
	if cells.is_empty():
		return null

	var st = SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)

	# Find the bounding box of this chunk's cells
	var min_x = INF
	var max_x = -INF
	var min_z = INF
	var max_z = -INF
	var avg_y: float = 0.0

	for cell in cells:
		var pos = cell.get_world_position()
		min_x = min(min_x, pos.x - HexMetrics.OUTER_RADIUS)
		max_x = max(max_x, pos.x + HexMetrics.OUTER_RADIUS)
		min_z = min(min_z, pos.z - HexMetrics.OUTER_RADIUS)
		max_z = max(max_z, pos.z + HexMetrics.OUTER_RADIUS)
		avg_y += pos.y

	avg_y /= cells.size()

	# Use a neutral color for the skirt (blends with fog)
	var skirt_color = Color(0.4, 0.45, 0.5)  # Gray-blue to match fog
	var down_normal = Vector3(0, -1, 0)

	# Build 4 walls around the chunk boundary
	var top_y = avg_y
	var bottom_y = SKIRT_BASE_Y

	# Wall vertices (going clockwise when viewed from above)
	var corners = [
		Vector3(min_x, top_y, min_z),  # 0: front-left
		Vector3(max_x, top_y, min_z),  # 1: front-right
		Vector3(max_x, top_y, max_z),  # 2: back-right
		Vector3(min_x, top_y, max_z),  # 3: back-left
	]

	# Build 4 walls
	for i in range(4):
		var c1 = corners[i]
		var c2 = corners[(i + 1) % 4]

		var top_left = c1
		var top_right = c2
		var bottom_left = Vector3(c1.x, bottom_y, c1.z)
		var bottom_right = Vector3(c2.x, bottom_y, c2.z)

		# Calculate outward-facing normal for this wall
		var edge = top_right - top_left
		var down = Vector3(0, -1, 0)
		var wall_normal = edge.cross(down).normalized()

		# Two triangles for the quad
		st.set_normal(wall_normal)
		st.set_color(skirt_color)
		st.add_vertex(top_left)
		st.set_normal(wall_normal)
		st.set_color(skirt_color)
		st.add_vertex(bottom_left)
		st.set_normal(wall_normal)
		st.set_color(skirt_color)
		st.add_vertex(bottom_right)

		st.set_normal(wall_normal)
		st.set_color(skirt_color)
		st.add_vertex(top_left)
		st.set_normal(wall_normal)
		st.set_color(skirt_color)
		st.add_vertex(bottom_right)
		st.set_normal(wall_normal)
		st.set_color(skirt_color)
		st.add_vertex(top_right)

	return st.commit()


## Build flat hex mesh (medium LOD) with per-hex skirts
func _build_flat_hex_mesh(cells: Array[HexCell]) -> ArrayMesh:
	var st = SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	var corners = HexMetrics.get_corners()
	var up_normal = Vector3(0, 1, 0)

	for cell in cells:
		var center = cell.get_world_position()
		var base_color = cell.get_color()
		# Boost saturation slightly to compensate for lack of terrace detail
		var h = base_color.h
		var s = min(base_color.s * 1.15, 1.0)  # 15% saturation boost
		var v = base_color.v
		var color = Color.from_hsv(h, s, v)
		# Use same color for tops and skirts - shader wall_darkening handles shading
		var skirt_color = color

		# Build hex top as 6 triangles from center - use flat up normal
		for i in range(6):
			var c1 = corners[i]
			var c2 = corners[(i + 1) % 6]

			st.set_normal(up_normal)
			st.set_color(color)
			st.add_vertex(center)
			st.set_normal(up_normal)
			st.set_color(color)
			st.add_vertex(Vector3(center.x + c1.x, center.y, center.z + c1.z))
			st.set_normal(up_normal)
			st.set_color(color)
			st.add_vertex(Vector3(center.x + c2.x, center.y, center.z + c2.z))

		# Build hex skirt - 6 quads around perimeter with outward normals
		for i in range(6):
			var c1 = corners[i]
			var c2 = corners[(i + 1) % 6]

			var top_left = Vector3(center.x + c1.x, center.y, center.z + c1.z)
			var top_right = Vector3(center.x + c2.x, center.y, center.z + c2.z)
			var bottom_left = Vector3(center.x + c1.x, SKIRT_BASE_Y, center.z + c1.z)
			var bottom_right = Vector3(center.x + c2.x, SKIRT_BASE_Y, center.z + c2.z)

			# Calculate outward-facing normal for this edge
			var edge = top_right - top_left
			var outward = Vector3(edge.z, 0, -edge.x).normalized()

			# Two triangles for the quad (facing outward)
			st.set_normal(outward)
			st.set_color(skirt_color)
			st.add_vertex(top_left)
			st.set_normal(outward)
			st.set_color(skirt_color)
			st.add_vertex(bottom_left)
			st.set_normal(outward)
			st.set_color(skirt_color)
			st.add_vertex(bottom_right)

			st.set_normal(outward)
			st.set_color(skirt_color)
			st.add_vertex(top_left)
			st.set_normal(outward)
			st.set_color(skirt_color)
			st.add_vertex(bottom_right)
			st.set_normal(outward)
			st.set_color(skirt_color)
			st.add_vertex(top_right)

	return st.commit()


## Build simple quad mesh (low LOD) - one quad per cell with box skirt
func _build_simple_quad_mesh(cells: Array[HexCell]) -> ArrayMesh:
	var st = SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	var up_normal = Vector3(0, 1, 0)

	for cell in cells:
		var center = cell.get_world_position()
		var base_color = cell.get_color()
		# Boost saturation slightly to compensate for lack of terrace detail
		var h = base_color.h
		var s = min(base_color.s * 1.15, 1.0)  # 15% saturation boost
		var v = base_color.v
		var color = Color.from_hsv(h, s, v)
		# Use same color for tops and skirts - shader wall_darkening handles shading
		var skirt_color = color
		var size = HexMetrics.OUTER_RADIUS * 0.85  # Match Three.js

		# Simple quad top with flat up normal
		var v1 = Vector3(center.x - size, center.y, center.z - size)
		var v2 = Vector3(center.x + size, center.y, center.z - size)
		var v3 = Vector3(center.x + size, center.y, center.z + size)
		var v4 = Vector3(center.x - size, center.y, center.z + size)

		st.set_normal(up_normal)
		st.set_color(color)
		st.add_vertex(v1)
		st.set_normal(up_normal)
		st.set_color(color)
		st.add_vertex(v2)
		st.set_normal(up_normal)
		st.set_color(color)
		st.add_vertex(v3)

		st.set_normal(up_normal)
		st.set_color(color)
		st.add_vertex(v1)
		st.set_normal(up_normal)
		st.set_color(color)
		st.add_vertex(v3)
		st.set_normal(up_normal)
		st.set_color(color)
		st.add_vertex(v4)

		# Box skirt - 4 walls around the quad with outward normals
		var quad_corners = [v1, v2, v3, v4]
		# Outward normals for each wall (in order: -Z, +X, +Z, -X)
		var wall_normals = [
			Vector3(0, 0, -1),
			Vector3(1, 0, 0),
			Vector3(0, 0, 1),
			Vector3(-1, 0, 0)
		]
		for i in range(4):
			var c1 = quad_corners[i]
			var c2 = quad_corners[(i + 1) % 4]
			var wall_normal = wall_normals[i]

			var top_left = c1
			var top_right = c2
			var bottom_left = Vector3(c1.x, SKIRT_BASE_Y, c1.z)
			var bottom_right = Vector3(c2.x, SKIRT_BASE_Y, c2.z)

			st.set_normal(wall_normal)
			st.set_color(skirt_color)
			st.add_vertex(top_left)
			st.set_normal(wall_normal)
			st.set_color(skirt_color)
			st.add_vertex(bottom_left)
			st.set_normal(wall_normal)
			st.set_color(skirt_color)
			st.add_vertex(bottom_right)

			st.set_normal(wall_normal)
			st.set_color(skirt_color)
			st.add_vertex(top_left)
			st.set_normal(wall_normal)
			st.set_color(skirt_color)
			st.add_vertex(bottom_right)
			st.set_normal(wall_normal)
			st.set_color(skirt_color)
			st.add_vertex(top_right)

	return st.commit()


## Update visibility and LOD based on camera - call every frame
func update(camera: Camera3D) -> void:
	var camera_pos = camera.global_position

	# Use camera XZ position for distance calculations
	# This ensures foreground terrain (close to camera) always gets HIGH LOD
	var camera_xz = Vector3(camera_pos.x, 0, camera_pos.z)

	# No zoom scaling - use fixed LOD distances based on camera position
	# This prevents issues where foreground gets culled/LOD'd incorrectly
	var effective_max_dist = MAX_RENDER_DISTANCE
	var max_dist_sq = effective_max_dist * effective_max_dist

	var visible_count = 0
	var culled_count = 0

	for key in chunks:
		var chunk: TerrainChunk = chunks[key]
		if not chunk.mesh_high:
			continue

		# Horizontal distance from camera to chunk
		var dx = chunk.center.x - camera_xz.x
		var dz = chunk.center.z - camera_xz.z
		var dist_sq = dx * dx + dz * dz

		if dist_sq > max_dist_sq:
			# Beyond render distance - hide all LODs
			chunk.mesh_high.visible = false
			chunk.mesh_medium.visible = false
			chunk.mesh_low.visible = false
			culled_count += 1
			continue

		visible_count += 1
		var dist = sqrt(dist_sq)

		# Debug: print first chunk distance occasionally
		if key == chunks.keys()[0] and Engine.get_frames_drawn() % 120 == 0:
			print("Chunk dist: %.1f, max: %.1f, cam: (%.1f, %.1f)" % [dist, effective_max_dist, camera_xz.x, camera_xz.z])

		# LOD selection based on distance from camera
		if dist < LOD_HIGH_TO_MEDIUM:
			chunk.mesh_high.visible = true
			chunk.mesh_medium.visible = false
			chunk.mesh_low.visible = false
		elif dist < LOD_MEDIUM_TO_LOW:
			chunk.mesh_high.visible = false
			chunk.mesh_medium.visible = true
			chunk.mesh_low.visible = false
		else:
			chunk.mesh_high.visible = false
			chunk.mesh_medium.visible = false
			chunk.mesh_low.visible = true


func get_chunk_count() -> int:
	return total_chunk_count


func dispose() -> void:
	for key in chunks:
		var chunk: TerrainChunk = chunks[key]
		if chunk.mesh_high:
			chunk.mesh_high.queue_free()
		if chunk.mesh_medium:
			chunk.mesh_medium.queue_free()
		if chunk.mesh_low:
			chunk.mesh_low.queue_free()
		if chunk.mesh_skirt:
			chunk.mesh_skirt.queue_free()
	chunks.clear()
	total_chunk_count = 0
