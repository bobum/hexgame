import { HexGrid } from '../core/HexGrid';
import { HexCell, MapConfig } from '../types';
import { HexDirection, AllDirections, opposite } from '../core/HexDirection';
import { HexMetrics } from '../core/HexMetrics';

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
 * Generates rivers flowing from high elevation to water.
 * Uses steepest descent algorithm with weighted random selection.
 */
export class RiverGenerator {
  private grid: HexGrid;
  private config: MapConfig;
  private random: () => number;

  constructor(grid: HexGrid, config: MapConfig) {
    this.grid = grid;
    this.config = config;
    // Use different seed offset for rivers to avoid correlation with terrain
    this.random = mulberry32(config.seed + 7777);
  }

  /**
   * Generate rivers on the map.
   */
  generate(): void {
    // Clear existing rivers
    for (const cell of this.grid.getAllCells()) {
      cell.riverDirections = [];
    }

    // Count land cells for budget calculation
    const landCells = this.grid.getAllCells().filter(
      cell => cell.elevation >= HexMetrics.waterLevel
    );

    if (landCells.length === 0) return;

    // Calculate river budget based on percentage
    let riverBudget = Math.round(landCells.length * this.config.riverPercentage);

    // Find potential river sources (high elevation + moisture)
    const sources = this.findRiverSources(landCells);

    // Generate rivers until budget exhausted or no more sources
    let attempts = 0;
    const maxAttempts = sources.length * 2;

    while (riverBudget > 0 && sources.length > 0 && attempts < maxAttempts) {
      attempts++;

      // Pick a random source (weighted toward better candidates)
      const sourceIndex = this.pickWeightedSource(sources);
      const source = sources[sourceIndex];

      // Try to create a river from this source
      const riverLength = this.traceRiver(source);

      if (riverLength > 0) {
        riverBudget -= riverLength;
        // Remove used source
        sources.splice(sourceIndex, 1);
      } else {
        // Source didn't work, remove it
        sources.splice(sourceIndex, 1);
      }
    }
  }

  /**
   * Find cells that are good river sources.
   * High elevation + high moisture = good source.
   */
  private findRiverSources(landCells: HexCell[]): HexCell[] {
    const sources: HexCell[] = [];
    const elevationRange = HexMetrics.maxElevation - HexMetrics.waterLevel;

    for (const cell of landCells) {
      // Skip cells already with rivers
      if (cell.riverDirections.length > 0) continue;

      // Skip cells adjacent to water (too close to ocean)
      if (this.isAdjacentToWater(cell)) continue;

      // Skip cells adjacent to existing rivers
      if (this.isAdjacentToRiver(cell)) continue;

      // Calculate source fitness score
      const elevationFactor = (cell.elevation - HexMetrics.waterLevel) / elevationRange;
      const score = cell.moisture * elevationFactor;

      // Add to sources with weighting
      if (score > 0.25) {
        sources.push(cell);
      }
    }

    return sources;
  }

  /**
   * Pick a source using weighted random selection.
   */
  private pickWeightedSource(sources: HexCell[]): number {
    const elevationRange = HexMetrics.maxElevation - HexMetrics.waterLevel;

    // Build weighted selection list
    const weights: number[] = [];
    let totalWeight = 0;

    for (const cell of sources) {
      const elevationFactor = (cell.elevation - HexMetrics.waterLevel) / elevationRange;
      const score = cell.moisture * elevationFactor;

      // Higher score = more weight
      let weight = 1;
      if (score > 0.75) weight = 4;
      else if (score > 0.5) weight = 2;

      weights.push(weight);
      totalWeight += weight;
    }

    // Random selection
    let pick = this.random() * totalWeight;
    for (let i = 0; i < weights.length; i++) {
      pick -= weights[i];
      if (pick <= 0) return i;
    }

    return sources.length - 1;
  }

  /**
   * Trace a river from source to water using steepest descent.
   * Returns the length of the river created.
   */
  private traceRiver(source: HexCell): number {
    let current = source;
    let length = 0;
    const visited = new Set<string>();

    while (current.elevation >= HexMetrics.waterLevel) {
      const key = `${current.q},${current.r}`;
      if (visited.has(key)) break; // Avoid loops
      visited.add(key);

      // Find best direction to flow
      const flowDir = this.findFlowDirection(current);

      if (flowDir === null) {
        // Can't flow anywhere - dead end
        break;
      }

      // Get the neighbor in that direction
      const neighbor = this.grid.getNeighbor(current, flowDir);
      if (!neighbor) break;

      // Check if neighbor already has a river (no merging)
      if (neighbor.riverDirections.length > 0) {
        // End river here, flowing into existing river
        current.riverDirections.push(flowDir);
        length++;
        break;
      }

      // Add river segment
      current.riverDirections.push(flowDir);
      length++;

      // Move to next cell
      current = neighbor;

      // Safety limit
      if (length > 100) break;
    }

    return length;
  }

  /**
   * Find the best direction for water to flow from a cell.
   * Prefers steepest downhill, with randomness for variety.
   */
  private findFlowDirection(cell: HexCell): HexDirection | null {
    const candidates: { dir: HexDirection; weight: number }[] = [];

    for (const dir of AllDirections) {
      const neighbor = this.grid.getNeighbor(cell, dir);
      if (!neighbor) continue;

      // Calculate elevation difference (positive = downhill)
      const elevationDiff = cell.elevation - neighbor.elevation;

      // Skip uphill (can't flow uphill)
      if (elevationDiff < 0) continue;

      // Weight based on steepness
      let weight = 1;
      if (elevationDiff > 0) {
        // Downhill - more weight for steeper descent
        weight = 1 + elevationDiff * 3;
      }

      // Extra weight if flowing toward water
      if (neighbor.elevation < HexMetrics.waterLevel) {
        weight += 5;
      }

      candidates.push({ dir, weight });
    }

    if (candidates.length === 0) return null;

    // Weighted random selection
    const totalWeight = candidates.reduce((sum, c) => sum + c.weight, 0);
    let pick = this.random() * totalWeight;

    for (const candidate of candidates) {
      pick -= candidate.weight;
      if (pick <= 0) return candidate.dir;
    }

    return candidates[candidates.length - 1].dir;
  }

  /**
   * Check if cell is adjacent to water.
   */
  private isAdjacentToWater(cell: HexCell): boolean {
    for (const dir of AllDirections) {
      const neighbor = this.grid.getNeighbor(cell, dir);
      if (neighbor && neighbor.elevation < HexMetrics.waterLevel) {
        return true;
      }
    }
    return false;
  }

  /**
   * Check if cell is adjacent to an existing river.
   */
  private isAdjacentToRiver(cell: HexCell): boolean {
    for (const dir of AllDirections) {
      const neighbor = this.grid.getNeighbor(cell, dir);
      if (neighbor && neighbor.riverDirections.length > 0) {
        return true;
      }
    }
    return false;
  }
}
