/**
 * Generic object pool for reusing objects instead of creating/destroying.
 * Reduces garbage collection pressure for frequently used objects.
 */
export class ObjectPool<T> {
  private available: T[] = [];
  private active: Set<T> = new Set();
  private factory: () => T;
  private reset: (obj: T) => void;
  private maxSize: number;

  // Statistics
  private _created = 0;
  private _reused = 0;
  private _peak = 0;

  /**
   * Create an object pool.
   * @param factory - Function to create new objects
   * @param reset - Function to reset an object before reuse
   * @param maxSize - Maximum pool size (prevents unbounded growth)
   */
  constructor(
    factory: () => T,
    reset: (obj: T) => void = () => {},
    maxSize: number = 1000
  ) {
    this.factory = factory;
    this.reset = reset;
    this.maxSize = maxSize;
  }

  /**
   * Acquire an object from the pool (or create new if empty).
   */
  acquire(): T {
    let obj: T;

    if (this.available.length > 0) {
      obj = this.available.pop()!;
      this._reused++;
    } else {
      obj = this.factory();
      this._created++;
    }

    this.active.add(obj);
    this._peak = Math.max(this._peak, this.active.size);

    return obj;
  }

  /**
   * Release an object back to the pool.
   */
  release(obj: T): void {
    if (!this.active.has(obj)) {
      console.warn('ObjectPool: releasing object not from this pool');
      return;
    }

    this.active.delete(obj);
    this.reset(obj);

    // Only keep up to maxSize in the pool
    if (this.available.length < this.maxSize) {
      this.available.push(obj);
    }
  }

  /**
   * Release all active objects back to the pool.
   */
  releaseAll(): void {
    for (const obj of this.active) {
      this.reset(obj);
      if (this.available.length < this.maxSize) {
        this.available.push(obj);
      }
    }
    this.active.clear();
  }

  /**
   * Pre-warm the pool with objects.
   */
  prewarm(count: number): void {
    for (let i = 0; i < count && this.available.length < this.maxSize; i++) {
      this.available.push(this.factory());
      this._created++;
    }
  }

  /**
   * Clear the pool entirely.
   */
  clear(): void {
    this.available = [];
    this.active.clear();
  }

  /**
   * Get pool statistics.
   */
  get stats() {
    return {
      available: this.available.length,
      active: this.active.size,
      created: this._created,
      reused: this._reused,
      peak: this._peak,
      reuseRate: this._reused / (this._created + this._reused) || 0,
    };
  }

  /**
   * Get the number of active objects.
   */
  get activeCount(): number {
    return this.active.size;
  }

  /**
   * Get the number of available objects.
   */
  get availableCount(): number {
    return this.available.length;
  }
}

/**
 * Pre-configured pools for common Three.js objects.
 */
import * as THREE from 'three';

export const Vector3Pool = new ObjectPool<THREE.Vector3>(
  () => new THREE.Vector3(),
  (v) => v.set(0, 0, 0),
  500
);

export const ColorPool = new ObjectPool<THREE.Color>(
  () => new THREE.Color(),
  (c) => c.set(0x000000),
  200
);

export const Matrix4Pool = new ObjectPool<THREE.Matrix4>(
  () => new THREE.Matrix4(),
  (m) => m.identity(),
  100
);
