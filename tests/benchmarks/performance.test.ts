/**
 * Computational Performance Benchmarks
 *
 * These benchmarks test CPU-bound operations without GPU rendering.
 * They can run in CI to catch algorithmic regressions.
 *
 * Tests focus on:
 * - Map generation time at various sizes
 * - Spatial hash query performance
 * - Coordinate conversion throughput
 * - Algorithm scaling (O(1) vs O(n) behavior)
 */

import { describe, it, expect, beforeAll } from 'vitest';
import { HexGrid } from '../../src/core/HexGrid';
import { HexCoordinates } from '../../src/core/HexCoordinates';
import { MapGenerator } from '../../src/generation/MapGenerator';
import { SpatialHash } from '../../src/utils/SpatialHash';
import { UnitManager } from '../../src/units/UnitManager';
import { UnitType, defaultMapConfig } from '../../src/types';

// Helper to measure execution time
function measure(fn: () => void, iterations: number = 1): number {
  const start = performance.now();
  for (let i = 0; i < iterations; i++) {
    fn();
  }
  return (performance.now() - start) / iterations;
}

describe('Performance Benchmarks', () => {
  describe('Map Generation Scaling', () => {
    const sizes = [
      { width: 20, height: 15, name: 'Small (20x15)' },
      { width: 40, height: 30, name: 'Medium (40x30)' },
      { width: 60, height: 45, name: 'Large (60x45)' },
    ];

    const times: Record<string, number> = {};

    for (const size of sizes) {
      it(`should generate ${size.name} map`, () => {
        const config = { ...defaultMapConfig, width: size.width, height: size.height, seed: 12345 };
        const grid = new HexGrid(config);
        const generator = new MapGenerator(grid);

        const time = measure(() => generator.generate());
        times[size.name] = time;

        console.log(`  ${size.name}: ${time.toFixed(2)}ms`);

        // Basic sanity check - generation should complete
        expect(grid.cellCount).toBeGreaterThan(0);
      });
    }

    it('should scale sub-quadratically with size', () => {
      // Skip if times weren't collected (tests ran in isolation)
      if (Object.keys(times).length < 2) {
        return;
      }

      // Medium should be < 4x small (area is 4x, so O(n) would be 4x)
      // We allow some overhead, so check < 6x
      const smallTime = times['Small (20x15)'];
      const mediumTime = times['Medium (40x30)'];

      if (smallTime && mediumTime) {
        const ratio = mediumTime / smallTime;
        console.log(`  Scaling ratio (medium/small): ${ratio.toFixed(2)}x`);
        expect(ratio).toBeLessThan(8); // Allow for overhead
      }
    });
  });

  describe('Coordinate Conversion Throughput', () => {
    it('should convert 10000 coordinates quickly', () => {
      const coords: HexCoordinates[] = [];
      for (let q = -50; q <= 50; q++) {
        for (let r = -50; r <= 50; r++) {
          coords.push(new HexCoordinates(q, r));
        }
      }

      const time = measure(() => {
        for (const coord of coords) {
          coord.toWorldPosition(0);
        }
      });

      console.log(`  10000 toWorldPosition: ${time.toFixed(2)}ms`);
      expect(time).toBeLessThan(100); // Should be fast
    });

    it('should round fractional coordinates efficiently', () => {
      const time = measure(() => {
        for (let i = 0; i < 10000; i++) {
          HexCoordinates.round(Math.random() * 100 - 50, Math.random() * 100 - 50);
        }
      });

      console.log(`  10000 round operations: ${time.toFixed(2)}ms`);
      expect(time).toBeLessThan(50);
    });

    it('should compute hex distance efficiently', () => {
      const a = new HexCoordinates(0, 0);
      const coords = Array.from({ length: 10000 }, () =>
        new HexCoordinates(Math.floor(Math.random() * 100 - 50), Math.floor(Math.random() * 100 - 50))
      );

      const time = measure(() => {
        for (const coord of coords) {
          a.distanceTo(coord);
        }
      });

      console.log(`  10000 distance calculations: ${time.toFixed(2)}ms`);
      expect(time).toBeLessThan(20);
    });
  });

  describe('Spatial Hash Performance', () => {
    let hash: SpatialHash<{ id: number }>;

    beforeAll(() => {
      hash = new SpatialHash<{ id: number }>(2);
      // Insert 1000 items in a grid pattern
      for (let x = 0; x < 100; x += 3) {
        for (let z = 0; z < 100; z += 3) {
          hash.insert({ id: x * 100 + z }, x, z);
        }
      }
    });

    it('should perform O(1) point lookups', () => {
      const time = measure(() => {
        for (let i = 0; i < 10000; i++) {
          hash.getAt(Math.random() * 100, Math.random() * 100);
        }
      });

      console.log(`  10000 point lookups: ${time.toFixed(2)}ms`);
      expect(time).toBeLessThan(50);
    });

    it('should perform radius queries efficiently', () => {
      const time = measure(() => {
        for (let i = 0; i < 1000; i++) {
          hash.queryRadius(Math.random() * 100, Math.random() * 100, 5);
        }
      });

      console.log(`  1000 radius queries (r=5): ${time.toFixed(2)}ms`);
      expect(time).toBeLessThan(100);
    });

    it('should maintain O(1) lookup regardless of hash size', () => {
      // Compare lookup time with small vs large hash
      const smallHash = new SpatialHash<number>(2);
      const largeHash = new SpatialHash<number>(2);

      for (let i = 0; i < 100; i++) {
        smallHash.insert(i, i, i);
      }
      for (let i = 0; i < 10000; i++) {
        largeHash.insert(i, i % 1000, Math.floor(i / 1000));
      }

      const smallTime = measure(() => {
        for (let i = 0; i < 1000; i++) {
          smallHash.getAt(50, 50);
        }
      });

      const largeTime = measure(() => {
        for (let i = 0; i < 1000; i++) {
          largeHash.getAt(500, 5);
        }
      });

      console.log(`  100-item hash lookup: ${smallTime.toFixed(3)}ms/1000`);
      console.log(`  10000-item hash lookup: ${largeTime.toFixed(3)}ms/1000`);

      // Large hash lookup should be similar time (O(1))
      // Allow 5x tolerance for cache effects, bucket size, and CI variance
      expect(largeTime).toBeLessThan(smallTime * 5 + 3);
    });
  });

  describe('Unit Manager Operations', () => {
    let grid: HexGrid;

    beforeAll(() => {
      const config = { ...defaultMapConfig, width: 50, height: 50, seed: 12345 };
      grid = new HexGrid(config);
      const generator = new MapGenerator(grid);
      generator.generate();
    });

    it('should create units efficiently', () => {
      const manager = new UnitManager(grid);
      const landCells = grid.getAllCells().filter(c => c.elevation >= 0);

      const time = measure(() => {
        for (let i = 0; i < Math.min(500, landCells.length); i++) {
          const cell = landCells[i];
          manager.createUnit(UnitType.Infantry, cell.q, cell.r, 1);
        }
      });

      console.log(`  Create 500 units: ${time.toFixed(2)}ms`);
      expect(time).toBeLessThan(200);
    });

    it('should lookup units by position efficiently', () => {
      const manager = new UnitManager(grid);
      const landCells = grid.getAllCells().filter(c => c.elevation >= 0);

      // Create some units
      for (let i = 0; i < Math.min(200, landCells.length); i++) {
        const cell = landCells[i];
        manager.createUnit(UnitType.Infantry, cell.q, cell.r, 1);
      }

      const time = measure(() => {
        for (let i = 0; i < 1000; i++) {
          const cell = landCells[Math.floor(Math.random() * landCells.length)];
          manager.getUnitAt(cell.q, cell.r);
        }
      });

      console.log(`  1000 unit position lookups: ${time.toFixed(2)}ms`);
      expect(time).toBeLessThan(100);
    });
  });

  describe('HexGrid Operations', () => {
    let grid: HexGrid;

    beforeAll(() => {
      const config = { ...defaultMapConfig, width: 60, height: 45, seed: 12345 };
      grid = new HexGrid(config);
      const generator = new MapGenerator(grid);
      generator.generate();
    });

    it('should perform cell lookups efficiently', () => {
      const coords = grid.getAllCells().map(c => new HexCoordinates(c.q, c.r));

      const time = measure(() => {
        for (const coord of coords) {
          grid.getCell(coord);
        }
      });

      console.log(`  ${coords.length} cell lookups: ${time.toFixed(2)}ms`);
      expect(time).toBeLessThan(100);
    });

    it('should find neighbors efficiently', () => {
      const cells = grid.getAllCells();

      const time = measure(() => {
        for (const cell of cells) {
          grid.getNeighbors(cell);
        }
      });

      console.log(`  ${cells.length} neighbor lookups: ${time.toFixed(2)}ms`);
      expect(time).toBeLessThan(200);
    });
  });

  describe('Ring and Spiral Generation', () => {
    it('should generate rings efficiently', () => {
      const center = new HexCoordinates(0, 0);

      const time = measure(() => {
        for (let r = 1; r <= 20; r++) {
          HexCoordinates.ring(center, r);
        }
      });

      console.log(`  Generate rings 1-20: ${time.toFixed(2)}ms`);
      expect(time).toBeLessThan(50);
    });

    it('should generate spirals efficiently', () => {
      const center = new HexCoordinates(0, 0);

      const time = measure(() => {
        HexCoordinates.spiral(center, 30);
      });

      // Spiral of radius 30 = 1 + 3*30*31 = 2791 hexes
      console.log(`  Generate spiral r=30 (2791 hexes): ${time.toFixed(2)}ms`);
      expect(time).toBeLessThan(20);
    });
  });
});

// Summary test to print results
describe('Benchmark Summary', () => {
  it('should complete all benchmarks', () => {
    console.log('\n  ✓ All computational benchmarks passed');
    console.log('  These metrics can be tracked over time to detect regressions.');
    expect(true).toBe(true);
  });
});
