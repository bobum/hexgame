# Skill: Add Map Feature

Add a new map feature (decoration) like trees, rocks, or buildings.

## Usage

```
/add-feature <name>
```

## Steps

### 1. Add to FeatureType enum

File: `src/types/index.ts`

```typescript
export enum FeatureType {
  // ...existing types...
  {{Name}} = '{{name}}',
}
```

### 2. Create feature mesh

File: `src/rendering/FeatureRenderer.ts`

In `createFeatureMesh()` method:

```typescript
private createFeatureMesh(feature: Feature): THREE.Mesh | null {
  switch (feature.type) {
    // ...existing cases...

    case FeatureType.{{Name}}:
      return this.create{{Name}}Mesh(feature);
  }
}

private create{{Name}}Mesh(feature: Feature): THREE.Mesh {
  // Create geometry
  const geometry = new THREE.{{GeometryType}}({{geometryParams}});

  // Create material
  const material = new THREE.MeshLambertMaterial({
    color: 0x{{hexColor}},
  });

  // Create mesh
  const mesh = new THREE.Mesh(geometry, material);
  mesh.position.copy(feature.position);
  mesh.scale.setScalar(feature.scale);
  mesh.rotation.y = feature.rotation;

  return mesh;
}
```

### 3. Add placement logic

File: `src/generation/MapGenerator.ts`

In `placeFeatures()` method:

```typescript
private placeFeatures(): void {
  for (const cell of this.grid.cells) {
    // ...existing placement...

    // {{Name}} placement
    if (cell.terrainType === TerrainType.{{ValidTerrain}}) {
      if (this.random() < {{placementChance}}) {
        const worldPos = new HexCoordinates(cell.q, cell.r)
          .toWorldPosition(cell.elevation);

        cell.features.push({
          type: FeatureType.{{Name}},
          position: new THREE.Vector3(
            worldPos.x + (this.random() - 0.5) * 0.5,
            worldPos.y,
            worldPos.z + (this.random() - 0.5) * 0.5
          ),
          scale: 0.3 + this.random() * 0.2,
          rotation: this.random() * Math.PI * 2,
        });
      }
    }
  }
}
```

### 4. Set visibility distance

File: `src/rendering/FeatureRenderer.ts`

In `update()` method, add visibility threshold:

```typescript
// {{Name}} visibility
const {{name}}Distance = {{maxDistance}};  // Hide beyond this distance
```

### 5. Add LOD (optional)

For complex features, add LOD levels in `FeatureRenderer`:

```typescript
private create{{Name}}LOD(feature: Feature, distance: number): THREE.Mesh {
  if (distance > {{lodThreshold}}) {
    // Simplified geometry
    return this.create{{Name}}SimpleMesh(feature);
  }
  return this.create{{Name}}Mesh(feature);
}
```

## Checklist

- [ ] Added to FeatureType enum
- [ ] Created mesh generation method
- [ ] Added placement rules in MapGenerator
- [ ] Set appropriate visibility distance
- [ ] Considered LOD for complex features
- [ ] Type check passes: `npm run typecheck`
- [ ] Visual inspection in game
