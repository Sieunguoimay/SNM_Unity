# Patch-Based Grass Placement

## Overview

The patch system provides a scene-based workflow for placing grass with full control over where blades go, what mesh they use, their size, and rotation. Instead of a single uniform grid, you place **GrassPatch** components as children of a GrassSystem — each patch defines a rectangular area of grass with its own mesh, material, density, and scale settings.

Patches are the recommended way to build grass layouts for levels. The older grid-based approach (placement map on the GrassSystem itself) still works as a fallback when no patches are present.

---

## How It Works

```
Scene Hierarchy:

  [GrassSystem]                          <-- existing component, collects child patches
    |-- [GrassPatch] "Meadow"            <-- large area, dense short grass
    |-- [GrassPatch] "Riverbank"         <-- thin strip, tall reeds mesh
    |-- [GrassPatch] "Flower Bed"        <-- small area, flower mesh, sparse
    |-- [GrassPatch] "Path Edge"         <-- narrow rectangle, low scale
```

At runtime (`OnEnable`), GrassSystem finds all child GrassPatch components, asks each to build its `Matrix4x4[]` instance data, groups them by mesh+material, and creates one draw call per group. All existing features (trample, wind, AO, color variation, frustum culling, recovery spring) work automatically across all patches.

If no GrassPatch children exist, GrassSystem falls back to the legacy grid-based path. No breaking changes.

---

## GrassPatch Component

### Fields

| Field | Default | Description |
|-------|---------|-------------|
| **Mesh** | — | Grass blade mesh for this patch. Required. |
| **Material** | — | Material using the InteractiveGrass shader. Required. |
| **Area Size** | 5 x 5 | Local XZ dimensions of the rectangular patch area. |
| **Cell Spacing** | 0.5 | Distance between grid cells in world units. Smaller = denser. |
| **Jitter** | 0.4 | Random XZ offset per blade as a fraction of cell spacing (0-1). 0 = perfect grid, 1 = maximum scatter. |
| **Random Seed** | 0 | Seed for reproducible placement. Same seed = same positions every time. |
| **Min Scale** | 0.8 | Minimum uniform scale applied to each blade. |
| **Max Scale** | 1.2 | Maximum uniform scale applied to each blade. |
| **Min Yaw** | 0 | Minimum Y-axis rotation (degrees). |
| **Max Yaw** | 360 | Maximum Y-axis rotation (degrees). |
| **Placement Map** | null | Optional texture for fine control. R=density, G=rotation, B=scale. Same convention as the grid-based system. |
| **Density Threshold** | 0.1 | When using a placement map, pixels with R below this are skipped. |
| **Raycast Mask** | Everything | Layer mask for terrain height snapping. |
| **Raycast Origin Height** | 50 | How far above the patch Y to start the downward ray. |
| **Raycast Max Distance** | 100 | Maximum ray distance. |

### Terrain Height Snapping

Every blade position raycasts straight down to find the terrain surface. The blade's Y position is set to the hit point. If the ray misses (no collider below), the blade is discarded. This means:

- Grass automatically conforms to hills, slopes, and uneven terrain.
- You only need to position the patch roughly — the Y position is refined per-blade.
- Patches placed over empty space (no colliders) produce no blades.

Set `Raycast Mask` to limit which layers are considered terrain. For example, exclude water colliders so grass doesn't spawn on the water surface.

### Distribution: Grid + Jitter

Blades are placed on a uniform grid within the patch area, then each blade is randomly offset by up to `jitter * cellSpacing * 0.5` in XZ. This gives:

- **Predictable density** — you know roughly how many blades to expect: `(areaSize.x / cellSpacing) * (areaSize.y / cellSpacing)`.
- **Natural look** — jitter breaks the grid regularity so it doesn't look artificial.
- **Reproducible** — same `randomSeed` always produces the same layout.

### Placement Map (Optional)

For fine-grained control within a patch, assign a placement map texture:

| Channel | Controls | Range |
|---------|----------|-------|
| **R** | Density — whether a blade spawns | 0 = skip, >threshold = spawn |
| **G** | Yaw rotation | 0-255 maps to minYaw-maxYaw |
| **B** | Scale | 0-255 maps to minScale-maxScale |

The texture is sampled in the patch's local UV space (0,0 = bottom-left corner, 1,1 = top-right corner). When no placement map is assigned, rotation and scale are randomized within the configured ranges.

Import the texture with **Point** filter, **No Compression**, and **Read/Write Enabled**.

---

## Setup Guide

### Basic Setup (single grass type)

1. Create a GameObject, add the `GrassSystem` component. Configure its feature settings (wind, trample, AO, etc.) as usual.
2. Create a child GameObject under it. Add the `GrassPatch` component.
3. Assign a grass blade **Mesh** and **Material** to the patch.
4. Set **Area Size** to the desired coverage area.
5. Position the patch GameObject where you want the grass. The Transform position defines the center of the patch.
6. Adjust **Cell Spacing** for density (smaller = more blades).
7. Enter Play Mode.

### Multiple grass types

1. Create several child GameObjects under GrassSystem, each with a GrassPatch.
2. Assign different meshes to each — e.g., short grass, tall grass, flowers.
3. Position and size each patch independently.
4. Patches with the **same mesh+material** are batched into a single draw call automatically. Patches with different meshes get separate draw calls.

### Overlapping patches

Patches can overlap. Blades from both patches will render in the overlapping region. This is useful for mixing grass types — e.g., a sparse flower patch overlapping a dense short grass patch.

### Working with terrain

1. Set **Raycast Mask** to your terrain layer (e.g., "Ground").
2. Position the patch above the terrain (the exact Y doesn't matter much — blades snap down).
3. Set **Raycast Origin Height** high enough to clear your terrain's highest point relative to the patch.
4. Blades over gaps, cliffs, or water (if water is on a different layer) are automatically discarded.

---

## Scene View Feedback

When a GrassPatch is **selected** in the Scene view:

- **Green wireframe box** — shows the patch area boundary.
- **Filled green overlay** — semi-transparent fill showing the patch footprint.
- **Mesh preview** — the assigned blade mesh is rendered at the patch center for reference.
- **Blade count label** — estimated number of blades shown above the patch.
- **Dot grid** — sampled blade positions shown as green dots (up to ~200 for performance).
- **Edge handles** — drag the edges of the patch to resize the area directly in the Scene view.

---

## How Grouping Works

At runtime, `GrassPatchCollector` iterates all patches and groups their matrices by `(Mesh, Material)` pair. Each unique combination becomes one `GrassRenderer` (one `RenderMeshIndirect` draw call). This means:

- 5 patches with the same mesh+material = 1 draw call (efficient).
- 3 patches with mesh A + 2 patches with mesh B = 2 draw calls.
- Each draw call gets its own frustum culling pass.

The combined world bounds of all patches define the `SurfaceCanvas` used for trample and wind mapping.

---

## Interaction with Existing Features

All GrassSystem features work with patches:

| Feature | Behavior with Patches |
|---------|----------------------|
| **Trample** | Shared trample map covers the combined bounds of all patches. Disturbers affect all patches. |
| **Wind** | Wind map and parameters are shared across all patch materials. |
| **Frustum Culling** | Applied per-renderer (per mesh+material group). |
| **Color Variation** | Applied per-material. All patches sharing a material get the same color range. |
| **Ambient Occlusion** | Applied per-material. |
| **Recovery Spring** | Works identically — spring parameters are shared. |

---

## Fallback Behavior

| Scenario | What Happens |
|----------|-------------|
| GrassPatch children present | Patch-based path. GrassSystem config's mesh/material/gridSize are ignored for placement. Feature configs (wind, trample, etc.) still apply. |
| No GrassPatch children | Legacy grid-based path. Behaves exactly as before (placement map on GrassSystem, layers, etc.). |

---

## Performance Notes

- Each patch calls `Physics.Raycast` per grid cell during `OnEnable` — this is a one-time cost at startup, not per-frame.
- For very large areas (10,000+ cells per patch), the startup raycast time is noticeable. Consider splitting into multiple smaller patches.
- Frustum culling runs per-instance per-frame on the CPU. The total instance count across all patches determines the CPU cost.
- Blade count estimate: `(areaSize.x / cellSpacing) * (areaSize.y / cellSpacing)` per patch, minus any discarded by raycast misses or placement map density.
