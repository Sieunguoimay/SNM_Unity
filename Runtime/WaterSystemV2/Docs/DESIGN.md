# WaterSystemV2 — Design

Date: 2026-07-11. Full parity rewrite of `Runtime/WaterSystem` (V1), independent and coexisting. V1 stays untouched until scenes migrate.

## Goals (from the design session)

Reusable engine module first (any game drops it in) · full visual parity with V1's 13 features · easier to install, extend, debug · cleaner · faster · shoreline bake stays manual but with better UX and a safe fallback.

## The core idea: static look vs dynamic systems

V1's root mistake was forcing everything through one `IWaterFeature` composite: six "features" only pushed unchanging config values into the material — and did it every frame — while the genuinely dynamic systems hid in the same abstraction. Adding any feature meant editing a 128-line god factory plus three new files.

V2 splits by nature, not by feature:

- **Static look** (`WaterLook` → `WaterMaterialBinder`): depth colors, caustics, foam, shoreline, sparkle, scroll normal, specular, refraction. Bound to the material **once**, re-bound only when edited (OnValidate → dirty flag). Keywords included, so features toggle live.
- **Dynamic systems** — the only per-frame code, each a plain class with an explicit tick:
  | System | Tick | Job |
  |---|---|---|
  | `WaveSimulation` | Update | GPU ping-pong heightfield, stamps, rain |
  | `WakeTracker` | Update | entry splash + wake trail from disturbers |
  | `BuoyancySolver` | FixedUpdate | Archimedes force, drag blending |
  | `PlanarReflection` | LateUpdate | mirror camera, change-driven + interval |

## Decisions

- **One component:** `WaterBody` ([ExecuteAlways], RequireComponent MeshFilter/Renderer). All config embedded `[Serializable]` — zero mandatory assets, prefab is the reuse vehicle. No factories, no DI, no `[WaterUpdater]` GameObject.
- **Independence:** own asmdef (`Snm.WaterSystemV2`, empty references) + namespace. The few SurfaceInteraction utilities V1 leaned on (canvas math, ping-pong, stamp buffer) are rewritten here — small, deliberate duplication buys the ability to delete either system without touching the other.
- **Interaction registry instead of wiring:** objects register into the static `WaterInteraction` lists (the `WaterDisturber`/`WaterFloater` adapter components do it automatically in OnEnable). Every water filters by its own bounds, so objects move freely between waters and `WorldInstaller`-style plumbing disappears. Bulk `RegisterAll` kept for installer-style code.
- **Shoreline stays baked, fails safe:** manual Scan → review → Bake in the inspector. Without a bake the shoreline keyword is forced off (V1 rendered garbage). Debug view "Shore Distance" shows the baked UV1 directly.
- **Known non-goals this round:** waves are still normal-only (no vertex displacement — silhouette stays flat, same as V1); overlapping water bodies unsupported (both buoyancy solvers would fight over drag restore).

## Perf deltas vs V1

Bind-once look (~40 material sets/frame saved) · no per-water updater GameObject · registry events instead of per-frame dictionary re-sync · stamp buffer clears only its dirty range and warns once per frame · debug display blit only on demand (V1 blitted every frame) · reflection interval exposed (was a hidden constant 4) · canvas syncs only when the transform actually moved.
