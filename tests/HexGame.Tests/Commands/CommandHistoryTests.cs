using FluentAssertions;
using HexGame.Commands;
using NSubstitute;
using Xunit;

namespace HexGame.Tests.Commands;

/// <summary>
/// Tests for CommandHistory undo/redo system.
/// </summary>
public class CommandHistoryTests
{
    [Fact]
    public void Execute_ValidCommand_ReturnsTrue()
    {
        var history = new CommandHistory();
        var command = CreateMockCommand(canExecute: true, executeResult: true);

        var result = history.Execute(command);

        result.Should().BeTrue();
    }

    [Fact]
    public void Execute_CommandCannotExecute_ReturnsFalse()
    {
        var history = new CommandHistory();
        var command = CreateMockCommand(canExecute: false);

        var result = history.Execute(command);

        result.Should().BeFalse();
    }

    [Fact]
    public void Execute_CommandExecuteFails_ReturnsFalse()
    {
        var history = new CommandHistory();
        var command = CreateMockCommand(canExecute: true, executeResult: false);

        var result = history.Execute(command);

        result.Should().BeFalse();
    }

    [Fact]
    public void Execute_AddsCommandToUndoStack()
    {
        var history = new CommandHistory();
        var command = CreateMockCommand(canExecute: true, executeResult: true);

        history.Execute(command);

        history.UndoCount.Should().Be(1);
        history.CanUndo.Should().BeTrue();
    }

    [Fact]
    public void Execute_ClearsRedoStack()
    {
        var history = new CommandHistory();
        var command1 = CreateMockCommand(canExecute: true, executeResult: true, undoResult: true);
        var command2 = CreateMockCommand(canExecute: true, executeResult: true);

        history.Execute(command1);
        history.Undo();
        history.RedoCount.Should().Be(1);

        history.Execute(command2);

        history.RedoCount.Should().Be(0);
        history.CanRedo.Should().BeFalse();
    }

    [Fact]
    public void Undo_NoCommands_ReturnsFalse()
    {
        var history = new CommandHistory();

        var result = history.Undo();

        result.Should().BeFalse();
    }

    [Fact]
    public void Undo_WithCommands_ReturnsTrue()
    {
        var history = new CommandHistory();
        var command = CreateMockCommand(canExecute: true, executeResult: true, undoResult: true);
        history.Execute(command);

        var result = history.Undo();

        result.Should().BeTrue();
    }

    [Fact]
    public void Undo_MovesCommandToRedoStack()
    {
        var history = new CommandHistory();
        var command = CreateMockCommand(canExecute: true, executeResult: true, undoResult: true);
        history.Execute(command);

        history.Undo();

        history.UndoCount.Should().Be(0);
        history.RedoCount.Should().Be(1);
        history.CanRedo.Should().BeTrue();
    }

    [Fact]
    public void Undo_UndoFails_CommandStaysInUndoStack()
    {
        var history = new CommandHistory();
        var command = CreateMockCommand(canExecute: true, executeResult: true, undoResult: false);
        history.Execute(command);

        var result = history.Undo();

        result.Should().BeFalse();
        history.UndoCount.Should().Be(1);
        history.RedoCount.Should().Be(0);
    }

    [Fact]
    public void Redo_NoCommands_ReturnsFalse()
    {
        var history = new CommandHistory();

        var result = history.Redo();

        result.Should().BeFalse();
    }

    [Fact]
    public void Redo_WithUndoneCommand_ReturnsTrue()
    {
        var history = new CommandHistory();
        var command = CreateMockCommand(canExecute: true, executeResult: true, undoResult: true);
        history.Execute(command);
        history.Undo();

        var result = history.Redo();

        result.Should().BeTrue();
    }

    [Fact]
    public void Redo_MovesCommandToUndoStack()
    {
        var history = new CommandHistory();
        var command = CreateMockCommand(canExecute: true, executeResult: true, undoResult: true);
        history.Execute(command);
        history.Undo();

        history.Redo();

        history.UndoCount.Should().Be(1);
        history.RedoCount.Should().Be(0);
    }

    [Fact]
    public void NextUndoDescription_ReturnsCorrectDescription()
    {
        var history = new CommandHistory();
        var command = CreateMockCommand(canExecute: true, executeResult: true, description: "Test Command");
        history.Execute(command);

        history.NextUndoDescription.Should().Be("Test Command");
    }

    [Fact]
    public void NextUndoDescription_NoCommands_ReturnsNull()
    {
        var history = new CommandHistory();

        history.NextUndoDescription.Should().BeNull();
    }

    [Fact]
    public void NextRedoDescription_ReturnsCorrectDescription()
    {
        var history = new CommandHistory();
        var command = CreateMockCommand(canExecute: true, executeResult: true, undoResult: true, description: "Test Command");
        history.Execute(command);
        history.Undo();

        history.NextRedoDescription.Should().Be("Test Command");
    }

    [Fact]
    public void Clear_RemovesAllCommands()
    {
        var history = new CommandHistory();
        var command1 = CreateMockCommand(canExecute: true, executeResult: true, undoResult: true);
        var command2 = CreateMockCommand(canExecute: true, executeResult: true);
        history.Execute(command1);
        history.Execute(command2);
        history.Undo();

        history.Clear();

        history.UndoCount.Should().Be(0);
        history.RedoCount.Should().Be(0);
        history.CanUndo.Should().BeFalse();
        history.CanRedo.Should().BeFalse();
    }

    [Fact]
    public void Shutdown_ClearsHistory()
    {
        var history = new CommandHistory();
        var command = CreateMockCommand(canExecute: true, executeResult: true);
        history.Execute(command);

        history.Shutdown();

        history.UndoCount.Should().Be(0);
    }

    [Fact]
    public void Execute_MultipleCommands_MaintainsOrder()
    {
        var history = new CommandHistory();
        var command1 = CreateMockCommand(canExecute: true, executeResult: true, undoResult: true, description: "First");
        var command2 = CreateMockCommand(canExecute: true, executeResult: true, undoResult: true, description: "Second");
        var command3 = CreateMockCommand(canExecute: true, executeResult: true, undoResult: true, description: "Third");

        history.Execute(command1);
        history.Execute(command2);
        history.Execute(command3);

        history.NextUndoDescription.Should().Be("Third");
        history.Undo();
        history.NextUndoDescription.Should().Be("Second");
        history.Undo();
        history.NextUndoDescription.Should().Be("First");
    }

    [Fact]
    public void UndoRedo_Sequence_MaintainsCorrectState()
    {
        var history = new CommandHistory();
        var command1 = CreateMockCommand(canExecute: true, executeResult: true, undoResult: true, description: "Cmd1");
        var command2 = CreateMockCommand(canExecute: true, executeResult: true, undoResult: true, description: "Cmd2");

        // Execute two commands
        history.Execute(command1);
        history.Execute(command2);
        history.UndoCount.Should().Be(2);
        history.RedoCount.Should().Be(0);

        // Undo one
        history.Undo();
        history.UndoCount.Should().Be(1);
        history.RedoCount.Should().Be(1);
        history.NextUndoDescription.Should().Be("Cmd1");
        history.NextRedoDescription.Should().Be("Cmd2");

        // Redo
        history.Redo();
        history.UndoCount.Should().Be(2);
        history.RedoCount.Should().Be(0);
        history.NextUndoDescription.Should().Be("Cmd2");
    }

    #region Helper Methods

    private static ICommand CreateMockCommand(
        bool canExecute = true,
        bool executeResult = true,
        bool undoResult = true,
        string description = "Test Command")
    {
        var command = Substitute.For<ICommand>();
        command.CanExecute().Returns(canExecute);
        command.Execute().Returns(executeResult);
        command.Undo().Returns(undoResult);
        command.Description.Returns(description);
        return command;
    }

    #endregion
}
