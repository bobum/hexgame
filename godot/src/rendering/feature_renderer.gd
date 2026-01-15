class_name FeatureRenderer
extends Node3D
## Renders instanced features (trees, rocks) using chunked MultiMesh for distance culling
## Features are grouped by chunk to match terrain chunking for consistent visibility

const FeatureClass = preload("res://src/core/feature.gd")

const CHUNK_SIZE: float = 16.0
const MAX_RENDER_DISTANCE: float = 50.0  # Match terrain culling distance

var tree_material: StandardMaterial3D
var rock_material: StandardMaterial3D

# Chunk storage: key -> FeatureChunk
var chunks: Dictionary = {}

# Shared meshes
var tree_mesh: ArrayMesh
var rock_mesh: ArrayMesh


class FeatureChunk:
	var tree_multimesh: MultiMeshInstance3D
	var rock_multimesh: MultiMeshInstance3D
	var chunk_x: int = 0
	var chunk_z: int = 0
	var center: Vector3 = Vector3.ZERO


func _init() -> void:
	# Create materials
	tree_material = StandardMaterial3D.new()
	tree_material.albedo_color = Color(0.133, 0.545, 0.133)  # Forest green
	tree_material.shading_mode = BaseMaterial3D.SHADING_MODE_PER_VERTEX
	tree_material.cull_mode = BaseMaterial3D.CULL_BACK

	rock_material = StandardMaterial3D.new()
	rock_material.albedo_color = Color(0.412, 0.412, 0.412)  # Dim gray
	rock_material.shading_mode = BaseMaterial3D.SHADING_MODE_PER_VERTEX
	rock_material.cull_mode = BaseMaterial3D.CULL_BACK

	# Create shared meshes
	tree_mesh = _create_tree_mesh()
	rock_mesh = _create_rock_mesh()


func _get_chunk_key(cx: int, cz: int) -> String:
	return "%d,%d" % [cx, cz]


func _get_feature_chunk_coords(pos: Vector3) -> Vector2i:
	var cx = int(floor(pos.x / CHUNK_SIZE))
	var cz = int(floor(pos.z / CHUNK_SIZE))
	return Vector2i(cx, cz)


func _get_chunk_center(cx: int, cz: int) -> Vector3:
	return Vector3((cx + 0.5) * CHUNK_SIZE, 0, (cz + 0.5) * CHUNK_SIZE)


## Build feature meshes from grid
func build(grid: HexGrid) -> void:
	dispose()

	# Group features by chunk
	var chunk_trees: Dictionary = {}  # key -> Array of features
	var chunk_rocks: Dictionary = {}

	for cell in grid.get_all_cells():
		for feature in cell.features:
			var chunk_coords = _get_feature_chunk_coords(feature.position)
			var key = _get_chunk_key(chunk_coords.x, chunk_coords.y)

			# Ensure chunk exists
			if not chunks.has(key):
				var new_chunk = FeatureChunk.new()
				new_chunk.chunk_x = chunk_coords.x
				new_chunk.chunk_z = chunk_coords.y
				new_chunk.center = _get_chunk_center(chunk_coords.x, chunk_coords.y)
				chunks[key] = new_chunk

			if feature.type == FeatureClass.Type.TREE:
				if not chunk_trees.has(key):
					chunk_trees[key] = []
				chunk_trees[key].append(feature)
			elif feature.type == FeatureClass.Type.ROCK:
				if not chunk_rocks.has(key):
					chunk_rocks[key] = []
				chunk_rocks[key].append(feature)

	# Build MultiMesh for each chunk
	var total_trees = 0
	var total_rocks = 0

	for key in chunks:
		var chunk: FeatureChunk = chunks[key]
		var trees = chunk_trees.get(key, [])
		var rocks = chunk_rocks.get(key, [])

		if trees.size() > 0:
			chunk.tree_multimesh = _build_multimesh(trees, tree_mesh, tree_material, "Trees_%s" % key)
			add_child(chunk.tree_multimesh)
			total_trees += trees.size()

		if rocks.size() > 0:
			chunk.rock_multimesh = _build_multimesh(rocks, rock_mesh, rock_material, "Rocks_%s" % key)
			add_child(chunk.rock_multimesh)
			total_rocks += rocks.size()

	print("Built features: %d trees, %d rocks" % [total_trees, total_rocks])


func _build_multimesh(features: Array, mesh: ArrayMesh, material: StandardMaterial3D, name: String) -> MultiMeshInstance3D:
	var multimesh = MultiMesh.new()
	multimesh.transform_format = MultiMesh.TRANSFORM_3D
	multimesh.use_colors = true
	multimesh.mesh = mesh
	multimesh.instance_count = features.size()

	var rng = RandomNumberGenerator.new()
	rng.seed = hash(name)  # Deterministic per chunk

	for i in range(features.size()):
		var feature = features[i]
		var transform = Transform3D()
		transform = transform.rotated(Vector3.UP, feature.rotation)
		transform = transform.scaled(Vector3.ONE * feature.scale)
		transform.origin = feature.position
		multimesh.set_instance_transform(i, transform)

		# Vary color slightly
		var base_color = material.albedo_color
		var variation = rng.randf_range(-0.1, 0.1)
		multimesh.set_instance_color(i, base_color.lightened(variation))

	var instance = MultiMeshInstance3D.new()
	instance.multimesh = multimesh
	instance.material_override = material
	instance.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	instance.name = name

	return instance


## Update visibility based on camera - hide chunks beyond render distance
func update(camera: Camera3D) -> void:
	if not camera:
		return

	var camera_pos = camera.global_position
	# Use camera XZ position for distance (matches terrain renderer)
	var camera_xz = Vector3(camera_pos.x, 0, camera_pos.z)

	var max_dist_sq = MAX_RENDER_DISTANCE * MAX_RENDER_DISTANCE

	for key in chunks:
		var chunk: FeatureChunk = chunks[key]

		# Distance from camera to chunk
		var dx = chunk.center.x - camera_xz.x
		var dz = chunk.center.z - camera_xz.z
		var dist_sq = dx * dx + dz * dz

		var visible = dist_sq <= max_dist_sq

		if chunk.tree_multimesh:
			chunk.tree_multimesh.visible = visible
		if chunk.rock_multimesh:
			chunk.rock_multimesh.visible = visible


## Create simple tree mesh (cone for foliage + cylinder trunk)
func _create_tree_mesh() -> ArrayMesh:
	var st = SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)

	# Cone for foliage
	var cone_height = 0.4
	var cone_radius = 0.15
	var cone_y = 0.3
	var segments = 6

	# Cone top vertex
	var top = Vector3(0, cone_y + cone_height, 0)

	# Cone base vertices
	for i in range(segments):
		var angle1 = float(i) / segments * TAU
		var angle2 = float(i + 1) / segments * TAU

		var v1 = Vector3(cos(angle1) * cone_radius, cone_y, sin(angle1) * cone_radius)
		var v2 = Vector3(cos(angle2) * cone_radius, cone_y, sin(angle2) * cone_radius)

		# Side triangle
		st.set_color(Color(0.133, 0.545, 0.133))
		st.add_vertex(top)
		st.add_vertex(v1)
		st.add_vertex(v2)

	# Trunk (cylinder)
	var trunk_radius_top = 0.03
	var trunk_radius_bottom = 0.04
	var trunk_height = 0.15

	for i in range(segments):
		var angle1 = float(i) / segments * TAU
		var angle2 = float(i + 1) / segments * TAU

		var t1 = Vector3(cos(angle1) * trunk_radius_top, trunk_height, sin(angle1) * trunk_radius_top)
		var t2 = Vector3(cos(angle2) * trunk_radius_top, trunk_height, sin(angle2) * trunk_radius_top)
		var b1 = Vector3(cos(angle1) * trunk_radius_bottom, 0, sin(angle1) * trunk_radius_bottom)
		var b2 = Vector3(cos(angle2) * trunk_radius_bottom, 0, sin(angle2) * trunk_radius_bottom)

		var trunk_color = Color(0.4, 0.26, 0.13)  # Brown
		st.set_color(trunk_color)
		st.add_vertex(t1)
		st.add_vertex(b1)
		st.add_vertex(t2)

		st.add_vertex(t2)
		st.add_vertex(b1)
		st.add_vertex(b2)

	st.generate_normals()
	return st.commit()


## Create rock mesh (deformed icosahedron)
func _create_rock_mesh() -> ArrayMesh:
	# Start with an icosahedron base
	var st = SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)

	# Icosahedron vertices (unit)
	var t = (1.0 + sqrt(5.0)) / 2.0
	var verts = [
		Vector3(-1, t, 0).normalized() * 0.12,
		Vector3(1, t, 0).normalized() * 0.12,
		Vector3(-1, -t, 0).normalized() * 0.12,
		Vector3(1, -t, 0).normalized() * 0.12,
		Vector3(0, -1, t).normalized() * 0.12,
		Vector3(0, 1, t).normalized() * 0.12,
		Vector3(0, -1, -t).normalized() * 0.12,
		Vector3(0, 1, -t).normalized() * 0.12,
		Vector3(t, 0, -1).normalized() * 0.12,
		Vector3(t, 0, 1).normalized() * 0.12,
		Vector3(-t, 0, -1).normalized() * 0.12,
		Vector3(-t, 0, 1).normalized() * 0.12
	]

	# Deform vertices for organic look
	for i in range(verts.size()):
		var v = verts[i]
		var noise = sin(v.x * 10) * cos(v.z * 10) * 0.3 + 1
		# Flatten bottom half
		var y_scale = 0.3 if v.y < 0 else 1.0
		verts[i] = Vector3(v.x * noise, v.y * noise * y_scale, v.z * noise)
		# Shift up so bottom sits at y=0
		verts[i].y += 0.08

	# Icosahedron faces (20 triangles)
	var faces = [
		[0, 11, 5], [0, 5, 1], [0, 1, 7], [0, 7, 10], [0, 10, 11],
		[1, 5, 9], [5, 11, 4], [11, 10, 2], [10, 7, 6], [7, 1, 8],
		[3, 9, 4], [3, 4, 2], [3, 2, 6], [3, 6, 8], [3, 8, 9],
		[4, 9, 5], [2, 4, 11], [6, 2, 10], [8, 6, 7], [9, 8, 1]
	]

	var rock_color = Color(0.412, 0.412, 0.412)
	for face in faces:
		st.set_color(rock_color)
		st.add_vertex(verts[face[0]])
		st.add_vertex(verts[face[1]])
		st.add_vertex(verts[face[2]])

	st.generate_normals()
	return st.commit()


## Clean up resources
func dispose() -> void:
	for key in chunks:
		var chunk: FeatureChunk = chunks[key]
		if chunk.tree_multimesh:
			chunk.tree_multimesh.queue_free()
		if chunk.rock_multimesh:
			chunk.rock_multimesh.queue_free()
	chunks.clear()
