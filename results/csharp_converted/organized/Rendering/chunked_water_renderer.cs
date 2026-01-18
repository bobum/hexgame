using Godot;
using Godot.Collections;


//# Renders water surface using chunked meshes for distance culling

//# Matches terrain chunking for consistent visibility
[GlobalClass]
public partial class ChunkedWaterRenderer : Godot.Node3D
{
	public const double CHUNK_SIZE = 16.0;
	public const double MAX_RENDER_DISTANCE = 50.0;
	// Match terrain culling distance
	public const double WATER_SURFACE_OFFSET = 0.12;
	// Above terrain to cover hex tops cleanly
	public const Color DEEP_COLOR = new Color(0.102, 0.298, 0.431);
	// 0x1a4c6e
	public const Color SHALLOW_COLOR = new Color(0.176, 0.545, 0.788);

	// 0x2d8bc9
	public Godot.ShaderMaterial WaterMaterial;


	// Chunk storage: key -> WaterChunk
	public Dictionary Chunks = new Dictionary{};


	public partial class WaterChunk : Godot.Object
	{

		public Godot.MeshInstance3D MeshInstance;
		public int ChunkX = 0;
		public int ChunkZ = 0;
		public Vector3 Center = Vector3.Zero;
	}


	public override void _Init()
	{
		WaterMaterial = _CreateWaterMaterial();
	}


	protected String _GetChunkKey(int cx, int cz)
	{
		return "%d,%d" % new Array{cx, cz, };
	}


	protected Vector2i _GetCellChunkCoords(Godot.HexCell cell)
	{
		var coords = HexCoordinates.New(cell.Q, cell.R);
		var world_pos = coords.ToWorldPosition(0);
		var cx = Int(Mathf.Floor(world_pos.X / CHUNK_SIZE));
		var cz = Int(Mathf.Floor(world_pos.Z / CHUNK_SIZE));
		return new Vector2i(cx, cz);
	}


	protected Vector3 _GetChunkCenter(int cx, int cz)
	{
		return new Vector3((cx + 0.5) * CHUNK_SIZE, 0, (cz + 0.5) * CHUNK_SIZE);
	}


	//# Build water meshes from grid
	public void Build(Godot.HexGrid grid)
	{
		Dispose();


		// Group water cells by chunk
		var chunk_cells = new Dictionary{};

		// key -> Array of cells
		foreach(HexCell cell in grid.GetAllCells())
		{
			if(cell.Elevation < HexMetrics.LAND_MIN_ELEVATION)
			{
				// Underwater (0-4)
				var chunk_coords = _GetCellChunkCoords(cell);
				var key = _GetChunkKey(chunk_coords.X, chunk_coords.Y);

				if(!Chunks.ContainsKey(key))
				{
					var new_chunk = WaterChunk.New();
					new_chunk.ChunkX = chunk_coords.X;
					new_chunk.ChunkZ = chunk_coords.Y;
					new_chunk.Center = _GetChunkCenter(chunk_coords.X, chunk_coords.Y);
					Chunks[key] = new_chunk;
				}

				if(!chunk_cells.ContainsKey(key))
				{
					chunk_cells[key] = new Array{};
				}
				chunk_cells[key].Append(cell);
			}
		}


		// Build mesh for each chunk
		var total_cells = 0;

		foreach(Dictionary key in Chunks)
		{
			var chunk = Chunks[key];
			var cells = chunk_cells.Get(key, new Array{});

			if(cells.Size() > 0)
			{
				var mesh = _BuildWaterMesh(cells);
				chunk.MeshInstance = MeshInstance3D.New();
				chunk.MeshInstance.Mesh = mesh;
				chunk.MeshInstance.MaterialOverride = WaterMaterial;
				chunk.MeshInstance.Name = "Water_%s" % key;
				AddChild(chunk.MeshInstance);
				total_cells += cells.Size();
			}
		}

		GD.Print("Built water mesh: %d cells" % total_cells);
	}


	protected Godot.ArrayMesh _BuildWaterMesh(Array cells)
	{
		var st = SurfaceTool.New();
		st.Begin(Mesh.PrimitiveType.PrimitiveTriangles);
		var corners = HexMetrics.GetCorners();

		// Water renders at sea level Y position
		var water_y = HexMetrics.SEA_LEVEL * HexMetrics.ELEVATION_STEP + WATER_SURFACE_OFFSET;

		foreach(Variant cell in cells)
		{
			var center = cell.GetWorldPosition();
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

				st.SetNormal(Vector3.Up);
				st.SetColor(color);
				st.AddVertex(center);
				st.SetColor(color);
				st.AddVertex(new Vector3(center.X + c1.X, center.Y, center.Z + c1.Z));
				st.SetColor(color);
				st.AddVertex(new Vector3(center.X + c2.X, center.Y, center.Z + c2.Z));
			}
		}

		return st.Commit();
	}


	//# Update visibility based on camera - hide chunks beyond render distance
	public void Update(Godot.Camera3D camera)
	{
		if(!camera)
		{
			return ;
		}

		var camera_pos = camera.GlobalPosition;

		// Use camera XZ position for distance (matches terrain renderer)
		var camera_xz = new Vector3(camera_pos.X, 0, camera_pos.Z);

		var max_dist_sq = MAX_RENDER_DISTANCE * MAX_RENDER_DISTANCE;

		foreach(Dictionary key in Chunks)
		{
			var chunk = Chunks[key];
			if(!chunk.MeshInstance)
			{
				continue;
			}

			var dx = chunk.Center.X - camera_xz.X;
			var dz = chunk.Center.Z - camera_xz.Z;
			var dist_sq = dx * dx + dz * dz;

			chunk.MeshInstance.Visible = dist_sq <= max_dist_sq;
		}
	}


	//# Update water animation
	public void UpdateAnimation(double delta)
	{
		if(WaterMaterial)
		{
			var current_time = WaterMaterial.GetShaderParameter("time");
			WaterMaterial.SetShaderParameter("time", current_time + delta);
		}
	}


	protected Godot.ShaderMaterial _CreateWaterMaterial()
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


	public void Dispose()
	{
		foreach(Dictionary key in Chunks)
		{
			var chunk = Chunks[key];
			if(chunk.MeshInstance)
			{
				chunk.MeshInstance.QueueFree();
			}
		}
		Chunks.Clear();
	}


}