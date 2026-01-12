# Skill: Run Tests

Run the test suite with various options.

## Usage

```
/run-tests [options]
```

## Commands

### Run all tests

```bash
npm run test
```

### Run tests in watch mode

```bash
npm run test:watch
```

### Run with coverage

```bash
npm run test:coverage
```

### Run unit tests only (exclude benchmarks)

```bash
npm run test:unit
```

### Run specific test file

```bash
npx vitest run tests/core/HexCoordinates.test.ts
```

### Run tests matching pattern

```bash
npx vitest run -t "pathfinding"
```

## Test Structure

Tests mirror the source structure:

```
tests/
├── core/
│   └── HexCoordinates.test.ts
├── pathfinding/
│   ├── Pathfinder.test.ts
│   ├── MovementCosts.test.ts
│   └── PriorityQueue.test.ts
├── units/
│   └── UnitManager.test.ts
├── utils/
│   └── SpatialHash.test.ts
└── benchmarks/
    └── performance.test.ts
```

## Coverage

Coverage excludes rendering and camera modules (Three.js dependent).

View coverage report:
```bash
npm run test:coverage
# Opens coverage/index.html
```

## Writing Tests

```typescript
import { describe, it, expect, beforeEach } from 'vitest';

describe('FeatureName', () => {
  beforeEach(() => {
    // Setup
  });

  it('should do expected behavior', () => {
    // Arrange
    const input = ...;

    // Act
    const result = functionUnderTest(input);

    // Assert
    expect(result).toBe(expectedValue);
  });
});
```

## Common Assertions

```typescript
expect(value).toBe(expected);           // Strict equality
expect(value).toEqual(expected);        // Deep equality
expect(value).toBeTruthy();             // Truthy check
expect(value).toBeCloseTo(num, digits); // Float comparison
expect(fn).toThrow();                   // Exception check
```
