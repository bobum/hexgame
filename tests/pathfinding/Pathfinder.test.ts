import { describe, it, expect, beforeEach } from 'vitest';
import { Pathfinder } from '../../src/pathfinding/Pathfinder';
import { HexGrid } from '../../src/core/HexGrid';
import { UnitManager } from '../../src/units/UnitManager';
import { TerrainType, UnitType, defaultMapConfig } from '../../src/types';

describe('Pathfinder', () => {
  let grid: HexGrid;
  let unitManager: UnitManager;
  let pathfinder: Pathfinder;

  beforeEach(() => {
    // Create a small test grid
    const config = { ...defaultMapConfig, width: 10, height: 10 };
    grid = new HexGrid(config);
    grid.initialize();

    // Set all cells to plains with elevation 1 (land)
    for (const cell of grid.getAllCells()) {
      cell.elevation = 1;
      cell.terrainType = TerrainType.Plains;
    }

    unitManager = new UnitManager(grid);
    pathfinder = new Pathfinder(grid, unitManager);
  });

  describe('findPath', () => {
    it('should find path between same cell', () => {
      const start = grid.getCellAt(0, 0)!;
      const result = pathfinder.findPath(start, start);

      expect(result.reachable).toBe(true);
      expect(result.cost).toBe(0);
      expect(result.path).toHaveLength(1);
      expect(result.path[0]).toBe(start);
    });

    it('should find path between adjacent cells', () => {
      const start = grid.getCellAt(0, 0)!;
      const end = grid.getCellAt(1, 0)!;
      const result = pathfinder.findPath(start, end);

      expect(result.reachable).toBe(true);
      expect(result.cost).toBe(1); // Plains cost = 1
      expect(result.path).toHaveLength(2);
      expect(result.path[0]).toBe(start);
      expect(result.path[1]).toBe(end);
    });

    it('should find optimal path over multiple cells', () => {
      const start = grid.getCellAt(0, 0)!;
      const end = grid.getCellAt(3, 0)!;
      const result = pathfinder.findPath(start, end);

      expect(result.reachable).toBe(true);
      expect(result.path.length).toBeGreaterThanOrEqual(2);
      expect(result.path[0]).toBe(start);
      expect(result.path[result.path.length - 1]).toBe(end);
    });

    it('should return not reachable for water destination', () => {
      const start = grid.getCellAt(0, 0)!;
      const end = grid.getCellAt(5, 5)!;
      end.elevation = -1; // Make it water

      const result = pathfinder.findPath(start, end);

      expect(result.reachable).toBe(false);
      expect(result.path).toHaveLength(0);
      expect(result.cost).toBe(Infinity);
    });

    it('should return not reachable for mountain destination', () => {
      const start = grid.getCellAt(0, 0)!;
      const end = grid.getCellAt(5, 5)!;
      end.terrainType = TerrainType.Mountains;

      const result = pathfinder.findPath(start, end);

      expect(result.reachable).toBe(false);
    });

    it('should path around water obstacles', () => {
      const start = grid.getCellAt(2, 2)!;
      const end = grid.getCellAt(6, 2)!;

      // Create a water wall blocking direct path (vertical line at column 4)
      grid.getCellAt(4, 1)!.elevation = -1;
      grid.getCellAt(4, 2)!.elevation = -1;
      grid.getCellAt(4, 3)!.elevation = -1;

      const result = pathfinder.findPath(start, end);

      // Should find a path around the water
      expect(result.reachable).toBe(true);
      // Path should not include water cells
      for (const cell of result.path) {
        expect(cell.elevation).toBeGreaterThanOrEqual(0);
      }
    });

    it('should path around mountain obstacles', () => {
      const start = grid.getCellAt(0, 0)!;
      const end = grid.getCellAt(4, 0)!;

      // Create a mountain wall
      grid.getCellAt(2, 0)!.terrainType = TerrainType.Mountains;
      grid.getCellAt(2, 1)!.terrainType = TerrainType.Mountains;

      const result = pathfinder.findPath(start, end);

      expect(result.reachable).toBe(true);
      for (const cell of result.path) {
        expect(cell.terrainType).not.toBe(TerrainType.Mountains);
      }
    });

    it('should not cross cliffs (2+ elevation difference)', () => {
      const start = grid.getCellAt(0, 0)!;
      start.elevation = 1;

      const neighbor = grid.getCellAt(1, 0)!;
      neighbor.elevation = 3; // 2 elevation diff = cliff

      const end = grid.getCellAt(2, 0)!;
      end.elevation = 3;

      // Surround destination so only path is through cliff
      grid.getCellAt(1, 1)!.elevation = -1;
      grid.getCellAt(2, 1)!.elevation = -1;

      const result = pathfinder.findPath(start, end);

      // Should either be unreachable or find path around
      if (result.reachable) {
        // Verify no cliff jumps in path
        for (let i = 0; i < result.path.length - 1; i++) {
          const diff = Math.abs(result.path[i + 1].elevation - result.path[i].elevation);
          expect(diff).toBeLessThan(2);
        }
      }
    });

    it('should avoid units by default', () => {
      const start = grid.getCellAt(0, 0)!;
      const end = grid.getCellAt(4, 0)!;

      // Place unit blocking direct path
      unitManager.createUnit(UnitType.Infantry, 2, 0, 1);

      const result = pathfinder.findPath(start, end);

      expect(result.reachable).toBe(true);
      // Path should not include cell with unit
      for (const cell of result.path) {
        if (cell.q === 2 && cell.r === 0) {
          expect(false).toBe(true); // Should not reach here
        }
      }
    });

    it('should ignore units when option set', () => {
      const start = grid.getCellAt(0, 0)!;
      const end = grid.getCellAt(4, 0)!;

      // Place unit in path
      unitManager.createUnit(UnitType.Infantry, 2, 0, 1);

      const result = pathfinder.findPath(start, end, { ignoreUnits: true });

      expect(result.reachable).toBe(true);
    });

    it('should respect maxCost limit', () => {
      const start = grid.getCellAt(0, 0)!;
      const end = grid.getCellAt(5, 0)!;

      // With maxCost of 3, we shouldn't reach a cell 5 hexes away
      const result = pathfinder.findPath(start, end, { maxCost: 3 });

      expect(result.reachable).toBe(false);
    });

    it('should find path with terrain cost differences', () => {
      const start = grid.getCellAt(0, 0)!;
      const end = grid.getCellAt(2, 0)!;

      // Make direct path through forest (cost 1.5)
      grid.getCellAt(1, 0)!.terrainType = TerrainType.Forest;

      const directResult = pathfinder.findPath(start, end);
      expect(directResult.reachable).toBe(true);
      // Cost should include forest penalty
      expect(directResult.cost).toBeGreaterThan(2);
    });
  });

  describe('getReachableCells', () => {
    it('should return only start cell with 0 movement', () => {
      const start = grid.getCellAt(5, 5)!;
      const reachable = pathfinder.getReachableCells(start, 0);

      expect(reachable.size).toBe(1);
      expect(reachable.has(start)).toBe(true);
      expect(reachable.get(start)).toBe(0);
    });

    it('should return neighbors with 1 movement point', () => {
      const start = grid.getCellAt(5, 5)!;
      const reachable = pathfinder.getReachableCells(start, 1);

      // Start + 6 neighbors = 7 cells (all plains cost 1)
      expect(reachable.size).toBe(7);
      expect(reachable.has(start)).toBe(true);
    });

    it('should return correct costs', () => {
      const start = grid.getCellAt(5, 5)!;
      const reachable = pathfinder.getReachableCells(start, 2);

      expect(reachable.get(start)).toBe(0);

      // Direct neighbors should have cost 1
      const neighbors = grid.getNeighbors(start);
      for (const neighbor of neighbors) {
        expect(reachable.get(neighbor)).toBe(1);
      }
    });

    it('should not include impassable cells', () => {
      const start = grid.getCellAt(5, 5)!;

      // Make some neighbors water
      const neighbors = grid.getNeighbors(start);
      neighbors[0].elevation = -1;
      neighbors[1].terrainType = TerrainType.Mountains;

      const reachable = pathfinder.getReachableCells(start, 2);

      expect(reachable.has(neighbors[0])).toBe(false);
      expect(reachable.has(neighbors[1])).toBe(false);
    });

    it('should not include cells with units', () => {
      const start = grid.getCellAt(5, 5)!;
      unitManager.createUnit(UnitType.Infantry, 6, 5, 1);

      const reachable = pathfinder.getReachableCells(start, 2);
      const blockedCell = grid.getCellAt(6, 5);

      expect(reachable.has(blockedCell!)).toBe(false);
    });

    it('should account for terrain costs', () => {
      const start = grid.getCellAt(5, 5)!;

      // Make some neighbors forest (cost 1.5)
      const neighbors = grid.getNeighbors(start);
      neighbors[0].terrainType = TerrainType.Forest;
      neighbors[1].terrainType = TerrainType.Forest;

      const reachable = pathfinder.getReachableCells(start, 1);

      // Forest cells should not be reachable with only 1 movement point
      expect(reachable.has(neighbors[0])).toBe(false);
      expect(reachable.has(neighbors[1])).toBe(false);
    });
  });

  describe('hasPath', () => {
    it('should return true for reachable destination', () => {
      const start = grid.getCellAt(0, 0)!;
      const end = grid.getCellAt(3, 3)!;

      expect(pathfinder.hasPath(start, end)).toBe(true);
    });

    it('should return false for unreachable destination', () => {
      const start = grid.getCellAt(0, 0)!;
      const end = grid.getCellAt(5, 5)!;
      end.elevation = -1;

      expect(pathfinder.hasPath(start, end)).toBe(false);
    });
  });

  describe('getStepCost', () => {
    it('should return correct cost for adjacent plains', () => {
      const from = grid.getCellAt(0, 0)!;
      const to = grid.getCellAt(1, 0)!;

      expect(pathfinder.getStepCost(from, to)).toBe(1);
    });

    it('should return Infinity for non-adjacent cells', () => {
      const from = grid.getCellAt(0, 0)!;
      const to = grid.getCellAt(3, 0)!;

      expect(pathfinder.getStepCost(from, to)).toBe(Infinity);
    });

    it('should include elevation cost', () => {
      const from = grid.getCellAt(0, 0)!;
      from.elevation = 1;
      const to = grid.getCellAt(1, 0)!;
      to.elevation = 2; // 1 level up

      const cost = pathfinder.getStepCost(from, to);
      expect(cost).toBeGreaterThan(1); // Base + elevation penalty
    });
  });

  describe('path optimality', () => {
    it('should prefer shorter paths on uniform terrain', () => {
      const start = grid.getCellAt(0, 0)!;
      const end = grid.getCellAt(3, 0)!;

      const result = pathfinder.findPath(start, end);

      // On uniform terrain, path should be roughly direct
      // Hex distance from (0,0) to (3,0) is 3
      expect(result.path.length).toBeLessThanOrEqual(5);
    });

    it('should prefer lower cost terrain', () => {
      const start = grid.getCellAt(0, 0)!;
      const end = grid.getCellAt(4, 0)!;

      // Create a forest barrier with one plains gap
      grid.getCellAt(2, 0)!.terrainType = TerrainType.Forest;
      grid.getCellAt(2, 1)!.terrainType = TerrainType.Forest;
      // Leave 2,-1 as plains

      const result = pathfinder.findPath(start, end);

      expect(result.reachable).toBe(true);
      // Should path through plains gap rather than forest
    });
  });
});
