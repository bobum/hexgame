using Godot;
using Godot.Collections;


//# Manages the hex grid data structure

//# Matches web/src/core/HexGrid.ts
[GlobalClass]
public partial class HexGrid : Godot.RefCounted
{
	public int Width = 0;
	public int Height = 0;
	public Dictionary Cells = new Dictionary{};


	// Key: Vector2i(q, r) -> HexCell
	public override void _Init(int w = 64, int h = 64)
	{
		Width = w;
		Height = h;
	}


	//# Initialize grid with empty cells
	public void Initialize()
	{
		Cells.Clear();
		foreach(int r in GD.Range(Height))
		{
			foreach(int q in GD.Range(Width))
			{
				var cell = HexCell.New();
				cell.Q = q;
				cell.R = r;
				cell.Elevation = 0;
				cell.TerrainType = TerrainType.Type.PLAINS;
				Cells[new Vector2i(q, r)] = cell;
			}
		}
	}


	//# Get cell at coordinates
	public Godot.HexCell GetCell(int q, int r)
	{
		var key = new Vector2i(q, r);
		return Cells.Get(key);
	}


	//# Set cell at coordinates
	public void SetCell(int q, int r, Godot.HexCell cell)
	{
		Cells[new Vector2i(q, r)] = cell;
	}


	//# Get neighbor of a cell in given direction
	public Godot.HexCell GetNeighbor(Godot.HexCell cell, int direction)
	{
		var offset = HexDirection.GetOffset(direction);
		return GetCell(cell.Q + offset.X, cell.R + offset.Y);
	}


	//# Get all valid neighbors of a cell
	public Array<HexCell> GetNeighbors(Godot.HexCell cell)
	{
		var neighbors = new Array{};
		foreach(int dir in GD.Range(6))
		{
			var neighbor = GetNeighbor(cell, dir);
			if(neighbor != null)
			{
				neighbors.Append(neighbor);
			}
		}
		return neighbors;
	}


	//# Check if coordinates are within bounds
	public bool IsValid(int q, int r)
	{
		return q >= 0 && q < Width && r >= 0 && r < Height;
	}


	//# Get all cells as array
	public Array<HexCell> GetAllCells()
	{
		var result = new Array{};
		foreach(Variant cell in Cells.Values())
		{
			result.Append(cell);
		}
		return result;
	}


}