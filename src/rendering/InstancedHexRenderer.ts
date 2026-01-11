import * as THREE from 'three';
import { HexGrid } from '../core/HexGrid';
import { HexCoordinates } from '../core/HexCoordinates';
import { HexMetrics, getTerrainColor, varyColor } from '../core/HexMetrics';
import { HexCell } from '../types';

/**
 * Renders hex terrain using GPU instancing.
 *
 * Uses a single hex geometry instanced for all cells.
 * Benefits:
 * - Fast per-hex updates (just update instance data)
 * - Lower memory for geometry
 * - Foundation for per-hex animations
 *
 * Trade-offs:
 * - Simpler geometry (no dynamic walls based on neighbors)
 * - Single draw call but may be less efficient than merged chunks for static terrain
 */
export class InstancedHexRenderer {
  private scene: THREE.Scene;
  private grid: HexGrid;

  private hexGeometry: THREE.BufferGeometry;
  private hexMesh: THREE.InstancedMesh | null = null;
  private material: THREE.MeshLambertMaterial;

  // Walls (merged geometry, not instanced - too complex to rotate correctly)
  private wallMesh: THREE.Mesh | null = null;
  private wallMaterial: THREE.MeshLambertMaterial;

  // Maps cell key to instance index
  private cellIndexMap: Map<string, number> = new Map();

  constructor(scene: THREE.Scene, grid: HexGrid) {
    this.scene = scene;
    this.grid = grid;

    // Create hex top geometry (flat hexagon)
    this.hexGeometry = this.createHexGeometry();

    // Materials
    this.material = new THREE.MeshLambertMaterial({
      vertexColors: false, // Using instance colors instead
      flatShading: true,
    });

    this.wallMaterial = new THREE.MeshLambertMaterial({
      vertexColors: true, // Walls use vertex colors like HexMeshBuilder
      flatShading: true,
    });
  }

  /**
   * Create a single hex top geometry centered at origin.
   */
  private createHexGeometry(): THREE.BufferGeometry {
    const vertices: number[] = [];
    const normals: number[] = [];

    const corners = HexMetrics.getCorners();

    // Create 6 triangles from center to edges
    for (let i = 0; i < 6; i++) {
      const c1 = corners[i];
      const c2 = corners[(i + 1) % 6];

      // Center vertex
      vertices.push(0, 0, 0);
      normals.push(0, 1, 0);

      // Two corner vertices (CCW winding)
      vertices.push(c2.x, 0, c2.z);
      normals.push(0, 1, 0);

      vertices.push(c1.x, 0, c1.z);
      normals.push(0, 1, 0);
    }

    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', new THREE.Float32BufferAttribute(vertices, 3));
    geometry.setAttribute('normal', new THREE.Float32BufferAttribute(normals, 3));

    return geometry;
  }


  /**
   * Build instanced meshes for all cells.
   */
  build(): void {
    this.dispose();

    // Collect all land cells
    const landCells: HexCell[] = [];
    for (const cell of this.grid.getAllCells()) {
      if (cell.elevation >= 0) {
        landCells.push(cell);
      }
    }

    if (landCells.length === 0) return;

    // Create hex instances
    this.hexMesh = new THREE.InstancedMesh(
      this.hexGeometry,
      this.material,
      landCells.length
    );

    const matrix = new THREE.Matrix4();
    const color = new THREE.Color();

    landCells.forEach((cell, i) => {
      const key = `${cell.q},${cell.r}`;
      this.cellIndexMap.set(key, i);

      const coords = new HexCoordinates(cell.q, cell.r);
      const pos = coords.toWorldPosition(cell.elevation);

      matrix.makeTranslation(pos.x, pos.y, pos.z);
      this.hexMesh!.setMatrixAt(i, matrix);

      color.copy(varyColor(getTerrainColor(cell.terrainType), 0.08));
      this.hexMesh!.setColorAt(i, color);
    });

    this.hexMesh.instanceMatrix.needsUpdate = true;
    if (this.hexMesh.instanceColor) {
      this.hexMesh.instanceColor.needsUpdate = true;
    }
    this.hexMesh.castShadow = true;
    this.hexMesh.receiveShadow = true;

    this.scene.add(this.hexMesh);

    // Build walls
    this.buildWalls(landCells);
  }

  /**
   * Get the edge index (0-5) that faces the given angle.
   * Matches HexMeshBuilder logic exactly.
   */
  private getEdgeIndexForAngle(angle: number): number {
    let normalizedAngle = angle;
    while (normalizedAngle < 0) normalizedAngle += Math.PI * 2;
    while (normalizedAngle >= Math.PI * 2) normalizedAngle -= Math.PI * 2;

    for (let i = 0; i < 6; i++) {
      const edgeFacingAngle = ((i + 1) * Math.PI / 3) % (Math.PI * 2);
      const diff = Math.abs(normalizedAngle - edgeFacingAngle);
      const diffWrapped = Math.min(diff, Math.PI * 2 - diff);
      if (diffWrapped < Math.PI / 6) {
        return i;
      }
    }
    return 0;
  }

  /**
   * Build walls as merged geometry (same approach as HexMeshBuilder).
   * Instancing walls is too complex due to rotation issues.
   */
  private buildWalls(landCells: HexCell[]): void {
    const vertices: number[] = [];
    const colors: number[] = [];
    const indices: number[] = [];
    let vertexIndex = 0;

    const corners = HexMetrics.getCorners();

    const addTriangle = (
      v1: THREE.Vector3,
      v2: THREE.Vector3,
      v3: THREE.Vector3,
      color: THREE.Color
    ) => {
      vertices.push(v1.x, v1.y, v1.z);
      vertices.push(v2.x, v2.y, v2.z);
      vertices.push(v3.x, v3.y, v3.z);

      colors.push(color.r, color.g, color.b);
      colors.push(color.r, color.g, color.b);
      colors.push(color.r, color.g, color.b);

      indices.push(vertexIndex, vertexIndex + 1, vertexIndex + 2);
      vertexIndex += 3;
    };

    for (const cell of landCells) {
      const coords = new HexCoordinates(cell.q, cell.r);
      const center = coords.toWorldPosition(cell.elevation);
      const wallColor = varyColor(getTerrainColor(cell.terrainType), 0.08).multiplyScalar(0.65);

      // Check all 6 directions
      for (let dir = 0; dir < 6; dir++) {
        const neighbor = this.grid.getNeighbor(cell, dir);

        let wallHeight = 0;
        if (!neighbor) {
          wallHeight = (cell.elevation - HexMetrics.minElevation) * HexMetrics.elevationStep;
        } else if (neighbor.elevation < cell.elevation) {
          wallHeight = (cell.elevation - neighbor.elevation) * HexMetrics.elevationStep;
        }

        if (wallHeight > 0) {
          // Calculate which edge this direction corresponds to
          const neighborCoords = coords.getNeighbor(dir);
          const neighborPos = neighborCoords.toWorldPosition(0);
          const dx = neighborPos.x - center.x;
          const dz = neighborPos.z - center.z;
          const angle = Math.atan2(dz, dx);
          const edgeIndex = this.getEdgeIndexForAngle(angle);

          // Get corner positions for this edge
          const corner1 = corners[edgeIndex];
          const corner2 = corners[(edgeIndex + 1) % 6];

          // Build wall quad (same as HexMeshBuilder.buildWallOnEdge)
          const topLeft = new THREE.Vector3(center.x + corner1.x, center.y, center.z + corner1.z);
          const topRight = new THREE.Vector3(center.x + corner2.x, center.y, center.z + corner2.z);
          const bottomLeft = new THREE.Vector3(topLeft.x, center.y - wallHeight, topLeft.z);
          const bottomRight = new THREE.Vector3(topRight.x, center.y - wallHeight, topRight.z);

          // Two triangles for the quad
          addTriangle(topLeft, bottomRight, bottomLeft, wallColor);
          addTriangle(topLeft, topRight, bottomRight, wallColor);
        }
      }
    }

    if (vertices.length === 0) return;

    // Create merged wall geometry
    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', new THREE.Float32BufferAttribute(vertices, 3));
    geometry.setAttribute('color', new THREE.Float32BufferAttribute(colors, 3));
    geometry.setIndex(indices);
    geometry.computeVertexNormals();

    this.wallMesh = new THREE.Mesh(geometry, this.wallMaterial);
    this.wallMesh.castShadow = true;
    this.wallMesh.receiveShadow = true;

    this.scene.add(this.wallMesh);
  }

  /**
   * Update a single cell's appearance (fast per-hex update).
   */
  updateCell(cell: HexCell): void {
    const key = `${cell.q},${cell.r}`;
    const index = this.cellIndexMap.get(key);

    if (index === undefined || !this.hexMesh) return;

    const coords = new HexCoordinates(cell.q, cell.r);
    const pos = coords.toWorldPosition(cell.elevation);

    const matrix = new THREE.Matrix4();
    matrix.makeTranslation(pos.x, pos.y, pos.z);
    this.hexMesh.setMatrixAt(index, matrix);

    const color = varyColor(getTerrainColor(cell.terrainType), 0.08);
    this.hexMesh.setColorAt(index, color);

    this.hexMesh.instanceMatrix.needsUpdate = true;
    if (this.hexMesh.instanceColor) {
      this.hexMesh.instanceColor.needsUpdate = true;
    }

    // Note: Walls would need full rebuild for elevation changes
  }

  /**
   * Get instance counts for debugging.
   */
  get hexCount(): number {
    return this.hexMesh?.count ?? 0;
  }

  get wallCount(): number {
    // Return triangle count for walls (merged geometry)
    return this.wallMesh?.geometry.index?.count ? this.wallMesh.geometry.index.count / 3 : 0;
  }

  dispose(): void {
    if (this.hexMesh) {
      this.scene.remove(this.hexMesh);
      this.hexMesh.dispose();
      this.hexMesh = null;
    }

    if (this.wallMesh) {
      this.scene.remove(this.wallMesh);
      this.wallMesh.geometry.dispose();
      this.wallMesh = null;
    }

    this.cellIndexMap.clear();
  }

  disposeAll(): void {
    this.dispose();
    this.hexGeometry.dispose();
    this.material.dispose();
    this.wallMaterial.dispose();
  }
}
