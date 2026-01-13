import * as THREE from 'three';
import { HexDirection } from '../core/HexDirection';

// Terrain types for the hex map
export enum TerrainType {
  Ocean = 'ocean',
  Coast = 'coast',
  Plains = 'plains',
  Forest = 'forest',
  Hills = 'hills',
  Mountains = 'mountains',
  Snow = 'snow',
  Desert = 'desert',
  Tundra = 'tundra',
  Jungle = 'jungle',
  Savanna = 'savanna',
  Taiga = 'taiga',
}

// Feature types that can be placed on hexes
export enum FeatureType {
  None = 'none',
  Tree = 'tree',
  Rock = 'rock',
  MountainPeak = 'mountainPeak',
}

// A single feature instance on a hex
export interface Feature {
  type: FeatureType;
  position: THREE.Vector3;
  scale: number;
  rotation: number;
}

// Core hex cell data
export interface HexCell {
  q: number;                  // Cube coordinate Q
  r: number;                  // Cube coordinate R
  s: number;                  // Cube coordinate S (derived: -q - r)
  elevation: number;          // -2 to 8
  terrainType: TerrainType;
  moisture: number;           // 0-1
  temperature: number;        // 0-1
  features: Feature[];
  riverDirections: HexDirection[];  // Directions water flows OUT of this cell
}

// Map generation configuration
export interface MapConfig {
  width: number;
  height: number;
  seed: number;
  noiseScale: number;
  octaves: number;
  persistence: number;
  lacunarity: number;
  landPercentage: number;
  mountainousness: number;
  riverPercentage: number;  // 0-0.2, controls river density
}

// Default map configuration
export const defaultMapConfig: MapConfig = {
  width: 40,
  height: 30,
  seed: 12345,
  noiseScale: 0.05,
  octaves: 4,
  persistence: 0.5,
  lacunarity: 2.0,
  landPercentage: 0.45,
  mountainousness: 0.6,
  riverPercentage: 0.1,  // 10% of land cells as river budget
};

// Camera configuration
export interface CameraConfig {
  minZoom: number;
  maxZoom: number;
  minPitch: number;
  maxPitch: number;
  panSpeed: number;
  rotateSpeed: number;
  zoomSpeed: number;
}

export const defaultCameraConfig: CameraConfig = {
  minZoom: 5,
  maxZoom: 80,
  minPitch: 20,
  maxPitch: 85,
  panSpeed: 0.5,
  rotateSpeed: 0.3,
  zoomSpeed: 2.0,
};

// Re-export unit types from units folder for backward compatibility
export {
  UnitType,
  UnitDomain,
  UnitStats,
  getUnitDomain,
  canTraverseLand,
  canTraverseWater,
} from '../units/UnitTypes';
export type { UnitData, UnitTypeStats } from '../units/UnitTypes';
