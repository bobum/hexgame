import * as THREE from 'three';
import { HexGrid } from '../core/HexGrid';
import { HexCoordinates } from '../core/HexCoordinates';
import { HexMeshBuilder } from './HexMeshBuilder';
import { HexCell } from '../types';

/**
 * A chunk of terrain containing multiple hex cells.
 */
interface TerrainChunk {
  mesh: THREE.Mesh | null;
  cells: HexCell[];
  dirty: boolean;
  chunkX: number;
  chunkZ: number;
}

/**
 * Renders hex terrain using a chunk-based system for better performance.
 * Each chunk is a separate mesh, enabling:
 * - Frustum culling per chunk
 * - Dirty-flag rebuilding (only rebuild changed chunks)
 * - Future: LOD per chunk, streaming
 */
export class ChunkedTerrainRenderer {
  private scene: THREE.Scene;
  private grid: HexGrid;
  private chunks: Map<string, TerrainChunk> = new Map();
  private material: THREE.MeshLambertMaterial;

  // Chunk size in hex cells (world units depend on hex metrics)
  static readonly CHUNK_SIZE = 16;

  constructor(scene: THREE.Scene, grid: HexGrid) {
    this.scene = scene;
    this.grid = grid;

    // Shared material for all chunks
    this.material = new THREE.MeshLambertMaterial({
      vertexColors: true,
      flatShading: true,
    });
  }

  /**
   * Get chunk key from chunk coordinates.
   */
  private getChunkKey(chunkX: number, chunkZ: number): string {
    return `${chunkX},${chunkZ}`;
  }

  /**
   * Get chunk coordinates for a cell based on its world position.
   */
  private getCellChunkCoords(cell: HexCell): { chunkX: number; chunkZ: number } {
    const coords = new HexCoordinates(cell.q, cell.r);
    const worldPos = coords.toWorldPosition(0);

    // Use world position to determine chunk
    const chunkX = Math.floor(worldPos.x / ChunkedTerrainRenderer.CHUNK_SIZE);
    const chunkZ = Math.floor(worldPos.z / ChunkedTerrainRenderer.CHUNK_SIZE);

    return { chunkX, chunkZ };
  }

  /**
   * Build or rebuild all terrain chunks.
   */
  build(): void {
    // Clear existing chunks
    this.dispose();

    // Group cells into chunks
    for (const cell of this.grid.getAllCells()) {
      // Skip water cells
      if (cell.elevation < 0) continue;

      const { chunkX, chunkZ } = this.getCellChunkCoords(cell);
      const key = this.getChunkKey(chunkX, chunkZ);

      let chunk = this.chunks.get(key);
      if (!chunk) {
        chunk = {
          mesh: null,
          cells: [],
          dirty: true,
          chunkX,
          chunkZ,
        };
        this.chunks.set(key, chunk);
      }

      chunk.cells.push(cell);
    }

    // Build mesh for each chunk
    for (const chunk of this.chunks.values()) {
      this.buildChunkMesh(chunk);
    }
  }

  /**
   * Build the mesh for a single chunk.
   */
  private buildChunkMesh(chunk: TerrainChunk): void {
    // Remove old mesh if exists
    if (chunk.mesh) {
      this.scene.remove(chunk.mesh);
      chunk.mesh.geometry.dispose();
    }

    if (chunk.cells.length === 0) {
      chunk.mesh = null;
      chunk.dirty = false;
      return;
    }

    // Build geometry for all cells in chunk
    const builder = new HexMeshBuilder();

    for (const cell of chunk.cells) {
      builder.buildCell(cell, this.grid);
    }

    const geometry = builder.build();

    // Create mesh
    chunk.mesh = new THREE.Mesh(geometry, this.material);
    chunk.mesh.receiveShadow = true;
    chunk.mesh.castShadow = true;

    // Set frustum culling hint - compute bounding box
    geometry.computeBoundingBox();
    geometry.computeBoundingSphere();

    // Name for debugging
    chunk.mesh.name = `chunk_${chunk.chunkX}_${chunk.chunkZ}`;

    this.scene.add(chunk.mesh);
    chunk.dirty = false;
  }

  /**
   * Mark a cell's chunk as dirty (needs rebuild).
   */
  markCellDirty(cell: HexCell): void {
    const { chunkX, chunkZ } = this.getCellChunkCoords(cell);
    const key = this.getChunkKey(chunkX, chunkZ);
    const chunk = this.chunks.get(key);

    if (chunk) {
      chunk.dirty = true;
    }
  }

  /**
   * Rebuild any dirty chunks.
   */
  rebuildDirtyChunks(): void {
    for (const chunk of this.chunks.values()) {
      if (chunk.dirty) {
        this.buildChunkMesh(chunk);
      }
    }
  }

  /**
   * Get chunk count for debugging.
   */
  get chunkCount(): number {
    return this.chunks.size;
  }

  /**
   * Get visible chunk count (for frustum culling stats).
   */
  getVisibleChunkCount(camera: THREE.Camera): number {
    const frustum = new THREE.Frustum();
    const projScreenMatrix = new THREE.Matrix4();

    projScreenMatrix.multiplyMatrices(
      camera.projectionMatrix,
      camera.matrixWorldInverse
    );
    frustum.setFromProjectionMatrix(projScreenMatrix);

    let visible = 0;
    for (const chunk of this.chunks.values()) {
      if (chunk.mesh && chunk.mesh.geometry.boundingSphere) {
        const sphere = chunk.mesh.geometry.boundingSphere.clone();
        sphere.applyMatrix4(chunk.mesh.matrixWorld);
        if (frustum.intersectsSphere(sphere)) {
          visible++;
        }
      }
    }

    return visible;
  }

  /**
   * Dispose of all resources.
   */
  dispose(): void {
    for (const chunk of this.chunks.values()) {
      if (chunk.mesh) {
        this.scene.remove(chunk.mesh);
        chunk.mesh.geometry.dispose();
      }
    }
    this.chunks.clear();
  }

  /**
   * Full cleanup including material.
   */
  disposeAll(): void {
    this.dispose();
    this.material.dispose();
  }
}
