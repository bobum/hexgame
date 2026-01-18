using Godot;
using Godot.Collections;


//# Renders water surface for underwater cells

//# Matches web/src/rendering/WaterRenderer.ts
[GlobalClass]
public partial class WaterRenderer : Godot.RefCounted
{
	public const double WATER_SURFACE_OFFSET = 0.12;
	// Above terrain to cover hex tops cleanly
	public const Color DEEP_COLOR = new Color(0.102, 0.298, 0.431);
	// 0x1a4c6e
	public const Color SHALLOW_COLOR = new Color(0.176, 0.545, 0.788);


	// 0x2d8bc9
	//# Build water mesh for all underwater cells
	public static Godot.ArrayMesh BuildWaterMesh(Godot.HexGrid grid)
	{
		var st = SurfaceTool.New();
		st.Begin(Mesh.PrimitiveType.PrimitiveTriangles);

		var corners = HexMetrics.GetCorners();
		var water_cell_count = 0;

		// Water renders at sea level Y position
		var water_y = HexMetrics.SEA_LEVEL * HexMetrics.ELEVATION_STEP + WATER_SURFACE_OFFSET;

		foreach(HexCell cell in grid.GetAllCells())
		{
			if(cell.Elevation < HexMetrics.LAND_MIN_ELEVATION)
			{
				// Underwater (0-4)
				water_cell_count += 1;
				var center = cell.GetWorldPosition();

				// Place water at fixed Y level (sea level)
				center.Y = water_y;


				// Color based on depth (distance below sea level)
				var depth = HexMetrics.SEA_LEVEL - cell.Elevation;
				var depth_factor = Mathf.Clamp(Float(depth) / 3.0, 0.0, 1.0);
				var color = SHALLOW_COLOR.Lerp(DEEP_COLOR, depth_factor);


				// Build hexagonal water surface
				foreach(int i in GD.Range(6))
				{
					var c1 = corners[i];
					var c2 = corners[(i + 1) % 6];

					var v1 = center;
					var v2 = new Vector3(center.X + c1.X, center.Y, center.Z + c1.Z);
					var v3 = new Vector3(center.X + c2.X, center.Y, center.Z + c2.Z);


					// Add triangle with normal pointing up
					st.SetNormal(Vector3.Up);
					st.SetColor(color);
					st.AddVertex(v1);
					st.SetColor(color);
					st.AddVertex(v2);
					st.SetColor(color);
					st.AddVertex(v3);
				}
			}
		}

		if(water_cell_count == 0)
		{
			return null;
		}

		GD.Print("Built water mesh: %d cells" % water_cell_count);
		return st.Commit();
	}


	//# Create water material with transparency and optional animation
	public static Godot.ShaderMaterial CreateWaterMaterial()
	{
		var shader = Shader.New();
		shader.Code = @"
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
";

		var material = ShaderMaterial.New();
		material.Shader = shader;
		material.SetShaderParameter("time", 0.0);
		material.SetShaderParameter("wave_amplitude", 0.03);
		material.SetShaderParameter("wave_frequency", 2.0);
		material.SetShaderParameter("alpha", 0.85);

		return material;
	}


	//# Create simple water material without shader (fallback)
	public static Godot.StandardMaterial3D CreateSimpleWaterMaterial()
	{
		var material = StandardMaterial3D.New();
		material.VertexColorUseAsAlbedo = true;
		material.Transparency = BaseMaterial3D.Transparency.TransparencyAlpha;
		material.AlbedoColor = new Color(SHALLOW_COLOR.R, SHALLOW_COLOR.G, SHALLOW_COLOR.B, 0.85);
		material.CullMode = BaseMaterial3D.CullMode.CullDisabled;
		material.ShadingMode = BaseMaterial3D.ShadingMode.ShadingModePerVertex;
		return material;
	}


}