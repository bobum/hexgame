class_name GameConfig
extends RefCounted
## Centralized game configuration constants
## Default values for map generation, units, and gameplay

# =============================================================================
# MAP DEFAULTS
# =============================================================================

## Default map width in hex cells
const DEFAULT_MAP_WIDTH: int = 32

## Default map height in hex cells
const DEFAULT_MAP_HEIGHT: int = 32

## Minimum map dimension
const MIN_MAP_SIZE: int = 8

## Maximum map dimension
const MAX_MAP_SIZE: int = 128

# =============================================================================
# GENERATION DEFAULTS
# =============================================================================

## Default sea level threshold (0.0-1.0, higher = more water)
const DEFAULT_SEA_LEVEL: float = 0.35

## Default river coverage percentage
const DEFAULT_RIVER_PERCENTAGE: float = 0.1

## Minimum river length in cells
const MIN_RIVER_LENGTH: int = 3

## Default noise scale for terrain generation
const DEFAULT_NOISE_SCALE: float = 0.02

## Default noise octaves
const DEFAULT_NOISE_OCTAVES: int = 4

## Default noise persistence
const DEFAULT_NOISE_PERSISTENCE: float = 0.5

## Default noise lacunarity
const DEFAULT_NOISE_LACUNARITY: float = 2.0

# =============================================================================
# UNIT DEFAULTS
# =============================================================================

## Default number of land units to spawn
const DEFAULT_LAND_UNITS: int = 10

## Default number of naval units to spawn
const DEFAULT_NAVAL_UNITS: int = 5

## Movement cost for crossing rivers
const RIVER_CROSSING_COST: float = 1.0

# =============================================================================
# PLAYER CONFIGURATION
# =============================================================================

## Human player ID
const PLAYER_HUMAN: int = 1

## Starting ID for AI players
const PLAYER_AI_START: int = 2

# =============================================================================
# BIOME FEATURE PROBABILITIES
# =============================================================================

## Feature spawn chances per biome: {biome: {tree: chance, rock: chance}}
const BIOME_FEATURES: Dictionary = {
	"FOREST": {"tree": 0.7, "rock": 0.1},
	"JUNGLE": {"tree": 0.85, "rock": 0.05},
	"PLAINS": {"tree": 0.15, "rock": 0.1},
	"SAVANNA": {"tree": 0.1, "rock": 0.15},
	"HILLS": {"tree": 0.2, "rock": 0.3},
	"MOUNTAINS": {"tree": 0.05, "rock": 0.4},
	"DESERT": {"tree": 0.0, "rock": 0.2},
	"SNOW": {"tree": 0.0, "rock": 0.15},
}
