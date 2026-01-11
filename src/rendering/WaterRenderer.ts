import * as THREE from 'three';
import { HexGrid } from '../core/HexGrid';
import { HexCoordinates } from '../core/HexCoordinates';
import { HexMetrics } from '../core/HexMetrics';
import { LODDistances } from './LODHexBuilder';

/**
 * Renders water surface with animation.
 */
export class WaterRenderer {
  private mesh: THREE.Mesh | null = null;
  private scene: THREE.Scene;
  private grid: HexGrid;
  private time = 0;

  // Water shader uniforms
  private uniforms = {
    uTime: { value: 0 },
    uWaterColor: { value: new THREE.Color(0x1a4c6e) },
    uWaterColorShallow: { value: new THREE.Color(0x2d8bc9) },
  };

  constructor(scene: THREE.Scene, grid: HexGrid) {
    this.scene = scene;
    this.grid = grid;
  }

  /**
   * Build the water mesh.
   */
  build(): void {
    // Remove existing mesh
    if (this.mesh) {
      this.scene.remove(this.mesh);
      this.mesh.geometry.dispose();
      (this.mesh.material as THREE.Material).dispose();
    }

    // Build water geometry from underwater cells
    const vertices: number[] = [];
    const indices: number[] = [];
    let vertexIndex = 0;

    for (const cell of this.grid.getAllCells()) {
      // Only render water for underwater cells
      if (cell.elevation < HexMetrics.waterLevel) {
        const coords = new HexCoordinates(cell.q, cell.r);
        const center = coords.toWorldPosition(0); // Water at elevation 0
        center.y = -0.05; // Slightly below land level to avoid z-fighting

        const corners = HexMetrics.getCorners();

        // Create hex face
        for (let i = 0; i < 6; i++) {
          const c1 = corners[i];
          const c2 = corners[(i + 1) % 6];

          vertices.push(center.x, center.y, center.z);
          vertices.push(center.x + c1.x, center.y, center.z + c1.z);
          vertices.push(center.x + c2.x, center.y, center.z + c2.z);

          indices.push(vertexIndex, vertexIndex + 1, vertexIndex + 2);
          vertexIndex += 3;
        }
      }
    }

    if (vertices.length === 0) return;

    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', new THREE.Float32BufferAttribute(vertices, 3));
    geometry.setIndex(indices);
    geometry.computeVertexNormals();

    // Water material with transparency
    const material = new THREE.ShaderMaterial({
      uniforms: this.uniforms,
      vertexShader: `
        uniform float uTime;
        varying vec3 vWorldPosition;

        void main() {
          vWorldPosition = (modelMatrix * vec4(position, 1.0)).xyz;

          // Subtle wave animation
          vec3 pos = position;
          pos.y += sin(pos.x * 2.0 + uTime) * 0.02;
          pos.y += sin(pos.z * 2.0 + uTime * 0.8) * 0.02;

          gl_Position = projectionMatrix * modelViewMatrix * vec4(pos, 1.0);
        }
      `,
      fragmentShader: `
        uniform vec3 uWaterColor;
        uniform vec3 uWaterColorShallow;
        varying vec3 vWorldPosition;

        void main() {
          // Simple gradient based on position for visual interest
          float gradient = sin(vWorldPosition.x * 0.1) * 0.5 + 0.5;
          vec3 color = mix(uWaterColor, uWaterColorShallow, gradient * 0.3);

          gl_FragColor = vec4(color, 0.85);
        }
      `,
      transparent: true,
      side: THREE.DoubleSide,
    });

    this.mesh = new THREE.Mesh(geometry, material);
    this.scene.add(this.mesh);
  }

  /**
   * Update water animation and visibility.
   */
  update(deltaTime: number, camera?: THREE.Camera): void {
    this.time += deltaTime;
    this.uniforms.uTime.value = this.time;

    // Hide water when walls start disappearing (medium LOD)
    if (camera && this.mesh) {
      const cameraHeight = camera.position.y;
      this.mesh.visible = cameraHeight < LODDistances.highToMedium * 1.2;
    }
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
