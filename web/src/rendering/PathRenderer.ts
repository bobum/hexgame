import * as THREE from 'three';
import { HexCell } from '../types';
import { HexCoordinates } from '../core/HexCoordinates';
import { HexMetrics } from '../core/HexMetrics';

/**
 * Renders pathfinding visualization:
 * - Reachable cells (highlighted hexes)
 * - Path preview (line from unit to destination)
 */
export class PathRenderer {
  private scene: THREE.Scene;

  // Reachable cells visualization
  private reachableMeshes: THREE.InstancedMesh | null = null;
  private maxReachableInstances = 500;

  // Path line visualization
  private pathLine: THREE.Line | null = null;
  private pathLineMaterial: THREE.LineBasicMaterial;

  // Destination marker
  private destinationMarker: THREE.Mesh | null = null;

  constructor(scene: THREE.Scene) {
    this.scene = scene;

    // Create path line material
    this.pathLineMaterial = new THREE.LineBasicMaterial({
      color: 0x00ff00,
      linewidth: 2,
      transparent: true,
      opacity: 0.8,
    });

    this.createReachableMesh();
    this.createDestinationMarker();
  }

  /**
   * Create instanced mesh for reachable cells.
   */
  private createReachableMesh(): void {
    // Create a flat hexagon for highlighting
    const shape = new THREE.Shape();
    const radius = HexMetrics.outerRadius * 0.9;
    for (let i = 0; i < 6; i++) {
      const angle = (Math.PI / 3) * i - Math.PI / 6;
      const x = Math.cos(angle) * radius;
      const y = Math.sin(angle) * radius;
      if (i === 0) {
        shape.moveTo(x, y);
      } else {
        shape.lineTo(x, y);
      }
    }
    shape.closePath();

    const geometry = new THREE.ShapeGeometry(shape);
    geometry.rotateX(-Math.PI / 2);

    const material = new THREE.MeshBasicMaterial({
      color: 0x00ff00,
      transparent: true,
      opacity: 0.3,
      side: THREE.DoubleSide,
      depthWrite: false,
    });

    this.reachableMeshes = new THREE.InstancedMesh(
      geometry,
      material,
      this.maxReachableInstances
    );
    this.reachableMeshes.count = 0;
    this.reachableMeshes.frustumCulled = false;
    this.reachableMeshes.renderOrder = 1; // Render after terrain
    this.scene.add(this.reachableMeshes);
  }

  /**
   * Create destination marker mesh.
   */
  private createDestinationMarker(): void {
    const geometry = new THREE.RingGeometry(0.6, 0.8, 6);
    geometry.rotateX(-Math.PI / 2);
    geometry.rotateY(Math.PI / 6);

    const material = new THREE.MeshBasicMaterial({
      color: 0xff0000,
      transparent: true,
      opacity: 0.8,
      side: THREE.DoubleSide,
      depthWrite: false,
    });

    this.destinationMarker = new THREE.Mesh(geometry, material);
    this.destinationMarker.visible = false;
    this.destinationMarker.renderOrder = 2;
    this.scene.add(this.destinationMarker);
  }

  /**
   * Show reachable cells for a unit.
   */
  showReachableCells(reachableCells: Map<HexCell, number>): void {
    if (!this.reachableMeshes) return;

    const matrix = new THREE.Matrix4();
    const color = new THREE.Color();
    let index = 0;

    for (const [cell, cost] of reachableCells) {
      if (index >= this.maxReachableInstances) break;

      const coords = new HexCoordinates(cell.q, cell.r);
      // For water cells (elevation < 0), render at water surface level
      const renderElevation = Math.max(cell.elevation, 0);
      const worldPos = coords.toWorldPosition(renderElevation);

      matrix.makeTranslation(worldPos.x, worldPos.y + 0.05, worldPos.z);
      this.reachableMeshes.setMatrixAt(index, matrix);

      // Color based on movement cost (green = cheap, yellow = expensive)
      const t = Math.min(cost / 4, 1); // Normalize to 0-1 over 4 movement points
      color.setHSL(0.33 - t * 0.33, 0.8, 0.5); // Green to yellow
      this.reachableMeshes.setColorAt(index, color);

      index++;
    }

    this.reachableMeshes.count = index;
    this.reachableMeshes.instanceMatrix.needsUpdate = true;
    if (this.reachableMeshes.instanceColor) {
      this.reachableMeshes.instanceColor.needsUpdate = true;
    }
  }

  /**
   * Hide reachable cells.
   */
  hideReachableCells(): void {
    if (this.reachableMeshes) {
      this.reachableMeshes.count = 0;
    }
  }

  /**
   * Show path preview from unit to destination.
   */
  showPath(path: HexCell[]): void {
    // Remove old path line
    if (this.pathLine) {
      this.scene.remove(this.pathLine);
      this.pathLine.geometry.dispose();
      this.pathLine = null;
    }

    if (path.length < 2) {
      this.hideDestinationMarker();
      return;
    }

    // Create path points
    const points: THREE.Vector3[] = [];
    for (const cell of path) {
      const coords = new HexCoordinates(cell.q, cell.r);
      // For water cells (elevation < 0), render at water surface level
      const renderElevation = Math.max(cell.elevation, 0);
      const worldPos = coords.toWorldPosition(renderElevation);
      points.push(new THREE.Vector3(worldPos.x, worldPos.y + 0.2, worldPos.z));
    }

    // Create line geometry
    const geometry = new THREE.BufferGeometry().setFromPoints(points);
    this.pathLine = new THREE.Line(geometry, this.pathLineMaterial);
    this.pathLine.renderOrder = 2;
    this.scene.add(this.pathLine);

    // Show destination marker at end of path
    const lastCell = path[path.length - 1];
    this.showDestinationMarker(lastCell);
  }

  /**
   * Hide path preview.
   */
  hidePath(): void {
    if (this.pathLine) {
      this.scene.remove(this.pathLine);
      this.pathLine.geometry.dispose();
      this.pathLine = null;
    }
    this.hideDestinationMarker();
  }

  /**
   * Show destination marker at a cell.
   */
  private showDestinationMarker(cell: HexCell): void {
    if (!this.destinationMarker) return;

    const coords = new HexCoordinates(cell.q, cell.r);
    // For water cells (elevation < 0), render at water surface level
    const renderElevation = Math.max(cell.elevation, 0);
    const worldPos = coords.toWorldPosition(renderElevation);
    this.destinationMarker.position.set(worldPos.x, worldPos.y + 0.1, worldPos.z);
    this.destinationMarker.visible = true;
  }

  /**
   * Hide destination marker.
   */
  private hideDestinationMarker(): void {
    if (this.destinationMarker) {
      this.destinationMarker.visible = false;
    }
  }

  /**
   * Update path line color (for valid/invalid paths).
   */
  setPathValid(valid: boolean): void {
    this.pathLineMaterial.color.set(valid ? 0x00ff00 : 0xff0000);
  }

  /**
   * Clean up resources.
   */
  dispose(): void {
    if (this.reachableMeshes) {
      this.scene.remove(this.reachableMeshes);
      this.reachableMeshes.geometry.dispose();
      (this.reachableMeshes.material as THREE.Material).dispose();
      this.reachableMeshes = null;
    }

    if (this.pathLine) {
      this.scene.remove(this.pathLine);
      this.pathLine.geometry.dispose();
      this.pathLine = null;
    }

    this.pathLineMaterial.dispose();

    if (this.destinationMarker) {
      this.scene.remove(this.destinationMarker);
      this.destinationMarker.geometry.dispose();
      (this.destinationMarker.material as THREE.Material).dispose();
      this.destinationMarker = null;
    }
  }
}
