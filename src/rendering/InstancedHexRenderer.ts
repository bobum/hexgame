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

  // Walls (instanced)
  private wallGeometry: THREE.BufferGeometry;
  private wallMesh: THREE.InstancedMesh | null = null;
  private wallMaterial: THREE.MeshLambertMaterial;

  // Maps cell key to instance index
  private cellIndexMap: Map<string, number> = new Map();

  constructor(scene: THREE.Scene, grid: HexGrid) {
    this.scene = scene;
    this.grid = grid;

    // Create hex top geometry (flat hexagon)
    this.hexGeometry = this.createHexGeometry();

    // Create wall geometry (unit quad at origin, in YZ plane, facing +X)
    this.wallGeometry = this.createWallGeometry();

    // Materials
    this.material = new THREE.MeshLambertMaterial({
      vertexColors: false,
      flatShading: true,
    });

    this.wallMaterial = new THREE.MeshLambertMaterial({
      vertexColors: false,
      flatShading: true,
    });
  }

  /**
   * Create unit wall geometry: 1x1 quad at origin, in YZ plane, facing +X.
   * Will be scaled/rotated/translated per instance.
   */
  private createWallGeometry(): THREE.BufferGeometry {
    // Unit quad centered at origin, facing +X
    // Vertices in YZ plane from (-0.5,-0.5) to (0.5,0.5)
    const vertices = new Float32Array([
      // Triangle 1
      0, 0.5, -0.5,   // top-left
      0, -0.5, 0.5,   // bottom-right
      0, -0.5, -0.5,  // bottom-left
      // Triangle 2
      0, 0.5, -0.5,   // top-left
      0, 0.5, 0.5,    // top-right
      0, -0.5, 0.5,   // bottom-right
    ]);

    const normals = new Float32Array([
      1, 0, 0,
      1, 0, 0,
      1, 0, 0,
      1, 0, 0,
      1, 0, 0,
      1, 0, 0,
    ]);

    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', new THREE.BufferAttribute(vertices, 3));
    geometry.setAttribute('normal', new THREE.BufferAttribute(normals, 3));
    return geometry;
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
   * Build walls using instancing.
   * Unit quad at origin -> scale/rotate/translate to each edge.
   */
  private buildWalls(landCells: HexCell[]): void {
    const corners = HexMetrics.getCorners();
    const edgeLength = HexMetrics.outerRadius; // Hex edge length = outer radius

    // Collect wall data
    const wallData: {
      x: number; y: number; z: number;
      angle: number;
      height: number;
      color: THREE.Color
    }[] = [];

    for (const cell of landCells) {
      const coords = new HexCoordinates(cell.q, cell.r);
      const center = coords.toWorldPosition(cell.elevation);
      const wallColor = varyColor(getTerrainColor(cell.terrainType), 0.08).multiplyScalar(0.65);

      for (let dir = 0; dir < 6; dir++) {
        const neighbor = this.grid.getNeighbor(cell, dir);

        let wallHeight = 0;
        if (!neighbor) {
          wallHeight = (cell.elevation - HexMetrics.minElevation) * HexMetrics.elevationStep;
        } else if (neighbor.elevation < cell.elevation) {
          wallHeight = (cell.elevation - neighbor.elevation) * HexMetrics.elevationStep;
        }

        if (wallHeight > 0) {
          // Find which edge this direction corresponds to
          const neighborCoords = coords.getNeighbor(dir);
          const neighborPos = neighborCoords.toWorldPosition(0);
          const dx = neighborPos.x - center.x;
          const dz = neighborPos.z - center.z;
          const angle = Math.atan2(dz, dx);
          const edgeIndex = this.getEdgeIndexForAngle(angle);

          // Edge midpoint (relative to hex center)
          const c1 = corners[edgeIndex];
          const c2 = corners[(edgeIndex + 1) % 6];
          const midX = (c1.x + c2.x) / 2;
          const midZ = (c1.z + c2.z) / 2;

          // Edge facing angle (outward from center)
          const edgeAngle = Math.atan2(midZ, midX);

          // Wall center position in world space
          // Top of wall is at center.y, bottom at center.y - wallHeight
          // So wall center Y = center.y - wallHeight/2
          wallData.push({
            x: center.x + midX,
            y: center.y - wallHeight / 2,
            z: center.z + midZ,
            angle: edgeAngle,
            height: wallHeight,
            color: wallColor.clone(),
          });
        }
      }
    }

    if (wallData.length === 0) return;

    // Create instanced mesh
    this.wallMesh = new THREE.InstancedMesh(
      this.wallGeometry,
      this.wallMaterial,
      wallData.length
    );

    const matrix = new THREE.Matrix4();
    const position = new THREE.Vector3();
    const quaternion = new THREE.Quaternion();
    const scale = new THREE.Vector3();
    const yAxis = new THREE.Vector3(0, 1, 0);

    wallData.forEach((wall, i) => {
      position.set(wall.x, wall.y, wall.z);
      // Rotate by -angle because Y rotation takes +X toward -Z
      quaternion.setFromAxisAngle(yAxis, -wall.angle);
      scale.set(1, wall.height, edgeLength);

      matrix.compose(position, quaternion, scale);
      this.wallMesh!.setMatrixAt(i, matrix);
      this.wallMesh!.setColorAt(i, wall.color);
    });

    this.wallMesh.instanceMatrix.needsUpdate = true;
    if (this.wallMesh.instanceColor) {
      this.wallMesh.instanceColor.needsUpdate = true;
    }
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
    return this.wallMesh?.count ?? 0;
  }

  dispose(): void {
    if (this.hexMesh) {
      this.scene.remove(this.hexMesh);
      this.hexMesh.dispose();
      this.hexMesh = null;
    }

    if (this.wallMesh) {
      this.scene.remove(this.wallMesh);
      this.wallMesh.dispose();
      this.wallMesh = null;
    }

    this.cellIndexMap.clear();
  }

  disposeAll(): void {
    this.dispose();
    this.hexGeometry.dispose();
    this.wallGeometry.dispose();
    this.material.dispose();
    this.wallMaterial.dispose();
  }
}
