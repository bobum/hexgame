namespace HexGame.Tests;

/// <summary>
/// Unit tests for CommandHistory.
/// </summary>
public partial class CommandHistoryTests : Node
{
    private CommandHistory _history = null!;

    public override void _Ready()
    {
        GD.Print("=== CommandHistory Tests ===");

        TestExecuteCommand();
        TestUndoRedo();
        TestRedoClearedOnNewCommand();
        TestCanUndoCanRedo();
        TestCommandDescriptions();

        GD.Print("=== All CommandHistory Tests Passed ===");
    }

    private void TestExecuteCommand()
    {
        _history = new CommandHistory();
        _history.Initialize();

        int value = 0;
        var cmd = new TestCommand(() => { value = 10; return true; }, () => { value = 0; return true; });

        bool result = _history.Execute(cmd);
        Assert(result, "Execute should return true");
        Assert(value == 10, "Value should be 10 after execute");
        Assert(_history.UndoCount == 1, "Undo count should be 1");

        _history.Shutdown();
        GD.Print("  [PASS] Execute command");
    }

    private void TestUndoRedo()
    {
        _history = new CommandHistory();
        _history.Initialize();

        int value = 0;
        var cmd = new TestCommand(() => { value = 42; return true; }, () => { value = 0; return true; });

        _history.Execute(cmd);
        Assert(value == 42, "Value should be 42");

        bool undoResult = _history.Undo();
        Assert(undoResult, "Undo should succeed");
        Assert(value == 0, "Value should be 0 after undo");
        Assert(_history.UndoCount == 0, "Undo stack should be empty");
        Assert(_history.RedoCount == 1, "Redo stack should have 1");

        bool redoResult = _history.Redo();
        Assert(redoResult, "Redo should succeed");
        Assert(value == 42, "Value should be 42 after redo");
        Assert(_history.UndoCount == 1, "Undo stack should have 1");
        Assert(_history.RedoCount == 0, "Redo stack should be empty");

        _history.Shutdown();
        GD.Print("  [PASS] Undo/Redo");
    }

    private void TestRedoClearedOnNewCommand()
    {
        _history = new CommandHistory();
        _history.Initialize();

        int value = 0;
        var cmd1 = new TestCommand(() => { value = 1; return true; }, () => { value = 0; return true; });
        var cmd2 = new TestCommand(() => { value = 2; return true; }, () => { value = 1; return true; });

        _history.Execute(cmd1);
        _history.Undo();
        Assert(_history.RedoCount == 1, "Redo should have 1 command");

        _history.Execute(cmd2);
        Assert(_history.RedoCount == 0, "Redo should be cleared after new command");

        _history.Shutdown();
        GD.Print("  [PASS] Redo cleared on new command");
    }

    private void TestCanUndoCanRedo()
    {
        _history = new CommandHistory();
        _history.Initialize();

        Assert(!_history.CanUndo, "Should not be able to undo with empty history");
        Assert(!_history.CanRedo, "Should not be able to redo with empty history");

        var cmd = new TestCommand(() => true, () => true);
        _history.Execute(cmd);

        Assert(_history.CanUndo, "Should be able to undo after execute");
        Assert(!_history.CanRedo, "Should not be able to redo before any undo");

        _history.Undo();
        Assert(!_history.CanUndo, "Should not be able to undo after undoing only command");
        Assert(_history.CanRedo, "Should be able to redo after undo");

        _history.Shutdown();
        GD.Print("  [PASS] CanUndo/CanRedo");
    }

    private void TestCommandDescriptions()
    {
        _history = new CommandHistory();
        _history.Initialize();

        Assert(_history.NextUndoDescription == null, "No undo description when empty");
        Assert(_history.NextRedoDescription == null, "No redo description when empty");

        var cmd = new TestCommand(() => true, () => true, "Test Command");
        _history.Execute(cmd);

        Assert(_history.NextUndoDescription == "Test Command", "Should show command description");

        _history.Undo();
        Assert(_history.NextRedoDescription == "Test Command", "Should show redo description");

        _history.Shutdown();
        GD.Print("  [PASS] Command descriptions");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception($"Assertion failed: {message}");
        }
    }

    /// <summary>
    /// Simple test command for unit testing.
    /// </summary>
    private class TestCommand : ICommand
    {
        private readonly Func<bool> _execute;
        private readonly Func<bool> _undo;
        public string Description { get; }

        public TestCommand(Func<bool> execute, Func<bool> undo, string description = "Test")
        {
            _execute = execute;
            _undo = undo;
            Description = description;
        }

        public bool Execute() => _execute();
        public bool Undo() => _undo();
        public bool CanExecute() => true;
    }
}
