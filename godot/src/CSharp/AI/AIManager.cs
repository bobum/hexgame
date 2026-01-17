using HexGame.Commands;
using HexGame.Core;
using HexGame.Events;
using HexGame.GameState;
using HexGame.Pathfinding;
using HexGame.Units;

namespace HexGame.AI;

/// <summary>
/// Manages AI players and orchestrates their turns.
/// Integrates with the turn system to execute AI decisions.
/// </summary>
public class AIManager : IService
{
    private readonly HexGrid _grid;
    private readonly IUnitManager _unitManager;
    private readonly IPathfinder _pathfinder;
    private readonly TurnManager _turnManager;
    private readonly CommandHistory _commandHistory;
    private readonly EventBus _eventBus;

    private readonly Dictionary<int, IAIController> _controllers = new();
    private bool _isProcessingTurn;

    /// <summary>
    /// Default delay between AI actions in milliseconds.
    /// Set to 0 for instant execution.
    /// </summary>
    public int ActionDelayMs { get; set; } = 500;

    /// <summary>
    /// Maximum actions per AI turn (safety limit).
    /// </summary>
    public int MaxActionsPerTurn { get; set; } = 100;

    /// <summary>
    /// Fired when AI starts processing a turn.
    /// </summary>
    public event Action<int>? AITurnStarted;

    /// <summary>
    /// Fired when AI performs an action.
    /// </summary>
    public event Action<int, AIAction>? AIActionPerformed;

    /// <summary>
    /// Fired when AI finishes processing a turn.
    /// </summary>
    public event Action<int>? AITurnEnded;

    /// <summary>
    /// Creates a new AI manager.
    /// </summary>
    public AIManager(
        HexGrid grid,
        IUnitManager unitManager,
        IPathfinder pathfinder,
        TurnManager turnManager,
        CommandHistory commandHistory,
        EventBus eventBus)
    {
        _grid = grid;
        _unitManager = unitManager;
        _pathfinder = pathfinder;
        _turnManager = turnManager;
        _commandHistory = commandHistory;
        _eventBus = eventBus;
    }

    #region IService Implementation

    public void Initialize()
    {
        // Subscribe to turn events
        _turnManager.TurnStarted += OnTurnStarted;
        _eventBus.Subscribe<TurnStartedEvent>(OnTurnStartedEvent);
    }

    public void Shutdown()
    {
        _turnManager.TurnStarted -= OnTurnStarted;
        _eventBus.Unsubscribe<TurnStartedEvent>(OnTurnStartedEvent);

        AITurnStarted = null;
        AIActionPerformed = null;
        AITurnEnded = null;
        _controllers.Clear();
    }

    #endregion

    #region Controller Management

    /// <summary>
    /// Registers an AI controller for a player.
    /// </summary>
    /// <param name="playerId">The player ID (must be >= 2 for AI).</param>
    /// <param name="controller">The AI controller to use.</param>
    public void RegisterController(int playerId, IAIController controller)
    {
        if (playerId < TurnManager.PlayerAiStart)
        {
            throw new ArgumentException($"AI player ID must be >= {TurnManager.PlayerAiStart}", nameof(playerId));
        }

        _controllers[playerId] = controller;
    }

    /// <summary>
    /// Unregisters an AI controller.
    /// </summary>
    /// <param name="playerId">The player ID.</param>
    /// <returns>True if a controller was removed.</returns>
    public bool UnregisterController(int playerId)
    {
        return _controllers.Remove(playerId);
    }

    /// <summary>
    /// Gets the controller for a player.
    /// </summary>
    public IAIController? GetController(int playerId)
    {
        return _controllers.TryGetValue(playerId, out var controller) ? controller : null;
    }

    /// <summary>
    /// Checks if a player has an AI controller.
    /// </summary>
    public bool HasController(int playerId)
    {
        return _controllers.ContainsKey(playerId);
    }

    #endregion

    #region Turn Processing

    private void OnTurnStarted()
    {
        // Check if it's an AI player's turn
        if (_turnManager.IsAiTurn && HasController(_turnManager.CurrentPlayer))
        {
            ProcessAITurn(_turnManager.CurrentPlayer);
        }
    }

    private void OnTurnStartedEvent(TurnStartedEvent evt)
    {
        // Alternative event-based trigger
        if (evt.PlayerId >= TurnManager.PlayerAiStart && HasController(evt.PlayerId))
        {
            ProcessAITurn(evt.PlayerId);
        }
    }

    /// <summary>
    /// Manually triggers AI turn processing.
    /// Useful for testing or custom turn flows.
    /// </summary>
    public void ProcessAITurn(int playerId)
    {
        if (_isProcessingTurn)
        {
            GD.PrintErr($"AIManager: Already processing a turn");
            return;
        }

        if (!_controllers.TryGetValue(playerId, out var controller))
        {
            GD.PrintErr($"AIManager: No controller for player {playerId}");
            return;
        }

        _isProcessingTurn = true;
        AITurnStarted?.Invoke(playerId);

        var context = CreateContext(playerId);
        controller.OnTurnStart(playerId, context);

        // Process actions
        int actionCount = 0;
        while (actionCount < MaxActionsPerTurn)
        {
            context = CreateContext(playerId); // Refresh context
            var action = controller.DecideAction(context);

            if (action == null || action is EndTurnAction)
            {
                break;
            }

            bool success = ExecuteAction(action, context);
            controller.OnActionComplete(action, success, CreateContext(playerId));

            AIActionPerformed?.Invoke(playerId, action);
            actionCount++;

            // Check if we should continue
            if (!success && action is not SkipAction)
            {
                GD.Print($"AIManager: Action failed - {action.Description}");
            }
        }

        if (actionCount >= MaxActionsPerTurn)
        {
            GD.PrintErr($"AIManager: Hit max action limit ({MaxActionsPerTurn}) for player {playerId}");
        }

        controller.OnTurnEnd(CreateContext(playerId));
        AITurnEnded?.Invoke(playerId);

        _isProcessingTurn = false;

        // End the turn
        _turnManager.EndTurn();
    }

    /// <summary>
    /// Creates an AI context for the specified player.
    /// </summary>
    private AIContext CreateContext(int playerId)
    {
        return new AIContext(playerId, _grid, _unitManager, _pathfinder, _turnManager);
    }

    /// <summary>
    /// Executes an AI action.
    /// </summary>
    private bool ExecuteAction(AIAction action, AIContext context)
    {
        if (!action.IsValid(context))
        {
            GD.Print($"AIManager: Invalid action - {action.Description}");
            return false;
        }

        return action switch
        {
            MoveAction move => ExecuteMove(move),
            AttackAction attack => ExecuteAttack(attack),
            MoveAndAttackAction moveAttack => ExecuteMoveAndAttack(moveAttack),
            FortifyAction fortify => ExecuteFortify(fortify),
            SkipAction => true,
            EndTurnAction => true,
            _ => false
        };
    }

    private bool ExecuteMove(MoveAction action)
    {
        if (!action.UnitId.HasValue) return false;

        var unit = _unitManager.GetUnit(action.UnitId.Value);
        if (unit == null) return false;

        var startCell = _grid.GetCell(unit.Q, unit.R);
        var endCell = _grid.GetCell(action.TargetQ, action.TargetR);
        if (startCell == null || endCell == null) return false;

        var pathResult = _pathfinder.FindPath(startCell, endCell, new PathOptions { UnitType = unit.Type });
        if (!pathResult.Reachable) return false;

        var command = new MoveUnitCommand(_unitManager, unit.Id, action.TargetQ, action.TargetR, (int)pathResult.Cost);
        return _commandHistory.Execute(command);
    }

    private bool ExecuteAttack(AttackAction action)
    {
        if (!action.UnitId.HasValue) return false;

        var attacker = _unitManager.GetUnit(action.UnitId.Value);
        var target = _unitManager.GetUnit(action.TargetUnitId);
        if (attacker == null || target == null) return false;

        // Publish attack event (actual combat resolution would be handled by combat system)
        _eventBus.Publish(new AttackRequestedEvent(attacker.Id, target.Id));

        // Mark attacker as having acted
        attacker.HasActed = true;

        return true;
    }

    private bool ExecuteMoveAndAttack(MoveAndAttackAction action)
    {
        // Execute move first
        var moveAction = new MoveAction(action.UnitId!.Value, action.MoveQ, action.MoveR);
        if (!ExecuteMove(moveAction)) return false;

        // Then attack
        var attackAction = new AttackAction(action.UnitId.Value, action.TargetUnitId);
        return ExecuteAttack(attackAction);
    }

    private bool ExecuteFortify(FortifyAction action)
    {
        if (!action.UnitId.HasValue) return false;

        var unit = _unitManager.GetUnit(action.UnitId.Value);
        if (unit == null) return false;

        // Use all movement and mark as acted
        unit.Movement = 0;
        unit.HasActed = true;

        _eventBus.Publish(new UnitFortifiedEvent(unit.Id));

        return true;
    }

    #endregion
}

#region AI Events

/// <summary>
/// Event fired when an attack is requested.
/// Combat system should subscribe to resolve the attack.
/// </summary>
public record AttackRequestedEvent(int AttackerId, int TargetId) : GameEventBase;

/// <summary>
/// Event fired when a unit fortifies.
/// </summary>
public record UnitFortifiedEvent(int UnitId) : GameEventBase;

#endregion
