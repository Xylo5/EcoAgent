# Road Generation System

## Script: Assets/Scripts/RoadGenerator.cs
Generates a straight-line road network creating city blocks. Attached to a GameObject in the scene.

### Inspector Fields
| Field | Type | Default | Notes |
|---|---|---|---|
| gridManager | GridManager | required | Reference to grid system |
| roadStart/End | CityTileData | required | Dead-end tiles |
| roadFiller1-5 | CityTileData | required | Straight road segments |
| roadIntersection | CityTileData | required | 4-way crossing |
| roadBranch | CityTileData | required | T-junction / corners |
| emptySpace | CityTileData | required | Non-blocking fill tile |
| horizontalRoads | int | 5 | Range 3-12 |
| verticalRoads | int | 5 | Range 3-12 |
| roadJitter | int | 1 | Range 0-2, subtle block size variation |
| borderMargin | int | 2 | Cells from grid edge (1 block = 2 cells) |
| randomSeed | int | 0 | 0 = random each run |
| generateOnStart | bool | true | Auto-generate on Play |
| fillEmptySpace | bool | true | Fill non-road areas |
| debugYOffset | float | 0.15 | Extra Y above terrain |
| lastComputedScale | float | readonly | Shows uniform scale applied |

### Generation Flow
1. `CacheGridSettings()` — reads gridWidth, gridHeight, cellSize from GridManager
2. `ClearMap()` — destroys all children
3. `InitGrids()` — creates roadMap and placedMap bool arrays
4. `GenerateRoadNetwork()` — paints straight horizontal/vertical roads on roadMap
5. `ComputeReferenceScale()` — measures filler1 bounds, computes uniform scale for all tiles
6. `InstantiateRoadTiles()` — classifies 2x2 blocks by neighbor count, picks tile + rotation
7. `FillEmptySpace()` — fills non-road cells with emptySpace (no grid blocking)

### Road Classification (by neighbor count)
| Neighbors | Tile | Rotation Logic |
|---|---|---|
| 0 | roadStart | 0° |
| 1 | roadStart (near edge) or roadEnd | Points away from neighbor. hasN→270°, hasE→0°, hasS→90°, hasW→180° |
| 2 straight | random filler | E-W→90°, N-S→0° |
| 2 corner | roadBranch | N+E→0°, E+S→90°, S+W→180°, N+W→270° |
| 3 | roadBranch (T-junction) | !S→0°, !W→90°, !N→180°, !E→270° |
| 4 | roadIntersection | 0° |

### Tile Instantiation (InstantiateTile)
1. Always uses `Instantiate()` (never PrefabUtility) so children can be modified
2. Computes world position via `GetWorldPosition()` (GridOrigin + cell offset + terrain height)
3. Applies uniform `referenceScale` (from filler1) to all tiles
4. Bounds-based centering: computes Renderer.bounds center offset, shifts root position
5. Sets layer to "Ignore Raycast"
6. Calls `OccupyCellsPermanent()` for road tiles at runtime only

### Scaling System
- `ComputeReferenceScale()` instantiates filler1 temporarily, measures its Renderer.bounds
- Computes: `referenceScale = (2 * cellSize) / maxBoundsSize`
- This same scale is applied to ALL road tile types (uniform appearance)
- `LogPrefabScales()` logs each prefab's native size + computed scale to Console

### Filler Weighting
- Filler1: weight 3 (~33%)
- Filler2: weight 3 (~33%)
- Filler3-5: weight 1 each (~11%)

### Road Network Layout
- Roads are evenly spaced within the border margin
- Slight random jitter (±roadJitter blocks) for natural block variation
- ~15% chance inner roads get trimmed (end early) from one side
- First/last roads always span full region
- roadStart tiles biased to IsNearEdge (borderMargin + 6 cells from edge)

### World Position Calculation (GetWorldPosition)
```
x = GridOrigin.x + cell.x * cellSize + (size * cellSize) * 0.5
z = GridOrigin.z + cell.y * cellSize + (size * cellSize) * 0.5
y = max(terrain height at 5 sample points) + terrain.position.y + debugYOffset
```
- 5-point sampling: center + 4 corners (prevents terrain clipping on slopes)
- Falls back to Terrain.activeTerrain if gridManager.terrain is null (edit mode)
- Matches BuildingPlacer.GridCellToWorldPos formula exactly

### Known Prefab Quirks
- Children have baked scene-space local positions (e.g., road mesh at localPos 227, 0, 289)
- Grass sub-mesh layers sit at local (0,0,0) but have mesh vertices at scene-space coords
- Both types produce identical world-space Renderer.bounds
- Bounds-based centering works for both types: offset = (bounds.center - targetPos) * scale
- DO NOT use child position averaging — fails due to mixed coordinate types
- DO NOT use PrefabUtility.InstantiatePrefab — prevents child modification

### CityTileData Assets (Assets/CityData/)
All road assets have `sizeInCells: 2`. Assigned via Inspector:
| Inspector Slot | Asset File | Prefab |
|---|---|---|
| roadStart | RoadStart_Grass.asset | Road Start Prefab |
| roadEnd | RoadEnd_Grass.asset | Road End Prefab |
| roadFiller1 | RoadFiller1.asset | Road Prefab 1 |
| roadFiller2 | RoadFiller2.asset | Road Prefab 2 |
| roadFiller3 | RoadFiller3.asset | Road Prefab 3 |
| roadFiller4 | RoadFiller4.asset | Road Prefab 4 |
| roadFiller5 | RoadFiller5.asset | Road Prefab 5 |
| roadIntersection | RoadIntersection_Grass.asset | Road Intersection |
| roadBranch | RoadBranch_Grass.asset | Road Branch |
| emptySpace | EmptySpace.asset | EmptySpace (sizeInCells: 1) |

## Editor: Assets/Editor/RoadGeneratorEditor.cs
Custom Inspector with 3 buttons:
- **Generate Roads** (green) — calls Generate()
- **Clear Roads** (red) — calls ClearMap()
- **Log Prefab Scales** (blue) — calls LogPrefabScales()
Also shows grid info box when GridManager is assigned.
