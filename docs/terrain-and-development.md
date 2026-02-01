# Terrain Types and Development Levels

## Overview

Each hex in the game world has two key attributes:
1. **Terrain Type** - The biome/ground type (affects visuals and gameplay)
2. **Development Level** - How built-up/populated the hex is (0-4)

The combination of these two attributes determines what models and features appear on each hex.

---

## Terrain Types

Current terrain types (TerrainTypeIndex in code):

| Index | Name | Description | Typical Location |
|-------|------|-------------|------------------|
| 0 | **Sand** | Beach sand, desert | Coastlines, beaches, arid areas |
| 1 | **Grass** | Grassland, plains | Inland areas, tropical lowlands |
| 2 | **Mud** | Swamp, jungle floor, wet earth | River deltas, rainforest, wetlands |
| 3 | **Stone** | Rocky terrain, volcanic | Hills, mountains, volcanic areas |
| 4 | **Snow** | Snow-covered, high altitude | Mountain peaks, high elevation |

### Terrain Generation Notes
- Terrain is assigned based on elevation + moisture in `ClimateGenerator.cs`
- Underwater cells use Mud (ocean floor)
- Beach level (water level elevation) uses Sand
- High elevation (hills) uses Stone or Snow based on moisture
- Land biomes use moisture thresholds: Desert < 0.2, Jungle/Swamp >= 0.8, Grassland in between

---

## Development Levels

5-tier system representing urbanization density:

| Tier | Name | Density | Description | Real-World Example |
|------|------|---------|-------------|--------------------|
| 0 | **Wilderness** | None | Untouched nature, no structures | National parks, remote beaches |
| 1 | **Rural/Village** | Very Low | Scattered small structures, farms | Adelaide Village, fishing villages |
| 2 | **Suburban** | Low-Medium | Residential neighborhoods, local shops | Carmichael, residential areas |
| 3 | **Urban** | Medium-High | Mid-rise buildings, commercial zones | Palmdale, Chippingham |
| 4 | **Dense Urban** | High | High-rises, downtown core, ports | Downtown Nassau, Paradise Island |

### Development Distribution (Nassau Example)
- **Dense Urban Core**: ~10-11 hexes clustered on east end
- **Urban**: Ring around downtown, spreading west
- **Suburban**: Further west, residential neighborhoods
- **Rural/Village**: Western tip, scattered settlements
- **Wilderness**: Undeveloped areas, nature preserves

---

## Terrain + Development Matrix

This matrix defines what assets/models should appear on each hex based on the combination of terrain type and development level.

### Sand (Beach/Desert)

| Dev Level | Assets | Examples |
|-----------|--------|----------|
| 0 - Wilderness | Palm trees, driftwood, sea grass, crabs | Empty beach |
| 1 - Rural | Beach shacks, small docks, fishing boats, nets | Fishing village |
| 2 - Suburban | Beach houses, small resorts, boardwalks | Beach community |
| 3 - Urban | Hotels, restaurants, piers, marinas | Tourist area |
| 4 - Dense Urban | Large resorts, cruise port, commercial waterfront | Nassau waterfront |

### Grass (Plains/Tropical Lowland)

| Dev Level | Assets | Examples |
|-----------|--------|----------|
| 0 - Wilderness | Tropical trees, bushes, flowers, wildlife | Jungle/forest |
| 1 - Rural | Small farms, cottages, dirt roads, livestock | Farming village |
| 2 - Suburban | Houses, schools, churches, small plazas | Residential neighborhood |
| 3 - Urban | Apartments, office buildings, shopping centers | Commercial district |
| 4 - Dense Urban | High-rises, skyscrapers, dense commercial | Downtown core |

### Mud (Swamp/Jungle Floor)

| Dev Level | Assets | Examples |
|-----------|--------|----------|
| 0 - Wilderness | Mangroves, ferns, vines, swamp flora | Wetland/swamp |
| 1 - Rural | Stilted houses, small docks, fishing huts | Swamp village |
| 2 - Suburban | Elevated houses, boardwalks, small bridges | Wetland community |
| 3 - Urban | Industrial, water treatment, elevated roads | Developed wetland |
| 4 - Dense Urban | Port facilities, industrial waterfront | Harbor district |

### Stone (Rocky/Volcanic)

| Dev Level | Assets | Examples |
|-----------|--------|----------|
| 0 - Wilderness | Rock formations, sparse vegetation, caves | Rocky hillside |
| 1 - Rural | Mountain huts, mining shacks, trails | Remote settlement |
| 2 - Suburban | Hillside homes, terraced buildings | Hill community |
| 3 - Urban | Built into hills, retaining walls, tunnels | Hillside city |
| 4 - Dense Urban | Fortress-style buildings, observation decks | Dramatic urban |

### Snow (High Altitude)

| Dev Level | Assets | Examples |
|-----------|--------|----------|
| 0 - Wilderness | Snow, ice, sparse alpine plants | Mountain peak |
| 1 - Rural | Mountain cabin, ski hut | Remote outpost |
| 2 - Suburban | Lodge, small resort | Mountain village |
| 3 - Urban | Ski resort, research station | Mountain town |
| 4 - Dense Urban | Large resort complex | Major ski destination |

*Note: Snow terrain is rare in Caribbean setting - may be limited to volcanic peaks or removed*

---

## Asset Requirements Summary

### Flora (by terrain, all development levels)

| Terrain | Level 1 (Small) | Level 2 (Medium) | Level 3 (Large) |
|---------|-----------------|------------------|-----------------|
| Sand | Sea grass, small palms | Coconut palms, sea grape | Large palms, beach almond |
| Grass | Shrubs, flowers | Banana plants, ferns | Large tropical trees, kapok |
| Mud | Water plants, reeds | Mangroves, bamboo | Large mangroves, swamp trees |
| Stone | Lichens, small plants | Hardy shrubs, agave | Cliff plants, small trees |
| Snow | Alpine grass | Pine shrubs | Alpine trees |

### Structures (by development level)

| Level | Small | Medium | Large |
|-------|-------|--------|-------|
| 1 - Rural | Shacks, huts | Small houses, barns | Farmhouses, small docks |
| 2 - Suburban | Houses, garages | Schools, churches | Shopping plazas, parks |
| 3 - Urban | Townhouses | Apartments, offices | Malls, mid-rises |
| 4 - Dense | Towers, commercial | High-rises, hotels | Skyscrapers, port facilities |

---

## Implementation Notes

### Current Code Structure
- `HexCell.TerrainTypeIndex` - Stores terrain type (0-4)
- `HexCell.UrbanLevel` - Currently 0-3, needs expansion to 0-4
- `HexFeatureManager` - Loads prefabs from `prefabs/features/` directories

### Proposed Changes
1. Expand `UrbanLevel` range from 0-3 to 0-4
2. Rename feature directories to match new system
3. Create terrain-specific subdirectories:
   ```
   prefabs/features/
   ├── flora/
   │   ├── sand/
   │   │   ├── level1/
   │   │   ├── level2/
   │   │   └── level3/
   │   ├── grass/
   │   ├── mud/
   │   ├── stone/
   │   └── snow/
   └── structures/
       ├── level1/  (rural)
       ├── level2/  (suburban)
       ├── level3/  (urban)
       └── level4/  (dense urban)
   ```

### Asset Sources (2030+ Modern Tropical Style)
- **Synty Studios** - Polygon City, Polygon Nature (low-poly, cohesive style)
- **Kenney.nl** - Free city and nature kits
- **Kay Lousberg** - Free low-poly packs on itch.io

---

## Visual Reference

Nassau, New Providence Island development gradient:
- East: Dense Urban (Downtown Nassau, Paradise Island)
- Central: Urban/Suburban (Palmdale, Victoria Gardens)
- West: Rural/Wilderness (Adelaide, Lyford Cay area)

The game should replicate this natural gradient where settlements cluster around ports/resources and density decreases with distance from the core.
