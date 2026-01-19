using HexGame.Units;

namespace HexGame.Pathfinding;

/// <summary>
/// Interface for movement cost calculation strategies.
/// Implements the Strategy pattern to handle different unit movement types.
/// </summary>
public interface IMovementStrategy
{
    /// <summary>
    /// Calculates the movement cost to traverse from one cell to an adjacent cell.
    /// </summary>
    /// <param name="from">Starting cell.</param>
    /// <param name="to">Destination cell.</param>
    /// <returns>Movement cost, or float.PositiveInfinity if impassable.</returns>
    float GetMovementCost(HexCell from, HexCell to);

    /// <summary>
    /// Checks if a cell is passable for this movement type.
    /// </summary>
    /// <param name="cell">The cell to check.</param>
    /// <returns>True if the cell can be traversed.</returns>
    bool IsPassable(HexCell cell);

    /// <summary>
    /// Checks if movement between two adjacent cells is possible.
    /// </summary>
    /// <param name="from">Starting cell.</param>
    /// <param name="to">Destination cell.</param>
    /// <returns>True if movement is possible.</returns>
    bool CanMoveBetween(HexCell from, HexCell to);
}

/// <summary>
/// Factory for creating movement strategies based on unit type.
/// </summary>
public static class MovementStrategyFactory
{
    /// <summary>
    /// Creates the appropriate movement strategy for a unit type.
    /// </summary>
    /// <param name="unitType">The type of unit.</param>
    /// <returns>The appropriate movement strategy.</returns>
    public static IMovementStrategy Create(UnitType unitType)
    {
        return unitType.GetDomain() switch
        {
            UnitDomain.Naval => NavalMovementStrategy.Instance,
            UnitDomain.Amphibious => AmphibiousMovementStrategy.Instance,
            _ => LandMovementStrategy.Instance
        };
    }

    /// <summary>
    /// Creates the appropriate movement strategy for a unit.
    /// </summary>
    /// <param name="unit">The unit.</param>
    /// <returns>The appropriate movement strategy.</returns>
    public static IMovementStrategy Create(Unit unit)
    {
        return Create(unit.Type);
    }
}
