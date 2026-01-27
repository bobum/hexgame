# Procedural Map Generation Plan

## Overview

Implement procedural map generation triggered by spacebar, following techniques from Catlike Coding tutorials 23-26 and proven patterns from the old implementation at `C:\Temp\godot`. The current test map should load initially, then be replaced by procedural generation on demand.

## Requirements

1. **Spacebar triggers regeneration** with a new random seed
2. **G key regenerates** with the same seed (for debugging)
3. **Test map loads initially** - existing HexMapEditor functionality remains
4. **Progressive generation** with visual feedback during generation
5. **Clean architecture** that integrates with existing chunk-based rendering

## Architecture Analysis

### Current Codebase (godot2)

| Component | File | Status |
|-----------|------|--------|
| Grid data | `HexGrid.cs`, `HexCell.cs` | ✅ Complete with rivers, roads, features |
| Mesh building | `HexMesh.cs`, `HexGridChunk.cs` | ✅ Chunk-based rendering |
| Feature placement | `HexFeatureManager.cs`, `HexHash.cs` | ✅ Hash-based deterministic placement |
| Pathfinding | `HexCellPriorityQueue.cs`, `HexGrid.cs` | ✅ A* with movement costs |
| Test generators | `Test*Generator.cs` | ✅ Various test patterns |

### Old Implementation (C:\Temp\godot)

| Component | File | Key Features |
|-----------|------|--------------|
| Map generator | `MapGenerator.cs` | FastNoiseLite, sync/async, pipeline architecture |
| River generator | `RiverGenerator.cs` | Steepest descent, budget-based, weighted selection |
| Feature generator | `FeatureGenerator.cs` | Biome-based chances, random placement |
| Constants | `GameConstants.cs` | Centralized tuning parameters |
| Main entry | `Main.cs` | Spacebar handler, regeneration workflow |

### Catlike Coding Tutorials

| Tutorial | Key Concepts |
|----------|--------------|
| Part 23 | Chunk-based land raising/sinking, map budget system |
| Part 24 | Multiple regions, erosion algorithm, region boundaries |
| Part 25 | Water cycle simulation (evaporation, precipitation, moisture) |
| Part 26 | Rivers (steepest descent + drainage), temperature, biome assignment |

## Implementation Strategy

We will create a **hybrid approach**:
- **Land generation**: Catlike-style chunk budget system for natural coastlines
- **Climate**: Simplified moisture system (noise-based like old impl, not full water cycle)
- **Rivers**: Catlike-style drainage with old impl's steepest descent
- **Features**: Existing HexFeatureManager with biome-based density

### Why This Hybrid?

1. **Catlike chunk budget** produces better coastlines than pure noise
2. **Full water cycle** is overkill - noise-based moisture is sufficient
3. **Old river generator** is proven and simpler than Catlike's drainage graph
4. **Existing feature system** is already well-integrated

## File Structure

```
godot2/src/
├── Generation/                    # NEW - Generation namespace
│   ├── IMapGenerator.cs          # Interface for generators
│   ├── MapGenerator.cs           # Main orchestrator
│   ├── LandGenerator.cs          # Catlike-style chunk budget
│   ├── ClimateGenerator.cs       # Moisture/temperature
│   ├── RiverGenerator.cs         # Steepest descent rivers
│   ├── FeaturePlacement.cs       # Biome-based feature density
│   └── GenerationConfig.cs       # Centralized parameters
├── HexMapEditor.cs               # MODIFY - Add spacebar handler
└── (existing files)
```

## Detailed Implementation Plan

### Phase 1: Infrastructure (Foundation)

#### 1.1 Create Generation Namespace and Interfaces

**File: `src/Generation/IMapGenerator.cs`**
```csharp
public interface IMapGenerator
{
    event Action? GenerationStarted;
    event Action<string, float>? GenerationProgress;
    event Action<bool>? GenerationCompleted;

    bool IsGenerating { get; }
    void Generate(HexGrid grid, int seed);
    void GenerateAsync(HexGrid grid, int seed);
    bool IsGenerationComplete();
    void FinishAsyncGeneration();
    void CancelGeneration();
}
```

**File: `src/Generation/GenerationConfig.cs`**
```csharp
public static class GenerationConfig
{
    // Land generation
    public const int LandBudgetMultiplier = 5;  // % of cells to raise
    public const int ChunkSize = 4;             // Land growth chunk size
    public const int MaxChunks = 10000;         // Safety limit

    // Moisture
    public const float MoistureNoiseScale = 0.03f;
    public const int MoistureSeedOffset = 1000;

    // Rivers
    public const float RiverPercentage = 0.05f;
    public const int MinRiverLength = 3;
    public const int RiverSeedOffset = 7777;

    // Elevation
    public const int MinElevation = -2;
    public const int MaxElevation = 8;
    public const int WaterLevel = 1;
}
```

#### 1.2 Create MapGenerator Orchestrator

**File: `src/Generation/MapGenerator.cs`**

Pipeline stages:
1. Reset all cells to underwater
2. Generate land using chunk budget
3. Generate moisture using noise
4. Assign terrain types based on elevation + moisture
5. Generate rivers
6. Clear/regenerate roads (optional)
7. Update feature placement via HexFeatureManager

### Phase 2: Land Generation (Catlike-style)

#### 2.1 Chunk-Based Land Raising

Following Catlike Part 23's approach:

```csharp
public class LandGenerator
{
    private HexGrid _grid;
    private Random _rng;

    public void Generate(int seed, float landPercentage)
    {
        _rng = new Random(seed);

        int landBudget = (int)(_grid.CellCount * landPercentage);

        while (landBudget > 0)
        {
            // Pick random starting cell
            var cell = GetRandomCell();

            // Grow a chunk of land from here
            int chunkSize = _rng.Next(GenerationConfig.MinChunkSize,
                                       GenerationConfig.MaxChunkSize);
            int raised = RaiseLandChunk(cell, chunkSize);
            landBudget -= raised;
        }
    }

    private int RaiseLandChunk(HexCell center, int budget)
    {
        var queue = new Queue<HexCell>();
        var processed = new HashSet<HexCell>();
        queue.Enqueue(center);
        int raised = 0;

        while (queue.Count > 0 && raised < budget)
        {
            var cell = queue.Dequeue();
            if (processed.Contains(cell)) continue;
            processed.Add(cell);

            // Raise this cell
            if (cell.Elevation < GenerationConfig.WaterLevel)
            {
                cell.Elevation = GenerationConfig.WaterLevel;
                raised++;
            }
            else if (_rng.NextDouble() < 0.3)
            {
                cell.Elevation++;  // Sometimes raise further
            }

            // Add neighbors with decreasing probability
            for (int d = 0; d < 6; d++)
            {
                var neighbor = cell.GetNeighbor((HexDirection)d);
                if (neighbor != null && !processed.Contains(neighbor))
                {
                    if (_rng.NextDouble() < 0.7)  // Expansion chance
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        return raised;
    }
}
```

#### 2.2 Elevation Refinement

After initial land raising:
- Apply erosion pass (lower isolated high cells)
- Smooth elevation transitions
- Create coastal variation

### Phase 3: Climate System

#### 3.1 Moisture Generation

Using FastNoiseLite (from old implementation):

```csharp
public class ClimateGenerator
{
    public void GenerateMoisture(HexGrid grid, int seed)
    {
        var noise = new FastNoiseLite();
        noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        noise.Seed = seed + GenerationConfig.MoistureSeedOffset;
        noise.Frequency = GenerationConfig.MoistureNoiseScale;

        foreach (var cell in grid.GetAllCells())
        {
            var pos = cell.Position;
            float moisture = (noise.GetNoise2D(pos.X, pos.Z) + 1f) / 2f;

            // Boost moisture near water
            if (IsNearWater(cell))
            {
                moisture = Mathf.Min(1f, moisture + 0.2f);
            }

            cell.Moisture = moisture;
        }
    }

    private bool IsNearWater(HexCell cell)
    {
        for (int d = 0; d < 6; d++)
        {
            var neighbor = cell.GetNeighbor((HexDirection)d);
            if (neighbor != null && neighbor.IsUnderwater)
                return true;
        }
        return false;
    }
}
```

#### 3.2 Temperature (Optional - Latitude-based)

```csharp
public void GenerateTemperature(HexGrid grid)
{
    float centerZ = grid.Height / 2f;

    foreach (var cell in grid.GetAllCells())
    {
        // Temperature decreases toward poles (top/bottom)
        float latitude = Mathf.Abs(cell.Coordinates.Z - centerZ) / centerZ;
        float baseTemp = 1f - latitude;

        // Reduce temperature at high elevation
        baseTemp -= cell.Elevation * 0.05f;

        cell.Temperature = Mathf.Clamp(baseTemp, 0f, 1f);
    }
}
```

#### 3.3 Biome Assignment

```csharp
public TerrainType DetermineBiome(HexCell cell)
{
    if (cell.IsUnderwater)
    {
        return cell.Elevation < GenerationConfig.WaterLevel - 2
            ? TerrainType.Ocean
            : TerrainType.Coast;
    }

    int heightAboveWater = cell.Elevation - GenerationConfig.WaterLevel;
    float moisture = cell.Moisture;

    // High elevation biomes
    if (heightAboveWater >= 6) return TerrainType.Snow;
    if (heightAboveWater >= 4) return TerrainType.Mountains;

    // Moisture-based biomes
    if (moisture < 0.2f) return TerrainType.Desert;
    if (moisture < 0.4f) return heightAboveWater >= 2 ? TerrainType.Hills : TerrainType.Savanna;
    if (moisture < 0.6f) return TerrainType.Plains;
    if (moisture < 0.8f) return TerrainType.Forest;
    return TerrainType.Jungle;
}
```

### Phase 4: River Generation

#### 4.1 River Source Selection

Following old implementation's weighted selection:

```csharp
public class RiverGenerator
{
    public void Generate(HexGrid grid, int seed, float riverPercentage)
    {
        _rng = new Random(seed + GenerationConfig.RiverSeedOffset);

        // Find valid river sources (high elevation + high moisture)
        var sources = FindRiverSources(grid);

        // Calculate river budget
        int riverBudget = (int)(grid.LandCellCount * riverPercentage);

        while (riverBudget > 0 && sources.Count > 0)
        {
            // Weighted random selection (prefer better sources)
            int idx = PickWeightedSource(sources);
            var source = sources[idx];

            int length = TraceRiver(source);
            if (length > 0)
            {
                riverBudget -= length;
            }

            sources.RemoveAt(idx);
        }
    }
}
```

#### 4.2 River Tracing (Steepest Descent)

```csharp
private int TraceRiver(HexCell source)
{
    var current = source;
    var riverCells = new List<(HexCell cell, HexDirection direction)>();
    var visited = new HashSet<HexCell>();

    while (!current.IsUnderwater)
    {
        if (visited.Contains(current)) break;
        visited.Add(current);

        // Find steepest descent
        HexDirection? flowDir = FindSteepestDescent(current);

        if (flowDir == null) break;

        var neighbor = current.GetNeighbor(flowDir.Value);
        if (neighbor == null) break;

        riverCells.Add((current, flowDir.Value));

        // Check for merge with existing river
        if (neighbor.HasRiver) break;

        current = neighbor;
    }

    // Only create river if long enough
    if (riverCells.Count < GenerationConfig.MinRiverLength)
        return 0;

    // Apply river to cells
    foreach (var (cell, dir) in riverCells)
    {
        SetOutgoingRiver(cell, dir);
    }

    return riverCells.Count;
}

private HexDirection? FindSteepestDescent(HexCell cell)
{
    HexDirection? best = null;
    int bestDrop = 0;

    for (int d = 0; d < 6; d++)
    {
        var neighbor = cell.GetNeighbor((HexDirection)d);
        if (neighbor == null) continue;

        int drop = cell.Elevation - neighbor.Elevation;
        if (drop > bestDrop)
        {
            bestDrop = drop;
            best = (HexDirection)d;
        }
    }

    // Also consider equal elevation with probability
    if (best == null)
    {
        var equalNeighbors = new List<HexDirection>();
        for (int d = 0; d < 6; d++)
        {
            var neighbor = cell.GetNeighbor((HexDirection)d);
            if (neighbor != null && neighbor.Elevation == cell.Elevation)
            {
                equalNeighbors.Add((HexDirection)d);
            }
        }
        if (equalNeighbors.Count > 0 && _rng.NextDouble() < 0.3)
        {
            best = equalNeighbors[_rng.Next(equalNeighbors.Count)];
        }
    }

    return best;
}
```

### Phase 5: Integration with HexMapEditor

#### 5.1 Modify HexMapEditor Input Handling

**File: `HexMapEditor.cs` (modifications)**

```csharp
// Add fields
private MapGenerator? _mapGenerator;
private int _currentSeed;

// In _Ready or initialization
_mapGenerator = new MapGenerator();
_currentSeed = (int)GD.Randi();

// Add to _Input method
public override void _Input(InputEvent @event)
{
    if (@event is InputEventKey keyEvent && keyEvent.Pressed)
    {
        switch (keyEvent.Keycode)
        {
            case Key.Space:
                _currentSeed = (int)GD.Randi();
                RegenerateMap();
                break;
            case Key.G:
                RegenerateMap();  // Same seed
                break;
        }
    }
    // ... existing input handling
}

private void RegenerateMap()
{
    GD.Print($"Regenerating map with seed: {_currentSeed}");

    // Clear existing map state
    ClearMap();

    // Generate new map
    _mapGenerator!.Generate(_hexGrid!, _currentSeed);

    // Refresh all chunks
    RefreshAllChunks();

    GD.Print("Map generation complete");
}

private void ClearMap()
{
    foreach (var cell in _hexGrid!.GetAllCells())
    {
        // Reset to default state
        cell.Elevation = 0;
        cell.WaterLevel = 1;
        cell.TerrainType = 0;
        cell.RemoveRiver();
        cell.RemoveRoads();
        cell.UrbanLevel = 0;
        cell.FarmLevel = 0;
        cell.PlantLevel = 0;
        cell.SpecialIndex = 0;
        cell.Walled = false;
    }
}

private void RefreshAllChunks()
{
    // Force refresh of all HexGridChunks
    // This triggers mesh rebuild with new cell data
    for (int i = 0; i < _hexGrid!.Chunks.Length; i++)
    {
        _hexGrid.Chunks[i].Refresh();
    }
}
```

### Phase 6: Feature Placement

Features are already handled by `HexFeatureManager`. We need to set appropriate feature levels after terrain generation:

```csharp
public class FeaturePlacement
{
    public void PlaceFeatures(HexGrid grid, int seed)
    {
        var rng = new Random(seed + 2000);

        foreach (var cell in grid.GetAllCells())
        {
            if (cell.IsUnderwater) continue;
            if (cell.HasRiver) continue;

            // Get biome-appropriate feature densities
            var (urban, farm, plant) = GetFeatureDensity(cell.TerrainType, rng);

            cell.UrbanLevel = urban;
            cell.FarmLevel = farm;
            cell.PlantLevel = plant;
        }
    }

    private (int urban, int farm, int plant) GetFeatureDensity(TerrainType terrain, Random rng)
    {
        return terrain switch
        {
            TerrainType.Forest => (0, 0, rng.Next(2, 4)),
            TerrainType.Jungle => (0, 0, rng.Next(2, 4)),
            TerrainType.Plains => (0, rng.Next(0, 2), rng.Next(0, 2)),
            TerrainType.Savanna => (0, 0, rng.Next(0, 2)),
            TerrainType.Desert => (0, 0, 0),
            TerrainType.Mountains => (0, 0, rng.Next(0, 2)),
            TerrainType.Snow => (0, 0, 0),
            _ => (0, 0, 0)
        };
    }
}
```

## Testing Strategy

### Unit Tests

Add to existing test suite (`godot2/tests/`):

1. **LandGeneratorTests.cs**
   - Land budget exhausted correctly
   - No cells below MinElevation
   - Water level cells exist

2. **RiverGeneratorTests.cs**
   - Rivers flow downhill
   - Rivers reach water or merge
   - Minimum length enforced

3. **BiomeAssignmentTests.cs**
   - Underwater cells get water terrain
   - High elevation gets snow/mountains
   - Moisture affects land biomes

### Integration Tests

1. Generate map, verify chunk meshes rebuild
2. Generate map, verify pathfinding still works
3. Generate multiple times, verify no memory leaks

### Manual Testing Checklist

- [ ] Spacebar generates new map with new seed
- [ ] G key regenerates same map
- [ ] Test map still loads initially (if that's the behavior we want)
- [ ] Rivers flow to water
- [ ] Biomes look reasonable
- [ ] Features appear in appropriate biomes
- [ ] No visual artifacts in chunk boundaries
- [ ] Performance acceptable (<500ms for 32x32)

## Implementation Order

### Sprint 1: Foundation
1. Create `Generation/` folder structure
2. Create `GenerationConfig.cs` with constants
3. Create `IMapGenerator.cs` interface
4. Create basic `MapGenerator.cs` skeleton

### Sprint 2: Land Generation
1. Implement `LandGenerator.cs` with chunk budget
2. Add erosion pass
3. Test with visual output

### Sprint 3: Climate & Biomes
1. Implement `ClimateGenerator.cs` (moisture only initially)
2. Implement biome assignment
3. Hook up to HexCell.TerrainType

### Sprint 4: Rivers
1. Port `RiverGenerator.cs` from old implementation
2. Adapt to use new HexCell river API
3. Test river connectivity

### Sprint 5: Integration
1. Add spacebar/G key handling to HexMapEditor
2. Implement ClearMap and RefreshAllChunks
3. Wire up feature placement

### Sprint 6: Polish
1. Add progress events for UI feedback
2. Add async generation option
3. Performance optimization
4. Write tests

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| River generation creates dead-ends | Medium | Medium | Add drainage fallback |
| Chunk boundary artifacts | Low | High | Existing chunk system handles this |
| Performance issues | Low | Medium | Async generation option |
| Memory leaks on regeneration | Medium | High | Careful cleanup in ClearMap |

## Dependencies

### Required
- FastNoiseLite (already available in Godot)
- Existing HexCell river/road APIs
- Existing HexGridChunk refresh mechanism

### Optional
- UI progress bar (can add later)
- Undo system for generation (not planned)

## Success Criteria

1. ✅ Spacebar triggers procedural map generation
2. ✅ Maps have natural-looking coastlines (not just noise)
3. ✅ Rivers flow from high to low elevation
4. ✅ Biomes reflect elevation and moisture
5. ✅ Features appear in appropriate densities
6. ✅ Existing pathfinding works on generated maps
7. ✅ Generation completes in <1 second for standard map size

## References

- Catlike Coding Hex Map Tutorial 23: https://catlikecoding.com/unity/tutorials/hex-map/part-23/
- Catlike Coding Hex Map Tutorial 24: https://catlikecoding.com/unity/tutorials/hex-map/part-24/
- Catlike Coding Hex Map Tutorial 25: https://catlikecoding.com/unity/tutorials/hex-map/part-25/
- Catlike Coding Hex Map Tutorial 26: https://catlikecoding.com/unity/tutorials/hex-map/part-26/
- Old implementation: `C:\Temp\godot\src\CSharp\Generation\`
