import { TerrainType } from '../types';

/**
 * Maps temperature and moisture values to terrain types.
 * Uses a grid-based approach similar to real biome classification.
 */

// Biome grid: rows are moisture (dry to wet), columns are temperature (cold to hot)
// 3x3 grid with thresholds at 0.33 and 0.66
const BiomeGrid: TerrainType[][] = [
  // Dry (moisture < 0.33)
  [TerrainType.Tundra, TerrainType.Desert, TerrainType.Desert],
  // Medium (0.33 <= moisture < 0.66)
  [TerrainType.Taiga, TerrainType.Plains, TerrainType.Savanna],
  // Wet (moisture >= 0.66)
  [TerrainType.Snow, TerrainType.Forest, TerrainType.Jungle],
];

/**
 * Get terrain type from temperature and moisture values.
 * Both values should be 0-1.
 */
export function getBiome(temperature: number, moisture: number): TerrainType {
  // Clamp values
  const t = Math.max(0, Math.min(1, temperature));
  const m = Math.max(0, Math.min(1, moisture));

  // Convert to grid indices
  const tempIndex = t < 0.33 ? 0 : t < 0.66 ? 1 : 2;
  const moistIndex = m < 0.33 ? 0 : m < 0.66 ? 1 : 2;

  return BiomeGrid[moistIndex][tempIndex];
}

/**
 * Get terrain type based on elevation and biome.
 * High elevations override biome with mountains/snow.
 */
export function getTerrainFromElevationAndBiome(
  elevation: number,
  temperature: number,
  moisture: number,
  waterLevel: number = 0
): TerrainType {
  // Underwater
  if (elevation < waterLevel) {
    return elevation < waterLevel - 1 ? TerrainType.Ocean : TerrainType.Coast;
  }

  // Very high elevation = mountains or snow
  if (elevation >= 6) {
    return TerrainType.Snow;
  }
  if (elevation >= 4) {
    return TerrainType.Mountains;
  }
  if (elevation >= 3) {
    return TerrainType.Hills;
  }

  // Use biome for lower elevations
  return getBiome(temperature, moisture);
}

/**
 * Calculate temperature based on latitude (row position) and elevation.
 * Returns 0-1 where 0 is cold and 1 is hot.
 */
export function calculateTemperature(
  row: number,
  maxRow: number,
  elevation: number,
  baseTemperature: number = 0.5,
  jitter: number = 0
): number {
  // Latitude effect: warmer near equator (middle of map)
  const normalizedRow = row / maxRow;
  const distanceFromEquator = Math.abs(normalizedRow - 0.5) * 2; // 0 at middle, 1 at edges
  const latitudeTemp = 1 - distanceFromEquator;

  // Elevation effect: colder at higher elevations
  const elevationEffect = Math.max(0, elevation) * 0.1;

  // Combine
  let temp = latitudeTemp * baseTemperature - elevationEffect + jitter;

  return Math.max(0, Math.min(1, temp));
}

/**
 * Calculate moisture based on distance from water and noise.
 */
export function calculateMoisture(
  distanceFromWater: number,
  noiseValue: number,
  maxDistance: number = 10
): number {
  // Closer to water = more moisture
  const waterMoisture = 1 - Math.min(distanceFromWater / maxDistance, 1);

  // Combine with noise
  const moisture = waterMoisture * 0.7 + noiseValue * 0.3;

  return Math.max(0, Math.min(1, moisture));
}
