using Godot;
using Godot.Collections;


//# Renders hex terrain using Godot's mesh system

//# Matches web/src/rendering/ChunkedTerrainRenderer.ts
[GlobalClass]
public partial class TerrainRenderer : Godot.RefCounted
{
	public Godot.HexGrid Grid;
	public Godot.MeshInstance3D MeshInstance;
	public Godot.HexMeshBuilder HexMeshBuilder;


	public override void _Init()
	{
		HexMeshBuilder = HexMeshBuilder.New();
	}


	//# Build terrain mesh for entire grid
	public void Build(Godot.HexGrid hex_grid, Godot.Node3D parent)
	{
		Grid = hex_grid;


		// Create mesh instance if needed
		if(MeshInstance == null)
		{
			MeshInstance = MeshInstance3D.New();
			parent.AddChild(MeshInstance);
		}


		// Build the mesh
		var mesh = HexMeshBuilder.BuildGridMesh(Grid);
		MeshInstance.Mesh = mesh;


		// Create material
		var material = StandardMaterial3D.New();
		material.VertexColorUseAsAlbedo = true;
		material.ShadingMode = BaseMaterial3D.ShadingMode.ShadingModePerVertex;
		MeshInstance.MaterialOverride = material;
	}


	//# Update specific cells (for dynamic changes)
	public void UpdateCells(Array<HexCell> _cells)
	{

		// TODO: Implement partial updates

	}


	//# Dispose of resources
	public void Dispose()
	{
		if(MeshInstance)
		{
			MeshInstance.QueueFree();
			MeshInstance = null;
		}
	}


}