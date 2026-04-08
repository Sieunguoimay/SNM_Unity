# Plan: Bad North-Style Intersection Bands

## Context
The water surface shader already has foam (`_FOAM_ON`) and shoreline waves (`_SHORELINE_ON`), but neither produces the calm, rhythmic contour bands seen in Bad North. The goal is a new feature: soft gradient bands that wrap around **all** geometry intersecting the water, pulsing gently inward/outward like lapping waves.

## Visual Target
- 2-3 soft white bands at the water-geometry intersection
- Smooth gradient falloff (not sharp sine peaks like existing shoreline)
- Slow breathing animation: bands expand and contract over time
- Fade out at deeper water
- Wraps around any submerged object, not just shoreline

## Shader Algorithm (`WaterIntersectionBands.hlsl`)
```
Input: thickness (already computed in WaterSurface.shader via ComputeThickness)

1. depthNorm = saturate(thickness / maxDepth)
2. Animate: offset depthNorm by a slow sine wave (breathing)
      animated = depthNorm - sin(time * speed) * pulseAmplitude
3. Create bands: use smoothstep pairs to carve soft rings at even intervals
      for each band i: center = (i+1) / (bandCount+1)
         band += smoothstep(center - width, center, animated)
              * smoothstep(center + width, center, animated)
4. Fade at depth: multiply by (1 - depthNorm)
5. Output: saturate(sum) * strength
```

## Files to Create (following existing feature pattern)

### 1. `WaterSystem/Shaders/WaterIntersectionBands.hlsl`
- `float ComputeIntersectionBands(float thickness)` function
- Parameters: `_BandCount`, `_BandSpeed`, `_BandStrength`, `_BandWidth`, `_BandMaxDepth`, `_BandPulse`

### 2. `WaterSystem/IntersectionBands/IntersectionBandsConfig.cs`
- Namespace: `Snm.WaterSystem.IntersectionBands`
- Fields: `enabled`, `bandCount` (1-5), `speed`, `strength`, `width`, `maxDepth`, `pulseAmplitude`
- Pattern: identical to `ShorelineConfig.cs`

### 3. `WaterSystem/IntersectionBands/IntersectionBandsFeature.cs`
- Constructor: `(Material material, IntersectionBandsConfig config)`
- `OnUpdate`: calls binder with config values
- Pattern: identical to `ShorelineFeature.cs`

### 4. `WaterSystem/IntersectionBands/IntersectionBandsShaderBinder.cs`
- Static `Shader.PropertyToID` per parameter
- `Bind(...)` sets all on material
- Pattern: identical to `ShorelineShaderBinder.cs`

## Files to Modify

### 5. `WaterSystem/Shaders/WaterSurface.shader`
- Add `#pragma multi_compile_local _ _INTERSECTION_BANDS_ON` (after `_SHORELINE_ON`)
- Add properties: `_BandCount`, `_BandSpeed`, `_BandStrength`, `_BandWidth`, `_BandMaxDepth`, `_BandPulse`
- Add CBUFFER entries
- Add `#include "WaterIntersectionBands.hlsl"`
- Add fragment block (after shoreline, before specular):
  ```
  #ifdef _INTERSECTION_BANDS_ON
  float bands = ComputeIntersectionBands(thickness);
  waterColor = lerp(waterColor, float3(1,1,1), bands);
  #endif
  ```

### 6. `WaterSystem/Core/WaterConfig.cs`
- Add `using Snm.WaterSystem.IntersectionBands;`
- Add field: `public IntersectionBandsConfig intersectionBands = new();`

### 7. `WaterSystem/Core/WaterSystemFactory.cs`
- Add `using Snm.WaterSystem.IntersectionBands;`
- Add keyword line: `if (config.intersectionBands.enabled) surfaceMaterial.EnableKeyword("_INTERSECTION_BANDS_ON");`
- Add feature line: `if (config.intersectionBands.enabled) composite.Add(new IntersectionBandsFeature(ctx.SurfaceMaterial, ctx.Config.intersectionBands));`

## Verification
1. Enable the feature in the WaterConfig SO in Unity Inspector
2. Confirm white bands appear around any object partially submerged in water
3. Confirm bands pulse/breathe slowly over time
4. Confirm bands fade at deeper water
5. Adjust `bandCount`, `width`, `speed`, `pulseAmplitude` to match desired look
