using Godot;
using Godot.Collections;


//# Renders hex terrain using a chunk-based system with LOD and culling

//# Matches web/src/rendering/ChunkedTerrainRenderer.ts
[GlobalClass]
public partial class ChunkedTerrainRenderer : Godot.Node3D
{
	public const double CHUNK_SIZE = 16.0;


	// Distance culling - chunks beyond this are hidden (works with fog)
	public const double MAX_RENDER_DISTANCE = 60.0;

	// Beyond fog end
	// LOD distance thresholds (matching Three.js LODDistances)
	public const double LOD_HIGH_TO_MEDIUM = 30.0;
	public const double LOD_MEDIUM_TO_LOW = 60.0;


	// Reference zoom distance for LOD scaling (default camera distance)
	public const double REFERENCE_ZOOM = 30.0;


	// Base Y level for skirts - below minimum terrain elevation
	public const double SKIRT_BASE_Y = HexMetrics.MIN_ELEVATION * HexMetrics.ELEVATION_STEP - 1.0;


	// Material for all terrain meshes
	public Godot.ShaderMaterial TerrainMaterial;
	public Godot.Shader TerrainShader;


	// Chunk storage: key -> TerrainChunk
	public Dictionary Chunks = new Dictionary{};


	// Stats
	public int TotalChunkCount = 0;


	public partial class TerrainChunk : Godot.Object
	{

		public Godot.MeshInstance3D MeshHigh;
		// Full detail
		public Godot.MeshInstance3D MeshMedium;
		// Simplified
		public Godot.MeshInstance3D MeshLow;
		// Very simple
		public Godot.MeshInstance3D MeshSkirt;
		// Always-visible boundary skirt
		public Array<HexCell> Cells = new Array{};
		public int ChunkX = 0;
		public int ChunkZ = 0;
		public Vector3 Center = Vector3.Zero;
	}


	public override void _Init()
	{

		// Load terrain shader with depth bias to prevent z-fighting
		TerrainShader = Load("res://src/rendering/terrain_shader.gdshader");
		TerrainMaterial = ShaderMaterial.New();
		TerrainMaterial.Shader = TerrainShader;
		TerrainMaterial.SetShaderParameter("depth_bias", 0.001);
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


	//# Build terrain from grid
	public void Build(Godot.HexGrid grid)
	{
		Dispose();


		// Group cells into chunks
		foreach(HexCell cell in grid.GetAllCells())
		{
			var chunk_coords = _GetCellChunkCoords(cell);
			var key = _GetChunkKey(chunk_coords.X, chunk_coords.Y);

			if(!Chunks.ContainsKey(key))
			{
				var new_chunk = TerrainChunk.New();
				new_chunk.ChunkX = chunk_coords.X;
				new_chunk.ChunkZ = chunk_coords.Y;
				new_chunk.Center = _GetChunkCenter(chunk_coords.X, chunk_coords.Y);
				Chunks[key] = new_chunk;
			}

			Chunks[key].Cells.Append(cell);
		}


		// Build meshes for all chunks
		foreach(Dictionary key in Chunks)
		{
			var chunk = Chunks[key];
			_BuildChunkMeshes(chunk, grid);
		}

		TotalChunkCount = Chunks.Size();
		GD.Print("Built %d terrain chunks" % TotalChunkCount);
	}


	protected void _BuildChunkMeshes(Godot.TerrainChunk chunk, Godot.HexGrid grid)
	{
		if(chunk.Cells.IsEmpty())
		{
			return ;
		}


		// HIGH detail - full hex with terraces
		var builder_high = HexMeshBuilder.New();
		foreach(HexCell cell in chunk.Cells)
		{
			builder_high.BuildCell(cell, grid);
		}
		var mesh_high = builder_high._CreateMesh();

		chunk.MeshHigh = MeshInstance3D.New();
		chunk.MeshHigh.Mesh = mesh_high;
		chunk.MeshHigh.MaterialOverride = TerrainMaterial;
		chunk.MeshHigh.CastShadow = GeometryInstance3D.ShadowCastingSetting.ShadowCastingSettingOn;
		chunk.MeshHigh.Name = "Chunk_%d_%d_HIGH" % new Array{chunk.ChunkX, chunk.ChunkZ, };
		AddChild(chunk.MeshHigh);


		// MEDIUM detail - flat hexes
		var mesh_medium = _BuildFlatHexMesh(chunk.Cells);
		chunk.MeshMedium = MeshInstance3D.New();
		chunk.MeshMedium.Mesh = mesh_medium;
		chunk.MeshMedium.MaterialOverride = TerrainMaterial;
		chunk.MeshMedium.CastShadow = GeometryInstance3D.ShadowCastingSetting.ShadowCastingSettingOff;
		chunk.MeshMedium.Name = "Chunk_%d_%d_MED" % new Array{chunk.ChunkX, chunk.ChunkZ, };
		chunk.MeshMedium.Visible = false;
		AddChild(chunk.MeshMedium);


		// LOW detail - simple quads (even simpler)
		var mesh_low = _BuildSimpleQuadMesh(chunk.Cells);
		chunk.MeshLow = MeshInstance3D.New();
		chunk.MeshLow.Mesh = mesh_low;
		chunk.MeshLow.MaterialOverride = TerrainMaterial;
		chunk.MeshLow.CastShadow = GeometryInstance3D.ShadowCastingSetting.ShadowCastingSettingOff;
		chunk.MeshLow.Name = "Chunk_%d_%d_LOW" % new Array{chunk.ChunkX, chunk.ChunkZ, };
		chunk.MeshLow.Visible = false;
		AddChild(chunk.MeshLow);


		// SKIRT disabled for now - was causing visual issues


		// TODO: Implement proper edge-only skirts that don't block terrain view

	}//# Build chunk boundary skirt - walls around the outer edges of chunk
	protected Godot.ArrayMesh _BuildChunkBoundarySkirt(Array<HexCell> cells, int chunk_x, int chunk_z)
	{
		if(cells.IsEmpty())
		{
			return null;
		}

		var st = SurfaceTool.New();
		st.Begin(Mesh.PrimitiveType.PrimitiveTriangles);


		// Find the bounding box of this chunk's cells
		var min_x = Mathf.Inf;
		var max_x =  - Mathf.Inf;
		var min_z = Mathf.Inf;
		var max_z =  - Mathf.Inf;
		var avg_y = 0.0;

		foreach(HexCell cell in cells)
		{
			var pos = cell.GetWorldPosition();
			min_x = Mathf.Min(min_x, pos.X - HexMetrics.OUTER_RADIUS);
			max_x = Mathf.Max(max_x, pos.X + HexMetrics.OUTER_RADIUS);
			min_z = Mathf.Min(min_z, pos.Z - HexMetrics.OUTER_RADIUS);
			max_z = Mathf.Max(max_z, pos.Z + HexMetrics.OUTER_RADIUS);
			avg_y += pos.Y;
		}

		avg_y /= cells.Size();


		// Use a neutral color for the skirt (blends with fog)
		var skirt_color = new Color(0.4, 0.45, 0.5);
		// Gray-blue to match fog
		var down_normal = new Vector3(0,  - 1, 0);


		// Build 4 walls around the chunk boundary
		var top_y = avg_y;
		var bottom_y = SKIRT_BASE_Y;


		// Wall vertices (going clockwise when viewed from above)
		var corners = new Array{
					new Vector3(min_x, top_y, min_z), 
					// 0: front-left
					new Vector3(max_x, top_y, min_z), 
					// 1: front-right
					new Vector3(max_x, top_y, max_z), 
					// 2: back-right
					new Vector3(min_x, top_y, max_z), 
					// 3: back-left
					};


		// Build 4 walls
		foreach(int i in GD.Range(4))
		{
			var c1 = corners[i];
			var c2 = corners[(i + 1) % 4];

			var top_left = c1;
			var top_right = c2;
			var bottom_left = new Vector3(c1.X, bottom_y, c1.Z);
			var bottom_right = new Vector3(c2.X, bottom_y, c2.Z);


			// Calculate outward-facing normal for this wall
			var edge = top_right - top_left;
			var down = new Vector3(0,  - 1, 0);
			var wall_normal = edge.Cross(down).Normalized();


			// Two triangles for the quad
			st.SetNormal(wall_normal);
			st.SetColor(skirt_color);
			st.AddVertex(top_left);
			st.SetNormal(wall_normal);
			st.SetColor(skirt_color);
			st.AddVertex(bottom_left);
			st.SetNormal(wall_normal);
			st.SetColor(skirt_color);
			st.AddVertex(bottom_right);

			st.SetNormal(wall_normal);
			st.SetColor(skirt_color);
			st.AddVertex(top_left);
			st.SetNormal(wall_normal);
			st.SetColor(skirt_color);
			st.AddVertex(bottom_right);
			st.SetNormal(wall_normal);
			st.SetColor(skirt_color);
			st.AddVertex(top_right);
		}

		return st.Commit();
	}


	//# Build flat hex mesh (medium LOD) with per-hex skirts
	protected Godot.ArrayMesh _BuildFlatHexMesh(Array<HexCell> cells)
	{
		var st = SurfaceTool.New();
		st.Begin(Mesh.PrimitiveType.PrimitiveTriangles);
		var corners = HexMetrics.GetCorners();
		var up_normal = new Vector3(0, 1, 0);

		foreach(HexCell cell in cells)
		{

			// Skip shallow water cells at sea level - rendered by water surface only
			if(cell.IsUnderwater() && cell.Elevation >= HexMetrics.SEA_LEVEL - 2)
			{
				continue;
			}
			var center = cell.GetWorldPosition();
			var base_color = cell.GetColor();

			// Boost saturation slightly to compensate for lack of terrace detail
			var h = base_color.H;
			var s = Mathf.Min(base_color.S * 1.15, 1.0);
			// 15% saturation boost
			var v = base_color.V;
			var color = Color.FromHsv(h, s, v);

			// Use same color for tops and skirts - shader wall_darkening handles shading
			var skirt_color = color;


			// Build hex top as 6 triangles from center - use flat up normal
			foreach(int i in GD.Range(6))
			{
				var c1 = corners[i];
				var c2 = corners[(i + 1) % 6];

				st.SetNormal(up_normal);
				st.SetColor(color);
				st.AddVertex(center);
				st.SetNormal(up_normal);
				st.SetColor(color);
				st.AddVertex(new Vector3(center.X + c1.X, center.Y, center.Z + c1.Z));
				st.SetNormal(up_normal);
				st.SetColor(color);
				st.AddVertex(new Vector3(center.X + c2.X, center.Y, center.Z + c2.Z));
			}


			// Build hex skirt - 6 quads around perimeter with outward normals
			foreach(int i in GD.Range(6))
			{
				var c1 = corners[i];
				var c2 = corners[(i + 1) % 6];

				var top_left = new Vector3(center.X + c1.X, center.Y, center.Z + c1.Z);
				var top_right = new Vector3(center.X + c2.X, center.Y, center.Z + c2.Z);
				var bottom_left = new Vector3(center.X + c1.X, SKIRT_BASE_Y, center.Z + c1.Z);
				var bottom_right = new Vector3(center.X + c2.X, SKIRT_BASE_Y, center.Z + c2.Z);


				// Calculate outward-facing normal for this edge
				var edge = top_right - top_left;
				var outward = new Vector3(edge.Z, 0,  - edge.X).Normalized();


				// Two triangles for the quad (facing outward)
				st.SetNormal(outward);
				st.SetColor(skirt_color);
				st.AddVertex(top_left);
				st.SetNormal(outward);
				st.SetColor(skirt_color);
				st.AddVertex(bottom_left);
				st.SetNormal(outward);
				st.SetColor(skirt_color);
				st.AddVertex(bottom_right);

				st.SetNormal(outward);
				st.SetColor(skirt_color);
				st.AddVertex(top_left);
				st.SetNormal(outward);
				st.SetColor(skirt_color);
				st.AddVertex(bottom_right);
				st.SetNormal(outward);
				st.SetColor(skirt_color);
				st.AddVertex(top_right);
			}
		}

		return st.Commit();
	}


	//# Build simple quad mesh (low LOD) - one quad per cell with box skirt
	protected Godot.ArrayMesh _BuildSimpleQuadMesh(Array<HexCell> cells)
	{
		var st = SurfaceTool.New();
		st.Begin(Mesh.PrimitiveType.PrimitiveTriangles);
		var up_normal = new Vector3(0, 1, 0);

		foreach(HexCell cell in cells)
		{

			// Skip shallow water cells at sea level - rendered by water surface only
			if(cell.IsUnderwater() && cell.Elevation >= HexMetrics.SEA_LEVEL - 2)
			{
				continue;
			}
			var center = cell.GetWorldPosition();
			var base_color = cell.GetColor();

			// Boost saturation slightly to compensate for lack of terrace detail
			var h = base_color.H;
			var s = Mathf.Min(base_color.S * 1.15, 1.0);
			// 15% saturation boost
			var v = base_color.V;
			var color = Color.FromHsv(h, s, v);

			// Use same color for tops and skirts - shader wall_darkening handles shading
			var skirt_color = color;
			var size = HexMetrics.OUTER_RADIUS * 0.85;

			// Match Three.js
			// Simple quad top with flat up normal
			var v1 = new Vector3(center.X - size, center.Y, center.Z - size);
			var v2 = new Vector3(center.X + size, center.Y, center.Z - size);
			var v3 = new Vector3(center.X + size, center.Y, center.Z + size);
			var v4 = new Vector3(center.X - size, center.Y, center.Z + size);

			st.SetNormal(up_normal);
			st.SetColor(color);
			st.AddVertex(v1);
			st.SetNormal(up_normal);
			st.SetColor(color);
			st.AddVertex(v2);
			st.SetNormal(up_normal);
			st.SetColor(color);
			st.AddVertex(v3);

			st.SetNormal(up_normal);
			st.SetColor(color);
			st.AddVertex(v1);
			st.SetNormal(up_normal);
			st.SetColor(color);
			st.AddVertex(v3);
			st.SetNormal(up_normal);
			st.SetColor(color);
			st.AddVertex(v4);


			// Box skirt - 4 walls around the quad with outward normals
			var quad_corners = new Array{v1, v2, v3, v4, };

			// Outward normals for each wall (in order: -Z, +X, +Z, -X)
			var wall_normals = new Array{
							new Vector3(0, 0,  - 1), 
							new Vector3(1, 0, 0), 
							new Vector3(0, 0, 1), 
							new Vector3( - 1, 0, 0), 
							};
			foreach(int i in GD.Range(4))
			{
				var c1 = quad_corners[i];
				var c2 = quad_corners[(i + 1) % 4];
				var wall_normal = wall_normals[i];

				var top_left = c1;
				var top_right = c2;
				var bottom_left = new Vector3(c1.X, SKIRT_BASE_Y, c1.Z);
				var bottom_right = new Vector3(c2.X, SKIRT_BASE_Y, c2.Z);

				st.SetNormal(wall_normal);
				st.SetColor(skirt_color);
				st.AddVertex(top_left);
				st.SetNormal(wall_normal);
				st.SetColor(skirt_color);
				st.AddVertex(bottom_left);
				st.SetNormal(wall_normal);
				st.SetColor(skirt_color);
				st.AddVertex(bottom_right);

				st.SetNormal(wall_normal);
				st.SetColor(skirt_color);
				st.AddVertex(top_left);
				st.SetNormal(wall_normal);
				st.SetColor(skirt_color);
				st.AddVertex(bottom_right);
				st.SetNormal(wall_normal);
				st.SetColor(skirt_color);
				st.AddVertex(top_right);
			}
		}

		return st.Commit();
	}


	//# Update visibility and LOD based on camera - call every frame
	public void Update(Godot.Camera3D camera)
	{
		var camera_pos = camera.GlobalPosition;


		// Use camera XZ position for distance calculations
		// This ensures foreground terrain (close to camera) always gets HIGH LOD
		var camera_xz = new Vector3(camera_pos.X, 0, camera_pos.Z);


		// No zoom scaling - use fixed LOD distances based on camera position
		// This prevents issues where foreground gets culled/LOD'd incorrectly
		var effective_max_dist = MAX_RENDER_DISTANCE;
		var max_dist_sq = effective_max_dist * effective_max_dist;

		var visible_count = 0;
		var culled_count = 0;

		foreach(Dictionary key in Chunks)
		{
			var chunk = Chunks[key];
			if(!chunk.MeshHigh)
			{
				continue;
			}


			// Horizontal distance from camera to chunk
			var dx = chunk.Center.X - camera_xz.X;
			var dz = chunk.Center.Z - camera_xz.Z;
			var dist_sq = dx * dx + dz * dz;

			if(dist_sq > max_dist_sq)
			{

				// Beyond render distance - hide all LODs
				chunk.MeshHigh.Visible = false;
				chunk.MeshMedium.Visible = false;
				chunk.MeshLow.Visible = false;
				culled_count += 1;
				continue;
			}

			visible_count += 1;
			var dist = Mathf.Sqrt(dist_sq);


			// Debug: print first chunk distance occasionally
			if(key == Chunks.Keys()[0] && Godot.Engine.GetFramesDrawn() % 120 == 0)
			{
				GD.Print("Chunk dist: %.1f, max: %.1f, cam: (%.1f, %.1f)" % new Array{dist, effective_max_dist, camera_xz.X, camera_xz.Z, });
			}


			// LOD selection based on distance from camera
			if(dist < LOD_HIGH_TO_MEDIUM)
			{
				chunk.MeshHigh.Visible = true;
				chunk.MeshMedium.Visible = false;
				chunk.MeshLow.Visible = false;
			}
			else if(dist < LOD_MEDIUM_TO_LOW)
			{
				chunk.MeshHigh.Visible = false;
				chunk.MeshMedium.Visible = true;
				chunk.MeshLow.Visible = false;
			}
			else
			{
				chunk.MeshHigh.Visible = false;
				chunk.MeshMedium.Visible = false;
				chunk.MeshLow.Visible = true;
			}
		}
	}


	public int GetChunkCount()
	{
		return TotalChunkCount;
	}


	public void Dispose()
	{
		foreach(Dictionary key in Chunks)
		{
			var chunk = Chunks[key];
			if(chunk.MeshHigh)
			{
				chunk.MeshHigh.QueueFree();
			}
			if(chunk.MeshMedium)
			{
				chunk.MeshMedium.QueueFree();
			}
			if(chunk.MeshLow)
			{
				chunk.MeshLow.QueueFree();
			}
			if(chunk.MeshSkirt)
			{
				chunk.MeshSkirt.QueueFree();
			}
		}
		Chunks.Clear();
		TotalChunkCount = 0;
	}


}