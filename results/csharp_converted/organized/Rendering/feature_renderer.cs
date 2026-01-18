using Godot;
using Godot.Collections;


//# Renders instanced features (trees, rocks) using chunked MultiMesh for distance culling

//# Features are grouped by chunk to match terrain chunking for consistent visibility
[GlobalClass]
public partial class FeatureRenderer : Godot.Node3D
{
	public const Godot.Resource FeatureClass = /* preload has no equivalent, add a 'ResourcePreloader' Node in your scene */("res://src/core/feature.gd");

	public const double CHUNK_SIZE = 16.0;
	public const double MAX_RENDER_DISTANCE = 50.0;

	// Match terrain culling distance
	public Godot.StandardMaterial3D TreeMaterial;
	public Godot.StandardMaterial3D RockMaterial;


	// Chunk storage: key -> FeatureChunk
	public Dictionary Chunks = new Dictionary{};


	// Shared meshes
	public Godot.ArrayMesh TreeMesh;
	public Godot.ArrayMesh RockMesh;


	public partial class FeatureChunk : Godot.Object
	{

		public Godot.MultiMeshInstance3D TreeMultimesh;
		public Godot.MultiMeshInstance3D RockMultimesh;
		public int ChunkX = 0;
		public int ChunkZ = 0;
		public Vector3 Center = Vector3.Zero;
	}


	public override void _Init()
	{

		// Create materials
		TreeMaterial = StandardMaterial3D.New();
		TreeMaterial.AlbedoColor = new Color(0.133, 0.545, 0.133);
		// Forest green
		TreeMaterial.ShadingMode = BaseMaterial3D.ShadingMode.ShadingModePerVertex;
		TreeMaterial.CullMode = BaseMaterial3D.CullMode.CullBack;

		RockMaterial = StandardMaterial3D.New();
		RockMaterial.AlbedoColor = new Color(0.412, 0.412, 0.412);
		// Dim gray
		RockMaterial.ShadingMode = BaseMaterial3D.ShadingMode.ShadingModePerVertex;
		RockMaterial.CullMode = BaseMaterial3D.CullMode.CullBack;


		// Create shared meshes
		TreeMesh = _CreateTreeMesh();
		RockMesh = _CreateRockMesh();
	}


	protected String _GetChunkKey(int cx, int cz)
	{
		return "%d,%d" % new Array{cx, cz, };
	}


	protected Vector2i _GetFeatureChunkCoords(Vector3 pos)
	{
		var cx = Int(Mathf.Floor(pos.X / CHUNK_SIZE));
		var cz = Int(Mathf.Floor(pos.Z / CHUNK_SIZE));
		return new Vector2i(cx, cz);
	}


	protected Vector3 _GetChunkCenter(int cx, int cz)
	{
		return new Vector3((cx + 0.5) * CHUNK_SIZE, 0, (cz + 0.5) * CHUNK_SIZE);
	}


	//# Build feature meshes from grid
	public void Build(Godot.HexGrid grid)
	{
		Dispose();


		// Group features by chunk
		var chunk_trees = new Dictionary{};
		// key -> Array of features
		var chunk_rocks = new Dictionary{};

		foreach(HexCell cell in grid.GetAllCells())
		{
			foreach(Variant feature in cell.Features)
			{
				var chunk_coords = _GetFeatureChunkCoords(feature.Position);
				var key = _GetChunkKey(chunk_coords.X, chunk_coords.Y);


				// Ensure chunk exists
				if(!Chunks.ContainsKey(key))
				{
					var new_chunk = FeatureChunk.New();
					new_chunk.ChunkX = chunk_coords.X;
					new_chunk.ChunkZ = chunk_coords.Y;
					new_chunk.Center = _GetChunkCenter(chunk_coords.X, chunk_coords.Y);
					Chunks[key] = new_chunk;
				}

				if(feature.Type == FeatureClass.Type.TREE)
				{
					if(!chunk_trees.ContainsKey(key))
					{
						chunk_trees[key] = new Array{};
					}
					chunk_trees[key].Append(feature);
				}
				else if(feature.Type == FeatureClass.Type.ROCK)
				{
					if(!chunk_rocks.ContainsKey(key))
					{
						chunk_rocks[key] = new Array{};
					}
					chunk_rocks[key].Append(feature);
				}
			}
		}


		// Build MultiMesh for each chunk
		var total_trees = 0;
		var total_rocks = 0;

		foreach(Dictionary key in Chunks)
		{
			var chunk = Chunks[key];
			var trees = chunk_trees.Get(key, new Array{});
			var rocks = chunk_rocks.Get(key, new Array{});

			if(trees.Size() > 0)
			{
				chunk.TreeMultimesh = _BuildMultimesh(trees, TreeMesh, TreeMaterial, "Trees_%s" % key);
				AddChild(chunk.TreeMultimesh);
				total_trees += trees.Size();
			}

			if(rocks.Size() > 0)
			{
				chunk.RockMultimesh = _BuildMultimesh(rocks, RockMesh, RockMaterial, "Rocks_%s" % key);
				AddChild(chunk.RockMultimesh);
				total_rocks += rocks.Size();
			}
		}

		GD.Print("Built features: %d trees, %d rocks" % new Array{total_trees, total_rocks, });
	}


	protected Godot.MultiMeshInstance3D _BuildMultimesh(Array features, Godot.ArrayMesh mesh, Godot.StandardMaterial3D material, String name)
	{
		var multimesh = MultiMesh.New();
		multimesh.TransformFormat = MultiMesh.TransformFormat.Transform3d;
		multimesh.UseColors = true;
		multimesh.Mesh = mesh;
		multimesh.InstanceCount = features.Size();

		var rng = RandomNumberGenerator.New();
		rng.Seed = GD.Hash(Name);

		// Deterministic per chunk
		foreach(int i in GD.Range(features.Size()))
		{
			var feature = features[i];
			var transform = new Transform3D();
			Transform = Transform.Rotated(Vector3.Up, feature.Rotation);
			Transform = Transform.Scaled(Vector3.One * feature.Scale);
			Transform.Origin = feature.Position;
			multimesh.SetInstanceTransform(i, Transform);


			// Vary color slightly
			var base_color = material.AlbedoColor;
			var variation = rng.RandfRange( - 0.1, 0.1);
			multimesh.SetInstanceColor(i, base_color.Lightened(variation));
		}

		var instance = MultiMeshInstance3D.New();
		instance.Multimesh = multimesh;
		instance.MaterialOverride = material;
		instance.CastShadow = GeometryInstance3D.ShadowCastingSetting.ShadowCastingSettingOn;
		instance.Name = Name;

		return instance;
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


			// Distance from camera to chunk
			var dx = chunk.Center.X - camera_xz.X;
			var dz = chunk.Center.Z - camera_xz.Z;
			var dist_sq = dx * dx + dz * dz;

			var visible = dist_sq <= max_dist_sq;

			if(chunk.TreeMultimesh)
			{
				chunk.TreeMultimesh.Visible = Visible;
			}
			if(chunk.RockMultimesh)
			{
				chunk.RockMultimesh.Visible = Visible;
			}
		}
	}


	//# Create simple tree mesh (cone for foliage + cylinder trunk)
	protected Godot.ArrayMesh _CreateTreeMesh()
	{
		var st = SurfaceTool.New();
		st.Begin(Mesh.PrimitiveType.PrimitiveTriangles);


		// Cone for foliage
		var cone_height = 0.4;
		var cone_radius = 0.15;
		var cone_y = 0.3;
		var segments = 6;


		// Cone top vertex
		var top = new Vector3(0, cone_y + cone_height, 0);


		// Cone base vertices
		foreach(int i in GD.Range(segments))
		{
			var angle1 = Float(i) / segments * Mathf.Tau;
			var angle2 = Float(i + 1) / segments * Mathf.Tau;

			var v1 = new Vector3(Mathf.Cos(angle1) * cone_radius, cone_y, Mathf.Sin(angle1) * cone_radius);
			var v2 = new Vector3(Mathf.Cos(angle2) * cone_radius, cone_y, Mathf.Sin(angle2) * cone_radius);


			// Side triangle
			st.SetColor(new Color(0.133, 0.545, 0.133));
			st.AddVertex(top);
			st.AddVertex(v1);
			st.AddVertex(v2);
		}


		// Trunk (cylinder)
		var trunk_radius_top = 0.03;
		var trunk_radius_bottom = 0.04;
		var trunk_height = 0.15;

		foreach(int i in GD.Range(segments))
		{
			var angle1 = Float(i) / segments * Mathf.Tau;
			var angle2 = Float(i + 1) / segments * Mathf.Tau;

			var t1 = new Vector3(Mathf.Cos(angle1) * trunk_radius_top, trunk_height, Mathf.Sin(angle1) * trunk_radius_top);
			var t2 = new Vector3(Mathf.Cos(angle2) * trunk_radius_top, trunk_height, Mathf.Sin(angle2) * trunk_radius_top);
			var b1 = new Vector3(Mathf.Cos(angle1) * trunk_radius_bottom, 0, Mathf.Sin(angle1) * trunk_radius_bottom);
			var b2 = new Vector3(Mathf.Cos(angle2) * trunk_radius_bottom, 0, Mathf.Sin(angle2) * trunk_radius_bottom);

			var trunk_color = new Color(0.4, 0.26, 0.13);
			// Brown
			st.SetColor(trunk_color);
			st.AddVertex(t1);
			st.AddVertex(b1);
			st.AddVertex(t2);

			st.AddVertex(t2);
			st.AddVertex(b1);
			st.AddVertex(b2);
		}

		st.GenerateNormals();
		return st.Commit();
	}


	//# Create rock mesh (deformed icosahedron)
	protected Godot.ArrayMesh _CreateRockMesh()
	{

		// Start with an icosahedron base
		var st = SurfaceTool.New();
		st.Begin(Mesh.PrimitiveType.PrimitiveTriangles);


		// Icosahedron vertices (unit)
		var t = (1.0 + Mathf.Sqrt(5.0)) / 2.0;
		var verts = new Array{
					new Vector3( - 1, t, 0).Normalized() * 0.12, 
					new Vector3(1, t, 0).Normalized() * 0.12, 
					new Vector3( - 1,  - t, 0).Normalized() * 0.12, 
					new Vector3(1,  - t, 0).Normalized() * 0.12, 
					new Vector3(0,  - 1, t).Normalized() * 0.12, 
					new Vector3(0, 1, t).Normalized() * 0.12, 
					new Vector3(0,  - 1,  - t).Normalized() * 0.12, 
					new Vector3(0, 1,  - t).Normalized() * 0.12, 
					new Vector3(t, 0,  - 1).Normalized() * 0.12, 
					new Vector3(t, 0, 1).Normalized() * 0.12, 
					new Vector3( - t, 0,  - 1).Normalized() * 0.12, 
					new Vector3( - t, 0, 1).Normalized() * 0.12, 
					};


		// Deform vertices for organic look
		foreach(int i in GD.Range(verts.Size()))
		{
			var v = verts[i];
			var noise = Mathf.Sin(v.X * 10) * Mathf.Cos(v.Z * 10) * 0.3 + 1;

			// Flatten bottom half
			var y_scale = ( v.Y < 0 ? 0.3 : 1.0 );
			verts[i] = new Vector3(v.X * noise, v.Y * noise * y_scale, v.Z * noise);

			// Shift up so bottom sits at y=0
			verts[i].Y += 0.08;
		}


		// Icosahedron faces (20 triangles)
		var faces = new Array{
					new Array{0, 11, 5, }, new Array{0, 5, 1, }, new Array{0, 1, 7, }, new Array{0, 7, 10, }, new Array{0, 10, 11, }, 
					new Array{1, 5, 9, }, new Array{5, 11, 4, }, new Array{11, 10, 2, }, new Array{10, 7, 6, }, new Array{7, 1, 8, }, 
					new Array{3, 9, 4, }, new Array{3, 4, 2, }, new Array{3, 2, 6, }, new Array{3, 6, 8, }, new Array{3, 8, 9, }, 
					new Array{4, 9, 5, }, new Array{2, 4, 11, }, new Array{6, 2, 10, }, new Array{8, 6, 7, }, new Array{9, 8, 1, }, 
					};

		var rock_color = new Color(0.412, 0.412, 0.412);
		foreach(Variant face in faces)
		{
			st.SetColor(rock_color);
			st.AddVertex(verts[face[0]]);
			st.AddVertex(verts[face[1]]);
			st.AddVertex(verts[face[2]]);
		}

		st.GenerateNormals();
		return st.Commit();
	}


	//# Clean up resources
	public void Dispose()
	{
		foreach(Dictionary key in Chunks)
		{
			var chunk = Chunks[key];
			if(chunk.TreeMultimesh)
			{
				chunk.TreeMultimesh.QueueFree();
			}
			if(chunk.RockMultimesh)
			{
				chunk.RockMultimesh.QueueFree();
			}
		}
		Chunks.Clear();
	}


}