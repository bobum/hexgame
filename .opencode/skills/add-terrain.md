# Skill: Add Terrain Type

Add a new terrain/biome type to the game.

## Usage

```
/add-terrain <name>
```

## Steps

### 1. Add to TerrainType enum

File: `src/types/index.ts`

```typescript
export enum TerrainType {
  // ...existing types...
  {{Name}} = '{{name}}',
}
```

### 2. Add color to HexMetrics

File: `src/core/HexMetrics.ts`

```typescript
export const TerrainColors: Record<TerrainType, THREE.Color> = {
  // ...existing colors...
  [TerrainType.{{Name}}]: new THREE.Color(0x{{hexColor}}),
};
```

### 3. Add movement costs

File: `src/pathfinding/MovementCosts.ts`

```typescript
// For land units
export const LandTerrainCosts: Record<TerrainType, number> = {
  // ...existing...
  [TerrainType.{{Name}}]: {{landCost}},  // 1 = easy, 2 = hard, Infinity = impassable
};

// For naval units
export const NavalTerrainCosts: Record<TerrainType, number> = {
  // ...existing...
  [TerrainType.{{Name}}]: {{navalCost}},  // Infinity for land terrain
};
```

### 4. Add generation rules (optional)

If the terrain should be generated procedurally, update `BiomeMapper.ts`:

File: `src/generation/BiomeMapper.ts`

```typescript
// In mapToBiome() method
if (elevation >= {{minElev}} && elevation <= {{maxElev}}) {
  if (temperature >= {{minTemp}} && moisture >= {{minMoist}}) {
    return TerrainType.{{Name}};
  }
}
```

### 5. Update passability checks (if special rules)

File: `src/pathfinding/MovementCosts.ts`

```typescript
export function isPassableForLand(cell: HexCell): boolean {
  // Add special cases if needed
  if (cell.terrainType === TerrainType.{{Name}}) {
    return {{isLandPassable}};
  }
  // ...existing logic
}
```

### 6. Add tests

File: `tests/pathfinding/MovementCosts.test.ts`

```typescript
describe('{{Name}} terrain', () => {
  it('should have correct land movement cost', () => {
    expect(LandTerrainCosts[TerrainType.{{Name}}]).toBe({{landCost}});
  });
});
```

## Checklist

- [ ] Added to TerrainType enum
- [ ] Added color to TerrainColors
- [ ] Added land movement cost
- [ ] Added naval movement cost
- [ ] Updated BiomeMapper (if procedural)
- [ ] Tests pass: `npm run test`
- [ ] Type check passes: `npm run typecheck`
- [ ] Visual inspection in game
