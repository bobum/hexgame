using Godot;
using Godot.Collections;


//# Axial hex coordinates (q, r) with cube coordinate conversions

//# Matches web/src/core/HexCoordinates.ts
[GlobalClass]
public partial class HexCoordinates : Godot.RefCounted
{
	public int Q;
	// Column (axial)
	public int R;


	// Row (axial)
	public override void _Init(int q_val = 0, int r_val = 0)
	{
		Q = q_val;
		R = r_val;
	}


	//# Cube coordinate X (derived from axial)
	public int GetX()
	{
		return Q;
	}


	//# Cube coordinate Y (derived from axial)
	public int GetY()
	{
		return  - Q - R;
	}


	//# Cube coordinate Z (derived from axial)
	public int GetZ()
	{
		return R;
	}


	//# Convert to world position at given elevation
	public Vector3 ToWorldPosition(int elevation = 0)
	{
		var x = (Q + R * 0.5) * (HexMetrics.INNER_RADIUS * 2.0);
		var z = R * (HexMetrics.OUTER_RADIUS * 1.5);
		var y = elevation * HexMetrics.ELEVATION_STEP;
		return new Vector3(x, y, z);
	}


	//# Create from world position
	public static Godot.HexCoordinates FromWorldPosition(Vector3 position)
	{
		var q_float = position.X / (HexMetrics.INNER_RADIUS * 2.0);
		var r_float = position.Z / (HexMetrics.OUTER_RADIUS * 1.5);
		q_float -= r_float * 0.5;


		// Round to nearest hex
		var q_int = Mathf.RoundToInt(q_float);
		var r_int = Mathf.RoundToInt(r_float);

		return HexCoordinates.New(q_int, r_int);
	}


	//# Calculate distance to another hex (in hex steps)
	public int DistanceTo(Godot.HexCoordinates other)
	{
		var dx = Mathf.Abs(GetX() - other.GetX());
		var dy = Mathf.Abs(GetY() - other.GetY());
		var dz = Mathf.Abs(GetZ() - other.GetZ());
		return (dx + dy + dz) / 2;
	}


	//# Get neighbor in given direction
	public Godot.HexCoordinates GetNeighbor(int direction)
	{
		var offsets = HexDirection.GetOffset(direction);
		return HexCoordinates.New(Q + offsets.X, R + offsets.Y);
	}


	//# String representation
	public override String _ToString()
	{
		return "(%d, %d)" % new Array{Q, R, };
	}


	//# Equality check
	public bool Equals(Godot.HexCoordinates other)
	{
		return Q == other.Q && R == other.R;
	}


}