/**
 * Bucket-based priority queue optimized for A* pathfinding.
 * Uses integer buckets for fast O(1) enqueue and amortized O(1) dequeue.
 * Priorities are multiplied by a precision factor to handle float values.
 */
export class PriorityQueue<T> {
  private buckets: Map<number, T[]> = new Map();
  private minPriority = Infinity;
  private _size = 0;
  private readonly precision: number;

  /**
   * @param precision - Multiplier for priorities to handle decimals (default 10 = 0.1 precision)
   */
  constructor(precision = 10) {
    this.precision = precision;
  }

  /**
   * Add an item with the given priority (lower = higher priority)
   */
  enqueue(item: T, priority: number): void {
    const bucketKey = Math.floor(priority * this.precision);

    let bucket = this.buckets.get(bucketKey);
    if (!bucket) {
      bucket = [];
      this.buckets.set(bucketKey, bucket);
    }
    bucket.push(item);

    if (bucketKey < this.minPriority) {
      this.minPriority = bucketKey;
    }

    this._size++;
  }

  /**
   * Remove and return the item with lowest priority
   */
  dequeue(): T | undefined {
    if (this._size === 0) {
      return undefined;
    }

    // Find the minimum bucket with items
    while (this.minPriority < Infinity) {
      const bucket = this.buckets.get(this.minPriority);
      if (bucket && bucket.length > 0) {
        this._size--;
        return bucket.shift();
      }
      // Bucket is empty, clean it up and find next
      this.buckets.delete(this.minPriority);
      this.minPriority = this.findMinPriority();
    }

    return undefined;
  }

  /**
   * Peek at the item with lowest priority without removing it
   */
  peek(): T | undefined {
    if (this._size === 0) {
      return undefined;
    }

    const bucket = this.buckets.get(this.minPriority);
    return bucket?.[0];
  }

  /**
   * Check if the queue is empty
   */
  isEmpty(): boolean {
    return this._size === 0;
  }

  /**
   * Clear all items from the queue
   */
  clear(): void {
    this.buckets.clear();
    this.minPriority = Infinity;
    this._size = 0;
  }

  /**
   * Get the number of items in the queue
   */
  get size(): number {
    return this._size;
  }

  /**
   * Find the minimum priority bucket key
   */
  private findMinPriority(): number {
    let min = Infinity;
    for (const key of this.buckets.keys()) {
      if (key < min) {
        min = key;
      }
    }
    return min;
  }
}
