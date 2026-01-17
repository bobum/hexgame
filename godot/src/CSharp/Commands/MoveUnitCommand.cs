namespace HexGame.Commands;

/// <summary>
/// Command to move a unit from one hex to another.
/// Supports undo by storing the original position.
/// </summary>
public class MoveUnitCommand : CommandBase
{
    private readonly int _unitId;
    private readonly int _fromQ;
    private readonly int _fromR;
    private readonly int _toQ;
    private readonly int _toR;
    private readonly int _movementCost;
    private readonly Func<int, int, int, int, bool> _moveAction;
    private bool _wasExecuted;

    /// <summary>
    /// Creates a new move unit command.
    /// </summary>
    /// <param name="unitId">ID of the unit to move.</param>
    /// <param name="fromQ">Starting Q coordinate.</param>
    /// <param name="fromR">Starting R coordinate.</param>
    /// <param name="toQ">Destination Q coordinate.</param>
    /// <param name="toR">Destination R coordinate.</param>
    /// <param name="movementCost">Movement points this move costs.</param>
    /// <param name="moveAction">Function to actually perform the move (unitId, toQ, toR, cost) -> success.</param>
    public MoveUnitCommand(
        int unitId,
        int fromQ, int fromR,
        int toQ, int toR,
        int movementCost,
        Func<int, int, int, int, bool> moveAction)
    {
        _unitId = unitId;
        _fromQ = fromQ;
        _fromR = fromR;
        _toQ = toQ;
        _toR = toR;
        _movementCost = movementCost;
        _moveAction = moveAction;
    }

    /// <summary>
    /// Gets the command description.
    /// </summary>
    public override string Description => $"Move unit {_unitId} from ({_fromQ},{_fromR}) to ({_toQ},{_toR})";

    /// <summary>
    /// Executes the move.
    /// </summary>
    public override bool Execute()
    {
        if (_wasExecuted)
        {
            // Re-executing (redo) - move to destination
            return _moveAction(_unitId, _toQ, _toR, 0); // No cost on redo
        }

        _wasExecuted = _moveAction(_unitId, _toQ, _toR, _movementCost);
        return _wasExecuted;
    }

    /// <summary>
    /// Undoes the move by returning unit to original position.
    /// </summary>
    public override bool Undo()
    {
        // Move back to original position, no movement cost (it's a revert)
        return _moveAction(_unitId, _fromQ, _fromR, 0);
    }
}
