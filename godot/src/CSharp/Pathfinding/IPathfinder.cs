using HexGame.Units;
using System.Threading;

namespace HexGame.Pathfinding;

/// <summary>
/// Interface for pathfinding services.
/// Extends IService for ServiceLocator registration.
/// </summary>
public interface IPathfinder : IService
{
    /// <summary>
    /// Finds the optimal path between two cells using A* algorithm.
    /// </summary>
    /// <param name="start">Starting cell.</param>
    /// <param name="end">Destination cell.</param>
    /// <param name="options">Pathfinding options.</param>
    /// <returns>Result containing path, cost, and reachable status.</returns>
    PathResult FindPath(HexCell start, HexCell end, PathOptions? options = null);

    /// <summary>
    /// Gets all cells reachable from a starting cell within a movement budget.
    /// </summary>
    /// <param name="start">Starting cell.</param>
    /// <param name="movementPoints">Available movement points.</param>
    /// <param name="options">Pathfinding options.</param>
    /// <returns>Dictionary of reachable cells and their costs.</returns>
    Dictionary<HexCell, float> GetReachableCells(HexCell start, float movementPoints, PathOptions? options = null);

    /// <summary>
    /// Checks if a path exists between two cells.
    /// </summary>
    /// <param name="start">Starting cell.</param>
    /// <param name="end">Destination cell.</param>
    /// <param name="ignoreUnits">Whether to ignore units when pathfinding.</param>
    /// <returns>True if a path exists.</returns>
    bool HasPath(HexCell start, HexCell end, bool ignoreUnits = false);

    /// <summary>
    /// Gets the movement cost between two adjacent cells.
    /// </summary>
    /// <param name="from">Starting cell.</param>
    /// <param name="to">Destination cell.</param>
    /// <param name="unitType">Optional unit type for domain-aware cost calculation.</param>
    /// <returns>Movement cost, or infinity if not adjacent or impassable.</returns>
    float GetStepCost(HexCell from, HexCell to, UnitType? unitType = null);

    /// <summary>
    /// Finds the optimal path between two cells asynchronously.
    /// Useful for large maps where pathfinding might take significant time.
    /// </summary>
    /// <param name="start">Starting cell.</param>
    /// <param name="end">Destination cell.</param>
    /// <param name="options">Pathfinding options.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Task containing the path result.</returns>
    Task<PathResult> FindPathAsync(HexCell start, HexCell end, PathOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all reachable cells from a starting cell asynchronously.
    /// Useful for large movement ranges where calculation might take significant time.
    /// </summary>
    /// <param name="start">Starting cell.</param>
    /// <param name="movementPoints">Available movement points.</param>
    /// <param name="options">Pathfinding options.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Task containing the dictionary of reachable cells.</returns>
    Task<Dictionary<HexCell, float>> GetReachableCellsAsync(HexCell start, float movementPoints, PathOptions? options = null, CancellationToken cancellationToken = default);
}
