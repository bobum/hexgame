using HexGame.Core;

namespace HexGame.Pathfinding;

/// <summary>
/// Movement strategy for land units.
/// </summary>
public class LandMovementStrategy : IMovementStrategy
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static readonly LandMovementStrategy Instance = new();

    /// <summary>
    /// Base movement costs for each terrain type (land units).
    /// </summary>
    private static readonly Dictionary<TerrainType, float> TerrainCosts = new()
    {
        { TerrainType.Plains, GameConstants.Movement.BaseCost },
        { TerrainType.Coast, GameConstants.Movement.BaseCost },
        { TerrainType.Desert, GameConstants.Movement.BaseCost },
        { TerrainType.Savanna, GameConstants.Movement.BaseCost },
        { TerrainType.Forest, GameConstants.Movement.ForestCost },
        { TerrainType.Taiga, GameConstants.Movement.ForestCost },
        { TerrainType.Jungle, GameConstants.Movement.JungleCost },
        { TerrainType.Tundra, GameConstants.Movement.ForestCost },
        { TerrainType.Hills, GameConstants.Movement.HillsCost },
        { TerrainType.Snow, GameConstants.Movement.SnowCost },
        { TerrainType.Mountains, float.PositiveInfinity },
        { TerrainType.Ocean, float.PositiveInfinity }
    };

    private LandMovementStrategy() { }

    public float GetMovementCost(HexCell from, HexCell to)
    {
        // Water is impassable for land units
        if (HexMetrics.IsWaterElevation(to.Elevation))
        {
            return float.PositiveInfinity;
        }

        // Get base terrain cost
        float cost = TerrainCosts.TryGetValue(to.TerrainType, out var baseCost)
            ? baseCost
            : float.PositiveInfinity;

        // If base terrain is impassable, return early
        if (float.IsPositiveInfinity(cost))
        {
            return float.PositiveInfinity;
        }

        // Elevation difference penalty
        int elevDiff = to.Elevation - from.Elevation;

        // Cliffs (2+ elevation difference) are impassable
        if (Math.Abs(elevDiff) >= GameConstants.Movement.CliffElevationDifference)
        {
            return float.PositiveInfinity;
        }

        // Climbing penalty - going uphill costs more
        if (elevDiff > 0)
        {
            cost += elevDiff * GameConstants.Movement.ClimbingPenaltyMultiplier;
        }

        // River crossing penalty
        if (CrossesRiver(from, to))
        {
            cost += GameConstants.Movement.RiverCrossingCost;
        }

        return cost;
    }

    public bool IsPassable(HexCell cell)
    {
        if (HexMetrics.IsWaterElevation(cell.Elevation))
        {
            return false;
        }
        if (cell.TerrainType is TerrainType.Mountains or TerrainType.Ocean)
        {
            return false;
        }
        return true;
    }

    public bool CanMoveBetween(HexCell from, HexCell to)
    {
        return !float.IsPositiveInfinity(GetMovementCost(from, to));
    }

    private static bool CrossesRiver(HexCell from, HexCell to)
    {
        // Check if there's a river between these cells
        // A river crossing occurs if either cell has a river edge facing the other
        if (!from.HasRiver && !to.HasRiver)
        {
            return false;
        }

        // Find the direction from 'from' to 'to'
        for (int dir = 0; dir < 6; dir++)
        {
            var offset = ((HexDirection)dir).GetOffset();
            if (from.Q + offset.X == to.Q && from.R + offset.Y == to.R)
            {
                // Check if 'from' has a river flowing in this direction
                if (from.RiverDirections.Contains(dir))
                {
                    return true;
                }

                // Check if 'to' has a river flowing in the opposite direction (toward 'from')
                int oppositeDir = (dir + 3) % 6;
                if (to.RiverDirections.Contains(oppositeDir))
                {
                    return true;
                }

                break;
            }
        }

        return false;
    }
}

/// <summary>
/// Movement strategy for naval units.
/// </summary>
public class NavalMovementStrategy : IMovementStrategy
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static readonly NavalMovementStrategy Instance = new();

    /// <summary>
    /// Base movement costs for each terrain type (naval units).
    /// </summary>
    private static readonly Dictionary<TerrainType, float> TerrainCosts = new()
    {
        { TerrainType.Ocean, GameConstants.Movement.OceanCost },
        { TerrainType.Coast, GameConstants.Movement.CoastCost },
        { TerrainType.Plains, float.PositiveInfinity },
        { TerrainType.Desert, float.PositiveInfinity },
        { TerrainType.Savanna, float.PositiveInfinity },
        { TerrainType.Forest, float.PositiveInfinity },
        { TerrainType.Taiga, float.PositiveInfinity },
        { TerrainType.Jungle, float.PositiveInfinity },
        { TerrainType.Tundra, float.PositiveInfinity },
        { TerrainType.Hills, float.PositiveInfinity },
        { TerrainType.Snow, float.PositiveInfinity },
        { TerrainType.Mountains, float.PositiveInfinity }
    };

    private NavalMovementStrategy() { }

    public float GetMovementCost(HexCell from, HexCell to)
    {
        // Naval units can move on water
        bool isWaterCell = HexMetrics.IsWaterElevation(to.Elevation) ||
                          to.TerrainType is TerrainType.Ocean or TerrainType.Coast;

        if (!isWaterCell)
        {
            return float.PositiveInfinity;
        }

        // Get base terrain cost
        float cost = TerrainCosts.TryGetValue(to.TerrainType, out var baseCost)
            ? baseCost
            : float.PositiveInfinity;

        // If terrain type isn't in naval costs but cell is water, use default
        if (float.IsPositiveInfinity(cost) && HexMetrics.IsWaterElevation(to.Elevation))
        {
            cost = GameConstants.Movement.OceanCost;
        }

        return cost;
    }

    public bool IsPassable(HexCell cell)
    {
        if (HexMetrics.IsWaterElevation(cell.Elevation))
        {
            return true;
        }
        if (cell.TerrainType is TerrainType.Ocean or TerrainType.Coast)
        {
            return true;
        }
        return false;
    }

    public bool CanMoveBetween(HexCell from, HexCell to)
    {
        return !float.IsPositiveInfinity(GetMovementCost(from, to));
    }
}

/// <summary>
/// Movement strategy for amphibious units.
/// </summary>
public class AmphibiousMovementStrategy : IMovementStrategy
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static readonly AmphibiousMovementStrategy Instance = new();


    private AmphibiousMovementStrategy() { }

    public float GetMovementCost(HexCell from, HexCell to)
    {
        float landCost = LandMovementStrategy.Instance.GetMovementCost(from, to);
        float navalCost = NavalMovementStrategy.Instance.GetMovementCost(from, to);

        // Use the better of the two costs
        float baseCost = Math.Min(landCost, navalCost);

        if (float.IsPositiveInfinity(baseCost))
        {
            return float.PositiveInfinity;
        }

        // Add embark/disembark cost when transitioning between land and water
        bool fromWater = HexMetrics.IsWaterElevation(from.Elevation);
        bool toWater = HexMetrics.IsWaterElevation(to.Elevation);

        if (fromWater != toWater)
        {
            baseCost += GameConstants.Movement.EmbarkDisembarkCost;
        }

        return baseCost;
    }

    public bool IsPassable(HexCell cell)
    {
        return LandMovementStrategy.Instance.IsPassable(cell) ||
               NavalMovementStrategy.Instance.IsPassable(cell);
    }

    public bool CanMoveBetween(HexCell from, HexCell to)
    {
        return !float.IsPositiveInfinity(GetMovementCost(from, to));
    }
}

/// <summary>
/// Static helper class for movement cost calculations.
/// </summary>
public static class MovementCosts
{
    /// <summary>
    /// Gets movement cost for a land unit.
    /// </summary>
    public static float GetLandMovementCost(HexCell from, HexCell to)
        => LandMovementStrategy.Instance.GetMovementCost(from, to);

    /// <summary>
    /// Gets movement cost for a naval unit.
    /// </summary>
    public static float GetNavalMovementCost(HexCell from, HexCell to)
        => NavalMovementStrategy.Instance.GetMovementCost(from, to);

    /// <summary>
    /// Gets movement cost based on unit type.
    /// </summary>
    public static float GetMovementCostForUnit(HexCell from, HexCell to, UnitType unitType)
        => MovementStrategyFactory.Create(unitType).GetMovementCost(from, to);

    /// <summary>
    /// Gets movement cost (legacy - assumes land unit).
    /// </summary>
    public static float GetMovementCost(HexCell from, HexCell to)
        => GetLandMovementCost(from, to);

    /// <summary>
    /// Checks if cell is passable for a unit type.
    /// </summary>
    public static bool IsPassableForUnit(HexCell cell, UnitType unitType)
        => MovementStrategyFactory.Create(unitType).IsPassable(cell);

    /// <summary>
    /// Checks if cell is passable (legacy - assumes land unit).
    /// </summary>
    public static bool IsPassable(HexCell cell)
        => LandMovementStrategy.Instance.IsPassable(cell);
}
