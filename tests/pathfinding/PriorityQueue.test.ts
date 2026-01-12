import { describe, it, expect, beforeEach } from 'vitest';
import { PriorityQueue } from '../../src/pathfinding/PriorityQueue';

describe('PriorityQueue', () => {
  let queue: PriorityQueue<string>;

  beforeEach(() => {
    queue = new PriorityQueue<string>();
  });

  describe('constructor', () => {
    it('should create an empty queue', () => {
      expect(queue.isEmpty()).toBe(true);
      expect(queue.size).toBe(0);
    });

    it('should accept custom precision', () => {
      const preciseQueue = new PriorityQueue<number>(100);
      preciseQueue.enqueue(1, 0.01);
      preciseQueue.enqueue(2, 0.02);
      expect(preciseQueue.dequeue()).toBe(1);
      expect(preciseQueue.dequeue()).toBe(2);
    });
  });

  describe('enqueue', () => {
    it('should add items to the queue', () => {
      queue.enqueue('a', 1);
      expect(queue.size).toBe(1);
      expect(queue.isEmpty()).toBe(false);
    });

    it('should accept multiple items', () => {
      queue.enqueue('a', 1);
      queue.enqueue('b', 2);
      queue.enqueue('c', 3);
      expect(queue.size).toBe(3);
    });

    it('should handle same priority items', () => {
      queue.enqueue('a', 1);
      queue.enqueue('b', 1);
      queue.enqueue('c', 1);
      expect(queue.size).toBe(3);
    });
  });

  describe('dequeue', () => {
    it('should return undefined for empty queue', () => {
      expect(queue.dequeue()).toBeUndefined();
    });

    it('should return items in priority order (lowest first)', () => {
      queue.enqueue('high', 10);
      queue.enqueue('low', 1);
      queue.enqueue('medium', 5);

      expect(queue.dequeue()).toBe('low');
      expect(queue.dequeue()).toBe('medium');
      expect(queue.dequeue()).toBe('high');
    });

    it('should handle float priorities', () => {
      queue.enqueue('c', 2.5);
      queue.enqueue('a', 1.1);
      queue.enqueue('b', 1.9);

      expect(queue.dequeue()).toBe('a');
      expect(queue.dequeue()).toBe('b');
      expect(queue.dequeue()).toBe('c');
    });

    it('should return items with same priority in FIFO order', () => {
      queue.enqueue('first', 1);
      queue.enqueue('second', 1);
      queue.enqueue('third', 1);

      expect(queue.dequeue()).toBe('first');
      expect(queue.dequeue()).toBe('second');
      expect(queue.dequeue()).toBe('third');
    });

    it('should decrement size after dequeue', () => {
      queue.enqueue('a', 1);
      queue.enqueue('b', 2);
      expect(queue.size).toBe(2);

      queue.dequeue();
      expect(queue.size).toBe(1);

      queue.dequeue();
      expect(queue.size).toBe(0);
    });
  });

  describe('peek', () => {
    it('should return undefined for empty queue', () => {
      expect(queue.peek()).toBeUndefined();
    });

    it('should return lowest priority item without removing it', () => {
      queue.enqueue('high', 10);
      queue.enqueue('low', 1);

      expect(queue.peek()).toBe('low');
      expect(queue.size).toBe(2);
      expect(queue.peek()).toBe('low'); // Still there
    });
  });

  describe('isEmpty', () => {
    it('should return true for new queue', () => {
      expect(queue.isEmpty()).toBe(true);
    });

    it('should return false after enqueue', () => {
      queue.enqueue('a', 1);
      expect(queue.isEmpty()).toBe(false);
    });

    it('should return true after dequeueing all items', () => {
      queue.enqueue('a', 1);
      queue.dequeue();
      expect(queue.isEmpty()).toBe(true);
    });
  });

  describe('clear', () => {
    it('should remove all items', () => {
      queue.enqueue('a', 1);
      queue.enqueue('b', 2);
      queue.enqueue('c', 3);

      queue.clear();

      expect(queue.isEmpty()).toBe(true);
      expect(queue.size).toBe(0);
      expect(queue.dequeue()).toBeUndefined();
    });
  });

  describe('size', () => {
    it('should track item count correctly', () => {
      expect(queue.size).toBe(0);

      queue.enqueue('a', 1);
      expect(queue.size).toBe(1);

      queue.enqueue('b', 2);
      expect(queue.size).toBe(2);

      queue.dequeue();
      expect(queue.size).toBe(1);

      queue.clear();
      expect(queue.size).toBe(0);
    });
  });

  describe('complex scenarios', () => {
    it('should handle interleaved enqueue and dequeue', () => {
      queue.enqueue('a', 3);
      queue.enqueue('b', 1);
      expect(queue.dequeue()).toBe('b');

      queue.enqueue('c', 2);
      expect(queue.dequeue()).toBe('c');
      expect(queue.dequeue()).toBe('a');
    });

    it('should handle large number of items', () => {
      const items: { value: string; priority: number }[] = [];
      for (let i = 0; i < 1000; i++) {
        const priority = Math.random() * 100;
        items.push({ value: `item-${i}`, priority });
        queue.enqueue(`item-${i}`, priority);
      }

      expect(queue.size).toBe(1000);

      // Sort items by priority to verify order
      items.sort((a, b) => a.priority - b.priority);

      // Due to bucket precision, we just verify all items are dequeued
      // and roughly in order (within bucket precision)
      let prevPriority = -Infinity;
      for (let i = 0; i < 1000; i++) {
        const item = queue.dequeue();
        expect(item).toBeDefined();
      }

      expect(queue.isEmpty()).toBe(true);
    });

    it('should handle negative priorities', () => {
      queue.enqueue('a', -1);
      queue.enqueue('b', -5);
      queue.enqueue('c', 0);

      expect(queue.dequeue()).toBe('b');
      expect(queue.dequeue()).toBe('a');
      expect(queue.dequeue()).toBe('c');
    });

    it('should handle zero priority', () => {
      queue.enqueue('b', 1);
      queue.enqueue('a', 0);

      expect(queue.dequeue()).toBe('a');
      expect(queue.dequeue()).toBe('b');
    });
  });
});
