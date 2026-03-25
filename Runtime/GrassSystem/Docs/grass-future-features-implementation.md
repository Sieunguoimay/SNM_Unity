# Grass System Future Features — Implementation Plan

## Context
The GrassSystem renders 2500 grass blades via GPU instancing (`Graphics.RenderMeshIndirect`). Trample interaction uses a blit-based ping-pong render texture (no compute shaders, mobile compatible). The feature document lists 9 enhancements across 4 categories. This plan covers each with concrete shader/C# changes.

## Recommended Implementation Order
**Phase 1 (shader-only, low risk, independent):** 1 → 2 → 3 → 8
**Phase 2 (performance, combine 6+7):** 6+7
**Phase 3 (new systems, medium-high effort):** 4 → 5 → 9

---

## Feature 1: Color Variation
**Effort: Low | Files: InteractiveGrass.shader, GrassSystemConfig.cs, new ColorVariationFeature.cs**

**Shader** — Vertex: compute per-instance random from instanceID via Knuth hash (`instanceID * 2654435761u`), pass `instanceRand` (float, TEXCOORD4) to fragment. Fragment: lerp between two variation colors using `instanceRand`, multiply into existing tint.

```hlsl
// vert:
uint hash = instanceID * 2654435761u;
float instanceRand = frac(float(hash) * (1.0 / 4294967296.0));
// frag:
half3 variation = lerp(_ColorVariationA.rgb, _ColorVariationB.rgb, instanceRand);
tint *= variation;
```

**Config:** `float colorVariation`, `Color colorVariationA/B`. Bind once in feature constructor.

**Steps:**
1. Add `_ColorVariationA`, `_ColorVariationB` to shader Properties and CBUFFER
2. Add `instanceRand` to Varyings (TEXCOORD4), compute hash in vertex
3. Apply variation multiply in fragment after tint computation
4. Create `ColorVariationFeature.cs` that reads config and sets material properties
5. Register in `GrassSystemFactory.Create()`

**Dependencies:** None
**Risk:** Low — Knuth hash is well-distributed. ShadowCaster pass doesn't need this (shadows are ColorMask 0).

---

## Feature 2: Ambient Occlusion at Base
**Effort: Very Low | Files: InteractiveGrass.shader, GrassSystemConfig.cs**

**Shader** — Pass `trampleStrength` from vertex to fragment (TEXCOORD5). Fragment: darken near root using heightFactor power curve, intensify when trampled.

```hlsl
// frag:
float aoExtra = _AOStrength * trampleStrength * 0.5;
float ao = lerp(1.0 - _AOStrength - aoExtra, 1.0, pow(heightFactor, _AOPower));
lit *= ao;
```

**Config:** `float aoStrength = 0.3`, `float aoPower = 2.0`.

**Steps:**
1. Add `_AOStrength` and `_AOPower` to shader Properties/CBUFFER
2. Add `trampleStrength` to Varyings (TEXCOORD5), assign from already-computed value in vertex
3. Compute AO factor in fragment, multiply into `lit`
4. Add config fields, bind to material

**Dependencies:** Uses `heightFactor` (exists) and `trampleStrength` (computed in vertex, just needs passing)
**Risk:** Very low — pure shader math, tunable via two parameters.

---

## Feature 3: Subsurface Scattering Fake
**Effort: Low | Files: InteractiveGrass.shader, GrassSystemConfig.cs**

**Shader** — Fragment only. Dot product of view direction and negated light direction, raised to power, masked by heightFactor (tips transmit more). Additive contribution.

```hlsl
// frag:
float3 viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);
float3 lightDir = mainLight.direction;
float vdl = saturate(dot(viewDir, -lightDir));
float sss = pow(vdl, _SSSPower) * heightFactor * _SSSStrength;
lit += _SSSColor.rgb * sss;
```

**Config:** `float sssStrength = 0.4`, `Color sssColor`, `float sssPower = 3.0`.

**Steps:**
1. Add `_SSSStrength`, `_SSSColor`, `_SSSPower` to shader Properties/CBUFFER
2. Add SSS computation in fragment after existing lighting
3. Add as additive contribution to final color
4. Create config fields, bind to material

**Dependencies:** None — uses `_WorldSpaceCameraPos` and `mainLight.direction` (already available in URP)
**Risk:** Low — single dot product + pow. May overbright with HDR color — user-tunable.
**Note:** If Feature 5 (accumulation) is active, multiply SSS by `(1 - accumulationAmount)`.

---

## Feature 8: Per-Blade Sway Variation
**Effort: Low | Files: InteractiveGrass.shader, GrassSystemConfig.cs (WindConfig)**

**Shader** — Vertex: derive phase offset from world position hash, add to wind UV sampling. Optionally add amplitude variation.

```hlsl
// vert:
float phaseOffset = frac(worldOrigin.x * 12.9898 + worldOrigin.z * 78.233);
float2 windUV = worldUV / windScale + _Time.y * windSpeed + phaseOffset * _SwayVariation;

// Optional amplitude variation:
float ampVar = 0.7 + frac(worldOrigin.x * 45.164 + worldOrigin.z * 37.912) * 0.6; // 0.7..1.3
windDir *= ampVar;
```

**Config:** Add `float swayVariation = 0.1` to WindConfig. Bind as `_WindParams2.x`.

**Steps:**
1. Add `_WindParams2` to shader CBUFFER
2. Compute `phaseOffset` from `worldOrigin` in vertex shader
3. Add phase offset to wind UV sampling
4. Optionally add amplitude variation
5. Add `swayVariation` to `WindConfig`, bind in `WindFeature.cs`

**Dependencies:** Requires wind to be enabled
**Risk:** Low — if spatial frequency is too high, adjacent blades look chaotic. Start with 0.05–0.15 range.

---

## Features 6+7: Distance LOD + Frustum Culling (Combined)
**Effort: Medium | Files: GrassRenderer.cs, new CullingFeature.cs, GrassSystemConfig.cs, InteractiveGrass.shader (optional fade)**

Combine into a single `CullingFeature : IGrassFeature` that filters the instance set each frame in one loop.

### C# — CullingFeature.OnUpdate():
1. Extract frustum planes via `GeometryUtility.CalculateFrustumPlanes(camera)`
2. Iterate all matrices. For each instance:
   - **Frustum test:** 6 plane dot products with margin (`grassHeight + maxWindSway`)
   - **Distance test:** squared distance to camera. Full density within `lodFullDistance`, skip odd indices within `lodFadeDistance`, cull beyond
3. Copy surviving matrices into pre-allocated temp array
4. Call `GrassRenderer.UpdateVisibleInstances(tempMatrices, count)`

```csharp
public void OnUpdate(float deltaTime)
{
    GeometryUtility.CalculateFrustumPlanes(_camera, _frustumPlanes);
    var camPos = _camera.transform.position;
    int visibleCount = 0;

    for (int i = 0; i < _allMatrices.Length; i++)
    {
        Vector3 pos = new(_allMatrices[i].m03, _allMatrices[i].m13, _allMatrices[i].m23);

        // Frustum check with margin
        if (!IsPointInFrustum(pos, _frustumPlanes, _margin)) continue;

        // LOD distance check
        float sqDist = (camPos - pos).sqrMagnitude;
        if (sqDist > _lodFadeDistSq) continue;
        if (sqDist > _lodFullDistSq && (i % 2 == 1)) continue;

        _tempMatrices[visibleCount++] = _allMatrices[i];
    }
    _renderer.UpdateVisibleInstances(_tempMatrices, visibleCount);
}
```

### GrassRenderer.cs — New method:
```csharp
public void UpdateVisibleInstances(Matrix4x4[] matrices, int count)
{
    _instanceBuffer.SetData(matrices, 0, 0, count);
    // Update indirect args instanceCount
}
```
Allocate buffer at max size (`gridSize.x * gridSize.y`), only vary count.

### Shader (optional):
Alpha dither fade at LOD boundary to prevent popping.

**Config:** `bool lodEnabled`, `float lodFullDistance = 15`, `float lodFadeDistance = 25`.

**Steps:**
1. Add `UpdateVisibleInstances(Matrix4x4[], int)` to `GrassRenderer`
2. Create `CullingFeature.cs` with combined frustum + distance test
3. Pre-allocate `Plane[6]` and `Matrix4x4[maxCount]`
4. Set margin to `grassHeight + maxWindSway` to prevent popping
5. Extend `GrassFeatureContext` with camera reference and original `Matrix4x4[]`
6. Register in factory before `RenderFeature`
7. (Optional) Add shader-side alpha dither fade

**Dependencies:** None (these two features depend on each other — must be combined)
**Risk:** Medium — 2500 instances × 6 plane tests = 15,000 dot products/frame (cheap). `SetData` upload is the bottleneck — use NativeArray path if possible. Shadow caster uses same culled set, so off-screen shadow pop-in is possible (acceptable for current scale).

---

## Feature 4: Cut/Destroyed Grass
**Effort: Medium | Files: new GrassCut.shader, new GrassCut.cs, new CutFeature.cs, InteractiveGrass.shader, GrassSystemConfig.cs**

**Approach:** Separate cut map (R16_SFloat PingPongTexture). Cut never fades — only blit when new cuts are submitted.

### GrassCut.shader — Simplified trample shader:
```hlsl
float4 frag(Varyings i) : SV_Target
{
    float4 prev = tex2D(_MainTex, i.uv);
    float cut = prev.r;

    for (int b = 0; b < _BrushCount; b++)
    {
        float4 brush = _Brushes[b];
        float2 worldPos = UVToWorld(i.uv);
        float dist = length(worldPos - brush.xy);
        float strength = saturate(1.0 - dist / brush.w);
        cut = max(cut, strength);
    }
    return float4(cut, 0, 0, 0);
}
```

### InteractiveGrass.shader — Sample `_CutMap` in vertex:
```hlsl
float cutValue = SAMPLE_TEXTURE2D_LOD(_CutMap, sampler_CutMap, worldUV, 0).r;
float cutScale = saturate(1.0 - cutValue * 2.0); // smooth shrink to zero
localPos *= cutScale;
```

### GrassCut.cs:
Manages its own `StampBuffer` + `SurfaceStampRenderer` + `PingPongTexture`. Only runs blit when stamps are queued (skip on empty frames since cut state persists).

### Interface:
New `IGrassCutter` with `WorldPosition`, `CutRadius`. Or add `bool IsCutter` to `IGrassDisturber`.

**Steps:**
1. Create `GrassCut.shader` — simplified trample shader with no fade
2. Create `CutConfig` in `GrassSystemConfig.cs`
3. Create `GrassCut.cs` — manages stamp buffer, PingPongTexture, SurfaceStampRenderer
4. Create `CutFeature.cs` — IGrassFeature that updates GrassCut and binds cut map
5. Add `_CutMap` to `InteractiveGrass.shader`, sample in vertex, apply scale
6. Register in factory
7. Create `IGrassCutter` interface or add cut mode to `IGrassDisturber`

**Dependencies:** Reuses `SurfaceStampRenderer`, `PingPongTexture`, `StampBuffer` (all exist)
**Risk:** Medium — second render texture/blit adds GPU cost. Mitigate by skipping blit on empty frames. Cut blades are degenerate triangles (zero scale) — still in draw call but very cheap.

---

## Feature 5: Snow/Rain Accumulation
**Effort: Low-Medium | Files: InteractiveGrass.shader, new AccumulationFeature.cs, GrassSystemConfig.cs**

**Approach:** Global uniform `_AccumulationAmount` (start simple, local stamp map later).

### Shader — Vertex (downward bend):
```hlsl
combinedBend.y = max(0, combinedBend.y - _AccumulationAmount * _AccumulationBend);
```

### Shader — Fragment (color blend):
```hlsl
float accumMask = heightFactor * _AccumulationAmount;
tint = lerp(tint, _SnowColor.rgb, accumMask);
```

### AccumulationFeature.cs:
```csharp
public void OnUpdate(float deltaTime)
{
    _current = Mathf.MoveToward(_current, _config.targetAccumulation,
                                 _config.accumulationRate * deltaTime);
    _material.SetFloat(ID_AccumulationAmount, _current);
}
```

**Config:** `float targetAccumulation`, `float accumulationRate`, `Color snowColor`, `float accumulationBend`.

**Steps:**
1. Add shader properties and CBUFFER entries
2. Add downward bend in vertex (after wind+trample combine)
3. Add color blend in fragment
4. Create `AccumulationFeature.cs`
5. Register in factory
6. (Future) Add local accumulation map for spatially-varying snow

**Dependencies:** Interacts with Feature 3 — snow should reduce SSS
**Risk:** Low-medium. Global uniform is trivial. Downward bend combines additively with trample — `max(0, ...)` clamp prevents negative Y.

---

## Feature 9: Recovery Spring
**Effort: Medium-High | Files: InteractiveGrass.shader, GrassSystemConfig.cs**

**Approach:** Heuristic spring in vertex shader using existing trample map channels as proxy for recovery state.

### Shader — Vertex:
```hlsl
// Detect recovery: hold buffer (z) depleted but strength (w) still fading
float isRecovering = step(trample.z, 0.001) * step(0.01, trample.w);
float recoveryProgress = 1.0 - trample.w;
float springOsc = exp(-recoveryProgress * _SpringDamping)
                * sin(recoveryProgress * _SpringFrequency * 6.283);
trampleStrength += springOsc * _SpringAmplitude * isRecovering;
trampleStrength = clamp(trampleStrength, -0.1, 1.0);
```

**Config:** `float springFrequency = 8`, `float springDamping = 3`, `float springAmplitude = 0.15`.

**Steps:**
1. Add spring parameters to shader CBUFFER and Properties
2. Detect recovery state using `trample.z` (hold) and `trample.w` (strength)
3. Compute damped sinusoidal oscillation based on `recoveryProgress`
4. Add spring bend to trample strength (allows slight negative for overshoot)
5. Clamp to prevent extreme values
6. Add config fields and bind

**Dependencies:** Requires trample enabled. Uses `trample.z` as state signal — Features 4 and 5 must preserve this channel.
**Risk:** Medium-high — oscillation is tied to fade speed, not real time. Tuning depends on `trampleFadeSpeed`. Fallback: add dedicated spring velocity texture (second R16G16 PingPongTexture) for physically correct spring dynamics.

---

## Shared Infrastructure

### New Varyings (vertex → fragment):
| Slot | Field | Used By |
|------|-------|---------|
| TEXCOORD4 | `instanceRand` | Feature 1 |
| TEXCOORD5 | `trampleStrength` | Features 2, 9 |
| TEXCOORD6 | `lodFade` | Feature 6 (optional) |

Pack into float4 if varying slots become scarce.

### GrassFeatureContext Extensions:
- `Matrix4x4[] AllMatrices` — needed by Features 6+7
- `Camera Camera` — needed by Features 6+7

### Cross-Feature Dependencies:
| Feature | Depends On | Interacts With |
|---------|-----------|----------------|
| 1. Color Variation | None | None |
| 2. AO at Base | None | None |
| 3. SSS Fake | None | 5 (accumulation reduces SSS) |
| 4. Cut Grass | SurfaceInteraction infra | None |
| 5. Accumulation | None | 3 (minor interaction) |
| 6+7. LOD+Culling | None | Combined together |
| 8. Sway Variation | Wind enabled | None |
| 9. Recovery Spring | Trample enabled | 4, 5 (if they modify trample map) |

## Critical Files
- `Assets/SNM_Unity/Runtime/GrassSystem/InteractiveGrass.shader` — 7 of 9 features touch this
- `Assets/SNM_Unity/Runtime/GrassSystem/GrassTrample.shader` — Features 4, 9
- `Assets/SNM_Unity/Runtime/GrassSystem/GrassSystemConfig.cs` — all features add config
- `Assets/SNM_Unity/Runtime/GrassSystem/GrassRenderer.cs` — Features 6+7
- `Assets/SNM_Unity/Runtime/GrassSystem/Core/GrassSystemFactory.cs` — register new features

## Verification
- **Visual features (1,2,3,5,8):** Enter Play mode, inspect grass visually. Toggle parameters in inspector to confirm effect.
- **Cut (4):** Create a test IGrassCutter, verify blades disappear in stamped area and stay cut.
- **LOD+Culling (6+7):** Move camera far away, verify instance count drops (log or debug window). Rotate camera, verify off-screen blades are culled.
- **Spring (9):** Walk a disturber through grass, observe recovery wobble when trample fades.
