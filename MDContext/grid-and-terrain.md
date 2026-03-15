# Grid & Terrain System

## Terrain
- **Position**: (-50, 0, -50)
- **Size**: 350 x 350 world units
- **Scene**: Assets/Scenes/Level_0.unity

## GridManager (Assets/Scripts/GridManager.cs)
Manages a 100x100 cell grid centered on the terrain.

### Key Properties
| Property | Value | Notes |
|---|---|---|
| gridWidth | 100 | cells |
| gridHeight | 100 | cells |
| cellSize | 2.5 | forced in Awake(), may be 0 in edit mode |
| GridOrigin | computed | world-space bottom-left corner of playable grid |

### GridOrigin Calculation
```
offsetX = (terrainData.size.x - gridWidth * cellSize) * 0.5 = (350 - 250) * 0.5 = 50
offsetZ = (terrainData.size.z - gridHeight * cellSize) * 0.5 = (350 - 250) * 0.5 = 50
GridOrigin = terrainPos + (offsetX, 0, offsetZ) = (-50+50, 0, -50+50) = (0, 0, 0)
```
- Grid spans world coords (0, 0, 0) to (250, 0, 250)
- GridOrigin property has edit-mode fallback: uses cellSize 2.5 if serialized value is 0
- Auto-finds Terrain in edit mode if not assigned

### Cell Occupancy
- `occupiedCells[,]` — buildings, temporary. Freed by `FreeCells()`
- `permanentCells[,]` — roads, environment. Never freed by `FreeCells()`
- Both arrays initialized in `Awake()` — **null in edit mode**
- `OccupyCellsPermanent()` must only be called at runtime (guard with `Application.isPlaying`)

### Edit Mode Gotchas
- `cellSize` is 0 until Awake runs → always use fallback: `cellSize > 0 ? cellSize : 2.5f`
- `terrain` field may be null if not Inspector-assigned → use `Terrain.activeTerrain` fallback
- `occupiedCells`/`permanentCells` are null → skip OccupyCellsPermanent calls
- `GridOrigin` property handles all these internally with fallbacks

### Key Methods
| Method | Notes |
|---|---|
| GetCellWorldCenter(cell) | Single cell center in world space |
| SnapToGrid(worldPos, size) | Snap world pos to nearest cell |
| WorldToGrid(worldPos) | World → grid cell |
| IsAreaAvailable(cell, size) | Check if cells are free |
| OccupyCells(cell, size) | Mark cells occupied (temporary) |
| FreeCells(cell, size) | Free cells (skips permanent) |
| OccupyCellsPermanent(cell, size) | Mark cells permanent (roads/env) |
| IsCellPermanent(cell) | Check if permanently blocked |

### World Position Formula (used by BuildingPlacer, RoadGenerator)
```
x = GridOrigin.x + cell.x * cellSize + (size * cellSize) / 2
z = GridOrigin.z + cell.y * cellSize + (size * cellSize) / 2
y = terrain.SampleHeight(x, z) + terrain.position.y
```

## Border Forest (Assets/Scripts/BorderForestSpawner.cs)
Spawns decorative trees in the terrain area OUTSIDE the playable grid.
- Border width: `(terrainSize - gridSize) / (2 * cellSize)` = (350-250)/(2*2.5) = 20 cells
- Trees are placed in these 20-cell-wide strips on all 4 sides
- Trees skip cells inside the grid area
- Road borderMargin of 2 cells keeps roads 1 block from grid edge (visual separation from trees)

## Coordinate Reference
```
Terrain: (-50, 0, -50) ────────────────────── (300, 0, -50)
         │  20-cell tree border                        │
         │  ┌──────────────────────────────┐           │
         │  │ Grid (0,0,0) to (250,0,250)  │           │
         │  │                              │           │
         │  │  Road area:                  │           │
         │  │  cell(2,2) to cell(97,97)    │           │
         │  │  with borderMargin=2         │           │
         │  │                              │           │
         │  └──────────────────────────────┘           │
         │                                             │
(-50, 0, 300) ──────────────────────────── (300, 0, 300)
```
