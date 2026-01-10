import * as THREE from 'three';
import { HexGrid } from '../core/HexGrid';
import { HexCoordinates } from '../core/HexCoordinates';
import { HexMeshBuilder } from './HexMeshBuilder';
import { LODHexBuilder, LODDistances } from './LODHexBuilder';
import { HexCell } from '../types';

/**
 * A chunk of terrain containing multiple hex cells with LOD support.
 */
interface TerrainChunk {
  lod: THREE.LOD | null;
  cells: HexCell[];
  dirty: boolean;
  loaded: boolean;
  chunkX: number;
  chunkZ: number;
  centerX: number;
  centerZ: number;
}

/**
 * Renders hex terrain using a chunk-based system with LOD and streaming.
 * Each chunk has 3 detail levels:
 * - High: Full hex geometry with walls (near camera)
 * - Medium: Hex tops only, no walls (mid-range)
 * - Low: Simple quads (far from camera)
 *
 * Streaming: Only chunks within viewRadius are loaded with geometry.
 * Chunks outside are unloaded to save memory and GPU resources.
 */
export class ChunkedTerrainRenderer {
  private scene: THREE.Scene;
  private grid: HexGrid;
  private chunks: Map<string, TerrainChunk> = new Map();
  private material: THREE.MeshLambertMaterial;

  // Chunk size in hex cells (world units depend on hex metrics)
  static readonly CHUNK_SIZE = 16;

  // Streaming settings
  private viewRadius = 5; // Load chunks within this radius (in chunk units)
  private unloadRadius = 7; // Unload chunks beyond this radius
  private lastCameraChunkX = Infinity;
  private lastCameraChunkZ = Infinity;

  // LOD statistics
  private lodStats = { high: 0, medium: 0, low: 0 };

  // Streaming statistics
  private streamingStats = { loaded: 0, unloaded: 0, total: 0 };

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
   * Set the view radius for streaming (in chunk units).
   */
  setViewRadius(radius: number): void {
    this.viewRadius = radius;
    this.unloadRadius = radius + 2;
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
   * Get chunk coordinates from world position.
   */
  private getChunkCoordsFromWorld(worldX: number, worldZ: number): { chunkX: number; chunkZ: number } {
    const chunkX = Math.floor(worldX / ChunkedTerrainRenderer.CHUNK_SIZE);
    const chunkZ = Math.floor(worldZ / ChunkedTerrainRenderer.CHUNK_SIZE);
    return { chunkX, chunkZ };
  }

  /**
   * Build or rebuild all terrain chunks (creates chunk data but doesn't load geometry).
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
          loaded: false,
          chunkX,
          chunkZ,
          centerX: (chunkX + 0.5) * ChunkedTerrainRenderer.CHUNK_SIZE,
          centerZ: (chunkZ + 0.5) * ChunkedTerrainRenderer.CHUNK_SIZE,
        };
        this.chunks.set(key, chunk);
      }

      chunk.cells.push(cell);
    }

    this.streamingStats.total = this.chunks.size;

    // Initially load all chunks (will be streamed on first update)
    for (const chunk of this.chunks.values()) {
      this.loadChunk(chunk);
    }
  }

  /**
   * Load geometry for a chunk.
   */
  private loadChunk(chunk: TerrainChunk): void {
    if (chunk.loaded && !chunk.dirty) return;

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
      chunk.loaded = true;
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
    mediumMesh.castShadow = false;

    // Build LOW detail geometry (simple quads)
    const lowBuilder = new LODHexBuilder();
    for (const cell of chunk.cells) {
      lowBuilder.buildCellLow(cell);
    }
    const lowGeometry = lowBuilder.build();
    const lowMesh = new THREE.Mesh(lowGeometry, this.material);
    lowMesh.receiveShadow = false;
    lowMesh.castShadow = false;

    // Add levels to LOD
    chunk.lod.addLevel(highMesh, 0);
    chunk.lod.addLevel(mediumMesh, LODDistances.highToMedium);
    chunk.lod.addLevel(lowMesh, LODDistances.mediumToLow);

    chunk.lod.position.set(0, 0, 0);

    this.scene.add(chunk.lod);
    chunk.dirty = false;
    chunk.loaded = true;
  }

  /**
   * Unload geometry for a chunk (keeps cell data).
   */
  private unloadChunk(chunk: TerrainChunk): void {
    if (!chunk.loaded) return;

    if (chunk.lod) {
      this.scene.remove(chunk.lod);
      chunk.lod.traverse((obj) => {
        if (obj instanceof THREE.Mesh) {
          obj.geometry.dispose();
        }
      });
      chunk.lod = null;
    }

    chunk.loaded = false;
    chunk.dirty = true; // Will need rebuild when loaded again
  }

  /**
   * Update streaming and LOD based on camera position.
   */
  update(camera: THREE.Camera): void {
    const cameraPos = camera.position;
    const { chunkX: camChunkX, chunkZ: camChunkZ } = this.getChunkCoordsFromWorld(cameraPos.x, cameraPos.z);

    // Only update streaming if camera moved to a new chunk
    if (camChunkX !== this.lastCameraChunkX || camChunkZ !== this.lastCameraChunkZ) {
      this.lastCameraChunkX = camChunkX;
      this.lastCameraChunkZ = camChunkZ;
      this.updateStreaming(camChunkX, camChunkZ);
    }

    // Reset LOD stats
    this.lodStats = { high: 0, medium: 0, low: 0 };

    // Update LOD for loaded chunks
    for (const chunk of this.chunks.values()) {
      if (chunk.lod) {
        chunk.lod.update(camera);

        const currentLevel = chunk.lod.getCurrentLevel();
        if (currentLevel === 0) this.lodStats.high++;
        else if (currentLevel === 1) this.lodStats.medium++;
        else this.lodStats.low++;
      }
    }
  }

  /**
   * Update which chunks are loaded based on camera chunk position.
   */
  private updateStreaming(camChunkX: number, camChunkZ: number): void {
    let loaded = 0;
    let unloaded = 0;

    for (const chunk of this.chunks.values()) {
      const dx = chunk.chunkX - camChunkX;
      const dz = chunk.chunkZ - camChunkZ;
      const distance = Math.sqrt(dx * dx + dz * dz);

      if (distance <= this.viewRadius) {
        // Load chunks within view radius
        if (!chunk.loaded) {
          this.loadChunk(chunk);
        }
        loaded++;
      } else if (distance > this.unloadRadius) {
        // Unload chunks beyond unload radius
        if (chunk.loaded) {
          this.unloadChunk(chunk);
        }
        unloaded++;
      } else {
        // In buffer zone - keep current state
        if (chunk.loaded) loaded++;
        else unloaded++;
      }
    }

    this.streamingStats.loaded = loaded;
    this.streamingStats.unloaded = unloaded;
  }

  /**
   * Get LOD statistics.
   */
  getLODStats(): { high: number; medium: number; low: number } {
    return this.lodStats;
  }

  /**
   * Get streaming statistics.
   */
  getStreamingStats(): { loaded: number; unloaded: number; total: number } {
    return this.streamingStats;
  }

  /**
   * Mark a cell's chunk as dirty.
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
   * Rebuild any dirty loaded chunks.
   */
  rebuildDirtyChunks(): void {
    for (const chunk of this.chunks.values()) {
      if (chunk.dirty && chunk.loaded) {
        this.loadChunk(chunk);
      }
    }
  }

  /**
   * Get chunk count.
   */
  get chunkCount(): number {
    return this.chunks.size;
  }

  /**
   * Get visible chunk count (frustum culling).
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
    this.lastCameraChunkX = Infinity;
    this.lastCameraChunkZ = Infinity;
  }

  /**
   * Full cleanup including material.
   */
  disposeAll(): void {
    this.dispose();
    this.material.dispose();
  }
}
