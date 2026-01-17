using HexGame.Units;

namespace HexGame.Pathfinding;

/// <summary>
/// A* pathfinder for hex grids.
/// Finds optimal paths considering terrain costs, elevation, and unit obstacles.
/// </summary>
public class Pathfinder
{
    private readonly HexGrid _grid;
    private readonly IUnitManager? _unitManager;

    /// <summary>
    /// Creates a new pathfinder.
    /// </summary>
    /// <param name="grid">The hex grid.</param>
    /// <param name="unitManager">Optional unit manager for collision avoidance.</param>
    public Pathfinder(HexGrid grid, IUnitManager? unitManager = null)
    {
        _grid = grid;
        _unitManager = unitManager;
    }

    /// <summary>
    /// Finds the optimal path between two cells using A* algorithm.
    /// </summary>
    /// <param name="start">Starting cell.</param>
    /// <param name="end">Destination cell.</param>
    /// <param name="options">Pathfinding options.</param>
    /// <returns>Result containing path, cost, and reachable status.</returns>
    public PathResult FindPath(HexCell start, HexCell end, PathOptions? options = null)
    {
        options ??= new PathOptions();

        IMovementStrategy strategy = options.UnitType.HasValue
            ? MovementStrategyFactory.Create(options.UnitType.Value)
            : LandMovementStrategy.Instance;

        // Quick check: destination must be passable
        if (!strategy.IsPassable(end))
        {
            return PathResult.NotReachable;
        }

        // Quick check: destination can't have a unit (unless ignoring units)
        if (!options.IgnoreUnits && _unitManager != null)
        {
            if (_unitManager.GetUnitAt(end.Q, end.R) != null)
            {
                return PathResult.NotReachable;
            }
        }

        // Same cell - trivial path
        if (start.Q == end.Q && start.R == end.R)
        {
            return new PathResult(new List<HexCell> { start }, 0, true);
        }

        var frontier = new PriorityQueue<HexCell, float>();
        var cameFrom = new Dictionary<string, HexCell?>();
        var costSoFar = new Dictionary<string, float>();

        string startKey = CellKey(start);
        string endKey = CellKey(end);

        frontier.Enqueue(start, 0);
        cameFrom[startKey] = null;
        costSoFar[startKey] = 0;

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            string currentKey = CellKey(current);

            // Found destination
            if (currentKey == endKey)
            {
                return new PathResult(
                    ReconstructPath(cameFrom, start, end),
                    costSoFar[endKey],
                    true
                );
            }

            // Explore neighbors
            foreach (var neighbor in _grid.GetNeighbors(current))
            {
                // Skip if there's a unit (unless ignoring units)
                if (!options.IgnoreUnits && _unitManager != null)
                {
                    var unitAtNeighbor = _unitManager.GetUnitAt(neighbor.Q, neighbor.R);
                    // Allow destination even if pathfinding toward a unit (for attack targeting)
                    if (unitAtNeighbor != null && CellKey(neighbor) != endKey)
                    {
                        continue;
                    }
                }

                // Calculate movement cost
                float moveCost = strategy.GetMovementCost(current, neighbor);

                // Skip impassable terrain
                if (float.IsPositiveInfinity(moveCost))
                {
                    continue;
                }

                float newCost = costSoFar[currentKey] + moveCost;

                // Skip if exceeds max cost
                if (newCost > options.MaxCost)
                {
                    continue;
                }

                string neighborKey = CellKey(neighbor);

                if (!costSoFar.TryGetValue(neighborKey, out var existingCost) || newCost < existingCost)
                {
                    costSoFar[neighborKey] = newCost;

                    // A* priority = cost so far + heuristic estimate to goal
                    float priority = newCost + Heuristic(neighbor, end);
                    frontier.Enqueue(neighbor, priority);
                    cameFrom[neighborKey] = current;
                }
            }
        }

        // No path found
        return PathResult.NotReachable;
    }

    /// <summary>
    /// Gets all cells reachable from a starting cell within a movement budget.
    /// </summary>
    /// <param name="start">Starting cell.</param>
    /// <param name="movementPoints">Available movement points.</param>
    /// <param name="options">Pathfinding options.</param>
    /// <returns>Dictionary of reachable cells and their costs.</returns>
    public Dictionary<HexCell, float> GetReachableCells(HexCell start, float movementPoints, PathOptions? options = null)
    {
        options ??= new PathOptions();

        IMovementStrategy strategy = options.UnitType.HasValue
            ? MovementStrategyFactory.Create(options.UnitType.Value)
            : LandMovementStrategy.Instance;

        var reachable = new Dictionary<HexCell, float>();
        var frontier = new PriorityQueue<HexCell, float>();
        var costSoFar = new Dictionary<string, float>();

        string startKey = CellKey(start);
        frontier.Enqueue(start, 0);
        costSoFar[startKey] = 0;
        reachable[start] = 0;

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            string currentKey = CellKey(current);
            float currentCost = costSoFar.GetValueOrDefault(currentKey, 0);

            foreach (var neighbor in _grid.GetNeighbors(current))
            {
                // Skip if there's a unit (unless ignoring)
                if (!options.IgnoreUnits && _unitManager != null)
                {
                    if (_unitManager.GetUnitAt(neighbor.Q, neighbor.R) != null)
                    {
                        continue;
                    }
                }

                // Calculate movement cost
                float moveCost = strategy.GetMovementCost(current, neighbor);

                // Skip impassable
                if (float.IsPositiveInfinity(moveCost))
                {
                    continue;
                }

                float newCost = currentCost + moveCost;

                // Skip if exceeds movement budget
                if (newCost > movementPoints)
                {
                    continue;
                }

                string neighborKey = CellKey(neighbor);

                if (!costSoFar.TryGetValue(neighborKey, out var existingCost) || newCost < existingCost)
                {
                    costSoFar[neighborKey] = newCost;
                    frontier.Enqueue(neighbor, newCost);
                    reachable[neighbor] = newCost;
                }
            }
        }

        return reachable;
    }

    /// <summary>
    /// Checks if a path exists between two cells.
    /// </summary>
    public bool HasPath(HexCell start, HexCell end, bool ignoreUnits = false)
    {
        var result = FindPath(start, end, new PathOptions { IgnoreUnits = ignoreUnits });
        return result.Reachable;
    }

    /// <summary>
    /// Gets the movement cost between two adjacent cells.
    /// </summary>
    public float GetStepCost(HexCell from, HexCell to, UnitType? unitType = null)
    {
        var fromCoords = from.Coordinates;
        var toCoords = to.Coordinates;

        if (fromCoords.DistanceTo(toCoords) != 1)
        {
            return float.PositiveInfinity; // Not adjacent
        }

        return unitType.HasValue
            ? MovementCosts.GetMovementCostForUnit(from, to, unitType.Value)
            : MovementCosts.GetMovementCost(from, to);
    }

    private static float Heuristic(HexCell a, HexCell b)
    {
        return a.Coordinates.DistanceTo(b.Coordinates);
    }

    private static string CellKey(HexCell cell) => $"{cell.Q},{cell.R}";

    private List<HexCell> ReconstructPath(Dictionary<string, HexCell?> cameFrom, HexCell start, HexCell end)
    {
        var path = new List<HexCell>();
        HexCell? current = end;

        while (current != null)
        {
            path.Insert(0, current);
            string key = CellKey(current);
            current = cameFrom.GetValueOrDefault(key);
        }

        return path;
    }
}

/// <summary>
/// Options for pathfinding.
/// </summary>
public class PathOptions
{
    /// <summary>
    /// Whether to ignore units when pathfinding.
    /// </summary>
    public bool IgnoreUnits { get; set; }

    /// <summary>
    /// Maximum cost for the path.
    /// </summary>
    public float MaxCost { get; set; } = float.PositiveInfinity;

    /// <summary>
    /// Unit type for domain-aware pathfinding.
    /// </summary>
    public UnitType? UnitType { get; set; }
}

/// <summary>
/// Result of a pathfinding operation.
/// </summary>
public readonly struct PathResult
{
    /// <summary>
    /// The path as a list of cells.
    /// </summary>
    public IReadOnlyList<HexCell> Path { get; }

    /// <summary>
    /// Total cost of the path.
    /// </summary>
    public float Cost { get; }

    /// <summary>
    /// Whether the destination is reachable.
    /// </summary>
    public bool Reachable { get; }

    /// <summary>
    /// Result indicating no path was found.
    /// </summary>
    public static readonly PathResult NotReachable = new(Array.Empty<HexCell>(), float.PositiveInfinity, false);

    public PathResult(IReadOnlyList<HexCell> path, float cost, bool reachable)
    {
        Path = path;
        Cost = cost;
        Reachable = reachable;
    }
}
