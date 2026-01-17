namespace HexGame.GameState;

/// <summary>
/// Convenience wrapper providing shortcuts to commonly used services.
/// Simplifies access to ServiceLocator-registered services.
/// </summary>
public static class GameContext
{
    /// <summary>
    /// Gets the EventBus service for publishing and subscribing to game events.
    /// Returns null if EventBus is not registered.
    /// </summary>
    /// <remarks>
    /// Prefer using <see cref="TryGetEvents"/> or <see cref="PublishEvent{T}"/> for null-safe access.
    /// </remarks>
    public static EventBus? Events => ServiceLocator.TryGet<EventBus>(out var eventBus) ? eventBus : null;

    /// <summary>
    /// Gets the CommandHistory service for undo/redo operations.
    /// Returns null if CommandHistory is not registered.
    /// </summary>
    /// <remarks>
    /// Prefer using <see cref="TryGetCommands"/> or <see cref="ExecuteCommand"/> for null-safe access.
    /// </remarks>
    public static CommandHistory? Commands => ServiceLocator.TryGet<CommandHistory>(out var commands) ? commands : null;

    /// <summary>
    /// Gets the EventBus service, throwing if not registered.
    /// Use this only when you're certain the service is registered.
    /// </summary>
    /// <exception cref="InvalidOperationException">If EventBus is not registered.</exception>
    public static EventBus GetEventsRequired() => ServiceLocator.Get<EventBus>();

    /// <summary>
    /// Gets the CommandHistory service, throwing if not registered.
    /// Use this only when you're certain the service is registered.
    /// </summary>
    /// <exception cref="InvalidOperationException">If CommandHistory is not registered.</exception>
    public static CommandHistory GetCommandsRequired() => ServiceLocator.Get<CommandHistory>();

    /// <summary>
    /// Attempts to get the EventBus service.
    /// </summary>
    /// <param name="eventBus">The EventBus if found.</param>
    /// <returns>True if the service was found.</returns>
    public static bool TryGetEvents([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out EventBus? eventBus)
    {
        return ServiceLocator.TryGet(out eventBus);
    }

    /// <summary>
    /// Attempts to get the CommandHistory service.
    /// </summary>
    /// <param name="commands">The CommandHistory if found.</param>
    /// <returns>True if the service was found.</returns>
    public static bool TryGetCommands([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out CommandHistory? commands)
    {
        return ServiceLocator.TryGet(out commands);
    }

    /// <summary>
    /// Checks if the game context is fully initialized (all core services registered).
    /// </summary>
    public static bool IsInitialized => ServiceLocator.IsInitialized;

    /// <summary>
    /// Publishes an event through the EventBus if available.
    /// Safe to call even if EventBus is not registered (no-op).
    /// </summary>
    /// <typeparam name="T">Event type.</typeparam>
    /// <param name="gameEvent">The event to publish.</param>
    public static void PublishEvent<T>(T gameEvent) where T : IGameEvent
    {
        if (TryGetEvents(out var eventBus))
        {
            eventBus.Publish(gameEvent);
        }
    }

    /// <summary>
    /// Executes a command through the CommandHistory if available.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <returns>True if command was executed, false if failed or CommandHistory unavailable.</returns>
    public static bool ExecuteCommand(ICommand command)
    {
        if (TryGetCommands(out var commands))
        {
            return commands.Execute(command);
        }
        // Fallback: execute directly without history
        return command.CanExecute() && command.Execute();
    }

    /// <summary>
    /// Undoes the last command if possible.
    /// </summary>
    /// <returns>True if a command was undone.</returns>
    public static bool Undo()
    {
        return TryGetCommands(out var commands) && commands.Undo();
    }

    /// <summary>
    /// Redoes the last undone command if possible.
    /// </summary>
    /// <returns>True if a command was redone.</returns>
    public static bool Redo()
    {
        return TryGetCommands(out var commands) && commands.Redo();
    }
}
