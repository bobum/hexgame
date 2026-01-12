# Skill: Run Benchmarks

Run performance benchmarks for the game systems.

## Usage

```
/benchmark [system]
```

## Commands

### Run all benchmarks

```bash
npm run bench
```

### Run specific benchmark

```bash
npx vitest run tests/benchmarks/performance.test.ts -t "pathfinding"
```

## Available Benchmarks

Located in `tests/benchmarks/performance.test.ts`:

### Map Generation
- Small map (20x15)
- Medium map (40x30)
- Large map (60x45)
- Max map (80x60)

### Pathfinding
- Short paths (10 hexes)
- Medium paths (30 hexes)
- Long paths (100 hexes)
- Reachable cells calculation

### Spatial Hash
- Insert performance
- Query performance
- Radius query scaling

## In-Game Stress Test

The game includes a built-in stress test accessible via the Debug UI:

1. Open the game
2. Expand "Performance" panel
3. Click "Run Stress Test"

This tests:
- Generation time at different map sizes
- Average FPS
- 1% low FPS
- Memory usage

## Performance Targets

| System | Target |
|--------|--------|
| Map generation (40x30) | < 500ms |
| Pathfinding (30 hex path) | < 5ms |
| Spatial query (radius 5) | < 1ms |
| Frame time | < 16ms (60 FPS) |

## Monitoring

### In-game stats

The Performance panel shows:
- FPS (current)
- Average frame time
- Max frame time
- 1% low FPS
- Generation time
- Memory usage (MB)

### Code access

```typescript
import { PerformanceMonitor } from './utils/PerformanceMonitor';

const monitor = new PerformanceMonitor();
monitor.recordFrame(deltaTime);

console.log(monitor.fps);
console.log(monitor.avgFrameTime);
console.log(monitor.onePercentLow);
```

## Profiling Tips

1. Use Chrome DevTools Performance tab
2. Look for long tasks (> 50ms)
3. Check for GC pressure (frequent spikes)
4. Monitor `renderer.info` for draw calls
