import * as THREE from 'three';
import { HexMetrics, getTerrainColor, varyColor, getTerrainTypeIndex, terraceLerp, terraceColorLerp } from '../core/HexMetrics';
import { HexCoordinates } from '../core/HexCoordinates';
import { HexDirection } from '../core/HexDirection';
import { HexCell, TerrainType } from '../types';
import { HexGrid } from '../core/HexGrid';

/**
 * Builds hex mesh geometry with flat shading and vertex colors.
 * Includes terrain type attribute for shader-based rendering.
 */
export class HexMeshBuilder {
  private vertices: number[] = [];
  private colors: number[] = [];
  private terrainTypes: number[] = [];
  // Splat map approach: 3 colors + RGB weights
  private color1: number[] = [];         // Main terrain color (R weight)
  private color2: number[] = [];         // First neighbor color (G weight)
  private color3: number[] = [];         // Second neighbor color (B weight)
  private splatWeights: number[] = [];   // RGB weights for blending
  private indices: number[] = [];
  private vertexIndex = 0;
  private currentTerrainType: TerrainType = TerrainType.Plains;
  // Current splat state
  private currentColor1 = new THREE.Color();
  private currentColor2 = new THREE.Color();
  private currentColor3 = new THREE.Color();
  private currentWeights = new THREE.Vector3(1, 0, 0);

  // Pre-calculated corner offsets for a flat-topped hex
  // Corners at 30°, 90°, 150°, 210°, 270°, 330°
  private corners: THREE.Vector3[];

  constructor() {
    this.corners = [];
    for (let i = 0; i < 6; i++) {
      const angle = (Math.PI / 3) * i + Math.PI / 6;
      this.corners.push(new THREE.Vector3(
        Math.cos(angle) * HexMetrics.outerRadius,
        0,
        Math.sin(angle) * HexMetrics.outerRadius
      ));
    }
    this.reset();
  }

  reset(): void {
    this.vertices = [];
    this.colors = [];
    this.terrainTypes = [];
    this.color1 = [];
    this.color2 = [];
    this.color3 = [];
    this.splatWeights = [];
    this.indices = [];
    this.vertexIndex = 0;
  }

  /**
   * Build geometry for a single hex cell.
   */
  buildCell(cell: HexCell, grid: HexGrid): void {
    const coords = new HexCoordinates(cell.q, cell.r);
    const center = coords.toWorldPosition(cell.elevation);
    const baseColor = varyColor(getTerrainColor(cell.terrainType), 0.08);
    this.currentTerrainType = cell.terrainType;

    // Gather neighbor data for blending
    const neighbors: (HexCell | undefined)[] = [];
    const neighborColors: THREE.Color[] = [];
    for (let dir = 0; dir < 6; dir++) {
      const neighbor = grid.getNeighbor(cell, dir as HexDirection);
      neighbors.push(neighbor);
      if (neighbor) {
        neighborColors.push(getTerrainColor(neighbor.terrainType));
      } else {
        neighborColors.push(baseColor.clone());
      }
    }

    // 1. Build the solid center hexagon
    this.buildTopFaceWithSplatting(center, baseColor, neighborColors);

    // 2. Build edge connections and corners for each direction
    for (let dir = 0; dir < 6; dir++) {
      const neighbor = neighbors[dir];
      const edgeIndex = this.getEdgeIndexForDirection(dir as HexDirection);

      // Build edge connection (only if we're the "owner" - higher cell or same level with lower coords)
      if (!neighbor) {
        // Edge of map - cliff down
        const wallHeight = (cell.elevation - HexMetrics.minElevation) * HexMetrics.elevationStep;
        if (wallHeight > 0) {
          this.buildCliff(center, edgeIndex, wallHeight, baseColor);
        }
      } else {
        const elevationDiff = cell.elevation - neighbor.elevation;
        const neighborCenter = new HexCoordinates(neighbor.q, neighbor.r).toWorldPosition(neighbor.elevation);
        const neighborColor = getTerrainColor(neighbor.terrainType);

        if (elevationDiff > 0) {
          // We're higher - build terraced slope (always, in any direction)
          this.buildTerracedSlope(center, neighborCenter, edgeIndex, baseColor, neighborColor);
        } else if (elevationDiff === 0 && dir <= 2) {
          // Same level - build flat bridge only in directions 0, 1, 2 to avoid duplicates
          this.buildFlatEdge(center, neighborCenter, edgeIndex, baseColor, neighborColor);
        }
        // If neighbor is higher (elevationDiff < 0), they build the terraced slope
      }

      // Build corner triangle (between this edge and the previous)
      // Corner at end of edge for direction d is shared with neighbor[d] and neighbor[d-1]
      // Only build in directions 0 and 1 following Catlike Coding's approach
      if (dir <= 1) {
        const prevDir = (dir + 5) % 6;  // Previous direction (wraps 0 -> 5)
        const prevNeighbor = neighbors[prevDir];
        if (neighbor && prevNeighbor) {
          this.buildCorner(cell, center, baseColor, dir as HexDirection, neighbor, prevNeighbor, grid);
        }
      }
    }
  }

  /**
   * Get edge index for a hex direction.
   */
  private getEdgeIndexForDirection(dir: HexDirection): number {
    // Map direction to edge index based on our corner layout
    // This needs to match how corners are arranged
    const dirToEdge = [5, 4, 3, 2, 1, 0];
    return dirToEdge[dir];
  }

  /**
   * Get the edge index (0-5) that faces the given angle.
   */
  private getEdgeIndexForAngle(angle: number): number {
    // Normalize angle to 0 to 2*PI
    let normalizedAngle = angle;
    while (normalizedAngle < 0) normalizedAngle += Math.PI * 2;
    while (normalizedAngle >= Math.PI * 2) normalizedAngle -= Math.PI * 2;

    // Each edge spans 60 degrees (PI/3 radians)
    // Edge 0 (between corners 0 and 1) faces outward at 60° (PI/3)
    // But corners start at 30°, so edge 0 faces at 60°
    // Edge i faces at (i+1) * 60° = (i+1) * PI/3

    // We need to find which edge's facing angle is closest to our target angle
    // Edge i faces at angle: (i * PI/3) + PI/6 + PI/2 = i * PI/3 + 2*PI/3
    // Actually, let's compute it differently:
    // Corner i is at angle (i * PI/3 + PI/6)
    // Edge i (from corner i to corner i+1) faces perpendicular to the edge line
    // The outward normal of edge i points at angle: (corner_i_angle + corner_{i+1}_angle) / 2 + PI/2

    // Simpler: edge 0 is between corners at 30° and 90°, so it faces at 60° (average)
    // But the OUTWARD direction is perpendicular, so it faces at 60° - 90° = -30° or equivalently 330°
    // Hmm, that's not right either.

    // Let me think again. The edge between corner 0 (30°) and corner 1 (90°)
    // is a line segment. The outward normal points away from center.
    // The midpoint of this edge is at angle 60°, so the outward normal points at 60°.

    // So edge i faces at angle: (i + 0.5) * PI/3 + PI/6 = (i + 1) * PI/3
    // Edge 0 faces at PI/3 = 60°
    // Edge 1 faces at 2*PI/3 = 120°
    // Edge 2 faces at PI = 180°
    // Edge 3 faces at 4*PI/3 = 240°
    // Edge 4 faces at 5*PI/3 = 300°
    // Edge 5 faces at 2*PI = 360° = 0°

    // Find which edge faces closest to our angle
    for (let i = 0; i < 6; i++) {
      const edgeFacingAngle = ((i + 1) * Math.PI / 3) % (Math.PI * 2);
      const diff = Math.abs(normalizedAngle - edgeFacingAngle);
      const diffWrapped = Math.min(diff, Math.PI * 2 - diff);
      if (diffWrapped < Math.PI / 6) { // Within 30 degrees
        return i;
      }
    }

    // Fallback - shouldn't happen
    return 0;
  }

  // Maps edge index to HexDirection index
  // Edge 0 faces direction 5, Edge 1 faces direction 4, etc.
  private static readonly EDGE_TO_DIRECTION = [5, 4, 3, 2, 1, 0];

  /**
   * Build the top hexagonal face (solid inner region only).
   * Uses solidFactor to create smaller hex, leaving room for edge/corner connections.
   */
  private buildTopFaceWithSplatting(
    center: THREE.Vector3,
    color: THREE.Color,
    neighborColors: THREE.Color[]
  ): void {
    const solid = HexMetrics.solidFactor;

    for (let i = 0; i < 6; i++) {
      const corner1 = this.corners[i];
      const corner2 = this.corners[(i + 1) % 6];

      const v1 = center.clone();
      // Use solid factor - corners at 80% of full radius
      const v2 = new THREE.Vector3(
        center.x + corner1.x * solid,
        center.y,
        center.z + corner1.z * solid
      );
      const v3 = new THREE.Vector3(
        center.x + corner2.x * solid,
        center.y,
        center.z + corner2.z * solid
      );

      // Get the two neighbors that border this edge
      const edgeDir = HexMeshBuilder.EDGE_TO_DIRECTION[i];
      const prevDir = (edgeDir + 1) % 6;

      const neighborColor1 = neighborColors[edgeDir];
      const neighborColor2 = neighborColors[prevDir];

      // Center vertex - 100% main color
      this.currentColor1.copy(color);
      this.currentColor2.copy(color);
      this.currentColor3.copy(color);
      this.currentWeights.set(1, 0, 0);
      this.addVertex(v1, color);

      // Corner vertices - blend with neighbors
      const nextDir = (edgeDir + 5) % 6;
      const neighborColorRight = neighborColors[nextDir];

      this.currentColor1.copy(color);
      this.currentColor2.copy(neighborColor1);
      this.currentColor3.copy(neighborColorRight);
      this.currentWeights.set(0.34, 0.33, 0.33);
      this.addVertex(v3, color);

      this.currentColor1.copy(color);
      this.currentColor2.copy(neighborColor1);
      this.currentColor3.copy(neighborColor2);
      this.currentWeights.set(0.34, 0.33, 0.33);
      this.addVertex(v2, color);

      this.indices.push(this.vertexIndex - 3, this.vertexIndex - 2, this.vertexIndex - 1);
    }
  }

  /**
   * Build a cliff (vertical wall) on a specific edge of the hex.
   * Used for map edges and multi-level elevation differences.
   */
  private buildCliff(
    center: THREE.Vector3,
    edgeIndex: number,
    height: number,
    baseColor: THREE.Color
  ): void {
    const wallColor = baseColor.clone().multiplyScalar(0.65);

    const corner1 = this.corners[edgeIndex];
    const corner2 = this.corners[(edgeIndex + 1) % 6];

    const topLeft = new THREE.Vector3(center.x + corner1.x, center.y, center.z + corner1.z);
    const topRight = new THREE.Vector3(center.x + corner2.x, center.y, center.z + corner2.z);
    const bottomLeft = new THREE.Vector3(topLeft.x, center.y - height, topLeft.z);
    const bottomRight = new THREE.Vector3(topRight.x, center.y - height, topRight.z);

    // Two triangles for the quad - reversed winding for outward-facing normal
    this.addTriangle(topLeft, bottomRight, bottomLeft, wallColor);
    this.addTriangle(topLeft, topRight, bottomRight, wallColor);
  }

  /**
   * Build a terraced slope on a specific edge of the hex.
   * Creates Catlike Coding style stepped terrain between elevation levels.
   * The terraces form a "bridge" between the solid regions of adjacent hexes.
   */
  private buildTerracedSlope(
    center: THREE.Vector3,
    neighborCenter: THREE.Vector3,
    edgeIndex: number,
    beginColor: THREE.Color,
    endColor: THREE.Color
  ): void {
    const corner1 = this.corners[edgeIndex];
    const corner2 = this.corners[(edgeIndex + 1) % 6];

    // Use solid factor to get the inner edge of this cell's solid region
    const solid = HexMetrics.solidFactor;

    // Top edge: outer boundary of THIS cell's solid region (higher elevation)
    const topLeft = new THREE.Vector3(
      center.x + corner1.x * solid,
      center.y,
      center.z + corner1.z * solid
    );
    const topRight = new THREE.Vector3(
      center.x + corner2.x * solid,
      center.y,
      center.z + corner2.z * solid
    );

    // The neighbor's edge that faces us is the opposite edge
    const oppositeEdge = (edgeIndex + 3) % 6;
    const oppCorner1 = this.corners[oppositeEdge];
    const oppCorner2 = this.corners[(oppositeEdge + 1) % 6];

    // Bottom edge: outer boundary of NEIGHBOR's solid region (lower elevation)
    // Note: corners are swapped to align the edges properly
    const bottomLeft = new THREE.Vector3(
      neighborCenter.x + oppCorner2.x * solid,
      neighborCenter.y,
      neighborCenter.z + oppCorner2.z * solid
    );
    const bottomRight = new THREE.Vector3(
      neighborCenter.x + oppCorner1.x * solid,
      neighborCenter.y,
      neighborCenter.z + oppCorner1.z * solid
    );

    // Build terraces from top to bottom
    let v1 = topLeft.clone();
    let v2 = topRight.clone();
    let c1 = beginColor.clone();
    let c2 = beginColor.clone();

    for (let step = 1; step <= HexMetrics.terraceSteps; step++) {
      // Interpolate to the next terrace level
      // terraceLerp handles the stepped vertical interpolation
      const v3 = terraceLerp(topLeft, bottomLeft, step);
      const v4 = terraceLerp(topRight, bottomRight, step);
      const c3 = terraceColorLerp(beginColor, endColor, step);
      const c4 = terraceColorLerp(beginColor, endColor, step);

      // Build quad for this terrace step
      const avgColor = new THREE.Color(
        (c1.r + c2.r + c3.r + c4.r) / 4,
        (c1.g + c2.g + c3.g + c4.g) / 4,
        (c1.b + c2.b + c3.b + c4.b) / 4
      );

      // Two triangles for the quad
      this.addTriangle(v1, v4, v3, avgColor);
      this.addTriangle(v1, v2, v4, avgColor);

      // Move to next step
      v1 = v3.clone();
      v2 = v4.clone();
      c1 = c3.clone();
      c2 = c4.clone();
    }
  }

  /**
   * Get the bridge offset for an edge direction (Catlike Coding style).
   * The bridge extends from the solid corner outward by blendFactor.
   */
  private getBridge(edgeIndex: number): THREE.Vector3 {
    const blend = HexMetrics.blendFactor;
    const c1 = this.corners[edgeIndex];
    const c2 = this.corners[(edgeIndex + 1) % 6];
    return new THREE.Vector3(
      (c1.x + c2.x) * blend,
      0,
      (c1.z + c2.z) * blend
    );
  }

  /**
   * Build a flat edge connection between two same-elevation hexes.
   * Spans from this cell's solid edge to the neighbor's solid edge.
   */
  private buildFlatEdge(
    center: THREE.Vector3,
    neighborCenter: THREE.Vector3,
    edgeIndex: number,
    color: THREE.Color,
    neighborColor: THREE.Color
  ): void {
    const solid = HexMetrics.solidFactor;
    const c1 = this.corners[edgeIndex];
    const c2 = this.corners[(edgeIndex + 1) % 6];

    // This cell's solid edge corners
    const v1 = new THREE.Vector3(center.x + c1.x * solid, center.y, center.z + c1.z * solid);
    const v2 = new THREE.Vector3(center.x + c2.x * solid, center.y, center.z + c2.z * solid);

    // Neighbor's solid edge corners (opposite edge)
    const oppositeEdge = (edgeIndex + 3) % 6;
    const oc1 = this.corners[oppositeEdge];
    const oc2 = this.corners[(oppositeEdge + 1) % 6];

    // Note: oc2 aligns with v1, oc1 aligns with v2 (corners are swapped)
    const v3 = new THREE.Vector3(neighborCenter.x + oc2.x * solid, neighborCenter.y, neighborCenter.z + oc2.z * solid);
    const v4 = new THREE.Vector3(neighborCenter.x + oc1.x * solid, neighborCenter.y, neighborCenter.z + oc1.z * solid);

    // Blend colors
    const blendColor = new THREE.Color(
      (color.r + neighborColor.r) / 2,
      (color.g + neighborColor.g) / 2,
      (color.b + neighborColor.b) / 2
    );

    // Build quad (two triangles) - CCW winding for upward-facing
    // Quad layout: v2--v4
    //              |   |
    //              v1--v3
    this.addTriangle(v1, v2, v4, blendColor);
    this.addTriangle(v1, v4, v3, blendColor);
  }

  /**
   * Build corner geometry where three hexes meet.
   * Handles terraced corners following Catlike Coding approach.
   */
  private buildCorner(
    cell: HexCell,
    center: THREE.Vector3,
    color: THREE.Color,
    dir: HexDirection,
    neighbor1: HexCell,  // neighbor in direction dir
    neighbor2: HexCell,  // neighbor in direction (dir - 1), i.e., prevDir
    grid: HexGrid
  ): void {
    const solid = HexMetrics.solidFactor;
    const edgeIndex = this.getEdgeIndexForDirection(dir);

    // The shared corner position P (at full radius) - where all three cells meet
    const cornerIdx = (edgeIndex + 1) % 6;
    const cornerOffset = this.corners[cornerIdx];
    const P = new THREE.Vector3(
      center.x + cornerOffset.x,
      0,
      center.z + cornerOffset.z
    );

    // Get neighbor centers at their elevations
    const n1Center = new HexCoordinates(neighbor1.q, neighbor1.r).toWorldPosition(neighbor1.elevation);
    const n2Center = new HexCoordinates(neighbor2.q, neighbor2.r).toWorldPosition(neighbor2.elevation);

    // Calculate solid corner vertices for each cell
    const v1 = new THREE.Vector3(
      center.x + cornerOffset.x * solid,
      center.y,
      center.z + cornerOffset.z * solid
    );
    const v2 = new THREE.Vector3(
      n1Center.x + (P.x - n1Center.x) * solid,
      n1Center.y,
      n1Center.z + (P.z - n1Center.z) * solid
    );
    const v3 = new THREE.Vector3(
      n2Center.x + (P.x - n2Center.x) * solid,
      n2Center.y,
      n2Center.z + (P.z - n2Center.z) * solid
    );

    // Get colors
    const c1 = color.clone();
    const c2 = getTerrainColor(neighbor1.terrainType);
    const c3 = getTerrainColor(neighbor2.terrainType);

    // Sort by elevation to find bottom, left, right (Catlike Coding approach)
    // Bottom is lowest, then we go counter-clockwise for left and right
    const e1 = cell.elevation;
    const e2 = neighbor1.elevation;
    const e3 = neighbor2.elevation;

    if (e1 <= e2) {
      if (e1 <= e3) {
        // e1 is lowest - current cell is bottom
        this.triangulateCorner(v1, c1, e1, v2, c2, e2, v3, c3, e3);
      } else {
        // e3 is lowest - neighbor2 is bottom, rotate CCW
        this.triangulateCorner(v3, c3, e3, v1, c1, e1, v2, c2, e2);
      }
    } else {
      if (e2 <= e3) {
        // e2 is lowest - neighbor1 is bottom, rotate CW
        this.triangulateCorner(v2, c2, e2, v3, c3, e3, v1, c1, e1);
      } else {
        // e3 is lowest - neighbor2 is bottom, rotate CCW
        this.triangulateCorner(v3, c3, e3, v1, c1, e1, v2, c2, e2);
      }
    }
  }

  /**
   * Triangulate a corner with bottom vertex first, then left and right.
   * Determines edge types and calls appropriate method.
   * Key insight: for Slope-Flat cases, terraces fan from the HIGH vertex.
   */
  private triangulateCorner(
    bottom: THREE.Vector3, bottomColor: THREE.Color, bottomElevation: number,
    left: THREE.Vector3, leftColor: THREE.Color, leftElevation: number,
    right: THREE.Vector3, rightColor: THREE.Color, rightElevation: number
  ): void {
    const leftEdgeType = this.getEdgeType(bottomElevation, leftElevation);
    const rightEdgeType = this.getEdgeType(bottomElevation, rightElevation);

    if (leftEdgeType === 'slope') {
      if (rightEdgeType === 'slope') {
        // Slope-Slope: terraced corner fanning from bottom (lowest)
        this.triangulateCornerTerraces(
          bottom, bottomColor, left, leftColor, right, rightColor
        );
      } else {
        // Slope-Flat: left is higher, right is same as bottom
        // Terraces fan from LEFT (the high point) down to both right and bottom
        this.triangulateCornerTerraces(
          left, leftColor, right, rightColor, bottom, bottomColor
        );
      }
    } else {
      if (rightEdgeType === 'slope') {
        // Flat-Slope: right is higher, left is same as bottom
        // Terraces fan from RIGHT (the high point) down to both bottom and left
        this.triangulateCornerTerraces(
          right, rightColor, bottom, bottomColor, left, leftColor
        );
      } else {
        // Flat-Flat: simple triangle
        this.addTriangleWithColors(bottom, bottomColor, left, leftColor, right, rightColor);
      }
    }
  }

  /**
   * Determine edge type based on elevation difference.
   * For terraced terrain, any elevation difference is a slope (terraced).
   */
  private getEdgeType(e1: number, e2: number): 'flat' | 'slope' {
    const diff = Math.abs(e1 - e2);
    if (diff === 0) return 'flat';
    return 'slope';  // All elevation differences get terraced
  }

  /**
   * Triangulate a corner where both edges are slopes (terraced).
   * Creates fan of triangles/quads from bottom vertex.
   */
  private triangulateCornerTerraces(
    bottom: THREE.Vector3, bottomColor: THREE.Color,
    left: THREE.Vector3, leftColor: THREE.Color,
    right: THREE.Vector3, rightColor: THREE.Color
  ): void {
    // First terrace step
    let v3 = terraceLerp(bottom, left, 1);
    let v4 = terraceLerp(bottom, right, 1);
    let c3 = terraceColorLerp(bottomColor, leftColor, 1);
    let c4 = terraceColorLerp(bottomColor, rightColor, 1);

    // Initial triangle from bottom to first step
    this.addTriangleWithColors(bottom, bottomColor, v3, c3, v4, c4);

    // Middle quads
    for (let i = 2; i < HexMetrics.terraceSteps; i++) {
      const v1 = v3;
      const v2 = v4;
      const c1 = c3;
      const c2 = c4;
      v3 = terraceLerp(bottom, left, i);
      v4 = terraceLerp(bottom, right, i);
      c3 = terraceColorLerp(bottomColor, leftColor, i);
      c4 = terraceColorLerp(bottomColor, rightColor, i);
      this.addQuadWithColors(v1, c1, v2, c2, v3, c3, v4, c4);
    }

    // Final quad to left and right vertices
    this.addQuadWithColors(v3, c3, v4, c4, left, leftColor, right, rightColor);
  }

  /**
   * Add a triangle with per-vertex colors.
   */
  private addTriangleWithColors(
    v1: THREE.Vector3, c1: THREE.Color,
    v2: THREE.Vector3, c2: THREE.Color,
    v3: THREE.Vector3, c3: THREE.Color
  ): void {
    // Calculate normal to determine winding
    const edge1 = new THREE.Vector3().subVectors(v2, v1);
    const edge2 = new THREE.Vector3().subVectors(v3, v1);
    const normal = new THREE.Vector3().crossVectors(edge1, edge2);

    const avgColor = new THREE.Color(
      (c1.r + c2.r + c3.r) / 3,
      (c1.g + c2.g + c3.g) / 3,
      (c1.b + c2.b + c3.b) / 3
    );

    if (normal.y < 0) {
      this.addTriangle(v1, v3, v2, avgColor);
    } else {
      this.addTriangle(v1, v2, v3, avgColor);
    }
  }

  /**
   * Add a quad with per-vertex colors.
   * Layout: v1 is bottom-left, v2 is bottom-right, v3 is top-left, v4 is top-right
   * Triangles share edge v2-v3 (diagonal), not just a vertex.
   */
  private addQuadWithColors(
    v1: THREE.Vector3, c1: THREE.Color,
    v2: THREE.Vector3, c2: THREE.Color,
    v3: THREE.Vector3, c3: THREE.Color,
    v4: THREE.Vector3, c4: THREE.Color
  ): void {
    const avgColor = new THREE.Color(
      (c1.r + c2.r + c3.r + c4.r) / 4,
      (c1.g + c2.g + c3.g + c4.g) / 4,
      (c1.b + c2.b + c3.b + c4.b) / 4
    );

    // Two triangles sharing edge v2-v3:
    // Triangle 1: v1 -> v2 -> v3 (bottom-left, bottom-right, top-left)
    // Triangle 2: v2 -> v4 -> v3 (bottom-right, top-right, top-left)
    this.addTriangleWithColors(v1, avgColor, v2, avgColor, v3, avgColor);
    this.addTriangleWithColors(v2, avgColor, v4, avgColor, v3, avgColor);
  }

  /**
   * Check if this cell should "own" (build) the corner shared with two neighbors.
   */
  private isCornerOwner(cell: HexCell, n1: HexCell, n2: HexCell): boolean {
    // Owner is the cell with lowest elevation, then lowest q, then lowest r
    const cells = [cell, n1, n2];
    cells.sort((a, b) => {
      if (a.elevation !== b.elevation) return a.elevation - b.elevation;
      if (a.q !== b.q) return a.q - b.q;
      return a.r - b.r;
    });
    return cells[0] === cell;
  }

  /**
   * Add a triangle with automatic winding order correction.
   * Ensures the triangle faces upward (positive Y normal).
   */
  private addTriangleWithWindingCheck(
    v1: THREE.Vector3,
    v2: THREE.Vector3,
    v3: THREE.Vector3,
    color: THREE.Color
  ): void {
    const edge1 = new THREE.Vector3().subVectors(v2, v1);
    const edge2 = new THREE.Vector3().subVectors(v3, v1);
    const normal = new THREE.Vector3().crossVectors(edge1, edge2);

    if (normal.y < 0) {
      this.addTriangle(v1, v3, v2, color);
    } else {
      this.addTriangle(v1, v2, v3, color);
    }
  }

  /**
   * Build a terraced or cliff corner for different elevations.
   */
  private buildTerracedCorner(
    v1: THREE.Vector3, v2: THREE.Vector3, v3: THREE.Vector3,
    cell: HexCell, n1: HexCell, n2: HexCell,
    c1: THREE.Color, c2: THREE.Color, c3: THREE.Color
  ): void {
    const avgColor = new THREE.Color(
      (c1.r + c2.r + c3.r) / 3,
      (c1.g + c2.g + c3.g) / 3,
      (c1.b + c2.b + c3.b) / 3
    );

    // Calculate triangle normal to ensure correct winding (should face generally upward)
    const edge1 = new THREE.Vector3().subVectors(v2, v1);
    const edge2 = new THREE.Vector3().subVectors(v3, v1);
    const normal = new THREE.Vector3().crossVectors(edge1, edge2);

    // If normal points downward, reverse winding order
    if (normal.y < 0) {
      this.addTriangle(v1, v3, v2, avgColor);
    } else {
      this.addTriangle(v1, v2, v3, avgColor);
    }
  }

  /**
   * Add a single vertex with current splat state.
   */
  private addVertex(v: THREE.Vector3, color: THREE.Color): void {
    this.vertices.push(v.x, v.y, v.z);
    this.colors.push(color.r, color.g, color.b);
    this.terrainTypes.push(getTerrainTypeIndex(this.currentTerrainType));
    // Splat colors
    this.color1.push(this.currentColor1.r, this.currentColor1.g, this.currentColor1.b);
    this.color2.push(this.currentColor2.r, this.currentColor2.g, this.currentColor2.b);
    this.color3.push(this.currentColor3.r, this.currentColor3.g, this.currentColor3.b);
    this.splatWeights.push(this.currentWeights.x, this.currentWeights.y, this.currentWeights.z);
    this.vertexIndex++;
  }

  /**
   * Add a triangle (for walls - no blending).
   */
  private addTriangle(
    v1: THREE.Vector3,
    v2: THREE.Vector3,
    v3: THREE.Vector3,
    color: THREE.Color
  ): void {
    // Walls don't blend - 100% main color
    this.currentColor1.copy(color);
    this.currentColor2.copy(color);
    this.currentColor3.copy(color);
    this.currentWeights.set(1, 0, 0);

    this.addVertex(v1, color);
    this.addVertex(v2, color);
    this.addVertex(v3, color);

    this.indices.push(this.vertexIndex - 3, this.vertexIndex - 2, this.vertexIndex - 1);
  }

  /**
   * Create the final BufferGeometry.
   */
  build(): THREE.BufferGeometry {
    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', new THREE.Float32BufferAttribute(this.vertices, 3));
    geometry.setAttribute('color', new THREE.Float32BufferAttribute(this.colors, 3));
    // For terrain shader compatibility
    geometry.setAttribute('terrainColor', new THREE.Float32BufferAttribute(this.colors, 3));
    geometry.setAttribute('terrainType', new THREE.Float32BufferAttribute(this.terrainTypes, 1));
    // Splat map blending - 3 colors + weights
    geometry.setAttribute('splatColor1', new THREE.Float32BufferAttribute(this.color1, 3));
    geometry.setAttribute('splatColor2', new THREE.Float32BufferAttribute(this.color2, 3));
    geometry.setAttribute('splatColor3', new THREE.Float32BufferAttribute(this.color3, 3));
    geometry.setAttribute('splatWeights', new THREE.Float32BufferAttribute(this.splatWeights, 3));
    geometry.setIndex(this.indices);
    geometry.computeVertexNormals();
    return geometry;
  }
}
