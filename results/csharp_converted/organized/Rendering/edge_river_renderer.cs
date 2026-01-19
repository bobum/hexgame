using Godot;
using Godot.Collections;


//# Renders rivers as edge-based meshes with animated flow.
//# Rivers follow hex boundaries, tracing along edges between hexes.

//# Matches web/src/rendering/EdgeRiverRenderer.ts
[GlobalClass]
public partial class EdgeRiverRenderer : Godot.Node3D
{
	public Godot.HexGrid Grid;
	public Godot.MeshInstance3D MeshInstance;
	public Godot.ShaderMaterial Material;
	public double Time = 0.0;


	// River rendering constants
	public const double RIVER_WIDTH = 0.15;
	public const double HEIGHT_OFFSET = 0.02;


	// Direction to corner indices mapping (same as web version)
	// Each direction maps to the two corners that form that edge
	public const Array<Vector2i> DIRECTION_TO_CORNERS = new Array{
			new Vector2i(5, 0), 
			// NE: corners 5->0
			new Vector2i(4, 5), 
			// E:  corners 4->5
			new Vector2i(3, 4), 
			// SE: corners 3->4
			new Vector2i(2, 3), 
			// SW: corners 2->3
			new Vector2i(1, 2), 
			// W:  corners 1->2
			new Vector2i(0, 1), 
			// NW: corners 0->1
			};


	public void Setup(Godot.HexGrid p_grid)
	{
		Grid = p_grid;
	}


	public void Build()
	{

		// Remove existing mesh
		if(MeshInstance)
		{
			MeshInstance.QueueFree();
			MeshInstance = null;
		}

		var vertices = new PackedVector3Array();
		var uvs = new PackedVector2Array();
		var indices = new PackedInt32Array();
		var vertex_index = 0;

		var corners = HexMetrics.GetCorners();
		var half_width = RIVER_WIDTH;


		// Build a map of incoming river directions for each cell
		var incoming_rivers = new Dictionary{};
		// "q,r" -> Array of directions
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


		// Track rendered edges to avoid duplicates
		var rendered_edges = new Dictionary{};


		// Process each cell that has rivers
		foreach(HexCell cell in Grid.GetAllCells())
		{
			var outgoing = cell.RiverDirections;
			var incoming_key = "%d,%d" % new Array{cell.Q, cell.R, };
			var incoming = incoming_rivers.Get(incoming_key, new Array{});

			if(outgoing.IsEmpty() && incoming.IsEmpty())
			{
				continue;
			}

			var center_pos = cell.GetWorldPosition();


			// Get world positions of all 6 corners for this cell
			var world_corners = new Array{};
			foreach(Vector3 c in corners)
			{


				world_corners.Append(new Vector3(, center_pos.X + c.X, 0, center_pos.Z + c.Z));
			}


			// For each outgoing river, draw the edge quad
			foreach(int out_dir in outgoing)
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


				// Get the corners for this edge
				var corner_pair = DIRECTION_TO_CORNERS[out_dir];
				var c1 = world_corners[corner_pair.X];
				var c2 = world_corners[corner_pair.Y];


				// Calculate edge midpoint and perpendicular
				var edge_dx = c2.X - c1.X;
				var edge_dz = c2.Z - c1.Z;
				var edge_len = Mathf.Sqrt(edge_dx * edge_dx + edge_dz * edge_dz);
				var perp_x =  - edge_dz / edge_len * half_width;
				var perp_z = edge_dx / edge_len * half_width;


				// Y positions for both cells
				var high_y = Mathf.Max(cell.Elevation, neighbor.Elevation) * HexMetrics.ELEVATION_STEP + HEIGHT_OFFSET;
				var low_y = Mathf.Min(cell.Elevation, neighbor.Elevation) * HexMetrics.ELEVATION_STEP + HEIGHT_OFFSET;
				var elevation_diff = cell.Elevation - neighbor.Elevation;


				// Draw horizontal quad along the edge (at higher elevation)
				vertices.Append(new Vector3(c1.X - perp_x, high_y, c1.Z - perp_z));
				vertices.Append(new Vector3(c1.X + perp_x, high_y, c1.Z + perp_z));
				vertices.Append(new Vector3(c2.X + perp_x, high_y, c2.Z + perp_z));
				vertices.Append(new Vector3(c2.X - perp_x, high_y, c2.Z - perp_z));

				uvs.Append(new Vector2(0, 0));
				uvs.Append(new Vector2(1, 0));
				uvs.Append(new Vector2(1, 1));
				uvs.Append(new Vector2(0, 1));

				indices.Append(vertex_index);
				indices.Append(vertex_index + 1);
				indices.Append(vertex_index + 2);
				indices.Append(vertex_index);
				indices.Append(vertex_index + 2);
				indices.Append(vertex_index + 3);
				vertex_index += 4;


				// If river flows downhill, draw waterfall
				if(elevation_diff > 0)
				{
					var neighbor_has_river = !neighbor.RiverDirections.IsEmpty() || neighbor.Elevation < HexMetrics.SEA_LEVEL;

					if(neighbor_has_river)
					{

						// Waterfall width along edge direction
						var edge_norm_x = edge_dx / edge_len * half_width;
						var edge_norm_z = edge_dz / edge_len * half_width;


						// Draw waterfall quad
						vertices.Append(new Vector3(c2.X - edge_norm_x, high_y, c2.Z - edge_norm_z));
						vertices.Append(new Vector3(c2.X + edge_norm_x, high_y, c2.Z + edge_norm_z));
						vertices.Append(new Vector3(c2.X + edge_norm_x, low_y, c2.Z + edge_norm_z));
						vertices.Append(new Vector3(c2.X - edge_norm_x, low_y, c2.Z - edge_norm_z));

						uvs.Append(new Vector2(0, 0));
						uvs.Append(new Vector2(1, 0));
						uvs.Append(new Vector2(1, 1));
						uvs.Append(new Vector2(0, 1));

						indices.Append(vertex_index);
						indices.Append(vertex_index + 1);
						indices.Append(vertex_index + 2);
						indices.Append(vertex_index);
						indices.Append(vertex_index + 2);
						indices.Append(vertex_index + 3);
						vertex_index += 4;
					}
				}
			}


			// If this cell has both incoming and outgoing rivers, draw connecting path
			if(!incoming.IsEmpty() && !outgoing.IsEmpty())
			{
				foreach(Variant in_dir in incoming)
				{
					foreach(int out_dir in outgoing)
					{

						// Get corners for incoming and outgoing edges
						var in_corners = DIRECTION_TO_CORNERS[in_dir];
						var out_corners = DIRECTION_TO_CORNERS[out_dir];


						// Find path from incoming edge START to outgoing edge START
						var path_corners = _FindCornerPath(in_corners.X, out_corners.X);

						if(path_corners.Size() >= 2)
						{
							var y = cell.Elevation * HexMetrics.ELEVATION_STEP + HEIGHT_OFFSET;


							// Draw quads along the corner path
							foreach(int i in GD.Range(path_corners.Size() - 1))
							{
								var c_idx1 = path_corners[i];
								var c_idx2 = path_corners[i + 1];
								var p1 = world_corners[c_idx1];
								var p2 = world_corners[c_idx2];


								// Calculate perpendicular for this segment
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

								uvs.Append(new Vector2(0, 0));
								uvs.Append(new Vector2(1, 0));
								uvs.Append(new Vector2(1, 1));
								uvs.Append(new Vector2(0, 1));

								indices.Append(vertex_index);
								indices.Append(vertex_index + 1);
								indices.Append(vertex_index + 2);
								indices.Append(vertex_index);
								indices.Append(vertex_index + 2);
								indices.Append(vertex_index + 3);
								vertex_index += 4;
							}
						}
					}
				}
			}
		}

		if(vertices.IsEmpty())
		{
			GD.Print("No river geometry to build");
			return ;
		}


		// Create mesh
		var arrays = new Array{};
		arrays.Resize(Mesh.ArrayType.ArrayMax);
		arrays[Mesh.ArrayType.ArrayVertex] = vertices;
		arrays[Mesh.ArrayType.ArrayTexUv] = uvs;
		arrays[Mesh.ArrayType.ArrayIndex] = indices;

		var mesh = ArrayMesh.New();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.PrimitiveTriangles, arrays);


		// Create mesh instance
		MeshInstance = MeshInstance3D.New();
		MeshInstance.Mesh = mesh;
		MeshInstance.MaterialOverride = _CreateRiverMaterial();
		AddChild(MeshInstance);

		GD.Print("Built river mesh: %d vertices" % vertices.Size());
	}


	protected Godot.ShaderMaterial _CreateRiverMaterial()
	{
		Material = ShaderMaterial.New();
		Material.Shader = _CreateRiverShader();
		Material.SetShaderParameter("river_color", new Color(0.176, 0.545, 0.788));
		Material.SetShaderParameter("river_color_deep", new Color(0.102, 0.361, 0.557));
		Material.SetShaderParameter("flow_speed", 1.5);
		Material.SetShaderParameter("time", 0.0);
		return Material;
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

// Simple noise function for water shimmer
float noise(vec2 p) {
	return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

void vertex() {
	world_uv = UV;
}

void fragment() {
	// Flow animation - scroll UV over time
	vec2 flow_uv = world_uv;
	flow_uv.y -= time * flow_speed;

	// Create ripple effect using noise
	float ripple = noise(flow_uv * 10.0 + time);
	ripple += noise(flow_uv * 5.0 - time * 0.5) * 0.5;
	ripple = ripple * 0.15;

	// Color variation based on position
	float color_mix = sin(VERTEX.x * 0.5 + VERTEX.z * 0.3) * 0.5 + 0.5;
	vec3 base_color = mix(river_color, river_color_deep, color_mix * 0.3);

	// Add ripple brightness variation
	vec3 color = base_color + vec3(ripple * 0.2);

	// Edge fade for smoother blending
	float edge_fade = smoothstep(0.0, 0.15, world_uv.x) * smoothstep(1.0, 0.85, world_uv.x);

	ALBEDO = color;
	ALPHA = 0.85 * edge_fade;
}
";
		return shader;
	}


	//# Find the shortest path of corner indices from start to end.
	protected Array<int> _FindCornerPath(int start_corner, int end_corner)
	{
		if(start_corner == end_corner)
		{
			return new Array{start_corner, };
		}


		// Try clockwise path
		var cw_path = new Array{start_corner, };
		var current = start_corner;
		while(current != end_corner)
		{
			current = (current + 1) % 6;
			cw_path.Append(current);
			if(cw_path.Size() > 6)
			{
				break;
			}
		}


		// Try counterclockwise path
		var ccw_path = new Array{start_corner, };
		current = start_corner;
		while(current != end_corner)
		{
			current = (current + 5) % 6;
			ccw_path.Append(current);
			if(ccw_path.Size() > 6)
			{
				break;
			}
		}


		// Return shorter path
		return ( cw_path.Size() <= ccw_path.Size() ? cw_path : ccw_path );
	}


	//# Create unique key for an edge between two cells.
	protected String _GetEdgeKey(int q1, int r1, int q2, int r2)
	{
		if(q1 < q2 || (q1 == q2 && r1 < r2))
		{
			return "%d,%d-%d,%d" % new Array{q1, r1, q2, r2, };
		}
		return "%d,%d-%d,%d" % new Array{q2, r2, q1, r1, };
	}


	//# Update river animation.
	public void Update(double delta)
	{
		Time += delta;
		if(Material)
		{
			Material.SetShaderParameter("time", Time);
		}
	}


}