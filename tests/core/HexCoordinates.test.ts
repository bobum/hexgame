import { describe, it, expect } from 'vitest';
import { HexCoordinates } from '../../src/core/HexCoordinates';
import { HexDirection } from '../../src/core/HexDirection';

describe('HexCoordinates', () => {
  describe('constructor', () => {
    it('should create coordinates with q and r', () => {
      const hex = new HexCoordinates(3, -2);
      expect(hex.q).toBe(3);
      expect(hex.r).toBe(-2);
    });

    it('should calculate s as -q - r', () => {
      const hex = new HexCoordinates(3, -2);
      expect(hex.s).toBe(-1); // -3 - (-2) = -1
    });

    it('should satisfy q + r + s = 0 constraint', () => {
      const testCases = [
        [0, 0], [1, 0], [0, 1], [-1, 1], [5, -3], [-7, 4]
      ];
      for (const [q, r] of testCases) {
        const hex = new HexCoordinates(q, r);
        expect(hex.q + hex.r + hex.s).toBe(0);
      }
    });
  });

  describe('fromOffset', () => {
    it('should convert offset (0,0) to cube (0,0)', () => {
      const hex = HexCoordinates.fromOffset(0, 0);
      expect(hex.q).toBe(0);
      expect(hex.r).toBe(0);
    });

    it('should handle odd rows correctly', () => {
      // Row 1 is odd, so col 1 becomes q = 1 - floor(1/2) = 1 - 0 = 1
      const hex = HexCoordinates.fromOffset(1, 1);
      expect(hex.q).toBe(1);
      expect(hex.r).toBe(1);
    });

    it('should handle even rows correctly', () => {
      // Row 2 is even, so col 2 becomes q = 2 - floor(2/2) = 2 - 1 = 1
      const hex = HexCoordinates.fromOffset(2, 2);
      expect(hex.q).toBe(1);
      expect(hex.r).toBe(2);
    });
  });

  describe('round', () => {
    it('should round exact coordinates to themselves', () => {
      const hex = HexCoordinates.round(3, -2);
      expect(hex.q).toBe(3);
      expect(hex.r).toBe(-2);
    });

    it('should round fractional coordinates to nearest hex', () => {
      const hex = HexCoordinates.round(2.3, -1.1);
      expect(hex.q).toBe(2);
      expect(hex.r).toBe(-1);
    });

    it('should handle edge cases at hex boundaries', () => {
      // Test near the boundary between hexes
      const hex = HexCoordinates.round(0.5, 0.5);
      // Should round to a valid hex that satisfies q + r + s = 0
      expect(hex.q + hex.r + hex.s).toBe(0);
    });
  });

  describe('getNeighbor', () => {
    // DirectionOffsets are [q, r, s]: NE=[1,0,-1], E=[1,-1,0], SE=[0,-1,1], SW=[-1,0,1], W=[-1,1,0], NW=[0,1,-1]
    it('should return correct NE neighbor', () => {
      const center = new HexCoordinates(0, 0);
      const neighbor = center.getNeighbor(HexDirection.NE);
      expect(neighbor.q).toBe(1);
      expect(neighbor.r).toBe(0);
    });

    it('should return correct E neighbor', () => {
      const center = new HexCoordinates(0, 0);
      const neighbor = center.getNeighbor(HexDirection.E);
      expect(neighbor.q).toBe(1);
      expect(neighbor.r).toBe(-1);
    });

    it('should return correct SE neighbor', () => {
      const center = new HexCoordinates(0, 0);
      const neighbor = center.getNeighbor(HexDirection.SE);
      expect(neighbor.q).toBe(0);
      expect(neighbor.r).toBe(-1);
    });

    it('should return correct SW neighbor', () => {
      const center = new HexCoordinates(0, 0);
      const neighbor = center.getNeighbor(HexDirection.SW);
      expect(neighbor.q).toBe(-1);
      expect(neighbor.r).toBe(0);
    });

    it('should return correct W neighbor', () => {
      const center = new HexCoordinates(0, 0);
      const neighbor = center.getNeighbor(HexDirection.W);
      expect(neighbor.q).toBe(-1);
      expect(neighbor.r).toBe(1);
    });

    it('should return correct NW neighbor', () => {
      const center = new HexCoordinates(0, 0);
      const neighbor = center.getNeighbor(HexDirection.NW);
      expect(neighbor.q).toBe(0);
      expect(neighbor.r).toBe(1);
    });
  });

  describe('getNeighbors', () => {
    it('should return exactly 6 neighbors', () => {
      const center = new HexCoordinates(0, 0);
      const neighbors = center.getNeighbors();
      expect(neighbors).toHaveLength(6);
    });

    it('should return all unique neighbors', () => {
      const center = new HexCoordinates(0, 0);
      const neighbors = center.getNeighbors();
      const keys = neighbors.map(n => n.toKey());
      const uniqueKeys = new Set(keys);
      expect(uniqueKeys.size).toBe(6);
    });

    it('should return neighbors at distance 1', () => {
      const center = new HexCoordinates(5, -3);
      const neighbors = center.getNeighbors();
      for (const neighbor of neighbors) {
        expect(center.distanceTo(neighbor)).toBe(1);
      }
    });
  });

  describe('distanceTo', () => {
    it('should return 0 for same coordinates', () => {
      const hex = new HexCoordinates(3, -2);
      expect(hex.distanceTo(hex)).toBe(0);
    });

    it('should return 1 for adjacent hexes', () => {
      const hex = new HexCoordinates(0, 0);
      const neighbor = hex.getNeighbor(HexDirection.E);
      expect(hex.distanceTo(neighbor)).toBe(1);
    });

    it('should return correct distance for non-adjacent hexes', () => {
      const a = new HexCoordinates(0, 0);
      const b = new HexCoordinates(3, -1);
      expect(a.distanceTo(b)).toBe(3);
    });

    it('should be symmetric', () => {
      const a = new HexCoordinates(2, -5);
      const b = new HexCoordinates(-3, 4);
      expect(a.distanceTo(b)).toBe(b.distanceTo(a));
    });
  });

  describe('ring', () => {
    it('should return center for radius 0', () => {
      const center = new HexCoordinates(0, 0);
      const ring = HexCoordinates.ring(center, 0);
      expect(ring).toHaveLength(1);
      expect(ring[0].equals(center)).toBe(true);
    });

    it('should return 6 hexes for radius 1', () => {
      const center = new HexCoordinates(0, 0);
      const ring = HexCoordinates.ring(center, 1);
      expect(ring).toHaveLength(6);
    });

    it('should return 12 hexes for radius 2', () => {
      const center = new HexCoordinates(0, 0);
      const ring = HexCoordinates.ring(center, 2);
      expect(ring).toHaveLength(12);
    });

    it('should return 6*radius hexes for any radius > 0', () => {
      const center = new HexCoordinates(0, 0);
      for (const radius of [1, 2, 3, 5, 10]) {
        const ring = HexCoordinates.ring(center, radius);
        expect(ring).toHaveLength(6 * radius);
      }
    });

    it('should return unique hexes in a ring', () => {
      const center = new HexCoordinates(0, 0);
      const ring = HexCoordinates.ring(center, 3);
      const keys = ring.map(h => h.toKey());
      const unique = new Set(keys);
      expect(unique.size).toBe(ring.length);
    });
  });

  describe('spiral', () => {
    it('should include center as first element', () => {
      const center = new HexCoordinates(2, -1);
      const spiral = HexCoordinates.spiral(center, 2);
      expect(spiral[0].equals(center)).toBe(true);
    });

    it('should return correct count for radius', () => {
      const center = new HexCoordinates(0, 0);
      // Total = 1 + 6 + 12 + 18 = 1 + 6*(1+2+3) = 1 + 36 = 37
      const spiral = HexCoordinates.spiral(center, 3);
      expect(spiral).toHaveLength(37);
    });

    it('should return 1 + 3*radius*(radius+1) hexes', () => {
      const center = new HexCoordinates(0, 0);
      for (const radius of [0, 1, 2, 3, 5]) {
        const spiral = HexCoordinates.spiral(center, radius);
        const expected = 1 + 3 * radius * (radius + 1);
        expect(spiral).toHaveLength(expected);
      }
    });
  });

  describe('equals', () => {
    it('should return true for same coordinates', () => {
      const a = new HexCoordinates(3, -2);
      const b = new HexCoordinates(3, -2);
      expect(a.equals(b)).toBe(true);
    });

    it('should return false for different coordinates', () => {
      const a = new HexCoordinates(3, -2);
      const b = new HexCoordinates(3, -1);
      expect(a.equals(b)).toBe(false);
    });
  });

  describe('toKey / fromKey', () => {
    it('should create consistent key format', () => {
      const hex = new HexCoordinates(5, -3);
      expect(hex.toKey()).toBe('5,-3');
    });

    it('should round-trip through key', () => {
      const original = new HexCoordinates(-7, 12);
      const key = original.toKey();
      const restored = HexCoordinates.fromKey(key);
      expect(restored.equals(original)).toBe(true);
    });

    it('should handle negative coordinates', () => {
      const hex = new HexCoordinates(-5, -3);
      const key = hex.toKey();
      const restored = HexCoordinates.fromKey(key);
      expect(restored.q).toBe(-5);
      expect(restored.r).toBe(-3);
    });
  });

  describe('toWorldPosition', () => {
    it('should return THREE.Vector3', () => {
      const hex = new HexCoordinates(0, 0);
      const pos = hex.toWorldPosition();
      expect(pos.x).toBeDefined();
      expect(pos.y).toBeDefined();
      expect(pos.z).toBeDefined();
    });

    it('should place origin hex at world origin', () => {
      const hex = new HexCoordinates(0, 0);
      const pos = hex.toWorldPosition(0);
      expect(pos.x).toBeCloseTo(0);
      expect(pos.y).toBeCloseTo(0);
      expect(pos.z).toBeCloseTo(0);
    });

    it('should apply elevation to y coordinate', () => {
      const hex = new HexCoordinates(0, 0);
      const pos0 = hex.toWorldPosition(0);
      const pos5 = hex.toWorldPosition(5);
      expect(pos5.y).toBeGreaterThan(pos0.y);
    });
  });

  describe('toString', () => {
    it('should include all three coordinates', () => {
      const hex = new HexCoordinates(3, -2);
      const str = hex.toString();
      expect(str).toContain('3');
      expect(str).toContain('-2');
      expect(str).toContain('-1'); // s value
    });
  });
});
