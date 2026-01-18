using HexGame.Utilities;

namespace HexGame.Units;

/// <summary>
/// Interface for unit management operations.
/// Provides query and command methods for unit lifecycle management.
/// </summary>
public interface IUnitManager : IService
{
    #region Query Methods

    /// <summary>
    /// Gets a unit by its ID.
    /// </summary>
    /// <param name="id">The unit ID.</param>
    /// <returns>The unit, or null if not found.</returns>
    Unit? GetUnit(int id);

    /// <summary>
    /// Gets the unit at a specific hex position.
    /// </summary>
    /// <param name="q">Q coordinate.</param>
    /// <param name="r">R coordinate.</param>
    /// <returns>The unit at that position, or null if none.</returns>
    Unit? GetUnitAt(int q, int r);

    /// <summary>
    /// Gets all units.
    /// </summary>
    /// <returns>List of all units.</returns>
    IReadOnlyList<Unit> GetAllUnits();

    /// <summary>
    /// Gets all units belonging to a player.
    /// </summary>
    /// <param name="playerId">The player ID.</param>
    /// <returns>List of units owned by that player.</returns>
    IReadOnlyList<Unit> GetPlayerUnits(int playerId);

    /// <summary>
    /// Gets units within a world-coordinate radius.
    /// </summary>
    /// <param name="worldX">Center X coordinate.</param>
    /// <param name="worldZ">Center Z coordinate.</param>
    /// <param name="radius">Search radius.</param>
    /// <returns>List of units within range.</returns>
    IReadOnlyList<Unit> GetUnitsInRadius(float worldX, float worldZ, float radius);

    /// <summary>
    /// Gets units within a world-coordinate rectangle.
    /// </summary>
    IReadOnlyList<Unit> GetUnitsInRect(float minX, float minZ, float maxX, float maxZ);

    /// <summary>
    /// Gets the total number of units.
    /// </summary>
    int UnitCount { get; }

    /// <summary>
    /// Gets counts by domain (land vs naval).
    /// </summary>
    /// <returns>Dictionary with "land" and "naval" counts.</returns>
    (int Land, int Naval) GetUnitCounts();

    #endregion

    #region Command Methods

    /// <summary>
    /// Creates a new unit at the specified position.
    /// </summary>
    /// <param name="type">Type of unit to create.</param>
    /// <param name="q">Q coordinate.</param>
    /// <param name="r">R coordinate.</param>
    /// <param name="playerId">Owner player ID.</param>
    /// <returns>The created unit, or null if creation failed.</returns>
    Unit? CreateUnit(UnitType type, int q, int r, int playerId);

    /// <summary>
    /// Moves a unit to a new position.
    /// </summary>
    /// <param name="unitId">ID of unit to move.</param>
    /// <param name="toQ">Destination Q coordinate.</param>
    /// <param name="toR">Destination R coordinate.</param>
    /// <param name="movementCost">Movement cost (-1 to skip cost check).</param>
    /// <returns>True if move succeeded.</returns>
    bool MoveUnit(int unitId, int toQ, int toR, int movementCost = -1);

    /// <summary>
    /// Removes a unit from the game.
    /// </summary>
    /// <param name="unitId">ID of unit to remove.</param>
    /// <returns>True if unit was found and removed.</returns>
    bool RemoveUnit(int unitId);

    /// <summary>
    /// Clears all units.
    /// </summary>
    void Clear();

    /// <summary>
    /// Resets movement for all units (start of turn).
    /// </summary>
    void ResetAllMovement();

    /// <summary>
    /// Resets movement for units of a specific player.
    /// </summary>
    /// <param name="playerId">The player ID.</param>
    void ResetPlayerMovement(int playerId);

    #endregion

    #region Spawning (for testing/setup)

    /// <summary>
    /// Spawns random land units for testing.
    /// </summary>
    /// <param name="count">Number to spawn.</param>
    /// <param name="playerId">Owner player ID.</param>
    /// <returns>Number actually spawned.</returns>
    int SpawnRandomUnits(int count, int playerId = 1);

    /// <summary>
    /// Spawns random naval units for testing.
    /// </summary>
    /// <param name="count">Number to spawn.</param>
    /// <param name="playerId">Owner player ID.</param>
    /// <returns>Number actually spawned.</returns>
    int SpawnRandomNavalUnits(int count, int playerId = 1);

    /// <summary>
    /// Spawns a mix of land and naval units.
    /// </summary>
    /// <returns>Tuple of (land spawned, naval spawned).</returns>
    (int Land, int Naval) SpawnMixedUnits(int landCount, int navalCount, int playerId = 1);

    #endregion

    #region Pool/Stats

    /// <summary>
    /// Sets up the object pool. Call after construction.
    /// </summary>
    void SetupPool();

    /// <summary>
    /// Pre-warms the unit pool with objects.
    /// </summary>
    /// <param name="count">Number to prewarm.</param>
    void PrewarmPool(int count);

    /// <summary>
    /// Gets object pool statistics.
    /// </summary>
    PoolStats GetPoolStats();

    /// <summary>
    /// Gets spatial hash statistics.
    /// </summary>
    SpatialHashStats GetSpatialStats();

    #endregion
}
