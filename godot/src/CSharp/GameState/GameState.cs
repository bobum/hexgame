namespace HexGame.GameState;

/// <summary>
/// Game states for the state machine.
/// </summary>
public enum GameStateType
{
    /// <summary>Initial loading/setup state.</summary>
    Setup,
    /// <summary>Game is being played.</summary>
    Playing,
    /// <summary>Game is paused.</summary>
    Paused,
    /// <summary>Game has ended.</summary>
    GameOver
}

/// <summary>
/// State machine for managing overall game state.
/// </summary>
public class GameStateMachine : IService
{
    private GameStateType _currentState = GameStateType.Setup;
    private GameStateType _previousState = GameStateType.Setup;

    /// <summary>
    /// Gets the current game state.
    /// </summary>
    public GameStateType CurrentState => _currentState;

    /// <summary>
    /// Gets the previous game state.
    /// </summary>
    public GameStateType PreviousState => _previousState;

    /// <summary>
    /// Fired when the game state changes.
    /// </summary>
    public event Action<GameStateType, GameStateType>? StateChanged;

    #region IService Implementation

    public void Initialize()
    {
        _currentState = GameStateType.Setup;
        _previousState = GameStateType.Setup;
    }

    public void Shutdown()
    {
        StateChanged = null;
    }

    #endregion

    /// <summary>
    /// Transitions to a new state.
    /// </summary>
    /// <param name="newState">The state to transition to.</param>
    /// <returns>True if the transition was successful.</returns>
    public bool TransitionTo(GameStateType newState)
    {
        if (!CanTransitionTo(newState))
        {
            return false;
        }

        _previousState = _currentState;
        _currentState = newState;

        StateChanged?.Invoke(_previousState, _currentState);

        // Publish to EventBus
        if (GameContext.TryGetEvents(out var eventBus))
        {
            eventBus.Publish(new GameStateChangedEvent(
                _previousState.ToString(),
                _currentState.ToString()
            ));
        }

        return true;
    }

    /// <summary>
    /// Checks if a transition to the given state is valid.
    /// </summary>
    /// <param name="newState">The target state.</param>
    /// <returns>True if the transition is valid.</returns>
    public bool CanTransitionTo(GameStateType newState)
    {
        // Define valid transitions
        return (_currentState, newState) switch
        {
            // From Setup
            (GameStateType.Setup, GameStateType.Playing) => true,

            // From Playing
            (GameStateType.Playing, GameStateType.Paused) => true,
            (GameStateType.Playing, GameStateType.GameOver) => true,

            // From Paused
            (GameStateType.Paused, GameStateType.Playing) => true,
            (GameStateType.Paused, GameStateType.Setup) => true,

            // From GameOver
            (GameStateType.GameOver, GameStateType.Setup) => true,

            // Same state is always valid (no-op)
            _ when _currentState == newState => true,

            // All other transitions are invalid
            _ => false
        };
    }

    /// <summary>
    /// Checks if the game is in the Setup state.
    /// </summary>
    public bool IsSetup => _currentState == GameStateType.Setup;

    /// <summary>
    /// Checks if the game is being played.
    /// </summary>
    public bool IsPlaying => _currentState == GameStateType.Playing;

    /// <summary>
    /// Checks if the game is paused.
    /// </summary>
    public bool IsPaused => _currentState == GameStateType.Paused;

    /// <summary>
    /// Checks if the game is over.
    /// </summary>
    public bool IsGameOver => _currentState == GameStateType.GameOver;

    /// <summary>
    /// Starts the game (transitions from Setup to Playing).
    /// </summary>
    public bool StartGame() => TransitionTo(GameStateType.Playing);

    /// <summary>
    /// Pauses the game.
    /// </summary>
    public bool Pause() => TransitionTo(GameStateType.Paused);

    /// <summary>
    /// Resumes the game from pause.
    /// </summary>
    public bool Resume() => TransitionTo(GameStateType.Playing);

    /// <summary>
    /// Ends the game.
    /// </summary>
    public bool EndGame() => TransitionTo(GameStateType.GameOver);

    /// <summary>
    /// Restarts the game (returns to setup).
    /// </summary>
    public bool Restart() => TransitionTo(GameStateType.Setup);
}
