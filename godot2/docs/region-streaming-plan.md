# Region Streaming System - Implementation Plan

**Created:** 2026-01-30
**Status:** Ready for Implementation
**GitHub Issue:** #117

---

## Executive Summary

This plan details the implementation of an "Ocean Boundaries" Region Streaming System. Each region is a self-contained area (~200x200 hexes) naturally separated by ocean, with explicit travel transitions between regions. Only one region (~300 MB) is loaded at a time.

**Design Goal:** Keep terminology generic ("Region" not "Island") so this engine can be reused for other projects.

---

## 1. Architecture Overview

### 1.1 Core Design Principles

- **Generic Naming**: Use "Region" throughout for engine reusability
- **Ocean Boundaries**: Natural ocean separation eliminates boundary stitching complexity
- **Single Region Loading**: Only ONE region in memory at a time (~300 MB budget)
- **Explicit Transitions**: Players consciously travel between regions via UI, not seamless streaming
- **Async Patterns**: Follow existing MapGenerator async/progress patterns

### 1.2 Component Overview

```
+-------------------+     +------------------+     +---------------------+
|  RegionSerializer |<--->|  RegionManager   |<--->|   RegionMapUI       |
|  (Save/Load)      |     |  (Orchestrator)  |     |   (Strategic View)  |
+-------------------+     +------------------+     +---------------------+
                                  ^
                                  |
                          +------------------+
                          | RegionTravelUI   |
                          | (Transition UX)  |
                          +------------------+
```

### 1.3 Data Flow

```
1. Save Region:
   HexGrid cells -> CellData[] -> RegionSerializer.Save() -> .region file

2. Load Region:
   .region file -> RegionSerializer.Load() -> CellData[] -> Apply to HexGrid

3. Travel Between Regions:
   Current Region -> Unload -> RegionTravelUI (transition) -> Load New -> Apply
```

---

## 2. Component 1: Region Serializer

### 2.1 File Format Design

**File Extension**: `.region`

**Binary Format Structure**:
```
[Header: 32 bytes]
  - Magic Number: 4 bytes ("HXRG")
  - Version: 4 bytes (uint32)
  - RegionID: 16 bytes (GUID)
  - Width: 4 bytes (int32)
  - Height: 4 bytes (int32)

[Metadata: Variable]
  - RegionName: Length-prefixed UTF-8 string
  - Seed: 4 bytes (int32)
  - GenerationTimestamp: 8 bytes (long)
  - ConnectionCount: 4 bytes (int32)
  - Connections: ConnectionCount * ConnectionRecord

[Cell Data: Width * Height * CellDataSize]
  - CellData records (packed binary)
```

**PackedCellData Binary Layout** (16 bytes per cell):
```csharp
public struct PackedCellData
{
    public short X;                // 2 bytes
    public short Z;                // 2 bytes
    public sbyte Elevation;        // 1 byte
    public sbyte WaterLevel;       // 1 byte
    public byte TerrainTypeIndex;  // 1 byte
    public byte FeatureFlags;      // 1 byte (UrbanLevel:2, FarmLevel:2, PlantLevel:2, Walled:1, Unused:1)
    public byte RiverFlags;        // 1 byte (HasIn:1, HasOut:1, InDir:3, OutDir:3)
    public byte RoadFlags;         // 1 byte (6 direction bits)
    public byte SpecialIndex;      // 1 byte
    public byte Reserved;          // 1 byte
    public ushort MoisturePacked;  // 2 bytes (half-precision float)
    public ushort Padding;         // 2 bytes alignment
}
```

**Size Estimation** (200x200 region):
- Cells: 40,000 × 16 bytes = 640 KB
- Metadata: ~10 KB
- **Total: ~650 KB per region file**

### 2.2 Class Design

```csharp
// File: src/Region/RegionSerializer.cs
namespace HexGame.Region;

public class RegionSerializer
{
    public const string MagicNumber = "HXRG";
    public const uint CurrentVersion = 1;

    public event Action<string, float>? SaveProgress;
    public event Action<string, float>? LoadProgress;

    public Task<bool> SaveAsync(RegionData region, string path, CancellationToken ct = default);
    public Task<RegionData?> LoadAsync(string path, CancellationToken ct = default);
    public Task<RegionMetadata?> LoadMetadataAsync(string path);  // Fast header-only read
}
```

```csharp
// File: src/Region/RegionData.cs
namespace HexGame.Region;

public class RegionData
{
    public Guid RegionId { get; set; }
    public string Name { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
    public int Seed { get; set; }
    public DateTime GeneratedAt { get; set; }
    public CellData[] Cells { get; set; } = Array.Empty<CellData>();
    public List<RegionConnection> Connections { get; set; } = new();
}

public class RegionConnection
{
    public Guid TargetRegionId { get; set; }
    public string TargetRegionName { get; set; } = "";
    public int DeparturePortIndex { get; set; }
    public int ArrivalPortIndex { get; set; }
    public float TravelTimeMinutes { get; set; }
}
```

---

## 3. Component 2: Region Map System

### 3.1 World-Level Data

```csharp
// File: src/Region/RegionMap.cs
namespace HexGame.Region;

public class RegionMap
{
    public string WorldName { get; set; } = "New World";
    public Guid CurrentRegionId { get; set; }
    public List<RegionMapEntry> Regions { get; set; } = new();

    public RegionMapEntry? GetCurrentRegion();
    public RegionMapEntry? GetRegionById(Guid id);
    public IEnumerable<RegionMapEntry> GetConnectedRegions(Guid fromRegionId);
}

public class RegionMapEntry
{
    public Guid RegionId { get; set; }
    public string Name { get; set; } = "";
    public Vector2 MapPosition { get; set; }
    public string FilePath { get; set; } = "";
    public bool IsDiscovered { get; set; }
    public RegionBiome PrimaryBiome { get; set; }
    public List<Guid> ConnectedRegions { get; set; } = new();
}

public enum RegionBiome { Temperate, Tropical, Arctic, Desert, Volcanic }
```

### 3.2 Region Map UI

```
+------------------------------------------------------------------+
|  WORLD MAP                                           [X] Close   |
+------------------------------------------------------------------+
|                                                                   |
|         [Desert Isle]                                             |
|              O---------O [Coral Atoll]                           |
|              |          \                                         |
|         [*Current*]       O [Volcanic Peak]                      |
|          Iron Coast        |                                      |
|              |             |                                      |
|              O-------------O                                      |
|         [Frost Haven]   [Jade Archipelago]                       |
|                                                                   |
+----------------------------+--------------------------------------+
| SELECTED: Volcanic Peak   |  Travel Time: ~15 minutes           |
| Biome: Volcanic           |  [SET SAIL]                          |
+----------------------------+--------------------------------------+
```

---

## 4. Component 3: Region Travel Screen

### 4.1 Travel Transition UX

```csharp
// File: src/UI/RegionTravelUI.cs
namespace HexGame.UI;

public partial class RegionTravelUI : Control
{
    [Signal] public delegate void TravelCancelledEventHandler();
    [Signal] public delegate void TravelCompletedEventHandler();

    public async Task ShowTravelAsync(
        RegionMapEntry fromRegion,
        RegionMapEntry toRegion,
        Func<Task<bool>> loadRegionTask);

    public void UpdateLoadProgress(string stage, float progress);
}
```

### 4.2 Visual Design

```
+------------------------------------------------------------------+
|     [Animated ocean waves - parallax layers]                      |
|                                                                   |
|                    [Ship sprite sailing]                          |
|                                                                   |
|     "The northern winds carry you south,                          |
|      toward warmer waters..."                                     |
|                                                                   |
+------------------------------------------------------------------+
|     [====================>         ] 67%                          |
|     Loading terrain...                         [Cancel Voyage]   |
+------------------------------------------------------------------+
```

---

## 5. Region Manager (Orchestrator)

```csharp
// File: src/Region/RegionManager.cs
namespace HexGame.Region;

public partial class RegionManager : Node
{
    public static RegionManager? Instance { get; private set; }

    [Signal] public delegate void RegionLoadingEventHandler(string regionId);
    [Signal] public delegate void RegionLoadedEventHandler(string regionId);
    [Signal] public delegate void RegionUnloadedEventHandler(string regionId);

    public RegionData? CurrentRegion { get; }
    public bool IsLoading { get; }

    public Task<RegionData> GenerateNewRegionAsync(string name, int width, int height, int seed, CancellationToken ct = default);
    public Task<bool> SaveCurrentRegionAsync();
    public Task<bool> LoadRegionAsync(Guid regionId);
    public Task<bool> TravelToRegionAsync(Guid destinationId);
}
```

---

## 6. Configuration

```csharp
// File: src/Region/RegionConfig.cs
namespace HexGame.Region;

public static class RegionConfig
{
    // Region Size
    public const int DefaultRegionWidth = 200;
    public const int DefaultRegionHeight = 200;
    public const int MinRegionSize = 50;
    public const int MaxRegionSize = 300;

    // File Format
    public const string FileMagicNumber = "HXRG";
    public const uint FileVersion = 1;
    public const string FileExtension = ".region";
    public const string RegionsDirectory = "regions";

    // Travel
    public const float MinTravelTimeSeconds = 3.0f;
    public const float TravelTimePerUnit = 0.5f;

    // Memory
    public const int EstimatedBytesPerCell = 500;
    public const int TargetMemoryBudgetMB = 300;
}
```

---

## 7. Implementation Phases

### Phase 1: Core Serialization (Week 1)

**Goal**: Save and load regions to/from disk.

**Files to Create**:
- `src/Region/RegionConfig.cs`
- `src/Region/RegionData.cs`
- `src/Region/RegionSerializer.cs`
- `tests/Region/RegionSerializerTests.cs`

**Tests**:
- Round-trip serialization preserves all cell data
- Metadata-only load is faster than full load
- Handles corrupted files gracefully

### Phase 2: Region Manager (Week 2)

**Goal**: Coordinate region lifecycle and grid integration.

**Files to Create**:
- `src/Region/RegionManager.cs`

**Files to Modify**:
- `src/HexGrid.cs` (add resize support if needed)

**Tests**:
- LoadRegion applies all cell properties correctly
- Unload clears grid state
- Generate wraps MapGenerator correctly

### Phase 3: Region Map System (Week 3)

**Goal**: Strategic world map for navigation.

**Files to Create**:
- `src/Region/RegionMap.cs`
- `src/UI/RegionMapUI.cs`
- `src/UI/RegionIcon.cs`

**Tests**:
- GetConnectedRegions returns valid paths
- Discovery/fog-of-war persists correctly

### Phase 4: Travel Transition UI (Week 4)

**Goal**: Engaging transition during region loading.

**Files to Create**:
- `src/UI/RegionTravelUI.cs`
- `assets/travel/` (art assets)

**Tests**:
- Cancel correctly aborts loading
- Progress updates display correctly
- Minimum travel time enforced

### Phase 5: Integration & Polish (Week 5)

**Goal**: Full end-to-end functionality.

**Tasks**:
- Add keybinds (M for map)
- Integrate with GameUI
- Auto-save on region exit
- Memory profiling
- Error handling
- Documentation

---

## 8. Integration Points

| Existing System | Integration | Notes |
|-----------------|-------------|-------|
| `MapGenerator` | Wrap for region generation | Use existing async patterns |
| `CellData` | Serialize directly | Already contains all cell state |
| `HexGrid` | Apply loaded regions | Use `SetChunkRefreshSuppression` |
| `GameUI` | Add region controls | Follow signal patterns |
| `FileAccess` | Binary I/O | Follow ScreenshotCamera pattern |

---

## 9. Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Memory spikes during transition | Medium | High | Unload before loading, force GC |
| Grid resize complexity | Medium | Medium | Fixed-size regions initially |
| File corruption | Low | High | Checksums, recovery mode |
| Long load times | Medium | Medium | Progress UI, optimize packing |
| Format evolution | Medium | Low | Version header for migration |

---

## 10. File Structure

```
src/
├── Region/
│   ├── RegionConfig.cs
│   ├── RegionData.cs
│   ├── RegionSerializer.cs
│   ├── RegionManager.cs
│   └── RegionMap.cs
└── UI/
    ├── RegionMapUI.cs
    ├── RegionIcon.cs
    └── RegionTravelUI.cs

tests/
└── Region/
    ├── RegionSerializerTests.cs
    ├── RegionManagerTests.cs
    └── RegionMapTests.cs
```

---

## 11. Reference Files

| File | Why It Matters |
|------|----------------|
| `src/Generation/LandGenerator.cs` | `CellData` struct to serialize |
| `src/Generation/MapGenerator.cs` | Async patterns, `ApplyGeneratedDataToGrid()` |
| `src/HexGrid.cs` | Grid management, chunk suppression |
| `src/UI/GameUI.cs` | UI patterns (signals, folders) |
| `src/ScreenshotCamera.cs` | File I/O with `Godot.FileAccess` |
