import * as THREE from 'three';
import { HexMetrics, getTerrainColor, varyColor } from '../core/HexMetrics';
import { HexCoordinates } from '../core/HexCoordinates';
import { HexCell } from '../types';

/**
 * Distance thresholds for LOD switching (in world units).
 */
export const LODDistances = {
  highToMedium: 30,
  mediumToLow: 60,
};

/**
 * Builds simplified hex geometry for lower LOD levels.
 */
export class LODHexBuilder {
  private vertices: number[] = [];
  private colors: number[] = [];
  private indices: number[] = [];
  private vertexIndex = 0;

  private corners: THREE.Vector3[];

  constructor() {
    this.corners = [];
    for (let i = 0; i < 6; i++) {
      const angle = (Math.PI / 3) * i + Math.PI / 6;
      this.corners.push(new THREE.Vector3(
        Math.cos(angle) * HexMetrics.outerRadius,
        0,
        Math.sin(angle) * HexMetrics.outerRadius
      ));
    }
  }

  reset(): void {
    this.vertices = [];
    this.colors = [];
    this.indices = [];
    this.vertexIndex = 0;
  }

  /**
   * Build MEDIUM detail: hex top only, no walls.
   */
  buildCellMedium(cell: HexCell): void {
    const coords = new HexCoordinates(cell.q, cell.r);
    const center = coords.toWorldPosition(cell.elevation);
    const baseColor = varyColor(getTerrainColor(cell.terrainType), 0.08);

    // Build hex top as 6 triangles from center
    for (let i = 0; i < 6; i++) {
      const corner1 = this.corners[i];
      const corner2 = this.corners[(i + 1) % 6];

      const v1 = new THREE.Vector3(center.x, center.y, center.z);
      const v2 = new THREE.Vector3(center.x + corner1.x, center.y, center.z + corner1.z);
      const v3 = new THREE.Vector3(center.x + corner2.x, center.y, center.z + corner2.z);

      this.addTriangle(v1, v3, v2, baseColor);
    }
  }

  /**
   * Build LOW detail: single quad per hex.
   */
  buildCellLow(cell: HexCell): void {
    const coords = new HexCoordinates(cell.q, cell.r);
    const center = coords.toWorldPosition(cell.elevation);
    const baseColor = varyColor(getTerrainColor(cell.terrainType), 0.08);

    // Simple quad approximation
    const size = HexMetrics.outerRadius * 0.85;

    const v1 = new THREE.Vector3(center.x - size, center.y, center.z - size);
    const v2 = new THREE.Vector3(center.x + size, center.y, center.z - size);
    const v3 = new THREE.Vector3(center.x + size, center.y, center.z + size);
    const v4 = new THREE.Vector3(center.x - size, center.y, center.z + size);

    this.addTriangle(v1, v2, v3, baseColor);
    this.addTriangle(v1, v3, v4, baseColor);
  }

  private addTriangle(
    v1: THREE.Vector3,
    v2: THREE.Vector3,
    v3: THREE.Vector3,
    color: THREE.Color
  ): void {
    this.vertices.push(v1.x, v1.y, v1.z);
    this.vertices.push(v2.x, v2.y, v2.z);
    this.vertices.push(v3.x, v3.y, v3.z);

    this.colors.push(color.r, color.g, color.b);
    this.colors.push(color.r, color.g, color.b);
    this.colors.push(color.r, color.g, color.b);

    this.indices.push(this.vertexIndex, this.vertexIndex + 1, this.vertexIndex + 2);
    this.vertexIndex += 3;
  }

  build(): THREE.BufferGeometry {
    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', new THREE.Float32BufferAttribute(this.vertices, 3));
    geometry.setAttribute('color', new THREE.Float32BufferAttribute(this.colors, 3));
    geometry.setIndex(this.indices);
    geometry.computeVertexNormals();
    geometry.computeBoundingSphere();
    return geometry;
  }
}
