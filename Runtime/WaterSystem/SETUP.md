# Water Surface Setup

How to add a new water body to a scene. Do the **one-time project setup** once, then use the **quick path** for every new water body.

## One-time project setup

Done once per project. If `WaterConfig.asset` and `WaterSystem_Default.prefab` already exist under `Assets/Content/Gameplay/Environment/Water/`, skip this section.

### 1. Create `WaterConfig.asset`

1. In `Assets/Content/Gameplay/Environment/Water/`, right-click → **Create → Snm → Water → Water Config**.
2. Name it `WaterConfig.asset`.
3. On the asset, under **Surface**:
   - Assign `waterSurfaceShader` = `Custom/WaterSurface` (`Assets/SNM_Unity/Runtime/WaterSystem/Shaders/WaterSurface.shader`).
   - Leave `waterSurfaceMaterial` empty — the factory creates one per instance.
   - Leave `autoGenerateMesh = true` — the per-instance `bakedMesh` override takes over when a mesh is baked.
4. Under the feature sections, verify the defaults you want are enabled:
   - `caustics.enabled`, `depth.enabled`, `wave.enabled`, `reflection.enabled`, `buoyancy.enabled` → on.
   - `shoreline.enabled` → **on** (this is the default feature set).
   - `foam.enabled`, `sparkle.enabled`, `scrollNormal.enabled` → your call.
5. Assign texture references where relevant:
   - `caustics.texture` → `caustics_001.bmp` (in the same folder).
   - `foam.foamTexture` → a tiling foam texture if foam is enabled.
   - `scrollNormal.normalMap` → a tiling normal map if scroll-normal is enabled.

Use the `WaterSystemMB` context menu **Auto-assign config references** on any instance to pull shader/shaders from the `Shaders/` folder automatically.

### 2. Create `WaterSystem_Default.prefab`

1. In the scene, create an empty GameObject named `WaterSystem_Default`. Leave its position at origin.
2. Add component **`WaterSystemMB`** (`Snm.WaterSystem.WaterSystemMB`).
3. Set **Config Asset** = the `WaterConfig.asset` from step 1.
4. Leave **Source Camera** empty (falls back to `Camera.main` at runtime).
5. Under **Mesh Bake**:
   - Leave **Baked Mesh** empty (set per-instance by the bake button).
   - Leave **Terrain Objects** empty.
   - **Water Size** = `(30, 30)` (or your typical water body size — per-instance override available).
   - **Grid Cell Size** = `1`.
   - **Max Shore Distance** = `4`.
   - **Along-Shore Tiling** = `0.1`.
6. Drag the GameObject into `Assets/Content/Gameplay/Environment/Water/` to save it as `WaterSystem_Default.prefab`.
7. Delete the scene instance.

### 3. Verify

Open a scratch scene, drag the prefab in, add a terrain cube, assign it to the prefab's **Terrain Objects**, click **Bake Water Mesh**, press Play. You should see water rendering with all enabled features. If nothing renders, check `Camera.main` exists in the scene.

## Quick path (gameplay scene)

1. **Drag** `WaterSystem_Default.prefab` into the scene and position it at the desired water level.
2. **Select** the instance. On the `WaterSystemMB` component:
   - Drag every surrounding terrain GameObject into **Terrain Objects**.
   - Set **Water Size** to cover the area you want (shown in the cyan gizmo).
   - Adjust **Grid Cell Size** (mesh resolution), **Max Shore Distance** (how far inland the shore-aware UV reaches), and **Along-Shore Tiling** (foam-band frequency along the shoreline) if needed.
3. **Click Bake Water Mesh.** The mesh is saved to `Assets/Generated/WaterMeshes/{scene}_{gameObject}.asset` and assigned to the instance's `Baked Mesh` field.
4. **Press Play.** Shoreline, caustics, depth fade, waves, reflection all render from `WaterConfig.asset`.

Interaction (buoyancy + ripples) wires automatically for any `WorldObject` whose `WorldObjectConfigSO` uses `EnvironmentBehaviorConfigSO` with `waveDisturber` / `buoyant` ticked. `LevelChunk` discovers the `WaterSystemMB` in the chunk and calls `SetDisturbers` / `SetBuoyants` for you.

## Sandbox / test scenes (no gameplay stack)

When you want to drop water into a bare scene without booting the full gameplay DI graph:

1. Do steps 1-3 above.
2. Add `WaterSandboxInteractor` to the water GameObject (it `[RequireComponent]`s `WaterSystemMB`).
3. Drag every `Rigidbody` that should float / make ripples into the **Participants** list.
4. Press Play. The interactor wraps each participant with `BuoyantComponent` + `WaveDisturberComponent` in `Awake` and calls `SetDisturbers` / `SetBuoyants` on the sibling `WaterSystemMB`.

The sandbox interactor is **not** for gameplay scenes — use the `EnvironmentBehaviorConfigSO` path there.

## What lives where

- **Per-instance** (on `WaterSystemMB`): the mesh bake inputs and the baked mesh reference. Each water body gets its own shape.
- **Shared** (in `WaterConfig.asset`): feature enable flags (shoreline, caustics, depth, waves, reflection, foam, sparkle, scroll-normal), tuning parameters, and the surface shader/material reference.

If you need a water body with different tuning (darker deep color, different wave speed, no reflection…), duplicate `WaterConfig.asset` and point that instance's `WaterSystemMB.configAsset` at the duplicate.

## Baking gotchas

- **Empty terrain list** disables the Bake button. Assign at least one terrain GameObject.
- **Zero Water Size** also disables the button. Leave it at the default `(30, 30)` or larger.
- **Rebaking** overwrites the existing mesh asset in place (`EditorUtility.CopySerialized`) — the reference in the inspector stays stable across rebakes.
- **Terrain with non-manifold edges** (isolated triangles, T-junctions) can produce orphan shoreline segments. Check the generator's scene-view gizmos via `Tools → GrabAndToss → Water Mesh Generator` (the standalone debug window) if shoreline foam looks wrong.
- The **baked mesh's `UV1`** carries shore distance (x) and along-shore arc (y). If you ever assign a non-baked mesh (e.g. a plain quad) to `Baked Mesh`, shoreline foam will render incorrectly — every vertex has `uv1 = (0,0)` and the shader will treat the entire surface as "at the shore".

## Features that come with the default config

| Feature | Driven by | Notes |
|---|---|---|
| Shoreline foam | Baked UV1 from `WaterMeshGenerator` | Requires a baked mesh. |
| Buoyancy | `IBuoyant` participants + `BuoyancyTracker` | Analytical volume for sphere/box/capsule. |
| Caustics | `CausticsFeature` + `caustics_001.bmp` | Tunable strength/scale/speed in config. |
| Depth fade | `DepthFeature` + scene depth | `shallowColor` / `deepColor` / `absorption`. |
| Waves | GPU heightfield sim (`WaveSimulationController`) | Drives surface normals each frame. |
| Interaction ripples | `WaveDisturberTracker` | Entry splash + wake trail. |
| Reflection | `ReflectionFeature` + mirror camera | `textureWidth` controls resolution. |

## Related files

- `Core/WaterSystemMB.cs` — scene-facing MonoBehaviour with the bake inputs.
- `Core/Editor/WaterSystemMBEditor.cs` — custom inspector with the Bake button.
- `Core/Editor/WaterMeshGenerator.cs` — marching-squares bake implementation.
- `Shoreline/` — `ShorelineConfig`, `ShorelineFeature`, `ShorelineShaderBinder`.
- `Shaders/WaterSurface.shader`, `Shaders/WaterShoreline.hlsl` — shader side.
- `ARCHITECTURE.md` — system-wide composition overview.
