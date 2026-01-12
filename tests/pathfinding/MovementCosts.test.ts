import { describe, it, expect } from 'vitest';
import {
  TerrainCosts,
  getMovementCost,
  isPassable,
  canMoveBetween,
} from '../../src/pathfinding/MovementCosts';
import { TerrainType, HexCell } from '../../src/types';

// Helper to create a mock cell
function createCell(overrides: Partial<HexCell> = {}): HexCell {
  return {
    q: 0,
    r: 0,
    s: 0,
    elevation: 1,
    terrainType: TerrainType.Plains,
    moisture: 0.5,
    temperature: 0.5,
    features: [],
    riverDirections: [],
    ...overrides,
  };
}

describe('MovementCosts', () => {
  describe('TerrainCosts', () => {
    it('should have cost 1 for plains, coast, desert, savanna', () => {
      expect(TerrainCosts[TerrainType.Plains]).toBe(1);
      expect(TerrainCosts[TerrainType.Coast]).toBe(1);
      expect(TerrainCosts[TerrainType.Desert]).toBe(1);
      expect(TerrainCosts[TerrainType.Savanna]).toBe(1);
    });

    it('should have cost 1.5 for forest, taiga, tundra', () => {
      expect(TerrainCosts[TerrainType.Forest]).toBe(1.5);
      expect(TerrainCosts[TerrainType.Taiga]).toBe(1.5);
      expect(TerrainCosts[TerrainType.Tundra]).toBe(1.5);
    });

    it('should have cost 2 for hills, jungle', () => {
      expect(TerrainCosts[TerrainType.Hills]).toBe(2);
      expect(TerrainCosts[TerrainType.Jungle]).toBe(2);
    });

    it('should have cost 2.5 for snow', () => {
      expect(TerrainCosts[TerrainType.Snow]).toBe(2.5);
    });

    it('should have Infinity for mountains and ocean', () => {
      expect(TerrainCosts[TerrainType.Mountains]).toBe(Infinity);
      expect(TerrainCosts[TerrainType.Ocean]).toBe(Infinity);
    });
  });

  describe('getMovementCost', () => {
    it('should return base terrain cost for flat terrain', () => {
      const from = createCell({ elevation: 1 });
      const to = createCell({ elevation: 1, terrainType: TerrainType.Plains });

      expect(getMovementCost(from, to)).toBe(1);
    });

    it('should return Infinity for water (elevation < 0)', () => {
      const from = createCell({ elevation: 1 });
      const to = createCell({ elevation: -1 });

      expect(getMovementCost(from, to)).toBe(Infinity);
    });

    it('should return Infinity for mountains', () => {
      const from = createCell({ elevation: 1 });
      const to = createCell({ elevation: 1, terrainType: TerrainType.Mountains });

      expect(getMovementCost(from, to)).toBe(Infinity);
    });

    it('should return Infinity for ocean', () => {
      const from = createCell({ elevation: 1 });
      const to = createCell({ elevation: 0, terrainType: TerrainType.Ocean });

      expect(getMovementCost(from, to)).toBe(Infinity);
    });

    it('should add climbing penalty for uphill', () => {
      const from = createCell({ elevation: 1 });
      const to = createCell({ elevation: 2, terrainType: TerrainType.Plains });

      const cost = getMovementCost(from, to);
      expect(cost).toBe(1.5); // base 1 + 0.5 * 1 elevation
    });

    it('should not add penalty for downhill', () => {
      const from = createCell({ elevation: 2 });
      const to = createCell({ elevation: 1, terrainType: TerrainType.Plains });

      expect(getMovementCost(from, to)).toBe(1);
    });

    it('should return Infinity for cliffs (2+ elevation diff up)', () => {
      const from = createCell({ elevation: 1 });
      const to = createCell({ elevation: 3, terrainType: TerrainType.Plains });

      expect(getMovementCost(from, to)).toBe(Infinity);
    });

    it('should return Infinity for cliffs (2+ elevation diff down)', () => {
      const from = createCell({ elevation: 3 });
      const to = createCell({ elevation: 1, terrainType: TerrainType.Plains });

      expect(getMovementCost(from, to)).toBe(Infinity);
    });

    it('should combine terrain cost and elevation penalty', () => {
      const from = createCell({ elevation: 1 });
      const to = createCell({ elevation: 2, terrainType: TerrainType.Forest });

      const cost = getMovementCost(from, to);
      expect(cost).toBe(2); // forest 1.5 + 0.5 * 1 elevation
    });
  });

  describe('isPassable', () => {
    it('should return true for plains', () => {
      const cell = createCell({ terrainType: TerrainType.Plains });
      expect(isPassable(cell)).toBe(true);
    });

    it('should return true for forest', () => {
      const cell = createCell({ terrainType: TerrainType.Forest });
      expect(isPassable(cell)).toBe(true);
    });

    it('should return false for water', () => {
      const cell = createCell({ elevation: -1 });
      expect(isPassable(cell)).toBe(false);
    });

    it('should return false for mountains', () => {
      const cell = createCell({ terrainType: TerrainType.Mountains });
      expect(isPassable(cell)).toBe(false);
    });

    it('should return false for ocean', () => {
      const cell = createCell({ terrainType: TerrainType.Ocean });
      expect(isPassable(cell)).toBe(false);
    });
  });

  describe('canMoveBetween', () => {
    it('should return true for passable terrain', () => {
      const from = createCell({ elevation: 1 });
      const to = createCell({ elevation: 1, terrainType: TerrainType.Plains });

      expect(canMoveBetween(from, to)).toBe(true);
    });

    it('should return false for impassable terrain', () => {
      const from = createCell({ elevation: 1 });
      const to = createCell({ elevation: -1 });

      expect(canMoveBetween(from, to)).toBe(false);
    });

    it('should return false for cliffs', () => {
      const from = createCell({ elevation: 1 });
      const to = createCell({ elevation: 4, terrainType: TerrainType.Plains });

      expect(canMoveBetween(from, to)).toBe(false);
    });
  });
});
