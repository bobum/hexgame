using Godot;
using Godot.Collections;


//# Renders rivers using chunked meshes for distance culling

//# Rivers follow hex edges, rendered per chunk
[GlobalClass]
public partial class ChunkedRiverRenderer : Godot.Node3D
{
	public const double CHUNK_SIZE = 16.0;
	public const double MAX_RENDER_DISTANCE = 50.0;
	public const double RIVER_WIDTH = 0.15;
	public const double HEIGHT_OFFSET = 0.02;

	public Godot.HexGrid Grid;
	public Godot.ShaderMaterial Material;
	public double Time = 0.0;


	// Chunk storage
	public Dictionary Chunks = new Dictionary{};


	// Direction to corner mapping
	public const Array<Vector2i> DIRECTION_TO_CORNERS = new Array{
			new Vector2i(5, 0), new Vector2i(4, 5), new Vector2i(3, 4), 
			new Vector2i(2, 3), new Vector2i(1, 2), new Vector2i(0, 1), 
			};


	public partial class RiverChunk : Godot.Object
	{

		public Godot.MeshInstance3D MeshInstance;
		public int ChunkX = 0;
		public int ChunkZ = 0;
		public Vector3 Center = Vector3.Zero;
	}


	protected String _GetChunkKey(int cx, int cz)
	{
		return "%d,%d" % new Array{cx, cz, };
	}


	protected Vector2i _GetCellChunkCoords(Godot.HexCell cell)
	{
		var coords = HexCoordinates.New(cell.Q, cell.R);
		var world_pos = coords.ToWorldPosition(0);
		return new Vector2i(Int(Mathf.Floor(world_pos.X / CHUNK_SIZE)), Int(Mathf.Floor(world_pos.Z / CHUNK_SIZE)));
	}


	protected Vector3 _GetChunkCenter(int cx, int cz)
	{
		return new Vector3((cx + 0.5) * CHUNK_SIZE, 0, (cz + 0.5) * CHUNK_SIZE);
	}


	public void Setup(Godot.HexGrid p_grid)
	{
		Grid = p_grid;
	}


	public void Build()
	{
		Dispose();
		Material = _CreateRiverMaterial();


		// Build incoming rivers map
		var incoming_rivers = new Dictionary{};
		foreach(HexCell cell in Grid.GetAllCells())
		{
			foreach(int dir in cell.RiverDirections)
			{
				var neighbor = Grid.GetNeighbor(cell, dir);
				if(neighbor)
				{
					var key = "%d,%d" % new Array{neighbor.Q, neighbor.R, };
					if(!incoming_rivers.ContainsKey(key))
					{
						incoming_rivers[key] = new Array{};
					}
					incoming_rivers[key].Append(HexDirection.Opposite(dir));
				}
			}
		}


		// Group river cells by chunk
		var chunk_river_cells = new Dictionary{};
		foreach(HexCell cell in Grid.GetAllCells())
		{
			var outgoing = cell.RiverDirections;
			var incoming_key = "%d,%d" % new Array{cell.Q, cell.R, };
			var incoming = incoming_rivers.Get(incoming_key, new Array{});

			if(outgoing.IsEmpty() && incoming.IsEmpty())
			{
				continue;
			}

			var chunk_coords = _GetCellChunkCoords(cell);
			var key = _GetChunkKey(chunk_coords.X, chunk_coords.Y);

			if(!Chunks.ContainsKey(key))
			{
				var new_chunk = RiverChunk.New();
				new_chunk.ChunkX = chunk_coords.X;
				new_chunk.ChunkZ = chunk_coords.Y;
				new_chunk.Center = _GetChunkCenter(chunk_coords.X, chunk_coords.Y);
				Chunks[key] = new_chunk;
			}

			if(!chunk_river_cells.ContainsKey(key))
			{
				chunk_river_cells[key] = new Array{};
			}
			chunk_river_cells[key].Append(new Dictionary{
							{"cell", cell},
							{"outgoing", outgoing},
							{"incoming", incoming},
							});
		}


		// Build mesh for each chunk
		var total_verts = 0;
		foreach(Dictionary key in Chunks)
		{
			var chunk = Chunks[key];
			var river_data = chunk_river_cells.Get(key, new Array{});
			if(river_data.Size() > 0)
			{
				var mesh = _BuildRiverMesh(river_data);
				if(mesh)
				{
					chunk.MeshInstance = MeshInstance3D.New();
					chunk.MeshInstance.Mesh = mesh;
					chunk.MeshInstance.MaterialOverride = Material;
					chunk.MeshInstance.Name = "River_%s" % key;
					AddChild(chunk.MeshInstance);
					total_verts += mesh.GetFaces().Size();
				}
			}
		}

		GD.Print("Built river mesh: %d vertices" % total_verts);
	}


	protected Godot.ArrayMesh _BuildRiverMesh(Array river_data)
	{
		var vertices = new PackedVector3Array();
		var uvs = new PackedVector2Array();
		var indices = new PackedInt32Array();
		var vertex_index = 0;

		var corners = HexMetrics.GetCorners();
		var half_width = RIVER_WIDTH;
		var rendered_edges = new Dictionary{};

		foreach(Variant data in river_data)
		{
			var cell = data["cell"];
			var outgoing = data["outgoing"];
			var incoming = data["incoming"];

			var center_pos = cell.GetWorldPosition();
			var world_corners = new Array{};
			foreach(Vector3 c in corners)
			{
				world_corners.Append(new Vector3(center_pos.X + c.X, 0, center_pos.Z + c.Z));
			}


			// Draw outgoing river edges
			foreach(Variant out_dir in outgoing)
			{
				var neighbor = Grid.GetNeighbor(cell, out_dir);
				if(!neighbor)
				{
					continue;
				}

				var edge_key = _GetEdgeKey(cell.Q, cell.R, neighbor.Q, neighbor.R);
				if(rendered_edges.ContainsKey(edge_key))
				{
					continue;
				}
				rendered_edges[edge_key] = true;

				var corner_pair = DIRECTION_TO_CORNERS[out_dir];
				var c1 = world_corners[corner_pair.X];
				var c2 = world_corners[corner_pair.Y];

				var edge_dx = c2.X - c1.X;
				var edge_dz = c2.Z - c1.Z;
				var edge_len = Mathf.Sqrt(edge_dx * edge_dx + edge_dz * edge_dz);
				var perp_x =  - edge_dz / edge_len * half_width;
				var perp_z = edge_dx / edge_len * half_width;

				var high_y = Mathf.Max(cell.Elevation, neighbor.Elevation) * HexMetrics.ELEVATION_STEP + HEIGHT_OFFSET;
				var low_y = Mathf.Min(cell.Elevation, neighbor.Elevation) * HexMetrics.ELEVATION_STEP + HEIGHT_OFFSET;


				// Horizontal quad
				vertices.Append(new Vector3(c1.X - perp_x, high_y, c1.Z - perp_z));
				vertices.Append(new Vector3(c1.X + perp_x, high_y, c1.Z + perp_z));
				vertices.Append(new Vector3(c2.X + perp_x, high_y, c2.Z + perp_z));
				vertices.Append(new Vector3(c2.X - perp_x, high_y, c2.Z - perp_z));
				uvs.Append(new Vector2(0, 0));uvs.Append(new Vector2(1, 0));
				uvs.Append(new Vector2(1, 1));uvs.Append(new Vector2(0, 1));
				indices.Append(vertex_index);indices.Append(vertex_index + 1);indices.Append(vertex_index + 2);
				indices.Append(vertex_index);indices.Append(vertex_index + 2);indices.Append(vertex_index + 3);
				vertex_index += 4;


				// Waterfall if downhill
				if(cell.Elevation > neighbor.Elevation)
				{
					var neighbor_has_river = !neighbor.RiverDirections.IsEmpty() || neighbor.Elevation < HexMetrics.SEA_LEVEL;
					if(neighbor_has_river)
					{
						var edge_norm_x = edge_dx / edge_len * half_width;
						var edge_norm_z = edge_dz / edge_len * half_width;
						vertices.Append(new Vector3(c2.X - edge_norm_x, high_y, c2.Z - edge_norm_z));
						vertices.Append(new Vector3(c2.X + edge_norm_x, high_y, c2.Z + edge_norm_z));
						vertices.Append(new Vector3(c2.X + edge_norm_x, low_y, c2.Z + edge_norm_z));
						vertices.Append(new Vector3(c2.X - edge_norm_x, low_y, c2.Z - edge_norm_z));
						uvs.Append(new Vector2(0, 0));uvs.Append(new Vector2(1, 0));
						uvs.Append(new Vector2(1, 1));uvs.Append(new Vector2(0, 1));
						indices.Append(vertex_index);indices.Append(vertex_index + 1);indices.Append(vertex_index + 2);
						indices.Append(vertex_index);indices.Append(vertex_index + 2);indices.Append(vertex_index + 3);
						vertex_index += 4;
					}
				}
			}


			// Connecting paths for through-flow
			if(!incoming.IsEmpty() && !outgoing.IsEmpty())
			{
				foreach(Variant in_dir in incoming)
				{
					foreach(Variant out_dir in outgoing)
					{
						var in_corners = DIRECTION_TO_CORNERS[in_dir];
						var out_corners = DIRECTION_TO_CORNERS[out_dir];
						var path = _FindCornerPath(in_corners.X, out_corners.X);

						if(path.Size() >= 2)
						{
							var y = cell.Elevation * HexMetrics.ELEVATION_STEP + HEIGHT_OFFSET;
							foreach(int i in GD.Range(path.Size() - 1))
							{
								var p1 = world_corners[path[i]];
								var p2 = world_corners[path[i + 1]];
								var seg_dx = p2.X - p1.X;
								var seg_dz = p2.Z - p1.Z;
								var seg_len = Mathf.Sqrt(seg_dx * seg_dx + seg_dz * seg_dz);
								if(seg_len < 0.001)
								{
									continue;
								}
								var s_perp_x =  - seg_dz / seg_len * half_width;
								var s_perp_z = seg_dx / seg_len * half_width;

								vertices.Append(new Vector3(p1.X - s_perp_x, y, p1.Z - s_perp_z));
								vertices.Append(new Vector3(p1.X + s_perp_x, y, p1.Z + s_perp_z));
								vertices.Append(new Vector3(p2.X + s_perp_x, y, p2.Z + s_perp_z));
								vertices.Append(new Vector3(p2.X - s_perp_x, y, p2.Z - s_perp_z));
								uvs.Append(new Vector2(0, 0));uvs.Append(new Vector2(1, 0));
								uvs.Append(new Vector2(1, 1));uvs.Append(new Vector2(0, 1));
								indices.Append(vertex_index);indices.Append(vertex_index + 1);indices.Append(vertex_index + 2);
								indices.Append(vertex_index);indices.Append(vertex_index + 2);indices.Append(vertex_index + 3);
								vertex_index += 4;
							}
						}
					}
				}
			}
		}

		if(vertices.IsEmpty())
		{
			return null;
		}

		var arrays = new Array{};
		arrays.Resize(Mesh.ArrayType.ArrayMax);
		arrays[Mesh.ArrayType.ArrayVertex] = vertices;
		arrays[Mesh.ArrayType.ArrayTexUv] = uvs;
		arrays[Mesh.ArrayType.ArrayIndex] = indices;

		var mesh = ArrayMesh.New();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.PrimitiveTriangles, arrays);
		return mesh;
	}


	protected Array<int> _FindCornerPath(int start, int end)
	{
		if(start == end)
		{
			return new Array{start, };
		}
		var cw = new Array{start, };
		var curr = start;
		while(curr != end && cw.Size() <= 6)
		{
			curr = (curr + 1) % 6;
			cw.Append(curr);
		}
		var ccw = new Array{start, };
		curr = start;
		while(curr != end && ccw.Size() <= 6)
		{
			curr = (curr + 5) % 6;
			ccw.Append(curr);
		}
		return ( cw.Size() <= ccw.Size() ? cw : ccw );
	}


	protected String _GetEdgeKey(int q1, int r1, int q2, int r2)
	{
		if(q1 < q2 || (q1 == q2 && r1 < r2))
		{
			return "%d,%d-%d,%d" % new Array{q1, r1, q2, r2, };
		}
		return "%d,%d-%d,%d" % new Array{q2, r2, q1, r1, };
	}


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
			chunk.MeshInstance.Visible = (dx * dx + dz * dz) <= max_dist_sq;
		}
	}


	public void UpdateAnimation(double delta)
	{
		Time += delta;
		if(Material)
		{
			Material.SetShaderParameter("time", Time);
		}
	}


	protected Godot.ShaderMaterial _CreateRiverMaterial()
	{
		var mat = ShaderMaterial.New();
		mat.Shader = _CreateRiverShader();
		mat.SetShaderParameter("river_color", new Color(0.176, 0.545, 0.788));
		mat.SetShaderParameter("river_color_deep", new Color(0.102, 0.361, 0.557));
		mat.SetShaderParameter("flow_speed", 1.5);
		mat.SetShaderParameter("time", 0.0);
		return mat;
	}


	protected Godot.Shader _CreateRiverShader()
	{
		var shader = Shader.New();
		shader.Code = @"
shader_type spatial;
render_mode blend_mix, cull_disabled, depth_draw_opaque;

uniform vec3 river_color : source_color = vec3(0.176, 0.545, 0.788);
uniform vec3 river_color_deep : source_color = vec3(0.102, 0.361, 0.557);
uniform float flow_speed = 1.5;
uniform float time = 0.0;

varying vec2 world_uv;

float noise(vec2 p) {
	return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

void vertex() {
	world_uv = UV;
}

void fragment() {
	vec2 flow_uv = world_uv;
	flow_uv.y -= time * flow_speed;
	float ripple = noise(flow_uv * 10.0 + time) + noise(flow_uv * 5.0 - time * 0.5) * 0.5;
	ripple = ripple * 0.15;
	float color_mix = sin(VERTEX.x * 0.5 + VERTEX.z * 0.3) * 0.5 + 0.5;
	vec3 base_color = mix(river_color, river_color_deep, color_mix * 0.3);
	vec3 color = base_color + vec3(ripple * 0.2);
	float edge_fade = smoothstep(0.0, 0.15, world_uv.x) * smoothstep(1.0, 0.85, world_uv.x);
	ALBEDO = color;
	ALPHA = 0.85 * edge_fade;
}
";
		return shader;
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