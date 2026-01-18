using Godot;
using Godot.Collections;


//# Hex direction utilities for flat-topped hexes

//# Matches web/src/core/HexDirection.ts
[GlobalClass]
public partial class HexDirection : Godot.RefCounted
{
	public enum Direction {NE = 0,  Northeast, E = 1,  East, SE = 2,  Southeast, SW = 3,  Southwest, W = 4,  West, NW = 5,  Northwest}


	// Axial coordinate offsets for each direction (q, r)
	// Must match web/src/core/HexDirection.ts DirectionOffsets
	public const Array<Vector2i> OFFSETS = new Array{
			new Vector2i(1, 0), 
			// NE: q+1, r+0
			new Vector2i(1,  - 1), 
			// E:  q+1, r-1
			new Vector2i(0,  - 1), 
			// SE: q+0, r-1
			new Vector2i( - 1, 0), 
			// SW: q-1, r+0
			new Vector2i( - 1, 1), 
			// W:  q-1, r+1
			new Vector2i(0, 1), 
			// NW: q+0, r+1
			};


	//# Get the offset for a direction
	public static Vector2i GetOffset(int direction)
	{
		return OFFSETS[direction % 6];
	}


	//# Get the opposite direction
	public static int Opposite(int direction)
	{
		return (direction + 3) % 6;
	}


	//# Get the next direction (clockwise)
	public static int Next(int direction)
	{
		return (direction + 1) % 6;
	}


	//# Get the previous direction (counter-clockwise)
	public static int Previous(int direction)
	{
		return (direction + 5) % 6;
	}


}