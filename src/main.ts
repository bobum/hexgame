import * as THREE from 'three';
import GUI from 'lil-gui';

import { HexGrid } from './core/HexGrid';
import { HexCoordinates } from './core/HexCoordinates';
import { HexMetrics } from './core/HexMetrics';
import { MapGenerator } from './generation/MapGenerator';
import { ChunkedTerrainRenderer } from './rendering/ChunkedTerrainRenderer';
import { InstancedHexRenderer } from './rendering/InstancedHexRenderer';
import { WaterRenderer } from './rendering/WaterRenderer';
import { FeatureRenderer } from './rendering/FeatureRenderer';
import { MapCamera } from './camera/MapCamera';
import { MapConfig, defaultMapConfig, HexCell, UnitType } from './types';
import { PerformanceMonitor } from './utils/PerformanceMonitor';
import { UnitManager, UnitRenderer } from './units';

/**
 * Main application class for the hex map game.
 */
class HexGame {
  // Three.js core
  private renderer: THREE.WebGLRenderer;
  private scene: THREE.Scene;
  private mapCamera: MapCamera;

  // Game systems
  grid!: HexGrid;  // Public for console debugging
  private mapGenerator!: MapGenerator;
  private terrainRenderer!: ChunkedTerrainRenderer;
  private instancedRenderer!: InstancedHexRenderer;
  private waterRenderer!: WaterRenderer;
  private featureRenderer!: FeatureRenderer;
  private unitManager!: UnitManager;
  private unitRenderer!: UnitRenderer;

  // Render mode
  private useInstancing: boolean = false;
  private useAsyncGeneration: boolean = true;

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
    // Performance stats
    avgFrameTime: string;
    onePercentLow: number;
    maxFrameTime: string;
    generationTime: string;
    memoryMB: number;
    // Units
    unitCount: number;
    poolStats: string;
  };

  // Interaction
  private raycaster: THREE.Raycaster;
  private mouse: THREE.Vector2;
  private hoveredCell: HexCell | null = null;
  private highlightMesh: THREE.Mesh | null = null;
  private groundPlane: THREE.Plane; // Cached for raycasting

  // Ground plane mesh (prevents seeing through terrain at distance)
  private groundPlaneMesh: THREE.Mesh | null = null;
  private mouseMovedThisFrame: boolean = false;

  // Animation
  private clock: THREE.Clock;

  // Performance monitoring
  private perfMonitor: PerformanceMonitor;

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
    this.scene.fog = new THREE.Fog(0x87ceeb, 15, 50);

    // Setup camera
    this.mapCamera = new MapCamera(window.innerWidth / window.innerHeight);

    // Setup lighting
    this.setupLighting();

    // Setup interaction
    this.raycaster = new THREE.Raycaster();
    this.mouse = new THREE.Vector2();
    this.groundPlane = new THREE.Plane(new THREE.Vector3(0, 1, 0), 0); // Cached plane
    this.setupInteraction();

    // Setup performance monitor
    this.perfMonitor = new PerformanceMonitor();

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
      // Performance stats
      avgFrameTime: '0.0 ms',
      onePercentLow: 0,
      maxFrameTime: '0.0 ms',
      generationTime: '0 ms',
      memoryMB: 0,
      // Units
      unitCount: 0,
      poolStats: '',
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

    mapFolder.add({ useAsync: this.useAsyncGeneration }, 'useAsync').name('Async Generation').onChange((v: boolean) => {
      this.useAsyncGeneration = v;
    });

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

    // Performance panel
    const perfFolder = this.gui.addFolder('Performance');
    perfFolder.add(this.debugInfo, 'avgFrameTime').name('Avg Frame').listen();
    perfFolder.add(this.debugInfo, 'maxFrameTime').name('Max Frame').listen();
    perfFolder.add(this.debugInfo, 'onePercentLow').name('1% Low FPS').listen();
    perfFolder.add(this.debugInfo, 'generationTime').name('Gen Time').listen();
    perfFolder.add(this.debugInfo, 'memoryMB').name('Memory (MB)').listen();
    perfFolder.add({
      toggleGraph: () => this.perfMonitor.toggleGraph()
    }, 'toggleGraph').name('Toggle Graph');
    perfFolder.add({
      runStressTest: () => this.runStressTest()
    }, 'runStressTest').name('Run Stress Test');
    perfFolder.open();

    // Units panel
    const unitsFolder = this.gui.addFolder('Units');
    unitsFolder.add(this.debugInfo, 'unitCount').name('Unit Count').listen();
    unitsFolder.add(this.debugInfo, 'poolStats').name('Pool Stats').listen();
    unitsFolder.add({
      spawn10: () => {
        const spawned = this.unitManager.spawnRandomUnits(10, 1);
        console.log(`Spawned ${spawned} units`);
        this.unitRenderer.markDirty();
      }
    }, 'spawn10').name('Spawn 10 Units');
    unitsFolder.add({
      spawn100: () => {
        const spawned = this.unitManager.spawnRandomUnits(100, 1);
        console.log(`Spawned ${spawned} units`);
        this.unitRenderer.markDirty();
      }
    }, 'spawn100').name('Spawn 100 Units');
    unitsFolder.add({
      clearUnits: () => {
        this.unitManager.clear();
        this.unitRenderer.markDirty();
        console.log('Cleared all units');
      }
    }, 'clearUnits').name('Clear Units');
    unitsFolder.open();

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
   * Setup shader parameter controls (called after terrain is generated).
   */
  private setupShaderUI(): void {
    // Remove old shader folder if it exists
    const existingFolder = this.gui.folders.find(f => f._title === 'Shader Settings');
    if (existingFolder) {
      existingFolder.destroy();
    }

    const material = this.terrainRenderer.getMaterial();
    const uniforms = material.uniforms;

    const shaderFolder = this.gui.addFolder('Shader Settings');

    // Noise settings
    shaderFolder.add(uniforms.uTextureScale, 'value', 0.5, 10, 0.1).name('Texture Scale');
    shaderFolder.add(uniforms.uNoiseStrength, 'value', 0, 1, 0.05).name('Noise Strength');
    shaderFolder.add(uniforms.uTriplanarSharpness, 'value', 1, 10, 0.5).name('Triplanar Sharpness');

    // Biome blending
    shaderFolder.add(uniforms.uBlendStrength, 'value', 0, 2, 0.1).name('Blend Strength');

    // Lighting
    shaderFolder.add(uniforms.uRoughness, 'value', 0, 1, 0.05).name('Roughness');

    // Ambient color (as object with r,g,b for lil-gui)
    const ambientProxy = {
      color: '#' + uniforms.uAmbientColor.value.getHexString()
    };
    shaderFolder.addColor(ambientProxy, 'color').name('Ambient Color').onChange((value: string) => {
      uniforms.uAmbientColor.value.set(value);
    });

    // Sun color
    const sunProxy = {
      color: '#' + uniforms.uSunColor.value.getHexString()
    };
    shaderFolder.addColor(sunProxy, 'color').name('Sun Color').onChange((value: string) => {
      uniforms.uSunColor.value.set(value);
    });

    shaderFolder.open();
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
    if (this.useAsyncGeneration) {
      this.generateMapAsync();
      return;
    }

    const startTime = performance.now();
    console.log('Generating map with seed:', this.config.seed, '(sync)');

    // Dispose old renderers if they exist
    if (this.terrainRenderer) this.terrainRenderer.dispose();
    if (this.instancedRenderer) this.instancedRenderer.dispose();
    if (this.waterRenderer) this.waterRenderer.dispose();
    if (this.featureRenderer) this.featureRenderer.dispose();
    if (this.unitRenderer) this.unitRenderer.dispose();
    if (this.groundPlaneMesh) {
      this.scene.remove(this.groundPlaneMesh);
      this.groundPlaneMesh.geometry.dispose();
      (this.groundPlaneMesh.material as THREE.Material).dispose();
      this.groundPlaneMesh = null;
    }

    // Recreate grid with new config
    this.grid = new HexGrid(this.config);
    this.mapGenerator = new MapGenerator(this.grid);
    this.mapGenerator.generate();

    // Create both renderers
    this.terrainRenderer = new ChunkedTerrainRenderer(this.scene, this.grid);
    this.instancedRenderer = new InstancedHexRenderer(this.scene, this.grid);
    this.waterRenderer = new WaterRenderer(this.scene, this.grid);
    this.featureRenderer = new FeatureRenderer(this.scene, this.grid);

    // Create unit system
    this.unitManager = new UnitManager(this.grid);
    this.unitRenderer = new UnitRenderer(this.scene, this.unitManager);

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

    // Create ground plane to prevent seeing through terrain at distance
    const bounds = this.grid.getMapBounds();
    this.createGroundPlane(bounds);

    // Update camera bounds and position
    this.mapCamera.setBounds(bounds);
    const center = this.grid.getMapCenter();
    this.mapCamera.setInitialPosition(center.x, center.z);

    // Update debug info
    this.debugInfo.cells = this.grid.cellCount;
    this.debugInfo.chunks = this.useInstancing ? 0 : this.terrainRenderer.chunkCount;
    this.debugInfo.hexInstances = this.useInstancing ? this.instancedRenderer.hexCount : 0;
    this.debugInfo.wallInstances = this.useInstancing ? this.instancedRenderer.wallCount : 0;

    // Record generation time
    const genTime = performance.now() - startTime;
    this.perfMonitor.recordGenerationTime(genTime);
    this.debugInfo.generationTime = `${genTime.toFixed(0)} ms`;

    // Setup shader UI controls
    this.setupShaderUI();

    console.log(`Map generated: ${this.grid.cellCount} cells in ${genTime.toFixed(0)}ms`);
  }

  /**
   * Generate map asynchronously using Web Workers.
   */
  private async generateMapAsync(): Promise<void> {
    const startTime = performance.now();
    console.log('Generating map with seed:', this.config.seed, '(async worker)');

    // Dispose old renderers if they exist
    if (this.terrainRenderer) this.terrainRenderer.dispose();
    if (this.instancedRenderer) this.instancedRenderer.dispose();
    if (this.waterRenderer) this.waterRenderer.dispose();
    if (this.featureRenderer) this.featureRenderer.dispose();
    if (this.unitRenderer) this.unitRenderer.dispose();
    if (this.groundPlaneMesh) {
      this.scene.remove(this.groundPlaneMesh);
      this.groundPlaneMesh.geometry.dispose();
      (this.groundPlaneMesh.material as THREE.Material).dispose();
      this.groundPlaneMesh = null;
    }

    // Recreate grid with new config
    this.grid = new HexGrid(this.config);
    this.mapGenerator = new MapGenerator(this.grid);

    // Run generation in worker
    const result = await this.mapGenerator.generateAsync();

    // Create renderers
    this.terrainRenderer = new ChunkedTerrainRenderer(this.scene, this.grid);
    this.instancedRenderer = new InstancedHexRenderer(this.scene, this.grid);
    this.waterRenderer = new WaterRenderer(this.scene, this.grid);
    this.featureRenderer = new FeatureRenderer(this.scene, this.grid);

    // Create unit system
    this.unitManager = new UnitManager(this.grid);
    this.unitRenderer = new UnitRenderer(this.scene, this.unitManager);

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

    // Create ground plane to prevent seeing through terrain at distance
    const bounds = this.grid.getMapBounds();
    this.createGroundPlane(bounds);

    // Update camera bounds and position
    this.mapCamera.setBounds(bounds);
    const center = this.grid.getMapCenter();
    this.mapCamera.setInitialPosition(center.x, center.z);

    // Update debug info
    this.debugInfo.cells = this.grid.cellCount;
    this.debugInfo.chunks = this.useInstancing ? 0 : this.terrainRenderer.chunkCount;
    this.debugInfo.hexInstances = this.useInstancing ? this.instancedRenderer.hexCount : 0;
    this.debugInfo.wallInstances = this.useInstancing ? this.instancedRenderer.wallCount : 0;

    // Record generation time
    const genTime = performance.now() - startTime;
    this.perfMonitor.recordGenerationTime(genTime);
    this.debugInfo.generationTime = `${genTime.toFixed(0)} ms (W:${result.workerTime.toFixed(0)})`;

    // Setup shader UI controls
    this.setupShaderUI();

    console.log(`Map generated (async): ${this.grid.cellCount} cells in ${genTime.toFixed(0)}ms (worker: ${result.workerTime.toFixed(0)}ms, features: ${result.featureTime.toFixed(0)}ms)`);
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
   * Create ground plane mesh below terrain.
   */
  private createGroundPlane(bounds: { minX: number; maxX: number; minZ: number; maxZ: number }): void {
    const groundY = HexMetrics.minElevation * HexMetrics.elevationStep - 0.5;
    const padding = 20;
    const groundGeo = new THREE.PlaneGeometry(
      bounds.maxX - bounds.minX + padding * 2,
      bounds.maxZ - bounds.minZ + padding * 2
    );
    groundGeo.rotateX(-Math.PI / 2);
    const groundMat = new THREE.MeshBasicMaterial({
      color: 0x1a4c6e, // Ocean color
      fog: true,
    });
    this.groundPlaneMesh = new THREE.Mesh(groundGeo, groundMat);
    this.groundPlaneMesh.position.set(
      (bounds.minX + bounds.maxX) / 2,
      groundY,
      (bounds.minZ + bounds.maxZ) / 2
    );
    this.scene.add(this.groundPlaneMesh);
  }

  /**
   * Update hex hover detection (only when mouse moved).
   */
  private updateHover(): void {
    // Skip if mouse hasn't moved - major performance win
    if (!this.mouseMovedThisFrame) return;
    this.mouseMovedThisFrame = false;

    this.raycaster.setFromCamera(this.mouse, this.mapCamera.camera);

    // Collect terrain meshes (LOD chunks) for raycasting
    const terrainObjects: THREE.Object3D[] = [];
    this.scene.traverse((obj) => {
      if (obj.name.startsWith('chunk_')) {
        terrainObjects.push(obj);
      }
    });

    // Raycast against terrain geometry
    const intersects = this.raycaster.intersectObjects(terrainObjects, true);

    if (intersects.length > 0) {
      // Use the intersection point on the terrain mesh
      const intersection = intersects[0].point;

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
    } else {
      // No terrain hit - clear selection
      this.hoveredCell = null;
      if (this.highlightMesh) {
        this.highlightMesh.visible = false;
      }
      this.debugInfo.hoveredHex = 'None';
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

    // Get camera distance for LOD calculations
    const cameraDistance = this.mapCamera.getDistance();

    // Update water animation and visibility
    this.waterRenderer.update(deltaTime, cameraDistance);

    // Update feature visibility based on camera distance
    this.featureRenderer.update(cameraDistance);

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

    // Update unit renderer
    if (this.unitRenderer) {
      this.unitRenderer.update();
    }

    // Update unit stats
    if (this.unitManager) {
      this.debugInfo.unitCount = this.unitManager.unitCount;
      const stats = this.unitManager.poolStats;
      this.debugInfo.poolStats = `${stats.active}/${stats.created} (${(stats.reuseRate * 100).toFixed(0)}%)`;
    }

    // Render
    this.renderer.render(this.scene, this.mapCamera.camera);

    // Record frame for performance monitoring
    this.perfMonitor.recordFrame(deltaTime);

    // Update debug stats
    this.debugInfo.fps = this.perfMonitor.fps;
    this.debugInfo.avgFrameTime = `${this.perfMonitor.avgFrameTime.toFixed(1)} ms`;
    this.debugInfo.maxFrameTime = `${this.perfMonitor.maxFrameTime.toFixed(1)} ms`;
    this.debugInfo.onePercentLow = this.perfMonitor.onePercentLow;

    // Update render stats
    this.debugInfo.drawCalls = this.renderer.info.render.calls;
    this.debugInfo.triangles = this.renderer.info.render.triangles;
    this.debugInfo.geometries = this.renderer.info.memory.geometries;
    if (!this.useInstancing) {
      this.debugInfo.visibleChunks = this.terrainRenderer.getVisibleChunkCount(this.mapCamera.camera);
    }

    // Update memory usage (if available)
    if ((performance as any).memory) {
      this.debugInfo.memoryMB = Math.round((performance as any).memory.usedJSHeapSize / 1024 / 1024);
    }
  }

  /**
   * Run automated stress test.
   */
  private async runStressTest(): Promise<void> {
    console.log('=== STRESS TEST STARTED ===');
    const results: { size: string; cells: number; genTime: number; avgFps: number; minFps: number }[] = [];

    const sizes = [
      { width: 20, height: 15, name: 'Small (20x15)' },
      { width: 40, height: 30, name: 'Medium (40x30)' },
      { width: 60, height: 45, name: 'Large (60x45)' },
      { width: 80, height: 60, name: 'Max (80x60)' },
    ];

    for (const size of sizes) {
      console.log(`Testing ${size.name}...`);

      // Set config and generate
      this.config.width = size.width;
      this.config.height = size.height;
      this.config.seed = 12345; // Consistent seed
      this.gui.controllersRecursive().forEach(c => c.updateDisplay());

      this.perfMonitor.reset();
      this.generateMap();

      // Wait for a few frames to stabilize
      await this.waitFrames(30);

      // Collect 120 frames of data
      this.perfMonitor.reset();
      await this.waitFrames(120);

      results.push({
        size: size.name,
        cells: this.grid.cellCount,
        genTime: this.perfMonitor.lastGenerationTime,
        avgFps: Math.round(1000 / this.perfMonitor.avgFrameTime),
        minFps: this.perfMonitor.onePercentLow,
      });
    }

    // Print results
    console.log('=== STRESS TEST RESULTS ===');
    console.table(results);

    // Reset to default size
    this.config.width = 30;
    this.config.height = 20;
    this.gui.controllersRecursive().forEach(c => c.updateDisplay());
    this.generateMap();

    console.log('=== STRESS TEST COMPLETE ===');
  }

  /**
   * Wait for N frames (for stress test timing).
   */
  private waitFrames(count: number): Promise<void> {
    return new Promise(resolve => {
      let frames = 0;
      const check = () => {
        frames++;
        if (frames >= count) {
          resolve();
        } else {
          requestAnimationFrame(check);
        }
      };
      requestAnimationFrame(check);
    });
  }
}

// Start the application
const game = new HexGame();

// Expose for console debugging
(window as any).game = game;
