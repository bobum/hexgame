# HexGame

A hex-based strategy game with Catlike Coding style terraced terrain.

## Project Structure

This is a monorepo containing multiple implementations:

```
hexgame/
├── web/          # Three.js/TypeScript web implementation
├── godot/        # Godot 4 implementation
├── .github/      # CI/CD workflows
├── .vscode/      # VS Code workspace settings
└── .opencode/    # OpenCode AI assistant config
```

## Implementations

### Web (Three.js + TypeScript)

The original implementation using Three.js for 3D rendering.

```bash
cd web
npm install
npm run dev
```

See [web/AGENTS.md](web/AGENTS.md) for details.

### Godot 4

Native game engine implementation for better performance and easier distribution.

1. Open Godot 4.3+
2. Import the `godot/` folder
3. Run the project (F5)

See [godot/AGENTS.md](godot/AGENTS.md) for details.

## Features

- **Hex Grid**: Axial coordinate system with cube coordinate conversions
- **Terraced Terrain**: Catlike Coding style stepped slopes between elevation levels
- **Procedural Generation**: Noise-based terrain with biome assignment
- **Pathfinding**: A* algorithm with terrain-aware movement costs
- **Unit System**: Turn-based movement with land/naval/amphibious domains

## Architecture

Both implementations share the same core design:

| Module | Purpose |
|--------|---------|
| Core | Hex coordinates, metrics, grid data |
| Generation | Procedural map generation |
| Rendering | Mesh building with terraces |
| Pathfinding | A* with domain awareness |
| Units | Unit types, management, turns |

## License

MIT
