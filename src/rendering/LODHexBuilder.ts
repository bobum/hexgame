import * as THREE from 'three';
import { HexMetrics, getTerrainColor, varyColor } from '../core/HexMetrics';
import { HexCoordinates } from '../core/HexCoordinates';
import { HexCell } from '../types';
import { HexGrid } from '../core/HexGrid';

/**
 * LOD levels for terrain rendering.
 */
export enum LODLevel {
  High = 0,    // Full detail: hex tops + walls
  Medium = 1,  // Medium detail: hex tops only, no walls
  Low = 2,     // Low detail: single quad per hex
}

/**
 * Distance thresholds for LOD switching.
 */
export const LODDistances = {
  highToMedium: 20,   // Switch from high to medium at this distance
  mediumToLow: 40,    // Switch from medium to low at this distance
};

/**
 * Builds hex geometry at different LOD levels.
 */
export class LODHexBuilder {
  private vertices: number[] = [];
  private colors: number[] = [];
  private indices: number[] = [];
  private vertexIndex = 0;

  // Pre-calculated corner offsets
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
    this.reset();
  }

  reset(): void {
    this.vertices = [];
    this.colors = [];
    this.indices = [];
    this.vertexIndex = 0;
  }

  /**
   * Build geometry for a cell at MEDIUM LOD (hex top only, no walls).
   */
  buildCellMedium(cell: HexCell): void {
    const coords = new HexCoordinates(cell.q, cell.r);
    const center = coords.toWorldPosition(cell.elevation);
    const baseColor = varyColor(getTerrainColor(cell.terrainType), 0.08);

    // Just the top hexagon face, no walls
    for (let i = 0; i < 6; i++) {
      const corner1 = this.corners[i];
      const corner2 = this.corners[(i + 1) % 6];

      const v1 = center.clone();
      const v2 = new THREE.Vector3(center.x + corner1.x, center.y, center.z + corner1.z);
      const v3 = new THREE.Vector3(center.x + corner2.x, center.y, center.z + corner2.z);

      this.addTriangle(v1, v3, v2, baseColor);
    }
  }

  /**
   * Build geometry for a cell at LOW LOD (simple quad).
   */
  buildCellLow(cell: HexCell): void {
    const coords = new HexCoordinates(cell.q, cell.r);
    const center = coords.toWorldPosition(cell.elevation);
    const baseColor = varyColor(getTerrainColor(cell.terrainType), 0.08);

    // Simple quad using 2 triangles (covers roughly the hex area)
    const size = HexMetrics.outerRadius * 0.9;

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
    return geometry;
  }
}
