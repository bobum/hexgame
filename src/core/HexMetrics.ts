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
