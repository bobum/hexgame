using Godot;
using Godot.Collections;


//# Handles hex hover detection and highlighting

//# Matches web hover system
[GlobalClass]
public partial class HexHover : Godot.Node3D
{
	public const Color HIGHLIGHT_COLOR = new Color(1.0, 0.9, 0.2, 0.8);
	// Yellow
	public const double HIGHLIGHT_HEIGHT = 0.1;
	// Slightly above terrain
	public const double RING_WIDTH = 0.08;

	// Width of highlight ring
	public Godot.MeshInstance3D HighlightMesh;
	public Godot.HexCell CurrentCell = null;
	public Godot.HexGrid Grid;
	public Godot.Camera3D Camera;


	// Callback for UI updates
	[Signal]
	public delegate void CellHoveredEventHandler(Godot.HexCell cell);
	[Signal]
	public delegate void CellUnhoveredEventHandler();


	public override void _Ready()
	{
		_CreateHighlightMesh();
	}


	public void Setup(Godot.HexGrid p_grid, Godot.Camera3D p_camera)
	{
		System.Diagnostics.Debug.Assert(p_grid != null, "HexHover requires HexGrid");
		System.Diagnostics.Debug.Assert(p_camera != null, "HexHover requires Camera3D");

		Grid = p_grid;
		Camera = p_camera;
	}


	protected void _CreateHighlightMesh()
	{
		var mesh = _BuildHexRingMesh();
		HighlightMesh = MeshInstance3D.New();
		HighlightMesh.Mesh = mesh;

		var material = StandardMaterial3D.New();
		material.AlbedoColor = HIGHLIGHT_COLOR;
		material.Transparency = BaseMaterial3D.Transparency.TransparencyAlpha;
		material.ShadingMode = BaseMaterial3D.ShadingMode.ShadingModeUnshaded;
		material.CullMode = BaseMaterial3D.CullMode.CullDisabled;
		HighlightMesh.MaterialOverride = material;

		HighlightMesh.Visible = false;
		AddChild(HighlightMesh);
	}


	protected Godot.ArrayMesh _BuildHexRingMesh()
	{
		var st = SurfaceTool.New();
		st.Begin(Mesh.PrimitiveType.PrimitiveTriangles);

		var corners = HexMetrics.GetCorners();
		var inner_scale = 1.0 - RING_WIDTH;

		foreach(int i in GD.Range(6))
		{
			var c1 = corners[i];
			var c2 = corners[(i + 1) % 6];


			// Outer corners
			var outer1 = new Vector3(c1.X, 0, c1.Z);
			var outer2 = new Vector3(c2.X, 0, c2.Z);


			// Inner corners (scaled down)
			var inner1 = new Vector3(c1.X * inner_scale, 0, c1.Z * inner_scale);
			var inner2 = new Vector3(c2.X * inner_scale, 0, c2.Z * inner_scale);


			// Build quad for this edge of the ring
			st.SetNormal(Vector3.Up);
			st.AddVertex(outer1);
			st.AddVertex(inner1);
			st.AddVertex(outer2);

			st.AddVertex(outer2);
			st.AddVertex(inner1);
			st.AddVertex(inner2);
		}

		return st.Commit();
	}


	public override void _Input(Godot.InputEvent event)
	{
		if(event is Godot.InputEventMouseMotion)
		{
			_UpdateHover();
		}
	}


	protected void _UpdateHover()
	{
		if(!Camera || !Grid)
		{
			return ;
		}

		var mouse_pos = GetViewport().GetMousePosition();
		var ray_origin = Camera.ProjectRayOrigin(mouse_pos);
		var ray_dir = Camera.ProjectRayNormal(mouse_pos);


		// Raycast to find intersection with ground plane (y = 0) or terrain
		var cell = _RaycastToHex(ray_origin, ray_dir);

		if(cell != CurrentCell)
		{
			CurrentCell = cell;
			if(cell)
			{
				_ShowHighlight(cell);
				EmitSignal("CellHovered", cell);
			}
			else
			{
				_HideHighlight();
				EmitSignal("CellUnhovered");
			}
		}
	}


	protected Godot.HexCell _RaycastToHex(Vector3 origin, Vector3 direction)
	{

		// Cast to sea level plane (where water and most terrain interaction happens)
		if(Mathf.Abs(direction.Y) < 0.001)
		{
			return null;
		}


		// Sea level Y position
		var sea_level_y = HexMetrics.SEA_LEVEL * HexMetrics.ELEVATION_STEP;


		// Cast to sea level plane to get XZ position
		var t = (sea_level_y - origin.Y) / direction.Y;

		if(t <= 0)
		{
			return null;
		}

		var hit_point = origin + direction * t;
		return _GetCellAtPosition(hit_point);
	}


	protected Godot.HexCell _GetCellAtPosition(Vector3 world_pos)
	{
		var coords = HexCoordinates.FromWorldPosition(world_pos);
		return Grid.GetCell(coords.Q, coords.R);
	}


	protected void _ShowHighlight(Godot.HexCell cell)
	{
		var world_pos = cell.GetWorldPosition();
		HighlightMesh.Position = new Vector3(world_pos.X, world_pos.Y + HIGHLIGHT_HEIGHT, world_pos.Z);
		HighlightMesh.Visible = true;
	}


	protected void _HideHighlight()
	{
		HighlightMesh.Visible = false;
		CurrentCell = null;
	}


	public Godot.HexCell GetHoveredCell()
	{
		return CurrentCell;
	}


}