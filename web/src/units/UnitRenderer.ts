/**
 * Renders units using instanced meshes for performance.
 */
import * as THREE from 'three';
import { UnitManager } from './UnitManager';
import { UnitType, UnitData } from '../types';
import { HexCoordinates } from '../core/HexCoordinates';
import { HexMetrics } from '../core/HexMetrics';

// Player colors for land units
const PLAYER_COLORS_LAND = [
  new THREE.Color(0x4488ff), // Player 1: Blue
  new THREE.Color(0xff4444), // Player 2: Red
  new THREE.Color(0x44ff44), // Player 3: Green
  new THREE.Color(0xff8844), // Player 4: Orange
];

// Player colors for naval units (yellow/gold tones to distinguish)
const PLAYER_COLORS_NAVAL = [
  new THREE.Color(0xffff44), // Player 1: Yellow
  new THREE.Color(0xffcc00), // Player 2: Gold
  new THREE.Color(0xccff44), // Player 3: Lime Yellow
  new THREE.Color(0xffaa00), // Player 4: Amber
];

// Marine (amphibious) uses cyan/teal
const PLAYER_COLORS_AMPHIBIOUS = [
  new THREE.Color(0x44ffff), // Player 1: Cyan
  new THREE.Color(0x00cccc), // Player 2: Teal
  new THREE.Color(0x44ccff), // Player 3: Sky Blue
  new THREE.Color(0x00ffcc), // Player 4: Aqua
];

export class UnitRenderer {
  private scene: THREE.Scene;
  private unitManager: UnitManager;

  // Instanced meshes per unit type
  private infantryMesh: THREE.InstancedMesh | null = null;
  private cavalryMesh: THREE.InstancedMesh | null = null;
  private archerMesh: THREE.InstancedMesh | null = null;
  private galleyMesh: THREE.InstancedMesh | null = null;
  private warshipMesh: THREE.InstancedMesh | null = null;
  private marineMesh: THREE.InstancedMesh | null = null;

  // Geometries
  private infantryGeometry: THREE.BufferGeometry;
  private cavalryGeometry: THREE.BufferGeometry;
  private archerGeometry: THREE.BufferGeometry;
  private galleyGeometry: THREE.BufferGeometry;
  private warshipGeometry: THREE.BufferGeometry;
  private marineGeometry: THREE.BufferGeometry;

  // Shared material
  private material: THREE.MeshLambertMaterial;

  // Max instances per type
  private readonly maxInstances = 500;

  // Dirty flag for rebuilding
  private needsRebuild = true;

  // Instance index → unit ID mapping per mesh type
  private infantryUnitIds: number[] = [];
  private cavalryUnitIds: number[] = [];
  private archerUnitIds: number[] = [];
  private galleyUnitIds: number[] = [];
  private warshipUnitIds: number[] = [];
  private marineUnitIds: number[] = [];

  // Currently selected unit IDs
  private selectedUnitIds: Set<number> = new Set();

  constructor(scene: THREE.Scene, unitManager: UnitManager) {
    this.scene = scene;
    this.unitManager = unitManager;

    // Create geometries for each unit type
    this.infantryGeometry = this.createInfantryGeometry();
    this.cavalryGeometry = this.createCavalryGeometry();
    this.archerGeometry = this.createArcherGeometry();
    this.galleyGeometry = this.createGalleyGeometry();
    this.warshipGeometry = this.createWarshipGeometry();
    this.marineGeometry = this.createMarineGeometry();

    // Shared material with vertex colors
    this.material = new THREE.MeshLambertMaterial({
      vertexColors: false,
      flatShading: true,
    });
  }

  /**
   * Infantry: Simple capsule/cylinder shape
   */
  private createInfantryGeometry(): THREE.BufferGeometry {
    const body = new THREE.CylinderGeometry(0.15, 0.15, 0.4, 8);
    body.translate(0, 0.25, 0);

    const head = new THREE.SphereGeometry(0.1, 8, 6);
    head.translate(0, 0.5, 0);

    // Merge using BufferGeometryUtils would be better, but keeping simple
    const geometry = new THREE.CylinderGeometry(0.15, 0.18, 0.5, 8);
    geometry.translate(0, 0.25, 0);
    geometry.computeVertexNormals();

    return geometry;
  }

  /**
   * Cavalry: Taller, more horizontal shape
   */
  private createCavalryGeometry(): THREE.BufferGeometry {
    const geometry = new THREE.BoxGeometry(0.5, 0.35, 0.25);
    geometry.translate(0, 0.25, 0);

    // Add a "rider" on top
    const rider = new THREE.CylinderGeometry(0.08, 0.08, 0.25, 6);
    rider.translate(0, 0.5, 0);

    geometry.computeVertexNormals();
    return geometry;
  }

  /**
   * Archer: Triangular/pointed shape
   */
  private createArcherGeometry(): THREE.BufferGeometry {
    const geometry = new THREE.ConeGeometry(0.15, 0.5, 6);
    geometry.translate(0, 0.25, 0);
    geometry.computeVertexNormals();
    return geometry;
  }

  /**
   * Galley: Small boat shape (elongated box)
   */
  private createGalleyGeometry(): THREE.BufferGeometry {
    const geometry = new THREE.BoxGeometry(0.6, 0.2, 0.25);
    geometry.translate(0, 0.1, 0);
    geometry.computeVertexNormals();
    return geometry;
  }

  /**
   * Warship: Larger boat with more prominent shape
   */
  private createWarshipGeometry(): THREE.BufferGeometry {
    const geometry = new THREE.BoxGeometry(0.7, 0.3, 0.35);
    geometry.translate(0, 0.15, 0);
    geometry.computeVertexNormals();
    return geometry;
  }

  /**
   * Marine: Similar to infantry but with distinctive shape
   */
  private createMarineGeometry(): THREE.BufferGeometry {
    const geometry = new THREE.CylinderGeometry(0.12, 0.2, 0.45, 6);
    geometry.translate(0, 0.25, 0);
    geometry.computeVertexNormals();
    return geometry;
  }

  /**
   * Build or rebuild all unit meshes.
   */
  build(): void {
    this.dispose();

    // Clear mappings
    this.infantryUnitIds = [];
    this.cavalryUnitIds = [];
    this.archerUnitIds = [];
    this.galleyUnitIds = [];
    this.warshipUnitIds = [];
    this.marineUnitIds = [];

    const units = this.unitManager.getAllUnits();

    // Count units by type
    const byType = {
      [UnitType.Infantry]: units.filter(u => u.type === UnitType.Infantry),
      [UnitType.Cavalry]: units.filter(u => u.type === UnitType.Cavalry),
      [UnitType.Archer]: units.filter(u => u.type === UnitType.Archer),
      [UnitType.Galley]: units.filter(u => u.type === UnitType.Galley),
      [UnitType.Warship]: units.filter(u => u.type === UnitType.Warship),
      [UnitType.Marine]: units.filter(u => u.type === UnitType.Marine),
    };

    // Create infantry instances
    if (byType[UnitType.Infantry].length > 0) {
      const { mesh, unitIds } = this.createInstancedMesh(
        this.infantryGeometry,
        byType[UnitType.Infantry],
        'land'
      );
      this.infantryMesh = mesh;
      this.infantryUnitIds = unitIds;
      this.scene.add(this.infantryMesh);
    }

    // Create cavalry instances
    if (byType[UnitType.Cavalry].length > 0) {
      const { mesh, unitIds } = this.createInstancedMesh(
        this.cavalryGeometry,
        byType[UnitType.Cavalry],
        'land'
      );
      this.cavalryMesh = mesh;
      this.cavalryUnitIds = unitIds;
      this.scene.add(this.cavalryMesh);
    }

    // Create archer instances
    if (byType[UnitType.Archer].length > 0) {
      const { mesh, unitIds } = this.createInstancedMesh(
        this.archerGeometry,
        byType[UnitType.Archer],
        'land'
      );
      this.archerMesh = mesh;
      this.archerUnitIds = unitIds;
      this.scene.add(this.archerMesh);
    }

    // Create galley instances (naval - yellow)
    if (byType[UnitType.Galley].length > 0) {
      const { mesh, unitIds } = this.createInstancedMesh(
        this.galleyGeometry,
        byType[UnitType.Galley],
        'naval'
      );
      this.galleyMesh = mesh;
      this.galleyUnitIds = unitIds;
      this.scene.add(this.galleyMesh);
    }

    // Create warship instances (naval - yellow)
    if (byType[UnitType.Warship].length > 0) {
      const { mesh, unitIds } = this.createInstancedMesh(
        this.warshipGeometry,
        byType[UnitType.Warship],
        'naval'
      );
      this.warshipMesh = mesh;
      this.warshipUnitIds = unitIds;
      this.scene.add(this.warshipMesh);
    }

    // Create marine instances (amphibious - cyan)
    if (byType[UnitType.Marine].length > 0) {
      const { mesh, unitIds } = this.createInstancedMesh(
        this.marineGeometry,
        byType[UnitType.Marine],
        'amphibious'
      );
      this.marineMesh = mesh;
      this.marineUnitIds = unitIds;
      this.scene.add(this.marineMesh);
    }

    this.needsRebuild = false;

    // Reapply selection colors
    this.applySelectionColors();
  }

  /**
   * Create an instanced mesh for a set of units.
   * Returns both the mesh and the unit ID mapping.
   */
  private createInstancedMesh(
    geometry: THREE.BufferGeometry,
    units: UnitData[],
    domain: 'land' | 'naval' | 'amphibious' = 'land'
  ): { mesh: THREE.InstancedMesh; unitIds: number[] } {
    const mesh = new THREE.InstancedMesh(
      geometry,
      this.material.clone(),
      Math.min(units.length, this.maxInstances)
    );

    const matrix = new THREE.Matrix4();
    const position = new THREE.Vector3();
    const quaternion = new THREE.Quaternion();
    const scale = new THREE.Vector3(1, 1, 1);
    const unitIds: number[] = [];

    // Select color palette based on domain
    const colorPalette = domain === 'naval' ? PLAYER_COLORS_NAVAL
      : domain === 'amphibious' ? PLAYER_COLORS_AMPHIBIOUS
      : PLAYER_COLORS_LAND;

    for (let i = 0; i < units.length && i < this.maxInstances; i++) {
      const unit = units[i];
      unitIds.push(unit.id);

      const coords = new HexCoordinates(unit.q, unit.r);
      const cell = coords.toWorldPosition(0);

      // Get actual elevation from grid
      const elevation = this.getElevation(unit.q, unit.r);
      position.set(cell.x, elevation * HexMetrics.elevationStep, cell.z);

      matrix.compose(position, quaternion, scale);
      mesh.setMatrixAt(i, matrix);

      // Set color based on player and domain
      const color = colorPalette[unit.playerId % colorPalette.length];
      mesh.setColorAt(i, color);
    }

    mesh.instanceMatrix.needsUpdate = true;
    if (mesh.instanceColor) {
      mesh.instanceColor.needsUpdate = true;
    }

    mesh.castShadow = true;
    mesh.receiveShadow = true;

    return { mesh, unitIds };
  }

  /**
   * Get elevation at hex (from grid).
   */
  private getElevation(q: number, r: number): number {
    // Access grid through unitManager isn't ideal, but works for now
    // Could inject grid reference directly if needed
    return 0; // Default, will be on ground level
  }

  /**
   * Mark for rebuild (call when units change).
   */
  markDirty(): void {
    this.needsRebuild = true;
  }

  /**
   * Set which units are selected (updates visuals).
   */
  setSelectedUnits(selectedIds: Set<number>): void {
    this.selectedUnitIds = selectedIds;
    this.applySelectionColors();
  }

  /**
   * Apply selection colors to all meshes.
   */
  private applySelectionColors(): void {
    const selectedColor = new THREE.Color(0xffffff); // White for selected

    // Update land units
    this.updateMeshColors(this.infantryMesh, this.infantryUnitIds, selectedColor, PLAYER_COLORS_LAND);
    this.updateMeshColors(this.cavalryMesh, this.cavalryUnitIds, selectedColor, PLAYER_COLORS_LAND);
    this.updateMeshColors(this.archerMesh, this.archerUnitIds, selectedColor, PLAYER_COLORS_LAND);

    // Update naval units
    this.updateMeshColors(this.galleyMesh, this.galleyUnitIds, selectedColor, PLAYER_COLORS_NAVAL);
    this.updateMeshColors(this.warshipMesh, this.warshipUnitIds, selectedColor, PLAYER_COLORS_NAVAL);

    // Update amphibious units
    this.updateMeshColors(this.marineMesh, this.marineUnitIds, selectedColor, PLAYER_COLORS_AMPHIBIOUS);
  }

  /**
   * Update colors for a single instanced mesh.
   */
  private updateMeshColors(
    mesh: THREE.InstancedMesh | null,
    unitIds: number[],
    selectedColor: THREE.Color,
    colorPalette: THREE.Color[]
  ): void {
    if (!mesh || unitIds.length === 0) return;

    for (let i = 0; i < unitIds.length; i++) {
      const unitId = unitIds[i];
      const unit = this.unitManager.getUnit(unitId);
      if (!unit) continue;

      if (this.selectedUnitIds.has(unitId)) {
        mesh.setColorAt(i, selectedColor);
      } else {
        const playerColor = colorPalette[unit.playerId % colorPalette.length];
        mesh.setColorAt(i, playerColor);
      }
    }

    if (mesh.instanceColor) {
      mesh.instanceColor.needsUpdate = true;
    }
  }

  /**
   * Update (rebuild if dirty).
   */
  update(): void {
    if (this.needsRebuild) {
      this.build();
    }
  }

  /**
   * Dispose of resources.
   */
  dispose(): void {
    if (this.infantryMesh) {
      this.scene.remove(this.infantryMesh);
      this.infantryMesh.dispose();
      this.infantryMesh = null;
    }
    if (this.cavalryMesh) {
      this.scene.remove(this.cavalryMesh);
      this.cavalryMesh.dispose();
      this.cavalryMesh = null;
    }
    if (this.archerMesh) {
      this.scene.remove(this.archerMesh);
      this.archerMesh.dispose();
      this.archerMesh = null;
    }
    if (this.galleyMesh) {
      this.scene.remove(this.galleyMesh);
      this.galleyMesh.dispose();
      this.galleyMesh = null;
    }
    if (this.warshipMesh) {
      this.scene.remove(this.warshipMesh);
      this.warshipMesh.dispose();
      this.warshipMesh = null;
    }
    if (this.marineMesh) {
      this.scene.remove(this.marineMesh);
      this.marineMesh.dispose();
      this.marineMesh = null;
    }
  }

  /**
   * Full cleanup including geometries.
   */
  disposeAll(): void {
    this.dispose();
    this.infantryGeometry.dispose();
    this.cavalryGeometry.dispose();
    this.archerGeometry.dispose();
    this.galleyGeometry.dispose();
    this.warshipGeometry.dispose();
    this.marineGeometry.dispose();
    this.material.dispose();
  }
}
