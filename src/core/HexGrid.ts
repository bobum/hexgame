import * as THREE from 'three';
import { HexCoordinates } from './HexCoordinates';
import { HexDirection, AllDirections } from './HexDirection';
import { HexCell, TerrainType, MapConfig, defaultMapConfig } from '../types';
import { SpatialHash } from '../utils/SpatialHash';

/**
 * Manages the hex grid - stores cells and provides access methods.
 */
export class HexGrid {
  private cells: Map<string, HexCell> = new Map();
  private spatialHash: SpatialHash<HexCell>;
  readonly width: number;
  readonly height: number;
  readonly config: MapConfig;

  constructor(config: Partial<MapConfig> = {}) {
    this.config = { ...defaultMapConfig, ...config };
    this.width = this.config.width;
    this.height = this.config.height;
    // Cell size of 2 works well for hex grids (slightly larger than hex radius)
    this.spatialHash = new SpatialHash<HexCell>(2);
  }

  // Initialize grid with empty cells
  initialize(): void {
    this.cells.clear();
    this.spatialHash.clear();

    for (let row = 0; row < this.height; row++) {
      for (let col = 0; col < this.width; col++) {
        const coords = HexCoordinates.fromOffset(col, row);
        const worldPos = coords.toWorldPosition(0);
        const cell: HexCell = {
          q: coords.q,
          r: coords.r,
          s: coords.s,
          elevation: 0,
          terrainType: TerrainType.Ocean,
          moisture: 0.5,
          temperature: 0.5,
          features: [],
        };
        this.cells.set(coords.toKey(), cell);
        this.spatialHash.insert(cell, worldPos.x, worldPos.z);
      }
    }
  }

  // Get cell at coordinates
  getCell(coords: HexCoordinates): HexCell | undefined {
    return this.cells.get(coords.toKey());
  }

  // Get cell by q, r directly
  getCellAt(q: number, r: number): HexCell | undefined {
    return this.cells.get(`${q},${r}`);
  }

  // Set/update a cell
  setCell(cell: HexCell): void {
    const key = `${cell.q},${cell.r}`;
    const coords = new HexCoordinates(cell.q, cell.r);
    const worldPos = coords.toWorldPosition(0);

    // Remove old cell from spatial hash if it exists
    const oldCell = this.cells.get(key);
    if (oldCell) {
      this.spatialHash.remove(oldCell);
    }

    this.cells.set(key, cell);
    this.spatialHash.insert(cell, worldPos.x, worldPos.z);
  }

  // Check if coordinates are within grid bounds
  isValidCoord(coords: HexCoordinates): boolean {
    return this.cells.has(coords.toKey());
  }

  // Get all cells as array
  getAllCells(): HexCell[] {
    return Array.from(this.cells.values());
  }

  // Get neighbor cell
  getNeighbor(cell: HexCell, direction: HexDirection): HexCell | undefined {
    const coords = new HexCoordinates(cell.q, cell.r);
    const neighborCoords = coords.getNeighbor(direction);
    return this.getCell(neighborCoords);
  }

  // Get all valid neighbors
  getNeighbors(cell: HexCell): HexCell[] {
    const neighbors: HexCell[] = [];
    for (const dir of AllDirections) {
      const neighbor = this.getNeighbor(cell, dir);
      if (neighbor) {
        neighbors.push(neighbor);
      }
    }
    return neighbors;
  }

  // Get cell count
  get cellCount(): number {
    return this.cells.size;
  }

  // Get center of the map in world coordinates
  getMapCenter(): { x: number; z: number } {
    const centerCol = Math.floor(this.width / 2);
    const centerRow = Math.floor(this.height / 2);
    const coords = HexCoordinates.fromOffset(centerCol, centerRow);
    const worldPos = coords.toWorldPosition(0);
    return { x: worldPos.x, z: worldPos.z };
  }

  // Get map bounds in world coordinates
  getMapBounds(): { minX: number; maxX: number; minZ: number; maxZ: number } {
    let minX = Infinity, maxX = -Infinity;
    let minZ = Infinity, maxZ = -Infinity;

    for (const cell of this.cells.values()) {
      const coords = new HexCoordinates(cell.q, cell.r);
      const pos = coords.toWorldPosition(0);
      minX = Math.min(minX, pos.x);
      maxX = Math.max(maxX, pos.x);
      minZ = Math.min(minZ, pos.z);
      maxZ = Math.max(maxZ, pos.z);
    }

    return { minX, maxX, minZ, maxZ };
  }

  // Iterator for cells
  [Symbol.iterator](): Iterator<HexCell> {
    return this.cells.values();
  }

  /**
   * Get cell at world position using spatial hash (O(1)).
   * Returns the closest cell to the given world coordinates.
   */
  getCellAtWorld(worldX: number, worldZ: number): HexCell | null {
    // Use HexCoordinates conversion for exact cell lookup
    const coords = HexCoordinates.fromWorldPosition(new THREE.Vector3(worldX, 0, worldZ));
    return this.getCell(coords) ?? null;
  }

  /**
   * Get all cells within a radius of a world position.
   */
  getCellsInRadius(worldX: number, worldZ: number, radius: number): HexCell[] {
    return this.spatialHash.queryRadius(worldX, worldZ, radius);
  }

  /**
   * Get all cells within a rectangular area.
   */
  getCellsInRect(minX: number, minZ: number, maxX: number, maxZ: number): HexCell[] {
    return this.spatialHash.queryRect(minX, minZ, maxX, maxZ);
  }

  /**
   * Get spatial hash statistics for debugging.
   */
  getSpatialHashStats() {
    return this.spatialHash.stats;
  }
}
