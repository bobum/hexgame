using HexGame.Units;

namespace HexGame.AI;

/// <summary>
/// Base class for AI actions.
/// Each action type represents a decision the AI can make.
/// </summary>
public abstract class AIAction
{
    /// <summary>
    /// The unit performing the action (if applicable).
    /// </summary>
    public int? UnitId { get; protected set; }

    /// <summary>
    /// Priority of this action (higher = more important).
    /// Used for action ordering.
    /// </summary>
    public float Priority { get; set; }

    /// <summary>
    /// Gets a description of this action for debugging.
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// Validates whether this action can be executed.
    /// </summary>
    /// <param name="context">Current game state.</param>
    /// <returns>True if the action is valid.</returns>
    public abstract bool IsValid(AIContext context);
}

/// <summary>
/// Action to move a unit to a new position.
/// </summary>
public class MoveAction : AIAction
{
    /// <summary>
    /// Target Q coordinate.
    /// </summary>
    public int TargetQ { get; }

    /// <summary>
    /// Target R coordinate.
    /// </summary>
    public int TargetR { get; }

    public override string Description =>
        $"Move unit {UnitId} to ({TargetQ}, {TargetR})";

    public MoveAction(int unitId, int targetQ, int targetR, float priority = 0)
    {
        UnitId = unitId;
        TargetQ = targetQ;
        TargetR = targetR;
        Priority = priority;
    }

    public override bool IsValid(AIContext context)
    {
        if (!UnitId.HasValue) return false;

        var unit = context.GetAllUnits().FirstOrDefault(u => u.Id == UnitId.Value);
        if (unit.Id == 0 || unit.Movement <= 0) return false;

        var path = context.FindPath(unit.Q, unit.R, TargetQ, TargetR, unit.Type);
        return path != null && path.Value.TotalCost <= unit.Movement;
    }
}

/// <summary>
/// Action to attack an enemy unit.
/// </summary>
public class AttackAction : AIAction
{
    /// <summary>
    /// The target unit to attack.
    /// </summary>
    public int TargetUnitId { get; }

    public override string Description =>
        $"Unit {UnitId} attacks unit {TargetUnitId}";

    public AttackAction(int unitId, int targetUnitId, float priority = 0)
    {
        UnitId = unitId;
        TargetUnitId = targetUnitId;
        Priority = priority;
    }

    public override bool IsValid(AIContext context)
    {
        if (!UnitId.HasValue) return false;

        var attacker = context.GetAllUnits().FirstOrDefault(u => u.Id == UnitId.Value);
        var target = context.GetAllUnits().FirstOrDefault(u => u.Id == TargetUnitId);

        if (attacker.Id == 0 || target.Id == 0) return false;
        if (attacker.HasActed) return false;
        if (attacker.PlayerId == target.PlayerId) return false;

        // Check if in attack range (adjacent)
        int distance = context.GetDistance(attacker.Q, attacker.R, target.Q, target.R);
        return distance <= 1;
    }
}

/// <summary>
/// Action to skip a unit's remaining actions this turn.
/// </summary>
public class SkipAction : AIAction
{
    public override string Description =>
        UnitId.HasValue ? $"Skip unit {UnitId}'s turn" : "Skip turn";

    public SkipAction(int? unitId = null)
    {
        UnitId = unitId;
        Priority = -100; // Low priority
    }

    public override bool IsValid(AIContext context) => true;
}

/// <summary>
/// Action to end the AI's turn.
/// </summary>
public class EndTurnAction : AIAction
{
    public override string Description => "End turn";

    public EndTurnAction()
    {
        Priority = -1000; // Lowest priority
    }

    public override bool IsValid(AIContext context) => true;
}

/// <summary>
/// Composite action that moves then attacks.
/// </summary>
public class MoveAndAttackAction : AIAction
{
    /// <summary>
    /// Intermediate position to move to.
    /// </summary>
    public int MoveQ { get; }

    /// <summary>
    /// Intermediate position to move to.
    /// </summary>
    public int MoveR { get; }

    /// <summary>
    /// Target unit to attack after moving.
    /// </summary>
    public int TargetUnitId { get; }

    public override string Description =>
        $"Unit {UnitId} moves to ({MoveQ}, {MoveR}) then attacks unit {TargetUnitId}";

    public MoveAndAttackAction(int unitId, int moveQ, int moveR, int targetUnitId, float priority = 0)
    {
        UnitId = unitId;
        MoveQ = moveQ;
        MoveR = moveR;
        TargetUnitId = targetUnitId;
        Priority = priority;
    }

    public override bool IsValid(AIContext context)
    {
        if (!UnitId.HasValue) return false;

        var unit = context.GetAllUnits().FirstOrDefault(u => u.Id == UnitId.Value);
        var target = context.GetAllUnits().FirstOrDefault(u => u.Id == TargetUnitId);

        if (unit.Id == 0 || target.Id == 0) return false;
        if (unit.HasActed) return false;

        // Check if we can reach the move position
        var path = context.FindPath(unit.Q, unit.R, MoveQ, MoveR, unit.Type);
        if (path == null || path.Value.TotalCost > unit.Movement) return false;

        // Check if target is adjacent to move position
        int attackDistance = context.GetDistance(MoveQ, MoveR, target.Q, target.R);
        return attackDistance <= 1;
    }
}

/// <summary>
/// Action to fortify a unit (skip movement for defensive bonus).
/// </summary>
public class FortifyAction : AIAction
{
    public override string Description =>
        $"Unit {UnitId} fortifies position";

    public FortifyAction(int unitId, float priority = 0)
    {
        UnitId = unitId;
        Priority = priority;
    }

    public override bool IsValid(AIContext context)
    {
        if (!UnitId.HasValue) return false;

        var unit = context.GetAllUnits().FirstOrDefault(u => u.Id == UnitId.Value);
        return unit.Id != 0 && !unit.HasActed;
    }
}

/// <summary>
/// Factory for creating common AI actions.
/// </summary>
public static class AIActionFactory
{
    /// <summary>
    /// Creates a move action toward a target position.
    /// </summary>
    public static MoveAction? CreateMoveToward(AIContext context, UnitSnapshot unit, int targetQ, int targetR)
    {
        var path = context.FindPath(unit.Q, unit.R, targetQ, targetR, unit.Type);
        if (path == null || path.Value.Steps.Count < 2) return null;

        var pathSteps = path.Value.Steps;

        // Find the furthest point we can reach
        var reachable = context.GetReachableCells(unit);
        var reachableSet = reachable.ToDictionary(c => (c.Q, c.R));

        for (int i = 1; i < pathSteps.Count; i++)
        {
            var step = pathSteps[i];
            if (!reachableSet.ContainsKey(step))
            {
                // Can't reach this step, use previous
                if (i > 1)
                {
                    var prev = pathSteps[i - 1];
                    return new MoveAction(unit.Id, prev.Q, prev.R);
                }
                return null;
            }
        }

        // Can reach the destination
        var dest = pathSteps[^1];
        return new MoveAction(unit.Id, dest.Q, dest.R);
    }

    /// <summary>
    /// Creates an attack action if target is in range.
    /// </summary>
    public static AttackAction? CreateAttackIfInRange(AIContext context, UnitSnapshot attacker, UnitSnapshot target)
    {
        int distance = context.GetDistance(attacker.Q, attacker.R, target.Q, target.R);
        if (distance > 1) return null;

        return new AttackAction(attacker.Id, target.Id);
    }

    /// <summary>
    /// Creates a move-and-attack action if possible.
    /// </summary>
    public static MoveAndAttackAction? CreateMoveAndAttack(AIContext context, UnitSnapshot unit, UnitSnapshot target)
    {
        // Find a cell adjacent to target that we can reach
        var reachable = context.GetReachableCells(unit);

        foreach (var cell in reachable)
        {
            int distToTarget = context.GetDistance(cell.Q, cell.R, target.Q, target.R);
            if (distToTarget == 1 && !cell.IsOccupied)
            {
                return new MoveAndAttackAction(unit.Id, cell.Q, cell.R, target.Id);
            }
        }

        return null;
    }
}
