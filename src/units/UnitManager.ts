/**
 * Manages all units in the game.
 * Uses object pooling for efficient unit creation/destruction.
 */
import { HexCoordinates } from '../core/HexCoordinates';
import { HexGrid } from '../core/HexGrid';
import { SpatialHash } from '../utils/SpatialHash';
import { ObjectPool } from '../utils/ObjectPool';
import {
  UnitData,
  UnitType,
  UnitStats,
  canTraverseLand,
  canTraverseWater,
} from './UnitTypes';

export class UnitManager {
  private units: Map<number, UnitData> = new Map();
  private spatialHash: SpatialHash<UnitData>;
  private nextId = 1;

  // Object pool for unit data
  private unitPool: ObjectPool<UnitData>;

  // Reference to grid for terrain checks
  private grid: HexGrid;

  constructor(grid: HexGrid) {
    this.grid = grid;
    this.spatialHash = new SpatialHash<UnitData>(2);

    // Create unit pool
    this.unitPool = new ObjectPool<UnitData>(
      () => ({
        id: 0,
        type: UnitType.Infantry,
        q: 0,
        r: 0,
        health: 100,
        maxHealth: 100,
        movement: 2,
        maxMovement: 2,
        attack: 10,
        defense: 8,
        playerId: 0,
        hasMoved: false,
      }),
      (unit) => {
        unit.id = 0;
        unit.q = 0;
        unit.r = 0;
        unit.health = 100;
        unit.movement = 2;
        unit.hasMoved = false;
      }
    );

    // Pre-warm pool
    this.unitPool.prewarm(50);
  }

  /**
   * Create a new unit at the specified hex.
   */
  createUnit(type: UnitType, q: number, r: number, playerId: number): UnitData | null {
    // Check if hex is valid
    const cell = this.grid.getCellAt(q, r);
    if (!cell) {
      return null;
    }

    // Check domain compatibility
    const isWater = cell.elevation < 0;
    if (isWater && !canTraverseWater(type)) {
      return null; // Land unit can't be placed on water
    }
    if (!isWater && !canTraverseLand(type)) {
      return null; // Naval unit can't be placed on land
    }

    // Check if hex is already occupied
    if (this.getUnitAt(q, r)) {
      return null;
    }

    // Get unit from pool and initialize
    const unit = this.unitPool.acquire();
    const stats = UnitStats[type];

    unit.id = this.nextId++;
    unit.type = type;
    unit.q = q;
    unit.r = r;
    unit.health = stats.maxHealth;
    unit.maxHealth = stats.maxHealth;
    unit.movement = stats.maxMovement;
    unit.maxMovement = stats.maxMovement;
    unit.attack = stats.attack;
    unit.defense = stats.defense;
    unit.playerId = playerId;
    unit.hasMoved = false;

    // Add to tracking
    this.units.set(unit.id, unit);
    const worldPos = new HexCoordinates(q, r).toWorldPosition(0);
    this.spatialHash.insert(unit, worldPos.x, worldPos.z);

    return unit;
  }

  /**
   * Remove a unit from the game.
   */
  removeUnit(unitId: number): boolean {
    const unit = this.units.get(unitId);
    if (!unit) return false;

    this.units.delete(unitId);
    this.spatialHash.remove(unit);
    this.unitPool.release(unit);

    return true;
  }

  /**
   * Move a unit to a new hex.
   * @param movementCost - Optional movement cost to deduct (if not provided, doesn't deduct)
   */
  moveUnit(unitId: number, toQ: number, toR: number, movementCost?: number): boolean {
    const unit = this.units.get(unitId);
    if (!unit) return false;

    // Check destination is valid
    const cell = this.grid.getCellAt(toQ, toR);
    if (!cell) return false;

    // Check domain compatibility
    const isWater = cell.elevation < 0;
    if (isWater && !canTraverseWater(unit.type)) {
      return false; // Land unit can't move to water
    }
    if (!isWater && !canTraverseLand(unit.type)) {
      return false; // Naval unit can't move to land
    }

    // Check not occupied
    if (this.getUnitAt(toQ, toR)) return false;

    // Check movement cost if provided
    if (movementCost !== undefined) {
      if (unit.movement < movementCost) {
        return false; // Not enough movement
      }
      unit.movement -= movementCost;
      unit.hasMoved = true;
    }

    // Update spatial hash
    this.spatialHash.remove(unit);

    // Update position
    unit.q = toQ;
    unit.r = toR;

    const newPos = new HexCoordinates(toQ, toR).toWorldPosition(0);
    this.spatialHash.insert(unit, newPos.x, newPos.z);

    return true;
  }

  /**
   * Spend movement points for a unit.
   */
  spendMovement(unitId: number, cost: number): boolean {
    const unit = this.units.get(unitId);
    if (!unit || unit.movement < cost) return false;

    unit.movement -= cost;
    unit.hasMoved = true;
    return true;
  }

  /**
   * Reset movement for all units (called at start of turn).
   */
  resetAllMovement(): void {
    for (const unit of this.units.values()) {
      unit.movement = unit.maxMovement;
      unit.hasMoved = false;
    }
  }

  /**
   * Reset movement for units of a specific player.
   */
  resetPlayerMovement(playerId: number): void {
    for (const unit of this.units.values()) {
      if (unit.playerId === playerId) {
        unit.movement = unit.maxMovement;
        unit.hasMoved = false;
      }
    }
  }

  /**
   * Check if a unit can move (has movement points left).
   */
  canMove(unitId: number): boolean {
    const unit = this.units.get(unitId);
    return unit !== undefined && unit.movement > 0;
  }

  /**
   * Get unit at a specific hex.
   */
  getUnitAt(q: number, r: number): UnitData | null {
    const worldPos = new HexCoordinates(q, r).toWorldPosition(0);
    const units = this.spatialHash.queryRadius(worldPos.x, worldPos.z, 0.5);
    return units.find(u => u.q === q && u.r === r) ?? null;
  }

  /**
   * Get all units within range of a hex.
   */
  getUnitsInRange(q: number, r: number, range: number): UnitData[] {
    const worldPos = new HexCoordinates(q, r).toWorldPosition(0);
    // Approximate world distance from hex range
    const worldRadius = range * 1.8;
    return this.spatialHash.queryRadius(worldPos.x, worldPos.z, worldRadius);
  }

  /**
   * Get unit by ID.
   */
  getUnit(id: number): UnitData | undefined {
    return this.units.get(id);
  }

  /**
   * Get all units.
   */
  getAllUnits(): UnitData[] {
    return Array.from(this.units.values());
  }

  /**
   * Get units for a specific player.
   */
  getPlayerUnits(playerId: number): UnitData[] {
    return this.getAllUnits().filter(u => u.playerId === playerId);
  }

  /**
   * Get unit count.
   */
  get unitCount(): number {
    return this.units.size;
  }

  /**
   * Get pool statistics.
   */
  get poolStats() {
    return this.unitPool.stats;
  }

  /**
   * Clear all units.
   */
  clear(): void {
    for (const unit of this.units.values()) {
      this.unitPool.release(unit);
    }
    this.units.clear();
    this.spatialHash.clear();
  }

  /**
   * Spawn random land units for testing.
   */
  spawnRandomUnits(count: number, playerId: number = 1): number {
    const landCells = this.grid.getAllCells().filter(c => c.elevation >= 0);
    let spawned = 0;

    const landTypes = [UnitType.Infantry, UnitType.Cavalry, UnitType.Archer];

    for (let i = 0; i < count && landCells.length > 0; i++) {
      const idx = Math.floor(Math.random() * landCells.length);
      const cell = landCells[idx];
      const type = landTypes[Math.floor(Math.random() * landTypes.length)];

      if (this.createUnit(type, cell.q, cell.r, playerId)) {
        spawned++;
        landCells.splice(idx, 1); // Remove cell so we don't double-place
      }
    }

    return spawned;
  }

  /**
   * Spawn random naval units for testing.
   */
  spawnRandomNavalUnits(count: number, playerId: number = 1): number {
    const waterCells = this.grid.getAllCells().filter(c => c.elevation < 0);
    let spawned = 0;

    const navalTypes = [UnitType.Galley, UnitType.Warship];

    for (let i = 0; i < count && waterCells.length > 0; i++) {
      const idx = Math.floor(Math.random() * waterCells.length);
      const cell = waterCells[idx];
      const type = navalTypes[Math.floor(Math.random() * navalTypes.length)];

      if (this.createUnit(type, cell.q, cell.r, playerId)) {
        spawned++;
        waterCells.splice(idx, 1);
      }
    }

    return spawned;
  }

  /**
   * Spawn a mix of land and naval units for testing.
   */
  spawnMixedUnits(landCount: number, navalCount: number, playerId: number = 1): { land: number; naval: number } {
    return {
      land: this.spawnRandomUnits(landCount, playerId),
      naval: this.spawnRandomNavalUnits(navalCount, playerId),
    };
  }
}
