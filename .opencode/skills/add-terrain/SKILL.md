---
name: add-terrain
description: Add a new terrain/biome type to the hex map game. Covers texture setup, climate generation rules, and feature placement integration.
license: MIT
compatibility: opencode
metadata:
  version: "2.0"
  engine: "godot-4.5"
---

# Add Terrain Type

Add a new terrain/biome type to the hex map.

## Usage

```
/add-terrain <name>
```

## Current Terrain Types

Terrain is indexed by `TerrainTypeIndex` (0-4):

| Index | Name | Conditions |
|-------|------|------------|
| 0 | Sand | moisture < 0.2 OR underwater |
| 1 | Grass | moisture 0.2-0.8, elevation < 4 |
| 2 | Mud | moisture >= 0.8 |
| 3 | Stone | elevation 4-5 (hills), moisture <= 0.8 |
| 4 | Snow | elevation >= 6 OR wet hills (moisture > 0.8) |

## Steps

### 1. Add terrain texture

Create: `godot2/textures/terrain/{{name}}.png`

- Same dimensions as existing textures
- Seamless tiling pattern
- PNG format

### 2. Update TerrainTextureArray

File: `godot2/src/TerrainTextureArray.cs`

This is a **static class**. Add to the `TexturePaths` array:

```csharp
private static readonly string[] TexturePaths = new[]
{
    "res://textures/terrain/sand.png",   // Index 0
    "res://textures/terrain/grass.png",  // Index 1
    "res://textures/terrain/mud.png",    // Index 2
    "res://textures/terrain/stone.png",  // Index 3
    "res://textures/terrain/snow.png",   // Index 4
    "res://textures/terrain/{{name}}.png" // Index 5 - NEW
};
```

### 3. Update ClimateGenerator

File: `godot2/src/Generation/ClimateGenerator.cs`

Add logic to the **static** `GetBiome` method:

```csharp
/// <returns>TerrainTypeIndex: 0=Sand, 1=Grass, 2=Mud, 3=Stone, 4=Snow</returns>
private static int GetBiome(int elevation, float moisture)
{
    if (elevation < GenerationConfig.WaterLevel)
        return 0; // Sand (underwater)

    if (elevation >= GenerationConfig.MountainElevation)
        return 4; // Snow

    if (elevation >= GenerationConfig.HillElevation)
        return moisture > GenerationConfig.ForestMoistureMax ? 4 : 3;

    // === ADD NEW TERRAIN HERE ===
    if (moisture >= {{min}} && moisture < {{max}})
        return 5; // {{Name}}

    if (moisture < GenerationConfig.DesertMoistureMax)
        return 0; // Sand

    if (moisture >= GenerationConfig.ForestMoistureMax)
        return 2; // Mud

    return 1; // Grass
}
```

**Key**: Method signature is `private static int GetBiome(int elevation, float moisture)` - takes primitives, not CellData.

### 4. Update GenerationConfig

File: `godot2/src/Generation/GenerationConfig.cs`

```csharp
#region Biome Thresholds

public const float {{Name}}MoistureMin = {{value}};
public const float {{Name}}MoistureMax = {{value}};

#endregion
```

### 5. Update FeatureGenerator

File: `godot2/src/Generation/FeatureGenerator.cs`

Add cases to the private methods:

```csharp
private int GetPlantLevel(ref CellData cell)
{
    switch (cell.TerrainTypeIndex)
    {
        case 0: return 0; // Sand
        case 1: // Grass - existing logic
        case 2: // Mud - existing logic
        case 3: // Stone - existing logic
        case 4: return 0; // Snow
        case 5: return cell.Moisture > 0.5f ? 2 : 1; // {{Name}}
        default: return 0;
    }
}
```

Same pattern for `GetFarmLevel` and `GetUrbanLevel`.

### 6. Add tests

File: `godot2/tests/Generation/ClimateGeneratorTests.cs`

```csharp
[Fact]
public void Generate_{{Name}}Conditions_SetsTerrainIndex5()
{
    // Arrange
    var data = new[] { new CellData { X = 5, Z = 5, Elevation = 2 } };
    var generator = new ClimateGenerator(10, 10, 12345);

    // Act
    generator.Generate(data);

    // Assert - moisture is generated, check terrain assignment
    // For deterministic test, may need specific seed that produces target moisture
    data[0].TerrainTypeIndex.Should().BeInRange(0, 5);
}
```

## Architecture Reference

### Generation Pipeline

1. `LandGenerator` → sets `Elevation`
2. `ClimateGenerator` → sets `Moisture` and `TerrainTypeIndex`
3. `RiverGenerator` → sets river flags
4. `FeatureGenerator` → sets feature levels based on terrain
5. `RoadGenerator` → connects settlements

### Key Constants (GenerationConfig.cs)

```csharp
WaterLevel = 1           // Below = underwater
HillElevation = 4        // Stone terrain
MountainElevation = 6    // Snow terrain
DesertMoistureMax = 0.2  // Below = sand
ForestMoistureMax = 0.8  // Above = mud
```

## Checklist

- [ ] Created texture at `godot2/textures/terrain/{{name}}.png`
- [ ] Added path to `TerrainTextureArray.TexturePaths` array
- [ ] Added logic to `ClimateGenerator.GetBiome()`
- [ ] Added constants to `GenerationConfig` (if needed)
- [ ] Added cases to `FeatureGenerator` methods (if needed)
- [ ] Tests pass: `dotnet test godot2/tests`
- [ ] Visual inspection in Godot editor
