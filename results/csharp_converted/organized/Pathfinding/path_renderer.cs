using Godot;
using Godot.Collections;


//# Renders pathfinding visualization:
//# - Reachable cells (highlighted hexes)
//# - Path preview (line from unit to destination)

//# Matches web/src/rendering/PathRenderer.ts
[GlobalClass]
public partial class PathRenderer : Godot.Node3D
{
	public Godot.HexGrid Grid;


	// Reachable cells visualization
	public Godot.MultiMeshInstance3D ReachableMeshes;
	public int MaxReachableInstances = 500;


	// Path line visualization
	public Godot.MeshInstance3D PathLine;
	public Godot.StandardMaterial3D PathMaterial;


	// Destination marker
	public Godot.MeshInstance3D DestinationMarker;


	public override void _Init()
	{
		_CreateReachableMesh();
		_CreatePathMaterial();
		_CreateDestinationMarker();
	}


	public void Setup(Godot.HexGrid p_grid)
	{
		System.Diagnostics.Debug.Assert(p_grid != null, "PathRenderer requires HexGrid");

		Grid = p_grid;
	}


	protected void _CreateReachableMesh()
	{

		// Create a flat hexagon for highlighting
		var mesh = _BuildHexShapeMesh();

		var multimesh = MultiMesh.New();
		multimesh.TransformFormat = MultiMesh.TransformFormat.Transform3d;
		multimesh.UseColors = true;
		multimesh.Mesh = mesh;
		multimesh.InstanceCount = MaxReachableInstances;
		multimesh.VisibleInstanceCount = 0;

		ReachableMeshes = MultiMeshInstance3D.New();
		ReachableMeshes.Multimesh = multimesh;

		var material = StandardMaterial3D.New();
		material.AlbedoColor = new Color(0.0, 1.0, 0.0, 0.5);
		material.Transparency = BaseMaterial3D.Transparency.TransparencyAlpha;
		material.ShadingMode = BaseMaterial3D.ShadingMode.ShadingModeUnshaded;
		material.CullMode = BaseMaterial3D.CullMode.CullDisabled;
		material.NoDepthTest = true;
		// Render on top of terrain
		material.VertexColorUseAsAlbedo = true;
		ReachableMeshes.MaterialOverride = material;

		AddChild(ReachableMeshes);
	}


	protected Godot.ArrayMesh _BuildHexShapeMesh()
	{
		var st = SurfaceTool.New();
		st.Begin(Mesh.PrimitiveType.PrimitiveTriangles);

		var radius = HexMetrics.OUTER_RADIUS * 0.9;
		var corners = new Array{};

		foreach(int i in GD.Range(6))
		{
			var angle = (Mathf.Pi / 3.0) * i - Mathf.Pi / 6.0;
			corners.Append(new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius));
		}


		// Build triangles from center
		var center = Vector3.Zero;
		st.SetNormal(Vector3.Up);
		foreach(int i in GD.Range(6))
		{
			st.AddVertex(center);
			st.AddVertex(corners[i]);
			st.AddVertex(corners[(i + 1) % 6]);
		}

		return st.Commit();
	}


	protected void _CreatePathMaterial()
	{
		PathMaterial = StandardMaterial3D.New();
		PathMaterial.AlbedoColor = new Color(0.0, 1.0, 0.0, 0.8);
		PathMaterial.Transparency = BaseMaterial3D.Transparency.TransparencyAlpha;
		PathMaterial.ShadingMode = BaseMaterial3D.ShadingMode.ShadingModeUnshaded;
	}


	protected void _CreateDestinationMarker()
	{

		// Create a ring mesh for destination
		var st = SurfaceTool.New();
		st.Begin(Mesh.PrimitiveType.PrimitiveTriangles);

		var inner_radius = 0.6;
		var outer_radius = 0.8;

		foreach(int i in GD.Range(6))
		{
			var angle1 = (Mathf.Pi / 3.0) * i - Mathf.Pi / 6.0;
			var angle2 = (Mathf.Pi / 3.0) * ((i + 1) % 6) - Mathf.Pi / 6.0;

			var inner1 = new Vector3(Mathf.Cos(angle1) * inner_radius, 0, Mathf.Sin(angle1) * inner_radius);
			var outer1 = new Vector3(Mathf.Cos(angle1) * outer_radius, 0, Mathf.Sin(angle1) * outer_radius);
			var inner2 = new Vector3(Mathf.Cos(angle2) * inner_radius, 0, Mathf.Sin(angle2) * inner_radius);
			var outer2 = new Vector3(Mathf.Cos(angle2) * outer_radius, 0, Mathf.Sin(angle2) * outer_radius);

			st.SetNormal(Vector3.Up);
			st.AddVertex(inner1);
			st.AddVertex(outer1);
			st.AddVertex(outer2);

			st.AddVertex(inner1);
			st.AddVertex(outer2);
			st.AddVertex(inner2);
		}

		DestinationMarker = MeshInstance3D.New();
		DestinationMarker.Mesh = st.Commit();

		var material = StandardMaterial3D.New();
		material.AlbedoColor = new Color(1.0, 0.0, 0.0, 0.8);
		material.Transparency = BaseMaterial3D.Transparency.TransparencyAlpha;
		material.ShadingMode = BaseMaterial3D.ShadingMode.ShadingModeUnshaded;
		material.CullMode = BaseMaterial3D.CullMode.CullDisabled;
		DestinationMarker.MaterialOverride = material;
		DestinationMarker.Visible = false;

		AddChild(DestinationMarker);
	}


	//# Show reachable cells for a unit
	public void ShowReachableCells(Dictionary reachable_cells)
	{
		if(ReachableMeshes == null)
		{
			return ;
		}

		var mm = ReachableMeshes.Multimesh;
		var index = 0;

		foreach(Variant cell in reachable_cells.Keys())
		{
			if(index >= MaxReachableInstances)
			{
				break;
			}

			var cost = reachable_cells[cell];
			var world_pos = HexCoordinates.New(cell.Q, cell.R).ToWorldPosition(0);


			// For water cells, render on water surface; for land, render on terrain
			var y_offset;
			if(cell.Elevation < HexMetrics.SEA_LEVEL)
			{
				y_offset = HexMetrics.SEA_LEVEL * HexMetrics.ELEVATION_STEP + 0.1;
			}
			// Just above water surface
			else
			{
				y_offset = cell.Elevation * HexMetrics.ELEVATION_STEP + 0.15;
			}

			var transform = new Transform3D();
			Transform.Origin = new Vector3(world_pos.X, y_offset, world_pos.Z);
			mm.SetInstanceTransform(index, Transform);


			// Color based on movement cost (green = cheap, yellow = expensive)
			var t = Mathf.Min(cost / 4.0, 1.0);
			var color = Color.FromHsv(0.33 - t * 0.33, 0.8, 0.7, 0.5);
			mm.SetInstanceColor(index, color);

			index += 1;
		}

		mm.VisibleInstanceCount = index;
	}


	//# Hide reachable cells
	public void HideReachableCells()
	{
		if(ReachableMeshes && ReachableMeshes.Multimesh)
		{
			ReachableMeshes.Multimesh.VisibleInstanceCount = 0;
		}
	}


	//# Show path preview from unit to destination
	public void ShowPath(Array path)
	{

		// Remove old path line
		if(PathLine)
		{
			PathLine.QueueFree();
			PathLine = null;
		}

		if(path.Size() < 2)
		{
			HideDestinationMarker();
			return ;
		}


		// Create path points
		var points = new Array{};
		foreach(Variant cell in path)
		{
			var world_pos = HexCoordinates.New(cell.Q, cell.R).ToWorldPosition(0);
			var y_pos;
			if(cell.Elevation < HexMetrics.SEA_LEVEL)
			{
				y_pos = HexMetrics.SEA_LEVEL * HexMetrics.ELEVATION_STEP + 0.15;
			}
			// Above water surface
			else
			{
				y_pos = cell.Elevation * HexMetrics.ELEVATION_STEP + 0.2;
			}
			points.Append(new Vector3(world_pos.X, y_pos, world_pos.Z));
		}


		// Create line mesh using ImmediateMesh
		var im = ImmediateMesh.New();
		im.SurfaceBegin(Mesh.PrimitiveType.PrimitiveLineStrip);
		foreach(PackedVector3Array point in points)
		{
			im.SurfaceAddVertex(point);
		}
		im.SurfaceEnd();

		PathLine = MeshInstance3D.New();
		PathLine.Mesh = im;
		PathLine.MaterialOverride = PathMaterial;
		AddChild(PathLine);


		// Show destination marker at end of path
		var last_cell = path[path.Size() - 1];
		ShowDestinationMarker(last_cell);
	}


	//# Hide path preview
	public void HidePath()
	{
		if(PathLine)
		{
			PathLine.QueueFree();
			PathLine = null;
		}
		HideDestinationMarker();
	}


	//# Show destination marker at a cell
	public void ShowDestinationMarker(Godot.HexCell cell)
	{
		if(DestinationMarker == null)
		{
			return ;
		}

		var world_pos = HexCoordinates.New(cell.Q, cell.R).ToWorldPosition(0);
		var y_pos;
		if(cell.Elevation < HexMetrics.SEA_LEVEL)
		{
			y_pos = HexMetrics.SEA_LEVEL * HexMetrics.ELEVATION_STEP + 0.12;
		}
		// Above water surface
		else
		{
			y_pos = cell.Elevation * HexMetrics.ELEVATION_STEP + 0.15;
		}
		DestinationMarker.Position = new Vector3(world_pos.X, y_pos, world_pos.Z);
		DestinationMarker.Visible = true;
	}


	//# Hide destination marker
	public void HideDestinationMarker()
	{
		if(DestinationMarker)
		{
			DestinationMarker.Visible = false;
		}
	}


	//# Update path line color (for valid/invalid paths)
	public void SetPathValid(bool valid)
	{
		if(PathMaterial)
		{
			PathMaterial.AlbedoColor = ( valid ? new Color(0.0, 1.0, 0.0, 0.8) : new Color(1.0, 0.0, 0.0, 0.8) );
		}
	}


}