using HexGame.Utilities;

namespace HexGame.Units;

/// <summary>
/// Runtime unit instance data.
/// Implements IPoolable for efficient object pooling.
/// </summary>
public class Unit : IPoolable
{
    #region Properties

    /// <summary>
    /// Unique identifier for this unit.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The type of unit.
    /// </summary>
    public UnitType Type { get; set; }

    /// <summary>
    /// Hex Q coordinate.
    /// </summary>
    public int Q { get; set; }

    /// <summary>
    /// Hex R coordinate.
    /// </summary>
    public int R { get; set; }

    /// <summary>
    /// Current health.
    /// </summary>
    public int Health { get; set; } = 100;

    /// <summary>
    /// Maximum health.
    /// </summary>
    public int MaxHealth { get; set; } = 100;

    /// <summary>
    /// Current movement points this turn.
    /// </summary>
    public int Movement { get; set; } = 2;

    /// <summary>
    /// Maximum movement points per turn.
    /// </summary>
    public int MaxMovement { get; set; } = 2;

    /// <summary>
    /// Attack strength.
    /// </summary>
    public int Attack { get; set; } = 10;

    /// <summary>
    /// Defense strength.
    /// </summary>
    public int Defense { get; set; } = 8;

    /// <summary>
    /// Owner player ID. 0 = neutral, 1 = player, 2+ = AI.
    /// </summary>
    public int PlayerId { get; set; }

    /// <summary>
    /// Whether this unit has moved this turn.
    /// </summary>
    public bool HasMoved { get; set; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the hex coordinates as a struct.
    /// </summary>
    public HexCoordinates Coordinates => new(Q, R);

    /// <summary>
    /// Gets the world position of this unit.
    /// </summary>
    /// <param name="elevation">Ground elevation at unit position.</param>
    public Vector3 GetWorldPosition(int elevation = 0)
    {
        return Coordinates.ToWorldPosition(elevation);
    }

    /// <summary>
    /// Gets the display name of this unit type.
    /// </summary>
    public string TypeName => Type.GetDisplayName();

    /// <summary>
    /// Gets the domain of this unit.
    /// </summary>
    public UnitDomain Domain => Type.GetDomain();

    /// <summary>
    /// Whether this unit can traverse land.
    /// </summary>
    public bool CanTraverseLand => Type.CanTraverseLand();

    /// <summary>
    /// Whether this unit can traverse water.
    /// </summary>
    public bool CanTraverseWater => Type.CanTraverseWater();

    /// <summary>
    /// Whether this unit can still move this turn.
    /// </summary>
    public bool CanMove => Movement > 0;

    #endregion

    #region Methods

    /// <summary>
    /// Resets movement for a new turn.
    /// </summary>
    public void ResetMovement()
    {
        Movement = MaxMovement;
        HasMoved = false;
    }

    /// <summary>
    /// Spends movement points.
    /// </summary>
    /// <param name="cost">Movement cost.</param>
    /// <returns>True if movement was spent successfully.</returns>
    public bool SpendMovement(int cost)
    {
        if (Movement < cost)
        {
            return false;
        }
        Movement -= cost;
        HasMoved = true;
        return true;
    }

    /// <summary>
    /// Initializes unit with new values (used when acquiring from pool).
    /// </summary>
    public void InitWith(UnitType type, int q, int r, int playerId)
    {
        Type = type;
        Q = q;
        R = r;
        PlayerId = playerId;
        HasMoved = false;
        ApplyStats();
    }

    /// <summary>
    /// Applies base stats from unit type.
    /// </summary>
    private void ApplyStats()
    {
        var stats = Type.GetStats();
        Health = stats.Health;
        MaxHealth = stats.Health;
        Movement = stats.Movement;
        MaxMovement = stats.Movement;
        Attack = stats.Attack;
        Defense = stats.Defense;
    }

    #endregion

    #region IPoolable Implementation

    /// <summary>
    /// Called when acquired from pool.
    /// </summary>
    public void OnGetFromPool()
    {
        // Ready for use
    }

    /// <summary>
    /// Called when returned to pool.
    /// </summary>
    public void OnReturnToPool()
    {
        // Cleanup if needed
    }

    /// <summary>
    /// Resets unit state for reuse from object pool.
    /// </summary>
    public void Reset()
    {
        Id = 0;
        Type = UnitType.Infantry;
        Q = 0;
        R = 0;
        Health = 100;
        MaxHealth = 100;
        Movement = 2;
        MaxMovement = 2;
        Attack = 10;
        Defense = 8;
        PlayerId = 0;
        HasMoved = false;
    }

    #endregion

    public override string ToString()
    {
        return $"Unit[{Id}] {TypeName} at ({Q},{R}) HP:{Health}/{MaxHealth} MP:{Movement}/{MaxMovement}";
    }
}
