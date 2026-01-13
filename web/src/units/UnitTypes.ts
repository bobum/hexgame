/**
 * Unit type definitions, stats, and domain classifications.
 */

/**
 * Domain determines where a unit can move.
 */
export enum UnitDomain {
  Land = 'land',           // Can only move on land (elevation >= 0, not water)
  Naval = 'naval',         // Can only move on water (elevation < 0 or coast)
  Amphibious = 'amphibious', // Can move on both land and water
}

/**
 * All available unit types.
 */
export enum UnitType {
  // Land units
  Infantry = 'infantry',
  Cavalry = 'cavalry',
  Archer = 'archer',

  // Naval units
  Galley = 'galley',
  Warship = 'warship',

  // Amphibious units
  Marine = 'marine',
}

/**
 * Runtime unit instance data.
 */
export interface UnitData {
  id: number;
  type: UnitType;
  q: number;              // Hex position Q
  r: number;              // Hex position R
  health: number;
  maxHealth: number;
  movement: number;       // Current movement points this turn
  maxMovement: number;    // Movement points per turn
  attack: number;
  defense: number;
  playerId: number;       // 0 = neutral, 1 = player, 2+ = AI
  hasMoved: boolean;      // Has this unit moved this turn?
}

/**
 * Static stats for each unit type.
 */
export interface UnitTypeStats {
  type: UnitType;
  domain: UnitDomain;
  health: number;
  maxHealth: number;
  movement: number;
  maxMovement: number;
  attack: number;
  defense: number;
  name: string;           // Display name
  description: string;    // Short description
}

/**
 * Stats definitions for all unit types.
 */
export const UnitStats: Record<UnitType, UnitTypeStats> = {
  // Land units
  [UnitType.Infantry]: {
    type: UnitType.Infantry,
    domain: UnitDomain.Land,
    health: 100,
    maxHealth: 100,
    movement: 2,
    maxMovement: 2,
    attack: 10,
    defense: 8,
    name: 'Infantry',
    description: 'Basic land unit. Slow but sturdy.',
  },
  [UnitType.Cavalry]: {
    type: UnitType.Cavalry,
    domain: UnitDomain.Land,
    health: 80,
    maxHealth: 80,
    movement: 4,
    maxMovement: 4,
    attack: 12,
    defense: 5,
    name: 'Cavalry',
    description: 'Fast land unit. Good for flanking.',
  },
  [UnitType.Archer]: {
    type: UnitType.Archer,
    domain: UnitDomain.Land,
    health: 60,
    maxHealth: 60,
    movement: 2,
    maxMovement: 2,
    attack: 15,
    defense: 3,
    name: 'Archer',
    description: 'Ranged land unit. High attack, low defense.',
  },

  // Naval units
  [UnitType.Galley]: {
    type: UnitType.Galley,
    domain: UnitDomain.Naval,
    health: 80,
    maxHealth: 80,
    movement: 3,
    maxMovement: 3,
    attack: 8,
    defense: 6,
    name: 'Galley',
    description: 'Light naval unit. Fast and maneuverable.',
  },
  [UnitType.Warship]: {
    type: UnitType.Warship,
    domain: UnitDomain.Naval,
    health: 150,
    maxHealth: 150,
    movement: 2,
    maxMovement: 2,
    attack: 20,
    defense: 12,
    name: 'Warship',
    description: 'Heavy naval unit. Slow but powerful.',
  },

  // Amphibious units
  [UnitType.Marine]: {
    type: UnitType.Marine,
    domain: UnitDomain.Amphibious,
    health: 70,
    maxHealth: 70,
    movement: 2,
    maxMovement: 2,
    attack: 8,
    defense: 6,
    name: 'Marine',
    description: 'Amphibious unit. Can move on land and water.',
  },
};

/**
 * Get the domain for a unit type.
 */
export function getUnitDomain(type: UnitType): UnitDomain {
  return UnitStats[type].domain;
}

/**
 * Get all unit types for a specific domain.
 */
export function getUnitTypesForDomain(domain: UnitDomain): UnitType[] {
  return Object.values(UnitType).filter(
    type => UnitStats[type].domain === domain
  );
}

/**
 * Get all land unit types.
 */
export function getLandUnitTypes(): UnitType[] {
  return getUnitTypesForDomain(UnitDomain.Land);
}

/**
 * Get all naval unit types.
 */
export function getNavalUnitTypes(): UnitType[] {
  return getUnitTypesForDomain(UnitDomain.Naval);
}

/**
 * Check if a unit type can traverse land.
 */
export function canTraverseLand(type: UnitType): boolean {
  const domain = getUnitDomain(type);
  return domain === UnitDomain.Land || domain === UnitDomain.Amphibious;
}

/**
 * Check if a unit type can traverse water.
 */
export function canTraverseWater(type: UnitType): boolean {
  const domain = getUnitDomain(type);
  return domain === UnitDomain.Naval || domain === UnitDomain.Amphibious;
}
