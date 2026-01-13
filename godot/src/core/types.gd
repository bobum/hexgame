class_name TerrainType
extends RefCounted
## Terrain type definitions
## Matches web/src/types/index.ts

enum Type {
	OCEAN,
	COAST,
	PLAINS,
	FOREST,
	HILLS,
	MOUNTAINS,
	SNOW,
	DESERT,
	TUNDRA,
	JUNGLE,
	SAVANNA,
	TAIGA
}

# Terrain colors - stylized low-poly palette
const COLORS: Dictionary = {
	Type.OCEAN: Color(0.102, 0.298, 0.431),      # 0x1a4c6e - Deep blue
	Type.COAST: Color(0.176, 0.545, 0.788),      # 0x2d8bc9 - Light blue
	Type.PLAINS: Color(0.486, 0.702, 0.259),     # 0x7cb342 - Grass green
	Type.FOREST: Color(0.180, 0.490, 0.196),     # 0x2e7d32 - Dark green
	Type.HILLS: Color(0.553, 0.431, 0.388),      # 0x8d6e63 - Brown
	Type.MOUNTAINS: Color(0.459, 0.459, 0.459),  # 0x757575 - Gray
	Type.SNOW: Color(0.925, 0.937, 0.945),       # 0xeceff1 - White
	Type.DESERT: Color(0.902, 0.784, 0.431),     # 0xe6c86e - Sand yellow
	Type.TUNDRA: Color(0.565, 0.643, 0.682),     # 0x90a4ae - Blue-gray
	Type.JUNGLE: Color(0.106, 0.369, 0.125),     # 0x1b5e20 - Deep green
	Type.SAVANNA: Color(0.773, 0.659, 0.333),    # 0xc5a855 - Golden brown
	Type.TAIGA: Color(0.290, 0.388, 0.365),      # 0x4a635d - Dark teal-green
}


static func get_color(terrain: Type) -> Color:
	return COLORS.get(terrain, Color.WHITE)


static func is_water(terrain: Type) -> bool:
	return terrain == Type.OCEAN or terrain == Type.COAST


static func get_name(terrain: Type) -> String:
	match terrain:
		Type.OCEAN: return "Ocean"
		Type.COAST: return "Coast"
		Type.PLAINS: return "Plains"
		Type.FOREST: return "Forest"
		Type.HILLS: return "Hills"
		Type.MOUNTAINS: return "Mountains"
		Type.SNOW: return "Snow"
		Type.DESERT: return "Desert"
		Type.TUNDRA: return "Tundra"
		Type.JUNGLE: return "Jungle"
		Type.SAVANNA: return "Savanna"
		Type.TAIGA: return "Taiga"
		_: return "Unknown"
