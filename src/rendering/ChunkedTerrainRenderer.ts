import * as THREE from 'three';
import { HexGrid } from '../core/HexGrid';
import { HexCoordinates } from '../core/HexCoordinates';
import { HexMeshBuilder } from './HexMeshBuilder';
import { LODHexBuilder, LODLevel, LODDistances } from './LODHexBuilder';
import { HexCell } from '../types';

/**
 * A chunk of terrain containing multiple hex cells with LOD support.
 */
interface TerrainChunk {
  lod: THREE.LOD | null;
  cells: HexCell[];
  dirty: boolean;
  chunkX: number;
  chunkZ: number;
  centerX: number;
  centerZ: number;
}

/**
 * Renders hex terrain using a chunk-based system with LOD for better performance.
 * Each chunk has 3 detail levels:
 * - High: Full hex geometry with walls (near camera)
 * - Medium: Hex tops only, no walls (mid-range)
 * - Low: Simple quads (far from camera)
 */
export class ChunkedTerrainRenderer {
  private scene: THREE.Scene;
  private grid: HexGrid;
  private chunks: Map<string, TerrainChunk> = new Map();
  private material: THREE.MeshLambertMaterial;

  // Chunk size in hex cells (world units depend on hex metrics)
  static readonly CHUNK_SIZE = 16;

  // LOD statistics
  private lodStats = { high: 0, medium: 0, low: 0 };

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
          lod: null,
          cells: [],
          dirty: true,
          chunkX,
          chunkZ,
          centerX: (chunkX + 0.5) * ChunkedTerrainRenderer.CHUNK_SIZE,
          centerZ: (chunkZ + 0.5) * ChunkedTerrainRenderer.CHUNK_SIZE,
        };
        this.chunks.set(key, chunk);
      }

      chunk.cells.push(cell);
    }

    // Build LOD meshes for each chunk
    for (const chunk of this.chunks.values()) {
      this.buildChunkLOD(chunk);
    }
  }

  /**
   * Build LOD meshes for a single chunk.
   */
  private buildChunkLOD(chunk: TerrainChunk): void {
    // Remove old LOD if exists
    if (chunk.lod) {
      this.scene.remove(chunk.lod);
      chunk.lod.traverse((obj) => {
        if (obj instanceof THREE.Mesh) {
          obj.geometry.dispose();
        }
      });
    }

    if (chunk.cells.length === 0) {
      chunk.lod = null;
      chunk.dirty = false;
      return;
    }

    // Create LOD object
    chunk.lod = new THREE.LOD();
    chunk.lod.name = `chunk_${chunk.chunkX}_${chunk.chunkZ}`;

    // Build HIGH detail geometry (full hexes with walls)
    const highBuilder = new HexMeshBuilder();
    for (const cell of chunk.cells) {
      highBuilder.buildCell(cell, this.grid);
    }
    const highGeometry = highBuilder.build();
    const highMesh = new THREE.Mesh(highGeometry, this.material);
    highMesh.receiveShadow = true;
    highMesh.castShadow = true;

    // Build MEDIUM detail geometry (hex tops only)
    const mediumBuilder = new LODHexBuilder();
    for (const cell of chunk.cells) {
      mediumBuilder.buildCellMedium(cell);
    }
    const mediumGeometry = mediumBuilder.build();
    const mediumMesh = new THREE.Mesh(mediumGeometry, this.material);
    mediumMesh.receiveShadow = true;
    mediumMesh.castShadow = false; // No shadows at medium distance

    // Build LOW detail geometry (simple quads)
    const lowBuilder = new LODHexBuilder();
    for (const cell of chunk.cells) {
      lowBuilder.buildCellLow(cell);
    }
    const lowGeometry = lowBuilder.build();
    const lowMesh = new THREE.Mesh(lowGeometry, this.material);
    lowMesh.receiveShadow = false;
    lowMesh.castShadow = false;

    // Add levels to LOD (distance thresholds)
    chunk.lod.addLevel(highMesh, 0);
    chunk.lod.addLevel(mediumMesh, LODDistances.highToMedium);
    chunk.lod.addLevel(lowMesh, LODDistances.mediumToLow);

    // Position LOD at chunk center for proper distance calculation
    chunk.lod.position.set(0, 0, 0);

    this.scene.add(chunk.lod);
    chunk.dirty = false;
  }

  /**
   * Update LOD levels based on camera position.
   * Call this each frame for LOD to work.
   */
  update(camera: THREE.Camera): void {
    // Reset stats
    this.lodStats = { high: 0, medium: 0, low: 0 };

    for (const chunk of this.chunks.values()) {
      if (chunk.lod) {
        // THREE.LOD.update() switches levels based on camera distance
        chunk.lod.update(camera);

        // Track which LOD level is active for stats
        const currentLevel = chunk.lod.getCurrentLevel();
        if (currentLevel === 0) this.lodStats.high++;
        else if (currentLevel === 1) this.lodStats.medium++;
        else this.lodStats.low++;
      }
    }
  }

  /**
   * Get LOD statistics for debugging.
   */
  getLODStats(): { high: number; medium: number; low: number } {
    return this.lodStats;
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
        this.buildChunkLOD(chunk);
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
      if (chunk.lod) {
        // Check if any mesh in LOD is visible
        const highMesh = chunk.lod.getObjectByProperty('type', 'Mesh') as THREE.Mesh;
        if (highMesh && highMesh.geometry.boundingSphere) {
          const sphere = highMesh.geometry.boundingSphere.clone();
          sphere.applyMatrix4(chunk.lod.matrixWorld);
          if (frustum.intersectsSphere(sphere)) {
            visible++;
          }
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
      if (chunk.lod) {
        this.scene.remove(chunk.lod);
        chunk.lod.traverse((obj) => {
          if (obj instanceof THREE.Mesh) {
            obj.geometry.dispose();
          }
        });
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
