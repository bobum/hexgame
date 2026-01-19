using Godot;
using Godot.Collections;


//# Movement cost calculations for pathfinding.

//# Matches web/src/pathfinding/MovementCosts.ts
[GlobalClass]
public partial class MovementCosts : Godot.RefCounted
{
	public const double RIVER_CROSSING_COST = 1.0;


	//# Base movement cost for each terrain type (land units).
	//# Lower = easier to traverse. INF = impassable.
	public const Dictionary LAND_TERRAIN_COSTS = new Dictionary{
			{TerrainType.Type.PLAINS, 1.0},
			{TerrainType.Type.COAST, 1.0},
			{TerrainType.Type.DESERT, 1.0},
			{TerrainType.Type.SAVANNA, 1.0},
			{TerrainType.Type.FOREST, 1.5},
			{TerrainType.Type.TAIGA, 1.5},
			{TerrainType.Type.JUNGLE, 2.0},
			{TerrainType.Type.TUNDRA, 1.5},
			{TerrainType.Type.HILLS, 2.0},
			{TerrainType.Type.SNOW, 2.5},
			{TerrainType.Type.MOUNTAINS, Mathf.Inf},
			// Impassable
			{TerrainType.Type.OCEAN, Mathf.Inf},
			// Impassable for land units
			};


	//# Base movement cost for each terrain type (naval units).
	public const Dictionary NAVAL_TERRAIN_COSTS = new Dictionary{
			{TerrainType.Type.OCEAN, 1.0},
			// Open water - easy sailing
			{TerrainType.Type.COAST, 1.5},
			// Coastal waters - slightly harder
			{TerrainType.Type.PLAINS, Mathf.Inf},
			{TerrainType.Type.DESERT, Mathf.Inf},
			{TerrainType.Type.SAVANNA, Mathf.Inf},
			{TerrainType.Type.FOREST, Mathf.Inf},
			{TerrainType.Type.TAIGA, Mathf.Inf},
			{TerrainType.Type.JUNGLE, Mathf.Inf},
			{TerrainType.Type.TUNDRA, Mathf.Inf},
			{TerrainType.Type.HILLS, Mathf.Inf},
			{TerrainType.Type.SNOW, Mathf.Inf},
			{TerrainType.Type.MOUNTAINS, Mathf.Inf},
			};


	//# Calculate the movement cost for a LAND unit to move from one cell to an adjacent cell.
	public static double GetLandMovementCost(Godot.HexCell from, Godot.HexCell to)
	{

		// Water is impassable for land units (elevation below sea level)
		if(to.Elevation < HexMetrics.SEA_LEVEL)
		{
			return Mathf.Inf;
		}


		// Get base terrain cost
		var cost = LAND_TERRAIN_COSTS.Get(to.TerrainType, Mathf.Inf);


		// If base terrain is impassable, return early
		if(!Mathf.IsFinite(cost))
		{
			return Mathf.Inf;
		}


		// Elevation difference penalty
		var elev_diff = to.Elevation - from.Elevation;


		// Cliffs (2+ elevation difference) are impassable
		if(Mathf.Abs(elev_diff) >= 2)
		{
			return Mathf.Inf;
		}


		// Climbing penalty - going uphill costs more
		if(elev_diff > 0)
		{
			cost += elev_diff * 0.5;


			// TODO: River crossing penalty (when rivers are implemented)
			// if crosses_river(from, to):

		}
		//     cost += RIVER_CROSSING_COST
		return cost;
	}


	//# Calculate the movement cost for a NAVAL unit to move from one cell to an adjacent cell.
	public static double GetNavalMovementCost(Godot.HexCell _from, Godot.HexCell to)
	{

		// Naval units can move on water (elevation below sea level) or Ocean/Coast terrain
		var is_water_cell = to.Elevation < HexMetrics.SEA_LEVEL || to.TerrainType == TerrainType.Type.OCEAN || to.TerrainType == TerrainType.Type.COAST;

		if(!is_water_cell)
		{
			return Mathf.Inf;
		}


		// Get base terrain cost for naval
		var cost = NAVAL_TERRAIN_COSTS.Get(to.TerrainType, Mathf.Inf);


		// If terrain type isn't in naval costs but cell is water, use default cost
		if(!Mathf.IsFinite(cost) && to.Elevation < HexMetrics.SEA_LEVEL)
		{
			cost = 1.0;
		}

		return cost;
	}


	//# Calculate the movement cost based on unit type (domain-aware).
	public static double GetMovementCostForUnit(Godot.HexCell from, Godot.HexCell to, UnitTypes.Type unit_type)
	{
		var domain = UnitTypes.GetDomain(unit_type);

		if(domain == UnitTypes.Domain.NAVAL)
		{
			return GetNavalMovementCost(from, to);
		}

		if(domain == UnitTypes.Domain.AMPHIBIOUS)
		{

			// Amphibious units can use either cost, pick the better one
			var land_cost = GetLandMovementCost(from, to);
			var naval_cost = GetNavalMovementCost(from, to);
			return Mathf.Min(land_cost, naval_cost);
		}


		// Default: land movement
		return GetLandMovementCost(from, to);
	}


	//# Legacy function - assumes land unit
	public static double GetMovementCost(Godot.HexCell from, Godot.HexCell to)
	{
		return GetLandMovementCost(from, to);
	}


	//# Check if a cell is passable for land units.
	public static bool IsPassableForLand(Godot.HexCell cell)
	{
		if(cell.Elevation < HexMetrics.SEA_LEVEL)
		{
			return false;
		}
		if(cell.TerrainType == TerrainType.Type.MOUNTAINS)
		{
			return false;
		}
		if(cell.TerrainType == TerrainType.Type.OCEAN)
		{
			return false;
		}
		return true;
	}


	//# Check if a cell is passable for naval units.
	public static bool IsPassableForNaval(Godot.HexCell cell)
	{

		// Naval can go on water (elevation below sea level) or Ocean/Coast terrain
		if(cell.Elevation < HexMetrics.SEA_LEVEL)
		{
			return true;
		}
		if(cell.TerrainType == TerrainType.Type.OCEAN)
		{
			return true;
		}
		if(cell.TerrainType == TerrainType.Type.COAST)
		{
			return true;
		}
		return false;
	}


	//# Check if a cell is passable for a specific unit type.
	public static bool IsPassableForUnit(Godot.HexCell cell, UnitTypes.Type unit_type)
	{
		var domain = UnitTypes.GetDomain(unit_type);

		if(domain == UnitTypes.Domain.NAVAL)
		{
			return IsPassableForNaval(cell);
		}

		if(domain == UnitTypes.Domain.AMPHIBIOUS)
		{
			return IsPassableForLand(cell) || IsPassableForNaval(cell);
		}

		return IsPassableForLand(cell);
	}


	//# Check if a cell is passable at all (legacy - assumes land)
	public static bool IsPassable(Godot.HexCell cell)
	{
		return IsPassableForLand(cell);
	}


	//# Check if movement between two adjacent cells is possible.
	public static bool CanMoveBetween(Godot.HexCell from, Godot.HexCell to)
	{
		return Mathf.IsFinite(GetMovementCost(from, to));
	}


	//# Check if movement between two adjacent cells is possible for a specific unit type.
	public static bool CanMoveBetweenForUnit(Godot.HexCell from, Godot.HexCell to, UnitTypes.Type unit_type)
	{
		return Mathf.IsFinite(GetMovementCostForUnit(from, to, unit_type));
	}


}