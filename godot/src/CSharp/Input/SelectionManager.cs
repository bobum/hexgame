using HexGame.Commands;
using HexGame.Core;
using HexGame.GameState;
using HexGame.Pathfinding;
using HexGame.Rendering;
using HexGame.Units;

namespace HexGame.Input;

/// <summary>
/// Manages selection state for units and cells.
/// Handles unit selection, movement commands, and visual feedback.
/// </summary>
public class SelectionManager : IService
{
    private readonly IUnitManager _unitManager;
    private readonly IPathfinder _pathfinder;
    private readonly CommandHistory _commandHistory;
    private readonly EventBus _eventBus;

    private Unit? _selectedUnit;
    private HexCell? _hoveredCell;
    private Dictionary<HexCell, float>? _reachableCells;
    private PathResult? _currentPath;

    /// <summary>
    /// The currently selected unit.
    /// </summary>
    public Unit? SelectedUnit => _selectedUnit;

    /// <summary>
    /// The currently hovered cell.
    /// </summary>
    public HexCell? HoveredCell => _hoveredCell;

    /// <summary>
    /// Cells reachable by the selected unit.
    /// </summary>
    public IReadOnlyDictionary<HexCell, float>? ReachableCells => _reachableCells;

    /// <summary>
    /// Current path preview for movement.
    /// </summary>
    public PathResult? CurrentPath => _currentPath;

    #region Events

    /// <summary>
    /// Fired when selection changes.
    /// </summary>
    public event Action<Unit?>? SelectionChanged;

    /// <summary>
    /// Fired when reachable cells are updated.
    /// </summary>
    public event Action<IReadOnlyDictionary<HexCell, float>?>? ReachableCellsChanged;

    /// <summary>
    /// Fired when the path preview changes.
    /// </summary>
    public event Action<PathResult?>? PathPreviewChanged;

    /// <summary>
    /// Fired when a move command is issued.
    /// </summary>
    public event Action<Unit, HexCell>? MoveCommandIssued;

    #endregion

    /// <summary>
    /// Creates a new selection manager.
    /// </summary>
    public SelectionManager(
        IUnitManager unitManager,
        IPathfinder pathfinder,
        CommandHistory commandHistory,
        EventBus eventBus)
    {
        _unitManager = unitManager;
        _pathfinder = pathfinder;
        _commandHistory = commandHistory;
        _eventBus = eventBus;
    }

    #region IService Implementation

    public void Initialize()
    {
        // Subscribe to relevant events
        _eventBus.Subscribe<UnitMovedEvent>(OnUnitMoved);
        _eventBus.Subscribe<UnitDestroyedEvent>(OnUnitDestroyed);
        _eventBus.Subscribe<TurnEndedEvent>(OnTurnEnded);
    }

    public void Shutdown()
    {
        _eventBus.Unsubscribe<UnitMovedEvent>(OnUnitMoved);
        _eventBus.Unsubscribe<UnitDestroyedEvent>(OnUnitDestroyed);
        _eventBus.Unsubscribe<TurnEndedEvent>(OnTurnEnded);

        SelectionChanged = null;
        ReachableCellsChanged = null;
        PathPreviewChanged = null;
        MoveCommandIssued = null;
    }

    #endregion

    #region Selection

    /// <summary>
    /// Handles a cell click event.
    /// </summary>
    public void HandleCellClick(HexCell? cell, int currentPlayerId)
    {
        if (cell == null)
        {
            ClearSelection();
            return;
        }

        // Check if clicking on a unit
        var unitAtCell = _unitManager.GetUnitAt(cell.Q, cell.R);

        if (unitAtCell != null && unitAtCell.PlayerId == currentPlayerId)
        {
            // Select friendly unit
            SelectUnit(unitAtCell);
        }
        else if (_selectedUnit != null)
        {
            // Try to move selected unit to cell
            TryMoveSelectedUnit(cell);
        }
        else
        {
            // Just select the cell (no unit selected)
            ClearSelection();
        }
    }

    /// <summary>
    /// Handles a cell action (right-click) event.
    /// </summary>
    public void HandleCellAction(HexCell? cell, int currentPlayerId)
    {
        if (cell == null || _selectedUnit == null) return;

        // Check for enemy unit (attack)
        var unitAtCell = _unitManager.GetUnitAt(cell.Q, cell.R);
        if (unitAtCell != null && unitAtCell.PlayerId != currentPlayerId)
        {
            // TODO: Issue attack command
            _eventBus.Publish(new AttackCommandEvent(_selectedUnit.Id, unitAtCell.Id));
            return;
        }

        // Otherwise, try to move
        TryMoveSelectedUnit(cell);
    }

    /// <summary>
    /// Selects a unit.
    /// </summary>
    public void SelectUnit(Unit? unit)
    {
        if (_selectedUnit == unit) return;

        _selectedUnit = unit;
        _currentPath = null;

        if (unit != null)
        {
            // Calculate reachable cells
            var cell = unit.Cell;
            if (cell != null)
            {
                _reachableCells = _pathfinder.GetReachableCells(
                    cell,
                    unit.CurrentMovement,
                    new PathOptions { UnitType = unit.UnitType, IgnoreUnits = false }
                );
            }
            else
            {
                _reachableCells = null;
            }
        }
        else
        {
            _reachableCells = null;
        }

        SelectionChanged?.Invoke(unit);
        ReachableCellsChanged?.Invoke(_reachableCells);
        PathPreviewChanged?.Invoke(null);
    }

    /// <summary>
    /// Clears the current selection.
    /// </summary>
    public void ClearSelection()
    {
        SelectUnit(null);
    }

    #endregion

    #region Hover and Path Preview

    /// <summary>
    /// Updates the hovered cell and path preview.
    /// </summary>
    public void UpdateHover(HexCell? cell)
    {
        _hoveredCell = cell;

        if (_selectedUnit == null || cell == null)
        {
            if (_currentPath != null)
            {
                _currentPath = null;
                PathPreviewChanged?.Invoke(null);
            }
            return;
        }

        // Check if cell is reachable
        if (_reachableCells != null && _reachableCells.ContainsKey(cell))
        {
            // Calculate path for preview
            var startCell = _selectedUnit.Cell;
            if (startCell != null)
            {
                var pathResult = _pathfinder.FindPath(
                    startCell,
                    cell,
                    new PathOptions { UnitType = _selectedUnit.UnitType, IgnoreUnits = false }
                );

                if (pathResult.Success && pathResult != _currentPath)
                {
                    _currentPath = pathResult;
                    PathPreviewChanged?.Invoke(pathResult);
                }
            }
        }
        else if (_currentPath != null)
        {
            _currentPath = null;
            PathPreviewChanged?.Invoke(null);
        }
    }

    #endregion

    #region Movement

    private void TryMoveSelectedUnit(HexCell targetCell)
    {
        if (_selectedUnit == null) return;

        // Check if target is reachable
        if (_reachableCells == null || !_reachableCells.ContainsKey(targetCell))
        {
            return;
        }

        // Check if target is occupied by another unit
        var unitAtTarget = _unitManager.GetUnitAt(targetCell.Q, targetCell.R);
        if (unitAtTarget != null)
        {
            return;
        }

        // Create and execute move command
        var startCell = _selectedUnit.Cell;
        if (startCell == null) return;

        var pathResult = _pathfinder.FindPath(
            startCell,
            targetCell,
            new PathOptions { UnitType = _selectedUnit.UnitType }
        );

        if (!pathResult.Success) return;

        var moveCommand = new MoveUnitCommand(
            _selectedUnit,
            _unitManager,
            pathResult.Path,
            pathResult.TotalCost
        );

        if (_commandHistory.Execute(moveCommand))
        {
            MoveCommandIssued?.Invoke(_selectedUnit, targetCell);

            // Update reachable cells after move
            RefreshReachableCells();
        }
    }

    private void RefreshReachableCells()
    {
        if (_selectedUnit == null)
        {
            _reachableCells = null;
            ReachableCellsChanged?.Invoke(null);
            return;
        }

        var cell = _selectedUnit.Cell;
        if (cell != null && _selectedUnit.CurrentMovement > 0)
        {
            _reachableCells = _pathfinder.GetReachableCells(
                cell,
                _selectedUnit.CurrentMovement,
                new PathOptions { UnitType = _selectedUnit.UnitType, IgnoreUnits = false }
            );
        }
        else
        {
            _reachableCells = new Dictionary<HexCell, float>();
        }

        ReachableCellsChanged?.Invoke(_reachableCells);
    }

    #endregion

    #region Event Handlers

    private void OnUnitMoved(UnitMovedEvent evt)
    {
        if (_selectedUnit != null && _selectedUnit.Id == evt.UnitId)
        {
            RefreshReachableCells();
        }
    }

    private void OnUnitDestroyed(UnitDestroyedEvent evt)
    {
        if (_selectedUnit != null && _selectedUnit.Id == evt.UnitId)
        {
            ClearSelection();
        }
    }

    private void OnTurnEnded(TurnEndedEvent evt)
    {
        ClearSelection();
    }

    #endregion
}

#region Commands

/// <summary>
/// Command to move a unit along a path.
/// </summary>
public class MoveUnitCommand : ICommand
{
    private readonly Unit _unit;
    private readonly IUnitManager _unitManager;
    private readonly List<HexCell> _path;
    private readonly float _movementCost;
    private readonly int _originalQ;
    private readonly int _originalR;
    private readonly float _originalMovement;

    public string Description => $"Move {_unit.UnitType} to ({_path[^1].Q}, {_path[^1].R})";

    public MoveUnitCommand(Unit unit, IUnitManager unitManager, List<HexCell> path, float movementCost)
    {
        _unit = unit;
        _unitManager = unitManager;
        _path = new List<HexCell>(path);
        _movementCost = movementCost;
        _originalQ = unit.Q;
        _originalR = unit.R;
        _originalMovement = unit.CurrentMovement;
    }

    public bool CanExecute()
    {
        return _unit.CurrentMovement >= _movementCost && _path.Count >= 2;
    }

    public bool Execute()
    {
        var destination = _path[^1];
        return _unitManager.MoveUnit(_unit.Id, destination.Q, destination.R, _movementCost);
    }

    public bool Undo()
    {
        if (_unitManager.MoveUnit(_unit.Id, _originalQ, _originalR, 0))
        {
            _unit.CurrentMovement = _originalMovement;
            return true;
        }
        return false;
    }
}

#endregion

#region Events

/// <summary>
/// Event fired when a unit moves.
/// </summary>
public record UnitMovedEvent(int UnitId, int FromQ, int FromR, int ToQ, int ToR);

/// <summary>
/// Event fired when a unit is destroyed.
/// </summary>
public record UnitDestroyedEvent(int UnitId);

/// <summary>
/// Event fired when an attack command is issued.
/// </summary>
public record AttackCommandEvent(int AttackerId, int TargetId);

#endregion
