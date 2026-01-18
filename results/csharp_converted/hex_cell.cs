using Godot;
using Godot.Collections;


//# Represents a single hex cell in the grid

//# Matches web/src/types/index.ts HexCell interface
[GlobalClass]
public partial class HexCell : Godot.RefCounted
{
	public int Q = 0;
	// Axial Q coordinate
	public int R = 0;
	// Axial R coordinate
	public int Elevation = 0;
	// Height level
	public TerrainType.Type TerrainType = TerrainType.Type.PLAINS;
	public double Moisture = 0.0;
	// 0-1 moisture level
	public double Temperature = 0.5;

	// 0-1 temperature
	// River data
	public bool HasRiver = false;
	public Array<int> RiverDirections = new Array{};

	// Which edges have rivers
	// Feature data
	public bool HasRoad = false;
	public Array Features = new Array{};


	// Trees, rocks, etc. (Feature objects)
	//# Get world position of this cell's center
	//# All cells render at their actual elevation
	public Vector3 GetWorldPosition()
	{
		var coords = HexCoordinates.New(Q, R);
		return coords.ToWorldPosition(Elevation);
	}


	//# Check if this cell is underwater (water is elevation 0-4, land is 5+)
	public bool IsUnderwater()
	{
		return Elevation < HexMetrics.LAND_MIN_ELEVATION;
	}


	//# Check if terrain is water type
	public bool IsWater()
	{
		return TerrainType.IsWater(TerrainType);
	}


	//# Get terrain color
	public Color GetColor()
	{
		return TerrainType.GetColor(TerrainType);
	}


	//# String representation
	public override String _ToString()
	{
		return "HexCell(%d, %d) elev=%d terrain=%s" % new Array{Q, R, Elevation, TerrainType.Type.Keys()[TerrainType], };
	}


}