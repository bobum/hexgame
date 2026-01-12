# Hexgame Agent Guide

This document provides context for AI agents working with the hexgame codebase.

## Project Overview

Hexgame is a web-based hex grid strategy game built with **Three.js** and **TypeScript**. It features procedural map generation, turn-based gameplay, A* pathfinding, and multiple unit types across land/naval/amphibious domains.

## Architecture

```
src/
├── camera/          # Isometric camera with pan/zoom/rotate
├── core/            # Hex coordinate system & grid management
├── generation/      # Procedural map generation (terrain, biomes, rivers)
├── pathfinding/     # A* pathfinding with domain-aware movement
├── rendering/       # Three.js rendering (terrain, water, units, LOD)
├── types/           # TypeScript interfaces and enums
├── units/           # Unit management, types, turn-based system
├── utils/           # Object pooling, spatial hashing, performance
└── workers/         # Web Workers for async operations
```

## Tech Stack

| Technology | Version | Purpose |
|------------|---------|---------|
| Three.js | 0.182.0 | 3D rendering engine |
| TypeScript | 5.9.3 | Type-safe language |
| Vite | 7.2.4 | Build tool and dev server |
| Vitest | 4.0.17 | Testing framework |

## Key Abstractions

### Coordinate System

The game uses **cube coordinates** for hex grids with constraint `q + r + s = 0`:

```typescript
// HexCoordinates is immutable
const coords = new HexCoordinates(q, r);  // s is derived as -q - r
const worldPos = coords.toWorldPosition(elevation);
const fromWorld = HexCoordinates.fromWorldPosition(vector3);
```

**Conversion helpers:**
- `HexCoordinates.fromOffset(col, row)` - From rectangular grid
- `HexCoordinates.round(q, r)` - Round fractional to nearest hex
- `coords.toKey()` / `HexCoordinates.fromKey(key)` - Map keys as `"q,r"`

### Core Types

```typescript
// Terrain types (12 biomes)
enum TerrainType {
  Ocean, Coast, Plains, Forest, Hills, Mountains,
  Snow, Desert, Tundra, Jungle, Savanna, Taiga
}

// Unit domains determine movement capabilities
enum UnitDomain {
  Land,       // Can only move on land
  Naval,      // Can only move on water
  Amphibious  // Can move on both
}

// Core cell data structure
interface HexCell {
  q: number; r: number; s: number;  // Cube coordinates
  elevation: number;                 // -2 to 8
  terrainType: TerrainType;
  moisture: number;                  // 0-1
  temperature: number;               // 0-1
  features: Feature[];               // Trees, rocks, peaks
  riverDirections: HexDirection[];   // Edge-based rivers
}
```

### Grid Access Patterns

```typescript
// Get cell by coordinates
const cell = grid.getCell(new HexCoordinates(q, r));
const cell = grid.getCellAt(q, r);  // Shorthand

// Get neighbors
const neighbors = coords.getNeighbors();  // All 6
const neighbor = coords.getNeighbor(HexDirection.NE);

// Spatial queries (O(1) via spatial hash)
const nearbyUnits = unitManager.getUnitsInRadius(q, r, radius);
```

## Coding Conventions

### ES Modules
- Use ES module imports (`import`/`export`)
- No CommonJS (`require`/`module.exports`)

### Data vs Classes
- Use **interfaces** for data structures (`HexCell`, `UnitData`, `MapConfig`)
- Use **classes** for stateful managers (`HexGrid`, `UnitManager`, `Pathfinder`)
- Keep data immutable where possible (`HexCoordinates` is immutable)

### Object Pooling
Units use object pooling for memory efficiency:
```typescript
// Acquire from pool
const unit = unitManager.spawnUnit(UnitType.Infantry, q, r, playerId);

// Return to pool
unitManager.removeUnit(unit.id);
```

### Three.js Lifecycle
Always dispose of Three.js resources:
```typescript
// In dispose() methods:
geometry.dispose();
material.dispose();
scene.remove(mesh);
```

### Worker Communication
Heavy operations use Web Workers:
```typescript
// Async map generation
const result = await mapGenerator.generateAsync();

// Worker returns serializable data only
interface WorkerResult {
  cells: SerializedCell[];
  workerTime: number;
}
```

## Common Pitfalls

### 1. Coordinate Confusion
- **Cube coords** (q, r, s): Used in game logic, satisfies `q + r + s = 0`
- **Offset coords** (col, row): Used for rectangular iteration
- **World position** (x, y, z): Three.js world space

Always use the appropriate conversion method.

### 2. Elevation vs TerrainType
- `elevation < 0` = water (regardless of terrainType)
- `TerrainType.Ocean` cells always have `elevation < 0`
- `TerrainType.Coast` can be at `elevation >= 0`

### 3. Movement Costs
- `Infinity` means impassable
- Check domain before calculating: `getMovementCostForUnit(from, to, unitType)`
- Cliffs (2+ elevation diff) are impassable

### 4. Three.js Memory Leaks
- Always call `dispose()` when removing renderers
- Remove objects from scene before disposing
- Dispose geometries AND materials

## Testing

```bash
npm run test           # Run unit tests
npm run test:watch     # Watch mode
npm run test:coverage  # Coverage report
npm run bench          # Performance benchmarks
```

Tests are in `tests/` mirroring `src/` structure. Rendering code is excluded from coverage (Three.js dependent).

## Development Commands

```bash
npm run dev        # Start dev server (http://localhost:5173)
npm run build      # Production build
npm run typecheck  # Type checking only
```

## Performance Considerations

- **LOD System**: Terrain uses 3 detail levels based on camera distance
- **Chunked Rendering**: 16x16 hex chunks with frustum culling
- **Spatial Hashing**: O(1) lookups for cells and units
- **Object Pooling**: Units are pooled to reduce GC pressure
- **Worker Offloading**: Map generation runs in Web Worker

## Adding New Features

### New Unit Type
1. Add to `UnitType` enum in `src/units/UnitTypes.ts`
2. Add stats to `UnitStats` record
3. Unit will automatically work with existing systems

### New Terrain Type
1. Add to `TerrainType` enum in `src/types/index.ts`
2. Add color to `TerrainColors` in `src/core/HexMetrics.ts`
3. Add movement costs in `src/pathfinding/MovementCosts.ts`
4. Update `BiomeMapper` generation logic if needed

### New Map Feature
1. Add to `FeatureType` enum in `src/types/index.ts`
2. Add mesh creation in `FeatureRenderer.createFeatureMesh()`
3. Add placement logic in `MapGenerator`

## File Reference

| File | Purpose |
|------|---------|
| `src/main.ts` | Application entry point, HexGame class |
| `src/core/HexCoordinates.ts` | Coordinate math and conversions |
| `src/core/HexGrid.ts` | Grid storage and spatial queries |
| `src/core/HexMetrics.ts` | Geometric constants and colors |
| `src/generation/MapGenerator.ts` | Orchestrates map generation |
| `src/pathfinding/Pathfinder.ts` | A* implementation |
| `src/pathfinding/MovementCosts.ts` | Terrain traversal costs |
| `src/units/UnitManager.ts` | Unit lifecycle and pooling |
| `src/units/UnitTypes.ts` | Unit definitions and stats |
| `src/rendering/ChunkedTerrainRenderer.ts` | LOD terrain rendering |
