using Godot;
using Godot.Collections;


//# Core hex geometry constants and utilities

//# Matches web/src/core/HexMetrics.ts
// Hex geometry
[GlobalClass]
public partial class HexMetrics : Godot.RefCounted
{
	public const double OUTER_RADIUS = 1.0;
	// Corner to center distance
	public const double INNER_RADIUS = OUTER_RADIUS * 0.866025404;

	// Edge to center (outer * sqrt(3)/2)
	// Elevation - sea level system (no negative elevations)
	public const double ELEVATION_STEP = 0.4;
	// Height per elevation level
	public const int MIN_ELEVATION = 0;
	// Ocean floor (deepest water)
	public const int SEA_LEVEL = 4;
	// Water surface elevation (water is 0-4)
	public const int LAND_MIN_ELEVATION = 5;
	// Minimum land elevation (always 1 above sea level)
	public const int MAX_ELEVATION = 13;

	// Highest land (8 levels above land min)
	// Terraces (Catlike Coding style)
	public const int TERRACES_PER_SLOPE = 2;


	// Number of flat terraces per slope
	public static int GetTerraceSteps()
	{
		return TERRACES_PER_SLOPE * 2 + 1;
	}


	public static double GetHorizontalTerraceStepSize()
	{
		return 1.0 / GetTerraceSteps();
	}


	public static double GetVerticalTerraceStepSize()
	{
		return 1.0 / (TERRACES_PER_SLOPE + 1);
	}


	// Blend regions (Catlike Coding style)
	public const double SOLID_FACTOR = 0.8;
	// Inner solid hex portion
	public const double BLEND_FACTOR = 0.2;


	// Outer blend portion (edge/corner connections)
	//# Get the 6 corner positions for a hex (flat-topped, starting at 30 degrees)
	public static Array<Vector3> GetCorners()
	{
		var corners = new Array{};
		foreach(int i in GD.Range(6))
		{
			var angle = (Mathf.Pi / 3.0) * i + Mathf.Pi / 6.0;
			// Start at 30 degrees


			corners.Append(new Vector3(, Mathf.Cos(angle) * OUTER_RADIUS, 0, Mathf.Sin(angle) * OUTER_RADIUS));
		}
		return corners;
	}


	//# Get corner at specific index (with wrapping)
	public static Vector3 GetCorner(int index)
	{
		var corners = GetCorners();
		return corners[((index % 6) + 6) % 6];


		//# Terrace interpolation - the key to Catlike Coding style terraces

	}//# Horizontal interpolation is linear, vertical only changes on odd steps
	public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
	{
		var h = step * GetHorizontalTerraceStepSize();
		var v = Mathf.Floor((step + 1) / 2.0) * GetVerticalTerraceStepSize();


		return new Vector3(, a.X + (b.X - a.X) * h, a.Y + (b.Y - a.Y) * v, a.Z + (b.Z - a.Z) * h);
	}


	//# Interpolate color along terrace
	public static Color TerraceColorLerp(Color a, Color b, int step)
	{
		var h = step * GetHorizontalTerraceStepSize();
		return a.Lerp(b, h);
	}


}