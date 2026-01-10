import * as THREE from 'three';
import { HexGrid } from '../core/HexGrid';
import { HexMeshBuilder } from './HexMeshBuilder';

/**
 * Renders the hex terrain as a single mesh with vertex colors.
 */
export class TerrainRenderer {
  private mesh: THREE.Mesh | null = null;
  private scene: THREE.Scene;
  private grid: HexGrid;

  constructor(scene: THREE.Scene, grid: HexGrid) {
    this.scene = scene;
    this.grid = grid;
  }

  /**
   * Build or rebuild the terrain mesh.
   */
  build(): void {
    // Remove existing mesh
    if (this.mesh) {
      this.scene.remove(this.mesh);
      this.mesh.geometry.dispose();
      (this.mesh.material as THREE.Material).dispose();
    }

    // Build new geometry
    const builder = new HexMeshBuilder();

    for (const cell of this.grid.getAllCells()) {
      // Skip water cells - they're rendered separately
      if (cell.elevation >= 0) {
        builder.buildCell(cell, this.grid);
      }
    }

    const geometry = builder.build();

    // Create material with vertex colors and flat shading
    const material = new THREE.MeshLambertMaterial({
      vertexColors: true,
      flatShading: true,
    });

    this.mesh = new THREE.Mesh(geometry, material);
    this.mesh.receiveShadow = true;
    this.mesh.castShadow = true;
    this.scene.add(this.mesh);
  }

  /**
   * Dispose of resources.
   */
  dispose(): void {
    if (this.mesh) {
      this.scene.remove(this.mesh);
      this.mesh.geometry.dispose();
      (this.mesh.material as THREE.Material).dispose();
      this.mesh = null;
    }
  }
}
