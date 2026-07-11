# WaterSystemV2 — Setup

## Water in 30 seconds

1. Create an empty GameObject, add **Snm → Water System V2 → Water Body**.
2. Set **Surface → Size**. Done — flat quad water with waves, depth fade and reflection runs on Play (camera falls back to `Camera.main`).
3. Optional textures for the full look: assign **Look → Caustics → Texture** and **Look → Foam → Texture** (any tiling grayscale caustics/foam textures).

Everything lives on that one component. Duplicate the GameObject or prefab it to reuse a tuned water.

## Interaction (ripples + floating)

Add **Water Disturber** to anything that should splash and leave a wake. Add **Water Floater** to any rigidbody that should float. That's all — they self-register and every WaterBody picks them up by area.

From code: `WaterInteraction.Register(...)` / `RegisterAll(...)` for custom `IWaterDisturber`/`IWaterFloater` implementations, `waterBody.AddDisturbance(pos, radius, strength)` for one-off splashes (explosions), `WaterBody.FindAt(position)` to query which water contains a point.

## Shoreline foam (needs a bake)

1. On the WaterBody inspector press **Scan Terrain Objects** — fills the list with every mesh crossing the water plane. Review it.
2. Press **Bake Shore Mesh** — writes `Assets/Generated/WaterMeshes/{scene}_{name}_V2.asset` and switches Mesh Source to Baked.
3. Enable **Look → Shoreline**.

Un-baked shoreline is safe: it simply stays hidden (the status panel tells you). Re-bake after moving terrain.

## Debugging

- The inspector **Status** panel answers the usual questions: is the bake active, which camera reflects, how many disturbers/floaters are live, stamps per frame, reflection renders per 60 frames.
- **Debug View** (bottom of the component) swaps the water color for diagnostics: `WaveHeight` (gray ripples), `Normals`, `ShoreDistance` (red at shore → blue deep; solid red = mesh has no baked UV1).
- Play-mode inspector buttons: **Clear Waves**, **Test Splash**, plus live wave/reflection texture previews.

## Requirements & gotchas

- URP with **Depth Texture** and **Opaque Texture** enabled (depth fade + refraction read them).
- The three shader references on WaterBody auto-fill when the component is added, and the surface shader re-resolves on enable. If a reference ever shows None, assign the `Snm/WaterSystemV2/*` shaders by hand (don't use Reset — it wipes your tuning).
- Waves are normal-only: the surface silhouette stays flat by design this round.
- Overlapping water rectangles are unsupported (buoyancy drag restore may conflict).
