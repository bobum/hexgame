namespace HexGame.Events;

/// <summary>
/// Base interface for all game events.
/// </summary>
public interface IGameEvent
{
    /// <summary>
    /// Timestamp when the event was created.
    /// </summary>
    DateTime Timestamp { get; }
}

/// <summary>
/// Base record for game events providing common functionality.
/// Using records for immutability and easy equality comparison.
/// </summary>
public abstract record GameEventBase : IGameEvent
{
    /// <summary>
    /// Timestamp when the event was created.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

#region Unit Events

/// <summary>
/// Event fired when a unit is created.
/// </summary>
/// <param name="UnitId">The ID of the created unit.</param>
/// <param name="UnitType">The type of unit.</param>
/// <param name="Q">Q coordinate where unit was placed.</param>
/// <param name="R">R coordinate where unit was placed.</param>
/// <param name="PlayerId">The owner player ID.</param>
public record UnitCreatedEvent(int UnitId, int UnitType, int Q, int R, int PlayerId) : GameEventBase;

/// <summary>
/// Event fired when a unit is removed/destroyed.
/// </summary>
/// <param name="UnitId">The ID of the removed unit.</param>
public record UnitRemovedEvent(int UnitId) : GameEventBase;

/// <summary>
/// Event fired when a unit moves.
/// </summary>
/// <param name="UnitId">The ID of the moved unit.</param>
/// <param name="FromQ">Starting Q coordinate.</param>
/// <param name="FromR">Starting R coordinate.</param>
/// <param name="ToQ">Destination Q coordinate.</param>
/// <param name="ToR">Destination R coordinate.</param>
/// <param name="MovementCost">Movement points spent.</param>
public record UnitMovedEvent(int UnitId, int FromQ, int FromR, int ToQ, int ToR, int MovementCost) : GameEventBase;

/// <summary>
/// Event fired when a unit is selected.
/// </summary>
/// <param name="UnitIds">List of selected unit IDs.</param>
public record UnitSelectionChangedEvent(IReadOnlyList<int> UnitIds) : GameEventBase;

#endregion

#region Turn Events

/// <summary>
/// Event fired when a turn starts.
/// </summary>
/// <param name="TurnNumber">The current turn number.</param>
/// <param name="CurrentPlayerId">The player whose turn it is.</param>
public record TurnStartedEvent(int TurnNumber, int CurrentPlayerId) : GameEventBase;

/// <summary>
/// Event fired when a turn ends.
/// </summary>
/// <param name="TurnNumber">The turn that ended.</param>
/// <param name="PlayerId">The player whose turn ended.</param>
public record TurnEndedEvent(int TurnNumber, int PlayerId) : GameEventBase;

#endregion

#region Cell Events

/// <summary>
/// Event fired when a hex cell is hovered.
/// </summary>
/// <param name="Q">Q coordinate of hovered cell.</param>
/// <param name="R">R coordinate of hovered cell.</param>
public record CellHoveredEvent(int Q, int R) : GameEventBase;

/// <summary>
/// Event fired when hovering exits a cell.
/// </summary>
public record CellUnhoveredEvent() : GameEventBase;

/// <summary>
/// Event fired when a cell is clicked/selected.
/// </summary>
/// <param name="Q">Q coordinate of clicked cell.</param>
/// <param name="R">R coordinate of clicked cell.</param>
/// <param name="Button">Mouse button that was clicked.</param>
public record CellClickedEvent(int Q, int R, int Button) : GameEventBase;

#endregion

#region Map Events

/// <summary>
/// Event fired when map generation starts.
/// </summary>
/// <param name="Width">Map width.</param>
/// <param name="Height">Map height.</param>
/// <param name="Seed">Generation seed.</param>
public record MapGenerationStartedEvent(int Width, int Height, int Seed) : GameEventBase;

/// <summary>
/// Event fired during map generation with progress updates.
/// </summary>
/// <param name="Phase">Current generation phase name.</param>
/// <param name="Progress">Progress percentage (0-1).</param>
public record MapGenerationProgressEvent(string Phase, float Progress) : GameEventBase;

/// <summary>
/// Event fired when map generation completes.
/// </summary>
/// <param name="Success">Whether generation succeeded.</param>
/// <param name="WorkerTimeMs">Time spent in worker thread.</param>
/// <param name="FeatureTimeMs">Time spent generating features.</param>
public record MapGenerationCompletedEvent(bool Success, float WorkerTimeMs, float FeatureTimeMs) : GameEventBase;

#endregion

#region Game State Events

/// <summary>
/// Event fired when the game state changes.
/// </summary>
/// <param name="OldState">Previous state.</param>
/// <param name="NewState">New state.</param>
public record GameStateChangedEvent(string OldState, string NewState) : GameEventBase;

#endregion
