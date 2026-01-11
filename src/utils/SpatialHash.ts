/**
 * Generic spatial hash grid for O(1) point lookups and efficient range queries.
 * Used for finding units, features, or any spatially-located objects.
 */
export class SpatialHash<T> {
  private buckets: Map<string, Set<T>> = new Map();
  private itemPositions: Map<T, { x: number; z: number }> = new Map();
  private readonly cellSize: number;

  // Statistics
  private _insertCount = 0;
  private _queryCount = 0;
  private _peakItems = 0;

  /**
   * Create a spatial hash with the given cell size.
   * Smaller cells = more memory but faster range queries.
   * Recommended: cell size slightly larger than typical query radius.
   */
  constructor(cellSize: number = 2) {
    this.cellSize = cellSize;
  }

  /**
   * Get the bucket key for a world position.
   */
  private getKey(x: number, z: number): string {
    const bx = Math.floor(x / this.cellSize);
    const bz = Math.floor(z / this.cellSize);
    return `${bx},${bz}`;
  }

  /**
   * Get bucket coordinates for a world position.
   */
  private getBucketCoords(x: number, z: number): { bx: number; bz: number } {
    return {
      bx: Math.floor(x / this.cellSize),
      bz: Math.floor(z / this.cellSize),
    };
  }

  /**
   * Insert an item at the given position.
   */
  insert(item: T, x: number, z: number): void {
    // Remove from old position if already tracked
    if (this.itemPositions.has(item)) {
      this.remove(item);
    }

    const key = this.getKey(x, z);
    let bucket = this.buckets.get(key);
    if (!bucket) {
      bucket = new Set();
      this.buckets.set(key, bucket);
    }
    bucket.add(item);
    this.itemPositions.set(item, { x, z });

    this._insertCount++;
    this._peakItems = Math.max(this._peakItems, this.itemPositions.size);
  }

  /**
   * Remove an item from the hash.
   */
  remove(item: T): boolean {
    const pos = this.itemPositions.get(item);
    if (!pos) return false;

    const key = this.getKey(pos.x, pos.z);
    const bucket = this.buckets.get(key);
    if (bucket) {
      bucket.delete(item);
      if (bucket.size === 0) {
        this.buckets.delete(key);
      }
    }
    this.itemPositions.delete(item);
    return true;
  }

  /**
   * Update an item's position (remove + insert).
   */
  update(item: T, x: number, z: number): void {
    this.insert(item, x, z); // insert handles removal automatically
  }

  /**
   * Get all items at exact bucket position (O(1)).
   */
  getAt(x: number, z: number): T[] {
    this._queryCount++;
    const key = this.getKey(x, z);
    const bucket = this.buckets.get(key);
    return bucket ? Array.from(bucket) : [];
  }

  /**
   * Get the first item at a position, or null.
   */
  getFirstAt(x: number, z: number): T | null {
    this._queryCount++;
    const key = this.getKey(x, z);
    const bucket = this.buckets.get(key);
    if (bucket && bucket.size > 0) {
      return bucket.values().next().value ?? null;
    }
    return null;
  }

  /**
   * Check if any item exists at position.
   */
  hasAt(x: number, z: number): boolean {
    const key = this.getKey(x, z);
    const bucket = this.buckets.get(key);
    return bucket !== undefined && bucket.size > 0;
  }

  /**
   * Query all items within a radius of the given point.
   * Uses squared distance for efficiency.
   */
  queryRadius(x: number, z: number, radius: number): T[] {
    this._queryCount++;
    const results: T[] = [];
    const radiusSq = radius * radius;

    // Calculate bucket range to check
    const { bx: minBx, bz: minBz } = this.getBucketCoords(x - radius, z - radius);
    const { bx: maxBx, bz: maxBz } = this.getBucketCoords(x + radius, z + radius);

    // Check all buckets in range
    for (let bx = minBx; bx <= maxBx; bx++) {
      for (let bz = minBz; bz <= maxBz; bz++) {
        const key = `${bx},${bz}`;
        const bucket = this.buckets.get(key);
        if (!bucket) continue;

        for (const item of bucket) {
          const pos = this.itemPositions.get(item);
          if (!pos) continue;

          const dx = pos.x - x;
          const dz = pos.z - z;
          if (dx * dx + dz * dz <= radiusSq) {
            results.push(item);
          }
        }
      }
    }

    return results;
  }

  /**
   * Query all items within a rectangular area.
   */
  queryRect(minX: number, minZ: number, maxX: number, maxZ: number): T[] {
    this._queryCount++;
    const results: T[] = [];

    const { bx: minBx, bz: minBz } = this.getBucketCoords(minX, minZ);
    const { bx: maxBx, bz: maxBz } = this.getBucketCoords(maxX, maxZ);

    for (let bx = minBx; bx <= maxBx; bx++) {
      for (let bz = minBz; bz <= maxBz; bz++) {
        const key = `${bx},${bz}`;
        const bucket = this.buckets.get(key);
        if (!bucket) continue;

        for (const item of bucket) {
          const pos = this.itemPositions.get(item);
          if (!pos) continue;

          if (pos.x >= minX && pos.x <= maxX && pos.z >= minZ && pos.z <= maxZ) {
            results.push(item);
          }
        }
      }
    }

    return results;
  }

  /**
   * Get all items in the hash.
   */
  getAll(): T[] {
    return Array.from(this.itemPositions.keys());
  }

  /**
   * Get the position of an item.
   */
  getPosition(item: T): { x: number; z: number } | null {
    return this.itemPositions.get(item) ?? null;
  }

  /**
   * Check if an item is in the hash.
   */
  has(item: T): boolean {
    return this.itemPositions.has(item);
  }

  /**
   * Clear all items.
   */
  clear(): void {
    this.buckets.clear();
    this.itemPositions.clear();
  }

  /**
   * Get the number of items in the hash.
   */
  get size(): number {
    return this.itemPositions.size;
  }

  /**
   * Get the number of active buckets.
   */
  get bucketCount(): number {
    return this.buckets.size;
  }

  /**
   * Get statistics for debugging.
   */
  get stats() {
    return {
      items: this.itemPositions.size,
      buckets: this.buckets.size,
      insertCount: this._insertCount,
      queryCount: this._queryCount,
      peakItems: this._peakItems,
      cellSize: this.cellSize,
    };
  }

  /**
   * Reset statistics counters.
   */
  resetStats(): void {
    this._insertCount = 0;
    this._queryCount = 0;
  }
}
