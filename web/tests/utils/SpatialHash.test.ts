import { describe, it, expect, beforeEach } from 'vitest';
import { SpatialHash } from '../../src/utils/SpatialHash';

describe('SpatialHash', () => {
  let hash: SpatialHash<string>;

  beforeEach(() => {
    hash = new SpatialHash<string>(2); // Cell size of 2
  });

  describe('insert and getAt', () => {
    it('should insert and retrieve an item', () => {
      hash.insert('item1', 0, 0);
      const items = hash.getAt(0, 0);
      expect(items).toContain('item1');
    });

    it('should retrieve items from same bucket', () => {
      hash.insert('item1', 0.5, 0.5);
      hash.insert('item2', 1.0, 1.0);
      // Both should be in same bucket (cell size 2)
      const items = hash.getAt(0, 0);
      expect(items).toContain('item1');
      expect(items).toContain('item2');
    });

    it('should not retrieve items from different buckets', () => {
      hash.insert('item1', 0, 0);
      hash.insert('item2', 5, 5);
      const items = hash.getAt(0, 0);
      expect(items).toContain('item1');
      expect(items).not.toContain('item2');
    });

    it('should return empty array for empty position', () => {
      const items = hash.getAt(100, 100);
      expect(items).toHaveLength(0);
    });
  });

  describe('getFirstAt', () => {
    it('should return first item at position', () => {
      hash.insert('item1', 0, 0);
      const item = hash.getFirstAt(0, 0);
      expect(item).toBe('item1');
    });

    it('should return null for empty position', () => {
      const item = hash.getFirstAt(100, 100);
      expect(item).toBeNull();
    });
  });

  describe('hasAt', () => {
    it('should return true when items exist', () => {
      hash.insert('item1', 0, 0);
      expect(hash.hasAt(0, 0)).toBe(true);
    });

    it('should return false when no items exist', () => {
      expect(hash.hasAt(0, 0)).toBe(false);
    });
  });

  describe('remove', () => {
    it('should remove an item', () => {
      hash.insert('item1', 0, 0);
      hash.remove('item1');
      expect(hash.getAt(0, 0)).toHaveLength(0);
    });

    it('should return true when item was removed', () => {
      hash.insert('item1', 0, 0);
      expect(hash.remove('item1')).toBe(true);
    });

    it('should return false when item did not exist', () => {
      expect(hash.remove('nonexistent')).toBe(false);
    });

    it('should clean up empty buckets', () => {
      hash.insert('item1', 0, 0);
      const bucketsBefore = hash.bucketCount;
      hash.remove('item1');
      expect(hash.bucketCount).toBeLessThan(bucketsBefore);
    });
  });

  describe('update', () => {
    it('should move item to new position', () => {
      hash.insert('item1', 0, 0);
      hash.update('item1', 10, 10);

      expect(hash.getAt(0, 0)).not.toContain('item1');
      expect(hash.getAt(10, 10)).toContain('item1');
    });

    it('should update position correctly', () => {
      hash.insert('item1', 0, 0);
      hash.update('item1', 5, 7);

      const pos = hash.getPosition('item1');
      expect(pos).toEqual({ x: 5, z: 7 });
    });
  });

  describe('queryRadius', () => {
    beforeEach(() => {
      // Insert items in a pattern
      hash.insert('center', 0, 0);
      hash.insert('near', 1, 1);
      hash.insert('far', 10, 10);
    });

    it('should find items within radius', () => {
      const results = hash.queryRadius(0, 0, 2);
      expect(results).toContain('center');
      expect(results).toContain('near');
    });

    it('should not find items outside radius', () => {
      const results = hash.queryRadius(0, 0, 2);
      expect(results).not.toContain('far');
    });

    it('should return empty array when no items in range', () => {
      const results = hash.queryRadius(100, 100, 1);
      expect(results).toHaveLength(0);
    });

    it('should use squared distance correctly', () => {
      // Item at (1, 1) is sqrt(2) ≈ 1.41 away from origin
      const results = hash.queryRadius(0, 0, 1.5);
      expect(results).toContain('near');
    });
  });

  describe('queryRect', () => {
    beforeEach(() => {
      hash.insert('inside', 5, 5);
      hash.insert('outside', 15, 15);
      hash.insert('edge', 10, 10);
    });

    it('should find items inside rectangle', () => {
      const results = hash.queryRect(0, 0, 10, 10);
      expect(results).toContain('inside');
      expect(results).toContain('edge');
    });

    it('should not find items outside rectangle', () => {
      const results = hash.queryRect(0, 0, 10, 10);
      expect(results).not.toContain('outside');
    });

    it('should include items on boundary', () => {
      const results = hash.queryRect(5, 5, 5, 5);
      expect(results).toContain('inside');
    });
  });

  describe('getAll', () => {
    it('should return all items', () => {
      hash.insert('a', 0, 0);
      hash.insert('b', 5, 5);
      hash.insert('c', 10, 10);

      const all = hash.getAll();
      expect(all).toHaveLength(3);
      expect(all).toContain('a');
      expect(all).toContain('b');
      expect(all).toContain('c');
    });

    it('should return empty array when empty', () => {
      expect(hash.getAll()).toHaveLength(0);
    });
  });

  describe('getPosition', () => {
    it('should return correct position', () => {
      hash.insert('item', 3.5, 7.2);
      const pos = hash.getPosition('item');
      expect(pos).toEqual({ x: 3.5, z: 7.2 });
    });

    it('should return null for unknown item', () => {
      expect(hash.getPosition('unknown')).toBeNull();
    });
  });

  describe('has', () => {
    it('should return true for inserted item', () => {
      hash.insert('item', 0, 0);
      expect(hash.has('item')).toBe(true);
    });

    it('should return false for unknown item', () => {
      expect(hash.has('unknown')).toBe(false);
    });
  });

  describe('clear', () => {
    it('should remove all items', () => {
      hash.insert('a', 0, 0);
      hash.insert('b', 5, 5);
      hash.clear();

      expect(hash.size).toBe(0);
      expect(hash.bucketCount).toBe(0);
    });
  });

  describe('size and bucketCount', () => {
    it('should track item count', () => {
      expect(hash.size).toBe(0);
      hash.insert('a', 0, 0);
      expect(hash.size).toBe(1);
      hash.insert('b', 5, 5);
      expect(hash.size).toBe(2);
    });

    it('should track bucket count', () => {
      hash.insert('a', 0, 0);
      hash.insert('b', 0.5, 0.5); // Same bucket
      hash.insert('c', 10, 10); // Different bucket

      expect(hash.bucketCount).toBe(2);
    });
  });

  describe('stats', () => {
    it('should track insert count', () => {
      hash.insert('a', 0, 0);
      hash.insert('b', 1, 1);
      expect(hash.stats.insertCount).toBe(2);
    });

    it('should track query count', () => {
      hash.getAt(0, 0);
      hash.queryRadius(0, 0, 5);
      hash.queryRect(0, 0, 10, 10);
      expect(hash.stats.queryCount).toBe(3);
    });

    it('should track peak items', () => {
      hash.insert('a', 0, 0);
      hash.insert('b', 1, 1);
      hash.insert('c', 2, 2);
      hash.remove('a');
      hash.remove('b');
      expect(hash.stats.peakItems).toBe(3);
    });

    it('should reset stats', () => {
      hash.insert('a', 0, 0);
      hash.getAt(0, 0);
      hash.resetStats();

      expect(hash.stats.insertCount).toBe(0);
      expect(hash.stats.queryCount).toBe(0);
    });
  });

  describe('cell size behavior', () => {
    it('should bucket items based on cell size', () => {
      const smallHash = new SpatialHash<string>(1);
      const largeHash = new SpatialHash<string>(10);

      // Items 5 units apart
      smallHash.insert('a', 0, 0);
      smallHash.insert('b', 5, 0);
      largeHash.insert('a', 0, 0);
      largeHash.insert('b', 5, 0);

      // Small hash: different buckets
      expect(smallHash.getAt(0, 0)).not.toContain('b');

      // Large hash: same bucket
      expect(largeHash.getAt(0, 0)).toContain('a');
      expect(largeHash.getAt(0, 0)).toContain('b');
    });
  });

  describe('negative coordinates', () => {
    it('should handle negative positions', () => {
      hash.insert('neg', -5, -5);
      expect(hash.getAt(-5, -5)).toContain('neg');
    });

    it('should query across negative coordinates', () => {
      hash.insert('neg', -3, -3);
      hash.insert('pos', 3, 3);

      const results = hash.queryRadius(0, 0, 5);
      expect(results).toContain('neg');
      expect(results).toContain('pos');
    });
  });
});
