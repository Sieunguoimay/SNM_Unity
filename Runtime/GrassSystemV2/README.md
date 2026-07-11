# GrassSystemV2

Self-contained GPU grass for Unity 6 / URP. Independent rewrite of GrassSystem —
zero references to any other module: drag this folder into any URP project and it works.

## Setup — 3 steps

1. **Add a `GrassWorld`** component to an empty GameObject (one per scene).
   Click **Create data asset** in the inspector (the health check offers it).
2. **Create a `GrassType`** (`Assets > Create > Snm > Grass System V2 > Grass Type`),
   assign a blade mesh + a material using the **`Snm/GrassV2`** shader
   (the inspector fixes wrong shaders with one click). Add it to the data's `Types`.
3. **Paint**: select the GrassWorld, press **Open Grass Painter** (or pick the tool
   in the scene toolbar) and drag over any collider. `CTRL` = erase, `[` `]` = brush size.
   For big areas drop a **GrassVolume** and press *Fill Volume*.

That's it — no config assets, no layers setup, no texture import rules
(wind is procedural; there is nothing to import wrong).

## Gameplay API

```csharp
// Trample: add a GrassDisturber component to anything that moves. Done.

GrassWorld.Instance.Cut(hitPoint, radius: 1.5f);                  // permanent, survives camera leaving
GrassWorld.Instance.ApplyEffect(GrassEffect.Burn,  pos, 3f);      // chars + shrivels (permanent in canvas)
GrassWorld.Instance.ApplyEffect(GrassEffect.Freeze, pos, 3f);     // frost + stops sway (thaws over time)
GrassWorld.Instance.ApplyEffect(GrassEffect.Tint,  pos, 3f);      // dye toward config.tintColor (fades)
GrassWorld.Instance.StampBend(pos, dirXZ, radius, strength);      // one-off shockwave bend
```

## Architecture (short)

```
GrassWorldData (SO asset)      painted blades, 20 B each, grouped per 16 m chunk
        │  upload once per chunk (never per frame)
GrassWorld (one component)     chunk load/unload + frustum at CHUNK level (cheap, CPU)
        ├─ GrassGpuDrivenTier  per-blade cull/LOD/thinning in GrassV2Cull.compute → indirect draws
        ├─ GrassSimpleTier     no compute: pre-shuffled ranges drawn as a distance-scaled prefix
        └─ GrassInteractionCanvas  two sliding 512² RTs: bend (trample+spring) & effects (burn/freeze/tint)
GrassV2.shader                 one shader for both tiers: procedural wind, bend, effects, AO, color variation
```

- **Tier selection**: `Auto` uses GPU-driven when the device has compute shaders,
  Simple otherwise (old mobile). Force either in the config for testing.
- **Cutting** is stored in per-instance flags (CPU + partial buffer re-upload),
  so it is truly persistent; canvas effects only exist within the canvas area
  around the camera.
- **No ShadowCaster pass** by design — shadow maps are too coarse for blades
  (blocky flicker). Root AO substitutes.

## Debugging

- **GrassWorld inspector** runs health checks with one-click fixes
  (missing data/mesh/material, wrong shader, chunk-size mismatch, budget too small…).
- **Draw Debug Overlay** toggle: chunk grid in the Scene view — green = drawn,
  yellow = resident, gray = unloaded — plus the interaction canvas square.
- **Show Stats Panel** toggle: live blade/chunk/draw/VRAM counters in play mode.

## Known limits

- One `GrassWorld` per scene (extras stay idle and warn).
- The Simple tier reads a StructuredBuffer in the vertex shader — needs GLES 3.1+ /
  Metal / Vulkan. Truly ancient GLES 3.0 devices are not supported.
- Effects (burn/freeze/tint) are forgotten outside the sliding canvas; cuts are not.
- Max 64 interaction stamps per frame (extras drop for that frame).
