# HexGame - Godot 4 Implementation

This is the Godot 4 implementation of HexGame, a hex-based strategy game with terraced terrain.

## Project Structure

```
godot/
├── project.godot          # Godot project configuration
├── src/                   # GDScript source files
│   ├── main.gd            # Main entry point
│   ├── core/              # Core hex grid systems
│   │   ├── hex_metrics.gd # Hex geometry constants
│   │   ├── hex_coordinates.gd # Axial coordinate system
│   │   ├── hex_direction.gd # Direction utilities
│   │   ├── hex_grid.gd    # Grid data structure
│   │   ├── hex_cell.gd    # Cell data class
│   │   └── types.gd       # Terrain types and colors
│   ├── generation/        # Map generation
│   │   └── map_generator.gd # Procedural terrain generation
│   ├── rendering/         # Terrain rendering
│   │   ├── terrain_renderer.gd # Main terrain renderer
│   │   └── hex_mesh_builder.gd # Mesh construction
│   ├── pathfinding/       # A* pathfinding (TODO)
│   └── units/             # Unit management (TODO)
├── scenes/                # Godot scenes (.tscn)
│   └── main.tscn          # Main game scene
├── resources/             # Resources (materials, etc.)
└── assets/                # Raw assets (textures, models)
```

## Key Systems

### Core (src/core/)
- **HexMetrics**: Hex geometry constants matching Catlike Coding tutorial
- **HexCoordinates**: Axial (q, r) coordinate system with cube conversions
- **HexGrid**: Grid data structure managing all cells
- **HexCell**: Individual cell data (elevation, terrain, features)

### Generation (src/generation/)
- **MapGenerator**: Noise-based procedural terrain using FastNoiseLite

### Rendering (src/rendering/)
- **TerrainRenderer**: Manages mesh instances and materials
- **HexMeshBuilder**: Builds ArrayMesh with terraced slopes

## Running the Project

1. Open Godot 4.3+
2. Import this project folder
3. Open `scenes/main.tscn`
4. Press F5 to run

## Porting from Web Version

This implementation mirrors the TypeScript/Three.js version in `../web/`. When porting:

| Web (TypeScript) | Godot (GDScript) |
|-----------------|------------------|
| `THREE.Vector3` | `Vector3` |
| `THREE.Color` | `Color` |
| `BufferGeometry` | `ArrayMesh` |
| `class X { }` | `class_name X` |
| `interface` | `class_name` with properties |

## TODO

- [ ] Complete corner triangulation with all terrace cases
- [ ] Implement water rendering
- [ ] Add river system
- [ ] Port pathfinding system
- [ ] Port unit management
- [ ] Add camera controls
- [ ] Implement UI
