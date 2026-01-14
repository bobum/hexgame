class_name FeatureRenderer
extends Node3D
## Renders instanced features (trees, rocks) using MultiMesh for performance

const FeatureClass = preload("res://src/core/feature.gd")

var tree_multimesh: MultiMeshInstance3D
var rock_multimesh: MultiMeshInstance3D

var tree_material: StandardMaterial3D
var rock_material: StandardMaterial3D


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


## Build feature meshes from grid
func build(grid: HexGrid) -> void:
	dispose()

	# Collect all features
	var trees: Array = []
	var rocks: Array = []

	for cell in grid.get_all_cells():
		for feature in cell.features:
			if feature.type == FeatureClass.Type.TREE:
				trees.append(feature)
			elif feature.type == FeatureClass.Type.ROCK:
				rocks.append(feature)

	# Build tree instances
	if trees.size() > 0:
		_build_trees(trees)

	# Build rock instances
	if rocks.size() > 0:
		_build_rocks(rocks)

	print("Built features: %d trees, %d rocks" % [trees.size(), rocks.size()])


func _build_trees(trees: Array) -> void:
	# Create tree mesh (cone + trunk)
	var tree_mesh = _create_tree_mesh()

	# Create MultiMesh
	var multimesh = MultiMesh.new()
	multimesh.transform_format = MultiMesh.TRANSFORM_3D
	multimesh.use_colors = true
	multimesh.mesh = tree_mesh
	multimesh.instance_count = trees.size()

	# Set transforms and colors
	var rng = RandomNumberGenerator.new()
	rng.seed = 12345  # Deterministic for consistent colors

	for i in range(trees.size()):
		var feature = trees[i]
		var transform = Transform3D()
		transform = transform.rotated(Vector3.UP, feature.rotation)
		transform = transform.scaled(Vector3.ONE * feature.scale)
		transform.origin = feature.position
		multimesh.set_instance_transform(i, transform)

		# Vary color slightly
		var color = Color(0.133, 0.545, 0.133)
		var variation = rng.randf_range(-0.1, 0.1)
		color = color.lightened(variation)
		multimesh.set_instance_color(i, color)

	# Create instance
	tree_multimesh = MultiMeshInstance3D.new()
	tree_multimesh.multimesh = multimesh
	tree_multimesh.material_override = tree_material
	tree_multimesh.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	tree_multimesh.name = "Trees"

	add_child(tree_multimesh)


func _build_rocks(rocks: Array) -> void:
	# Create rock mesh (deformed icosahedron)
	var rock_mesh = _create_rock_mesh()

	# Create MultiMesh
	var multimesh = MultiMesh.new()
	multimesh.transform_format = MultiMesh.TRANSFORM_3D
	multimesh.use_colors = true
	multimesh.mesh = rock_mesh
	multimesh.instance_count = rocks.size()

	# Set transforms and colors
	var rng = RandomNumberGenerator.new()
	rng.seed = 54321  # Deterministic for consistent colors

	for i in range(rocks.size()):
		var feature = rocks[i]
		var transform = Transform3D()
		transform = transform.rotated(Vector3.UP, feature.rotation)
		transform = transform.scaled(Vector3.ONE * feature.scale)
		transform.origin = feature.position
		multimesh.set_instance_transform(i, transform)

		# Vary color slightly
		var color = Color(0.412, 0.412, 0.412)
		var variation = rng.randf_range(-0.15, 0.15)
		color = color.lightened(variation)
		multimesh.set_instance_color(i, color)

	# Create instance
	rock_multimesh = MultiMeshInstance3D.new()
	rock_multimesh.multimesh = multimesh
	rock_multimesh.material_override = rock_material
	rock_multimesh.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	rock_multimesh.name = "Rocks"

	add_child(rock_multimesh)


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


## Update visibility based on camera distance
func update_visibility(camera_distance: float) -> void:
	var show_features = camera_distance < 75.0

	if tree_multimesh:
		tree_multimesh.visible = show_features
	if rock_multimesh:
		rock_multimesh.visible = show_features


## Clean up resources
func dispose() -> void:
	if tree_multimesh:
		tree_multimesh.queue_free()
		tree_multimesh = null
	if rock_multimesh:
		rock_multimesh.queue_free()
		rock_multimesh = null
