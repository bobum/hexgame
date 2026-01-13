class_name HexMetrics
extends RefCounted
## Core hex geometry constants and utilities
## Matches web/src/core/HexMetrics.ts

# Hex geometry
const OUTER_RADIUS: float = 1.0  # Corner to center distance
const INNER_RADIUS: float = OUTER_RADIUS * 0.866025404  # Edge to center (outer * sqrt(3)/2)

# Elevation
const ELEVATION_STEP: float = 0.4  # Height per elevation level
const MAX_ELEVATION: int = 8
const MIN_ELEVATION: int = -2
const WATER_LEVEL: int = 0  # Sea level

# Terraces (Catlike Coding style)
const TERRACES_PER_SLOPE: int = 2  # Number of flat terraces per slope


static func get_terrace_steps() -> int:
	return TERRACES_PER_SLOPE * 2 + 1


static func get_horizontal_terrace_step_size() -> float:
	return 1.0 / get_terrace_steps()


static func get_vertical_terrace_step_size() -> float:
	return 1.0 / (TERRACES_PER_SLOPE + 1)


# Blend regions
# TODO: Implement proper Catlike Coding style edge/corner connections
# For now, use full hexes (1.0) to avoid gaps
const SOLID_FACTOR: float = 1.0  # Inner solid hex portion (1.0 = full hex, no gaps)
const BLEND_FACTOR: float = 0.0  # Outer blend portion


## Get the 6 corner positions for a hex (flat-topped, starting at 30 degrees)
static func get_corners() -> Array[Vector3]:
	var corners: Array[Vector3] = []
	for i in range(6):
		var angle = (PI / 3.0) * i + PI / 6.0  # Start at 30 degrees
		corners.append(Vector3(
			cos(angle) * OUTER_RADIUS,
			0,
			sin(angle) * OUTER_RADIUS
		))
	return corners


## Get corner at specific index (with wrapping)
static func get_corner(index: int) -> Vector3:
	var corners = get_corners()
	return corners[((index % 6) + 6) % 6]


## Terrace interpolation - the key to Catlike Coding style terraces
## Horizontal interpolation is linear, vertical only changes on odd steps
static func terrace_lerp(a: Vector3, b: Vector3, step: int) -> Vector3:
	var h = step * get_horizontal_terrace_step_size()
	var v = floor((step + 1) / 2.0) * get_vertical_terrace_step_size()

	return Vector3(
		a.x + (b.x - a.x) * h,
		a.y + (b.y - a.y) * v,
		a.z + (b.z - a.z) * h
	)


## Interpolate color along terrace
static func terrace_color_lerp(a: Color, b: Color, step: int) -> Color:
	var h = step * get_horizontal_terrace_step_size()
	return a.lerp(b, h)
