namespace HexGame.Commands;

/// <summary>
/// Manages command history for undo/redo functionality.
/// Maintains separate stacks for executed and undone commands.
/// </summary>
public class CommandHistory : IService
{
    /// <summary>
    /// Maximum number of commands to keep in history.
    /// </summary>
    public const int MaxHistorySize = 100;

    private readonly Stack<ICommand> _undoStack = new();
    private readonly Stack<ICommand> _redoStack = new();
    private readonly object _lock = new();

    #region IService Implementation

    /// <summary>
    /// Initializes the command history service.
    /// </summary>
    public void Initialize()
    {
        // No initialization needed
    }

    /// <summary>
    /// Shuts down and clears command history.
    /// </summary>
    public void Shutdown()
    {
        Clear();
    }

    #endregion

    #region Execute/Undo/Redo

    /// <summary>
    /// Executes a command and adds it to history if successful.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <returns>True if the command was executed successfully.</returns>
    public bool Execute(ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!command.CanExecute())
        {
            return false;
        }

        lock (_lock)
        {
            if (command.Execute())
            {
                _undoStack.Push(command);
                _redoStack.Clear(); // Clear redo stack when new command is executed

                // Trim history if needed
                TrimHistory();

                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Undoes the most recent command.
    /// </summary>
    /// <returns>True if a command was undone.</returns>
    public bool Undo()
    {
        lock (_lock)
        {
            if (_undoStack.Count == 0)
            {
                return false;
            }

            var command = _undoStack.Pop();
            if (command.Undo())
            {
                _redoStack.Push(command);
                return true;
            }

            // If undo failed, push it back
            _undoStack.Push(command);
            return false;
        }
    }

    /// <summary>
    /// Redoes the most recently undone command.
    /// </summary>
    /// <returns>True if a command was redone.</returns>
    public bool Redo()
    {
        lock (_lock)
        {
            if (_redoStack.Count == 0)
            {
                return false;
            }

            var command = _redoStack.Pop();
            if (command.Execute())
            {
                _undoStack.Push(command);
                return true;
            }

            // If redo failed, push it back
            _redoStack.Push(command);
            return false;
        }
    }

    #endregion

    #region Query

    /// <summary>
    /// Gets whether there are commands that can be undone.
    /// </summary>
    public bool CanUndo
    {
        get
        {
            lock (_lock)
            {
                return _undoStack.Count > 0;
            }
        }
    }

    /// <summary>
    /// Gets whether there are commands that can be redone.
    /// </summary>
    public bool CanRedo
    {
        get
        {
            lock (_lock)
            {
                return _redoStack.Count > 0;
            }
        }
    }

    /// <summary>
    /// Gets the description of the next command to undo.
    /// </summary>
    public string? NextUndoDescription
    {
        get
        {
            lock (_lock)
            {
                return _undoStack.Count > 0 ? _undoStack.Peek().Description : null;
            }
        }
    }

    /// <summary>
    /// Gets the description of the next command to redo.
    /// </summary>
    public string? NextRedoDescription
    {
        get
        {
            lock (_lock)
            {
                return _redoStack.Count > 0 ? _redoStack.Peek().Description : null;
            }
        }
    }

    /// <summary>
    /// Gets the number of commands in the undo stack.
    /// </summary>
    public int UndoCount
    {
        get
        {
            lock (_lock)
            {
                return _undoStack.Count;
            }
        }
    }

    /// <summary>
    /// Gets the number of commands in the redo stack.
    /// </summary>
    public int RedoCount
    {
        get
        {
            lock (_lock)
            {
                return _redoStack.Count;
            }
        }
    }

    #endregion

    #region Management

    /// <summary>
    /// Clears all command history.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }
    }

    private void TrimHistory()
    {
        if (_undoStack.Count <= MaxHistorySize)
        {
            return;
        }

        // Stack.ToArray() returns elements in LIFO order (newest first at index 0)
        // We want to keep the newest MaxHistorySize commands
        var commands = _undoStack.ToArray();
        _undoStack.Clear();

        // Push from index MaxHistorySize-1 down to 0 to restore correct order
        // (oldest of the kept commands first, newest last)
        for (int i = MaxHistorySize - 1; i >= 0; i--)
        {
            _undoStack.Push(commands[i]);
        }
    }

    #endregion
}
