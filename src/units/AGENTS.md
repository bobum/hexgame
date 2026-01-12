# Units Module Agent Guide

This module manages unit types, lifecycle, and turn-based gameplay.

## Files

| File | Purpose |
|------|---------|
| `UnitTypes.ts` | Unit definitions, stats, and domains |
| `UnitManager.ts` | Unit lifecycle with object pooling |
| `UnitRenderer.ts` | Three.js rendering of units |
| `TurnManager.ts` | Turn-based game flow |
| `index.ts` | Public exports |

## Unit Types

### Current Units

| Type | Domain | Health | Movement | Attack | Defense |
|------|--------|--------|----------|--------|---------|
| Infantry | Land | 100 | 2 | 10 | 8 |
| Cavalry | Land | 80 | 4 | 12 | 5 |
| Archer | Land | 60 | 2 | 15 | 3 |
| Galley | Naval | 80 | 3 | 8 | 6 |
| Warship | Naval | 150 | 2 | 20 | 12 |
| Marine | Amphibious | 70 | 2 | 8 | 6 |

### Domains

```typescript
enum UnitDomain {
  Land,       // Can only move on land terrain
  Naval,      // Can only move on water (Ocean, Coast)
  Amphibious  // Can move on both
}

// Check domain
const domain = getUnitDomain(UnitType.Infantry);  // Land
canTraverseLand(UnitType.Galley);   // false
canTraverseWater(UnitType.Marine);  // true
```

## UnitData Interface

Runtime unit state (mutable):

```typescript
interface UnitData {
  id: number;           // Unique identifier
  type: UnitType;       // Unit type enum
  q: number;            // Hex position Q
  r: number;            // Hex position R
  health: number;       // Current health
  maxHealth: number;    // Maximum health
  movement: number;     // Remaining movement this turn
  maxMovement: number;  // Movement per turn
  attack: number;       // Attack strength
  defense: number;      // Defense strength
  playerId: number;     // 0=neutral, 1=player, 2+=AI
  hasMoved: boolean;    // Moved this turn?
}
```

## UnitManager

Manages unit lifecycle with **object pooling**:

```typescript
const unitManager = new UnitManager(grid);

// Spawn unit (acquires from pool)
const unit = unitManager.spawnUnit(UnitType.Infantry, q, r, playerId);

// Get unit
const unit = unitManager.getUnit(unitId);
const unit = unitManager.getUnitAt(q, r);

// Move unit
unitManager.moveUnit(unitId, newQ, newR, movementCost);

// Remove unit (returns to pool)
unitManager.removeUnit(unitId);

// Spawn helpers
unitManager.spawnRandomUnits(count, playerId);      // Land units
unitManager.spawnRandomNavalUnits(count, playerId); // Naval units
unitManager.spawnMixedUnits(land, naval, playerId); // Both
```

### Object Pooling

Units are pooled to reduce garbage collection:

```typescript
// Pool stats
const stats = unitManager.poolStats;
// { created: 50, active: 30, available: 20, reuseRate: 0.4 }

// Clear all units (returns to pool)
unitManager.clear();
```

### Spatial Queries

```typescript
// Get all units in radius (world units)
const nearby = unitManager.getUnitsInRadius(q, r, radius);

// Check if cell is occupied
const occupied = unitManager.isOccupied(q, r);
```

## TurnManager

Controls turn-based game flow:

```typescript
const turnManager = new TurnManager(unitManager);
turnManager.startGame();

// Check current state
turnManager.isHumanTurn;        // true if player's turn
turnManager.currentPlayer;      // Player ID
turnManager.currentTurn;        // Turn number
turnManager.currentPhase;       // TurnPhase enum

// Actions
turnManager.canMove();          // Can units move?
turnManager.endTurn();          // End current turn

// Status string
turnManager.getStatus();        // "Turn 1 - Player (Movement)"
```

### Turn Phases

```typescript
enum TurnPhase {
  Movement,  // Units can move
  Combat,    // Combat resolution (not yet implemented)
  End        // Turn ending
}
```

### Player IDs

```typescript
const PLAYER_HUMAN = 1;  // Human player
const PLAYER_AI = 2;     // AI opponent
```

## UnitRenderer

Renders units as Three.js meshes:

```typescript
const unitRenderer = new UnitRenderer(scene, unitManager);

// Update after changes
unitRenderer.markDirty();  // Triggers rebuild on next update()
unitRenderer.update();     // Called each frame

// Selection highlighting
unitRenderer.setSelectedUnits(selectedIds);  // Set<number>

// Cleanup
unitRenderer.dispose();
```

## Adding a New Unit Type

1. Add to `UnitType` enum:
```typescript
enum UnitType {
  // ...existing...
  Trebuchet = 'trebuchet',
}
```

2. Add stats to `UnitStats`:
```typescript
[UnitType.Trebuchet]: {
  type: UnitType.Trebuchet,
  domain: UnitDomain.Land,
  health: 40,
  maxHealth: 40,
  movement: 1,
  maxMovement: 1,
  attack: 25,
  defense: 2,
  name: 'Trebuchet',
  description: 'Siege unit. Slow but devastating.',
},
```

3. Unit automatically works with:
   - Pathfinding (uses domain)
   - Spawning (UnitManager)
   - Rendering (UnitRenderer - may want custom mesh)

4. For custom rendering, update `UnitRenderer.createUnitMesh()`.

## Movement Flow

```typescript
// 1. Check if it's player's turn and unit can move
if (turnManager.isHumanTurn && turnManager.canMove()) {

  // 2. Find path to destination
  const result = pathfinder.findPath(startCell, endCell, {
    unitType: unit.type
  });

  // 3. Check if within movement range
  if (result.reachable && result.cost <= unit.movement) {

    // 4. Execute move
    unitManager.moveUnit(unit.id, endCell.q, endCell.r, result.cost);

    // 5. Update renderer
    unitRenderer.markDirty();
  }
}
```

## Turn End Flow

```typescript
// When turn ends:
// 1. Reset all units' movement points
// 2. Clear hasMoved flags
// 3. Switch to next player
// 4. If AI turn, process AI moves
// 5. Increment turn counter if back to player 1
```
