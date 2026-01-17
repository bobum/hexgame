using HexGame.Core;
using HexGame.GameState;
using HexGame.Pathfinding;
using HexGame.Units;

namespace HexGame.AI;

/// <summary>
/// Read-only snapshot of game state for AI evaluation.
/// Provides safe access to game data without allowing direct mutations.
/// </summary>
public class AIContext
{
    private readonly HexGrid _grid;
    private readonly IUnitManager _unitManager;
    private readonly IPathfinder _pathfinder;
    private readonly TurnManager _turnManager;

    /// <summary>
    /// The AI player's ID.
    /// </summary>
    public int PlayerId { get; }

    /// <summary>
    /// Current turn number.
    /// </summary>
    public int CurrentTurn => _turnManager.CurrentTurn;

    /// <summary>
    /// Current turn phase.
    /// </summary>
    public TurnPhase CurrentPhase => _turnManager.CurrentPhase;

    /// <summary>
    /// Grid dimensions.
    /// </summary>
    public (int Width, int Height) GridSize => (_grid.Width, _grid.Height);

    /// <summary>
    /// Creates a new AI context.
    /// </summary>
    public AIContext(
        int playerId,
        HexGrid grid,
        IUnitManager unitManager,
        IPathfinder pathfinder,
        TurnManager turnManager)
    {
        PlayerId = playerId;
        _grid = grid;
        _unitManager = unitManager;
        _pathfinder = pathfinder;
        _turnManager = turnManager;
    }

    #region Unit Queries

    /// <summary>
    /// Gets all units owned by this AI player.
    /// </summary>
    public IReadOnlyList<UnitSnapshot> GetMyUnits()
    {
        return _unitManager.GetPlayerUnits(PlayerId)
            .Select(u => new UnitSnapshot(u))
            .ToList();
    }

    /// <summary>
    /// Gets all enemy units (not owned by this AI).
    /// </summary>
    public IReadOnlyList<UnitSnapshot> GetEnemyUnits()
    {
        return _unitManager.GetAllUnits()
            .Where(u => u.PlayerId != PlayerId)
            .Select(u => new UnitSnapshot(u))
            .ToList();
    }

    /// <summary>
    /// Gets all units on the map.
    /// </summary>
    public IReadOnlyList<UnitSnapshot> GetAllUnits()
    {
        return _unitManager.GetAllUnits()
            .Select(u => new UnitSnapshot(u))
            .ToList();
    }

    /// <summary>
    /// Gets units that can still move this turn.
    /// </summary>
    public IReadOnlyList<UnitSnapshot> GetMovableUnits()
    {
        return _unitManager.GetPlayerUnits(PlayerId)
            .Where(u => u.Movement > 0)
            .Select(u => new UnitSnapshot(u))
            .ToList();
    }

    /// <summary>
    /// Gets units that haven't acted this turn.
    /// </summary>
    public IReadOnlyList<UnitSnapshot> GetReadyUnits()
    {
        return _unitManager.GetPlayerUnits(PlayerId)
            .Where(u => !u.HasActedThisTurn)
            .Select(u => new UnitSnapshot(u))
            .ToList();
    }

    /// <summary>
    /// Gets the unit at a specific position.
    /// </summary>
    public UnitSnapshot? GetUnitAt(int q, int r)
    {
        var unit = _unitManager.GetUnitAt(q, r);
        return unit != null ? new UnitSnapshot(unit) : null;
    }

    #endregion

    #region Map Queries

    /// <summary>
    /// Gets the cell at specified coordinates.
    /// </summary>
    public CellSnapshot? GetCell(int q, int r)
    {
        var cell = _grid.GetCell(q, r);
        return cell != null ? new CellSnapshot(cell, _unitManager.GetUnitAt(q, r)) : null;
    }

    /// <summary>
    /// Gets cells within a radius of a position.
    /// </summary>
    public IReadOnlyList<CellSnapshot> GetCellsInRadius(int centerQ, int centerR, int radius)
    {
        var center = _grid.GetCell(centerQ, centerR);
        if (center == null) return Array.Empty<CellSnapshot>();

        return _grid.GetCellsInRadius(center, radius)
            .Select(c => new CellSnapshot(c, _unitManager.GetUnitAt(c.Q, c.R)))
            .ToList();
    }

    /// <summary>
    /// Gets all cells matching a predicate.
    /// </summary>
    public IReadOnlyList<CellSnapshot> FindCells(Func<CellSnapshot, bool> predicate)
    {
        return _grid.GetAllCells()
            .Select(c => new CellSnapshot(c, _unitManager.GetUnitAt(c.Q, c.R)))
            .Where(predicate)
            .ToList();
    }

    #endregion

    #region Pathfinding Queries

    /// <summary>
    /// Gets all cells reachable by a unit.
    /// </summary>
    public IReadOnlyList<CellSnapshot> GetReachableCells(UnitSnapshot unit)
    {
        var startCell = _grid.GetCell(unit.Q, unit.R);
        if (startCell == null) return Array.Empty<CellSnapshot>();

        var reachable = _pathfinder.GetReachableCells(
            startCell,
            unit.Movement,
            new PathOptions { UnitType = unit.Type }
        );

        return reachable.Keys
            .Select(c => new CellSnapshot(c, _unitManager.GetUnitAt(c.Q, c.R)))
            .ToList();
    }

    /// <summary>
    /// Finds a path between two positions.
    /// </summary>
    public PathSnapshot? FindPath(int fromQ, int fromR, int toQ, int toR, UnitType? unitType = null)
    {
        var start = _grid.GetCell(fromQ, fromR);
        var end = _grid.GetCell(toQ, toR);
        if (start == null || end == null) return null;

        var result = _pathfinder.FindPath(start, end, new PathOptions { UnitType = unitType });
        return result.Reachable ? new PathSnapshot(result) : null;
    }

    /// <summary>
    /// Calculates distance between two positions.
    /// </summary>
    public int GetDistance(int q1, int r1, int q2, int r2)
    {
        var coords1 = new HexCoordinates(q1, r1);
        var coords2 = new HexCoordinates(q2, r2);
        return coords1.DistanceTo(coords2);
    }

    /// <summary>
    /// Checks if a path exists between two positions.
    /// </summary>
    public bool HasPath(int fromQ, int fromR, int toQ, int toR)
    {
        var start = _grid.GetCell(fromQ, fromR);
        var end = _grid.GetCell(toQ, toR);
        if (start == null || end == null) return false;

        return _pathfinder.HasPath(start, end);
    }

    #endregion

    #region Strategic Queries

    /// <summary>
    /// Finds the nearest enemy unit to a position.
    /// </summary>
    public (UnitSnapshot? Unit, int Distance) FindNearestEnemy(int q, int r)
    {
        var enemies = GetEnemyUnits();
        if (enemies.Count == 0) return (null, int.MaxValue);

        UnitSnapshot? nearest = null;
        int minDistance = int.MaxValue;

        foreach (var enemy in enemies)
        {
            int dist = GetDistance(q, r, enemy.Q, enemy.R);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = enemy;
            }
        }

        return (nearest, minDistance);
    }

    /// <summary>
    /// Counts units within attack range of a position.
    /// </summary>
    public int CountThreats(int q, int r, int range = 1)
    {
        return GetEnemyUnits()
            .Count(e => GetDistance(q, r, e.Q, e.R) <= range);
    }

    /// <summary>
    /// Evaluates a position's strategic value.
    /// Higher values indicate better positions.
    /// </summary>
    public float EvaluatePosition(int q, int r)
    {
        var cell = GetCell(q, r);
        if (cell == null) return float.MinValue;

        float value = 0;

        // Terrain value
        value += cell.TerrainType switch
        {
            TerrainType.Mountains => 3f,  // Defensive bonus
            TerrainType.Hills => 2f,
            TerrainType.Forest => 1.5f,
            TerrainType.Plains => 1f,
            TerrainType.Grassland => 1f,
            TerrainType.Ocean => -10f,    // Can't stand on ocean (usually)
            TerrainType.Coast => 0.5f,
            _ => 0f
        };

        // Elevation value
        value += cell.Elevation * 0.5f;

        // Threat penalty
        value -= CountThreats(q, r) * 2f;

        return value;
    }

    #endregion
}

#region Snapshot Types

/// <summary>
/// Immutable snapshot of a unit's state.
/// </summary>
public readonly struct UnitSnapshot
{
    public int Id { get; }
    public UnitType Type { get; }
    public int PlayerId { get; }
    public int Q { get; }
    public int R { get; }
    public float Health { get; }
    public float MaxHealth { get; }
    public float Movement { get; }
    public float MaxMovement { get; }
    public bool HasActed { get; }
    public float HealthPercent => MaxHealth > 0 ? Health / MaxHealth : 0;
    public bool IsAlive => Health > 0;

    public UnitSnapshot(Unit unit)
    {
        Id = unit.Id;
        Type = unit.Type;
        PlayerId = unit.PlayerId;
        Q = unit.Q;
        R = unit.R;
        Health = unit.Health;
        MaxHealth = unit.MaxHealth;
        Movement = unit.Movement;
        MaxMovement = unit.MaxMovement;
        HasActed = unit.HasActedThisTurn;
    }
}

/// <summary>
/// Immutable snapshot of a cell's state.
/// </summary>
public readonly struct CellSnapshot
{
    public int Q { get; }
    public int R { get; }
    public float Elevation { get; }
    public TerrainType TerrainType { get; }
    public bool HasRiver { get; }
    public bool IsUnderwater { get; }
    public int? OccupyingUnitId { get; }
    public bool IsOccupied => OccupyingUnitId.HasValue;

    public CellSnapshot(HexCell cell, Unit? occupant)
    {
        Q = cell.Q;
        R = cell.R;
        Elevation = cell.Elevation;
        TerrainType = cell.TerrainType;
        HasRiver = cell.HasRiver;
        IsUnderwater = cell.IsUnderwater;
        OccupyingUnitId = occupant?.Id;
    }
}

/// <summary>
/// Immutable snapshot of a pathfinding result.
/// </summary>
public readonly struct PathSnapshot
{
    public IReadOnlyList<(int Q, int R)> Steps { get; }
    public float TotalCost { get; }
    public int Length => Steps.Count;

    public PathSnapshot(PathResult result)
    {
        Steps = result.Path.Select(c => (c.Q, c.R)).ToList();
        TotalCost = result.Cost;
    }
}

#endregion
