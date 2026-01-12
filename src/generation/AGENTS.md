# Generation Module Agent Guide

This module handles procedural map generation including terrain, biomes, and rivers.

## Files

| File | Purpose |
|------|---------|
| `MapGenerator.ts` | Orchestrates multi-stage generation |
| `NoiseGenerator.ts` | Simplex noise with octave layering |
| `BiomeMapper.ts` | Determines terrain from elevation/climate |
| `RiverGenerator.ts` | Edge-based river system |

## Generation Pipeline

Map generation follows these stages in order:

```
1. Elevation    → Base height map using noise
2. Climate      → Moisture and temperature distribution
3. Terrain      → Biome assignment based on elevation + climate
4. Rivers       → Water flow from high to low elevation
5. Features     → Trees, rocks, mountain peaks
```

### Async Generation

For large maps, generation runs in a Web Worker:

```typescript
// Async (recommended for maps > 30x30)
const result = await mapGenerator.generateAsync();
// Returns: { cells, workerTime, featureTime }

// Sync (blocks main thread)
mapGenerator.generate();
```

## NoiseGenerator

Uses Simplex noise with octave-based detail:

```typescript
const noise = new NoiseGenerator(seed);

// Single sample
const value = noise.sample(x, z);  // Returns -1 to 1

// With octave layering (more detail)
const value = noise.octaveNoise(x, z, {
  octaves: 4,
  persistence: 0.5,  // Amplitude decay
  lacunarity: 2.0,   // Frequency multiplier
  scale: 0.05        // Base frequency
});
```

## BiomeMapper

Maps elevation + climate to terrain types:

```typescript
// Elevation ranges
< 0              → Ocean
0                → Coast
1-2              → Plains/Desert/Tundra (by temperature)
3-4              → Forest/Jungle/Taiga (by moisture)
5-6              → Hills
7+               → Mountains/Snow
```

### Climate Factors

- **Temperature**: 0 (cold) to 1 (hot), varies with latitude and elevation
- **Moisture**: 0 (dry) to 1 (wet), varies with distance from water

## RiverGenerator

Rivers use an **edge-based** system (not cell-based):

```typescript
// Rivers flow along hex edges, not through centers
cell.riverDirections: HexDirection[]  // Edges where water flows OUT
```

### River Generation Algorithm

1. Find potential sources (high elevation land cells)
2. For each source, flow downhill toward water
3. Track river edges, not river cells
4. Rivers merge when paths intersect

### River Crossing

```typescript
import { crossesRiver } from '../pathfinding/MovementCosts';

// Check if movement crosses a river edge
if (crossesRiver(fromCell, toCell, direction)) {
  // Apply river crossing penalty
}
```

## MapConfig Parameters

```typescript
interface MapConfig {
  width: number;           // Map width in hexes
  height: number;          // Map height in hexes
  seed: number;            // Random seed for reproducibility
  noiseScale: number;      // Base noise frequency (0.01-0.2)
  octaves: number;         // Noise detail layers (1-8)
  persistence: number;     // Octave amplitude decay (0.1-0.9)
  lacunarity: number;      // Octave frequency multiplier (1.5-3.0)
  landPercentage: number;  // Target land vs water ratio (0.2-0.8)
  mountainousness: number; // Elevation variance (0.1-1.0)
  riverPercentage: number; // River density (0-0.2)
}
```

## Adding New Biomes

1. Add enum value to `TerrainType` in `src/types/index.ts`
2. Add color to `TerrainColors` in `src/core/HexMetrics.ts`
3. Update `BiomeMapper.mapToBiome()` with conditions
4. Add movement cost in `src/pathfinding/MovementCosts.ts`

## Feature Placement

Features are placed after terrain generation:

```typescript
// In MapGenerator.placeFeatures()
if (cell.terrainType === TerrainType.Forest) {
  // 30% chance of tree
  if (random() < 0.3) {
    cell.features.push({
      type: FeatureType.Tree,
      position: randomOffset,
      scale: randomScale,
      rotation: randomRotation
    });
  }
}
```

## Worker Communication

The worker receives serializable config and returns serializable cells:

```typescript
// Worker input
{ type: 'generate', config: MapConfig }

// Worker output
{
  cells: Array<{
    q, r, elevation, terrainType, moisture, temperature, riverDirections
  }>,
  time: number
}
```

Features are NOT generated in the worker (they contain Three.js Vector3).

## Performance Notes

- Generation scales with `width * height`
- Async generation prevents UI freeze for large maps
- River generation is O(n) where n = number of river sources
- Feature placement happens on main thread after worker completes
