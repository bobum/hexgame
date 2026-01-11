/**
 * Web Worker for map generation.
 * Handles noise calculations and biome assignment off the main thread.
 */

import { WorkerTask, WorkerResult } from './WorkerPool';

// Simplex noise implementation (self-contained for worker)
class SimplexNoise {
  private perm: number[] = [];
  private gradP: { x: number; y: number }[] = [];

  private grad3 = [
    { x: 1, y: 1 }, { x: -1, y: 1 }, { x: 1, y: -1 }, { x: -1, y: -1 },
    { x: 1, y: 0 }, { x: -1, y: 0 }, { x: 0, y: 1 }, { x: 0, y: -1 },
  ];

  constructor(seed: number) {
    this.seed(seed);
  }

  private seed(seed: number): void {
    const p: number[] = [];
    for (let i = 0; i < 256; i++) {
      p[i] = i;
    }

    // Shuffle using seed
    let n = seed;
    for (let i = 255; i > 0; i--) {
      n = (n * 16807) % 2147483647;
      const j = n % (i + 1);
      [p[i], p[j]] = [p[j], p[i]];
    }

    this.perm = [];
    this.gradP = [];
    for (let i = 0; i < 512; i++) {
      this.perm[i] = p[i & 255];
      this.gradP[i] = this.grad3[this.perm[i] % 8];
    }
  }

  noise2D(x: number, y: number): number {
    const F2 = 0.5 * (Math.sqrt(3) - 1);
    const G2 = (3 - Math.sqrt(3)) / 6;

    const s = (x + y) * F2;
    const i = Math.floor(x + s);
    const j = Math.floor(y + s);

    const t = (i + j) * G2;
    const X0 = i - t;
    const Y0 = j - t;
    const x0 = x - X0;
    const y0 = y - Y0;

    let i1: number, j1: number;
    if (x0 > y0) {
      i1 = 1; j1 = 0;
    } else {
      i1 = 0; j1 = 1;
    }

    const x1 = x0 - i1 + G2;
    const y1 = y0 - j1 + G2;
    const x2 = x0 - 1 + 2 * G2;
    const y2 = y0 - 1 + 2 * G2;

    const ii = i & 255;
    const jj = j & 255;

    let n0 = 0, n1 = 0, n2 = 0;

    let t0 = 0.5 - x0 * x0 - y0 * y0;
    if (t0 >= 0) {
      const gi0 = this.gradP[ii + this.perm[jj]];
      t0 *= t0;
      n0 = t0 * t0 * (gi0.x * x0 + gi0.y * y0);
    }

    let t1 = 0.5 - x1 * x1 - y1 * y1;
    if (t1 >= 0) {
      const gi1 = this.gradP[ii + i1 + this.perm[jj + j1]];
      t1 *= t1;
      n1 = t1 * t1 * (gi1.x * x1 + gi1.y * y1);
    }

    let t2 = 0.5 - x2 * x2 - y2 * y2;
    if (t2 >= 0) {
      const gi2 = this.gradP[ii + 1 + this.perm[jj + 1]];
      t2 *= t2;
      n2 = t2 * t2 * (gi2.x * x2 + gi2.y * y2);
    }

    return 70 * (n0 + n1 + n2);
  }
}

// Types for map generation
interface MapGenInput {
  width: number;
  height: number;
  seed: number;
  noiseScale: number;
  octaves: number;
  persistence: number;
  lacunarity: number;
  landPercentage: number;
  mountainousness: number;
}

interface CellData {
  q: number;
  r: number;
  s: number;
  elevation: number;
  moisture: number;
  temperature: number;
  terrainType: string; // TerrainType string enum value
}

interface MapGenOutput {
  cells: CellData[];
  generationTime: number;
}

// Terrain types (must match main thread string enum)
const TerrainType = {
  Ocean: 'ocean',
  Coast: 'coast',
  Plains: 'plains',
  Forest: 'forest',
  Hills: 'hills',
  Mountains: 'mountains',
  Snow: 'snow',
  Desert: 'desert',
  Tundra: 'tundra',
  Jungle: 'jungle',
  Savanna: 'savanna',
  Taiga: 'taiga',
};

// Hex coordinate conversion
function fromOffset(col: number, row: number): { q: number; r: number; s: number } {
  const q = col - Math.floor(row / 2);
  const r = row;
  return { q, r, s: -q - r };
}

// Multi-octave noise
function fractalNoise(
  noise: SimplexNoise,
  x: number,
  y: number,
  octaves: number,
  persistence: number,
  lacunarity: number,
  scale: number
): number {
  let value = 0;
  let amplitude = 1;
  let frequency = scale;
  let maxValue = 0;

  for (let i = 0; i < octaves; i++) {
    value += noise.noise2D(x * frequency, y * frequency) * amplitude;
    maxValue += amplitude;
    amplitude *= persistence;
    frequency *= lacunarity;
  }

  return value / maxValue;
}

// Biome determination
function getBiome(elevation: number, moisture: number, temperature: number): string {
  if (elevation < 0) return TerrainType.Ocean;
  if (elevation === 0) return TerrainType.Coast;

  if (elevation >= 6) return TerrainType.Snow;
  if (elevation >= 4) return TerrainType.Mountains;

  if (temperature < 0.3) {
    return moisture > 0.5 ? TerrainType.Tundra : TerrainType.Snow;
  }

  if (temperature > 0.7 && moisture < 0.3) {
    return TerrainType.Desert;
  }

  if (elevation >= 2) return TerrainType.Hills;

  if (moisture > 0.6) return TerrainType.Forest;

  return TerrainType.Plains;
}

// Main generation function
function generateMap(input: MapGenInput): MapGenOutput {
  const startTime = performance.now();
  const noise = new SimplexNoise(input.seed);
  const moistureNoise = new SimplexNoise(input.seed + 1000);
  const temperatureNoise = new SimplexNoise(input.seed + 2000);

  const cells: CellData[] = [];

  // First pass: generate raw elevation values
  const elevations: number[] = [];
  for (let row = 0; row < input.height; row++) {
    for (let col = 0; col < input.width; col++) {
      const value = fractalNoise(
        noise, col, row,
        input.octaves,
        input.persistence,
        input.lacunarity,
        input.noiseScale
      );
      elevations.push(value);
    }
  }

  // Find threshold for land percentage
  const sorted = [...elevations].sort((a, b) => a - b);
  const thresholdIndex = Math.floor(sorted.length * (1 - input.landPercentage));
  const landThreshold = sorted[thresholdIndex];

  // Second pass: create cells with normalized elevation and biomes
  for (let row = 0; row < input.height; row++) {
    for (let col = 0; col < input.width; col++) {
      const { q, r, s } = fromOffset(col, row);
      const rawElevation = elevations[row * input.width + col];

      // Normalize to elevation levels
      let elevation: number;
      if (rawElevation < landThreshold) {
        // Underwater: -2 to 0
        const t = (rawElevation - sorted[0]) / (landThreshold - sorted[0]);
        elevation = Math.floor(t * 3) - 2;
      } else {
        // Land: 0 to 8
        const t = (rawElevation - landThreshold) / (sorted[sorted.length - 1] - landThreshold);
        elevation = Math.floor(t * 8 * input.mountainousness);
      }
      elevation = Math.max(-2, Math.min(8, elevation));

      // Generate moisture and temperature
      const moisture = (moistureNoise.noise2D(col * 0.08, row * 0.08) + 1) / 2;
      const latitudeTemp = 1 - Math.abs(row / input.height - 0.5) * 2;
      const tempNoise = (temperatureNoise.noise2D(col * 0.05, row * 0.05) + 1) / 2;
      const temperature = latitudeTemp * 0.7 + tempNoise * 0.3;

      // Determine biome
      const terrainType = getBiome(elevation, moisture, temperature);

      cells.push({
        q, r, s,
        elevation,
        moisture,
        temperature,
        terrainType,
      });
    }
  }

  return {
    cells,
    generationTime: performance.now() - startTime,
  };
}

// Worker message handler
self.onmessage = (e: MessageEvent<WorkerTask<MapGenInput>>) => {
  const { id, type, data } = e.data;

  try {
    let result: unknown;

    switch (type) {
      case 'generateMap':
        result = generateMap(data);
        break;
      default:
        throw new Error(`Unknown task type: ${type}`);
    }

    const response: WorkerResult = { id, type, data: result };
    self.postMessage(response);
  } catch (error) {
    const response: WorkerResult = {
      id,
      type,
      data: null,
      error: error instanceof Error ? error.message : 'Unknown error',
    };
    self.postMessage(response);
  }
};
