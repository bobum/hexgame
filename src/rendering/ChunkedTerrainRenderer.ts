import * as THREE from 'three';
import { HexGrid } from '../core/HexGrid';
import { HexCoordinates } from '../core/HexCoordinates';
import { HexMeshBuilder } from './HexMeshBuilder';
import { LODHexBuilder, LODDistances } from './LODHexBuilder';
import { createTerrainMaterial, updateTerrainMaterial } from './TerrainShaderMaterial';
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
  centerX: number;  // World center for LOD positioning
  centerZ: number;
}

/**
 * Renders hex terrain using a chunk-based system with LOD.
 * Each chunk has 3 detail levels:
 * - High: Full hex geometry with walls
 * - Medium: Hex tops only, no walls
 * - Low: Simple quads
 */
export class ChunkedTerrainRenderer {
  private scene: THREE.Scene;
  private grid: HexGrid;
  private chunks: Map<string, TerrainChunk> = new Map();
  private material: THREE.ShaderMaterial;

  static readonly CHUNK_SIZE = 16;

  constructor(scene: THREE.Scene, grid: HexGrid) {
    this.scene = scene;
    this.grid = grid;

    this.material = createTerrainMaterial();
  }

  /**
   * Update shader time uniform for animated effects.
   */
  updateShader(deltaTime: number): void {
    updateTerrainMaterial(this.material, deltaTime);
  }

  private getChunkKey(chunkX: number, chunkZ: number): string {
    return `${chunkX},${chunkZ}`;
  }

  private getCellChunkCoords(cell: HexCell): { chunkX: number; chunkZ: number } {
    const coords = new HexCoordinates(cell.q, cell.r);
    const worldPos = coords.toWorldPosition(0);
    const chunkX = Math.floor(worldPos.x / ChunkedTerrainRenderer.CHUNK_SIZE);
    const chunkZ = Math.floor(worldPos.z / ChunkedTerrainRenderer.CHUNK_SIZE);
    return { chunkX, chunkZ };
  }

  build(): void {
    this.dispose();

    // Group cells into chunks
    for (const cell of this.grid.getAllCells()) {
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

    // Build LOD for each chunk
    for (const chunk of this.chunks.values()) {
      this.buildChunkLOD(chunk);
    }
  }

  private buildChunkLOD(chunk: TerrainChunk): void {
    // Remove old LOD
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

    // Create LOD object and position it at chunk center
    chunk.lod = new THREE.LOD();
    chunk.lod.position.set(chunk.centerX, 0, chunk.centerZ);

    // HIGH detail - full hex with walls
    const highBuilder = new HexMeshBuilder();
    for (const cell of chunk.cells) {
      highBuilder.buildCell(cell, this.grid);
    }
    const highGeo = highBuilder.build();
    const highMesh = new THREE.Mesh(highGeo, this.material);
    highMesh.castShadow = true;
    highMesh.receiveShadow = true;
    // Offset mesh to compensate for LOD position
    highMesh.position.set(-chunk.centerX, 0, -chunk.centerZ);

    // MEDIUM detail - hex tops only
    const medBuilder = new LODHexBuilder();
    for (const cell of chunk.cells) {
      medBuilder.buildCellMedium(cell);
    }
    const medGeo = medBuilder.build();
    const medMesh = new THREE.Mesh(medGeo, this.material);
    medMesh.receiveShadow = true;
    medMesh.position.set(-chunk.centerX, 0, -chunk.centerZ);

    // LOW detail - simple quads
    const lowBuilder = new LODHexBuilder();
    for (const cell of chunk.cells) {
      lowBuilder.buildCellLow(cell);
    }
    const lowGeo = lowBuilder.build();
    const lowMesh = new THREE.Mesh(lowGeo, this.material);
    lowMesh.position.set(-chunk.centerX, 0, -chunk.centerZ);

    // Add LOD levels (distance is from camera to LOD object position)
    chunk.lod.addLevel(highMesh, 0);
    chunk.lod.addLevel(medMesh, LODDistances.highToMedium);
    chunk.lod.addLevel(lowMesh, LODDistances.mediumToLow);

    chunk.lod.name = `chunk_${chunk.chunkX}_${chunk.chunkZ}`;
    this.scene.add(chunk.lod);
    chunk.dirty = false;
  }

  /**
   * Update LOD levels based on camera position. Call each frame.
   */
  update(camera: THREE.Camera): void {
    for (const chunk of this.chunks.values()) {
      if (chunk.lod) {
        chunk.lod.update(camera);
      }
    }
  }

  markCellDirty(cell: HexCell): void {
    const { chunkX, chunkZ } = this.getCellChunkCoords(cell);
    const key = this.getChunkKey(chunkX, chunkZ);
    const chunk = this.chunks.get(key);
    if (chunk) {
      chunk.dirty = true;
    }
  }

  rebuildDirtyChunks(): void {
    for (const chunk of this.chunks.values()) {
      if (chunk.dirty) {
        this.buildChunkLOD(chunk);
      }
    }
  }

  get chunkCount(): number {
    return this.chunks.size;
  }

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
      if (chunk.lod && chunk.lod.children.length > 0) {
        const mesh = chunk.lod.children[0] as THREE.Mesh;
        if (mesh && mesh.geometry.boundingSphere) {
          const sphere = mesh.geometry.boundingSphere.clone();
          // Apply both mesh offset and LOD position
          sphere.center.add(mesh.position);
          sphere.center.add(chunk.lod.position);
          if (frustum.intersectsSphere(sphere)) {
            visible++;
          }
        }
      }
    }
    return visible;
  }

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

  disposeAll(): void {
    this.dispose();
    this.material.dispose();
  }
}
