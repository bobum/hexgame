/**
 * Worker management and factory functions.
 * Provides easy access to worker pools for different tasks.
 */

import { WorkerPool } from './WorkerPool';

// Vite worker import syntax
import MapGeneratorWorker from './mapGenerator.worker?worker';
import PathfindingWorker from './pathfinding.worker?worker';

// Singleton pools
let mapGenPool: WorkerPool | null = null;
let pathfindingPool: WorkerPool | null = null;

/**
 * Get or create the map generation worker pool.
 * Uses a single worker since map gen is typically one task at a time.
 */
export function getMapGenPool(): WorkerPool {
  if (!mapGenPool) {
    mapGenPool = new WorkerPool(() => new MapGeneratorWorker(), 1);
  }
  return mapGenPool;
}

/**
 * Get or create the pathfinding worker pool.
 * Uses multiple workers for parallel pathfinding requests.
 */
export function getPathfindingPool(): WorkerPool {
  if (!pathfindingPool) {
    // Use half available cores for pathfinding
    const poolSize = Math.max(1, Math.floor((navigator.hardwareConcurrency || 4) / 2));
    pathfindingPool = new WorkerPool(() => new PathfindingWorker(), poolSize);
  }
  return pathfindingPool;
}

/**
 * Terminate all worker pools (cleanup).
 */
export function terminateAllPools(): void {
  mapGenPool?.terminate();
  pathfindingPool?.terminate();
  mapGenPool = null;
  pathfindingPool = null;
}

/**
 * Check if Web Workers are supported.
 */
export function workersSupported(): boolean {
  return typeof Worker !== 'undefined';
}

// Re-export types
export { WorkerPool } from './WorkerPool';
export type { WorkerTask, WorkerResult } from './WorkerPool';
