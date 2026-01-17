using System.Text.Json.Serialization;
using HexGame.Core;
using HexGame.Units;

namespace HexGame.Persistence;

/// <summary>
/// Root save data container with all game state.
/// </summary>
public class SaveData
{
    /// <summary>
    /// Save file format version for compatibility checks.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Timestamp when the save was created.
    /// </summary>
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User-provided save name.
    /// </summary>
    public string SaveName { get; set; } = "Unnamed Save";

    /// <summary>
    /// Total playtime in seconds.
    /// </summary>
    public double PlaytimeSeconds { get; set; }

    /// <summary>
    /// Map/grid data.
    /// </summary>
    public MapSaveData Map { get; set; } = new();

    /// <summary>
    /// All units in the game.
    /// </summary>
    public List<UnitSaveData> Units { get; set; } = new();

    /// <summary>
    /// Turn and game state data.
    /// </summary>
    public GameStateSaveData GameState { get; set; } = new();

    /// <summary>
    /// Random seed used for generation (for reproducibility).
    /// </summary>
    public int MapSeed { get; set; }
}

/// <summary>
/// Save data for the hex grid map.
/// </summary>
public class MapSaveData
{
    /// <summary>
    /// Grid width in cells.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Grid height in cells.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// All cell data.
    /// </summary>
    public List<CellSaveData> Cells { get; set; } = new();
}

/// <summary>
/// Save data for a single hex cell.
/// </summary>
public class CellSaveData
{
    /// <summary>
    /// Axial Q coordinate.
    /// </summary>
    public int Q { get; set; }

    /// <summary>
    /// Axial R coordinate.
    /// </summary>
    public int R { get; set; }

    /// <summary>
    /// Elevation level.
    /// </summary>
    public int Elevation { get; set; }

    /// <summary>
    /// Terrain type as integer for serialization.
    /// </summary>
    public int TerrainType { get; set; }

    /// <summary>
    /// Moisture value.
    /// </summary>
    public float Moisture { get; set; }

    /// <summary>
    /// Temperature value.
    /// </summary>
    public float Temperature { get; set; }

    /// <summary>
    /// Whether the cell has a river.
    /// </summary>
    public bool HasRiver { get; set; }

    /// <summary>
    /// River flow directions.
    /// </summary>
    public List<int> RiverDirections { get; set; } = new();

    /// <summary>
    /// Whether the cell has a road.
    /// </summary>
    public bool HasRoad { get; set; }

    /// <summary>
    /// Features on this cell.
    /// </summary>
    public List<FeatureSaveData> Features { get; set; } = new();
}

/// <summary>
/// Save data for a feature (tree, rock, etc.).
/// </summary>
public class FeatureSaveData
{
    /// <summary>
    /// Feature type as integer.
    /// </summary>
    public int Type { get; set; }

    /// <summary>
    /// World X position.
    /// </summary>
    public float PositionX { get; set; }

    /// <summary>
    /// World Y position.
    /// </summary>
    public float PositionY { get; set; }

    /// <summary>
    /// World Z position.
    /// </summary>
    public float PositionZ { get; set; }

    /// <summary>
    /// Rotation in radians.
    /// </summary>
    public float Rotation { get; set; }

    /// <summary>
    /// Scale multiplier.
    /// </summary>
    public float Scale { get; set; }
}

/// <summary>
/// Save data for a unit.
/// </summary>
public class UnitSaveData
{
    /// <summary>
    /// Unique unit ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Unit type as integer.
    /// </summary>
    public int UnitType { get; set; }

    /// <summary>
    /// Owning player ID.
    /// </summary>
    public int PlayerId { get; set; }

    /// <summary>
    /// Q coordinate.
    /// </summary>
    public int Q { get; set; }

    /// <summary>
    /// R coordinate.
    /// </summary>
    public int R { get; set; }

    /// <summary>
    /// Current health.
    /// </summary>
    public int CurrentHealth { get; set; }

    /// <summary>
    /// Remaining movement points.
    /// </summary>
    public float CurrentMovement { get; set; }

    /// <summary>
    /// Whether the unit has acted this turn.
    /// </summary>
    public bool HasActed { get; set; }
}

/// <summary>
/// Save data for game state (turns, phase, etc.).
/// </summary>
public class GameStateSaveData
{
    /// <summary>
    /// Current turn number.
    /// </summary>
    public int CurrentTurn { get; set; } = 1;

    /// <summary>
    /// Current player ID.
    /// </summary>
    public int CurrentPlayer { get; set; } = 1;

    /// <summary>
    /// Current turn phase as string.
    /// </summary>
    public string CurrentPhase { get; set; } = "Movement";

    /// <summary>
    /// Total number of players.
    /// </summary>
    public int PlayerCount { get; set; } = 2;

    /// <summary>
    /// Current game state machine state name.
    /// </summary>
    public string StateMachineName { get; set; } = "Playing";

    /// <summary>
    /// Next unit ID to assign.
    /// </summary>
    public int NextUnitId { get; set; } = 1;
}

/// <summary>
/// Metadata for a save file (shown in load menu).
/// </summary>
public class SaveMetadata
{
    /// <summary>
    /// File path of the save.
    /// </summary>
    public string FilePath { get; set; } = "";

    /// <summary>
    /// User-provided save name.
    /// </summary>
    public string SaveName { get; set; } = "";

    /// <summary>
    /// When the save was created.
    /// </summary>
    public DateTime SavedAt { get; set; }

    /// <summary>
    /// Turn number at time of save.
    /// </summary>
    public int TurnNumber { get; set; }

    /// <summary>
    /// Total playtime.
    /// </summary>
    public TimeSpan Playtime { get; set; }

    /// <summary>
    /// Save format version.
    /// </summary>
    public int Version { get; set; }
}
