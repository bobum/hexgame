import { HexCoordinates } from './HexCoordinates';
import { HexDirection, AllDirections } from './HexDirection';
import { HexCell, TerrainType, MapConfig, defaultMapConfig } from '../types';

/**
 * Manages the hex grid - stores cells and provides access methods.
 */
export class HexGrid {
  private cells: Map<string, HexCell> = new Map();
  readonly width: number;
  readonly height: number;
  readonly config: MapConfig;

  constructor(config: Partial<MapConfig> = {}) {
    this.config = { ...defaultMapConfig, ...config };
    this.width = this.config.width;
    this.height = this.config.height;
  }

  // Initialize grid with empty cells
  initialize(): void {
    this.cells.clear();

    for (let row = 0; row < this.height; row++) {
      for (let col = 0; col < this.width; col++) {
        const coords = HexCoordinates.fromOffset(col, row);
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
    this.cells.set(key, cell);
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
}
