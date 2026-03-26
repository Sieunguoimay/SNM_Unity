# Spring Motion — Usage Guide

A shader-based jiggle/inertia system for 3D meshes. When an object moves and suddenly stops, its vertices overshoot and spring back — like a wobbly antenna or dangling accessory. The effect is distance-based: vertices farther from the pivot point deform more.

## Quick Setup

1. **Create a material** using the `Snm/SpringMotion` shader.
2. **Assign the material** to your mesh's Renderer.
3. **Add the `SpringMotionDriver`** component to the same GameObject.
4. **Move the object** — the mesh will jiggle on stops and direction changes.

That's it for basic usage. The defaults work well for small-to-medium accessories.

## Component: SpringMotionDriver

### Spring Parameters

| Parameter | Default | Description |
|---|---|---|
| **Stiffness** | 40 | How fast the spring snaps back. Higher = stiffer, less wobble. Range: 10 (very loose) to 200 (almost rigid). |
| **Damping** | 4 | How quickly oscillation dies out. Higher = fewer bounces. Range: 1 (bouncy) to 10 (overdamped, no overshoot). |

### Vertex Falloff Parameters

| Parameter | Default | Description |
|---|---|---|
| **Max Distance** | 1 | Object-space distance that maps to full effect strength. Should roughly match how far the farthest vertex is from the pivot. |
| **Falloff Power** | 1.5 | Exponent controlling the falloff curve. 1 = linear (uniform bend), 2+ = quadratic (base stays stiff, tip wobbles most). |
| **Max Displacement** | 0.5 | World-space clamp preventing extreme stretching. Increase for exaggerated cartoon effects. |

### Pivot Parameters

| Parameter | Default | Description |
|---|---|---|
| **Pivot Offset** | (0,0,0) | The attachment point in object space. Vertices at this point don't move at all. Typically set to where the object connects to its parent (e.g., the base of an antenna, hilt of a sword). |
| **Pivot Gizmo Radius** | 0.05 | Size of the cyan wireframe sphere drawn at the pivot in the Scene view (selected only). |

### Target

| Parameter | Default | Description |
|---|---|---|
| **Renderer** | auto | The Renderer to push spring data to. If left empty, auto-detects from the same GameObject. |

## Gizmos

When the GameObject is selected in the Scene view:

- **Cyan wireframe sphere** (small) — the pivot point in world space.
- **Cyan wireframe sphere** (large) — the `Max Distance` falloff range. Vertices inside this sphere are partially affected; vertices at the edge get full effect.
- **Yellow line** (play mode only) — current spring displacement direction and magnitude.

## Physics / FixedUpdate Support

The driver handles both physics-driven and script-driven objects:

- **Rigidbody objects** (moved in FixedUpdate) — position is captured in `FixedUpdate` so the spring tracks the physics step accurately.
- **Script/animation objects** (moved in Update) — position is read directly in `LateUpdate`.

No configuration needed — this is handled automatically.

## API

### `AddImpulse(Vector3 worldImpulse)`

Kick the spring with an instantaneous force. Useful for:

- **Impact/collision** — call with the collision normal to make the object wobble on hit.
- **Pickup/drop** — give a small upward impulse when grabbed.
- **Throw release** — impulse in throw direction for a stretch-and-snap effect.

```csharp
GetComponent<SpringMotionDriver>().AddImpulse(Vector3.up * 5f);
```

### `ResetSpring()`

Instantly snap the spring back to rest. Useful when teleporting the object or resetting state.

```csharp
GetComponent<SpringMotionDriver>().ResetSpring();
```

## Tuning Presets

Here are starting points for common use cases:

| Use Case | Stiffness | Damping | Falloff Power | Max Displacement |
|---|---|---|---|---|
| Stiff antenna | 80 | 5 | 2.0 | 0.3 |
| Wobbly accessory | 30 | 2 | 1.5 | 0.5 |
| Soft ponytail/tail | 15 | 1.5 | 1.0 | 0.8 |
| Bouncy cartoon | 20 | 1 | 1.0 | 1.0 |
| Subtle jewelry | 100 | 6 | 2.0 | 0.1 |

## Shader: Snm/SpringMotion

A full URP Lit PBR shader with spring vertex deformation. Supports:

- Albedo texture + color tint
- Normal mapping
- Metallic / Smoothness
- Main light shadows + additional lights
- Fog
- Correct shadow casting (shadow caster pass also deforms vertices)
- Depth prepass

### Using SpringMotion.hlsl in Your Own Shaders

If you have a custom shader and want to add spring motion to it, include the hlsl file and call the function in your vertex stage:

```hlsl
#include "Assets/SNM_Unity/Runtime/SpringMotion/SpringMotion.hlsl"

// In your CBUFFER:
float4 _SpringPivotOS;
float _SpringMaxDistance;
float _SpringFalloffPower;
float4 _SpringDisplacement;

// In your vertex function:
float3 posOS = input.positionOS.xyz;
float3 normalOS = input.normalOS;
ApplySpringMotionFull(posOS, normalOS,
    _SpringPivotOS.xyz, _SpringDisplacement.xyz,
    _SpringMaxDistance, _SpringFalloffPower);
```

Two functions are available:

- `ApplySpringMotion(posOS, pivotOS, displacement, maxDist, falloff)` — position only, cheaper.
- `ApplySpringMotionFull(inout posOS, inout normalOS, ...)` — also adjusts normals for correct shading on deformed mesh.

## Limitations

- **No collision** — vertices can clip through nearby geometry. For collision-aware secondary motion, use bone-based physics instead.
- **Uniform displacement** — all vertices move in the same direction (scaled by distance). This doesn't simulate bending/curving along a chain. For chain-like motion (hair, rope), a bone chain is more appropriate.
- **Single pivot** — the effect radiates from one point. For meshes with multiple attachment points, use multiple sub-meshes each with their own driver, or consider a bone-based approach.
