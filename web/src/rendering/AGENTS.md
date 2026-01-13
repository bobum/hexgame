# Rendering Module Agent Guide

This module handles Three.js rendering including terrain, water, features, and paths.

## Files

| File | Purpose |
|------|---------|
| `ChunkedTerrainRenderer.ts` | LOD terrain with chunk-based rendering |
| `InstancedHexRenderer.ts` | GPU instancing alternative |
| `WaterRenderer.ts` | Animated ocean surface |
| `EdgeRiverRenderer.ts` | Animated river flow |
| `FeatureRenderer.ts` | Trees, rocks, mountain peaks |
| `PathRenderer.ts` | Movement path visualization |
| `TerrainShaderMaterial.ts` | Custom terrain shader |
| `HexMeshBuilder.ts` | Low-level hex geometry |
| `LODHexBuilder.ts` | Multi-detail hex meshes |

## Render Modes

Two terrain rendering approaches (toggle in UI):

### Chunked + LOD (Default)

- Divides map into 16x16 hex chunks
- 3 LOD levels based on camera distance
- Frustum culling per chunk
- Better for varied detail at different distances

### Instanced

- Uses GPU instancing for all hexes
- Single draw call for all terrain
- Better for consistent high density
- No LOD support

## ChunkedTerrainRenderer

```typescript
const renderer = new ChunkedTerrainRenderer(scene, grid);
renderer.build();  // Initial geometry creation

// Per-frame updates
renderer.update(camera);        // LOD and culling
renderer.updateShader(delta);   // Shader uniforms

// Stats
renderer.chunkCount;            // Total chunks
renderer.getVisibleChunkCount(); // Currently visible

renderer.dispose();  // Cleanup
```

### LOD Levels

| Level | Distance | Detail |
|-------|----------|--------|
| High | < 15 | Full hex geometry with walls |
| Medium | 15-30 | Hex tops only, no walls |
| Low | > 30 | Simple quad per hex |

### Chunk Naming

Chunks are named `chunk_{chunkX}_{chunkZ}` for raycasting identification.

## WaterRenderer

Animated ocean surface with foam effects:

```typescript
const water = new WaterRenderer(scene, grid);
water.build();

// Per-frame
water.update(deltaTime, cameraDistance);

water.dispose();
```

Water mesh is named `water_surface` for raycasting.

## EdgeRiverRenderer

Rivers rendered along hex edges with flow animation:

```typescript
const rivers = new EdgeRiverRenderer(scene, grid);
rivers.build();

// Per-frame
rivers.update(deltaTime, cameraDistance);

// Shader access for UI controls
rivers.getUniforms();

rivers.dispose();
```

## FeatureRenderer

Trees, rocks, and mountain peaks with LOD:

```typescript
const features = new FeatureRenderer(scene, grid);
features.build();

// Per-frame (visibility based on camera distance)
features.update(cameraDistance);

features.dispose();
```

### Feature Visibility

| Feature | Max Distance |
|---------|--------------|
| Trees | 40 units |
| Rocks | 30 units |
| Peaks | 60 units |

## PathRenderer

Visualizes movement paths and reachable cells:

```typescript
const pathRenderer = new PathRenderer(scene);

// Show reachable cells
pathRenderer.showReachableCells(cells);  // HexCell[]
pathRenderer.hideReachableCells();

// Show path
pathRenderer.showPath(path);  // HexCell[]
pathRenderer.hidePath();

// Color indicates validity
pathRenderer.setPathValid(true);   // Green
pathRenderer.setPathValid(false);  // Red

pathRenderer.dispose();
```

## TerrainShaderMaterial

Custom shader with:
- Biome-based coloring
- Triplanar texturing
- Noise detail
- Lighting

```typescript
const material = new TerrainShaderMaterial();

// Uniforms accessible for UI
material.uniforms.uTextureScale.value = 5.0;
material.uniforms.uNoiseStrength.value = 0.3;
material.uniforms.uTriplanarSharpness.value = 4.0;
material.uniforms.uBlendStrength.value = 0.5;
```

## HexMeshBuilder

Low-level hex geometry construction:

```typescript
// Creates BufferGeometry for a single hex
const geometry = HexMeshBuilder.createHexGeometry(cell, {
  includeWalls: true,
  wallHeight: 0.3
});
```

## LODHexBuilder

Multi-detail hex mesh generation:

```typescript
// High detail (full hex with walls)
const high = LODHexBuilder.createHighDetail(cell);

// Medium detail (hex top only)
const medium = LODHexBuilder.createMediumDetail(cell);

// Low detail (simple quad)
const low = LODHexBuilder.createLowDetail(cell);
```

## Three.js Lifecycle

**Always dispose resources when removing:**

```typescript
dispose(): void {
  // Remove from scene first
  this.scene.remove(this.mesh);

  // Dispose geometry
  this.mesh.geometry.dispose();

  // Dispose material(s)
  if (Array.isArray(this.mesh.material)) {
    this.mesh.material.forEach(m => m.dispose());
  } else {
    this.mesh.material.dispose();
  }

  // Dispose textures if any
  this.texture?.dispose();
}
```

## Adding New Renderable

1. Create renderer class with standard interface:
```typescript
class MyRenderer {
  constructor(scene: THREE.Scene, grid: HexGrid) {}
  build(): void {}
  update(deltaTime: number): void {}
  dispose(): void {}
}
```

2. Instantiate in `main.ts`:
```typescript
this.myRenderer = new MyRenderer(this.scene, this.grid);
```

3. Call in appropriate lifecycle:
```typescript
// In generateMap()
this.myRenderer.build();

// In animate()
this.myRenderer.update(deltaTime);
```

## Raycasting

Terrain and water meshes support raycasting:

```typescript
// Collect raycast targets
const targets: THREE.Object3D[] = [];
scene.traverse((obj) => {
  if (obj.name.startsWith('chunk_') || obj.name === 'water_surface') {
    targets.push(obj);
  }
});

const intersects = raycaster.intersectObjects(targets, true);
```

## Performance Considerations

- **LOD**: Use LOD for terrain at distance
- **Frustum Culling**: Chunks outside view are hidden
- **Instancing**: Consider for large uniform areas
- **Feature Culling**: Hide small features at distance
- **Shader Complexity**: Keep fragment shaders simple
- **Draw Calls**: Merge geometries where possible
