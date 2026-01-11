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

    // Get hex corners for edge calculations
    const corners = HexMetrics.getCorners();

    // Track which edges have been rendered to avoid duplicates
    const renderedEdges = new Set<string>();

    for (const cell of this.grid.getAllCells()) {
      if (cell.riverDirections.length === 0) continue;

      const coords = new HexCoordinates(cell.q, cell.r);
      const centerPos = coords.toWorldPosition(cell.elevation);
      const baseY = centerPos.y + EdgeRiverRenderer.HEIGHT_OFFSET;

      for (const direction of cell.riverDirections) {
        // Create unique edge key to avoid duplicate rendering
        const neighbor = this.grid.getNeighbor(cell, direction);
        if (!neighbor) continue;

        const edgeKey = this.getEdgeKey(cell.q, cell.r, neighbor.q, neighbor.r);
        if (renderedEdges.has(edgeKey)) continue;
        renderedEdges.add(edgeKey);

        // Get edge corners based on direction
        // Direction maps to edge between corners[dir] and corners[(dir+1)%6]
        // But we need to map HexDirection to the correct edge index
        const edgeIndex = this.directionToEdgeIndex(direction);
        const corner1 = corners[edgeIndex];
        const corner2 = corners[(edgeIndex + 1) % 6];

        // Calculate edge midpoint and perpendicular for width
        const edgeMidX = centerPos.x + (corner1.x + corner2.x) / 2;
        const edgeMidZ = centerPos.z + (corner1.z + corner2.z) / 2;

        // Edge direction vector
        const edgeDX = corner2.x - corner1.x;
        const edgeDZ = corner2.z - corner1.z;
        const edgeLen = Math.sqrt(edgeDX * edgeDX + edgeDZ * edgeDZ);

        // Perpendicular vector (for river width)
        const perpX = -edgeDZ / edgeLen;
        const perpZ = edgeDX / edgeLen;

        // River quad vertices (along the edge, with width perpendicular)
        const halfWidth = EdgeRiverRenderer.RIVER_WIDTH;
        const halfLength = edgeLen * 0.4; // Cover 80% of edge

        // Calculate neighbor elevation for smooth transition
        const neighborCoords = new HexCoordinates(neighbor.q, neighbor.r);
        const neighborPos = neighborCoords.toWorldPosition(neighbor.elevation);
        const neighborY = neighborPos.y + EdgeRiverRenderer.HEIGHT_OFFSET;

        // Use average Y for the river surface at the edge
        const avgY = (baseY + neighborY) / 2;

        // Edge direction (along the edge line)
        const edgeNormX = edgeDX / edgeLen;
        const edgeNormZ = edgeDZ / edgeLen;

        // Four corners of the river quad
        // v0: corner1 side, -width
        // v1: corner1 side, +width
        // v2: corner2 side, +width
        // v3: corner2 side, -width
        const v0x = edgeMidX - edgeNormX * halfLength - perpX * halfWidth;
        const v0z = edgeMidZ - edgeNormZ * halfLength - perpZ * halfWidth;
        const v1x = edgeMidX - edgeNormX * halfLength + perpX * halfWidth;
        const v1z = edgeMidZ - edgeNormZ * halfLength + perpZ * halfWidth;
        const v2x = edgeMidX + edgeNormX * halfLength + perpX * halfWidth;
        const v2z = edgeMidZ + edgeNormZ * halfLength + perpZ * halfWidth;
        const v3x = edgeMidX + edgeNormX * halfLength - perpX * halfWidth;
        const v3z = edgeMidZ + edgeNormZ * halfLength - perpZ * halfWidth;

        // Add vertices
        vertices.push(v0x, avgY, v0z);
        vertices.push(v1x, avgY, v1z);
        vertices.push(v2x, avgY, v2z);
        vertices.push(v3x, avgY, v3z);

        // UV coordinates for flow animation
        // V coordinate along flow direction
        uvs.push(0, 0);
        uvs.push(1, 0);
        uvs.push(1, 1);
        uvs.push(0, 1);

        // Two triangles for quad
        indices.push(vertexIndex, vertexIndex + 1, vertexIndex + 2);
        indices.push(vertexIndex, vertexIndex + 2, vertexIndex + 3);
        vertexIndex += 4;
      }
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
