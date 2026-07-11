# WaterSystemV2 — Architecture

```
WaterBody (MonoBehaviour, [ExecuteAlways], the only scene object)
│  embedded settings: WaterSurfaceSettings · WaterLook · WaterWaveSettings
│                     WaterReflectionSettings · WaterBuoyancySettings · WaterShoreBakeSettings
│
├─ surface        MeshFilter/MeshRenderer on the same GameObject
│                 mesh: auto quad │ baked shore mesh │ custom
│                 material: owned instance of Snm/WaterSystemV2/WaterSurface (or override asset)
│
├─ look (edit-time + on change only)
│                 OnValidate → dirty → WaterMaterialBinder.Apply(material, look…)
│                 WaterShaderIds = every property id + keyword (single source of truth)
│
└─ dynamics (play mode only; rebuilt when a structural setting changes)
   ├─ WaveSimulation      Update      ping-pong RT (R16G16 sfloat) ← WaveSimulationV2.shader
   │                                  stamps ← StampBuffer ← WakeTracker / AddDisturbance()
   │                                  binds _WaveTex per frame (ping-pong alternates)
   ├─ WakeTracker         Update      reads WaterInteraction.Disturbers, filters by WaterCanvas
   ├─ BuoyancySolver      FixedUpdate reads WaterInteraction.Floaters, Archimedes + drag
   └─ PlanarReflection    LateUpdate  hidden mirror camera → _ReflectionTex + _ReflectionVP
                                      renders on change or every frameInterval frames
```

Data flow C# → GPU: everything is per-material (`Material.Set*` via `WaterShaderIds`), no globals. The surface shader (`WaterSurfaceV2.shader`) composites depth absorption → refraction → caustics → foam → shoreline → specular → reflection → sparkle in one URP transparent pass, with a `_DebugView` escape hatch that replaces the output with wave height / normals / shore distance.

## Folders

- `Core/` — data + pure math, no rendering: settings classes, `WaterCanvas` (world↔UV), `WaterShaderIds`, `IWaterDisturber`/`IWaterFloater` contracts, `WaterInteraction` registry.
- `Rendering/` — GPU-facing: `WaterMaterialBinder`, `WaveSimulation`, `PlanarReflection` (+ pure `ReflectionProjection`/`ReflectionPlane` math), `PingPongTexture`, `StampBuffer`, `SurfaceMeshBuilder`.
- `Physics/` — `WakeTracker`, `BuoyancySolver`.
- `Interaction/` — drop-in adapter components `WaterDisturber`, `WaterFloater`.
- `Shaders/` — surface + sim + display shaders and their HLSL includes.
- `Editor/` — `WaterBodyEditor` (status panel, Scan/Bake, previews), `WaterShoreBaker` (marching squares, ported from V1).

## Extending

- **New look parameter:** add the field to `WaterLook`, the id to `WaterShaderIds`, one line in `WaterMaterialBinder`, and the shader property. Nothing else — no factory, no feature class.
- **New dynamic system:** write a plain class with a Tick and IDisposable, create/dispose it in `WaterBody.BuildDynamics`/`TearDownDynamics`, call it from the right update hook. The four existing systems are the templates.
- **Custom interaction shapes:** implement `IWaterDisturber`/`IWaterFloater` yourself and register via `WaterInteraction` — the bundled adapters are bounds-based approximations.
