# Rendering Optimization Plan: Chunking, LOD, Culling, Fog & UI Panels

## Overview
Implement performance optimizations and UI panels based on the reference implementation in C:\Temp\godot.

## Sprint 7: Rendering Optimization & UI

### Phase 1: Chunking System
**Goal**: Divide the hex grid into spatial chunks for efficient rendering.

#### Key Classes to Create
- `ChunkedTerrainRenderer` - Manages terrain chunks with LOD meshes
- `ChunkedRiverRenderer` - Manages river chunks with animation
- `ChunkedRendererBase` - Abstract base for chunked renderers
- `RenderingSystem` - Central coordinator for all renderers

#### Configuration Constants
```csharp
public const float ChunkSize = 16.0f;        // World units per chunk
public const float MaxRenderDistance = 60.0f; // Cull beyond this
```

#### Implementation
1. Cell-to-chunk mapping: `Vector2I GetCellChunkCoords(HexCell cell)`
2. Chunk storage: `Dictionary<string, TerrainChunk>` keyed by `"{cx},{cz}"`
3. Each chunk stores: coordinates, center position, list of cells, LOD meshes

---

### Phase 2: LOD (Level of Detail) System
**Goal**: Reduce polygon count for distant chunks.

#### Three LOD Levels
| Level | Distance | Detail |
|-------|----------|--------|
| HIGH | < 30 units | Full terraces, edge connections |
| MEDIUM | 30-60 units | Flat hexagons |
| LOW | 60+ units | Simple quads |

#### Configuration Constants
```csharp
public const float LodHighToMedium = 30.0f;
public const float LodMediumToLow = 60.0f;
```

#### Implementation
1. Build three mesh variants per chunk during `Build()`
2. Switch visibility based on camera distance in `Update()`
3. `HexMeshBuilder` handles different complexity levels

---

### Phase 3: Frustum Culling
**Goal**: Skip rendering off-screen and distant chunks.

#### Two-Tier Strategy
1. **Distance culling** - Hide chunks beyond MaxRenderDistance
2. **Engine frustum culling** - Godot handles visible mesh culling

#### Implementation
```csharp
public void Update(Camera3D camera)
{
    var cameraXz = new Vector3(camera.GlobalPosition.X, 0, camera.GlobalPosition.Z);
    float maxDistSq = MaxRenderDistance * MaxRenderDistance;

    foreach (var chunk in _chunks.Values)
    {
        float distSq = (chunk.Center - cameraXz).LengthSquared();

        if (distSq > maxDistSq)
        {
            // Cull - hide all LODs
            SetChunkVisible(chunk, false);
        }
        else
        {
            // Select appropriate LOD
            SelectLOD(chunk, Mathf.Sqrt(distSq));
        }
    }
}
```

---

### Phase 4: Fog System
**Goal**: Create atmospheric depth with distance fog.

#### Configuration
```csharp
public float FogNear { get; set; } = 15.0f;     // Start distance
public float FogFar { get; set; } = 50.0f;      // Full fog distance
public float FogDensity { get; set; } = 0.5f;   // Intensity
```

#### Implementation Options
1. **Shader-based fog** - Apply in terrain shader (preferred)
2. **Post-process fog** - WorldEnvironment fog settings
3. **Fog-of-war overlay** - Separate chunked renderer for exploration fog

---

### Phase 5: Control Panel (Right Side UI)
**Goal**: UI panel for runtime parameter adjustment.

#### Panel Structure
- Position: Right edge, 260px wide
- Collapsible sections using VBoxContainer

#### Sections
1. **Map Generation**
   - Width/Height spinners (10-80, 10-60)
   - Seed spinner with Random button
   - Async checkbox, Regenerate button

2. **Terrain**
   - Scale, Octaves, Persistence, Lacunarity sliders
   - Sea Level, Mountains sliders

3. **Rendering**
   - LOD distances
   - Chunk size
   - Max render distance

4. **Fog**
   - Near/Far distance sliders
   - Density slider

5. **Info**
   - FPS, Cell count, Draw calls, Triangles

#### Implementation
- Extend existing UI or create `GameUI` class
- Emit signals for parameter changes
- Connect to relevant systems

---

### Phase 6: Diagnostic Panel (Lower Left)
**Goal**: Real-time performance monitoring with visual graph.

#### Components
1. **Frame Time Graph** (200x60 pixels)
   - Last 200 frames history
   - Color-coded: Green (60fps), Yellow (30fps), Red (<30fps)
   - Threshold lines

2. **Statistics Label**
   - FPS, Average frame time
   - 1% Low (99th percentile), Max frame time

#### Configuration
```csharp
public const int HistorySize = 200;
public const float TargetFrameTime = 16.67f;   // 60 FPS
public const float WarningFrameTime = 33.33f;  // 30 FPS
```

---

## File Structure

### New Files to Create
```
godot2/src/Rendering/
├── ChunkedTerrainRenderer.cs
├── ChunkedRiverRenderer.cs
├── ChunkedRendererBase.cs
├── RenderingSystem.cs
├── RenderingConfig.cs
└── HexMeshBuilder.cs

godot2/src/UI/
├── GameUI.cs
├── ControlPanel.cs
└── PerformanceMonitor.cs

godot2/tests/Rendering/
├── ChunkedTerrainRendererTests.cs
├── RenderingSystemTests.cs
└── PerformanceMonitorTests.cs
```

### Files to Modify
- `HexGrid.cs` - Add chunk coordinate helpers
- `HexGridChunk.cs` - Integrate with chunked rendering
- `HexMesh.cs` - Support LOD mesh generation
- `MapCamera.cs` - Expose position for culling

---

## Implementation Order

1. **RenderingConfig.cs** - Constants and configuration
2. **ChunkedRendererBase.cs** - Abstract base class
3. **ChunkedTerrainRenderer.cs** - Main terrain chunking
4. **RenderingSystem.cs** - Coordinator
5. **PerformanceMonitor.cs** - Diagnostic panel
6. **GameUI.cs / ControlPanel.cs** - Control panel
7. **Fog integration** - Shader or environment fog
8. **ChunkedRiverRenderer.cs** - River chunking (optional)

---

## Test Plan

### Unit Tests
- Chunk coordinate calculation
- LOD selection logic
- Distance culling
- Performance metric calculations

### Integration Tests
- Full rendering pipeline
- UI signal emission
- Camera-renderer integration

### Visual Verification
- LOD transitions smooth
- No popping artifacts
- Culling works correctly
- Fog looks natural
- UI panels functional

---

## Success Criteria

1. **Performance**: Maintain 60 FPS with large maps (80x60)
2. **Visual Quality**: Smooth LOD transitions, no pop-in
3. **UI**: All controls functional, responsive
4. **Diagnostics**: Accurate FPS and frame time reporting
