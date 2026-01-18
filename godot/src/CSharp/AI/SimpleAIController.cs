namespace HexGame.AI;

/// <summary>
/// A simple aggressive AI controller that moves units toward enemies and attacks.
/// Serves as a reference implementation for IAIController.
/// </summary>
public class SimpleAIController : IAIController
{
    private int _playerId;
    private readonly Queue<AIAction> _plannedActions = new();

    public string Name => "Simple Aggressive AI";
    public int DifficultyLevel => 1;

    public void OnTurnStart(int playerId, AIContext context)
    {
        _playerId = playerId;
        _plannedActions.Clear();

        // Plan all actions at the start of turn
        PlanActions(context);
    }

    public AIAction? DecideAction(AIContext context)
    {
        // Return queued actions if available
        if (_plannedActions.Count > 0)
        {
            var action = _plannedActions.Dequeue();
            // Validate the action is still valid
            if (action.IsValid(context))
            {
                return action;
            }
            // If invalid, try the next one
            return DecideAction(context);
        }

        // No more planned actions, end turn
        return null;
    }

    public void OnActionComplete(AIAction action, bool success, AIContext context)
    {
        // Could use this for learning or replanning
        if (!success)
        {
            GD.Print($"SimpleAI: Action failed - {action.Description}");
        }
    }

    public void OnTurnEnd(AIContext context)
    {
        _plannedActions.Clear();
    }

    private void PlanActions(AIContext context)
    {
        var myUnits = context.GetReadyUnits();
        var enemies = context.GetEnemyUnits();

        if (enemies.Count == 0)
        {
            // No enemies, just skip
            return;
        }

        foreach (var unit in myUnits)
        {
            var action = PlanUnitAction(context, unit, enemies);
            if (action != null)
            {
                _plannedActions.Enqueue(action);
            }
        }

        // Sort by priority (higher first)
        var sorted = _plannedActions.OrderByDescending(a => a.Priority).ToList();
        _plannedActions.Clear();
        foreach (var action in sorted)
        {
            _plannedActions.Enqueue(action);
        }
    }

    private AIAction? PlanUnitAction(AIContext context, UnitSnapshot unit, IReadOnlyList<UnitSnapshot> enemies)
    {
        // Find nearest enemy
        var (nearestEnemy, distance) = context.FindNearestEnemy(unit.Q, unit.R);
        if (nearestEnemy == null) return null;

        // If adjacent, attack
        if (distance == 1)
        {
            return new AttackAction(unit.Id, nearestEnemy.Value.Id, priority: 100);
        }

        // If we can move and attack, do that
        if (unit.Movement > 0)
        {
            var moveAndAttack = AIActionFactory.CreateMoveAndAttack(context, unit, nearestEnemy.Value);
            if (moveAndAttack != null)
            {
                moveAndAttack.Priority = 90;
                return moveAndAttack;
            }

            // Otherwise just move toward enemy
            var moveToward = AIActionFactory.CreateMoveToward(context, unit, nearestEnemy.Value.Q, nearestEnemy.Value.R);
            if (moveToward != null)
            {
                moveToward.Priority = 50;
                return moveToward;
            }
        }

        // Can't do anything useful, skip this unit
        return new SkipAction(unit.Id);
    }
}

/// <summary>
/// A defensive AI that prioritizes holding positions and counter-attacking.
/// </summary>
public class DefensiveAIController : IAIController
{
    private int _playerId;

    public string Name => "Defensive AI";
    public int DifficultyLevel => 2;

    public void OnTurnStart(int playerId, AIContext context)
    {
        _playerId = playerId;
    }

    public AIAction? DecideAction(AIContext context)
    {
        // Process one unit at a time
        var readyUnits = context.GetReadyUnits();
        if (readyUnits.Count == 0) return null;

        var unit = readyUnits[0];
        return PlanDefensiveAction(context, unit);
    }

    public void OnActionComplete(AIAction action, bool success, AIContext context) { }

    public void OnTurnEnd(AIContext context) { }

    private AIAction? PlanDefensiveAction(AIContext context, UnitSnapshot unit)
    {
        // Check for adjacent threats
        var threats = CountAdjacentThreats(context, unit);

        if (threats > 0)
        {
            // Find the weakest adjacent enemy and attack
            var weakestThreat = FindWeakestAdjacentEnemy(context, unit);
            if (weakestThreat != null)
            {
                return new AttackAction(unit.Id, weakestThreat.Value.Id, priority: 80);
            }
        }

        // If no immediate threats, look for a better defensive position
        if (unit.Movement > 0)
        {
            var betterPosition = FindBetterDefensivePosition(context, unit);
            if (betterPosition != null)
            {
                return new MoveAction(unit.Id, betterPosition.Value.Q, betterPosition.Value.R, priority: 30);
            }
        }

        // Fortify in place
        return new FortifyAction(unit.Id, priority: 10);
    }

    private int CountAdjacentThreats(AIContext context, UnitSnapshot unit)
    {
        return context.CountThreats(unit.Q, unit.R, 1);
    }

    private UnitSnapshot? FindWeakestAdjacentEnemy(AIContext context, UnitSnapshot unit)
    {
        var enemies = context.GetEnemyUnits()
            .Where(e => context.GetDistance(unit.Q, unit.R, e.Q, e.R) == 1)
            .OrderBy(e => e.Health)
            .ToList();

        return enemies.Count > 0 ? enemies[0] : null;
    }

    private CellSnapshot? FindBetterDefensivePosition(AIContext context, UnitSnapshot unit)
    {
        var currentValue = context.EvaluatePosition(unit.Q, unit.R);
        var reachable = context.GetReachableCells(unit);

        CellSnapshot? best = null;
        float bestValue = currentValue;

        foreach (var cell in reachable)
        {
            if (cell.IsOccupied) continue;

            float value = context.EvaluatePosition(cell.Q, cell.R);
            if (value > bestValue)
            {
                bestValue = value;
                best = cell;
            }
        }

        return best;
    }
}
