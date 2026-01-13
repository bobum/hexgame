import { createNoise2D, NoiseFunction2D } from 'simplex-noise';

/**
 * Simple seeded random number generator (Mulberry32)
 */
function mulberry32(seed: number): () => number {
  return function() {
    let t = seed += 0x6D2B79F5;
    t = Math.imul(t ^ t >>> 15, t | 1);
    t ^= t + Math.imul(t ^ t >>> 7, t | 61);
    return ((t ^ t >>> 14) >>> 0) / 4294967296;
  };
}

/**
 * Wrapper around simplex noise with multi-octave support.
 */
export class NoiseGenerator {
  private noise2D: NoiseFunction2D;
  private seed: number;

  constructor(seed: number = 12345) {
    this.seed = seed;
    // Create seeded PRNG
    const prng = mulberry32(seed);
    this.noise2D = createNoise2D(prng);
  }

  // Get raw noise value at position (-1 to 1)
  get(x: number, y: number): number {
    return this.noise2D(x, y);
  }

  // Get normalized noise value (0 to 1)
  getNormalized(x: number, y: number): number {
    return (this.noise2D(x, y) + 1) / 2;
  }

  // Multi-octave noise (fractal brownian motion)
  getFBM(
    x: number,
    y: number,
    octaves: number = 4,
    persistence: number = 0.5,
    lacunarity: number = 2.0,
    scale: number = 1.0
  ): number {
    let total = 0;
    let frequency = scale;
    let amplitude = 1;
    let maxValue = 0;

    for (let i = 0; i < octaves; i++) {
      total += this.noise2D(x * frequency, y * frequency) * amplitude;
      maxValue += amplitude;
      amplitude *= persistence;
      frequency *= lacunarity;
    }

    // Normalize to -1 to 1
    return total / maxValue;
  }

  // Get FBM normalized to 0-1
  getFBMNormalized(
    x: number,
    y: number,
    octaves: number = 4,
    persistence: number = 0.5,
    lacunarity: number = 2.0,
    scale: number = 1.0
  ): number {
    return (this.getFBM(x, y, octaves, persistence, lacunarity, scale) + 1) / 2;
  }

  // Ridged noise for mountain ranges
  getRidged(
    x: number,
    y: number,
    octaves: number = 4,
    persistence: number = 0.5,
    lacunarity: number = 2.0,
    scale: number = 1.0
  ): number {
    let total = 0;
    let frequency = scale;
    let amplitude = 1;
    let maxValue = 0;

    for (let i = 0; i < octaves; i++) {
      // Take absolute value and invert for ridges
      let n = 1 - Math.abs(this.noise2D(x * frequency, y * frequency));
      n = n * n; // Square for sharper ridges
      total += n * amplitude;
      maxValue += amplitude;
      amplitude *= persistence;
      frequency *= lacunarity;
    }

    return total / maxValue;
  }

  // Get seed for reproducibility
  getSeed(): number {
    return this.seed;
  }
}
