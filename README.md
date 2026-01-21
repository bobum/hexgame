# HexGame

A hex-based strategy game built in Godot 4 with C#, implementing the [Catlike Coding Hex Map tutorials](https://catlikecoding.com/unity/tutorials/hex-map/).

## About

This project is a direct port of Jasper Flick's excellent Unity Hex Map tutorial series to Godot 4 with C#. The tutorials cover creating a full-featured hex-based map system with terraced terrain, rivers, roads, water bodies, terrain features, and walls.

### Catlike Coding Tutorials Implemented

| Tutorial | Status | Description |
|----------|--------|-------------|
| [Part 1: Creating a Hexagonal Grid](https://catlikecoding.com/unity/tutorials/hex-map/part-1/) | Done | Basic hex grid with axial coordinates |
| [Part 2: Blending Cell Colors](https://catlikecoding.com/unity/tutorials/hex-map/part-2/) | Done | Color blending between cells |
| [Part 3: Elevation and Terraces](https://catlikecoding.com/unity/tutorials/hex-map/part-3/) | Done | Stepped terrain with terraces |
| [Part 4: Irregularity](https://catlikecoding.com/unity/tutorials/hex-map/part-4/) | Done | Noise-based vertex perturbation |
| [Part 5: Larger Maps](https://catlikecoding.com/unity/tutorials/hex-map/part-5/) | Done | Chunk-based rendering system |
| [Part 6: Rivers](https://catlikecoding.com/unity/tutorials/hex-map/part-6/) | Done | River channels carved into terrain |
| [Part 7: Roads](https://catlikecoding.com/unity/tutorials/hex-map/part-7/) | Done | Road network rendering |
| [Part 8: Water](https://catlikecoding.com/unity/tutorials/hex-map/part-8/) | Done | Water bodies and shores |
| [Part 9: Terrain Features](https://catlikecoding.com/unity/tutorials/hex-map/part-9/) | Done | Urban, farm, and plant features |
| [Part 10: Walls](https://catlikecoding.com/unity/tutorials/hex-map/part-10/) | Done | City walls with gaps for rivers/roads |

## Project Structure

```
hexgame/
├── godot2/           # Godot 4.3+ C# implementation
│   ├── src/          # C# source files
│   ├── scenes/       # Godot scenes
│   └── assets/       # Textures, materials
├── tests/            # .NET 9 xUnit tests
│   └── HexGame.Tests/
└── .github/          # CI/CD workflows
```

## Requirements

- **Godot 4.3+** with .NET support
- **.NET 9 SDK** (for running tests)

## Getting Started

### Running the Game

1. Open Godot 4.3+ (.NET version)
2. Import the `godot2/` folder as a project
3. Run the project (F5)

### Running Tests

```bash
cd tests/HexGame.Tests
dotnet test
```

## Controls

- **L** - Toggle coordinate labels

## Features

- **Hex Grid**: Axial coordinate system with cube coordinate conversions
- **Terraced Terrain**: Catlike Coding style stepped slopes between elevation levels
- **Rivers**: Carved river channels with water surface rendering
- **Roads**: Road network with proper intersection handling
- **Water Bodies**: Lakes and oceans with shore blending
- **Terrain Features**: Urban buildings, farms, and vegetation
- **Walls**: City walls that respect rivers, roads, and terrain

## Credits

- **Jasper Flick** - [Catlike Coding](https://catlikecoding.com/) for the original Unity tutorials
- This project is an educational port to learn both the hex map concepts and Godot/C# development

## License

MIT
