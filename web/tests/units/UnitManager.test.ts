import { describe, it, expect, beforeEach } from 'vitest';
import { UnitManager } from '../../src/units/UnitManager';
import { HexGrid } from '../../src/core/HexGrid';
import { UnitType } from '../../src/types';
import { defaultMapConfig } from '../../src/types';

describe('UnitManager', () => {
  let grid: HexGrid;
  let unitManager: UnitManager;

  beforeEach(() => {
    // Create a small test grid
    const config = { ...defaultMapConfig, width: 10, height: 10 };
    grid = new HexGrid(config);
    grid.initialize(); // Must initialize to create cells

    // Set all cells to have valid elevation (>= 0) for land
    for (const cell of grid.getAllCells()) {
      cell.elevation = 1; // Make all cells land
    }

    unitManager = new UnitManager(grid);
  });

  describe('createUnit', () => {
    it('should create a unit at valid position', () => {
      const unit = unitManager.createUnit(UnitType.Infantry, 0, 0, 1);
      expect(unit).not.toBeNull();
      expect(unit?.type).toBe(UnitType.Infantry);
      expect(unit?.q).toBe(0);
      expect(unit?.r).toBe(0);
      expect(unit?.playerId).toBe(1);
    });

    it('should assign unique IDs to units', () => {
      const unit1 = unitManager.createUnit(UnitType.Infantry, 0, 0, 1);
      const unit2 = unitManager.createUnit(UnitType.Infantry, 1, 0, 1);
      expect(unit1?.id).not.toBe(unit2?.id);
    });

    it('should not create unit on occupied hex', () => {
      unitManager.createUnit(UnitType.Infantry, 0, 0, 1);
      const unit2 = unitManager.createUnit(UnitType.Cavalry, 0, 0, 2);
      expect(unit2).toBeNull();
    });

    it('should not create unit on water (elevation < 0)', () => {
      // Set a cell to water
      const cell = grid.getCellAt(5, 5);
      if (cell) cell.elevation = -1;

      const unit = unitManager.createUnit(UnitType.Infantry, 5, 5, 1);
      expect(unit).toBeNull();
    });

    it('should initialize unit stats from UnitStats', () => {
      const unit = unitManager.createUnit(UnitType.Infantry, 0, 0, 1);
      expect(unit?.health).toBeGreaterThan(0);
      expect(unit?.maxHealth).toBe(unit?.health);
      expect(unit?.attack).toBeGreaterThan(0);
      expect(unit?.defense).toBeGreaterThan(0);
    });

    it('should create different unit types', () => {
      const infantry = unitManager.createUnit(UnitType.Infantry, 0, 0, 1);
      const cavalry = unitManager.createUnit(UnitType.Cavalry, 1, 0, 1);
      const archer = unitManager.createUnit(UnitType.Archer, 2, 0, 1);

      expect(infantry?.type).toBe(UnitType.Infantry);
      expect(cavalry?.type).toBe(UnitType.Cavalry);
      expect(archer?.type).toBe(UnitType.Archer);
    });
  });

  describe('removeUnit', () => {
    it('should remove existing unit', () => {
      const unit = unitManager.createUnit(UnitType.Infantry, 0, 0, 1);
      expect(unit).not.toBeNull();

      const removed = unitManager.removeUnit(unit!.id);
      expect(removed).toBe(true);
      expect(unitManager.getUnit(unit!.id)).toBeUndefined();
    });

    it('should return false for non-existent unit', () => {
      expect(unitManager.removeUnit(999)).toBe(false);
    });

    it('should free up hex for new unit', () => {
      const unit1 = unitManager.createUnit(UnitType.Infantry, 0, 0, 1);
      unitManager.removeUnit(unit1!.id);

      const unit2 = unitManager.createUnit(UnitType.Cavalry, 0, 0, 2);
      expect(unit2).not.toBeNull();
    });
  });

  describe('moveUnit', () => {
    it('should move unit to valid position', () => {
      const unit = unitManager.createUnit(UnitType.Infantry, 0, 0, 1);
      const moved = unitManager.moveUnit(unit!.id, 1, 0);

      expect(moved).toBe(true);
      expect(unit?.q).toBe(1);
      expect(unit?.r).toBe(0);
    });

    it('should not move to occupied hex', () => {
      const unit1 = unitManager.createUnit(UnitType.Infantry, 0, 0, 1);
      unitManager.createUnit(UnitType.Cavalry, 1, 0, 1);

      const moved = unitManager.moveUnit(unit1!.id, 1, 0);
      expect(moved).toBe(false);
      expect(unit1?.q).toBe(0);
    });

    it('should not move to water', () => {
      const unit = unitManager.createUnit(UnitType.Infantry, 0, 0, 1);
      const cell = grid.getCellAt(2, 2);
      if (cell) cell.elevation = -1;

      const moved = unitManager.moveUnit(unit!.id, 2, 2);
      expect(moved).toBe(false);
    });

    it('should return false for non-existent unit', () => {
      expect(unitManager.moveUnit(999, 1, 1)).toBe(false);
    });

    it('should free old position for other units', () => {
      const unit1 = unitManager.createUnit(UnitType.Infantry, 0, 0, 1);
      unitManager.moveUnit(unit1!.id, 1, 0);

      const unit2 = unitManager.createUnit(UnitType.Cavalry, 0, 0, 2);
      expect(unit2).not.toBeNull();
    });
  });

  describe('getUnitAt', () => {
    it('should find unit at position', () => {
      const created = unitManager.createUnit(UnitType.Infantry, 3, 2, 1);
      const found = unitManager.getUnitAt(3, 2);
      expect(found).toBe(created);
    });

    it('should return null for empty position', () => {
      expect(unitManager.getUnitAt(0, 0)).toBeNull();
    });

    it('should return null after unit removed', () => {
      const unit = unitManager.createUnit(UnitType.Infantry, 0, 0, 1);
      unitManager.removeUnit(unit!.id);
      expect(unitManager.getUnitAt(0, 0)).toBeNull();
    });
  });

  describe('getUnit', () => {
    it('should get unit by ID', () => {
      const created = unitManager.createUnit(UnitType.Infantry, 0, 0, 1);
      const found = unitManager.getUnit(created!.id);
      expect(found).toBe(created);
    });

    it('should return undefined for invalid ID', () => {
      expect(unitManager.getUnit(999)).toBeUndefined();
    });
  });

  describe('getAllUnits', () => {
    it('should return all units', () => {
      unitManager.createUnit(UnitType.Infantry, 0, 0, 1);
      unitManager.createUnit(UnitType.Cavalry, 1, 0, 1);
      unitManager.createUnit(UnitType.Archer, 2, 0, 1);

      const all = unitManager.getAllUnits();
      expect(all).toHaveLength(3);
    });

    it('should return empty array when no units', () => {
      expect(unitManager.getAllUnits()).toHaveLength(0);
    });
  });

  describe('getPlayerUnits', () => {
    it('should filter units by player', () => {
      unitManager.createUnit(UnitType.Infantry, 0, 0, 1);
      unitManager.createUnit(UnitType.Cavalry, 1, 0, 1);
      unitManager.createUnit(UnitType.Archer, 2, 0, 2);

      const player1Units = unitManager.getPlayerUnits(1);
      const player2Units = unitManager.getPlayerUnits(2);

      expect(player1Units).toHaveLength(2);
      expect(player2Units).toHaveLength(1);
    });
  });

  describe('unitCount', () => {
    it('should track unit count', () => {
      expect(unitManager.unitCount).toBe(0);

      unitManager.createUnit(UnitType.Infantry, 0, 0, 1);
      expect(unitManager.unitCount).toBe(1);

      unitManager.createUnit(UnitType.Cavalry, 1, 0, 1);
      expect(unitManager.unitCount).toBe(2);
    });

    it('should decrement on removal', () => {
      const unit = unitManager.createUnit(UnitType.Infantry, 0, 0, 1);
      unitManager.removeUnit(unit!.id);
      expect(unitManager.unitCount).toBe(0);
    });
  });

  describe('clear', () => {
    it('should remove all units', () => {
      unitManager.createUnit(UnitType.Infantry, 0, 0, 1);
      unitManager.createUnit(UnitType.Cavalry, 1, 0, 1);

      unitManager.clear();

      expect(unitManager.unitCount).toBe(0);
      expect(unitManager.getAllUnits()).toHaveLength(0);
    });
  });

  describe('poolStats', () => {
    it('should report pool statistics', () => {
      unitManager.createUnit(UnitType.Infantry, 0, 0, 1);

      const stats = unitManager.poolStats;
      expect(stats.active).toBe(1);
      expect(stats.created).toBeGreaterThan(0);
    });

    it('should track reuse rate', () => {
      // Create and remove units to trigger pool reuse
      const unit = unitManager.createUnit(UnitType.Infantry, 0, 0, 1);
      unitManager.removeUnit(unit!.id);
      unitManager.createUnit(UnitType.Infantry, 0, 0, 1);

      const stats = unitManager.poolStats;
      expect(stats.reused).toBeGreaterThan(0);
    });
  });

  describe('getUnitsInRange', () => {
    it('should find units within range', () => {
      unitManager.createUnit(UnitType.Infantry, 0, 0, 1);
      unitManager.createUnit(UnitType.Cavalry, 1, 0, 1);
      unitManager.createUnit(UnitType.Archer, 5, 5, 1);

      const nearby = unitManager.getUnitsInRange(0, 0, 2);
      expect(nearby.length).toBeGreaterThanOrEqual(2);
    });
  });
});
