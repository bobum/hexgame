using HexGame.Core;
using HexGame.GameState;
using HexGame.Units;

namespace HexGame.Bridge;

/// <summary>
/// Bridge class for communication between C# and GDScript.
/// Provides a stable API that GDScript UI can call into.
/// </summary>
public partial class GDScriptBridge : Node
{
    private static GDScriptBridge? _instance;

    /// <summary>
    /// Singleton instance for easy access from GDScript.
    /// </summary>
    public static GDScriptBridge? Instance => _instance;

    #region Signals (for GDScript to connect to)

    /// <summary>
    /// Emitted when the game state changes.
    /// </summary>
    [Signal]
    public delegate void GameStateChangedEventHandler(string stateName);

    /// <summary>
    /// Emitted when a unit is selected.
    /// </summary>
    [Signal]
    public delegate void UnitSelectedEventHandler(int unitId, string unitType, int playerId);

    /// <summary>
    /// Emitted when selection is cleared.
    /// </summary>
    [Signal]
    public delegate void SelectionClearedEventHandler();

    /// <summary>
    /// Emitted when a unit moves.
    /// </summary>
    [Signal]
    public delegate void UnitMovedEventHandler(int unitId, int fromQ, int fromR, int toQ, int toR);

    /// <summary>
    /// Emitted when turn changes.
    /// </summary>
    [Signal]
    public delegate void TurnChangedEventHandler(int turnNumber, int currentPlayer, string phase);

    /// <summary>
    /// Emitted when a cell is hovered.
    /// </summary>
    [Signal]
    public delegate void CellHoveredEventHandler(int q, int r, string terrain, int elevation);

    /// <summary>
    /// Emitted when hover leaves all cells.
    /// </summary>
    [Signal]
    public delegate void CellHoverClearedEventHandler();

    /// <summary>
    /// Emitted when a message should be shown to the user.
    /// </summary>
    [Signal]
    public delegate void ShowMessageEventHandler(string message, string messageType);

    /// <summary>
    /// Emitted when resources/stats update.
    /// </summary>
    [Signal]
    public delegate void StatsUpdatedEventHandler(Godot.Collections.Dictionary stats);

    #endregion

    public override void _EnterTree()
    {
        _instance = this;
    }

    public override void _ExitTree()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    #region Methods callable from GDScript

    /// <summary>
    /// Ends the current player's turn.
    /// </summary>
    public void EndTurn()
    {
        if (ServiceLocator.TryGet<TurnManager>(out var turnManager))
        {
            turnManager.EndTurn();
        }
    }

    /// <summary>
    /// Selects a unit by ID.
    /// </summary>
    public void SelectUnit(int unitId)
    {
        if (ServiceLocator.TryGet<IUnitManager>(out var unitManager))
        {
            var unit = unitManager.GetUnit(unitId);
            if (unit != null)
            {
                EmitSignal(SignalName.UnitSelected, unit.Id, unit.UnitType.ToString(), unit.PlayerId);
            }
        }
    }

    /// <summary>
    /// Clears the current selection.
    /// </summary>
    public void ClearSelection()
    {
        EmitSignal(SignalName.SelectionCleared);
    }

    /// <summary>
    /// Gets unit info as a dictionary for GDScript.
    /// </summary>
    public Godot.Collections.Dictionary GetUnitInfo(int unitId)
    {
        var dict = new Godot.Collections.Dictionary();

        if (ServiceLocator.TryGet<IUnitManager>(out var unitManager))
        {
            var unit = unitManager.GetUnit(unitId);
            if (unit != null)
            {
                dict["id"] = unit.Id;
                dict["type"] = unit.UnitType.ToString();
                dict["player_id"] = unit.PlayerId;
                dict["q"] = unit.Q;
                dict["r"] = unit.R;
                dict["health"] = unit.CurrentHealth;
                dict["max_health"] = unit.MaxHealth;
                dict["movement"] = unit.CurrentMovement;
                dict["max_movement"] = unit.MaxMovement;
                dict["attack"] = unit.Attack;
                dict["defense"] = unit.Defense;
            }
        }

        return dict;
    }

    /// <summary>
    /// Gets cell info as a dictionary for GDScript.
    /// </summary>
    public Godot.Collections.Dictionary GetCellInfo(int q, int r)
    {
        var dict = new Godot.Collections.Dictionary();

        if (ServiceLocator.TryGet<HexGrid>(out var grid))
        {
            var cell = grid.GetCell(q, r);
            if (cell != null)
            {
                dict["q"] = cell.Q;
                dict["r"] = cell.R;
                dict["terrain"] = cell.TerrainType.ToString();
                dict["elevation"] = cell.Elevation;
                dict["has_river"] = cell.HasRiver;
                dict["has_road"] = cell.HasRoad;
                dict["is_water"] = cell.IsWater;
                dict["moisture"] = cell.Moisture;
            }
        }

        return dict;
    }

    /// <summary>
    /// Gets current turn info.
    /// </summary>
    public Godot.Collections.Dictionary GetTurnInfo()
    {
        var dict = new Godot.Collections.Dictionary();

        if (ServiceLocator.TryGet<TurnManager>(out var turnManager))
        {
            dict["turn"] = turnManager.CurrentTurn;
            dict["player"] = turnManager.CurrentPlayer;
            dict["phase"] = turnManager.CurrentPhase.ToString();
            dict["is_human_turn"] = turnManager.IsHumanTurn;
            dict["can_move"] = turnManager.CanMove;
            dict["can_attack"] = turnManager.CanAttack;
        }

        return dict;
    }

    /// <summary>
    /// Gets all units for a player.
    /// </summary>
    public Godot.Collections.Array<int> GetPlayerUnits(int playerId)
    {
        var units = new Godot.Collections.Array<int>();

        if (ServiceLocator.TryGet<IUnitManager>(out var unitManager))
        {
            foreach (var unit in unitManager.GetAllUnits())
            {
                if (unit.PlayerId == playerId)
                {
                    units.Add(unit.Id);
                }
            }
        }

        return units;
    }

    /// <summary>
    /// Checks if undo is available.
    /// </summary>
    public bool CanUndo()
    {
        if (ServiceLocator.TryGet<Commands.CommandHistory>(out var history))
        {
            return history.CanUndo;
        }
        return false;
    }

    /// <summary>
    /// Checks if redo is available.
    /// </summary>
    public bool CanRedo()
    {
        if (ServiceLocator.TryGet<Commands.CommandHistory>(out var history))
        {
            return history.CanRedo;
        }
        return false;
    }

    /// <summary>
    /// Performs undo.
    /// </summary>
    public bool Undo()
    {
        if (ServiceLocator.TryGet<Commands.CommandHistory>(out var history))
        {
            return history.Undo();
        }
        return false;
    }

    /// <summary>
    /// Performs redo.
    /// </summary>
    public bool Redo()
    {
        if (ServiceLocator.TryGet<Commands.CommandHistory>(out var history))
        {
            return history.Redo();
        }
        return false;
    }

    /// <summary>
    /// Moves camera to focus on coordinates.
    /// </summary>
    public void FocusCamera(int q, int r)
    {
        if (ServiceLocator.TryGet<Input.CameraController>(out var camera))
        {
            camera.FocusOnCoords(q, r);
        }
    }

    #endregion

    #region Methods for C# to emit signals to GDScript

    /// <summary>
    /// Notifies GDScript that game state changed.
    /// </summary>
    public void NotifyGameStateChanged(string stateName)
    {
        EmitSignal(SignalName.GameStateChanged, stateName);
    }

    /// <summary>
    /// Notifies GDScript that a unit was selected.
    /// </summary>
    public void NotifyUnitSelected(Unit unit)
    {
        EmitSignal(SignalName.UnitSelected, unit.Id, unit.UnitType.ToString(), unit.PlayerId);
    }

    /// <summary>
    /// Notifies GDScript that selection was cleared.
    /// </summary>
    public void NotifySelectionCleared()
    {
        EmitSignal(SignalName.SelectionCleared);
    }

    /// <summary>
    /// Notifies GDScript that a unit moved.
    /// </summary>
    public void NotifyUnitMoved(int unitId, int fromQ, int fromR, int toQ, int toR)
    {
        EmitSignal(SignalName.UnitMoved, unitId, fromQ, fromR, toQ, toR);
    }

    /// <summary>
    /// Notifies GDScript that turn changed.
    /// </summary>
    public void NotifyTurnChanged(int turn, int player, string phase)
    {
        EmitSignal(SignalName.TurnChanged, turn, player, phase);
    }

    /// <summary>
    /// Notifies GDScript of hovered cell.
    /// </summary>
    public void NotifyCellHovered(HexCell cell)
    {
        EmitSignal(SignalName.CellHovered, cell.Q, cell.R, cell.TerrainType.ToString(), cell.Elevation);
    }

    /// <summary>
    /// Notifies GDScript that hover was cleared.
    /// </summary>
    public void NotifyCellHoverCleared()
    {
        EmitSignal(SignalName.CellHoverCleared);
    }

    /// <summary>
    /// Shows a message to the user via GDScript UI.
    /// </summary>
    public void ShowMessage(string message, string messageType = "info")
    {
        EmitSignal(SignalName.ShowMessage, message, messageType);
    }

    /// <summary>
    /// Updates stats display.
    /// </summary>
    public void UpdateStats(Godot.Collections.Dictionary stats)
    {
        EmitSignal(SignalName.StatsUpdated, stats);
    }

    #endregion
}
