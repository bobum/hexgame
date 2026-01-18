using Godot;
using Godot.Collections;


//# Centralized game configuration constants

//# Default values for map generation, units, and gameplay
// =============================================================================
// MAP DEFAULTS

// =============================================================================
//# Default map width in hex cells
[GlobalClass]
public partial class GameConfig : Godot.RefCounted
{
	public const int DEFAULT_MAP_WIDTH = 32;


	//# Default map height in hex cells
	public const int DEFAULT_MAP_HEIGHT = 32;


	//# Minimum map dimension
	public const int MIN_MAP_SIZE = 8;


	//# Maximum map dimension
	public const int MAX_MAP_SIZE = 128;


	// =============================================================================
	// GENERATION DEFAULTS

	// =============================================================================
	//# Default sea level threshold (0.0-1.0, higher = more water)
	public const double DEFAULT_SEA_LEVEL = 0.35;


	//# Default river coverage percentage
	public const double DEFAULT_RIVER_PERCENTAGE = 0.1;


	//# Minimum river length in cells
	public const int MIN_RIVER_LENGTH = 3;


	//# Default noise scale for terrain generation
	public const double DEFAULT_NOISE_SCALE = 0.02;


	//# Default noise octaves
	public const int DEFAULT_NOISE_OCTAVES = 4;


	//# Default noise persistence
	public const double DEFAULT_NOISE_PERSISTENCE = 0.5;


	//# Default noise lacunarity
	public const double DEFAULT_NOISE_LACUNARITY = 2.0;


	// =============================================================================
	// UNIT DEFAULTS

	// =============================================================================
	//# Default number of land units to spawn
	public const int DEFAULT_LAND_UNITS = 10;


	//# Default number of naval units to spawn
	public const int DEFAULT_NAVAL_UNITS = 5;


	//# Movement cost for crossing rivers
	public const double RIVER_CROSSING_COST = 1.0;


	// =============================================================================
	// PLAYER CONFIGURATION

	// =============================================================================
	//# Human player ID
	public const int PLAYER_HUMAN = 1;


	//# Starting ID for AI players
	public const int PLAYER_AI_START = 2;


	// =============================================================================
	// BIOME FEATURE PROBABILITIES

	// =============================================================================
	//# Feature spawn chances per biome: {biome: {tree: chance, rock: chance}}
	public const Dictionary BIOME_FEATURES = new Dictionary{
			{"FOREST", new Dictionary{{"tree", 0.7},{"rock", 0.1},}},
			{"JUNGLE", new Dictionary{{"tree", 0.85},{"rock", 0.05},}},
			{"PLAINS", new Dictionary{{"tree", 0.15},{"rock", 0.1},}},
			{"SAVANNA", new Dictionary{{"tree", 0.1},{"rock", 0.15},}},
			{"HILLS", new Dictionary{{"tree", 0.2},{"rock", 0.3},}},
			{"MOUNTAINS", new Dictionary{{"tree", 0.05},{"rock", 0.4},}},
			{"DESERT", new Dictionary{{"tree", 0.0},{"rock", 0.2},}},
			{"SNOW", new Dictionary{{"tree", 0.0},{"rock", 0.15},}},
			};


}