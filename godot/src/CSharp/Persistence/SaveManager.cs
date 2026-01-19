using System.Text.Json;
using HexGame.Core;
using HexGame.Events;
using HexGame.GameState;
using HexGame.Units;
using GodotFileAccess = Godot.FileAccess;

namespace HexGame.Persistence;

/// <summary>
/// Manages saving and loading game state.
/// </summary>
public class SaveManager : IService
{
    private const string SaveDirectory = "user://saves/";
    private const string SaveExtension = ".hexsave";
    private const string MetadataExtension = ".meta";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly EventBus _eventBus;
    private double _sessionStartTime;
    private double _totalPlaytime;

    /// <summary>
    /// Current save file version.
    /// </summary>
    public const int CurrentVersion = 1;

    /// <summary>
    /// Event fired when save completes.
    /// </summary>
    public event Action<string, bool>? SaveCompleted;

    /// <summary>
    /// Event fired when load completes.
    /// </summary>
    public event Action<string, bool>? LoadCompleted;

    public SaveManager(EventBus eventBus)
    {
        _eventBus = eventBus;
    }

    #region IService Implementation

    public void Initialize()
    {
        EnsureSaveDirectoryExists();
        _sessionStartTime = Time.GetTicksMsec() / 1000.0;
    }

    public void Shutdown()
    {
        SaveCompleted = null;
        LoadCompleted = null;
    }

    #endregion

    #region Save Operations

    /// <summary>
    /// Saves the current game state.
    /// </summary>
    public bool Save(string saveName)
    {
        try
        {
            var saveData = CreateSaveData(saveName);
            var fileName = GenerateFileName(saveName);
            var filePath = SaveDirectory + fileName + SaveExtension;

            // Serialize to JSON
            var json = JsonSerializer.Serialize(saveData, JsonOptions);

            // Write to file using Godot's FileAccess
            using var file = GodotFileAccess.Open(filePath, GodotFileAccess.ModeFlags.Write);
            if (file == null)
            {
                GD.PrintErr($"SaveManager: Failed to open file for writing: {filePath}");
                SaveCompleted?.Invoke(saveName, false);
                return false;
            }

            file.StoreString(json);

            // Save metadata separately for quick listing
            SaveMetadata(saveData, filePath);

            GD.Print($"SaveManager: Game saved to {filePath}");
            SaveCompleted?.Invoke(saveName, true);
            _eventBus.Publish(new GameSavedEvent(saveName, filePath));

            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"SaveManager: Save failed - {ex.Message}");
            SaveCompleted?.Invoke(saveName, false);
            return false;
        }
    }

    /// <summary>
    /// Quick save to default slot.
    /// </summary>
    public bool QuickSave()
    {
        return Save("QuickSave");
    }

    /// <summary>
    /// Auto save with timestamp.
    /// </summary>
    public bool AutoSave()
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
        return Save($"AutoSave_{timestamp}");
    }

    private SaveData CreateSaveData(string saveName)
    {
        var saveData = new SaveData
        {
            SaveName = saveName,
            SavedAt = DateTime.UtcNow,
            PlaytimeSeconds = GetTotalPlaytime(),
            Version = CurrentVersion
        };

        // Serialize map
        if (ServiceLocator.TryGet<HexGrid>(out var grid))
        {
            saveData.Map = SerializeMap(grid);
        }

        // Serialize units
        if (ServiceLocator.TryGet<IUnitManager>(out var unitManager))
        {
            saveData.Units = SerializeUnits(unitManager);
        }

        // Serialize game state
        saveData.GameState = SerializeGameState();

        return saveData;
    }

    private static MapSaveData SerializeMap(HexGrid grid)
    {
        var mapData = new MapSaveData
        {
            Width = grid.Width,
            Height = grid.Height
        };

        foreach (var cell in grid.GetAllCells())
        {
            var cellData = new CellSaveData
            {
                Q = cell.Q,
                R = cell.R,
                Elevation = cell.Elevation,
                TerrainType = (int)cell.TerrainType,
                Moisture = cell.Moisture,
                Temperature = cell.Temperature,
                HasRiver = cell.HasRiver,
                HasRoad = cell.HasRoad
            };

            cellData.RiverDirections.AddRange(cell.RiverDirections);

            foreach (var feature in cell.Features)
            {
                cellData.Features.Add(new FeatureSaveData
                {
                    Type = (int)feature.Type,
                    PositionX = feature.Position.X,
                    PositionY = feature.Position.Y,
                    PositionZ = feature.Position.Z,
                    Rotation = feature.Rotation,
                    Scale = feature.Scale
                });
            }

            mapData.Cells.Add(cellData);
        }

        return mapData;
    }

    private static List<UnitSaveData> SerializeUnits(IUnitManager unitManager)
    {
        var units = new List<UnitSaveData>();

        foreach (var unit in unitManager.GetAllUnits())
        {
            units.Add(new UnitSaveData
            {
                Id = unit.Id,
                UnitType = (int)unit.UnitType,
                PlayerId = unit.PlayerId,
                Q = unit.Q,
                R = unit.R,
                CurrentHealth = unit.CurrentHealth,
                CurrentMovement = unit.CurrentMovement,
                HasActed = unit.HasActed
            });
        }

        return units;
    }

    private static GameStateSaveData SerializeGameState()
    {
        var stateData = new GameStateSaveData();

        if (ServiceLocator.TryGet<TurnManager>(out var turnManager))
        {
            stateData.CurrentTurn = turnManager.CurrentTurn;
            stateData.CurrentPlayer = turnManager.CurrentPlayer;
            stateData.CurrentPhase = turnManager.CurrentPhase.ToString();
            stateData.PlayerCount = turnManager.PlayerCount;
        }

        if (ServiceLocator.TryGet<GameStateMachine>(out var stateMachine))
        {
            stateData.StateMachineName = stateMachine.CurrentState.ToString();
        }

        return stateData;
    }

    private void SaveMetadata(SaveData saveData, string savePath)
    {
        var metaPath = savePath.Replace(SaveExtension, MetadataExtension);
        var metadata = new SaveMetadata
        {
            FilePath = savePath,
            SaveName = saveData.SaveName,
            SavedAt = saveData.SavedAt,
            TurnNumber = saveData.GameState.CurrentTurn,
            Playtime = TimeSpan.FromSeconds(saveData.PlaytimeSeconds),
            Version = saveData.Version
        };

        var json = JsonSerializer.Serialize(metadata, JsonOptions);

        using var file = GodotFileAccess.Open(metaPath, GodotFileAccess.ModeFlags.Write);
        file?.StoreString(json);
    }

    #endregion

    #region Load Operations

    /// <summary>
    /// Loads a game from the specified file path.
    /// </summary>
    public bool Load(string filePath)
    {
        try
        {
            if (!GodotFileAccess.FileExists(filePath))
            {
                GD.PrintErr($"SaveManager: Save file not found: {filePath}");
                LoadCompleted?.Invoke(filePath, false);
                return false;
            }

            using var file = GodotFileAccess.Open(filePath, GodotFileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PrintErr($"SaveManager: Failed to open file: {filePath}");
                LoadCompleted?.Invoke(filePath, false);
                return false;
            }

            var json = file.GetAsText();
            var saveData = JsonSerializer.Deserialize<SaveData>(json, JsonOptions);

            if (saveData == null)
            {
                GD.PrintErr("SaveManager: Failed to deserialize save data");
                LoadCompleted?.Invoke(filePath, false);
                return false;
            }

            // Version check
            if (saveData.Version > CurrentVersion)
            {
                GD.PrintErr($"SaveManager: Save version {saveData.Version} is newer than supported version {CurrentVersion}");
                LoadCompleted?.Invoke(filePath, false);
                return false;
            }

            // Apply save data
            ApplySaveData(saveData);

            _totalPlaytime = saveData.PlaytimeSeconds;
            _sessionStartTime = Time.GetTicksMsec() / 1000.0;

            GD.Print($"SaveManager: Game loaded from {filePath}");
            LoadCompleted?.Invoke(filePath, true);
            _eventBus.Publish(new GameLoadedEvent(saveData.SaveName, filePath));

            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"SaveManager: Load failed - {ex.Message}");
            LoadCompleted?.Invoke(filePath, false);
            return false;
        }
    }

    /// <summary>
    /// Quick load from default slot.
    /// </summary>
    public bool QuickLoad()
    {
        var quickSavePath = FindQuickSave();
        if (quickSavePath != null)
        {
            return Load(quickSavePath);
        }

        GD.PrintErr("SaveManager: No quick save found");
        return false;
    }

    private void ApplySaveData(SaveData saveData)
    {
        // Clear existing state
        if (ServiceLocator.TryGet<IUnitManager>(out var unitManager))
        {
            unitManager.Clear();
        }

        // Load map
        if (ServiceLocator.TryGet<HexGrid>(out var grid))
        {
            DeserializeMap(saveData.Map, grid);
        }

        // Load units
        if (unitManager != null)
        {
            DeserializeUnits(saveData.Units, unitManager);
        }

        // Load game state
        DeserializeGameState(saveData.GameState);
    }

    private static void DeserializeMap(MapSaveData mapData, HexGrid grid)
    {
        // Resize grid if needed
        if (grid.Width != mapData.Width || grid.Height != mapData.Height)
        {
            grid.Resize(mapData.Width, mapData.Height);
        }

        foreach (var cellData in mapData.Cells)
        {
            var cell = grid.GetCell(cellData.Q, cellData.R);
            if (cell == null) continue;

            cell.Elevation = cellData.Elevation;
            cell.TerrainType = (TerrainType)cellData.TerrainType;
            cell.Moisture = cellData.Moisture;
            cell.Temperature = cellData.Temperature;
            cell.HasRiver = cellData.HasRiver;
            cell.HasRoad = cellData.HasRoad;

            cell.RiverDirections.Clear();
            cell.RiverDirections.AddRange(cellData.RiverDirections);

            cell.Features.Clear();
            foreach (var featureData in cellData.Features)
            {
                cell.Features.Add(new Feature(
                    (Feature.FeatureType)featureData.Type,
                    new Vector3(featureData.PositionX, featureData.PositionY, featureData.PositionZ),
                    featureData.Rotation,
                    featureData.Scale
                ));
            }
        }
    }

    private static void DeserializeUnits(List<UnitSaveData> unitsData, IUnitManager unitManager)
    {
        foreach (var unitData in unitsData)
        {
            var unit = unitManager.CreateUnit(
                (UnitType)unitData.UnitType,
                unitData.Q,
                unitData.R,
                unitData.PlayerId
            );

            if (unit != null)
            {
                unit.CurrentHealth = unitData.CurrentHealth;
                unit.CurrentMovement = unitData.CurrentMovement;
                unit.HasActed = unitData.HasActed;
            }
        }
    }

    private static void DeserializeGameState(GameStateSaveData stateData)
    {
        if (ServiceLocator.TryGet<TurnManager>(out var turnManager))
        {
            // Use reflection or direct property setting if available
            // For now, we'd need to add setter methods to TurnManager
            turnManager.PlayerCount = stateData.PlayerCount;
            // turnManager.SetTurnState(stateData.CurrentTurn, stateData.CurrentPlayer, ...);
        }
    }

    #endregion

    #region File Management

    /// <summary>
    /// Gets a list of all available saves.
    /// </summary>
    public List<SaveMetadata> GetSaveList()
    {
        var saves = new List<SaveMetadata>();

        var dir = DirAccess.Open(SaveDirectory);
        if (dir == null)
        {
            return saves;
        }

        dir.ListDirBegin();
        var fileName = dir.GetNext();

        while (!string.IsNullOrEmpty(fileName))
        {
            if (fileName.EndsWith(MetadataExtension))
            {
                var metaPath = SaveDirectory + fileName;
                var metadata = LoadMetadata(metaPath);
                if (metadata != null)
                {
                    saves.Add(metadata);
                }
            }
            fileName = dir.GetNext();
        }

        dir.ListDirEnd();

        // Sort by date, newest first
        saves.Sort((a, b) => b.SavedAt.CompareTo(a.SavedAt));

        return saves;
    }

    /// <summary>
    /// Deletes a save file.
    /// </summary>
    public bool DeleteSave(string filePath)
    {
        try
        {
            var metaPath = filePath.Replace(SaveExtension, MetadataExtension);

            if (GodotFileAccess.FileExists(filePath))
            {
                DirAccess.RemoveAbsolute(filePath);
            }

            if (GodotFileAccess.FileExists(metaPath))
            {
                DirAccess.RemoveAbsolute(metaPath);
            }

            GD.Print($"SaveManager: Deleted save {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"SaveManager: Delete failed - {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Checks if a quick save exists.
    /// </summary>
    public bool HasQuickSave()
    {
        return FindQuickSave() != null;
    }

    private string? FindQuickSave()
    {
        var saves = GetSaveList();
        var quickSave = saves.FirstOrDefault(s => s.SaveName == "QuickSave");
        return quickSave?.FilePath;
    }

    private SaveMetadata? LoadMetadata(string metaPath)
    {
        try
        {
            using var file = GodotFileAccess.Open(metaPath, GodotFileAccess.ModeFlags.Read);
            if (file == null) return null;

            var json = file.GetAsText();
            return JsonSerializer.Deserialize<SaveMetadata>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static void EnsureSaveDirectoryExists()
    {
        if (!DirAccess.DirExistsAbsolute(SaveDirectory))
        {
            DirAccess.MakeDirRecursiveAbsolute(SaveDirectory);
        }
    }

    private static string GenerateFileName(string saveName)
    {
        // Sanitize the save name for use as a file name
        var sanitized = saveName
            .Replace(" ", "_")
            .Replace("/", "_")
            .Replace("\\", "_")
            .Replace(":", "_");

        return sanitized;
    }

    private double GetTotalPlaytime()
    {
        var currentSessionTime = (Time.GetTicksMsec() / 1000.0) - _sessionStartTime;
        return _totalPlaytime + currentSessionTime;
    }

    #endregion
}

#region Events

/// <summary>
/// Event fired when game is saved.
/// </summary>
public record GameSavedEvent(string SaveName, string FilePath) : GameEventBase;

/// <summary>
/// Event fired when game is loaded.
/// </summary>
public record GameLoadedEvent(string SaveName, string FilePath) : GameEventBase;

#endregion
