using Godot;
using Godot.Collections;


//# Unit type definitions, stats, and domain classifications.

//# Matches web/src/units/UnitTypes.ts
//# Domain determines where a unit can move.
[GlobalClass]
public partial class UnitTypes : Godot.RefCounted
{
	public enum Domain {LAND,  Can only move on land (elevation >= SEA_LEVEL), NAVAL,  Can only move on water (elevation < SEA_LEVEL), AMPHIBIOUS,  Can move on both land and water}


	//# All available unit types.
	public enum Type { Land units, INFANTRY, CAVALRY, ARCHER,  Naval units, GALLEY, WARSHIP,  Amphibious units, MARINE}


	//# Static stats for each unit type.
	public const Dictionary STATS = new Dictionary{
			{Type.Infantry, new Dictionary{
						{"domain", Domain.Land},
						{"health", 100},
						{"movement", 2},
						{"attack", 10},
						{"defense", 8},
						{"name", "Infantry"},
						{"description", "Basic land unit. Slow but sturdy."},
						}},
			{Type.Cavalry, new Dictionary{
						{"domain", Domain.Land},
						{"health", 80},
						{"movement", 4},
						{"attack", 12},
						{"defense", 5},
						{"name", "Cavalry"},
						{"description", "Fast land unit. Good for flanking."},
						}},
			{Type.Archer, new Dictionary{
						{"domain", Domain.Land},
						{"health", 60},
						{"movement", 2},
						{"attack", 15},
						{"defense", 3},
						{"name", "Archer"},
						{"description", "Ranged land unit. High attack, low defense."},
						}},
			{Type.Galley, new Dictionary{
						{"domain", Domain.Naval},
						{"health", 80},
						{"movement", 3},
						{"attack", 8},
						{"defense", 6},
						{"name", "Galley"},
						{"description", "Light naval unit. Fast and maneuverable."},
						}},
			{Type.Warship, new Dictionary{
						{"domain", Domain.Naval},
						{"health", 150},
						{"movement", 2},
						{"attack", 20},
						{"defense", 12},
						{"name", "Warship"},
						{"description", "Heavy naval unit. Slow but powerful."},
						}},
			{Type.Marine, new Dictionary{
						{"domain", Domain.Amphibious},
						{"health", 70},
						{"movement", 2},
						{"attack", 8},
						{"defense", 6},
						{"name", "Marine"},
						{"description", "Amphibious unit. Can move on land and water."},
						}},
			};


	public static Dictionary GetStats(Type unit_type)
	{
		return STATS.Get(unit_type, new Dictionary{});
	}


	public static Domain GetDomain(Type unit_type)
	{
		return STATS[unit_type]["domain"];
	}


	public static String GetName(Type unit_type)
	{
		return STATS[unit_type]["name"];
	}


	public static bool CanTraverseLand(Type unit_type)
	{
		var domain = GetDomain(unit_type);
		return domain == Domain.Land || domain == Domain.Amphibious;
	}


	public static bool CanTraverseWater(Type unit_type)
	{
		var domain = GetDomain(unit_type);
		return domain == Domain.Naval || domain == Domain.Amphibious;
	}


	public static Array<Type> GetLandTypes()
	{
		return new Array{Type.Infantry, Type.Cavalry, Type.Archer, };
	}


	public static Array<Type> GetNavalTypes()
	{
		return new Array{Type.Galley, Type.Warship, };
	}


	public static bool IsNaval(Type unit_type)
	{
		var domain = GetDomain(unit_type);
		return domain == Domain.Naval;
	}


	public static bool IsAmphibious(Type unit_type)
	{
		var domain = GetDomain(unit_type);
		return domain == Domain.Amphibious;
	}


}