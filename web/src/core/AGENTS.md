# Core Module Agent Guide

This module contains the foundational hex grid coordinate system and metrics.

## Files

| File | Purpose |
|------|---------|
| `HexCoordinates.ts` | Cube coordinate system and conversions |
| `HexGrid.ts` | Grid storage with spatial hash integration |
| `HexMetrics.ts` | Geometric constants, colors, elevation |
| `HexDirection.ts` | 6 directional enum and offset lookups |

## Coordinate System

### Cube Coordinates (q, r, s)

The game uses axial/cube coordinates where `q + r + s = 0`:

```
     NW    NE
       \  /
    W --*-- E
       /  \
     SW    SE
```

- `q` increases going East
- `r` increases going Southeast
- `s` is always `-q - r` (derived, not stored separately in most cases)

### Key Conversions

```typescript
// World position (Three.js) ↔ Hex coordinates
const coords = HexCoordinates.fromWorldPosition(vector3);
const worldPos = coords.toWorldPosition(elevation);

// Offset (rectangular) ↔ Hex coordinates
const coords = HexCoordinates.fromOffset(col, row);

// Round fractional coordinates to nearest hex
const rounded = HexCoordinates.round(fractionalQ, fractionalR);
```

### Neighbor Access

```typescript
// Single neighbor in direction
const ne = coords.getNeighbor(HexDirection.NE);

// All 6 neighbors
const neighbors = coords.getNeighbors();

// Distance in hex steps
const dist = coords.distanceTo(otherCoords);
```

## HexGrid Storage

Cells are stored in a `Map<string, HexCell>` using coordinate keys:

```typescript
// Get cell
const cell = grid.getCell(coords);        // By HexCoordinates
const cell = grid.getCellAt(q, r);        // By q, r values

// Set cell
grid.setCell(cell);

// Iteration
for (const cell of grid.cells) { ... }
```

### Spatial Hash Integration

The grid includes a spatial hash for O(1) position queries:

```typescript
// Query by world position
const cell = grid.getCellAtWorldPosition(worldX, worldZ);

// Query radius (in world units)
const cells = grid.getCellsInRadius(worldX, worldZ, radius);
```

## HexMetrics Constants

```typescript
HexMetrics.outerRadius = 1;           // Vertex distance from center
HexMetrics.innerRadius = 0.866;       // Edge distance from center (√3/2)
HexMetrics.elevationStep = 0.3;       // World units per elevation level
HexMetrics.minElevation = -2;         // Ocean floor
HexMetrics.maxElevation = 8;          // Mountain peaks
```

### Terrain Colors

`HexMetrics.TerrainColors` maps each `TerrainType` to a Three.js `Color`:

```typescript
const color = HexMetrics.TerrainColors[TerrainType.Forest];
```

## HexDirection

Six directions with offset lookups:

```typescript
enum HexDirection { NE, E, SE, SW, W, NW }

// Get offset for direction
const [dq, dr] = DirectionOffsets[HexDirection.NE];  // [1, -1]

// Get opposite direction
const opposite = HexDirection.opposite(HexDirection.NE);  // SW
```

## Common Patterns

### Creating Coordinates

```typescript
// Direct construction
const coords = new HexCoordinates(5, -3);

// From rectangular iteration
for (let row = 0; row < height; row++) {
  for (let col = 0; col < width; col++) {
    const coords = HexCoordinates.fromOffset(col, row);
  }
}
```

### Map Key Usage

```typescript
// For Map/Set storage
const key = coords.toKey();  // "5,-3"
const coords = HexCoordinates.fromKey(key);

// In grid
const cell = grid.cells.get(coords.toKey());
```

### Ring/Spiral Queries

```typescript
// All hexes at distance 3
const ring = HexCoordinates.ring(center, 3);

// All hexes from center to distance 5
const spiral = HexCoordinates.spiral(center, 5);
```

## Pitfalls

1. **HexCoordinates is immutable** - Methods return new instances
2. **s is derived** - Never set s manually, it's computed as `-q - r`
3. **Offset vs Cube** - Don't mix coordinate systems in calculations
4. **Elevation in world Y** - `toWorldPosition(elevation)` handles Y conversion
