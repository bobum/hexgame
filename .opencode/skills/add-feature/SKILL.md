---
name: add-feature
description: Add a new special feature (castle, ziggurat, megaflora) to the hex map game. Covers prefab creation, placement rules, and HexFeatureManager integration.
license: MIT
compatibility: opencode
metadata:
  version: "3.0"
  engine: "godot-4.5"
---

# Add Special Feature

Add a new special feature type to the hex map.

## Usage

```
/add-feature <name>
```

## Current Special Features

| SpecialIndex | Name | Terrain | Condition |
|--------------|------|---------|-----------|
| 0 | None | - | - |
| 1 | Castle | Grass/Stone | elevation >= 4 |
| 2 | Ziggurat | Sand | always |
| 3 | Megaflora | Mud | moisture > 0.7 |

**Note**: Current code has 3 special features. Array size is 3 in HexFeatureManager.

## Steps

### 1. Create prefab scene

File: `godot2/prefabs/features/special/{{name}}.tscn`

```
{{Name}} (Node3D)
├── MeshInstance3D
│   └── StandardMaterial3D
└── (optional) CollisionShape3D
```

### 2. Update HexFeatureManager.PreloadPrefabs()

File: `godot2/src/HexFeatureManager.cs` (line ~74)

Increase array size and add new prefab:

```csharp
// Special prefabs - currently 3, increase to 4
_specialPrefabsCache = new PackedScene[4];  // Was 3
_specialPrefabsCache[0] = GD.Load<PackedScene>("res://prefabs/features/special/castle.tscn");
_specialPrefabsCache[1] = GD.Load<PackedScene>("res://prefabs/features/special/ziggurat.tscn");
_specialPrefabsCache[2] = GD.Load<PackedScene>("res://prefabs/features/special/megaflora.tscn");
_specialPrefabsCache[3] = GD.Load<PackedScene>("res://prefabs/features/special/{{name}}.tscn");  // NEW
```

### 3. Update HexFeatureManager.Initialize() fallback

Same file (line ~149), update the fallback:

```csharp
_specialPrefabs = new PackedScene[4];  // Was 3
_specialPrefabs[0] = GD.Load<PackedScene>("res://prefabs/features/special/castle.tscn");
_specialPrefabs[1] = GD.Load<PackedScene>("res://prefabs/features/special/ziggurat.tscn");
_specialPrefabs[2] = GD.Load<PackedScene>("res://prefabs/features/special/megaflora.tscn");
_specialPrefabs[3] = GD.Load<PackedScene>("res://prefabs/features/special/{{name}}.tscn");  // NEW
```

### 4. Update GetSpecialFeatureIndex()

File: `godot2/src/Generation/FeatureGenerator.cs` (line 251)

This is the key method that determines which special feature to place. Add your feature's logic:

```csharp
internal int GetSpecialFeatureIndex(ref CellData cell)
{
    switch (cell.TerrainTypeIndex)
    {
        case 0: // Sand (desert) - Ziggurat
            return 2;

        case 1: // Grass (plains) - Castle (high elevation only)
            if (cell.Elevation >= GenerationConfig.CastleMinElevation)
                return 1;
            // NEW: {{Name}} on grass with specific condition
            if (cell.{{Condition}})
                return 4;
            return 0;

        case 2: // Mud (jungle) - Megaflora (high moisture only)
            if (cell.Moisture > GenerationConfig.MegafloraMoistureThreshold)
                return 3;
            return 0;

        case 3: // Stone (hills) - Castle (high elevation only)
            if (cell.Elevation >= GenerationConfig.CastleMinElevation)
                return 1;
            return 0;

        case 4: // Snow - no special features
            // NEW: Or add {{Name}} for snow terrain
            // if (cell.{{Condition}})
            //     return 4;
            return 0;

        default:
            return 0;
    }
}
```

**Key points**:
- Return value is SpecialIndex (1-based): 1=Castle, 2=Ziggurat, 3=Megaflora, 4={{Name}}
- Array index = SpecialIndex - 1 (handled in AddSpecialFeature)
- Add conditions within existing terrain cases, or add new terrain handling

### 5. Add configuration constants (if needed)

File: `godot2/src/Generation/GenerationConfig.cs`

```csharp
#region Feature Placement

/// <summary>
/// Condition threshold for {{Name}} placement.
/// </summary>
public const float {{Name}}Threshold = {{value}};

#endregion
```

### 6. Add tests

File: `godot2/tests/Generation/FeatureGeneratorTests.cs`

```csharp
[Fact]
public void GetSpecialFeatureIndex_{{Name}}Conditions_Returns4()
{
    // Arrange
    var cell = new CellData
    {
        TerrainTypeIndex = {{terrainIndex}},
        Elevation = {{elevation}},
        Moisture = {{moisture}}
    };
    var rng = new Random(42);
    var generator = new FeatureGenerator(rng);

    // Act
    var result = generator.GetSpecialFeatureIndex(ref cell);

    // Assert
    result.Should().Be(4);
}
```

## Architecture Reference

### How Special Features Work

1. `PlaceSpecialFeatures()` iterates cells (line 96)
2. Calls `CanPlaceFeature()` - checks not underwater, no rivers (line 130)
3. Random chance check via `SpecialFeatureChance` (line 110)
4. Calls `GetSpecialFeatureIndex()` to determine which feature (line 114)
5. Sets `cell.SpecialIndex` and clears density features (lines 117-121)

### AddSpecialFeature() in HexFeatureManager (line 683)

```csharp
public void AddSpecialFeature(HexCell cell, Vector3 position)
{
    int index = cell.SpecialIndex - 1;  // 1-based to 0-based
    if (index < 0 || index >= _specialPrefabs.Length || _specialPrefabs[index] == null)
        return;

    var feature = _specialPrefabs[index].Instantiate<Node3D>();
    feature.Position = HexMetrics.Perturb(position);
    feature.RotationDegrees = new Vector3(0f, 360f * hash.e, 0f);
    _container.AddChild(feature);
}
```

### Constraints

- Cannot place on underwater cells
- Cannot place on cells with rivers
- Placing special feature clears PlantLevel, FarmLevel, UrbanLevel
- Setting `SpecialIndex > 0` in HexCell clears roads

## Checklist

- [ ] Created prefab at `godot2/prefabs/features/special/{{name}}.tscn`
- [ ] Increased array size in `PreloadPrefabs()` from 3 to 4
- [ ] Added prefab load in `PreloadPrefabs()` at index [3]
- [ ] Increased array size in `Initialize()` fallback from 3 to 4
- [ ] Added prefab load in `Initialize()` at index [3]
- [ ] Added return case in `GetSpecialFeatureIndex()` returning 4
- [ ] Added config constants (if needed)
- [ ] Tests pass: `dotnet test godot2/tests`
- [ ] Feature appears in generated maps
