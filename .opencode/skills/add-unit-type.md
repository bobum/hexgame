# Skill: Add Unit Type

Add a new unit type to the game.

## Usage

```
/add-unit-type <name> [domain]
```

- `name`: The unit type name (e.g., "Trebuchet", "Scout")
- `domain`: Optional - "land", "naval", or "amphibious" (default: "land")

## Steps

### 1. Add to UnitType enum

File: `src/units/UnitTypes.ts`

```typescript
export enum UnitType {
  // ...existing types...
  {{Name}} = '{{name}}',
}
```

### 2. Add stats to UnitStats

File: `src/units/UnitTypes.ts`

```typescript
[UnitType.{{Name}}]: {
  type: UnitType.{{Name}},
  domain: UnitDomain.{{Domain}},
  health: {{health}},
  maxHealth: {{health}},
  movement: {{movement}},
  maxMovement: {{movement}},
  attack: {{attack}},
  defense: {{defense}},
  name: '{{Name}}',
  description: '{{description}}',
},
```

### 3. Verify integration

The unit will automatically work with:
- [x] Pathfinding (uses domain from UnitStats)
- [x] UnitManager spawning
- [x] UnitRenderer (default mesh)
- [x] Turn system

### 4. Optional: Custom rendering

If the unit needs a unique appearance, update `UnitRenderer.createUnitMesh()`:

```typescript
private createUnitMesh(unit: UnitData): THREE.Mesh {
  if (unit.type === UnitType.{{Name}}) {
    // Custom geometry
  }
  // ...default geometry
}
```

### 5. Add tests

File: `tests/units/UnitTypes.test.ts`

```typescript
describe('{{Name}}', () => {
  it('should have correct domain', () => {
    expect(getUnitDomain(UnitType.{{Name}})).toBe(UnitDomain.{{Domain}});
  });
});
```

## Checklist

- [ ] Added to UnitType enum
- [ ] Added stats to UnitStats record
- [ ] Verified domain is correct
- [ ] Stats are balanced against existing units
- [ ] Tests pass: `npm run test`
- [ ] Type check passes: `npm run typecheck`
