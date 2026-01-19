namespace HexGame.Commands;

/// <summary>
/// Interface for commands that can be executed, undone, and redone.
/// Implements the Command pattern for action history management.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Gets a description of what this command does (for UI/debugging).
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <returns>True if execution succeeded.</returns>
    bool Execute();

    /// <summary>
    /// Undoes the command, reverting state to before execution.
    /// </summary>
    /// <returns>True if undo succeeded.</returns>
    bool Undo();

    /// <summary>
    /// Checks if this command can be executed in the current state.
    /// </summary>
    /// <returns>True if the command can be executed.</returns>
    bool CanExecute();
}

/// <summary>
/// Base class for commands providing common functionality.
/// </summary>
public abstract class CommandBase : ICommand
{
    /// <summary>
    /// Gets a description of what this command does.
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <returns>True if execution succeeded.</returns>
    public abstract bool Execute();

    /// <summary>
    /// Undoes the command.
    /// </summary>
    /// <returns>True if undo succeeded.</returns>
    public abstract bool Undo();

    /// <summary>
    /// Checks if this command can be executed.
    /// Override to add precondition checks.
    /// </summary>
    /// <returns>True by default.</returns>
    public virtual bool CanExecute() => true;
}
