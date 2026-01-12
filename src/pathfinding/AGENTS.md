# Pathfinding Module Agent Guide

This module provides A* pathfinding with domain-aware movement costs.

## Files

| File | Purpose |
|------|---------|
| `Pathfinder.ts` | A* algorithm implementation |
| `MovementCosts.ts` | Terrain costs and passability |
| `PriorityQueue.ts` | Min-heap for A* frontier |
| `index.ts` | Public exports |

## Domain-Aware Pathfinding

Units have different movement domains that determine where they can move:

| Domain | Can Traverse |
|--------|--------------|
| Land | All land terrain except Mountains and Ocean |
| Naval | Ocean and Coast cells (elevation < 0) |
| Amphibious | Both land and water |

## Using the Pathfinder

```typescript
const pathfinder = new Pathfinder(grid, unitManager);

// Find path with unit type for domain awareness
const result = pathfinder.findPath(startCell, endCell, {
  unitType: UnitType.Infantry
});

if (result.reachable) {
  console.log(result.path);  // HexCell[]
  console.log(result.cost);  // Total movement cost
}

// Get all reachable cells within movement range
const reachable = pathfinder.getReachableCells(startCell, movementPoints, {
  unitType: UnitType.Cavalry
});
```

## Movement Costs

### Land Unit Costs

```typescript
const LandTerrainCosts = {
  Plains: 1,
  Coast: 1,
  Desert: 1,
  Savanna: 1,
  Forest: 1.5,
  Taiga: 1.5,
  Jungle: 2,
  Tundra: 1.5,
  Hills: 2,
  Snow: 2.5,
  Mountains: Infinity,  // Impassable
  Ocean: Infinity       // Impassable
};
```

### Naval Unit Costs

```typescript
const NavalTerrainCosts = {
  Ocean: 1,       // Easy sailing
  Coast: 1.5,     // Slightly harder
  // All land: Infinity
};
```

### Additional Cost Factors

```typescript
// Elevation difference (climbing penalty)
if (elevationDiff > 0) {
  cost += elevationDiff * 0.5;
}

// Cliff check (impassable)
if (Math.abs(elevationDiff) >= 2) {
  return Infinity;
}

// River crossing penalty
if (crossesRiver(from, to, direction)) {
  cost += RIVER_CROSSING_COST;  // 1.0
}
```

## API Reference

### getMovementCostForUnit()

```typescript
function getMovementCostForUnit(
  from: HexCell,
  to: HexCell,
  unitType: UnitType,
  direction?: HexDirection
): number
```

Returns movement cost or `Infinity` if impassable.

### isPassableForUnit()

```typescript
function isPassableForUnit(cell: HexCell, unitType: UnitType): boolean
```

Quick passability check without calculating full cost.

### crossesRiver()

```typescript
function crossesRiver(
  from: HexCell,
  to: HexCell,
  direction?: HexDirection
): boolean
```

Checks if movement crosses a river edge.

## PriorityQueue

Min-heap implementation for A* frontier:

```typescript
const pq = new PriorityQueue<HexCell>();

pq.push(cell, priority);  // Lower priority = higher precedence
const next = pq.pop();    // Returns lowest priority item
const isEmpty = pq.isEmpty();
```

## Pathfinder Options

```typescript
interface PathfindOptions {
  unitType?: UnitType;           // For domain-aware costs
  ignoreUnits?: boolean;         // Ignore unit collisions
  maxCost?: number;              // Stop if cost exceeds
}
```

## A* Implementation Notes

1. Uses hex distance as heuristic (admissible)
2. Tracks `cameFrom` for path reconstruction
3. Early exit when goal reached
4. Returns `{ reachable, path, cost }`

## Unit Collision

By default, pathfinding considers other units as obstacles:

```typescript
// Cell occupied by enemy unit = impassable
// Cell occupied by friendly unit = can pass through

// To ignore unit collisions:
pathfinder.findPath(start, end, { ignoreUnits: true });
```

## Adding New Terrain Costs

1. Update `LandTerrainCosts` and/or `NavalTerrainCosts` in `MovementCosts.ts`
2. If new terrain type, ensure it's handled in passability checks
3. Update tests in `tests/pathfinding/MovementCosts.test.ts`

## Performance

- A* with hex distance heuristic is optimal for hex grids
- `getReachableCells` uses Dijkstra (no goal = no heuristic)
- Spatial hash provides O(1) unit collision checks
- Avoid calling pathfinding every frame - cache results
