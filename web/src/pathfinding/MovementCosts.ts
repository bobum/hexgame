import { TerrainType, HexCell } from '../types';
import { UnitType, UnitDomain, getUnitDomain, canTraverseLand, canTraverseWater } from '../units/UnitTypes';
import { HexDirection, opposite as getOppositeDirection } from '../core/HexDirection';

/**
 * River crossing cost penalty.
 */
export const RIVER_CROSSING_COST = 1;

/**
 * Base movement cost for each terrain type (land units).
 * Lower = easier to traverse.
 * Infinity = impassable.
 */
export const LandTerrainCosts: Record<TerrainType, number> = {
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
  [TerrainType.Ocean]: Infinity,      // Impassable for land units
};

/**
 * Base movement cost for each terrain type (naval units).
 * Naval units can only move on water (Ocean) and coast.
 */
export const NavalTerrainCosts: Record<TerrainType, number> = {
  [TerrainType.Ocean]: 1,             // Open water - easy sailing
  [TerrainType.Coast]: 1.5,           // Coastal waters - slightly harder
  [TerrainType.Plains]: Infinity,     // Land - impassable
  [TerrainType.Desert]: Infinity,
  [TerrainType.Savanna]: Infinity,
  [TerrainType.Forest]: Infinity,
  [TerrainType.Taiga]: Infinity,
  [TerrainType.Jungle]: Infinity,
  [TerrainType.Tundra]: Infinity,
  [TerrainType.Hills]: Infinity,
  [TerrainType.Snow]: Infinity,
  [TerrainType.Mountains]: Infinity,
};

// Legacy export for backward compatibility
export const TerrainCosts = LandTerrainCosts;

/**
 * Check if movement crosses a river between two cells.
 * @param from - Starting cell
 * @param to - Destination cell
 * @param direction - Direction of movement from 'from' to 'to'
 */
export function crossesRiver(from: HexCell, to: HexCell, direction?: HexDirection): boolean {
  // If direction provided, check if either cell has a river on that edge
  if (direction !== undefined) {
    const oppositeDir = getOppositeDirection(direction);
    // River flows OUT of 'from' in our direction = we cross it
    if (from.riverDirections.includes(direction)) {
      return true;
    }
    // River flows OUT of 'to' toward us = we cross it
    if (to.riverDirections.includes(oppositeDir)) {
      return true;
    }
  }

  // Fallback: check if any river edges exist between these cells
  // This is less accurate but works without direction info
  return from.riverDirections.length > 0 || to.riverDirections.length > 0;
}

/**
 * Calculate the movement cost for a LAND unit to move from one cell to an adjacent cell.
 */
export function getLandMovementCost(from: HexCell, to: HexCell, direction?: HexDirection): number {
  // Water is impassable for land units (elevation < 0)
  if (to.elevation < 0) {
    return Infinity;
  }

  // Get base terrain cost
  let cost = LandTerrainCosts[to.terrainType];

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

  // River crossing penalty
  if (crossesRiver(from, to, direction)) {
    cost += RIVER_CROSSING_COST;
  }

  return cost;
}

/**
 * Calculate the movement cost for a NAVAL unit to move from one cell to an adjacent cell.
 */
export function getNavalMovementCost(from: HexCell, to: HexCell): number {
  // Naval units can move on water (Ocean, Coast terrain) or any cell with elevation < 0
  const isWaterCell = to.elevation < 0 ||
                      to.terrainType === TerrainType.Ocean ||
                      to.terrainType === TerrainType.Coast;

  if (!isWaterCell) {
    return Infinity;
  }

  // Get base terrain cost for naval - default to 1 for any water cell
  let cost = NavalTerrainCosts[to.terrainType];

  // If terrain type isn't in naval costs but cell is water, use default cost
  if (!isFinite(cost) && to.elevation < 0) {
    cost = 1; // Default water movement cost
  }

  return cost;
}

/**
 * Calculate the movement cost based on unit type (domain-aware).
 */
export function getMovementCostForUnit(
  from: HexCell,
  to: HexCell,
  unitType: UnitType,
  direction?: HexDirection
): number {
  const domain = getUnitDomain(unitType);

  if (domain === UnitDomain.Naval) {
    return getNavalMovementCost(from, to);
  }

  if (domain === UnitDomain.Amphibious) {
    // Amphibious units can use either cost, pick the better one
    const landCost = getLandMovementCost(from, to, direction);
    const navalCost = getNavalMovementCost(from, to);
    return Math.min(landCost, navalCost);
  }

  // Default: land movement
  return getLandMovementCost(from, to, direction);
}

/**
 * Calculate the movement cost to move from one cell to an adjacent cell.
 * Legacy function - assumes land unit for backward compatibility.
 *
 * @param from - The starting cell
 * @param to - The destination cell (must be adjacent)
 * @returns The movement cost, or Infinity if impassable
 */
export function getMovementCost(from: HexCell, to: HexCell): number {
  return getLandMovementCost(from, to);
}

/**
 * Check if a cell is passable for land units.
 */
export function isPassableForLand(cell: HexCell): boolean {
  if (cell.elevation < 0) return false;
  if (cell.terrainType === TerrainType.Mountains) return false;
  if (cell.terrainType === TerrainType.Ocean) return false;
  return true;
}

/**
 * Check if a cell is passable for naval units.
 */
export function isPassableForNaval(cell: HexCell): boolean {
  // Naval can go on water (elevation < 0) or Ocean/Coast terrain
  if (cell.elevation < 0) return true;
  if (cell.terrainType === TerrainType.Ocean) return true;
  if (cell.terrainType === TerrainType.Coast) return true;
  return false;
}

/**
 * Check if a cell is passable for a specific unit type.
 */
export function isPassableForUnit(cell: HexCell, unitType: UnitType): boolean {
  const domain = getUnitDomain(unitType);

  if (domain === UnitDomain.Naval) {
    return isPassableForNaval(cell);
  }

  if (domain === UnitDomain.Amphibious) {
    return isPassableForLand(cell) || isPassableForNaval(cell);
  }

  return isPassableForLand(cell);
}

/**
 * Check if a cell is passable at all (can be moved through).
 * Legacy function - assumes land unit for backward compatibility.
 */
export function isPassable(cell: HexCell): boolean {
  return isPassableForLand(cell);
}

/**
 * Check if movement between two adjacent cells is possible.
 * Takes into account terrain and elevation differences.
 */
export function canMoveBetween(from: HexCell, to: HexCell): boolean {
  return isFinite(getMovementCost(from, to));
}

/**
 * Check if movement between two adjacent cells is possible for a specific unit type.
 */
export function canMoveBetweenForUnit(from: HexCell, to: HexCell, unitType: UnitType): boolean {
  return isFinite(getMovementCostForUnit(from, to, unitType));
}
