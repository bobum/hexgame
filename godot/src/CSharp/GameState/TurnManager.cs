using HexGame.Units;

namespace HexGame.GameState;

/// <summary>
/// Manages turn-based gameplay.
/// Tracks current turn, current player, and turn phases.
/// </summary>
public class TurnManager : IService
{
    /// <summary>
    /// Player ID for the human player.
    /// </summary>
    public const int PlayerHuman = 1;

    /// <summary>
    /// Starting player ID for AI players.
    /// </summary>
    public const int PlayerAiStart = 2;

    private readonly IUnitManager _unitManager;

    /// <summary>
    /// Current turn number.
    /// </summary>
    public int CurrentTurn { get; private set; } = 1;

    /// <summary>
    /// Current player ID.
    /// </summary>
    public int CurrentPlayer { get; private set; } = PlayerHuman;

    /// <summary>
    /// Current turn phase.
    /// </summary>
    public TurnPhase CurrentPhase { get; private set; } = TurnPhase.Movement;

    /// <summary>
    /// Total number of players (including human).
    /// </summary>
    public int PlayerCount { get; set; } = 2;

    #region Events

    /// <summary>
    /// Fired when a turn starts.
    /// </summary>
    public event Action? TurnStarted;

    /// <summary>
    /// Fired when a turn ends.
    /// </summary>
    public event Action? TurnEnded;

    /// <summary>
    /// Fired when the phase changes.
    /// </summary>
    public event Action<TurnPhase>? PhaseChanged;

    /// <summary>
    /// Fired when the current player changes.
    /// </summary>
    public event Action<int>? PlayerChanged;

    #endregion

    /// <summary>
    /// Creates a new turn manager.
    /// </summary>
    /// <param name="unitManager">The unit manager for resetting movement.</param>
    public TurnManager(IUnitManager unitManager)
    {
        _unitManager = unitManager;
    }

    #region IService Implementation

    public void Initialize()
    {
        // Already initialized in constructor
    }

    public void Shutdown()
    {
        TurnStarted = null;
        TurnEnded = null;
        PhaseChanged = null;
        PlayerChanged = null;
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Gets whether it's the human player's turn.
    /// </summary>
    public bool IsHumanTurn => CurrentPlayer == PlayerHuman;

    /// <summary>
    /// Gets whether it's an AI player's turn.
    /// </summary>
    public bool IsAiTurn => CurrentPlayer >= PlayerAiStart;

    /// <summary>
    /// Gets whether units can move in the current phase.
    /// </summary>
    public bool CanMove => CurrentPhase == TurnPhase.Movement;

    /// <summary>
    /// Gets whether units can attack in the current phase.
    /// </summary>
    public bool CanAttack => CurrentPhase == TurnPhase.Combat;

    /// <summary>
    /// Checks if a unit belongs to the current player.
    /// </summary>
    public bool IsCurrentPlayerUnit(int playerId) => playerId == CurrentPlayer;

    /// <summary>
    /// Gets a status string describing the current turn state.
    /// </summary>
    public string GetStatus()
    {
        string playerName = CurrentPlayer == PlayerHuman ? "Player" : $"AI {CurrentPlayer - 1}";
        return $"Turn {CurrentTurn} - {playerName} ({CurrentPhase.GetDisplayName()})";
    }

    #endregion

    #region Game Flow

    /// <summary>
    /// Starts a new game (reset to turn 1, player 1).
    /// </summary>
    public void StartGame()
    {
        CurrentTurn = 1;
        CurrentPlayer = PlayerHuman;
        CurrentPhase = TurnPhase.Movement;

        _unitManager.ResetAllMovement();

        TurnStarted?.Invoke();

        // Publish to EventBus
        if (GameContext.TryGetEvents(out var eventBus))
        {
            eventBus.Publish(new TurnStartedEvent(CurrentTurn, CurrentPlayer));
        }
    }

    /// <summary>
    /// Ends the current player's turn and advances to the next player.
    /// </summary>
    public void EndTurn()
    {
        TurnEnded?.Invoke();

        // Publish to EventBus
        if (GameContext.TryGetEvents(out var eventBus))
        {
            eventBus.Publish(new TurnEndedEvent(CurrentTurn, CurrentPlayer));
        }

        // Advance to next player
        CurrentPlayer++;

        // If we've gone through all players, start new turn
        if (CurrentPlayer > PlayerCount)
        {
            CurrentTurn++;
            CurrentPlayer = PlayerHuman;
        }

        // Reset to movement phase
        CurrentPhase = TurnPhase.Movement;

        // Reset movement for the new current player
        _unitManager.ResetPlayerMovement(CurrentPlayer);

        PlayerChanged?.Invoke(CurrentPlayer);
        TurnStarted?.Invoke();

        // Publish turn started
        if (GameContext.TryGetEvents(out eventBus))
        {
            eventBus.Publish(new TurnStartedEvent(CurrentTurn, CurrentPlayer));
        }
    }

    /// <summary>
    /// Advances to the next phase.
    /// </summary>
    public void AdvancePhase()
    {
        CurrentPhase = CurrentPhase switch
        {
            TurnPhase.Movement => TurnPhase.Combat,
            TurnPhase.Combat => TurnPhase.End,
            _ => CurrentPhase
        };

        if (CurrentPhase == TurnPhase.End)
        {
            EndTurn();
            return;
        }

        PhaseChanged?.Invoke(CurrentPhase);
    }

    #endregion
}
