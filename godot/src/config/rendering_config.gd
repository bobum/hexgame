class_name RenderingConfig
extends RefCounted
## Centralized rendering configuration constants
## Eliminates magic numbers scattered across rendering files

# =============================================================================
# CHUNKING & CULLING
# =============================================================================

## Size of terrain/water/feature chunks in world units
const CHUNK_SIZE: float = 16.0

## Maximum render distance for terrain chunks
const TERRAIN_RENDER_DISTANCE: float = 60.0

## Maximum render distance for water/features/units (closer than terrain)
const DETAIL_RENDER_DISTANCE: float = 50.0

# =============================================================================
# LEVEL OF DETAIL (LOD)
# =============================================================================

## Distance threshold: HIGH → MEDIUM detail
const LOD_HIGH_TO_MEDIUM: float = 30.0

## Distance threshold: MEDIUM → LOW detail
const LOD_MEDIUM_TO_LOW: float = 60.0

## Reference zoom level for LOD calculations
const LOD_REFERENCE_ZOOM: float = 30.0

# =============================================================================
# WATER RENDERING
# =============================================================================

## Water surface Y offset above terrain (prevents z-fighting)
const WATER_SURFACE_OFFSET: float = 0.12

## Deep water color (ocean floor)
const WATER_DEEP_COLOR: Color = Color(0.102, 0.298, 0.431)  # #1a4c6e

## Shallow water color (coastal)
const WATER_SHALLOW_COLOR: Color = Color(0.176, 0.545, 0.788)  # #2d8bc9

# =============================================================================
# RIVER RENDERING
# =============================================================================

## Width of river meshes
const RIVER_WIDTH: float = 0.15

## River Y offset above terrain
const RIVER_HEIGHT_OFFSET: float = 0.02

# =============================================================================
# HEX HOVER HIGHLIGHT
# =============================================================================

## Color for hex hover highlight
const HIGHLIGHT_COLOR: Color = Color(1.0, 0.9, 0.2, 0.8)  # Yellow

## Height offset for highlight ring
const HIGHLIGHT_HEIGHT: float = 0.1

## Width of highlight ring
const HIGHLIGHT_RING_WIDTH: float = 0.08

# =============================================================================
# UNIT RENDERING
# =============================================================================

## Color for selected units
const UNIT_SELECTED_COLOR: Color = Color(1.0, 1.0, 1.0)  # White

## Land unit colors by player
const PLAYER_COLORS_LAND: Array[Color] = [
	Color(0.2, 0.6, 0.2),   # Player 0: Green
	Color(0.2, 0.4, 0.8),   # Player 1: Blue
	Color(0.8, 0.2, 0.2),   # Player 2: Red
	Color(0.7, 0.5, 0.1),   # Player 3: Orange
]

## Naval unit colors by player
const PLAYER_COLORS_NAVAL: Array[Color] = [
	Color(0.1, 0.5, 0.5),   # Player 0: Teal
	Color(0.1, 0.3, 0.7),   # Player 1: Navy Blue
	Color(0.6, 0.1, 0.3),   # Player 2: Maroon
	Color(0.5, 0.4, 0.1),   # Player 3: Brown
]

## Amphibious unit colors by player
const PLAYER_COLORS_AMPHIBIOUS: Array[Color] = [
	Color(0.3, 0.5, 0.4),   # Player 0: Sea Green
	Color(0.2, 0.4, 0.6),   # Player 1: Steel Blue
	Color(0.6, 0.2, 0.3),   # Player 2: Dark Red
	Color(0.5, 0.4, 0.2),   # Player 3: Olive
]
