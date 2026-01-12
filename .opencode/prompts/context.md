# Hexgame Project Context

## Quick Reference

**Tech Stack**: TypeScript, Three.js, Vite, Vitest

**Commands**:
- `npm run dev` - Start dev server
- `npm run test` - Run tests
- `npm run typecheck` - Type checking
- `npm run build` - Production build

## Project Structure

```
src/
├── core/         # Hex coordinates, grid, metrics
├── generation/   # Procedural map generation
├── pathfinding/  # A* with domain-aware movement
├── rendering/    # Three.js terrain, water, features
├── units/        # Unit types, manager, turns
├── camera/       # Isometric camera controls
├── utils/        # Pooling, spatial hash, perf
└── workers/      # Web Worker async operations
```

## Key Concepts

### Coordinates
- **Cube coords** (q, r, s): Game logic, `q + r + s = 0`
- **World position**: Three.js Vector3

### Unit Domains
- **Land**: Move on terrain
- **Naval**: Move on water
- **Amphibious**: Both

### Terrain
- 12 biome types with movement costs
- Elevation -2 to 8
- Rivers on hex edges

## Common Patterns

```typescript
// Get cell
const cell = grid.getCellAt(q, r);

// Convert coords
const world = coords.toWorldPosition(elevation);
const hex = HexCoordinates.fromWorldPosition(vector3);

// Pathfinding
const result = pathfinder.findPath(start, end, { unitType });

// Unit operations
const unit = unitManager.spawnUnit(type, q, r, playerId);
unitManager.moveUnit(unitId, newQ, newR, cost);
```

## Pitfalls

1. Always dispose Three.js resources
2. Elevation < 0 = water
3. HexCoordinates is immutable
4. Use spatial hash for O(1) queries
