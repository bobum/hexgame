using Godot;
using Godot.Collections;


//# Centralized rendering configuration constants

//# Eliminates magic numbers scattered across rendering files
// =============================================================================
// CHUNKING & CULLING

// =============================================================================
//# Size of terrain/water/feature chunks in world units
[GlobalClass]
public partial class RenderingConfig : Godot.RefCounted
{
	public const double CHUNK_SIZE = 16.0;


	//# Maximum render distance for terrain chunks
	public const double TERRAIN_RENDER_DISTANCE = 60.0;


	//# Maximum render distance for water/features/units (closer than terrain)
	public const double DETAIL_RENDER_DISTANCE = 50.0;


	// =============================================================================
	// LEVEL OF DETAIL (LOD)

	// =============================================================================
	//# Distance threshold: HIGH → MEDIUM detail
	public const double LOD_HIGH_TO_MEDIUM = 30.0;


	//# Distance threshold: MEDIUM → LOW detail
	public const double LOD_MEDIUM_TO_LOW = 60.0;


	//# Reference zoom level for LOD calculations
	public const double LOD_REFERENCE_ZOOM = 30.0;


	// =============================================================================
	// WATER RENDERING

	// =============================================================================
	//# Water surface Y offset above terrain (prevents z-fighting)
	public const double WATER_SURFACE_OFFSET = 0.12;


	//# Deep water color (ocean floor)
	public const Color WATER_DEEP_COLOR = new Color(0.102, 0.298, 0.431);

	// #1a4c6e
	//# Shallow water color (coastal)
	public const Color WATER_SHALLOW_COLOR = new Color(0.176, 0.545, 0.788);

	// #2d8bc9
	// =============================================================================
	// RIVER RENDERING

	// =============================================================================
	//# Width of river meshes
	public const double RIVER_WIDTH = 0.15;


	//# River Y offset above terrain
	public const double RIVER_HEIGHT_OFFSET = 0.02;


	// =============================================================================
	// HEX HOVER HIGHLIGHT

	// =============================================================================
	//# Color for hex hover highlight
	public const Color HIGHLIGHT_COLOR = new Color(1.0, 0.9, 0.2, 0.8);

	// Yellow
	//# Height offset for highlight ring
	public const double HIGHLIGHT_HEIGHT = 0.1;


	//# Width of highlight ring
	public const double HIGHLIGHT_RING_WIDTH = 0.08;


	// =============================================================================
	// UNIT RENDERING

	// =============================================================================
	//# Color for selected units
	public const Color UNIT_SELECTED_COLOR = new Color(1.0, 1.0, 1.0);

	// White
	//# Land unit colors by player
	public const Array<Color> PLAYER_COLORS_LAND = new Array{
			new Color(0.2, 0.6, 0.2), 
			// Player 0: Green
			new Color(0.2, 0.4, 0.8), 
			// Player 1: Blue
			new Color(0.8, 0.2, 0.2), 
			// Player 2: Red
			new Color(0.7, 0.5, 0.1), 
			// Player 3: Orange
			};


	//# Naval unit colors by player
	public const Array<Color> PLAYER_COLORS_NAVAL = new Array{
			new Color(0.1, 0.5, 0.5), 
			// Player 0: Teal
			new Color(0.1, 0.3, 0.7), 
			// Player 1: Navy Blue
			new Color(0.6, 0.1, 0.3), 
			// Player 2: Maroon
			new Color(0.5, 0.4, 0.1), 
			// Player 3: Brown
			};


	//# Amphibious unit colors by player
	public const Array<Color> PLAYER_COLORS_AMPHIBIOUS = new Array{
			new Color(0.3, 0.5, 0.4), 
			// Player 0: Sea Green
			new Color(0.2, 0.4, 0.6), 
			// Player 1: Steel Blue
			new Color(0.6, 0.2, 0.3), 
			// Player 2: Dark Red
			new Color(0.5, 0.4, 0.2), 
			// Player 3: Olive
			};


}