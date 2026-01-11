import * as THREE from 'three';
import { HexGrid } from '../core/HexGrid';
import { HexCoordinates } from '../core/HexCoordinates';
import { HexMetrics } from '../core/HexMetrics';
import { HexDirection } from '../core/HexDirection';
import { LODDistances } from './LODHexBuilder';

/**
 * Renders rivers as edge-based meshes with animated flow.
 * Rivers are rendered as quad strips along hex edges.
 */
export class EdgeRiverRenderer {
  private mesh: THREE.Mesh | null = null;
  private scene: THREE.Scene;
  private grid: HexGrid;
  private time = 0;

  // River width relative to hex edge
  private static readonly RIVER_WIDTH = 0.25;
  // Height offset above terrain to avoid z-fighting
  private static readonly HEIGHT_OFFSET = 0.02;

  private uniforms = {
    uTime: { value: 0 },
    uRiverColor: { value: new THREE.Color(0x2d8bc9) },
    uRiverColorDeep: { value: new THREE.Color(0x1a5c8e) },
    uFlowSpeed: { value: 1.5 },
  };

  constructor(scene: THREE.Scene, grid: HexGrid) {
    this.scene = scene;
    this.grid = grid;
  }

  /**
   * Build the river mesh from grid river data.
   */
  build(): void {
    // Remove existing mesh
    if (this.mesh) {
      this.scene.remove(this.mesh);
      this.mesh.geometry.dispose();
      (this.mesh.material as THREE.Material).dispose();
    }

    const vertices: number[] = [];
    const uvs: number[] = [];
    const indices: number[] = [];
    let vertexIndex = 0;

    // Find river sources (cells with outgoing rivers but no incoming)
    const hasIncoming = new Set<string>();
    for (const cell of this.grid.getAllCells()) {
      for (const dir of cell.riverDirections) {
        const neighbor = this.grid.getNeighbor(cell, dir);
        if (neighbor) {
          hasIncoming.add(`${neighbor.q},${neighbor.r}`);
        }
      }
    }

    // Find sources
    const sources = this.grid.getAllCells().filter(cell =>
      cell.riverDirections.length > 0 && !hasIncoming.has(`${cell.q},${cell.r}`)
    );

    // Track rendered paths to avoid duplicates
    const renderedCells = new Set<string>();

    // Trace and render each river path
    for (const source of sources) {
      const points = this.traceRiverPath(source);
      if (points.length < 2) continue;

      // Build ONE continuous quad strip for entire river
      const result = this.buildRiverStrip(points, vertexIndex, renderedCells);
      vertices.push(...result.vertices);
      uvs.push(...result.uvs);
      indices.push(...result.indices);
      vertexIndex = result.nextVertexIndex;
    }

    if (vertices.length === 0) return;

    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', new THREE.Float32BufferAttribute(vertices, 3));
    geometry.setAttribute('uv', new THREE.Float32BufferAttribute(uvs, 2));
    geometry.setIndex(indices);
    geometry.computeVertexNormals();

    // River material with animated flow
    const material = new THREE.ShaderMaterial({
      uniforms: THREE.UniformsUtils.merge([
        THREE.UniformsLib.fog,
        this.uniforms,
      ]),
      vertexShader: `
        #include <fog_pars_vertex>

        varying vec2 vUv;
        varying vec3 vWorldPosition;

        void main() {
          vUv = uv;
          vWorldPosition = (modelMatrix * vec4(position, 1.0)).xyz;

          vec4 mvPosition = modelViewMatrix * vec4(position, 1.0);
          gl_Position = projectionMatrix * mvPosition;

          #include <fog_vertex>
        }
      `,
      fragmentShader: `
        #include <fog_pars_fragment>

        uniform float uTime;
        uniform vec3 uRiverColor;
        uniform vec3 uRiverColorDeep;
        uniform float uFlowSpeed;

        varying vec2 vUv;
        varying vec3 vWorldPosition;

        // Simple noise function for water shimmer
        float noise(vec2 p) {
          return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
        }

        void main() {
          // Flow animation - scroll UV over time
          vec2 flowUv = vUv;
          flowUv.y -= uTime * uFlowSpeed;

          // Create ripple effect using noise
          float ripple = noise(flowUv * 10.0 + uTime);
          ripple += noise(flowUv * 5.0 - uTime * 0.5) * 0.5;
          ripple = ripple * 0.15;

          // Color variation based on position
          float colorMix = sin(vWorldPosition.x * 0.5 + vWorldPosition.z * 0.3) * 0.5 + 0.5;
          vec3 baseColor = mix(uRiverColor, uRiverColorDeep, colorMix * 0.3);

          // Add ripple brightness variation
          vec3 color = baseColor + vec3(ripple * 0.2);

          // Edge fade for smoother blending
          float edgeFade = smoothstep(0.0, 0.15, vUv.x) * smoothstep(1.0, 0.85, vUv.x);

          gl_FragColor = vec4(color, 0.85 * edgeFade);

          #include <fog_fragment>
        }
      `,
      transparent: true,
      side: THREE.DoubleSide,
      fog: true,
    });

    this.mesh = new THREE.Mesh(geometry, material);
    this.mesh.name = 'rivers';
    this.scene.add(this.mesh);
  }

  /**
   * Trace a river path and get edge MIDPOINTS only.
   * This creates a simple path through the centers of river edges.
   */
  private traceRiverPath(source: { q: number; r: number; elevation: number; riverDirections: HexDirection[] }): Array<{
    x: number; y: number; z: number;
  }> {
    const points: Array<{ x: number; y: number; z: number }> = [];
    let current: { q: number; r: number; elevation: number; riverDirections: HexDirection[] } | undefined = source;
    const visited = new Set<string>();
    const corners = HexMetrics.getCorners();

    while (current && current.riverDirections.length > 0) {
      const key = `${current.q},${current.r}`;
      if (visited.has(key)) break;
      visited.add(key);

      const coords = new HexCoordinates(current.q, current.r);
      const centerPos = coords.toWorldPosition(current.elevation);

      const dir = current.riverDirections[0];
      const edgeIndex = dir as number;
      const c1 = corners[edgeIndex];
      const c2 = corners[(edgeIndex + 1) % 6];

      // Edge midpoint
      const midX = centerPos.x + (c1.x + c2.x) / 2;
      const midZ = centerPos.z + (c1.z + c2.z) / 2;

      // Get neighbor for Y averaging
      const neighbor = this.grid.getNeighbor(current as any, dir);
      let avgY = centerPos.y + EdgeRiverRenderer.HEIGHT_OFFSET;
      if (neighbor) {
        const neighborCoords = new HexCoordinates(neighbor.q, neighbor.r);
        const neighborPos = neighborCoords.toWorldPosition(neighbor.elevation);
        avgY = (centerPos.y + neighborPos.y) / 2 + EdgeRiverRenderer.HEIGHT_OFFSET;
      }

      points.push({ x: midX, y: avgY, z: midZ });

      if (!neighbor) break;
      current = neighbor;
    }

    return points;
  }

  /**
   * Build ONE continuous quad strip connecting edge midpoints.
   * Perpendicular is calculated based on direction to next/prev point.
   */
  private buildRiverStrip(
    points: Array<{ x: number; y: number; z: number }>,
    startIndex: number,
    renderedCells: Set<string>
  ): { vertices: number[]; uvs: number[]; indices: number[]; nextVertexIndex: number } {
    const vertices: number[] = [];
    const uvs: number[] = [];
    const indices: number[] = [];
    let vertexIndex = startIndex;

    if (points.length < 2) {
      return { vertices, uvs, indices, nextVertexIndex: vertexIndex };
    }

    const halfWidth = EdgeRiverRenderer.RIVER_WIDTH;

    // Build continuous quad strip
    for (let i = 0; i < points.length; i++) {
      const p = points[i];

      // Calculate direction based on neighbors
      let dirX: number, dirZ: number;
      if (i === 0) {
        // First point: direction to next
        dirX = points[1].x - p.x;
        dirZ = points[1].z - p.z;
      } else if (i === points.length - 1) {
        // Last point: direction from prev
        dirX = p.x - points[i - 1].x;
        dirZ = p.z - points[i - 1].z;
      } else {
        // Middle: average of prev→current and current→next
        dirX = points[i + 1].x - points[i - 1].x;
        dirZ = points[i + 1].z - points[i - 1].z;
      }

      // Normalize
      const len = Math.sqrt(dirX * dirX + dirZ * dirZ);
      if (len > 0) {
        dirX /= len;
        dirZ /= len;
      } else {
        dirX = 1;
        dirZ = 0;
      }

      // Perpendicular
      const perpX = -dirZ;
      const perpZ = dirX;

      // Two vertices at this point (left and right side of river)
      vertices.push(p.x - perpX * halfWidth, p.y, p.z - perpZ * halfWidth);
      vertices.push(p.x + perpX * halfWidth, p.y, p.z + perpZ * halfWidth);

      // UV coordinates
      const v = i / (points.length - 1);
      uvs.push(0, v);
      uvs.push(1, v);

      // Create triangles connecting to previous pair
      if (i > 0) {
        const prev = vertexIndex - 2;
        const curr = vertexIndex;
        indices.push(prev, curr, prev + 1);
        indices.push(prev + 1, curr, curr + 1);
      }

      vertexIndex += 2;
    }

    return { vertices, uvs, indices, nextVertexIndex: vertexIndex };
  }

  /**
   * Map HexDirection to edge index (0-5).
   * Edges are between corners, so edge i is between corner i and corner i+1.
   */
  private directionToEdgeIndex(direction: HexDirection): number {
    // HexDirection enum: NE=0, E=1, SE=2, SW=3, W=4, NW=5
    // Edges face outward: edge 0 faces NE, edge 1 faces E, etc.
    // But corners are at 30°, 90°, 150°, etc.
    // Edge 0 (between corners 0 and 1) faces ~60° (between 30° and 90°)

    // This mapping depends on hex orientation. For flat-topped hexes:
    // Direction NE (0) corresponds to edge 0
    // Direction E (1) corresponds to edge 1
    // etc.
    return direction;
  }

  /**
   * Create unique key for an edge between two cells.
   */
  private getEdgeKey(q1: number, r1: number, q2: number, r2: number): string {
    // Sort to ensure consistent key regardless of direction
    if (q1 < q2 || (q1 === q2 && r1 < r2)) {
      return `${q1},${r1}-${q2},${r2}`;
    }
    return `${q2},${r2}-${q1},${r1}`;
  }

  /**
   * Update river animation.
   */
  update(deltaTime: number, cameraDistance?: number): void {
    this.time += deltaTime;
    this.uniforms.uTime.value = this.time;

    // Hide rivers when camera is zoomed out past LOD threshold
    if (cameraDistance !== undefined && this.mesh) {
      this.mesh.visible = cameraDistance < LODDistances.highToMedium * 1.5;
    }
  }

  /**
   * Get uniforms for UI controls.
   */
  getUniforms() {
    return this.uniforms;
  }

  /**
   * Dispose of resources.
   */
  dispose(): void {
    if (this.mesh) {
      this.scene.remove(this.mesh);
      this.mesh.geometry.dispose();
      (this.mesh.material as THREE.Material).dispose();
      this.mesh = null;
    }
  }
}
