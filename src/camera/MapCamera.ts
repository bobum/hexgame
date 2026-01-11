import * as THREE from 'three';
import { CameraConfig, defaultCameraConfig } from '../types';

/**
 * Orbital camera for the hex map with pan, zoom, and rotation.
 */
export class MapCamera {
  camera: THREE.PerspectiveCamera;
  private config: CameraConfig;

  // Camera state
  private target = new THREE.Vector3(0, 0, 0);
  private distance = 30;
  private pitch = 50; // Degrees from horizontal
  private yaw = 0; // Rotation around Y axis

  // Smooth interpolation targets
  private targetTarget = new THREE.Vector3(0, 0, 0);
  private targetDistance = 30;
  private targetPitch = 50;
  private targetYaw = 0;

  // Input state
  private keys: Set<string> = new Set();
  private isDragging = false;
  private isRotating = false;
  private lastMouse = { x: 0, y: 0 };

  // Map bounds for clamping
  private bounds: { minX: number; maxX: number; minZ: number; maxZ: number } | null = null;

  constructor(
    aspect: number,
    config: Partial<CameraConfig> = {}
  ) {
    this.config = { ...defaultCameraConfig, ...config };

    this.camera = new THREE.PerspectiveCamera(60, aspect, 0.1, 1000);
    this.updateCameraPosition();

    this.setupEventListeners();
  }

  /**
   * Set the map bounds for camera clamping.
   */
  setBounds(bounds: { minX: number; maxX: number; minZ: number; maxZ: number }): void {
    this.bounds = bounds;
    // Add some padding
    const padding = 5;
    this.bounds.minX -= padding;
    this.bounds.maxX += padding;
    this.bounds.minZ -= padding;
    this.bounds.maxZ += padding;
  }

  /**
   * Set initial camera position to look at center of map.
   */
  setInitialPosition(centerX: number, centerZ: number): void {
    this.target.set(centerX, 0, centerZ);
    this.targetTarget.copy(this.target);
    this.updateCameraPosition();
  }

  /**
   * Setup keyboard and mouse event listeners.
   */
  private setupEventListeners(): void {
    // Keyboard
    window.addEventListener('keydown', (e) => {
      this.keys.add(e.key.toLowerCase());
    });

    window.addEventListener('keyup', (e) => {
      this.keys.delete(e.key.toLowerCase());
    });

    // Mouse wheel for zoom
    window.addEventListener('wheel', (e) => {
      e.preventDefault();
      const zoomDelta = e.deltaY > 0 ? 1.1 : 0.9;
      this.targetDistance = THREE.MathUtils.clamp(
        this.targetDistance * zoomDelta,
        this.config.minZoom,
        this.config.maxZoom
      );
    }, { passive: false });

    // Mouse buttons for dragging
    window.addEventListener('mousedown', (e) => {
      if (e.button === 1) { // Middle mouse
        this.isDragging = true;
        this.lastMouse = { x: e.clientX, y: e.clientY };
        e.preventDefault();
      } else if (e.button === 2) { // Right mouse
        this.isRotating = true;
        this.lastMouse = { x: e.clientX, y: e.clientY };
        e.preventDefault();
      }
    });

    window.addEventListener('mouseup', (e) => {
      if (e.button === 1) this.isDragging = false;
      if (e.button === 2) this.isRotating = false;
    });

    window.addEventListener('mousemove', (e) => {
      const deltaX = e.clientX - this.lastMouse.x;
      const deltaY = e.clientY - this.lastMouse.y;

      if (this.isDragging) {
        // Pan the camera
        const panSpeed = this.config.panSpeed * this.distance * 0.01;
        const yawRad = THREE.MathUtils.degToRad(this.yaw);

        this.targetTarget.x -= (deltaX * Math.cos(yawRad) + deltaY * Math.sin(yawRad)) * panSpeed;
        this.targetTarget.z -= (-deltaX * Math.sin(yawRad) + deltaY * Math.cos(yawRad)) * panSpeed;
      }

      if (this.isRotating) {
        // Rotate the camera (inverted for natural feel)
        this.targetYaw -= deltaX * this.config.rotateSpeed;
        this.targetPitch = THREE.MathUtils.clamp(
          this.targetPitch + deltaY * this.config.rotateSpeed,
          this.config.minPitch,
          this.config.maxPitch
        );
      }

      this.lastMouse = { x: e.clientX, y: e.clientY };
    });

    // Prevent context menu on right click
    window.addEventListener('contextmenu', (e) => e.preventDefault());
  }

  /**
   * Update camera each frame.
   */
  update(deltaTime: number): void {
    // Handle keyboard input for panning
    const panSpeed = this.config.panSpeed * this.distance * deltaTime;
    const yawRad = THREE.MathUtils.degToRad(this.yaw);

    let moveX = 0;
    let moveZ = 0;

    if (this.keys.has('w') || this.keys.has('arrowup')) moveZ -= 1;
    if (this.keys.has('s') || this.keys.has('arrowdown')) moveZ += 1;
    if (this.keys.has('a') || this.keys.has('arrowleft')) moveX -= 1;
    if (this.keys.has('d') || this.keys.has('arrowright')) moveX += 1;

    if (moveX !== 0 || moveZ !== 0) {
      // Normalize diagonal movement
      const length = Math.sqrt(moveX * moveX + moveZ * moveZ);
      moveX /= length;
      moveZ /= length;

      // Apply movement in camera-relative direction
      this.targetTarget.x += (moveX * Math.cos(yawRad) + moveZ * Math.sin(yawRad)) * panSpeed;
      this.targetTarget.z += (-moveX * Math.sin(yawRad) + moveZ * Math.cos(yawRad)) * panSpeed;
    }

    // Handle keyboard rotation
    if (this.keys.has('q')) this.targetYaw -= this.config.rotateSpeed * deltaTime * 60;
    if (this.keys.has('e')) this.targetYaw += this.config.rotateSpeed * deltaTime * 60;

    // Handle keyboard tilt
    if (this.keys.has('r')) {
      this.targetPitch = THREE.MathUtils.clamp(
        this.targetPitch + this.config.rotateSpeed * deltaTime * 60,
        this.config.minPitch,
        this.config.maxPitch
      );
    }
    if (this.keys.has('f')) {
      this.targetPitch = THREE.MathUtils.clamp(
        this.targetPitch - this.config.rotateSpeed * deltaTime * 60,
        this.config.minPitch,
        this.config.maxPitch
      );
    }

    // Clamp target to bounds
    if (this.bounds) {
      this.targetTarget.x = THREE.MathUtils.clamp(
        this.targetTarget.x,
        this.bounds.minX,
        this.bounds.maxX
      );
      this.targetTarget.z = THREE.MathUtils.clamp(
        this.targetTarget.z,
        this.bounds.minZ,
        this.bounds.maxZ
      );
    }

    // Smooth interpolation
    const smoothing = 1 - Math.pow(0.001, deltaTime);
    this.target.lerp(this.targetTarget, smoothing);
    this.distance = THREE.MathUtils.lerp(this.distance, this.targetDistance, smoothing);
    this.pitch = THREE.MathUtils.lerp(this.pitch, this.targetPitch, smoothing);
    this.yaw = THREE.MathUtils.lerp(this.yaw, this.targetYaw, smoothing);

    this.updateCameraPosition();
  }

  /**
   * Calculate and set camera position based on orbital parameters.
   */
  private updateCameraPosition(): void {
    const pitchRad = THREE.MathUtils.degToRad(this.pitch);
    const yawRad = THREE.MathUtils.degToRad(this.yaw);

    // Calculate camera position on sphere around target
    const horizontalDist = this.distance * Math.cos(pitchRad);
    const verticalDist = this.distance * Math.sin(pitchRad);

    this.camera.position.set(
      this.target.x + horizontalDist * Math.sin(yawRad),
      this.target.y + verticalDist,
      this.target.z + horizontalDist * Math.cos(yawRad)
    );

    this.camera.lookAt(this.target);
  }

  /**
   * Handle window resize.
   */
  onResize(width: number, height: number): void {
    this.camera.aspect = width / height;
    this.camera.updateProjectionMatrix();
  }

  /**
   * Get the current target position (useful for raycasting).
   */
  getTarget(): THREE.Vector3 {
    return this.target.clone();
  }

  /**
   * Get the current orbital distance (zoom level).
   */
  getDistance(): number {
    return this.distance;
  }
}
