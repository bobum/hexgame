import { TerrainType, HexCell } from '../types';

/**
 * Base movement cost for each terrain type.
 * Lower = easier to traverse.
 * Infinity = impassable.
 */
export const TerrainCosts: Record<TerrainType, number> = {
  [TerrainType.Plains]: 1,
  [TerrainType.Coast]: 1,
  [TerrainType.Desert]: 1,
  [TerrainType.Savanna]: 1,
  [TerrainType.Forest]: 1.5,
  [TerrainType.Taiga]: 1.5,
  [TerrainType.Jungle]: 2,
  [TerrainType.Tundra]: 1.5,
  [TerrainType.Hills]: 2,
  [TerrainType.Snow]: 2.5,
  [TerrainType.Mountains]: Infinity,  // Impassable
  [TerrainType.Ocean]: Infinity,      // Impassable
};

/**
 * Calculate the movement cost to move from one cell to an adjacent cell.
 *
 * @param from - The starting cell
 * @param to - The destination cell (must be adjacent)
 * @returns The movement cost, or Infinity if impassable
 */
export function getMovementCost(from: HexCell, to: HexCell): number {
  // Water is impassable (elevation < 0)
  if (to.elevation < 0) {
    return Infinity;
  }

  // Get base terrain cost
  let cost = TerrainCosts[to.terrainType];

  // If base terrain is impassable, return early
  if (!isFinite(cost)) {
    return Infinity;
  }

  // Elevation difference penalty
  const elevDiff = to.elevation - from.elevation;

  // Cliffs (2+ elevation difference) are impassable
  if (Math.abs(elevDiff) >= 2) {
    return Infinity;
  }

  // Climbing penalty - going uphill costs more
  if (elevDiff > 0) {
    cost += elevDiff * 0.5;
  }

  return cost;
}

/**
 * Check if a cell is passable at all (can be moved through).
 * Useful for quick filtering without calculating full cost.
 */
export function isPassable(cell: HexCell): boolean {
  // Water is impassable
  if (cell.elevation < 0) {
    return false;
  }

  // Mountains are impassable
  if (cell.terrainType === TerrainType.Mountains) {
    return false;
  }

  // Ocean is impassable
  if (cell.terrainType === TerrainType.Ocean) {
    return false;
  }

  return true;
}

/**
 * Check if movement between two adjacent cells is possible.
 * Takes into account terrain and elevation differences.
 */
export function canMoveBetween(from: HexCell, to: HexCell): boolean {
  return isFinite(getMovementCost(from, to));
}
