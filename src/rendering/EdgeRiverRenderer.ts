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
   * Simple approach: one quad per river edge, positioned exactly on the edge.
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

    const corners = HexMetrics.getCorners();
    const halfWidth = EdgeRiverRenderer.RIVER_WIDTH;

    // Track rendered edges to avoid duplicates
    const renderedEdges = new Set<string>();

    // For each cell with rivers, draw a quad on each river edge
    for (const cell of this.grid.getAllCells()) {
      if (cell.riverDirections.length === 0) continue;

      const coords = new HexCoordinates(cell.q, cell.r);
      const centerPos = coords.toWorldPosition(cell.elevation);

      for (const dir of cell.riverDirections) {
        // Get neighbor to create unique edge key
        const neighbor = this.grid.getNeighbor(cell, dir);
        if (!neighbor) continue;

        const edgeKey = this.getEdgeKey(cell.q, cell.r, neighbor.q, neighbor.r);
        if (renderedEdges.has(edgeKey)) continue;
        renderedEdges.add(edgeKey);

        // Get edge corners
        const edgeIndex = dir as number;
        const c1 = corners[edgeIndex];
        const c2 = corners[(edgeIndex + 1) % 6];

        // World positions of corners
        const corner1X = centerPos.x + c1.x;
        const corner1Z = centerPos.z + c1.z;
        const corner2X = centerPos.x + c2.x;
        const corner2Z = centerPos.z + c2.z;

        // Y position: use higher of the two cells to stay on top
        const y = Math.max(cell.elevation, neighbor.elevation) * HexMetrics.elevationStep + EdgeRiverRenderer.HEIGHT_OFFSET;

        // Edge direction and perpendicular
        const edgeDX = corner2X - corner1X;
        const edgeDZ = corner2Z - corner1Z;
        const edgeLen = Math.sqrt(edgeDX * edgeDX + edgeDZ * edgeDZ);
        const perpX = -edgeDZ / edgeLen * halfWidth;
        const perpZ = edgeDX / edgeLen * halfWidth;

        // Four vertices: two at each corner, offset perpendicular to edge
        vertices.push(corner1X - perpX, y, corner1Z - perpZ); // 0: corner1 left
        vertices.push(corner1X + perpX, y, corner1Z + perpZ); // 1: corner1 right
        vertices.push(corner2X + perpX, y, corner2Z + perpZ); // 2: corner2 right
        vertices.push(corner2X - perpX, y, corner2Z - perpZ); // 3: corner2 left

        // UVs
        uvs.push(0, 0);
        uvs.push(1, 0);
        uvs.push(1, 1);
        uvs.push(0, 1);

        // Two triangles
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
