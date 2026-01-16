class_name WaterRenderer
extends RefCounted
## Renders water surface for underwater cells
## Matches web/src/rendering/WaterRenderer.ts

const WATER_SURFACE_OFFSET: float = 0.12  # Above terrain to cover hex tops cleanly
const DEEP_COLOR: Color = Color(0.102, 0.298, 0.431)  # 0x1a4c6e
const SHALLOW_COLOR: Color = Color(0.176, 0.545, 0.788)  # 0x2d8bc9


## Build water mesh for all underwater cells
static func build_water_mesh(grid: HexGrid) -> ArrayMesh:
	var st = SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)

	var corners = HexMetrics.get_corners()
	var water_cell_count = 0
	# Water renders at sea level Y position
	var water_y = HexMetrics.SEA_LEVEL * HexMetrics.ELEVATION_STEP + WATER_SURFACE_OFFSET

	for cell in grid.get_all_cells():
		if cell.elevation < HexMetrics.LAND_MIN_ELEVATION:  # Underwater (0-4)
			water_cell_count += 1
			var center = cell.get_world_position()
			# Place water at fixed Y level (sea level)
			center.y = water_y

			# Color based on depth (distance below sea level)
			var depth = HexMetrics.SEA_LEVEL - cell.elevation
			var depth_factor = clampf(float(depth) / 3.0, 0.0, 1.0)
			var color = SHALLOW_COLOR.lerp(DEEP_COLOR, depth_factor)

			# Build hexagonal water surface
			for i in range(6):
				var c1 = corners[i]
				var c2 = corners[(i + 1) % 6]

				var v1 = center
				var v2 = Vector3(center.x + c1.x, center.y, center.z + c1.z)
				var v3 = Vector3(center.x + c2.x, center.y, center.z + c2.z)

				# Add triangle with normal pointing up
				st.set_normal(Vector3.UP)
				st.set_color(color)
				st.add_vertex(v1)
				st.set_color(color)
				st.add_vertex(v2)
				st.set_color(color)
				st.add_vertex(v3)

	if water_cell_count == 0:
		return null

	print("Built water mesh: %d cells" % water_cell_count)
	return st.commit()


## Create water material with transparency and optional animation
static func create_water_material() -> ShaderMaterial:
	var shader = Shader.new()
	shader.code = """
shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_disabled;

uniform float time = 0.0;
uniform float wave_amplitude = 0.03;
uniform float wave_frequency = 2.0;
uniform float alpha = 0.85;

void vertex() {
	// Subtle wave animation
	VERTEX.y += sin(VERTEX.x * wave_frequency + time) * wave_amplitude;
	VERTEX.y += sin(VERTEX.z * wave_frequency * 0.8 + time * 0.8) * wave_amplitude;
}

void fragment() {
	// Use vertex color for base
	ALBEDO = COLOR.rgb;

	// Add subtle color variation based on position
	float variation = sin(VERTEX.x * 0.5 + time * 0.5) * 0.1;
	ALBEDO = mix(ALBEDO, ALBEDO * 1.2, variation);

	// Transparency
	ALPHA = alpha;

	// Slight roughness for water look
	ROUGHNESS = 0.3;
	METALLIC = 0.0;
}
"""

	var material = ShaderMaterial.new()
	material.shader = shader
	material.set_shader_parameter("time", 0.0)
	material.set_shader_parameter("wave_amplitude", 0.03)
	material.set_shader_parameter("wave_frequency", 2.0)
	material.set_shader_parameter("alpha", 0.85)

	return material


## Create simple water material without shader (fallback)
static func create_simple_water_material() -> StandardMaterial3D:
	var material = StandardMaterial3D.new()
	material.vertex_color_use_as_albedo = true
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	material.albedo_color = Color(SHALLOW_COLOR.r, SHALLOW_COLOR.g, SHALLOW_COLOR.b, 0.85)
	material.cull_mode = BaseMaterial3D.CULL_DISABLED
	material.shading_mode = BaseMaterial3D.SHADING_MODE_PER_VERTEX
	return material
