using HexGame.Utilities;

namespace HexGame.Units;

/// <summary>
/// Manages all units in the game.
/// Uses object pooling for memory efficiency and spatial hashing for fast lookups.
/// </summary>
public class UnitManager : IUnitManager
{
    private readonly HexGrid _grid;
    private readonly Dictionary<int, Unit> _units = new();
    private readonly Dictionary<string, Unit> _hexPositions = new(); // "q,r" -> Unit
    private SpatialHash<Unit> _spatialHash = new(2.0f);
    private ObjectPool<Unit>? _unitPool;
    private int _nextId = 1;
    private readonly object _lock = new();
    private readonly Random _random = new();

    /// <summary>
    /// Fired when a unit is created.
    /// </summary>
    public event Action<Unit>? UnitCreated;

    /// <summary>
    /// Fired when a unit is removed.
    /// </summary>
    public event Action<int>? UnitRemoved;

    /// <summary>
    /// Fired when a unit moves.
    /// </summary>
    public event Action<Unit, int, int>? UnitMoved; // unit, fromQ, fromR

    /// <summary>
    /// Creates a new UnitManager.
    /// </summary>
    /// <param name="grid">The hex grid to use for validation.</param>
    public UnitManager(HexGrid grid)
    {
        _grid = grid;
    }

    #region IService Implementation

    public void Initialize()
    {
        // Already initialized in constructor
    }

    public void Shutdown()
    {
        Clear();
        _unitPool?.Clear();
    }

    #endregion

    #region Pool Setup

    public void SetupPool()
    {
        _unitPool = new ObjectPool<Unit>(maxSize: 500);
    }

    public void PrewarmPool(int count)
    {
        _unitPool?.Prewarm(count);
    }

    public PoolStats GetPoolStats()
    {
        return _unitPool?.GetStats() ?? new PoolStats(0, 0, 0, 0, 0, 0);
    }

    public SpatialHashStats GetSpatialStats()
    {
        lock (_lock)
        {
            return _spatialHash.GetStats();
        }
    }

    #endregion

    #region Query Methods

    public Unit? GetUnit(int id)
    {
        lock (_lock)
        {
            return _units.TryGetValue(id, out var unit) ? unit : null;
        }
    }

    public Unit? GetUnitAt(int q, int r)
    {
        lock (_lock)
        {
            string key = $"{q},{r}";
            return _hexPositions.TryGetValue(key, out var unit) ? unit : null;
        }
    }

    public IReadOnlyList<Unit> GetAllUnits()
    {
        lock (_lock)
        {
            return _units.Values.ToList();
        }
    }

    public IReadOnlyList<Unit> GetPlayerUnits(int playerId)
    {
        lock (_lock)
        {
            return _units.Values.Where(u => u.PlayerId == playerId).ToList();
        }
    }

    public IReadOnlyList<Unit> GetUnitsInRadius(float worldX, float worldZ, float radius)
    {
        lock (_lock)
        {
            return _spatialHash.QueryRadius(worldX, worldZ, radius);
        }
    }

    public IReadOnlyList<Unit> GetUnitsInRect(float minX, float minZ, float maxX, float maxZ)
    {
        lock (_lock)
        {
            return _spatialHash.QueryRect(minX, minZ, maxX, maxZ);
        }
    }

    public int UnitCount
    {
        get
        {
            lock (_lock)
            {
                return _units.Count;
            }
        }
    }

    public (int Land, int Naval) GetUnitCounts()
    {
        lock (_lock)
        {
            int land = 0, naval = 0;
            foreach (var unit in _units.Values)
            {
                if (unit.Type.GetDomain() == UnitDomain.Naval)
                {
                    naval++;
                }
                else
                {
                    land++;
                }
            }
            return (land, naval);
        }
    }

    #endregion

    #region Command Methods

    public Unit? CreateUnit(UnitType type, int q, int r, int playerId)
    {
        lock (_lock)
        {
            // Check if hex is valid
            var cell = _grid.GetCell(q, r);
            if (cell == null)
            {
                return null;
            }

            // Check domain compatibility
            bool isWater = HexMetrics.IsWaterElevation(cell.Elevation);
            if (isWater && !type.CanTraverseWater())
            {
                return null; // Land unit can't be on water
            }
            if (!isWater && !type.CanTraverseLand())
            {
                return null; // Naval unit can't be on land
            }

            // Check if hex is occupied
            if (GetUnitAt(q, r) != null)
            {
                return null;
            }

            // Acquire unit from pool or create new
            Unit unit;
            if (_unitPool != null)
            {
                unit = _unitPool.Acquire();
            }
            else
            {
                unit = new Unit();
            }

            unit.Id = _nextId++;
            unit.InitWith(type, q, r, playerId);

            // Add to tracking
            _units[unit.Id] = unit;
            _hexPositions[$"{q},{r}"] = unit;

            // Add to spatial hash
            var worldPos = new HexCoordinates(q, r).ToWorldPosition(cell.Elevation);
            _spatialHash.Insert(unit, worldPos.X, worldPos.Z);

            // Fire event
            UnitCreated?.Invoke(unit);

            // Publish to EventBus if available
            if (GameContext.TryGetEvents(out var eventBus))
            {
                eventBus.Publish(new UnitCreatedEvent(unit.Id, (int)type, q, r, playerId));
            }

            return unit;
        }
    }

    public bool MoveUnit(int unitId, int toQ, int toR, int movementCost = -1)
    {
        lock (_lock)
        {
            if (!_units.TryGetValue(unitId, out var unit))
            {
                return false;
            }

            // Check destination is valid
            var cell = _grid.GetCell(toQ, toR);
            if (cell == null)
            {
                return false;
            }

            // Check domain compatibility
            bool isWater = HexMetrics.IsWaterElevation(cell.Elevation);
            if (isWater && !unit.CanTraverseWater)
            {
                return false;
            }
            if (!isWater && !unit.CanTraverseLand)
            {
                return false;
            }

            // Check not occupied
            if (GetUnitAt(toQ, toR) != null)
            {
                return false;
            }

            // Check and spend movement cost
            if (movementCost >= 0)
            {
                if (unit.Movement < movementCost)
                {
                    return false;
                }
                unit.SpendMovement(movementCost);
            }

            // Store old position
            int fromQ = unit.Q;
            int fromR = unit.R;

            // Update spatial indexes
            _hexPositions.Remove($"{fromQ},{fromR}");
            _hexPositions[$"{toQ},{toR}"] = unit;

            // Update position
            unit.Q = toQ;
            unit.R = toR;

            // Update spatial hash
            var worldPos = new HexCoordinates(toQ, toR).ToWorldPosition(cell.Elevation);
            _spatialHash.Update(unit, worldPos.X, worldPos.Z);

            // Fire event
            UnitMoved?.Invoke(unit, fromQ, fromR);

            // Publish to EventBus if available
            if (GameContext.TryGetEvents(out var eventBus))
            {
                eventBus.Publish(new UnitMovedEvent(unit.Id, fromQ, fromR, toQ, toR, movementCost));
            }

            return true;
        }
    }

    public bool RemoveUnit(int unitId)
    {
        lock (_lock)
        {
            if (!_units.TryGetValue(unitId, out var unit))
            {
                return false;
            }

            // Remove from spatial indexes
            _hexPositions.Remove($"{unit.Q},{unit.R}");
            _spatialHash.Remove(unit);

            _units.Remove(unitId);

            // Return to pool
            _unitPool?.Release(unit);

            // Fire event
            UnitRemoved?.Invoke(unitId);

            // Publish to EventBus if available
            if (GameContext.TryGetEvents(out var eventBus))
            {
                eventBus.Publish(new UnitRemovedEvent(unitId));
            }

            return true;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            // Return all units to pool
            if (_unitPool != null)
            {
                foreach (var unit in _units.Values)
                {
                    _unitPool.Release(unit);
                }
            }

            _units.Clear();
            _hexPositions.Clear();
            _spatialHash.Clear();
            _nextId = 1;
        }
    }

    public void ResetAllMovement()
    {
        lock (_lock)
        {
            foreach (var unit in _units.Values)
            {
                unit.ResetMovement();
            }
        }
    }

    public void ResetPlayerMovement(int playerId)
    {
        lock (_lock)
        {
            foreach (var unit in _units.Values)
            {
                if (unit.PlayerId == playerId)
                {
                    unit.ResetMovement();
                }
            }
        }
    }

    #endregion

    #region Spawning

    public int SpawnRandomUnits(int count, int playerId = 1)
    {
        var landCells = new List<HexCell>();
        foreach (var cell in _grid.GetAllCells())
        {
            if (HexMetrics.IsLandElevation(cell.Elevation))
            {
                landCells.Add(cell);
            }
        }

        int spawned = 0;
        var landTypes = UnitTypeExtensions.GetLandTypes();

        for (int i = 0; i < count && landCells.Count > 0; i++)
        {
            int idx = _random.Next(landCells.Count);
            var cell = landCells[idx];
            var type = landTypes[_random.Next(landTypes.Length)];

            if (CreateUnit(type, cell.Q, cell.R, playerId) != null)
            {
                spawned++;
                landCells.RemoveAt(idx);
            }
        }

        return spawned;
    }

    public int SpawnRandomNavalUnits(int count, int playerId = 1)
    {
        var waterCells = new List<HexCell>();
        foreach (var cell in _grid.GetAllCells())
        {
            if (HexMetrics.IsWaterElevation(cell.Elevation))
            {
                waterCells.Add(cell);
            }
        }

        int spawned = 0;
        var navalTypes = UnitTypeExtensions.GetNavalTypes();

        for (int i = 0; i < count && waterCells.Count > 0; i++)
        {
            int idx = _random.Next(waterCells.Count);
            var cell = waterCells[idx];
            var type = navalTypes[_random.Next(navalTypes.Length)];

            if (CreateUnit(type, cell.Q, cell.R, playerId) != null)
            {
                spawned++;
                waterCells.RemoveAt(idx);
            }
        }

        return spawned;
    }

    public (int Land, int Naval) SpawnMixedUnits(int landCount, int navalCount, int playerId = 1)
    {
        return (
            SpawnRandomUnits(landCount, playerId),
            SpawnRandomNavalUnits(navalCount, playerId)
        );
    }

    #endregion
}
