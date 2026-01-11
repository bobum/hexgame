import * as THREE from 'three';
import { HexMetrics, getTerrainColor, varyColor, getTerrainTypeIndex } from '../core/HexMetrics';
import { HexCoordinates } from '../core/HexCoordinates';
import { HexCell, TerrainType } from '../types';

// Base Y level for skirts
const SKIRT_BASE_Y = HexMetrics.minElevation * HexMetrics.elevationStep - 1;

/**
 * Distance thresholds for LOD switching (in world units).
 */
export const LODDistances = {
  highToMedium: 30,
  mediumToLow: 60,
};

/**
 * Builds simplified hex geometry for lower LOD levels.
 * Includes terrain type attribute for shader-based rendering.
 */
export class LODHexBuilder {
  private vertices: number[] = [];
  private colors: number[] = [];
  private terrainTypes: number[] = [];
  // Splat attributes (needed for shader compatibility)
  private color1: number[] = [];
  private color2: number[] = [];
  private color3: number[] = [];
  private splatWeights: number[] = [];
  private indices: number[] = [];
  private vertexIndex = 0;
  private currentTerrainType: TerrainType = TerrainType.Plains;
  private currentColor = new THREE.Color();

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
    this.terrainTypes = [];
    this.color1 = [];
    this.color2 = [];
    this.color3 = [];
    this.splatWeights = [];
    this.indices = [];
    this.vertexIndex = 0;
  }

  /**
   * Build MEDIUM detail: hex top only, no walls, plus skirt.
   */
  buildCellMedium(cell: HexCell): void {
    const coords = new HexCoordinates(cell.q, cell.r);
    const center = coords.toWorldPosition(cell.elevation);
    const baseColor = varyColor(getTerrainColor(cell.terrainType), 0.08);
    this.currentTerrainType = cell.terrainType;
    this.currentColor.copy(baseColor);

    // Build hex top as 6 triangles from center
    for (let i = 0; i < 6; i++) {
      const corner1 = this.corners[i];
      const corner2 = this.corners[(i + 1) % 6];

      const v1 = new THREE.Vector3(center.x, center.y, center.z);
      const v2 = new THREE.Vector3(center.x + corner1.x, center.y, center.z + corner1.z);
      const v3 = new THREE.Vector3(center.x + corner2.x, center.y, center.z + corner2.z);

      this.addTriangle(v1, v3, v2, baseColor);
    }

    // Add skirt around hex perimeter
    this.buildHexSkirt(center, baseColor);
  }

  /**
   * Build LOW detail: single quad per hex, plus skirt.
   */
  buildCellLow(cell: HexCell): void {
    const coords = new HexCoordinates(cell.q, cell.r);
    const center = coords.toWorldPosition(cell.elevation);
    const baseColor = varyColor(getTerrainColor(cell.terrainType), 0.08);
    this.currentTerrainType = cell.terrainType;
    this.currentColor.copy(baseColor);

    // Simple quad approximation
    const size = HexMetrics.outerRadius * 0.85;

    const v1 = new THREE.Vector3(center.x - size, center.y, center.z - size);
    const v2 = new THREE.Vector3(center.x + size, center.y, center.z - size);
    const v3 = new THREE.Vector3(center.x + size, center.y, center.z + size);
    const v4 = new THREE.Vector3(center.x - size, center.y, center.z + size);

    this.addTriangle(v1, v2, v3, baseColor);
    this.addTriangle(v1, v3, v4, baseColor);

    // Add simple box skirt for low LOD
    this.buildBoxSkirt(center, size, baseColor);
  }

  /**
   * Build hex-shaped skirt around perimeter (for medium LOD).
   */
  private buildHexSkirt(center: THREE.Vector3, color: THREE.Color): void {
    const darkerColor = color.clone().multiplyScalar(0.6); // Darker for sides

    for (let i = 0; i < 6; i++) {
      const corner1 = this.corners[i];
      const corner2 = this.corners[(i + 1) % 6];

      // Four corners of the skirt quad
      const topLeft = new THREE.Vector3(center.x + corner1.x, center.y, center.z + corner1.z);
      const topRight = new THREE.Vector3(center.x + corner2.x, center.y, center.z + corner2.z);
      const bottomLeft = new THREE.Vector3(center.x + corner1.x, SKIRT_BASE_Y, center.z + corner1.z);
      const bottomRight = new THREE.Vector3(center.x + corner2.x, SKIRT_BASE_Y, center.z + corner2.z);

      // Two triangles for the quad (facing outward)
      this.addTriangle(topLeft, bottomLeft, bottomRight, darkerColor);
      this.addTriangle(topLeft, bottomRight, topRight, darkerColor);
    }
  }

  /**
   * Build box-shaped skirt (for low LOD quads).
   */
  private buildBoxSkirt(center: THREE.Vector3, size: number, color: THREE.Color): void {
    const darkerColor = color.clone().multiplyScalar(0.6);
    const y = center.y;

    // Four walls
    const corners = [
      new THREE.Vector3(center.x - size, y, center.z - size),
      new THREE.Vector3(center.x + size, y, center.z - size),
      new THREE.Vector3(center.x + size, y, center.z + size),
      new THREE.Vector3(center.x - size, y, center.z + size),
    ];

    for (let i = 0; i < 4; i++) {
      const c1 = corners[i];
      const c2 = corners[(i + 1) % 4];

      const topLeft = c1.clone();
      const topRight = c2.clone();
      const bottomLeft = new THREE.Vector3(c1.x, SKIRT_BASE_Y, c1.z);
      const bottomRight = new THREE.Vector3(c2.x, SKIRT_BASE_Y, c2.z);

      this.addTriangle(topLeft, bottomLeft, bottomRight, darkerColor);
      this.addTriangle(topLeft, bottomRight, topRight, darkerColor);
    }
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

    // Add terrain type for each vertex (for shader)
    const terrainTypeFloat = getTerrainTypeIndex(this.currentTerrainType);
    this.terrainTypes.push(terrainTypeFloat, terrainTypeFloat, terrainTypeFloat);

    // Splat attributes: all 3 colors same, weight 100% on first (no blending for LOD)
    for (let i = 0; i < 3; i++) {
      this.color1.push(this.currentColor.r, this.currentColor.g, this.currentColor.b);
      this.color2.push(this.currentColor.r, this.currentColor.g, this.currentColor.b);
      this.color3.push(this.currentColor.r, this.currentColor.g, this.currentColor.b);
      this.splatWeights.push(1, 0, 0);  // 100% main color
    }

    this.indices.push(this.vertexIndex, this.vertexIndex + 1, this.vertexIndex + 2);
    this.vertexIndex += 3;
  }

  build(): THREE.BufferGeometry {
    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', new THREE.Float32BufferAttribute(this.vertices, 3));
    geometry.setAttribute('color', new THREE.Float32BufferAttribute(this.colors, 3));
    // For terrain shader compatibility
    geometry.setAttribute('terrainColor', new THREE.Float32BufferAttribute(this.colors, 3));
    geometry.setAttribute('terrainType', new THREE.Float32BufferAttribute(this.terrainTypes, 1));
    // Splat map attributes (no blending for LOD, but shader needs them)
    geometry.setAttribute('splatColor1', new THREE.Float32BufferAttribute(this.color1, 3));
    geometry.setAttribute('splatColor2', new THREE.Float32BufferAttribute(this.color2, 3));
    geometry.setAttribute('splatColor3', new THREE.Float32BufferAttribute(this.color3, 3));
    geometry.setAttribute('splatWeights', new THREE.Float32BufferAttribute(this.splatWeights, 3));
    geometry.setIndex(this.indices);
    geometry.computeVertexNormals();
    geometry.computeBoundingSphere();
    return geometry;
  }
}
