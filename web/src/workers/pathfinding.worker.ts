/**
 * Web Worker for A* pathfinding.
 * Runs pathfinding calculations off the main thread to prevent UI freezing.
 */

import { WorkerTask, WorkerResult } from './WorkerPool';

// Pathfinding types
interface PathfindingInput {
  startQ: number;
  startR: number;
  endQ: number;
  endR: number;
  // Grid data passed as typed array for efficiency
  gridWidth: number;
  gridHeight: number;
  // Packed cell data: [elevation, terrainType, isPassable] per cell
  cellData: Int8Array;
}

interface PathfindingOutput {
  path: Array<{ q: number; r: number }>;
  cost: number;
  nodesExplored: number;
  computeTime: number;
}

// Hex coordinate helpers
function toKey(q: number, r: number): string {
  return `${q},${r}`;
}

function getNeighbors(q: number, r: number): Array<{ q: number; r: number }> {
  // Cube coordinate neighbor offsets
  return [
    { q: q + 1, r: r },
    { q: q + 1, r: r - 1 },
    { q: q, r: r - 1 },
    { q: q - 1, r: r },
    { q: q - 1, r: r + 1 },
    { q: q, r: r + 1 },
  ];
}

function hexDistance(q1: number, r1: number, q2: number, r2: number): number {
  return (Math.abs(q1 - q2) + Math.abs(q1 + r1 - q2 - r2) + Math.abs(r1 - r2)) / 2;
}

// A* implementation
function findPath(input: PathfindingInput): PathfindingOutput {
  const startTime = performance.now();
  const { startQ, startR, endQ, endR, gridWidth, gridHeight, cellData } = input;

  // Helper to get cell data
  function getCellIndex(q: number, r: number): number {
    // Convert cube to offset coordinates
    const col = q + Math.floor(r / 2);
    const row = r;
    if (col < 0 || col >= gridWidth || row < 0 || row >= gridHeight) {
      return -1;
    }
    return (row * gridWidth + col) * 3; // 3 values per cell
  }

  function isPassable(q: number, r: number): boolean {
    const idx = getCellIndex(q, r);
    if (idx < 0) return false;
    return cellData[idx + 2] === 1; // isPassable flag
  }

  function getMoveCost(q: number, r: number): number {
    const idx = getCellIndex(q, r);
    if (idx < 0) return Infinity;
    const elevation = cellData[idx];
    const terrainType = cellData[idx + 1];

    // Base cost + terrain modifier
    let cost = 1;
    if (terrainType === 4) cost = 1.5; // Hills
    if (terrainType === 5) cost = 2.0; // Mountains
    if (terrainType === 3) cost = 1.2; // Forest

    return cost;
  }

  // A* algorithm
  interface Node {
    q: number;
    r: number;
    g: number;
    f: number;
    parent: Node | null;
  }

  const openSet: Node[] = [];
  const closedSet = new Set<string>();
  let nodesExplored = 0;

  const startNode: Node = {
    q: startQ,
    r: startR,
    g: 0,
    f: hexDistance(startQ, startR, endQ, endR),
    parent: null,
  };

  openSet.push(startNode);

  while (openSet.length > 0) {
    // Find node with lowest f score
    openSet.sort((a, b) => a.f - b.f);
    const current = openSet.shift()!;
    nodesExplored++;

    // Check if we reached the goal
    if (current.q === endQ && current.r === endR) {
      // Reconstruct path
      const path: Array<{ q: number; r: number }> = [];
      let node: Node | null = current;
      while (node) {
        path.unshift({ q: node.q, r: node.r });
        node = node.parent;
      }

      return {
        path,
        cost: current.g,
        nodesExplored,
        computeTime: performance.now() - startTime,
      };
    }

    closedSet.add(toKey(current.q, current.r));

    // Explore neighbors
    for (const neighbor of getNeighbors(current.q, current.r)) {
      const key = toKey(neighbor.q, neighbor.r);

      if (closedSet.has(key)) continue;
      if (!isPassable(neighbor.q, neighbor.r)) continue;

      const g = current.g + getMoveCost(neighbor.q, neighbor.r);
      const h = hexDistance(neighbor.q, neighbor.r, endQ, endR);
      const f = g + h;

      // Check if already in open set with better score
      const existing = openSet.find(n => n.q === neighbor.q && n.r === neighbor.r);
      if (existing) {
        if (g < existing.g) {
          existing.g = g;
          existing.f = f;
          existing.parent = current;
        }
      } else {
        openSet.push({
          q: neighbor.q,
          r: neighbor.r,
          g,
          f,
          parent: current,
        });
      }
    }
  }

  // No path found
  return {
    path: [],
    cost: Infinity,
    nodesExplored,
    computeTime: performance.now() - startTime,
  };
}

// Worker message handler
self.onmessage = (e: MessageEvent<WorkerTask<PathfindingInput>>) => {
  const { id, type, data } = e.data;

  try {
    let result: unknown;

    switch (type) {
      case 'findPath':
        result = findPath(data);
        break;
      default:
        throw new Error(`Unknown task type: ${type}`);
    }

    const response: WorkerResult = { id, type, data: result };
    self.postMessage(response);
  } catch (error) {
    const response: WorkerResult = {
      id,
      type,
      data: null,
      error: error instanceof Error ? error.message : 'Unknown error',
    };
    self.postMessage(response);
  }
};
