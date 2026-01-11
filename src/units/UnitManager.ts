/**
 * Manages all units in the game.
 * Uses object pooling for efficient unit creation/destruction.
 */
import * as THREE from 'three';
import { UnitData, UnitType, UnitStats } from '../types';
import { HexCoordinates } from '../core/HexCoordinates';
import { HexGrid } from '../core/HexGrid';
import { SpatialHash } from '../utils/SpatialHash';
import { ObjectPool } from '../utils/ObjectPool';

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
      }),
      (unit) => {
        unit.id = 0;
        unit.q = 0;
        unit.r = 0;
        unit.health = 100;
        unit.movement = 2;
      }
    );

    // Pre-warm pool
    this.unitPool.prewarm(50);
  }

  /**
   * Create a new unit at the specified hex.
   */
  createUnit(type: UnitType, q: number, r: number, playerId: number): UnitData | null {
    // Check if hex is valid and passable
    const cell = this.grid.getCellAt(q, r);
    if (!cell || cell.elevation < 0) {
      return null; // Can't place on water
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
   */
  moveUnit(unitId: number, toQ: number, toR: number): boolean {
    const unit = this.units.get(unitId);
    if (!unit) return false;

    // Check destination is valid
    const cell = this.grid.getCellAt(toQ, toR);
    if (!cell || cell.elevation < 0) return false;

    // Check not occupied
    if (this.getUnitAt(toQ, toR)) return false;

    // Update spatial hash
    const oldPos = new HexCoordinates(unit.q, unit.r).toWorldPosition(0);
    this.spatialHash.remove(unit);

    // Update position
    unit.q = toQ;
    unit.r = toR;

    const newPos = new HexCoordinates(toQ, toR).toWorldPosition(0);
    this.spatialHash.insert(unit, newPos.x, newPos.z);

    return true;
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
   * Spawn random units for testing.
   */
  spawnRandomUnits(count: number, playerId: number = 1): number {
    const cells = this.grid.getAllCells().filter(c => c.elevation >= 0);
    let spawned = 0;

    const types = [UnitType.Infantry, UnitType.Cavalry, UnitType.Archer];

    for (let i = 0; i < count && cells.length > 0; i++) {
      const idx = Math.floor(Math.random() * cells.length);
      const cell = cells[idx];
      const type = types[Math.floor(Math.random() * types.length)];

      if (this.createUnit(type, cell.q, cell.r, playerId)) {
        spawned++;
        cells.splice(idx, 1); // Remove cell so we don't double-place
      }
    }

    return spawned;
  }
}
