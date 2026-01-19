using HexGame.Core;
using HexGame.GameState;
using HexGame.Units;

namespace HexGame.Bridge;

/// <summary>
/// Provides data for UI elements in a format friendly to GDScript.
/// Aggregates game state into UI-ready dictionaries.
/// </summary>
public partial class UIDataProvider : Node
{
    /// <summary>
    /// Gets the complete game state for UI display.
    /// </summary>
    public Godot.Collections.Dictionary GetGameState()
    {
        var state = new Godot.Collections.Dictionary();

        // Turn info
        if (ServiceLocator.TryGet<TurnManager>(out var turnManager))
        {
            state["turn"] = turnManager.CurrentTurn;
            state["current_player"] = turnManager.CurrentPlayer;
            state["phase"] = turnManager.CurrentPhase.ToString();
            state["is_human_turn"] = turnManager.IsHumanTurn;
            state["status"] = turnManager.GetStatus();
        }

        // Game state
        if (ServiceLocator.TryGet<GameStateMachine>(out var stateMachine))
        {
            state["game_state"] = stateMachine.CurrentState.ToString();
            state["is_playing"] = stateMachine.IsPlaying;
            state["is_paused"] = stateMachine.IsPaused;
        }

        // Unit counts
        if (ServiceLocator.TryGet<IUnitManager>(out var unitManager))
        {
            var units = unitManager.GetAllUnits().ToList();
            state["total_units"] = units.Count;
            state["player_units"] = units.Count(u => u.PlayerId == GameConstants.GameState.HumanPlayerId);
            state["enemy_units"] = units.Count(u => u.PlayerId != GameConstants.GameState.HumanPlayerId);
        }

        // Command history
        if (ServiceLocator.TryGet<Commands.CommandHistory>(out var history))
        {
            state["can_undo"] = history.CanUndo;
            state["can_redo"] = history.CanRedo;
            state["undo_count"] = history.UndoCount;
            state["redo_count"] = history.RedoCount;
        }

        return state;
    }

    /// <summary>
    /// Gets minimap data for all visible cells.
    /// </summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetMinimapData()
    {
        var data = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        if (!ServiceLocator.TryGet<HexGrid>(out var grid))
        {
            return data;
        }

        foreach (var cell in grid.GetAllCells())
        {
            var cellData = new Godot.Collections.Dictionary
            {
                ["q"] = cell.Q,
                ["r"] = cell.R,
                ["terrain"] = (int)cell.TerrainType,
                ["elevation"] = cell.Elevation,
                ["is_water"] = cell.IsWater
            };
            data.Add(cellData);
        }

        return data;
    }

    /// <summary>
    /// Gets unit positions for minimap.
    /// </summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetUnitPositions()
    {
        var data = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        if (!ServiceLocator.TryGet<IUnitManager>(out var unitManager))
        {
            return data;
        }

        foreach (var unit in unitManager.GetAllUnits())
        {
            var unitData = new Godot.Collections.Dictionary
            {
                ["id"] = unit.Id,
                ["q"] = unit.Q,
                ["r"] = unit.R,
                ["player_id"] = unit.PlayerId,
                ["type"] = unit.UnitType.ToString()
            };
            data.Add(unitData);
        }

        return data;
    }

    /// <summary>
    /// Gets detailed unit panel data for a selected unit.
    /// </summary>
    public Godot.Collections.Dictionary GetUnitPanelData(int unitId)
    {
        var data = new Godot.Collections.Dictionary();

        if (!ServiceLocator.TryGet<IUnitManager>(out var unitManager))
        {
            return data;
        }

        var unit = unitManager.GetUnit(unitId);
        if (unit == null)
        {
            return data;
        }

        data["id"] = unit.Id;
        data["type"] = unit.UnitType.ToString();
        data["type_name"] = unit.UnitType.GetDisplayName();
        data["type_description"] = unit.UnitType.GetStats().Description;
        data["player_id"] = unit.PlayerId;

        // Position
        data["q"] = unit.Q;
        data["r"] = unit.R;

        // Stats
        data["health"] = unit.CurrentHealth;
        data["max_health"] = unit.MaxHealth;
        data["health_percent"] = unit.MaxHealth > 0 ? (float)unit.CurrentHealth / unit.MaxHealth : 0f;

        data["movement"] = unit.CurrentMovement;
        data["max_movement"] = unit.MaxMovement;
        data["movement_percent"] = unit.MaxMovement > 0 ? unit.CurrentMovement / unit.MaxMovement : 0f;

        data["attack"] = unit.Attack;
        data["defense"] = unit.Defense;

        // Domain
        data["domain"] = unit.UnitType.GetDomain().ToString();
        data["can_traverse_water"] = unit.UnitType.GetDomain() != UnitDomain.Land;
        data["can_traverse_land"] = unit.UnitType.GetDomain() != UnitDomain.Naval;

        return data;
    }

    /// <summary>
    /// Gets cell tooltip data.
    /// </summary>
    public Godot.Collections.Dictionary GetCellTooltipData(int q, int r)
    {
        var data = new Godot.Collections.Dictionary();

        if (!ServiceLocator.TryGet<HexGrid>(out var grid))
        {
            return data;
        }

        var cell = grid.GetCell(q, r);
        if (cell == null)
        {
            return data;
        }

        data["q"] = cell.Q;
        data["r"] = cell.R;
        data["coordinates"] = $"({cell.Q}, {cell.R})";

        // Terrain
        data["terrain"] = cell.TerrainType.ToString();
        data["terrain_name"] = cell.TerrainType.GetDisplayName();
        data["elevation"] = cell.Elevation;
        data["is_water"] = cell.IsWater;

        // Features
        data["has_river"] = cell.HasRiver;
        data["has_road"] = cell.HasRoad;
        data["feature_count"] = cell.Features.Count;

        // Movement cost hint
        data["base_movement_cost"] = GetBaseMovementCost(cell.TerrainType);

        // Unit on cell
        if (ServiceLocator.TryGet<IUnitManager>(out var unitManager))
        {
            var unitOnCell = unitManager.GetUnitAt(q, r);
            if (unitOnCell != null)
            {
                data["has_unit"] = true;
                data["unit_id"] = unitOnCell.Id;
                data["unit_type"] = unitOnCell.UnitType.ToString();
                data["unit_player"] = unitOnCell.PlayerId;
            }
            else
            {
                data["has_unit"] = false;
            }
        }

        return data;
    }

    /// <summary>
    /// Gets action button states (what actions are currently available).
    /// </summary>
    public Godot.Collections.Dictionary GetActionStates()
    {
        var states = new Godot.Collections.Dictionary();

        // Turn controls
        if (ServiceLocator.TryGet<TurnManager>(out var turnManager))
        {
            states["end_turn_enabled"] = turnManager.IsHumanTurn;
            states["can_move"] = turnManager.CanMove;
            states["can_attack"] = turnManager.CanAttack;
        }

        // Undo/Redo
        if (ServiceLocator.TryGet<Commands.CommandHistory>(out var history))
        {
            states["undo_enabled"] = history.CanUndo;
            states["redo_enabled"] = history.CanRedo;
            states["undo_description"] = history.NextUndoDescription ?? "";
            states["redo_description"] = history.NextRedoDescription ?? "";
        }

        // Game controls
        if (ServiceLocator.TryGet<GameStateMachine>(out var stateMachine))
        {
            states["pause_enabled"] = stateMachine.IsPlaying;
            states["resume_enabled"] = stateMachine.IsPaused;
        }

        return states;
    }

    /// <summary>
    /// Gets terrain legend data for UI display.
    /// </summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetTerrainLegend()
    {
        var legend = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        foreach (TerrainType terrain in Enum.GetValues<TerrainType>())
        {
            var color = terrain.GetColor();
            var entry = new Godot.Collections.Dictionary
            {
                ["type"] = (int)terrain,
                ["name"] = terrain.GetDisplayName(),
                ["color_r"] = color.R,
                ["color_g"] = color.G,
                ["color_b"] = color.B,
                ["movement_cost"] = GetBaseMovementCost(terrain),
                ["is_water"] = terrain.IsWater()
            };
            legend.Add(entry);
        }

        return legend;
    }

    private static float GetBaseMovementCost(TerrainType terrain)
    {
        return terrain switch
        {
            TerrainType.Ocean => GameConstants.Movement.OceanCost,
            TerrainType.Coast => GameConstants.Movement.CoastCost,
            TerrainType.Forest => GameConstants.Movement.ForestCost,
            TerrainType.Jungle => GameConstants.Movement.JungleCost,
            TerrainType.Hills => GameConstants.Movement.HillsCost,
            TerrainType.Mountains => float.PositiveInfinity,
            TerrainType.Snow => GameConstants.Movement.SnowCost,
            TerrainType.Swamp => GameConstants.Movement.MarshCost,
            _ => GameConstants.Movement.BaseCost
        };
    }
}
