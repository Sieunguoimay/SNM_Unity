# Placement Map & Multi-Mesh Layers

## Overview

The grass system supports two features that work together:

1. **Placement Map** — a texture that controls where grass grows, how it rotates, and how tall it is. No code needed per-field.
2. **Multi-Mesh Layers** — multiple grass types (tall, short, flowers, weeds) in the same field, each with their own mesh and material.

Both are optional. Without them the system falls back to a uniform grid of identical blades (original behavior).

---

## Placement Map

### What it does

Each pixel in the texture maps to one grid cell. The texture's RGB channels encode per-blade properties:

| Channel | Controls | Range |
|---------|----------|-------|
| **R** | Density — whether grass spawns here | 0 = empty, 255 = grass |
| **G** | Yaw rotation | 0-255 maps to 0-360 degrees |
| **B** | Height scale | 0-255 maps to `minScale`-`maxScale` |

Grid size is derived from the texture dimensions (e.g. a 100x100 texture = 100x100 grid).

### Setup

1. Create a texture at your desired grid resolution (e.g. 50x50, 100x100).
2. Paint using the channel meanings above. A simple approach: paint white where you want grass, black where you don't.
3. In Unity, set the texture import settings:
   - **Filter Mode:** Point (no filter)
   - **Compression:** None or RGBA32
   - **Read/Write:** Enabled
4. On your `GrassSystem` component, assign the texture to `Placement Map`.
5. Adjust `Density Threshold` (default 0.1) — pixels with R below this value won't spawn grass.
6. Adjust `Min Scale` / `Max Scale` to control the height range the B channel maps to.

### Tips

- When no placement map is assigned, `Grid Size` controls the field dimensions as before.
- When a placement map is assigned, `Grid Size` is ignored — the texture dimensions are used instead.
- `Cell Spacing` still controls world-space distance between blades regardless of mode.

---

## Multi-Mesh Layers

### What it does

Instead of rendering one grass mesh everywhere, you define **layers**. Each layer has its own mesh, material, and reads a different channel from the placement map for density. This lets you paint exactly where each grass type appears.

Each layer is a separate draw call sharing the same interaction system (trample, wind).

### Channel assignment (up to 4 layers)

| Channel | Suggested use |
|---------|--------------|
| R | Primary grass (tall) |
| G | Secondary grass (short/weeds) |
| B | Flowers / accent |
| A | Reserved / extra layer |

### Setup

1. Create a placement map where each channel represents a different grass type. For example, paint R for tall grass areas and G for flower areas.
2. On your `GrassSystem` component, expand the **Layers** array and add entries.
3. For each layer, configure:
   - **Name** — label for your reference
   - **Mesh** — the grass blade mesh for this layer
   - **Material** — the material (must use the InteractiveGrass shader or compatible)
   - **Density Channel** — which placement map channel to read (0=R, 1=G, 2=B, 3=A)
   - **Density Threshold** — minimum channel value to spawn (0-1)
   - **Min Scale / Max Scale** — height scale range for this layer
   - **Yaw Random Seed** — offset so blades in different layers don't share identical rotations
4. Leave the top-level `Grass Mesh` / `Grass Material` fields as-is — they are only used when the Layers array is empty (single-mesh fallback).

### How interaction works with layers

- **Trample** and **wind** are shared across all layers. Each layer's material receives the same `_TrampleMap` and `_WindMap` textures. No extra configuration needed.
- The trample map resolution matches the placement map dimensions (or `Grid Size` if no placement map).

### Fallback behavior

- **Layers empty + no placement map** — uniform grid of identical blades (original behavior).
- **Layers empty + placement map assigned** — single mesh, placement-map-driven density/rotation/scale.
- **Layers populated + placement map assigned** — full multi-mesh, channel-per-layer mode.
- **Layers populated + no placement map** — all layers place grass at every cell (a warning is logged). Assign a placement map to get channel-based filtering.

---

## Quick-Start Example

1. In Photoshop/GIMP, create a 64x64 image.
2. On the R channel, paint a circle in the center (tall grass area).
3. On the G channel, paint scattered dots around it (flowers).
4. Save as PNG, import into Unity with Point filter + Read/Write enabled.
5. Create a `GrassSystem` GameObject.
6. Assign the texture to `Placement Map`.
7. Add 2 layers:
   - Layer 0: tall grass mesh, density channel = 0 (R)
   - Layer 1: flower mesh, density channel = 1 (G)
8. Enter Play mode — tall grass appears in the circle, flowers appear at the dots, trample works on both.
