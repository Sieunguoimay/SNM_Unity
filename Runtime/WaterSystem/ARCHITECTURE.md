# Water System Architecture

The water system lives under `Assets/SNM_Unity/Runtime/WaterSystem/` and follows a **modular feature-based architecture**.

## Core Layer

- **`WaterSystemMB`** — The MonoBehaviour entry point. Placed on a GameObject in the scene. Holds the `WaterConfig` and a reference camera. On `Start()`, delegates to the factory.
- **`WaterSystemFactory`** — The **composition root**. The only place that knows how all pieces connect. Creates the surface, enables shader keywords, instantiates each feature based on config flags, and wires them into the update loop.
- **`WaterFeatureComposite`** — A composite pattern container that holds all `IWaterFeature` instances and broadcasts `OnUpdate`/`OnFixedUpdate`/`OnLateUpdate` to them.
- **`UpdateDispatcher`** — A MonoBehaviour (`[ExecuteInEditMode]`) that bridges Unity's update loop to the composite. Uses snapshot arrays for safe iteration.
- **`WaterFeatureContext`** — A shared context bag holding `WaterConfig`, `SurfaceCanvas`, `Material`, and `Camera`, passed to features that need it.
- **`WaterSystemHandle`** — The public API handle returned by the factory. Exposes `ReflectionTexture`, `IWaveSimulation`, `DisturberTracker`, and `BuoyancyTracker`. Also owns the `IDisposable` cleanup scope.

## Feature Plug-ins (all implement `IWaterFeature`)

Each feature is **toggled by a bool in its config** and only instantiated if enabled:

| Feature | Purpose |
|---|---|
| **Wave Simulation** | GPU-based ping-pong heightfield simulation via compute shaders. Stamps disturbances as UV-space circles. `WaveSimulationController` implements both `IWaveSimulation` and `IWaterFeature`. |
| **Wave Disturbers** | `WaveDisturberTracker` watches `IWaveDisturber` objects (provided externally). Generates entry splashes and continuous wakes based on speed, spacing, and proximity to the surface. |
| **Buoyancy** | `BuoyancyTracker` applies Archimedes' principle to `IBuoyant` objects — computes submerged volume, applies upward force, lerps drag, and dampens vertical bobbing near the surface. |
| **Reflection** | Reactive pipeline: camera movement triggers mirror transform + oblique projection, which triggers shader bind + render request. Renders to a `RenderTexture` via a mirrored camera. |
| **Depth** | Binds depth-related shader parameters to the surface material. |
| **Caustics** | Binds caustic texture/animation parameters. Supports chromatic split via shader keyword. |
| **Foam** | Binds foam shader parameters to the surface material. |
| **Shoreline** | Binds shoreline effect parameters to the surface material. |
| **Sparkle** | Binds sparkle/specular highlight parameters to the surface material. |
| **ScrollNormal** | Binds scrolling normal map parameters to the surface material. |

## Surface Layer

- **`SurfaceInstaller`** — Creates the water mesh GameObject (auto-generated quad or custom mesh), material, and returns a `SurfaceCanvas` for world-to-UV mapping.
- **`WaterSurfaceMB`** — Hosts the `MeshFilter`/`MeshRenderer` and exposes the `SurfaceCanvas`.

## Wave Simulation Pipeline

1. `WaveSimulationFactory` creates a ping-pong `RenderTexture` pair (R16G16_SFloat), a `StampTextureBuffer(32)`, and materials for both simulation and display shaders.
2. `WaveSimulationController` orchestrates each frame:
   - `WaveSimulationPass.Execute()` — stamps pending disturbances into the ping-pong buffer.
   - `WaveDisplayPass.Render()` — converts the raw simulation texture into a display texture.
   - Binds `_WaveTex` and `_WaveNormalStrength` to the surface material.
3. Disturbances are added as `WaveDisturbance` structs (UV position, radius, strength).

## Disturber Tracking

`WaveDisturberTracker` syncs against a live `IEnumerable<IWaveDisturber>` each frame:

- **Entry splash** — On first contact with water, emits a single disturbance scaled by entry velocity.
- **Continuous wake** — While near/in water, emits disturbances when the object moves beyond a speed-dependent spacing threshold. Speed is mapped through an `AnimationCurve` for fine control.
- **Proximity tolerance** — Objects slightly above the water surface can still generate wake.

## Buoyancy System

`BuoyancyTracker` runs in `FixedUpdate` and applies physics forces to `IBuoyant` objects:

1. Computes submersion ratio from `GetSubmergedVolume(waterY) / GetTotalVolume()`.
2. Applies upward buoyancy force via Archimedes' principle: `waterDensity * submergedVolume * gravity`.
3. Lerps `linearDamping` and `angularDamping` toward in-water values based on submersion ratio.
4. Applies surface dampening force to reduce vertical bobbing when 20%–80% submerged.
5. Restores original drag values when the object exits the water.

## Reflection System

Uses a reactive `Effect` system (signals/computed values):

1. **Effect 1**: Camera position/rotation change → mirror the reflection camera across the water plane → compute oblique projection matrix.
2. **Effect 2**: Projection changed → bind shader uniforms → flag render request.
3. **LateUpdate**: If the scheduler allows and a render is requested, the `ReflectionRenderer` renders one frame.

## Key Design Patterns

1. **Feature toggle** — Every subsystem checks `config.*.enabled` before instantiation or execution.
2. **External contracts** — Game objects implement `IWaveDisturber` or `IBuoyant`; the water system defines the interface, the game layer implements it. Injected as `IEnumerable<T>` via `SetDisturbers()`/`SetBuoyants()` before `Start()`.
3. **Composition over inheritance** — No base classes; features are flat implementations of `IWaterFeature`.
4. **Deterministic cleanup** — `DisposeCollection` chains all disposables. `WaterSystemHandle.Dispose()` tears everything down.
5. **Reactive reflection** — Uses an `Effect` system so the reflection only re-renders when the camera actually moves.

## Lifecycle

```
WaterSystemMB.Start()
  └─ WaterSystemFactory.Create()
       ├─ SurfaceInstaller.Install()          → SurfaceCanvas + Material
       ├─ Enable shader keywords
       ├─ Create features (Reflection, Caustics, Depth, Wave, Foam, etc.)
       ├─ Create trackers (WaveDisturberTracker, BuoyancyTracker)
       ├─ Register composite with UpdateDispatcher
       └─ Return WaterSystemHandle

Each frame:
  UpdateDispatcher.Update()       → composite.Update()       → each feature.OnUpdate()
  UpdateDispatcher.FixedUpdate()  → composite.FixedUpdate()  → each feature.OnFixedUpdate()
  UpdateDispatcher.LateUpdate()   → composite.LateUpdate()   → each feature.OnLateUpdate()

WaterSystemMB.OnDestroy()
  └─ WaterSystemHandle.Dispose()
       └─ DisposeCollection → composite.Dispose() + surface cleanup + updater cleanup
```
