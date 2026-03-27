# Patch Placement — Implementation Plan

## Problem

The grass system's placement is limited to a single uniform grid centered on the GrassSystem MonoBehaviour, controlled by an optional placement map texture. This is too rigid for level design:

- Can't place grass in arbitrary non-rectangular regions.
- Can't use different meshes in different world areas without separate GrassSystem instances.
- Can't iterate quickly on layout by moving/resizing in the Scene view.
- No terrain height conformity — blades sit on a flat plane.

## Solution

A **patch-based authoring system** where `GrassPatch` MonoBehaviours placed as children of `GrassSystem` define rectangular areas of grass. Each patch has independent mesh, material, density, scale, and rotation settings. At runtime, patches are collected, their matrices are grouped by mesh+material, and fed into the existing `GrassRenderer` pipeline.

---

## Architecture

### Data Flow

```
Edit Time                             Runtime (OnEnable)
---------                             -------------------
GrassPatch[] (child MBs)         -->  GrassPatchCollector.Collect()
  each: transform, mesh,                |
        material, area, density,         +--> BuildMatrices() per patch
        scale, rotation,                 |      grid + jitter
        raycast settings                 |      raycast down for height
                                         |      placement map sampling
                                         |
                                         +--> Group by (Mesh, Material)
                                         |
                                         +--> GrassSystemFactory.CreateFromPatches()
                                                |
                                                +--> GrassRenderer per group
                                                +--> Features (trample, wind, AO, ...)
                                                +--> UpdateDispatcher
```

### Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Authoring primitive | MonoBehaviour on child GameObjects | Unity-native: drag, position, duplicate, undo in Scene view |
| Patch shape | Rectangle | Simple, predictable. Circle/polygon can be added later. |
| Distribution | Grid + jitter | Predictable density, natural look, reproducible via seed |
| Height placement | Per-blade downward raycast | Essential for non-flat terrain. Blades without ground are discarded. |
| Grouping | By (Mesh, Material) at collection time | Minimizes draw calls. N patches with same mesh = 1 draw call. |
| Backwards compatibility | Patches only used when child GrassPatch exist | Existing setups with grid/layers work unchanged. |
| Canvas computation | Combined bounds of all patches | Single SurfaceCanvas for trample/wind covers the full grass area. |

---

## New Types

### GrassPatch (MonoBehaviour)

**File:** `Assets/SNM_Unity/Runtime/GrassSystem/Core/GrassPatch.cs`

Authoring component placed on child GameObjects under GrassSystem. Holds all per-patch settings (mesh, material, area size, cell spacing, jitter, scale range, yaw range, placement map, raycast config). Exposes `BuildMatrices()` which:

1. Iterates a grid of cells within the local area.
2. Applies jitter offset per cell using `System.Random(randomSeed)`.
3. Optionally samples placement map for density/rotation/scale per cell.
4. Raycasts down per cell to snap Y to terrain.
5. Computes final `Matrix4x4.TRS(worldPos, rotation, scale)` per surviving blade.
6. Returns `Matrix4x4[]`.

Includes `OnDrawGizmosSelected` for Scene view bounds, mesh preview, and blade count.

### GrassPatchCollector (static utility)

**File:** `Assets/SNM_Unity/Runtime/GrassSystem/Core/GrassPatchCollector.cs`

- `Collect(GrassPatch[])` — calls `BuildMatrices()` on each patch, groups results by `(Mesh.GetInstanceID(), Material.GetInstanceID())`, returns `List<RenderGroup>`.
- `ComputeWorldBounds(GrassPatch[])` — encapsulates all patch areas into a single `Bounds`.

### GrassPatchEditor (CustomEditor)

**File:** `Assets/SNM_Unity/Runtime/GrassSystem/Editor/GrassPatchEditor.cs`

- Inspector: blade count estimate, missing mesh/material warnings.
- Scene GUI: draggable edge handles for area resize, sampled dot grid showing blade positions.

---

## Modified Types

### GrassSystem

**File:** `Assets/SNM_Unity/Runtime/GrassSystem/GrassSystem.cs`

`OnEnable()` now has two paths:

1. **Patch path** (new): `GetComponentsInChildren<GrassPatch>()`. If any found, collect and group via `GrassPatchCollector`, create canvas from combined bounds, call `GrassSystemFactory.CreateFromPatches()`.
2. **Grid path** (existing): unchanged fallback when no patches present.

### GrassSystemFactory

**File:** `Assets/SNM_Unity/Runtime/GrassSystem/Core/GrassSystemFactory.cs`

Added `CreateFromPatches(config, renderGroups, canvas, worldBounds)` method. Mirrors the existing `Create()` logic but accepts pre-grouped `RenderGroup` list instead of raw matrices/layers. Each group creates its own `GrassRenderer` with the group's mesh, material, and matrices. Features are wired identically to the existing path.

---

## Files Changed Summary

| File | Change |
|------|--------|
| `Core/GrassPatch.cs` | **New** — authoring MonoBehaviour |
| `Core/GrassPatchCollector.cs` | **New** — collection + grouping utility |
| `Editor/GrassPatchEditor.cs` | **New** — custom inspector + scene handles |
| `GrassSystem.cs` | **Modified** — added patch detection in OnEnable |
| `Core/GrassSystemFactory.cs` | **Modified** — added CreateFromPatches method |

---

## Verification Checklist

- [ ] Existing GrassSystem with no GrassPatch children works identically (backwards compat).
- [ ] Single GrassPatch child with mesh+material renders grass in play mode.
- [ ] Multiple GrassPatch children with different meshes render correctly (separate draw calls).
- [ ] Multiple patches with the same mesh+material are batched (single draw call).
- [ ] Moving a GrassPatch in the Scene and rebuilding (Rebuild() or re-enter play) updates positions.
- [ ] Jitter = 0 produces a visible grid pattern. Jitter = 1 produces scattered blades.
- [ ] Random seed produces reproducible placement across play sessions.
- [ ] Raycast height snapping works on terrain — blades follow slopes.
- [ ] Blades over empty space (no collider) are discarded.
- [ ] Placement map on a patch controls density/rotation/scale within that patch.
- [ ] Trample works across patches (disturber near any patch causes bending).
- [ ] Wind works across all patch materials.
- [ ] Frustum culling works (rotate camera away, blades are culled).
- [ ] Scene view gizmos: bounds box, dot grid, blade count label, edge resize handles.
- [ ] 10,000+ total blades across patches renders smoothly.

---

## Future Extensions

- **Circle/polygon patch shapes** — add a shape enum and alternate distribution logic.
- **Brush-painted placement** — an editor tool that paints GrassPatch areas like terrain details.
- **Runtime patch add/remove** — dynamic grass spawning during gameplay.
- **Spatial chunking** — subdivide large patches for more efficient frustum culling.
- **Normal alignment** — optionally tilt blades to match terrain surface normal, not just Y position.
