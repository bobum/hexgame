namespace HexGame.AI;

/// <summary>
/// Interface for AI player decision-making.
/// Implement this interface to create custom AI strategies.
/// </summary>
public interface IAIController
{
    /// <summary>
    /// Called when it becomes this AI's turn.
    /// </summary>
    /// <param name="playerId">The AI player's ID.</param>
    /// <param name="context">Read-only snapshot of the current game state.</param>
    void OnTurnStart(int playerId, AIContext context);

    /// <summary>
    /// Called to request the AI's next action.
    /// Return null to end the turn.
    /// </summary>
    /// <param name="context">Current game state snapshot.</param>
    /// <returns>The action to perform, or null to end turn.</returns>
    AIAction? DecideAction(AIContext context);

    /// <summary>
    /// Called when an action completes (for learning/state update).
    /// </summary>
    /// <param name="action">The action that was executed.</param>
    /// <param name="success">Whether the action succeeded.</param>
    /// <param name="context">Updated game state.</param>
    void OnActionComplete(AIAction action, bool success, AIContext context);

    /// <summary>
    /// Called when this AI's turn ends.
    /// </summary>
    /// <param name="context">Final state of the turn.</param>
    void OnTurnEnd(AIContext context);

    /// <summary>
    /// Gets the name of this AI strategy (for display/debugging).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the difficulty level hint (1-5).
    /// </summary>
    int DifficultyLevel { get; }
}
