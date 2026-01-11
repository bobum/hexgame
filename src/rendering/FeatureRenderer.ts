import * as THREE from 'three';
import { mergeGeometries } from 'three/examples/jsm/utils/BufferGeometryUtils.js';
import { HexGrid } from '../core/HexGrid';
import { FeatureType, Feature } from '../types';
import { LODDistances } from './LODHexBuilder';

/**
 * Renders instanced features (trees, rocks) on hex cells.
 */
export class FeatureRenderer {
  private scene: THREE.Scene;
  private grid: HexGrid;

  private treeMesh: THREE.InstancedMesh | null = null;
  private rockMesh: THREE.InstancedMesh | null = null;

  // Shared geometries
  private treeGeometry: THREE.BufferGeometry;
  private rockGeometry: THREE.BufferGeometry;

  // Materials
  private treeMaterial: THREE.MeshLambertMaterial;
  private rockMaterial: THREE.MeshLambertMaterial;

  constructor(scene: THREE.Scene, grid: HexGrid) {
    this.scene = scene;
    this.grid = grid;

    // Create tree geometry (cone + cylinder trunk)
    this.treeGeometry = this.createTreeGeometry();

    // Create rock geometry (irregular icosahedron)
    this.rockGeometry = this.createRockGeometry();

    // Materials
    this.treeMaterial = new THREE.MeshLambertMaterial({
      color: 0x228b22,
      flatShading: true,
    });

    this.rockMaterial = new THREE.MeshLambertMaterial({
      color: 0x696969,
      flatShading: true,
    });
  }

  /**
   * Create simple tree geometry (cone for foliage + trunk).
   */
  private createTreeGeometry(): THREE.BufferGeometry {
    // Simple cone tree - positioned above the ground
    const cone = new THREE.ConeGeometry(0.15, 0.4, 6);
    cone.translate(0, 0.3, 0);

    // Small trunk
    const trunk = new THREE.CylinderGeometry(0.03, 0.04, 0.15, 6);
    trunk.translate(0, 0.075, 0);

    // Properly merge geometries using Three.js utility
    const merged = mergeGeometries([cone, trunk]);

    cone.dispose();
    trunk.dispose();

    if (!merged) {
      // Fallback to just a cone if merge fails
      return new THREE.ConeGeometry(0.15, 0.5, 6).translate(0, 0.25, 0);
    }

    merged.computeVertexNormals();
    return merged;
  }

  /**
   * Create rock geometry (deformed icosahedron).
   */
  private createRockGeometry(): THREE.BufferGeometry {
    const geometry = new THREE.IcosahedronGeometry(0.12, 0);

    // Deform vertices for organic look - use consistent seed based on position
    const positions = geometry.getAttribute('position');
    for (let i = 0; i < positions.count; i++) {
      const x = positions.getX(i);
      const y = positions.getY(i);
      const z = positions.getZ(i);

      // Deterministic displacement based on position
      const noise = Math.sin(x * 10) * Math.cos(z * 10) * 0.3 + 1;
      // Flatten the bottom half to sit on ground
      const yScale = y < 0 ? 0.3 : 1;
      positions.setXYZ(i, x * noise, y * noise * yScale, z * noise);
    }

    // Position so bottom sits at y=0
    geometry.translate(0, 0.08, 0);
    geometry.computeVertexNormals();

    return geometry;
  }

  /**
   * Build instanced meshes for all features.
   */
  build(): void {
    this.dispose();

    // Collect all features
    const trees: Feature[] = [];
    const rocks: Feature[] = [];

    for (const cell of this.grid.getAllCells()) {
      for (const feature of cell.features) {
        if (feature.type === FeatureType.Tree) {
          trees.push(feature);
        } else if (feature.type === FeatureType.Rock) {
          rocks.push(feature);
        }
      }
    }

    // Create tree instances
    if (trees.length > 0) {
      this.treeMesh = new THREE.InstancedMesh(
        this.treeGeometry,
        this.treeMaterial,
        trees.length
      );

      const matrix = new THREE.Matrix4();
      const position = new THREE.Vector3();
      const rotation = new THREE.Quaternion();
      const scale = new THREE.Vector3();

      trees.forEach((tree, i) => {
        position.copy(tree.position);
        rotation.setFromAxisAngle(new THREE.Vector3(0, 1, 0), tree.rotation);
        scale.setScalar(tree.scale);

        matrix.compose(position, rotation, scale);
        this.treeMesh!.setMatrixAt(i, matrix);

        // Vary tree color slightly
        const color = new THREE.Color(0x228b22);
        color.offsetHSL(0, 0, (Math.random() - 0.5) * 0.1);
        this.treeMesh!.setColorAt(i, color);
      });

      this.treeMesh.instanceMatrix.needsUpdate = true;
      if (this.treeMesh.instanceColor) {
        this.treeMesh.instanceColor.needsUpdate = true;
      }
      this.treeMesh.castShadow = true;
      this.treeMesh.receiveShadow = true;

      this.scene.add(this.treeMesh);
    }

    // Create rock instances
    if (rocks.length > 0) {
      this.rockMesh = new THREE.InstancedMesh(
        this.rockGeometry,
        this.rockMaterial,
        rocks.length
      );

      const matrix = new THREE.Matrix4();
      const position = new THREE.Vector3();
      const rotation = new THREE.Quaternion();
      const scale = new THREE.Vector3();

      rocks.forEach((rock, i) => {
        position.copy(rock.position);
        rotation.setFromAxisAngle(new THREE.Vector3(0, 1, 0), rock.rotation);
        scale.setScalar(rock.scale);

        matrix.compose(position, rotation, scale);
        this.rockMesh!.setMatrixAt(i, matrix);

        // Vary rock color
        const color = new THREE.Color(0x696969);
        color.offsetHSL(0, 0, (Math.random() - 0.5) * 0.15);
        this.rockMesh!.setColorAt(i, color);
      });

      this.rockMesh.instanceMatrix.needsUpdate = true;
      if (this.rockMesh.instanceColor) {
        this.rockMesh.instanceColor.needsUpdate = true;
      }
      this.rockMesh.castShadow = true;
      this.rockMesh.receiveShadow = true;

      this.scene.add(this.rockMesh);
    }
  }

  /**
   * Update feature visibility based on camera distance.
   * Hides features when camera is zoomed out past medium LOD threshold.
   * @param cameraDistance - Orbital distance from camera to target (zoom level)
   */
  update(cameraDistance: number): void {
    // Hide features when camera is zoomed out past medium LOD threshold
    const showFeatures = cameraDistance < LODDistances.highToMedium * 1.5;

    if (this.treeMesh) {
      this.treeMesh.visible = showFeatures;
    }
    if (this.rockMesh) {
      this.rockMesh.visible = showFeatures;
    }
  }

  /**
   * Dispose of resources.
   */
  dispose(): void {
    if (this.treeMesh) {
      this.scene.remove(this.treeMesh);
      this.treeMesh.dispose();
      this.treeMesh = null;
    }

    if (this.rockMesh) {
      this.scene.remove(this.rockMesh);
      this.rockMesh.dispose();
      this.rockMesh = null;
    }
  }

  /**
   * Full cleanup including shared resources.
   */
  disposeAll(): void {
    this.dispose();
    this.treeGeometry.dispose();
    this.rockGeometry.dispose();
    this.treeMaterial.dispose();
    this.rockMaterial.dispose();
  }
}
