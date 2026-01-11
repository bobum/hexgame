import * as THREE from 'three';
import { HexGrid } from '../core/HexGrid';
import { HexCoordinates } from '../core/HexCoordinates';
import { HexMetrics } from '../core/HexMetrics';
import { NoiseGenerator } from './NoiseGenerator';
import { getTerrainFromElevationAndBiome, calculateTemperature } from './BiomeMapper';
import { HexCell, TerrainType, FeatureType } from '../types';
import { getMapGenPool, workersSupported } from '../workers';

/**
 * Procedural map generator using noise-based terrain generation.
 */
export class MapGenerator {
  private grid: HexGrid;
  private elevationNoise: NoiseGenerator;
  private moistureNoise: NoiseGenerator;
  private temperatureNoise: NoiseGenerator;
  private featureNoise: NoiseGenerator;

  constructor(grid: HexGrid) {
    this.grid = grid;
    const seed = grid.config.seed;
    this.elevationNoise = new NoiseGenerator(seed);
    this.moistureNoise = new NoiseGenerator(seed + 1000);
    this.temperatureNoise = new NoiseGenerator(seed + 2000);
    this.featureNoise = new NoiseGenerator(seed + 3000);
  }

  /**
   * Generate complete map with terrain, biomes, and features.
   * Synchronous version - runs on main thread.
   */
  generate(): void {
    this.grid.initialize();
    this.generateElevation();
    this.generateClimate();
    this.assignTerrain();
    this.generateFeatures();
  }

  /**
   * Generate map asynchronously using Web Workers.
   * Falls back to synchronous generation if workers unavailable.
   * @returns Promise that resolves when generation is complete
   */
  async generateAsync(): Promise<{ workerTime: number; featureTime: number }> {
    if (!workersSupported()) {
      console.log('Web Workers not supported, falling back to sync generation');
      this.generate();
      return { workerTime: 0, featureTime: 0 };
    }

    this.grid.initialize();

    const pool = getMapGenPool();
    const config = this.grid.config;

    // Run terrain generation in worker
    interface WorkerCellData {
      q: number;
      r: number;
      s: number;
      elevation: number;
      moisture: number;
      temperature: number;
      terrainType: string;
    }

    const result = await pool.runTask<
      {
        width: number;
        height: number;
        seed: number;
        noiseScale: number;
        octaves: number;
        persistence: number;
        lacunarity: number;
        landPercentage: number;
        mountainousness: number;
      },
      { cells: WorkerCellData[]; generationTime: number }
    >('generateMap', {
      width: config.width,
      height: config.height,
      seed: config.seed,
      noiseScale: config.noiseScale,
      octaves: config.octaves,
      persistence: config.persistence,
      lacunarity: config.lacunarity,
      landPercentage: config.landPercentage,
      mountainousness: config.mountainousness,
    });

    // Apply worker results to grid
    for (const cellData of result.cells) {
      const cell = this.grid.getCellAt(cellData.q, cellData.r);
      if (cell) {
        cell.elevation = cellData.elevation;
        cell.moisture = cellData.moisture;
        cell.temperature = cellData.temperature;
        cell.terrainType = cellData.terrainType as TerrainType;
        this.grid.setCell(cell);
      }
    }

    // Generate features on main thread (requires THREE.Vector3)
    const featureStart = performance.now();
    this.generateFeatures();
    const featureTime = performance.now() - featureStart;

    return { workerTime: result.generationTime, featureTime };
  }

  /**
   * Generate elevation using multi-octave noise.
   */
  private generateElevation(): void {
    const { noiseScale, octaves, persistence, lacunarity, landPercentage, mountainousness } =
      this.grid.config;

    const cells = this.grid.getAllCells();

    // First pass: generate raw elevation values
    const elevations: number[] = [];
    for (const cell of cells) {
      const coords = new HexCoordinates(cell.q, cell.r);
      const worldPos = coords.toWorldPosition(0);

      // Base continental noise
      let elevation = this.elevationNoise.getFBM(
        worldPos.x,
        worldPos.z,
        octaves,
        persistence,
        lacunarity,
        noiseScale
      );

      // Add ridged noise for mountain ranges
      const ridged = this.elevationNoise.getRidged(
        worldPos.x + 500,
        worldPos.z + 500,
        3,
        0.5,
        2.0,
        noiseScale * 1.5
      );
      elevation += ridged * mountainousness * 0.5;

      // Edge falloff to create islands/continents
      const edgeFalloff = this.calculateEdgeFalloff(cell);
      elevation -= edgeFalloff * 0.5;

      elevations.push(elevation);
    }

    // Find threshold for desired land percentage
    const sortedElevations = [...elevations].sort((a, b) => a - b);
    const waterThresholdIndex = Math.floor((1 - landPercentage) * sortedElevations.length);
    const waterThreshold = sortedElevations[waterThresholdIndex];

    // Second pass: normalize and quantize elevation
    const maxRawAboveWater = Math.max(...elevations) - waterThreshold;
    const minRawBelowWater = Math.min(...elevations) - waterThreshold;

    for (let i = 0; i < cells.length; i++) {
      const cell = cells[i];
      let elevation = elevations[i];

      // Shift so water threshold becomes 0
      elevation -= waterThreshold;

      // Scale to elevation range
      const { maxElevation, minElevation } = HexMetrics;
      if (elevation >= 0) {
        // Land: scale to 0 to maxElevation
        elevation = maxRawAboveWater > 0
          ? (elevation / maxRawAboveWater) * maxElevation
          : 0;
      } else {
        // Water: scale to minElevation to 0
        elevation = minRawBelowWater < 0
          ? (elevation / Math.abs(minRawBelowWater)) * Math.abs(minElevation)
          : 0;
      }

      // Quantize to integer levels
      cell.elevation = Math.round(elevation);
      cell.elevation = Math.max(minElevation, Math.min(maxElevation, cell.elevation));

      this.grid.setCell(cell);
    }
  }

  /**
   * Calculate edge falloff for island generation.
   */
  private calculateEdgeFalloff(cell: HexCell): number {
    const coords = new HexCoordinates(cell.q, cell.r);
    const worldPos = coords.toWorldPosition(0);
    const bounds = this.grid.getMapBounds();

    const centerX = (bounds.minX + bounds.maxX) / 2;
    const centerZ = (bounds.minZ + bounds.maxZ) / 2;
    const maxDistX = (bounds.maxX - bounds.minX) / 2;
    const maxDistZ = (bounds.maxZ - bounds.minZ) / 2;

    const normalizedX = maxDistX > 0 ? Math.abs(worldPos.x - centerX) / maxDistX : 0;
    const normalizedZ = maxDistZ > 0 ? Math.abs(worldPos.z - centerZ) / maxDistZ : 0;

    // Use max for rectangular falloff
    const distance = Math.max(normalizedX, normalizedZ);

    // Smooth falloff starting at 70% from center
    if (distance < 0.7) return 0;
    return Math.pow((distance - 0.7) / 0.3, 2);
  }

  /**
   * Generate temperature and moisture for each cell.
   */
  private generateClimate(): void {
    const cells = this.grid.getAllCells();
    const { noiseScale } = this.grid.config;

    // Calculate distance from water for moisture
    const waterDistances = this.calculateWaterDistances();

    for (const cell of cells) {
      const coords = new HexCoordinates(cell.q, cell.r);
      const worldPos = coords.toWorldPosition(0);

      // Temperature: based on latitude + noise + elevation
      const tempNoise =
        this.temperatureNoise.getNormalized(worldPos.x * noiseScale, worldPos.z * noiseScale) *
          0.2 -
        0.1;
      cell.temperature = calculateTemperature(
        cell.r + Math.floor(this.grid.height / 2),
        this.grid.height,
        cell.elevation,
        0.6,
        tempNoise
      );

      // Moisture: based on distance from water + noise
      const key = coords.toKey();
      const waterDist = waterDistances.get(key) || 10;
      const moistNoise = this.moistureNoise.getNormalized(
        worldPos.x * noiseScale * 2,
        worldPos.z * noiseScale * 2
      );
      cell.moisture = 1 - Math.min(waterDist / 8, 1);
      cell.moisture = cell.moisture * 0.6 + moistNoise * 0.4;
      cell.moisture = Math.max(0, Math.min(1, cell.moisture));

      this.grid.setCell(cell);
    }
  }

  /**
   * BFS to calculate distance from water for each land cell.
   */
  private calculateWaterDistances(): Map<string, number> {
    const distances = new Map<string, number>();
    const queue: Array<{ cell: HexCell; distance: number }> = [];

    // Initialize with water cells at distance 0
    for (const cell of this.grid.getAllCells()) {
      const key = `${cell.q},${cell.r}`;
      if (cell.elevation < HexMetrics.waterLevel) {
        distances.set(key, 0);
        queue.push({ cell, distance: 0 });
      }
    }

    // BFS outward
    while (queue.length > 0) {
      const { cell, distance } = queue.shift()!;
      const neighbors = this.grid.getNeighbors(cell);

      for (const neighbor of neighbors) {
        const key = `${neighbor.q},${neighbor.r}`;
        if (!distances.has(key)) {
          distances.set(key, distance + 1);
          queue.push({ cell: neighbor, distance: distance + 1 });
        }
      }
    }

    return distances;
  }

  /**
   * Assign terrain types based on elevation and climate.
   */
  private assignTerrain(): void {
    for (const cell of this.grid.getAllCells()) {
      cell.terrainType = getTerrainFromElevationAndBiome(
        cell.elevation,
        cell.temperature,
        cell.moisture,
        HexMetrics.waterLevel
      );
      this.grid.setCell(cell);
    }
  }

  /**
   * Generate features (trees, rocks) based on terrain.
   */
  private generateFeatures(): void {
    for (const cell of this.grid.getAllCells()) {
      cell.features = [];

      // Skip water cells
      if (cell.elevation < HexMetrics.waterLevel) continue;

      const coords = new HexCoordinates(cell.q, cell.r);
      const centerPos = coords.toWorldPosition(cell.elevation);

      // Determine feature density based on terrain
      let treeDensity = 0;
      let rockDensity = 0;

      switch (cell.terrainType) {
        case TerrainType.Forest:
        case TerrainType.Jungle:
          treeDensity = 0.8;
          break;
        case TerrainType.Taiga:
          treeDensity = 0.6;
          break;
        case TerrainType.Plains:
        case TerrainType.Savanna:
          treeDensity = 0.15;
          break;
        case TerrainType.Hills:
          treeDensity = 0.2;
          rockDensity = 0.3;
          break;
        case TerrainType.Mountains:
          rockDensity = 0.5;
          break;
        case TerrainType.Tundra:
          rockDensity = 0.2;
          break;
      }

      // Place trees
      if (treeDensity > 0) {
        const treeCount = Math.floor(treeDensity * 3);
        for (let i = 0; i < treeCount; i++) {
          const noise = this.featureNoise.getNormalized(
            centerPos.x + i * 100,
            centerPos.z + i * 100
          );
          if (noise < treeDensity) {
            const angle = noise * Math.PI * 2;
            const dist = (0.2 + noise * 0.4) * HexMetrics.innerRadius;
            cell.features.push({
              type: FeatureType.Tree,
              position: new THREE.Vector3(
                centerPos.x + Math.cos(angle) * dist,
                centerPos.y,
                centerPos.z + Math.sin(angle) * dist
              ),
              scale: 0.8 + noise * 0.4,
              rotation: noise * Math.PI * 2,
            });
          }
        }
      }

      // Place rocks
      if (rockDensity > 0) {
        const rockCount = Math.floor(rockDensity * 2);
        for (let i = 0; i < rockCount; i++) {
          const noise = this.featureNoise.getNormalized(
            centerPos.x + i * 200 + 1000,
            centerPos.z + i * 200 + 1000
          );
          if (noise < rockDensity) {
            const angle = noise * Math.PI * 2;
            const dist = (0.2 + noise * 0.3) * HexMetrics.innerRadius;
            cell.features.push({
              type: FeatureType.Rock,
              position: new THREE.Vector3(
                centerPos.x + Math.cos(angle) * dist,
                centerPos.y,
                centerPos.z + Math.sin(angle) * dist
              ),
              scale: 0.5 + noise * 0.5,
              rotation: noise * Math.PI * 2,
            });
          }
        }
      }

      this.grid.setCell(cell);
    }
  }
}
