class_name UnitTypes
extends RefCounted
## Unit type definitions, stats, and domain classifications.
## Matches web/src/units/UnitTypes.ts

## Domain determines where a unit can move.
enum Domain {
	LAND,        # Can only move on land (elevation >= 0, not water)
	NAVAL,       # Can only move on water (elevation < 0)
	AMPHIBIOUS,  # Can move on both land and water
}

## All available unit types.
enum Type {
	# Land units
	INFANTRY,
	CAVALRY,
	ARCHER,
	# Naval units
	GALLEY,
	WARSHIP,
	# Amphibious units
	MARINE,
}

## Static stats for each unit type.
const STATS: Dictionary = {
	Type.INFANTRY: {
		"domain": Domain.LAND,
		"health": 100,
		"movement": 2,
		"attack": 10,
		"defense": 8,
		"name": "Infantry",
		"description": "Basic land unit. Slow but sturdy.",
	},
	Type.CAVALRY: {
		"domain": Domain.LAND,
		"health": 80,
		"movement": 4,
		"attack": 12,
		"defense": 5,
		"name": "Cavalry",
		"description": "Fast land unit. Good for flanking.",
	},
	Type.ARCHER: {
		"domain": Domain.LAND,
		"health": 60,
		"movement": 2,
		"attack": 15,
		"defense": 3,
		"name": "Archer",
		"description": "Ranged land unit. High attack, low defense.",
	},
	Type.GALLEY: {
		"domain": Domain.NAVAL,
		"health": 80,
		"movement": 3,
		"attack": 8,
		"defense": 6,
		"name": "Galley",
		"description": "Light naval unit. Fast and maneuverable.",
	},
	Type.WARSHIP: {
		"domain": Domain.NAVAL,
		"health": 150,
		"movement": 2,
		"attack": 20,
		"defense": 12,
		"name": "Warship",
		"description": "Heavy naval unit. Slow but powerful.",
	},
	Type.MARINE: {
		"domain": Domain.AMPHIBIOUS,
		"health": 70,
		"movement": 2,
		"attack": 8,
		"defense": 6,
		"name": "Marine",
		"description": "Amphibious unit. Can move on land and water.",
	},
}


static func get_stats(unit_type: Type) -> Dictionary:
	return STATS.get(unit_type, {})


static func get_domain(unit_type: Type) -> Domain:
	return STATS[unit_type]["domain"]


static func get_name(unit_type: Type) -> String:
	return STATS[unit_type]["name"]


static func can_traverse_land(unit_type: Type) -> bool:
	var domain = get_domain(unit_type)
	return domain == Domain.LAND or domain == Domain.AMPHIBIOUS


static func can_traverse_water(unit_type: Type) -> bool:
	var domain = get_domain(unit_type)
	return domain == Domain.NAVAL or domain == Domain.AMPHIBIOUS


static func get_land_types() -> Array[Type]:
	return [Type.INFANTRY, Type.CAVALRY, Type.ARCHER]


static func get_naval_types() -> Array[Type]:
	return [Type.GALLEY, Type.WARSHIP]


static func is_naval(unit_type: Type) -> bool:
	var domain = get_domain(unit_type)
	return domain == Domain.NAVAL


static func is_amphibious(unit_type: Type) -> bool:
	var domain = get_domain(unit_type)
	return domain == Domain.AMPHIBIOUS
