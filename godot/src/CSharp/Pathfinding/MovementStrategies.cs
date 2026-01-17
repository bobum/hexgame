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
    /// River crossing cost.
    /// </summary>
    public const float RiverCrossingCost = 1.0f;

    /// <summary>
    /// Base movement costs for each terrain type (land units).
    /// </summary>
    private static readonly Dictionary<TerrainType, float> TerrainCosts = new()
    {
        { TerrainType.Plains, 1.0f },
        { TerrainType.Coast, 1.0f },
        { TerrainType.Desert, 1.0f },
        { TerrainType.Savanna, 1.0f },
        { TerrainType.Forest, 1.5f },
        { TerrainType.Taiga, 1.5f },
        { TerrainType.Jungle, 2.0f },
        { TerrainType.Tundra, 1.5f },
        { TerrainType.Hills, 2.0f },
        { TerrainType.Snow, 2.5f },
        { TerrainType.Mountains, float.PositiveInfinity },
        { TerrainType.Ocean, float.PositiveInfinity }
    };

    private LandMovementStrategy() { }

    public float GetMovementCost(HexCell from, HexCell to)
    {
        // Water is impassable for land units
        if (to.Elevation < HexMetrics.SeaLevel)
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
        if (Math.Abs(elevDiff) >= 2)
        {
            return float.PositiveInfinity;
        }

        // Climbing penalty - going uphill costs more
        if (elevDiff > 0)
        {
            cost += elevDiff * 0.5f;
        }

        // River crossing penalty
        if (CrossesRiver(from, to))
        {
            cost += RiverCrossingCost;
        }

        return cost;
    }

    public bool IsPassable(HexCell cell)
    {
        if (cell.Elevation < HexMetrics.SeaLevel)
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

        // TODO: Implement proper river crossing detection
        // For now, simplified check
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
        { TerrainType.Ocean, 1.0f },
        { TerrainType.Coast, 1.5f },
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
        bool isWaterCell = to.Elevation < HexMetrics.SeaLevel ||
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
        if (float.IsPositiveInfinity(cost) && to.Elevation < HexMetrics.SeaLevel)
        {
            cost = 1.0f;
        }

        return cost;
    }

    public bool IsPassable(HexCell cell)
    {
        if (cell.Elevation < HexMetrics.SeaLevel)
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

    /// <summary>
    /// Extra cost for embarking/disembarking.
    /// </summary>
    public const float EmbarkCost = 1.0f;

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
        bool fromWater = from.Elevation < HexMetrics.SeaLevel;
        bool toWater = to.Elevation < HexMetrics.SeaLevel;

        if (fromWater != toWater)
        {
            baseCost += EmbarkCost;
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
