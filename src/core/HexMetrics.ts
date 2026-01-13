import * as THREE from 'three';
import { TerrainType } from '../types';

// Core hex geometry constants
export const HexMetrics = {
  // Hex geometry
  outerRadius: 1.0,                           // Corner to center distance
  innerRadius: 1.0 * 0.866025404,             // Edge to center (outer * sqrt(3)/2)

  // Elevation
  elevationStep: 0.4,                         // Height per elevation level
  maxElevation: 8,
  minElevation: -2,
  waterLevel: 0,                              // Sea level

  // Terraces (Catlike Coding style)
  terracesPerSlope: 2,                        // Number of flat terraces per slope
  get terraceSteps(): number {                // Total interpolation steps (2*terracesPerSlope + 1)
    return this.terracesPerSlope * 2 + 1;
  },
  get horizontalTerraceStepSize(): number {   // Horizontal interpolation step size
    return 1.0 / this.terraceSteps;
  },
  get verticalTerraceStepSize(): number {     // Vertical interpolation step size
    return 1.0 / (this.terracesPerSlope + 1);
  },

  // Blend regions
  solidFactor: 0.8,                           // Inner solid hex portion
  blendFactor: 0.2,                           // Outer blend portion

  // Get the 6 corner positions for a hex
  getCorners(): THREE.Vector3[] {
    const corners: THREE.Vector3[] = [];
    for (let i = 0; i < 6; i++) {
      const angle = (Math.PI / 3) * i + Math.PI / 6; // Start at 30°
      corners.push(new THREE.Vector3(
        Math.cos(angle) * this.outerRadius,
        0,
        Math.sin(angle) * this.outerRadius
      ));
    }
    return corners;
  },

  // Get corner at specific index (with wrapping)
  getCorner(index: number): THREE.Vector3 {
    const corners = this.getCorners();
    return corners[((index % 6) + 6) % 6].clone();
  },
};

/**
 * Terrace interpolation - the key to Catlike Coding style terraces.
 * Horizontal interpolation is linear, vertical only changes on odd steps.
 * This creates flat "platforms" separated by short slopes.
 *
 * @param a Start position
 * @param b End position
 * @param step Current step (1 to terraceSteps)
 * @returns Interpolated position
 */
export function terraceLerp(a: THREE.Vector3, b: THREE.Vector3, step: number): THREE.Vector3 {
  // Horizontal interpolation is linear
  const h = step * HexMetrics.horizontalTerraceStepSize;

  // Vertical interpolation only changes on odd steps
  // step: 1 -> v = 1/3, step: 2 -> v = 1/3, step: 3 -> v = 2/3, step: 4 -> v = 2/3
  const v = Math.floor((step + 1) / 2) * HexMetrics.verticalTerraceStepSize;

  return new THREE.Vector3(
    a.x + (b.x - a.x) * h,
    a.y + (b.y - a.y) * v,
    a.z + (b.z - a.z) * h
  );
}

/**
 * Interpolate color along terrace (same as terraceLerp but for colors).
 */
export function terraceColorLerp(a: THREE.Color, b: THREE.Color, step: number): THREE.Color {
  const h = step * HexMetrics.horizontalTerraceStepSize;
  return new THREE.Color(
    a.r + (b.r - a.r) * h,
    a.g + (b.g - a.g) * h,
    a.b + (b.b - a.b) * h
  );
}

// Terrain colors - stylized low-poly palette
export const TerrainColors: Record<TerrainType, number> = {
  [TerrainType.Ocean]: 0x1a4c6e,      // Deep blue
  [TerrainType.Coast]: 0x2d8bc9,      // Light blue
  [TerrainType.Plains]: 0x7cb342,     // Grass green
  [TerrainType.Forest]: 0x2e7d32,     // Dark green
  [TerrainType.Hills]: 0x8d6e63,      // Brown
  [TerrainType.Mountains]: 0x757575,  // Gray
  [TerrainType.Snow]: 0xeceff1,       // White
  [TerrainType.Desert]: 0xe6c86e,     // Sand yellow
  [TerrainType.Tundra]: 0x90a4ae,     // Blue-gray
  [TerrainType.Jungle]: 0x1b5e20,     // Deep green
  [TerrainType.Savanna]: 0xc5a855,    // Golden brown
  [TerrainType.Taiga]: 0x4a635d,      // Dark teal-green
};

// Get THREE.Color from terrain type
export function getTerrainColor(terrain: TerrainType): THREE.Color {
  return new THREE.Color(TerrainColors[terrain]);
}

// Slightly vary a color for visual interest
export function varyColor(color: THREE.Color, amount: number = 0.05): THREE.Color {
  const hsl = { h: 0, s: 0, l: 0 };
  color.getHSL(hsl);
  hsl.l += (Math.random() - 0.5) * amount;
  hsl.l = Math.max(0, Math.min(1, hsl.l));
  return new THREE.Color().setHSL(hsl.h, hsl.s, hsl.l);
}

// Terrain type to numeric index for shader
const TerrainTypeIndices: Record<TerrainType, number> = {
  [TerrainType.Ocean]: 0,
  [TerrainType.Coast]: 1,
  [TerrainType.Plains]: 2,
  [TerrainType.Forest]: 3,
  [TerrainType.Hills]: 4,
  [TerrainType.Mountains]: 5,
  [TerrainType.Snow]: 6,
  [TerrainType.Desert]: 7,
  [TerrainType.Tundra]: 8,
  [TerrainType.Jungle]: 9,
  [TerrainType.Savanna]: 10,
  [TerrainType.Taiga]: 11,
};

// Get numeric index for terrain type (for shaders)
export function getTerrainTypeIndex(terrain: TerrainType): number {
  return TerrainTypeIndices[terrain];
}
