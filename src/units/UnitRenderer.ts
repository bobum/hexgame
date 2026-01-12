/**
 * Renders units using instanced meshes for performance.
 */
import * as THREE from 'three';
import { UnitManager } from './UnitManager';
import { UnitType, UnitData } from '../types';
import { HexCoordinates } from '../core/HexCoordinates';
import { HexMetrics } from '../core/HexMetrics';

// Player colors
const PLAYER_COLORS = [
  new THREE.Color(0x4488ff), // Player 1: Blue
  new THREE.Color(0xff4444), // Player 2: Red
  new THREE.Color(0x44ff44), // Player 3: Green
  new THREE.Color(0xffff44), // Player 4: Yellow
];

export class UnitRenderer {
  private scene: THREE.Scene;
  private unitManager: UnitManager;

  // Instanced meshes per unit type
  private infantryMesh: THREE.InstancedMesh | null = null;
  private cavalryMesh: THREE.InstancedMesh | null = null;
  private archerMesh: THREE.InstancedMesh | null = null;

  // Geometries
  private infantryGeometry: THREE.BufferGeometry;
  private cavalryGeometry: THREE.BufferGeometry;
  private archerGeometry: THREE.BufferGeometry;

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

  // Currently selected unit IDs
  private selectedUnitIds: Set<number> = new Set();

  constructor(scene: THREE.Scene, unitManager: UnitManager) {
    this.scene = scene;
    this.unitManager = unitManager;

    // Create geometries for each unit type
    this.infantryGeometry = this.createInfantryGeometry();
    this.cavalryGeometry = this.createCavalryGeometry();
    this.archerGeometry = this.createArcherGeometry();

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
   * Build or rebuild all unit meshes.
   */
  build(): void {
    this.dispose();

    // Clear mappings
    this.infantryUnitIds = [];
    this.cavalryUnitIds = [];
    this.archerUnitIds = [];

    const units = this.unitManager.getAllUnits();

    // Count units by type
    const byType = {
      [UnitType.Infantry]: units.filter(u => u.type === UnitType.Infantry),
      [UnitType.Cavalry]: units.filter(u => u.type === UnitType.Cavalry),
      [UnitType.Archer]: units.filter(u => u.type === UnitType.Archer),
    };

    // Create infantry instances
    if (byType[UnitType.Infantry].length > 0) {
      const { mesh, unitIds } = this.createInstancedMesh(
        this.infantryGeometry,
        byType[UnitType.Infantry]
      );
      this.infantryMesh = mesh;
      this.infantryUnitIds = unitIds;
      this.scene.add(this.infantryMesh);
    }

    // Create cavalry instances
    if (byType[UnitType.Cavalry].length > 0) {
      const { mesh, unitIds } = this.createInstancedMesh(
        this.cavalryGeometry,
        byType[UnitType.Cavalry]
      );
      this.cavalryMesh = mesh;
      this.cavalryUnitIds = unitIds;
      this.scene.add(this.cavalryMesh);
    }

    // Create archer instances
    if (byType[UnitType.Archer].length > 0) {
      const { mesh, unitIds } = this.createInstancedMesh(
        this.archerGeometry,
        byType[UnitType.Archer]
      );
      this.archerMesh = mesh;
      this.archerUnitIds = unitIds;
      this.scene.add(this.archerMesh);
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
    units: UnitData[]
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

      // Set color based on player
      const color = PLAYER_COLORS[unit.playerId % PLAYER_COLORS.length];
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

    // Update infantry
    this.updateMeshColors(this.infantryMesh, this.infantryUnitIds, selectedColor);

    // Update cavalry
    this.updateMeshColors(this.cavalryMesh, this.cavalryUnitIds, selectedColor);

    // Update archers
    this.updateMeshColors(this.archerMesh, this.archerUnitIds, selectedColor);
  }

  /**
   * Update colors for a single instanced mesh.
   */
  private updateMeshColors(
    mesh: THREE.InstancedMesh | null,
    unitIds: number[],
    selectedColor: THREE.Color
  ): void {
    if (!mesh || unitIds.length === 0) return;

    for (let i = 0; i < unitIds.length; i++) {
      const unitId = unitIds[i];
      const unit = this.unitManager.getUnit(unitId);
      if (!unit) continue;

      if (this.selectedUnitIds.has(unitId)) {
        mesh.setColorAt(i, selectedColor);
      } else {
        const playerColor = PLAYER_COLORS[unit.playerId % PLAYER_COLORS.length];
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
  }

  /**
   * Full cleanup including geometries.
   */
  disposeAll(): void {
    this.dispose();
    this.infantryGeometry.dispose();
    this.cavalryGeometry.dispose();
    this.archerGeometry.dispose();
    this.material.dispose();
  }
}
