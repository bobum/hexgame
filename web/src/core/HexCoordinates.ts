import * as THREE from 'three';
import { HexMetrics } from './HexMetrics';
import { HexDirection, DirectionOffsets } from './HexDirection';

/**
 * Cube coordinates for hexagonal grids.
 * Uses the constraint: q + r + s = 0
 * This is an immutable value type.
 */
export class HexCoordinates {
  readonly q: number;  // Column-ish
  readonly r: number;  // Row-ish (diagonal)
  readonly s: number;  // Derived: -q - r

  constructor(q: number, r: number) {
    this.q = q;
    this.r = r;
    this.s = -q - r;
  }

  // Create from offset coordinates (rectangular grid)
  static fromOffset(col: number, row: number): HexCoordinates {
    // Odd-r offset coordinate system
    const q = col - Math.floor(row / 2);
    const r = row;
    return new HexCoordinates(q, r);
  }

  // Create from world position (XZ plane)
  static fromWorldPosition(position: THREE.Vector3): HexCoordinates {
    const { outerRadius, innerRadius } = HexMetrics;

    // Convert to fractional cube coordinates
    const q = (position.x * Math.sqrt(3) / 3 - position.z / 3) / outerRadius;
    const r = position.z * 2 / 3 / outerRadius;

    return HexCoordinates.round(q, r);
  }

  // Round fractional cube coordinates to nearest hex
  static round(q: number, r: number): HexCoordinates {
    const s = -q - r;

    let rq = Math.round(q);
    let rr = Math.round(r);
    let rs = Math.round(s);

    const qDiff = Math.abs(rq - q);
    const rDiff = Math.abs(rr - r);
    const sDiff = Math.abs(rs - s);

    // Fix rounding errors by recalculating the coordinate with largest diff
    if (qDiff > rDiff && qDiff > sDiff) {
      rq = -rr - rs;
    } else if (rDiff > sDiff) {
      rr = -rq - rs;
    }
    // s is always derived, no need to fix

    return new HexCoordinates(rq, rr);
  }

  // Convert to world position (center of hex)
  toWorldPosition(elevation: number = 0): THREE.Vector3 {
    const { outerRadius, innerRadius, elevationStep } = HexMetrics;

    const x = (this.q + this.r / 2) * innerRadius * 2;
    const z = this.r * outerRadius * 1.5;
    const y = elevation * elevationStep;

    return new THREE.Vector3(x, y, z);
  }

  // Get neighbor in specified direction
  getNeighbor(direction: HexDirection): HexCoordinates {
    const [dq, dr] = DirectionOffsets[direction];
    return new HexCoordinates(this.q + dq, this.r + dr);
  }

  // Get all 6 neighbors
  getNeighbors(): HexCoordinates[] {
    return [
      this.getNeighbor(HexDirection.NE),
      this.getNeighbor(HexDirection.E),
      this.getNeighbor(HexDirection.SE),
      this.getNeighbor(HexDirection.SW),
      this.getNeighbor(HexDirection.W),
      this.getNeighbor(HexDirection.NW),
    ];
  }

  // Distance to another hex (in hex steps)
  distanceTo(other: HexCoordinates): number {
    return Math.max(
      Math.abs(this.q - other.q),
      Math.abs(this.r - other.r),
      Math.abs(this.s - other.s)
    );
  }

  // Get all hexes in a ring at distance
  static ring(center: HexCoordinates, radius: number): HexCoordinates[] {
    if (radius === 0) return [center];

    const results: HexCoordinates[] = [];
    // Start at the hex radius steps in direction 4 (SW)
    let hex = new HexCoordinates(
      center.q + DirectionOffsets[HexDirection.SW][0] * radius,
      center.r + DirectionOffsets[HexDirection.SW][1] * radius
    );

    // Walk around the ring
    for (let dir = 0; dir < 6; dir++) {
      for (let i = 0; i < radius; i++) {
        results.push(hex);
        hex = hex.getNeighbor(dir as HexDirection);
      }
    }

    return results;
  }

  // Get all hexes in a spiral from center out to radius
  static spiral(center: HexCoordinates, radius: number): HexCoordinates[] {
    const results: HexCoordinates[] = [center];
    for (let r = 1; r <= radius; r++) {
      results.push(...HexCoordinates.ring(center, r));
    }
    return results;
  }

  // Equality check
  equals(other: HexCoordinates): boolean {
    return this.q === other.q && this.r === other.r;
  }

  // String key for use in Maps
  toKey(): string {
    return `${this.q},${this.r}`;
  }

  // Create from key string
  static fromKey(key: string): HexCoordinates {
    const [q, r] = key.split(',').map(Number);
    return new HexCoordinates(q, r);
  }

  toString(): string {
    return `Hex(${this.q}, ${this.r}, ${this.s})`;
  }
}
