using HexGame.Core;
using HexGame.GameState;
using HexGame.Input;
using HexGame.Units;

namespace HexGame.Bridge;

/// <summary>
/// Central hub that connects C# events to GDScript signals.
/// Subscribes to EventBus events and forwards them to GDScriptBridge.
/// </summary>
public class SignalHub : IService
{
    private readonly EventBus _eventBus;
    private readonly GDScriptBridge _bridge;

    private SelectionManager? _selectionManager;
    private InputManager? _inputManager;
    private TurnManager? _turnManager;
    private GameStateMachine? _stateMachine;

    /// <summary>
    /// Creates a new signal hub.
    /// </summary>
    public SignalHub(EventBus eventBus, GDScriptBridge bridge)
    {
        _eventBus = eventBus;
        _bridge = bridge;
    }

    #region IService Implementation

    public void Initialize()
    {
        // Subscribe to EventBus events
        _eventBus.Subscribe<UnitMovedEvent>(OnUnitMoved);
        _eventBus.Subscribe<UnitDestroyedEvent>(OnUnitDestroyed);
        _eventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
        _eventBus.Subscribe<TurnEndedEvent>(OnTurnEnded);
        _eventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);

        // Connect to SelectionManager if available
        if (ServiceLocator.TryGet<SelectionManager>(out _selectionManager))
        {
            _selectionManager.SelectionChanged += OnSelectionChanged;
        }

        // Connect to InputManager if available
        if (ServiceLocator.TryGet<InputManager>(out _inputManager))
        {
            _inputManager.CellHovered += OnCellHovered;
        }

        // Connect to TurnManager if available
        if (ServiceLocator.TryGet<TurnManager>(out _turnManager))
        {
            _turnManager.PhaseChanged += OnPhaseChanged;
        }

        // Connect to GameStateMachine if available
        if (ServiceLocator.TryGet<GameStateMachine>(out _stateMachine))
        {
            _stateMachine.StateChanged += OnStateMachineChanged;
        }
    }

    public void Shutdown()
    {
        // Unsubscribe from EventBus
        _eventBus.Unsubscribe<UnitMovedEvent>(OnUnitMoved);
        _eventBus.Unsubscribe<UnitDestroyedEvent>(OnUnitDestroyed);
        _eventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
        _eventBus.Unsubscribe<TurnEndedEvent>(OnTurnEnded);
        _eventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);

        // Disconnect from managers
        if (_selectionManager != null)
        {
            _selectionManager.SelectionChanged -= OnSelectionChanged;
        }

        if (_inputManager != null)
        {
            _inputManager.CellHovered -= OnCellHovered;
        }

        if (_turnManager != null)
        {
            _turnManager.PhaseChanged -= OnPhaseChanged;
        }

        if (_stateMachine != null)
        {
            _stateMachine.StateChanged -= OnStateMachineChanged;
        }
    }

    #endregion

    #region Event Handlers

    private void OnUnitMoved(UnitMovedEvent evt)
    {
        _bridge.NotifyUnitMoved(evt.UnitId, evt.FromQ, evt.FromR, evt.ToQ, evt.ToR);
    }

    private void OnUnitDestroyed(UnitDestroyedEvent evt)
    {
        // Notify UI that unit was destroyed
        _bridge.ShowMessage($"Unit {evt.UnitId} was destroyed", "combat");
    }

    private void OnTurnStarted(TurnStartedEvent evt)
    {
        string phase = _turnManager?.CurrentPhase.ToString() ?? "Movement";
        _bridge.NotifyTurnChanged(evt.TurnNumber, evt.PlayerId, phase);

        if (evt.PlayerId == GameConstants.GameState.HumanPlayerId)
        {
            _bridge.ShowMessage($"Turn {evt.TurnNumber} - Your turn!", "turn");
        }
        else
        {
            _bridge.ShowMessage($"Turn {evt.TurnNumber} - AI Player {evt.PlayerId}'s turn", "turn");
        }
    }

    private void OnTurnEnded(TurnEndedEvent evt)
    {
        // Can notify UI if needed
    }

    private void OnGameStateChanged(GameStateChangedEvent evt)
    {
        _bridge.NotifyGameStateChanged(evt.NewState);
    }

    private void OnSelectionChanged(Unit? unit)
    {
        if (unit != null)
        {
            _bridge.NotifyUnitSelected(unit);
        }
        else
        {
            _bridge.NotifySelectionCleared();
        }
    }

    private void OnCellHovered(HexCell? cell)
    {
        if (cell != null)
        {
            _bridge.NotifyCellHovered(cell);
        }
        else
        {
            _bridge.NotifyCellHoverCleared();
        }
    }

    private void OnPhaseChanged(TurnPhase phase)
    {
        if (_turnManager != null)
        {
            _bridge.NotifyTurnChanged(
                _turnManager.CurrentTurn,
                _turnManager.CurrentPlayer,
                phase.ToString()
            );
        }
    }

    private void OnStateMachineChanged(GameState.GameState oldState, GameState.GameState newState)
    {
        _bridge.NotifyGameStateChanged(newState.Name);
    }

    #endregion
}

#region Additional Events

/// <summary>
/// Event fired when game state changes.
/// </summary>
public record GameStateChangedEvent(string NewState);

#endregion
