import * as THREE from 'three';
import GUI from 'lil-gui';

import { HexGrid } from './core/HexGrid';
import { HexCoordinates } from './core/HexCoordinates';
import { MapGenerator } from './generation/MapGenerator';
import { ChunkedTerrainRenderer } from './rendering/ChunkedTerrainRenderer';
import { InstancedHexRenderer } from './rendering/InstancedHexRenderer';
import { WaterRenderer } from './rendering/WaterRenderer';
import { FeatureRenderer } from './rendering/FeatureRenderer';
import { MapCamera } from './camera/MapCamera';
import { MapConfig, defaultMapConfig, HexCell } from './types';

/**
 * Main application class for the hex map game.
 */
class HexGame {
  // Three.js core
  private renderer: THREE.WebGLRenderer;
  private scene: THREE.Scene;
  private mapCamera: MapCamera;

  // Game systems
  private grid!: HexGrid;
  private mapGenerator!: MapGenerator;
  private terrainRenderer!: ChunkedTerrainRenderer;
  private instancedRenderer!: InstancedHexRenderer;
  private waterRenderer!: WaterRenderer;
  private featureRenderer!: FeatureRenderer;

  // Render mode
  private useInstancing: boolean = false;

  // Debug UI
  private gui: GUI;
  private config: MapConfig;
  private debugInfo: {
    cells: number;
    fps: number;
    hoveredHex: string;
    drawCalls: number;
    triangles: number;
    geometries: number;
    chunks: number;
    visibleChunks: number;
    renderMode: string;
    hexInstances: number;
    wallInstances: number;
  };

  // Interaction
  private raycaster: THREE.Raycaster;
  private mouse: THREE.Vector2;
  private hoveredCell: HexCell | null = null;
  private highlightMesh: THREE.Mesh | null = null;
  private groundPlane: THREE.Plane; // Cached for raycasting
  private mouseMovedThisFrame: boolean = false;

  // Animation
  private clock: THREE.Clock;

  constructor() {
    // Initialize config
    this.config = { ...defaultMapConfig };

    // Setup renderer
    this.renderer = new THREE.WebGLRenderer({ antialias: true });
    this.renderer.setSize(window.innerWidth, window.innerHeight);
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    this.renderer.shadowMap.enabled = true;
    this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    this.renderer.setClearColor(0x87ceeb); // Sky blue background
    document.body.appendChild(this.renderer.domElement);

    // Setup scene
    this.scene = new THREE.Scene();
    this.scene.fog = new THREE.Fog(0x87ceeb, 50, 150);

    // Setup camera
    this.mapCamera = new MapCamera(window.innerWidth / window.innerHeight);

    // Setup lighting
    this.setupLighting();

    // Setup interaction
    this.raycaster = new THREE.Raycaster();
    this.mouse = new THREE.Vector2();
    this.groundPlane = new THREE.Plane(new THREE.Vector3(0, 1, 0), 0); // Cached plane
    this.setupInteraction();

    // Setup debug UI
    this.debugInfo = {
      cells: 0,
      fps: 0,
      hoveredHex: 'None',
      drawCalls: 0,
      triangles: 0,
      geometries: 0,
      chunks: 0,
      visibleChunks: 0,
      renderMode: 'Chunked+LOD',
      hexInstances: 0,
      wallInstances: 0,
    };
    this.gui = new GUI();
    this.setupDebugUI();

    // Clock for delta time
    this.clock = new THREE.Clock();

    // Generate initial map
    this.generateMap();

    // Handle resize
    window.addEventListener('resize', () => this.onResize());

    // Start render loop
    this.animate();
  }

  /**
   * Setup scene lighting.
   */
  private setupLighting(): void {
    // Ambient light for base illumination
    const ambient = new THREE.AmbientLight(0xffffff, 0.4);
    this.scene.add(ambient);

    // Directional light (sun)
    const sun = new THREE.DirectionalLight(0xffffff, 1.0);
    sun.position.set(50, 80, 30);
    sun.castShadow = true;
    sun.shadow.mapSize.width = 2048;
    sun.shadow.mapSize.height = 2048;
    sun.shadow.camera.near = 0.5;
    sun.shadow.camera.far = 200;
    sun.shadow.camera.left = -60;
    sun.shadow.camera.right = 60;
    sun.shadow.camera.top = 60;
    sun.shadow.camera.bottom = -60;
    this.scene.add(sun);

    // Hemisphere light for sky/ground color variation
    const hemi = new THREE.HemisphereLight(0x87ceeb, 0x8b7355, 0.3);
    this.scene.add(hemi);
  }

  /**
   * Setup debug UI with lil-gui.
   */
  private setupDebugUI(): void {
    const mapFolder = this.gui.addFolder('Map Generation');

    mapFolder.add(this.config, 'width', 10, 80, 1).name('Width');
    mapFolder.add(this.config, 'height', 10, 60, 1).name('Height');
    mapFolder.add(this.config, 'seed', 1, 99999, 1).name('Seed');
    mapFolder.add(this.config, 'noiseScale', 0.01, 0.2, 0.005).name('Noise Scale');
    mapFolder.add(this.config, 'octaves', 1, 8, 1).name('Octaves');
    mapFolder.add(this.config, 'persistence', 0.1, 0.9, 0.05).name('Persistence');
    mapFolder.add(this.config, 'lacunarity', 1.5, 3.0, 0.1).name('Lacunarity');
    mapFolder.add(this.config, 'landPercentage', 0.2, 0.8, 0.05).name('Land %');
    mapFolder.add(this.config, 'mountainousness', 0.1, 1.0, 0.05).name('Mountains');

    mapFolder.add({
      regenerate: () => this.generateMap()
    }, 'regenerate').name('Regenerate Map');

    mapFolder.add({
      randomSeed: () => {
        this.config.seed = Math.floor(Math.random() * 99999);
        this.gui.controllersRecursive().forEach(c => c.updateDisplay());
        this.generateMap();
      }
    }, 'randomSeed').name('Random Seed');

    mapFolder.open();

    // Info panel
    const infoFolder = this.gui.addFolder('Info');
    infoFolder.add(this.debugInfo, 'cells').name('Total Cells').listen();
    infoFolder.add(this.debugInfo, 'fps').name('FPS').listen();
    infoFolder.add(this.debugInfo, 'hoveredHex').name('Hovered Hex').listen();
    infoFolder.open();

    // Render stats panel
    const statsFolder = this.gui.addFolder('Render Stats');
    statsFolder.add(this.debugInfo, 'renderMode').name('Mode').listen();
    statsFolder.add(this.debugInfo, 'drawCalls').name('Draw Calls').listen();
    statsFolder.add(this.debugInfo, 'triangles').name('Triangles').listen();
    statsFolder.add(this.debugInfo, 'geometries').name('Geometries').listen();
    statsFolder.add(this.debugInfo, 'chunks').name('Total Chunks').listen();
    statsFolder.add(this.debugInfo, 'visibleChunks').name('Visible Chunks').listen();
    statsFolder.add(this.debugInfo, 'hexInstances').name('Hex Instances').listen();
    statsFolder.add(this.debugInfo, 'wallInstances').name('Wall Instances').listen();
    statsFolder.add({
      toggleRenderer: () => this.toggleRenderMode()
    }, 'toggleRenderer').name('Toggle Instancing');
    statsFolder.open();

    // Controls help
    const helpFolder = this.gui.addFolder('Controls');
    helpFolder.add({ text: 'WASD / Arrows: Pan' }, 'text').name('');
    helpFolder.add({ text: 'Mouse Wheel: Zoom' }, 'text').name('');
    helpFolder.add({ text: 'Q / E: Rotate' }, 'text').name('');
    helpFolder.add({ text: 'R / F: Tilt Up/Down' }, 'text').name('');
    helpFolder.add({ text: 'Right Drag: Rotate + Tilt' }, 'text').name('');
    helpFolder.add({ text: 'Middle Drag: Pan' }, 'text').name('');
  }

  /**
   * Setup mouse interaction for hex selection.
   */
  private setupInteraction(): void {
    window.addEventListener('mousemove', (e) => {
      this.mouse.x = (e.clientX / window.innerWidth) * 2 - 1;
      this.mouse.y = -(e.clientY / window.innerHeight) * 2 + 1;
      this.mouseMovedThisFrame = true; // Flag for throttled hover detection
    });

    // Create highlight mesh (hexagonal ring)
    const highlightGeo = new THREE.RingGeometry(0.7, 0.85, 6);
    highlightGeo.rotateX(-Math.PI / 2);
    highlightGeo.rotateY(Math.PI / 6); // Align with hex orientation
    const highlightMat = new THREE.MeshBasicMaterial({
      color: 0xffff00,
      side: THREE.DoubleSide,
      transparent: true,
      opacity: 0.8,
    });
    this.highlightMesh = new THREE.Mesh(highlightGeo, highlightMat);
    this.highlightMesh.visible = false;
    this.scene.add(this.highlightMesh);
  }

  /**
   * Generate or regenerate the map.
   */
  private generateMap(): void {
    console.log('Generating map with seed:', this.config.seed);

    // Dispose old renderers if they exist
    if (this.terrainRenderer) this.terrainRenderer.dispose();
    if (this.instancedRenderer) this.instancedRenderer.dispose();
    if (this.waterRenderer) this.waterRenderer.dispose();
    if (this.featureRenderer) this.featureRenderer.dispose();

    // Recreate grid with new config
    this.grid = new HexGrid(this.config);
    this.mapGenerator = new MapGenerator(this.grid);
    this.mapGenerator.generate();

    // Create both renderers
    this.terrainRenderer = new ChunkedTerrainRenderer(this.scene, this.grid);
    this.instancedRenderer = new InstancedHexRenderer(this.scene, this.grid);
    this.waterRenderer = new WaterRenderer(this.scene, this.grid);
    this.featureRenderer = new FeatureRenderer(this.scene, this.grid);

    // Build active renderer based on mode
    if (this.useInstancing) {
      this.instancedRenderer.build();
      this.debugInfo.renderMode = 'Instanced';
    } else {
      this.terrainRenderer.build();
      this.debugInfo.renderMode = 'Chunked+LOD';
    }
    this.waterRenderer.build();
    this.featureRenderer.build();

    // Update camera bounds and position
    const bounds = this.grid.getMapBounds();
    this.mapCamera.setBounds(bounds);
    const center = this.grid.getMapCenter();
    this.mapCamera.setInitialPosition(center.x, center.z);

    // Update debug info
    this.debugInfo.cells = this.grid.cellCount;
    this.debugInfo.chunks = this.useInstancing ? 0 : this.terrainRenderer.chunkCount;
    this.debugInfo.hexInstances = this.useInstancing ? this.instancedRenderer.hexCount : 0;
    this.debugInfo.wallInstances = this.useInstancing ? this.instancedRenderer.wallCount : 0;

    console.log('Map generated:', this.grid.cellCount, 'cells');
  }

  /**
   * Toggle between chunked and instanced rendering.
   */
  private toggleRenderMode(): void {
    this.useInstancing = !this.useInstancing;

    // Dispose current terrain and rebuild with new mode
    this.terrainRenderer.dispose();
    this.instancedRenderer.dispose();

    if (this.useInstancing) {
      this.instancedRenderer.build();
      this.debugInfo.renderMode = 'Instanced';
      this.debugInfo.chunks = 0;
      this.debugInfo.visibleChunks = 0;
      this.debugInfo.hexInstances = this.instancedRenderer.hexCount;
      this.debugInfo.wallInstances = this.instancedRenderer.wallCount;
    } else {
      this.terrainRenderer.build();
      this.debugInfo.renderMode = 'Chunked+LOD';
      this.debugInfo.chunks = this.terrainRenderer.chunkCount;
      this.debugInfo.hexInstances = 0;
      this.debugInfo.wallInstances = 0;
    }

    // Release focus from GUI button so keyboard controls work
    if (document.activeElement instanceof HTMLElement) {
      document.activeElement.blur();
    }

    console.log('Switched to', this.debugInfo.renderMode, 'rendering');
  }

  /**
   * Update hex hover detection (only when mouse moved).
   */
  private updateHover(): void {
    // Skip if mouse hasn't moved - major performance win
    if (!this.mouseMovedThisFrame) return;
    this.mouseMovedThisFrame = false;

    this.raycaster.setFromCamera(this.mouse, this.mapCamera.camera);

    // Use cached ground plane for raycasting
    const intersection = new THREE.Vector3();

    if (this.raycaster.ray.intersectPlane(this.groundPlane, intersection)) {
      // Convert world position to hex coordinates
      const hexCoords = HexCoordinates.fromWorldPosition(intersection);
      const cell = this.grid.getCell(hexCoords);

      if (cell) {
        this.hoveredCell = cell;

        // Update highlight position
        if (this.highlightMesh) {
          const worldPos = hexCoords.toWorldPosition(cell.elevation);
          this.highlightMesh.position.set(worldPos.x, worldPos.y + 0.05, worldPos.z);
          this.highlightMesh.visible = true;
        }

        // Update debug info
        this.debugInfo.hoveredHex = `(${cell.q}, ${cell.r}) ${cell.terrainType} E:${cell.elevation}`;
      } else {
        this.hoveredCell = null;
        if (this.highlightMesh) {
          this.highlightMesh.visible = false;
        }
        this.debugInfo.hoveredHex = 'None';
      }
    }
  }

  /**
   * Handle window resize.
   */
  private onResize(): void {
    this.mapCamera.onResize(window.innerWidth, window.innerHeight);
    this.renderer.setSize(window.innerWidth, window.innerHeight);
  }

  /**
   * Main animation loop.
   */
  private animate(): void {
    requestAnimationFrame(() => this.animate());

    const deltaTime = this.clock.getDelta();

    // Update camera
    this.mapCamera.update(deltaTime);

    // Update water animation
    this.waterRenderer.update(deltaTime);

    // Update terrain shader (time uniform for potential animations)
    if (!this.useInstancing) {
      this.terrainRenderer.updateShader(deltaTime);
    }

    // Update hover detection
    this.updateHover();

    // Update terrain LOD (only for chunked mode)
    if (!this.useInstancing) {
      this.terrainRenderer.update(this.mapCamera.camera);
    }

    // Update FPS
    this.debugInfo.fps = Math.round(1 / deltaTime);

    // Render
    this.renderer.render(this.scene, this.mapCamera.camera);

    // Update render stats
    this.debugInfo.drawCalls = this.renderer.info.render.calls;
    this.debugInfo.triangles = this.renderer.info.render.triangles;
    this.debugInfo.geometries = this.renderer.info.memory.geometries;
    if (!this.useInstancing) {
      this.debugInfo.visibleChunks = this.terrainRenderer.getVisibleChunkCount(this.mapCamera.camera);
    }
  }
}

// Start the application
new HexGame();
