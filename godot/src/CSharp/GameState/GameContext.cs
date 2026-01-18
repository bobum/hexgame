using HexGame.Services.DependencyInjection;

namespace HexGame.GameState;

/// <summary>
/// Convenience wrapper providing shortcuts to commonly used services.
/// Supports both the new DI container and legacy ServiceLocator for backwards compatibility.
/// </summary>
public static class GameContext
{
    /// <summary>
    /// Gets the EventBus service for publishing and subscribing to game events.
    /// Returns null if EventBus is not registered.
    /// </summary>
    public static EventBus? Events
    {
        get
        {
            // Try new DI container first
            if (GameServices.TryGet<EventBus>(out var eventBus))
                return eventBus;
            // Fall back to legacy ServiceLocator
            return ServiceLocator.TryGet<EventBus>(out eventBus) ? eventBus : null;
        }
    }

    /// <summary>
    /// Gets the CommandHistory service for undo/redo operations.
    /// Returns null if CommandHistory is not registered.
    /// </summary>
    public static CommandHistory? Commands
    {
        get
        {
            if (GameServices.TryGet<CommandHistory>(out var commands))
                return commands;
            return ServiceLocator.TryGet<CommandHistory>(out commands) ? commands : null;
        }
    }

    /// <summary>
    /// Gets the EventBus service, throwing if not registered.
    /// </summary>
    /// <exception cref="InvalidOperationException">If EventBus is not registered.</exception>
    public static EventBus GetEventsRequired()
    {
        if (GameServices.TryGet<EventBus>(out var eventBus))
            return eventBus;
        return ServiceLocator.Get<EventBus>();
    }

    /// <summary>
    /// Gets the CommandHistory service, throwing if not registered.
    /// </summary>
    /// <exception cref="InvalidOperationException">If CommandHistory is not registered.</exception>
    public static CommandHistory GetCommandsRequired()
    {
        if (GameServices.TryGet<CommandHistory>(out var commands))
            return commands;
        return ServiceLocator.Get<CommandHistory>();
    }

    /// <summary>
    /// Attempts to get the EventBus service.
    /// </summary>
    public static bool TryGetEvents([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out EventBus? eventBus)
    {
        if (GameServices.TryGet(out eventBus))
            return true;
        return ServiceLocator.TryGet(out eventBus);
    }

    /// <summary>
    /// Attempts to get the CommandHistory service.
    /// </summary>
    public static bool TryGetCommands([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out CommandHistory? commands)
    {
        if (GameServices.TryGet(out commands))
            return true;
        return ServiceLocator.TryGet(out commands);
    }

    /// <summary>
    /// Checks if the game context is fully initialized.
    /// </summary>
    public static bool IsInitialized => ServiceLocator.IsInitialized;

    /// <summary>
    /// Publishes an event through the EventBus if available.
    /// </summary>
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
    public static bool ExecuteCommand(ICommand command)
    {
        if (TryGetCommands(out var commands))
        {
            return commands.Execute(command);
        }
        return command.CanExecute() && command.Execute();
    }

    /// <summary>
    /// Undoes the last command if possible.
    /// </summary>
    public static bool Undo()
    {
        return TryGetCommands(out var commands) && commands.Undo();
    }

    /// <summary>
    /// Redoes the last undone command if possible.
    /// </summary>
    public static bool Redo()
    {
        return TryGetCommands(out var commands) && commands.Redo();
    }

    #region Convenience Accessors for Common Services

    /// <summary>
    /// Gets a service from the DI container or ServiceLocator.
    /// </summary>
    public static T? Get<T>() where T : class, IService
    {
        if (GameServices.TryGet<T>(out var service))
            return service;
        return ServiceLocator.TryGet<T>(out service) ? service : null;
    }

    /// <summary>
    /// Gets a required service, throwing if not found.
    /// </summary>
    public static T GetRequired<T>() where T : class, IService
    {
        if (GameServices.TryGet<T>(out var service))
            return service;
        return ServiceLocator.Get<T>();
    }

    /// <summary>
    /// Tries to get a service.
    /// </summary>
    public static bool TryGet<T>([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out T? service) where T : class, IService
    {
        if (GameServices.TryGet(out service))
            return true;
        return ServiceLocator.TryGet(out service);
    }

    #endregion
}
