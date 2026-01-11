import * as THREE from 'three';
import { HexGrid } from '../core/HexGrid';
import { HexCoordinates } from '../core/HexCoordinates';
import { HexMetrics } from '../core/HexMetrics';
import { HexDirection, opposite } from '../core/HexDirection';
import { LODDistances } from './LODHexBuilder';

/**
 * Renders rivers as edge-based meshes with animated flow.
 * Rivers follow hex boundaries, tracing along edges between hexes.
 */
export class EdgeRiverRenderer {
  private mesh: THREE.Mesh | null = null;
  private scene: THREE.Scene;
  private grid: HexGrid;
  private time = 0;

  // River width relative to hex edge
  private static readonly RIVER_WIDTH = 0.15;
  // Height offset above terrain to avoid z-fighting
  private static readonly HEIGHT_OFFSET = 0.02;

  private uniforms = {
    uTime: { value: 0 },
    uRiverColor: { value: new THREE.Color(0x2d8bc9) },
    uRiverColorDeep: { value: new THREE.Color(0x1a5c8e) },
    uFlowSpeed: { value: 1.5 },
  };

  /**
   * Maps HexDirection to the corner indices that form that edge.
   * Corners are at angles 30°, 90°, 150°, 210°, 270°, 330° (indices 0-5).
   *
   * Corner layout (looking down at XZ plane, +Z is forward):
   *       1 (90°, +Z)
   *      / \
   *   2 /   \ 0 (30°, +X,+Z)
   *    |     |
   *   3 \   / 5 (330°, +X,-Z)
   *      \ /
   *       4 (270°, -Z)
   *
   * Edge indices are determined by which direction they face:
   * Edge 0 faces 60° (toward +X,+Z), Edge 5 faces 0° (toward +X), etc.
   *
   * HexDirection world angles (calculated from toWorldPosition):
   * - NE [1,0,-1]: angle 0° → edge 5
   * - E [1,-1,0]: angle -60° (300°) → edge 4
   * - SE [0,-1,1]: angle -120° (240°) → edge 3
   * - SW [-1,0,1]: angle 180° → edge 2
   * - W [-1,1,0]: angle 120° → edge 1
   * - NW [0,1,-1]: angle 60° → edge 0
   */
  private static readonly DIRECTION_TO_CORNERS: Record<HexDirection, [number, number]> = {
    [HexDirection.NE]: [5, 0],  // edge 5: corners 5→0 (faces 0°, +X direction)
    [HexDirection.E]: [4, 5],   // edge 4: corners 4→5 (faces 300°, +X,-Z direction)
    [HexDirection.SE]: [3, 4],  // edge 3: corners 3→4 (faces 240°, -X,-Z direction)
    [HexDirection.SW]: [2, 3],  // edge 2: corners 2→3 (faces 180°, -X direction)
    [HexDirection.W]: [1, 2],   // edge 1: corners 1→2 (faces 120°, -X,+Z direction)
    [HexDirection.NW]: [0, 1],  // edge 0: corners 0→1 (faces 60°, +X,+Z direction)
  };

  constructor(scene: THREE.Scene, grid: HexGrid) {
    this.scene = scene;
    this.grid = grid;
  }

  /**
   * Build the river mesh from grid river data.
   * Rivers are drawn as quads along hex edges, with connecting segments
   * that trace around hex boundaries when rivers pass through.
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

    // Build a map of incoming river directions for each cell
    const incomingRivers = new Map<string, HexDirection[]>();
    for (const cell of this.grid.getAllCells()) {
      for (const dir of cell.riverDirections) {
        const neighbor = this.grid.getNeighbor(cell, dir);
        if (neighbor) {
          const key = `${neighbor.q},${neighbor.r}`;
          if (!incomingRivers.has(key)) {
            incomingRivers.set(key, []);
          }
          incomingRivers.get(key)!.push(opposite(dir));
        }
      }
    }

    // Track rendered edges to avoid duplicates
    const renderedEdges = new Set<string>();

    // Process each cell that has rivers
    for (const cell of this.grid.getAllCells()) {
      const outgoing = cell.riverDirections;
      const incoming = incomingRivers.get(`${cell.q},${cell.r}`) || [];

      if (outgoing.length === 0 && incoming.length === 0) continue;

      const coords = new HexCoordinates(cell.q, cell.r);
      const centerPos = coords.toWorldPosition(cell.elevation);

      // Get world positions of all 6 corners for this cell
      const worldCorners: THREE.Vector3[] = corners.map(c =>
        new THREE.Vector3(centerPos.x + c.x, 0, centerPos.z + c.z)
      );

      // For each outgoing river, draw the edge quad
      for (const outDir of outgoing) {
        const neighbor = this.grid.getNeighbor(cell, outDir);
        if (!neighbor) continue;

        const edgeKey = this.getEdgeKey(cell.q, cell.r, neighbor.q, neighbor.r);
        if (renderedEdges.has(edgeKey)) continue;
        renderedEdges.add(edgeKey);

        // Get the corners for this edge
        const [c1Idx, c2Idx] = EdgeRiverRenderer.DIRECTION_TO_CORNERS[outDir];
        const c1 = worldCorners[c1Idx];
        const c2 = worldCorners[c2Idx];

        // Calculate edge midpoint and perpendicular
        const midX = (c1.x + c2.x) / 2;
        const midZ = (c1.z + c2.z) / 2;
        const edgeDX = c2.x - c1.x;
        const edgeDZ = c2.z - c1.z;
        const edgeLen = Math.sqrt(edgeDX * edgeDX + edgeDZ * edgeDZ);
        const perpX = -edgeDZ / edgeLen * halfWidth;
        const perpZ = edgeDX / edgeLen * halfWidth;

        // Y positions for both cells
        const highY = Math.max(cell.elevation, neighbor.elevation) * HexMetrics.elevationStep
          + EdgeRiverRenderer.HEIGHT_OFFSET;
        const lowY = Math.min(cell.elevation, neighbor.elevation) * HexMetrics.elevationStep
          + EdgeRiverRenderer.HEIGHT_OFFSET;
        const elevationDiff = cell.elevation - neighbor.elevation;

        // Draw horizontal quad along the edge (at higher elevation)
        vertices.push(c1.x - perpX, highY, c1.z - perpZ);
        vertices.push(c1.x + perpX, highY, c1.z + perpZ);
        vertices.push(c2.x + perpX, highY, c2.z + perpZ);
        vertices.push(c2.x - perpX, highY, c2.z - perpZ);

        uvs.push(0, 0);
        uvs.push(1, 0);
        uvs.push(1, 1);
        uvs.push(0, 1);

        indices.push(vertexIndex, vertexIndex + 1, vertexIndex + 2);
        indices.push(vertexIndex, vertexIndex + 2, vertexIndex + 3);
        vertexIndex += 4;

        // If river flows downhill, draw waterfall connecting upper river to lower river
        // Waterfall only exists where river continues below (neighbor has river or is water)
        if (elevationDiff > 0) {
          const neighborHasRiver = neighbor.riverDirections.length > 0 ||
                                   neighbor.elevation < HexMetrics.waterLevel;

          if (neighborHasRiver) {
            // Waterfall goes down the vertical wall edge at corner c2
            // Width along the edge direction so it sits on the wall face
            const edgeNormX = edgeDX / edgeLen * halfWidth;
            const edgeNormZ = edgeDZ / edgeLen * halfWidth;

            // Draw waterfall quad on the vertical edge at c2
            vertices.push(c2.x - edgeNormX, highY, c2.z - edgeNormZ);  // top (toward c1)
            vertices.push(c2.x + edgeNormX, highY, c2.z + edgeNormZ);  // top (away from c1)
            vertices.push(c2.x + edgeNormX, lowY, c2.z + edgeNormZ);   // bottom (away from c1)
            vertices.push(c2.x - edgeNormX, lowY, c2.z - edgeNormZ);   // bottom (toward c1)

            // UVs for waterfall - V goes from top to bottom for downward flow
            uvs.push(0, 0);
            uvs.push(1, 0);
            uvs.push(1, 1);
            uvs.push(0, 1);

            indices.push(vertexIndex, vertexIndex + 1, vertexIndex + 2);
            indices.push(vertexIndex, vertexIndex + 2, vertexIndex + 3);
            vertexIndex += 4;
          }
        }
      }

      // If this cell has both incoming and outgoing rivers, draw connecting path
      // along the hex boundary from incoming edge to outgoing edge
      if (incoming.length > 0 && outgoing.length > 0) {
        for (const inDir of incoming) {
          for (const outDir of outgoing) {
            // Get corners for incoming and outgoing edges
            const [inC1, inC2] = EdgeRiverRenderer.DIRECTION_TO_CORNERS[inDir];
            const [outC1, outC2] = EdgeRiverRenderer.DIRECTION_TO_CORNERS[outDir];

            // Find path from incoming edge START (inC1, where waterfall ends)
            // to outgoing edge START (outC1, where next edge begins)
            // This ensures the path connects waterfall to the next river segment
            const pathCorners = this.findCornerPath(inC1, outC1);

            if (pathCorners.length >= 2) {
              const y = cell.elevation * HexMetrics.elevationStep + EdgeRiverRenderer.HEIGHT_OFFSET;

              // Draw quads along the corner path
              for (let i = 0; i < pathCorners.length - 1; i++) {
                const cIdx1 = pathCorners[i];
                const cIdx2 = pathCorners[i + 1];
                const p1 = worldCorners[cIdx1];
                const p2 = worldCorners[cIdx2];

                // Calculate perpendicular for this segment
                const segDX = p2.x - p1.x;
                const segDZ = p2.z - p1.z;
                const segLen = Math.sqrt(segDX * segDX + segDZ * segDZ);
                if (segLen < 0.001) continue;

                const sPerpX = -segDZ / segLen * halfWidth;
                const sPerpZ = segDX / segLen * halfWidth;

                vertices.push(p1.x - sPerpX, y, p1.z - sPerpZ);
                vertices.push(p1.x + sPerpX, y, p1.z + sPerpZ);
                vertices.push(p2.x + sPerpX, y, p2.z + sPerpZ);
                vertices.push(p2.x - sPerpX, y, p2.z - sPerpZ);

                uvs.push(0, 0);
                uvs.push(1, 0);
                uvs.push(1, 1);
                uvs.push(0, 1);

                indices.push(vertexIndex, vertexIndex + 1, vertexIndex + 2);
                indices.push(vertexIndex, vertexIndex + 2, vertexIndex + 3);
                vertexIndex += 4;
              }
            }
          }
        }
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
   * Find the shortest path of corner indices from start to end,
   * going around the hex boundary (clockwise or counterclockwise).
   */
  private findCornerPath(startCorner: number, endCorner: number): number[] {
    if (startCorner === endCorner) {
      return [startCorner];
    }

    // Try clockwise path
    const cwPath: number[] = [startCorner];
    let current = startCorner;
    while (current !== endCorner) {
      current = (current + 1) % 6;
      cwPath.push(current);
      if (cwPath.length > 6) break; // Safety
    }

    // Try counterclockwise path
    const ccwPath: number[] = [startCorner];
    current = startCorner;
    while (current !== endCorner) {
      current = (current + 5) % 6; // -1 mod 6
      ccwPath.push(current);
      if (ccwPath.length > 6) break; // Safety
    }

    // Return shorter path
    return cwPath.length <= ccwPath.length ? cwPath : ccwPath;
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
