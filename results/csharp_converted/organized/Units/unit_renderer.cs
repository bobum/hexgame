using Godot;
using Godot.Collections;


//# Renders units using instanced meshes for performance.

//# Matches web/src/units/UnitRenderer.ts
// Player colors for land units
[GlobalClass]
public partial class UnitRenderer : Godot.Node3D
{
	public const Array<Color> PLAYER_COLORS_LAND = new Array{
			new Color(0.267, 0.533, 1.0), 
			// Player 1: Blue
			new Color(1.0, 0.267, 0.267), 
			// Player 2: Red
			new Color(0.267, 1.0, 0.267), 
			// Player 3: Green
			new Color(1.0, 0.533, 0.267), 
			// Player 4: Orange
			};


	// Player colors for naval units (yellow tones)
	public const Array<Color> PLAYER_COLORS_NAVAL = new Array{
			new Color(1.0, 1.0, 0.267), 
			// Player 1: Yellow
			new Color(1.0, 0.8, 0.0), 
			// Player 2: Gold
			new Color(0.8, 1.0, 0.267), 
			// Player 3: Lime Yellow
			new Color(1.0, 0.667, 0.0), 
			// Player 4: Amber
			};


	// Amphibious units (cyan tones)
	public const Array<Color> PLAYER_COLORS_AMPHIBIOUS = new Array{
			new Color(0.267, 1.0, 1.0), 
			// Player 1: Cyan
			new Color(0.0, 0.8, 0.8), 
			// Player 2: Teal
			new Color(0.267, 0.8, 1.0), 
			// Player 3: Sky Blue
			new Color(0.0, 1.0, 0.8), 
			// Player 4: Aqua
			};

	public const Color SELECTED_COLOR = new Color(1.0, 1.0, 1.0);

	// White for selected
	public Godot.UnitManager UnitManager;
	public Godot.HexGrid Grid;


	// MultiMesh instances per unit type
	public Dictionary Multimeshes = new Dictionary{};

	// UnitTypes.Type -> MultiMeshInstance3D
	// Instance index -> unit ID mapping per type
	public Dictionary UnitIdMaps = new Dictionary{};

	// UnitTypes.Type -> Array[int]
	// Selected unit IDs
	public Dictionary SelectedUnitIds = new Dictionary{};

	// Set<int>
	public bool NeedsRebuild = true;


	// Cached meshes
	protected Godot.Mesh _InfantryMesh;
	protected Godot.Mesh _CavalryMesh;
	protected Godot.Mesh _ArcherMesh;
	protected Godot.Mesh _GalleyMesh;
	protected Godot.Mesh _WarshipMesh;
	protected Godot.Mesh _MarineMesh;


	public override void _Init()
	{

		// Pre-create meshes
		_InfantryMesh = _CreateInfantryMesh();
		_CavalryMesh = _CreateCavalryMesh();
		_ArcherMesh = _CreateArcherMesh();
		_GalleyMesh = _CreateGalleyMesh();
		_WarshipMesh = _CreateWarshipMesh();
		_MarineMesh = _CreateMarineMesh();
	}


	public void Setup(Godot.UnitManager p_unit_manager, Godot.HexGrid p_grid)
	{
		System.Diagnostics.Debug.Assert(p_unit_manager != null, "UnitRenderer requires UnitManager");
		System.Diagnostics.Debug.Assert(p_grid != null, "UnitRenderer requires HexGrid");

		UnitManager = p_unit_manager;
		Grid = p_grid;


		// Disconnect old signals if reconnecting
		if(UnitManager.)
		{
			UnitManager.;
		}
		if(UnitManager.)
		{
			UnitManager.;
		}
		if(UnitManager.)
		{
			UnitManager.;
		}


		// Connect signals
		UnitManager.UnitCreated += _on_unit_created;
		UnitManager.UnitRemoved += _on_unit_removed;
		UnitManager.UnitMoved += _on_unit_moved;
	}


	protected void _OnUnitCreated(Godot.Unit _unit)
	{
		NeedsRebuild = true;
	}


	protected void _OnUnitRemoved(int _unit_id)
	{
		NeedsRebuild = true;
	}


	protected void _OnUnitMoved(Godot.Unit _unit, int _from_q, int _from_r)
	{
		NeedsRebuild = true;
	}


	public void Build()
	{
		_ClearMeshes();

		var units = UnitManager.GetAllUnits();


		// Group units by chunk, then by type within chunk
		var chunks_by_type = new Dictionary{};

		// chunk_key -> { unit_type -> [units] }
		foreach(Unit unit in units)
		{
			var world_pos = unit.GetWorldPosition();
			var cx = Int(Mathf.Floor(world_pos.X / CHUNK_SIZE));
			var cz = Int(Mathf.Floor(world_pos.Z / CHUNK_SIZE));
			var chunk_key = "%d,%d" % new Array{cx, cz, };

			if(!chunks_by_type.ContainsKey(chunk_key))
			{
				chunks_by_type[chunk_key] = new Dictionary{};
				foreach(Variant unit_type in new Array{UnitTypes.Type.INFANTRY, UnitTypes.Type.CAVALRY, UnitTypes.Type.ARCHER, 
									UnitTypes.Type.GALLEY, UnitTypes.Type.WARSHIP, UnitTypes.Type.MARINE, })
				{
					chunks_by_type[chunk_key][unit_type] = new Array{};
				}
			}

			chunks_by_type[chunk_key][unit.Type].Append(unit);
		}


		// Build MultiMesh for each chunk and type
		foreach(Dictionary chunk_key in chunks_by_type)
		{
			UnitChunks[chunk_key] = new Dictionary{};
			var chunk_types = chunks_by_type[chunk_key];

			_CreateChunkTypeMultimesh(chunk_key, UnitTypes.Type.INFANTRY, chunk_types[UnitTypes.Type.INFANTRY], _InfantryMesh, PLAYER_COLORS_LAND);
			_CreateChunkTypeMultimesh(chunk_key, UnitTypes.Type.CAVALRY, chunk_types[UnitTypes.Type.CAVALRY], _CavalryMesh, PLAYER_COLORS_LAND);
			_CreateChunkTypeMultimesh(chunk_key, UnitTypes.Type.ARCHER, chunk_types[UnitTypes.Type.ARCHER], _ArcherMesh, PLAYER_COLORS_LAND);
			_CreateChunkTypeMultimesh(chunk_key, UnitTypes.Type.GALLEY, chunk_types[UnitTypes.Type.GALLEY], _GalleyMesh, PLAYER_COLORS_NAVAL);
			_CreateChunkTypeMultimesh(chunk_key, UnitTypes.Type.WARSHIP, chunk_types[UnitTypes.Type.WARSHIP], _WarshipMesh, PLAYER_COLORS_NAVAL);
			_CreateChunkTypeMultimesh(chunk_key, UnitTypes.Type.MARINE, chunk_types[UnitTypes.Type.MARINE], _MarineMesh, PLAYER_COLORS_AMPHIBIOUS);
		}

		NeedsRebuild = false;
	}


	protected void _ClearMeshes()
	{
		foreach(Variant mm in Multimeshes.Values())
		{
			if(mm)
			{
				mm.QueueFree();
			}
		}
		Multimeshes.Clear();
		UnitIdMaps.Clear();


		// Clear chunked meshes
		foreach(Dictionary chunk_key in UnitChunks)
		{
			var chunk_meshes = UnitChunks[chunk_key];
			foreach(Variant mm in chunk_meshes.Values())
			{
				if(mm)
				{
					mm.QueueFree();
				}
			}
		}
		UnitChunks.Clear();
	}


	protected void _CreateChunkTypeMultimesh(String chunk_key, UnitTypes.Type unit_type, Array units, Godot.Mesh mesh, Array<Color> colors)
	{
		if(units.IsEmpty())
		{
			return ;
		}

		var multimesh = MultiMesh.New();
		multimesh.TransformFormat = MultiMesh.TransformFormat.Transform3d;
		multimesh.UseColors = true;
		multimesh.Mesh = mesh;
		multimesh.InstanceCount = units.Size();

		foreach(int i in GD.Range(units.Size()))
		{
			var unit = units[i];
			var cell = Grid.GetCell(unit.Q, unit.R);
			var world_pos = unit.GetWorldPosition();
			var elevation = ( cell ? cell.Elevation : 0 );

			var is_on_water = cell != null && cell.Elevation < HexMetrics.SEA_LEVEL;
			if(UnitTypes.IsNaval(unit.Type) || (UnitTypes.IsAmphibious(unit.Type) && is_on_water))
			{
				world_pos.Y = HexMetrics.SEA_LEVEL * HexMetrics.ELEVATION_STEP + 0.1;
			}
			// Water surface + slight offset
			else
			{
				world_pos.Y = elevation * HexMetrics.ELEVATION_STEP + 0.25;
			}

			var transform = new Transform3D();
			Transform.Origin = world_pos;
			multimesh.SetInstanceTransform(i, Transform);

			var color = colors[unit.PlayerId % colors.Size()];
			multimesh.SetInstanceColor(i, color);
		}

		var instance = MultiMeshInstance3D.New();
		instance.Multimesh = multimesh;

		var material = StandardMaterial3D.New();
		material.VertexColorUseAsAlbedo = true;
		material.ShadingMode = BaseMaterial3D.ShadingMode.ShadingModePerVertex;
		instance.MaterialOverride = material;

		AddChild(instance);
		UnitChunks[chunk_key][unit_type] = instance;
	}


	protected void _CreateTypeMultimesh(UnitTypes.Type unit_type, Array units, Godot.Mesh mesh, Array<Color> colors)
	{
		if(units.IsEmpty())
		{
			Multimeshes[unit_type] = null;
			return ;
		}

		var multimesh = MultiMesh.New();
		multimesh.TransformFormat = MultiMesh.TransformFormat.Transform3d;
		multimesh.UseColors = true;
		multimesh.Mesh = mesh;
		multimesh.InstanceCount = units.Size();

		var unit_ids = new Array{};

		foreach(int i in GD.Range(units.Size()))
		{
			var unit = units[i];
			unit_ids.Append(unit.Id);


			// Get position
			var cell = Grid.GetCell(unit.Q, unit.R);
			var world_pos = unit.GetWorldPosition();
			var elevation = ( cell ? cell.Elevation : 0 );


			// Naval units float on water surface, land units on terrain
			// Amphibious units use water surface when in water, terrain when on land
			var is_on_water = cell != null && cell.Elevation < HexMetrics.SEA_LEVEL;
			if(UnitTypes.IsNaval(unit.Type) || (UnitTypes.IsAmphibious(unit.Type) && is_on_water))
			{
				world_pos.Y = HexMetrics.SEA_LEVEL * HexMetrics.ELEVATION_STEP + 0.1;
			}
			// Water surface + slight offset
			else
			{
				world_pos.Y = elevation * HexMetrics.ELEVATION_STEP + 0.25;
			}


			// Create transform
			var transform = new Transform3D();
			Transform.Origin = world_pos;
			multimesh.SetInstanceTransform(i, Transform);


			// Set color based on player
			var color = colors[unit.PlayerId % colors.Size()];
			multimesh.SetInstanceColor(i, color);
		}

		UnitIdMaps[unit_type] = unit_ids;

		var instance = MultiMeshInstance3D.New();
		instance.Multimesh = multimesh;


		// Create material
		var material = StandardMaterial3D.New();
		material.VertexColorUseAsAlbedo = true;
		material.ShadingMode = BaseMaterial3D.ShadingMode.ShadingModePerVertex;
		instance.MaterialOverride = material;

		AddChild(instance);
		Multimeshes[unit_type] = instance;
	}


	//# Infantry: Simple cylinder shape
	protected Godot.Mesh _CreateInfantryMesh()
	{
		var mesh = CylinderMesh.New();
		mesh.TopRadius = 0.15;
		mesh.BottomRadius = 0.18;
		mesh.Height = 0.5;
		mesh.RadialSegments = 8;
		return mesh;
	}


	//# Cavalry: Box shape (horse-like)
	protected Godot.Mesh _CreateCavalryMesh()
	{
		var mesh = BoxMesh.New();
		mesh.Size = new Vector3(0.5, 0.35, 0.25);
		return mesh;
	}


	//# Archer: Cone shape (pointed)
	protected Godot.Mesh _CreateArcherMesh()
	{
		var mesh = CylinderMesh.New();
		mesh.TopRadius = 0.0;
		mesh.BottomRadius = 0.15;
		mesh.Height = 0.5;
		mesh.RadialSegments = 6;
		return mesh;
	}


	//# Galley: Small boat (elongated box)
	protected Godot.Mesh _CreateGalleyMesh()
	{
		var mesh = BoxMesh.New();
		mesh.Size = new Vector3(0.6, 0.2, 0.25);
		return mesh;
	}


	//# Warship: Larger boat
	protected Godot.Mesh _CreateWarshipMesh()
	{
		var mesh = BoxMesh.New();
		mesh.Size = new Vector3(0.7, 0.3, 0.35);
		return mesh;
	}


	//# Marine: Similar to infantry but distinctive
	protected Godot.Mesh _CreateMarineMesh()
	{
		var mesh = CylinderMesh.New();
		mesh.TopRadius = 0.12;
		mesh.BottomRadius = 0.2;
		mesh.Height = 0.45;
		mesh.RadialSegments = 6;
		return mesh;
	}


	public void SetSelectedUnits(Array<int> ids)
	{
		SelectedUnitIds.Clear();
		foreach(int id in ids)
		{
			SelectedUnitIds[id] = true;
		}
		_ApplySelectionColors();
	}


	protected void _ApplySelectionColors()
	{
		_UpdateTypeColors(UnitTypes.Type.INFANTRY, PLAYER_COLORS_LAND);
		_UpdateTypeColors(UnitTypes.Type.CAVALRY, PLAYER_COLORS_LAND);
		_UpdateTypeColors(UnitTypes.Type.ARCHER, PLAYER_COLORS_LAND);
		_UpdateTypeColors(UnitTypes.Type.GALLEY, PLAYER_COLORS_NAVAL);
		_UpdateTypeColors(UnitTypes.Type.WARSHIP, PLAYER_COLORS_NAVAL);
		_UpdateTypeColors(UnitTypes.Type.MARINE, PLAYER_COLORS_AMPHIBIOUS);
	}


	protected void _UpdateTypeColors(UnitTypes.Type unit_type, Array<Color> colors)
	{
		var instance = Multimeshes.Get(unit_type);
		if(instance == null)
		{
			return ;
		}

		var unit_ids = UnitIdMaps.Get(unit_type, new Array{});
		if(unit_ids.IsEmpty())
		{
			return ;
		}

		var mm = instance.Multimesh;
		foreach(int i in GD.Range(unit_ids.Size()))
		{
			var unit_id = unit_ids[i];
			var unit = UnitManager.GetUnit(unit_id);
			if(unit == null)
			{
				continue;
			}

			var color;
			if(SelectedUnitIds.ContainsKey(unit_id))
			{
				color = SELECTED_COLOR;
			}
			else
			{
				color = colors[unit.PlayerId % colors.Size()];
			}

			mm.SetInstanceColor(i, color);
		}
	}


	public const double CHUNK_SIZE = 16.0;
	public const double MAX_RENDER_DISTANCE = 50.0;


	// Chunk storage for units
	public Dictionary UnitChunks = new Dictionary{};

	// chunk_key -> Dictionary of unit_type -> MultiMeshInstance3D
	public void Update()
	{
		if(NeedsRebuild)
		{
			Build();
		}
	}


	public void UpdateVisibility(Godot.Camera3D camera)
	{
		if(!camera)
		{
			return ;
		}

		var camera_pos = camera.GlobalPosition;

		// Use camera XZ position for distance (matches terrain renderer)
		var camera_xz = new Vector3(camera_pos.X, 0, camera_pos.Z);

		var max_dist_sq = MAX_RENDER_DISTANCE * MAX_RENDER_DISTANCE;

		foreach(Dictionary chunk_key in UnitChunks)
		{
			var parts = chunk_key.Split(",");
			var cx = Int(parts[0]);
			var cz = Int(parts[1]);
			var center_x = (cx + 0.5) * CHUNK_SIZE;
			var center_z = (cz + 0.5) * CHUNK_SIZE;

			var dx = center_x - camera_xz.X;
			var dz = center_z - camera_xz.Z;
			var visible = (dx * dx + dz * dz) <= max_dist_sq;

			var chunk_meshes = UnitChunks[chunk_key];
			foreach(Variant mm in chunk_meshes.Values())
			{
				if(mm)
				{
					mm.Visible = Visible;
				}
			}
		}
	}


	public void MarkDirty()
	{
		NeedsRebuild = true;
	}


}