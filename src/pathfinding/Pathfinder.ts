import { HexCell } from '../types';
import { HexGrid } from '../core/HexGrid';
import { HexCoordinates } from '../core/HexCoordinates';
import { UnitManager } from '../units/UnitManager';
import { PriorityQueue } from './PriorityQueue';
import { getMovementCost, isPassable } from './MovementCosts';

/**
 * Result of a pathfinding operation.
 */
export interface PathResult {
  /** The path from start to end (inclusive of both), empty if no path found */
  path: HexCell[];
  /** Total movement cost of the path */
  cost: number;
  /** Whether the destination is reachable */
  reachable: boolean;
}

/**
 * A* pathfinder for hex grids.
 * Finds optimal paths considering terrain costs, elevation, and unit obstacles.
 */
export class Pathfinder {
  constructor(
    private grid: HexGrid,
    private unitManager?: UnitManager
  ) {}

  /**
   * Find the optimal path between two cells using A* algorithm.
   *
   * @param start - Starting cell
   * @param end - Destination cell
   * @param options - Optional configuration
   * @returns PathResult with path, cost, and reachability
   */
  findPath(
    start: HexCell,
    end: HexCell,
    options: {
      /** Ignore units when pathfinding (useful for theoretical paths) */
      ignoreUnits?: boolean;
      /** Maximum cost to search (limits search area) */
      maxCost?: number;
    } = {}
  ): PathResult {
    const { ignoreUnits = false, maxCost = Infinity } = options;

    // Quick check: destination must be passable
    if (!isPassable(end)) {
      return { path: [], cost: Infinity, reachable: false };
    }

    // Quick check: destination can't have a unit (unless ignoring units)
    if (!ignoreUnits && this.unitManager) {
      const unitAtEnd = this.unitManager.getUnitAt(end.q, end.r);
      if (unitAtEnd) {
        return { path: [], cost: Infinity, reachable: false };
      }
    }

    // Same cell - trivial path
    if (start.q === end.q && start.r === end.r) {
      return { path: [start], cost: 0, reachable: true };
    }

    const frontier = new PriorityQueue<HexCell>();
    const cameFrom = new Map<string, HexCell | null>();
    const costSoFar = new Map<string, number>();

    const startKey = this.cellKey(start);
    const endKey = this.cellKey(end);

    frontier.enqueue(start, 0);
    cameFrom.set(startKey, null);
    costSoFar.set(startKey, 0);

    while (!frontier.isEmpty()) {
      const current = frontier.dequeue()!;
      const currentKey = this.cellKey(current);

      // Found destination
      if (currentKey === endKey) {
        return {
          path: this.reconstructPath(cameFrom, start, end),
          cost: costSoFar.get(endKey)!,
          reachable: true,
        };
      }

      // Explore neighbors
      for (const neighbor of this.grid.getNeighbors(current)) {
        // Skip if there's a unit (unless ignoring units)
        if (!ignoreUnits && this.unitManager) {
          const unitAtNeighbor = this.unitManager.getUnitAt(neighbor.q, neighbor.r);
          // Allow destination even if pathfinding toward a unit (for attack targeting)
          if (unitAtNeighbor && this.cellKey(neighbor) !== endKey) {
            continue;
          }
        }

        const moveCost = getMovementCost(current, neighbor);

        // Skip impassable terrain
        if (!isFinite(moveCost)) {
          continue;
        }

        const newCost = costSoFar.get(currentKey)! + moveCost;

        // Skip if exceeds max cost
        if (newCost > maxCost) {
          continue;
        }

        const neighborKey = this.cellKey(neighbor);

        if (!costSoFar.has(neighborKey) || newCost < costSoFar.get(neighborKey)!) {
          costSoFar.set(neighborKey, newCost);

          // A* priority = cost so far + heuristic estimate to goal
          const priority = newCost + this.heuristic(neighbor, end);
          frontier.enqueue(neighbor, priority);
          cameFrom.set(neighborKey, current);
        }
      }
    }

    // No path found
    return { path: [], cost: Infinity, reachable: false };
  }

  /**
   * Get all cells reachable from a starting cell within a movement budget.
   * Useful for showing movement range.
   *
   * @param start - Starting cell
   * @param movementPoints - Maximum movement cost allowed
   * @param options - Optional configuration
   * @returns Map of reachable cells to their movement cost
   */
  getReachableCells(
    start: HexCell,
    movementPoints: number,
    options: {
      ignoreUnits?: boolean;
    } = {}
  ): Map<HexCell, number> {
    const { ignoreUnits = false } = options;
    const reachable = new Map<HexCell, number>();
    const frontier = new PriorityQueue<HexCell>();
    const costSoFar = new Map<string, number>();

    const startKey = this.cellKey(start);
    frontier.enqueue(start, 0);
    costSoFar.set(startKey, 0);
    reachable.set(start, 0);

    while (!frontier.isEmpty()) {
      const current = frontier.dequeue()!;
      const currentKey = this.cellKey(current);
      const currentCost = costSoFar.get(currentKey)!;

      for (const neighbor of this.grid.getNeighbors(current)) {
        // Skip if there's a unit (unless ignoring)
        if (!ignoreUnits && this.unitManager) {
          const unitAtNeighbor = this.unitManager.getUnitAt(neighbor.q, neighbor.r);
          if (unitAtNeighbor) {
            continue;
          }
        }

        const moveCost = getMovementCost(current, neighbor);

        // Skip impassable
        if (!isFinite(moveCost)) {
          continue;
        }

        const newCost = currentCost + moveCost;

        // Skip if exceeds movement budget
        if (newCost > movementPoints) {
          continue;
        }

        const neighborKey = this.cellKey(neighbor);

        if (!costSoFar.has(neighborKey) || newCost < costSoFar.get(neighborKey)!) {
          costSoFar.set(neighborKey, newCost);
          frontier.enqueue(neighbor, newCost);
          reachable.set(neighbor, newCost);
        }
      }
    }

    return reachable;
  }

  /**
   * Check if a path exists between two cells.
   * More efficient than findPath if you only need yes/no.
   */
  hasPath(start: HexCell, end: HexCell, ignoreUnits = false): boolean {
    const result = this.findPath(start, end, { ignoreUnits });
    return result.reachable;
  }

  /**
   * Get the movement cost between two adjacent cells.
   * Returns Infinity if not adjacent or impassable.
   */
  getStepCost(from: HexCell, to: HexCell): number {
    // Check if adjacent
    const fromCoords = new HexCoordinates(from.q, from.r);
    const toCoords = new HexCoordinates(to.q, to.r);

    if (fromCoords.distanceTo(toCoords) !== 1) {
      return Infinity; // Not adjacent
    }

    return getMovementCost(from, to);
  }

  /**
   * Heuristic function for A* - hex distance.
   * Uses minimum possible cost (1) to ensure admissibility.
   */
  private heuristic(a: HexCell, b: HexCell): number {
    const coordsA = new HexCoordinates(a.q, a.r);
    const coordsB = new HexCoordinates(b.q, b.r);
    return coordsA.distanceTo(coordsB);
  }

  /**
   * Generate a unique key for a cell.
   */
  private cellKey(cell: HexCell): string {
    return `${cell.q},${cell.r}`;
  }

  /**
   * Reconstruct the path from the cameFrom map.
   */
  private reconstructPath(
    cameFrom: Map<string, HexCell | null>,
    start: HexCell,
    end: HexCell
  ): HexCell[] {
    const path: HexCell[] = [];
    let current: HexCell | null = end;

    while (current) {
      path.unshift(current);
      const key = this.cellKey(current);
      current = cameFrom.get(key) ?? null;
    }

    return path;
  }
}
