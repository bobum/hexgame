import * as THREE from 'three';
import { HexMetrics, getTerrainColor, varyColor } from '../core/HexMetrics';
import { HexCoordinates } from '../core/HexCoordinates';
import { HexDirection } from '../core/HexDirection';
import { HexCell } from '../types';
import { HexGrid } from '../core/HexGrid';

/**
 * Builds hex mesh geometry with flat shading and vertex colors.
 */
export class HexMeshBuilder {
  private vertices: number[] = [];
  private colors: number[] = [];
  private indices: number[] = [];
  private vertexIndex = 0;

  // Pre-calculated corner offsets for a flat-topped hex
  // Corners at 30°, 90°, 150°, 210°, 270°, 330°
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
   * Build geometry for a single hex cell.
   */
  buildCell(cell: HexCell, grid: HexGrid): void {
    const coords = new HexCoordinates(cell.q, cell.r);
    const center = coords.toWorldPosition(cell.elevation);
    const baseColor = varyColor(getTerrainColor(cell.terrainType), 0.08);

    // 1. Build the top hexagon face
    this.buildTopFace(center, baseColor);

    // 2. Build walls for each of the 6 edges
    for (let edge = 0; edge < 6; edge++) {
      // Check neighbor in the corresponding direction
      const neighbor = grid.getNeighbor(cell, edge as HexDirection);

      let wallHeight = 0;
      if (!neighbor) {
        // Edge of map - wall down to min elevation
        wallHeight = (cell.elevation - HexMetrics.minElevation) * HexMetrics.elevationStep;
      } else if (neighbor.elevation < cell.elevation) {
        // Neighbor is lower - wall down to neighbor's elevation
        wallHeight = (cell.elevation - neighbor.elevation) * HexMetrics.elevationStep;
      }

      if (wallHeight > 0) {
        // Calculate which edge this direction corresponds to
        // by finding which edge faces toward the neighbor's position
        const neighborCoords = coords.getNeighbor(edge as HexDirection);
        const neighborPos = neighborCoords.toWorldPosition(0);

        // Direction from cell center to neighbor center
        const dx = neighborPos.x - center.x;
        const dz = neighborPos.z - center.z;
        const angle = Math.atan2(dz, dx);

        // Find which edge faces this angle
        // Edge i is between corners[i] and corners[(i+1)%6]
        // Each edge faces outward at angle: (i * 60° + 60°) converted to radians
        const edgeIndex = this.getEdgeIndexForAngle(angle);

        this.buildWallOnEdge(center, edgeIndex, wallHeight, baseColor);
      }
    }
  }

  /**
   * Get the edge index (0-5) that faces the given angle.
   */
  private getEdgeIndexForAngle(angle: number): number {
    // Normalize angle to 0 to 2*PI
    let normalizedAngle = angle;
    while (normalizedAngle < 0) normalizedAngle += Math.PI * 2;
    while (normalizedAngle >= Math.PI * 2) normalizedAngle -= Math.PI * 2;

    // Each edge spans 60 degrees (PI/3 radians)
    // Edge 0 (between corners 0 and 1) faces outward at 60° (PI/3)
    // But corners start at 30°, so edge 0 faces at 60°
    // Edge i faces at (i+1) * 60° = (i+1) * PI/3

    // We need to find which edge's facing angle is closest to our target angle
    // Edge i faces at angle: (i * PI/3) + PI/6 + PI/2 = i * PI/3 + 2*PI/3
    // Actually, let's compute it differently:
    // Corner i is at angle (i * PI/3 + PI/6)
    // Edge i (from corner i to corner i+1) faces perpendicular to the edge line
    // The outward normal of edge i points at angle: (corner_i_angle + corner_{i+1}_angle) / 2 + PI/2

    // Simpler: edge 0 is between corners at 30° and 90°, so it faces at 60° (average)
    // But the OUTWARD direction is perpendicular, so it faces at 60° - 90° = -30° or equivalently 330°
    // Hmm, that's not right either.

    // Let me think again. The edge between corner 0 (30°) and corner 1 (90°)
    // is a line segment. The outward normal points away from center.
    // The midpoint of this edge is at angle 60°, so the outward normal points at 60°.

    // So edge i faces at angle: (i + 0.5) * PI/3 + PI/6 = (i + 1) * PI/3
    // Edge 0 faces at PI/3 = 60°
    // Edge 1 faces at 2*PI/3 = 120°
    // Edge 2 faces at PI = 180°
    // Edge 3 faces at 4*PI/3 = 240°
    // Edge 4 faces at 5*PI/3 = 300°
    // Edge 5 faces at 2*PI = 360° = 0°

    // Find which edge faces closest to our angle
    for (let i = 0; i < 6; i++) {
      const edgeFacingAngle = ((i + 1) * Math.PI / 3) % (Math.PI * 2);
      const diff = Math.abs(normalizedAngle - edgeFacingAngle);
      const diffWrapped = Math.min(diff, Math.PI * 2 - diff);
      if (diffWrapped < Math.PI / 6) { // Within 30 degrees
        return i;
      }
    }

    // Fallback - shouldn't happen
    return 0;
  }

  /**
   * Build the top hexagonal face.
   */
  private buildTopFace(center: THREE.Vector3, color: THREE.Color): void {
    for (let i = 0; i < 6; i++) {
      const corner1 = this.corners[i];
      const corner2 = this.corners[(i + 1) % 6];

      const v1 = center.clone();
      const v2 = new THREE.Vector3(center.x + corner1.x, center.y, center.z + corner1.z);
      const v3 = new THREE.Vector3(center.x + corner2.x, center.y, center.z + corner2.z);

      // CCW winding for upward normal
      this.addTriangle(v1, v3, v2, color);
    }
  }

  /**
   * Build a wall on a specific edge of the hex.
   */
  private buildWallOnEdge(
    center: THREE.Vector3,
    edgeIndex: number,
    height: number,
    baseColor: THREE.Color
  ): void {
    const wallColor = baseColor.clone().multiplyScalar(0.65);

    const corner1 = this.corners[edgeIndex];
    const corner2 = this.corners[(edgeIndex + 1) % 6];

    const topLeft = new THREE.Vector3(center.x + corner1.x, center.y, center.z + corner1.z);
    const topRight = new THREE.Vector3(center.x + corner2.x, center.y, center.z + corner2.z);
    const bottomLeft = new THREE.Vector3(topLeft.x, center.y - height, topLeft.z);
    const bottomRight = new THREE.Vector3(topRight.x, center.y - height, topRight.z);

    // Two triangles for the quad - reversed winding for outward-facing normal
    this.addTriangle(topLeft, bottomRight, bottomLeft, wallColor);
    this.addTriangle(topLeft, topRight, bottomRight, wallColor);
  }

  /**
   * Add a triangle.
   */
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

  /**
   * Create the final BufferGeometry.
   */
  build(): THREE.BufferGeometry {
    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', new THREE.Float32BufferAttribute(this.vertices, 3));
    geometry.setAttribute('color', new THREE.Float32BufferAttribute(this.colors, 3));
    geometry.setIndex(this.indices);
    geometry.computeVertexNormals();
    return geometry;
  }
}
