import * as THREE from 'three';
import { HexGrid } from '../core/HexGrid';
import { HexCoordinates } from '../core/HexCoordinates';
import { HexMetrics } from '../core/HexMetrics';
import { HexMeshBuilder } from './HexMeshBuilder';
import { LODHexBuilder, LODDistances } from './LODHexBuilder';
import { createTerrainMaterial, updateTerrainMaterial } from './TerrainShaderMaterial';
import { HexCell } from '../types';

// Hard distance cutoff - beyond fog (50), chunks are invisible anyway
const MAX_RENDER_DISTANCE = 70;

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
  maxElevation: number;  // Highest cell elevation in chunk (for occlusion)
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
          maxElevation: 0,
        };
        this.chunks.set(key, chunk);
      }
      chunk.cells.push(cell);
      // Track max elevation for occlusion culling
      if (cell.elevation > chunk.maxElevation) {
        chunk.maxElevation = cell.elevation;
      }
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

    // MEDIUM detail - hex tops only (with per-hex skirts built in)
    const medBuilder = new LODHexBuilder();
    for (const cell of chunk.cells) {
      medBuilder.buildCellMedium(cell);
    }
    const medGeo = medBuilder.build();
    const medMesh = new THREE.Mesh(medGeo, this.material);
    medMesh.receiveShadow = true;
    medMesh.position.set(-chunk.centerX, 0, -chunk.centerZ);

    // LOW detail - simple quads (with per-hex skirts built in)
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
    // First update visibility (frustum + distance culling)
    this.updateVisibility(camera);

    // Then update LOD levels for visible chunks only
    for (const chunk of this.chunks.values()) {
      if (chunk.lod && chunk.lod.visible) {
        chunk.lod.update(camera);
      }
    }
  }

  /**
   * Get bounding sphere for a chunk in world space.
   */
  private getChunkBoundingSphere(chunk: TerrainChunk): THREE.Sphere {
    if (chunk.lod && chunk.lod.children.length > 0) {
      const mesh = chunk.lod.children[0] as THREE.Mesh;
      if (mesh && mesh.geometry.boundingSphere) {
        const sphere = mesh.geometry.boundingSphere.clone();
        // Apply mesh offset and LOD position to get world space
        sphere.center.add(mesh.position);
        sphere.center.add(chunk.lod.position);
        return sphere;
      }
    }
    // Fallback: create sphere from chunk center
    const radius = ChunkedTerrainRenderer.CHUNK_SIZE * 0.75;
    return new THREE.Sphere(
      new THREE.Vector3(chunk.centerX, 0, chunk.centerZ),
      radius
    );
  }

  /**
   * Update chunk visibility based on frustum, distance, and occlusion culling.
   */
  private updateVisibility(camera: THREE.Camera): void {
    const frustum = new THREE.Frustum();
    const projScreenMatrix = new THREE.Matrix4();
    projScreenMatrix.multiplyMatrices(
      camera.projectionMatrix,
      camera.matrixWorldInverse
    );
    frustum.setFromProjectionMatrix(projScreenMatrix);

    const cameraPos = camera.position;
    const maxDistSq = MAX_RENDER_DISTANCE * MAX_RENDER_DISTANCE;

    // First pass: distance and frustum culling
    const visibleChunks: { chunk: TerrainChunk; distSq: number }[] = [];

    for (const chunk of this.chunks.values()) {
      if (!chunk.lod) continue;

      // Distance check first (cheaper than frustum test)
      const dx = chunk.centerX - cameraPos.x;
      const dz = chunk.centerZ - cameraPos.z;
      const distSq = dx * dx + dz * dz;

      if (distSq > maxDistSq) {
        chunk.lod.visible = false;
        continue;
      }

      // Frustum check
      const sphere = this.getChunkBoundingSphere(chunk);
      if (!frustum.intersectsSphere(sphere)) {
        chunk.lod.visible = false;
        continue;
      }

      // Passed frustum + distance, candidate for occlusion check
      visibleChunks.push({ chunk, distSq });
    }

    // Second pass: horizon-based occlusion culling
    // Only effective when camera is relatively low
    const cameraHeight = cameraPos.y;
    const maxTerrainHeight = HexMetrics.maxElevation * HexMetrics.elevationStep;

    // Skip occlusion culling if camera is high above terrain (looking down)
    if (cameraHeight > maxTerrainHeight * 3) {
      for (const { chunk } of visibleChunks) {
        chunk.lod!.visible = true;
      }
      return;
    }

    // Sort by distance (near to far)
    visibleChunks.sort((a, b) => a.distSq - b.distSq);

    // Track horizon angle per direction sector (8 sectors for 360°)
    const NUM_SECTORS = 16;
    const horizonAngles = new Float32Array(NUM_SECTORS).fill(-Math.PI / 2);

    for (const { chunk, distSq } of visibleChunks) {
      const dx = chunk.centerX - cameraPos.x;
      const dz = chunk.centerZ - cameraPos.z;
      const dist = Math.sqrt(distSq);

      // Calculate which direction sector this chunk is in
      const angle = Math.atan2(dz, dx);
      const sector = Math.floor(((angle + Math.PI) / (2 * Math.PI)) * NUM_SECTORS) % NUM_SECTORS;

      // Calculate angle from camera to chunk's highest point
      const chunkTopY = chunk.maxElevation * HexMetrics.elevationStep;
      const angleToTop = Math.atan2(chunkTopY - cameraHeight, dist);

      // Check if chunk top is below the horizon in this sector
      if (angleToTop < horizonAngles[sector] - 0.05) {
        // Chunk is occluded by terrain in front
        chunk.lod!.visible = false;
        continue;
      }

      // Chunk is visible
      chunk.lod!.visible = true;

      // Update horizon angle for this sector if chunk peak is higher
      if (angleToTop > horizonAngles[sector]) {
        horizonAngles[sector] = angleToTop;
        // Also update adjacent sectors slightly (terrain has width)
        const prevSector = (sector - 1 + NUM_SECTORS) % NUM_SECTORS;
        const nextSector = (sector + 1) % NUM_SECTORS;
        const adjacentAngle = angleToTop - 0.1; // Reduced effect for adjacent
        if (adjacentAngle > horizonAngles[prevSector]) {
          horizonAngles[prevSector] = adjacentAngle;
        }
        if (adjacentAngle > horizonAngles[nextSector]) {
          horizonAngles[nextSector] = adjacentAngle;
        }
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

  /**
   * Get the terrain material for GUI controls.
   */
  getMaterial(): THREE.ShaderMaterial {
    return this.material;
  }

  /**
   * Get count of currently visible chunks (after culling).
   */
  getVisibleChunkCount(): number {
    let visible = 0;
    for (const chunk of this.chunks.values()) {
      if (chunk.lod && chunk.lod.visible) {
        visible++;
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
