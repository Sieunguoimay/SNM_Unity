#ifndef WATER_V2_FOAM_INCLUDED
#define WATER_V2_FOAM_INCLUDED

TEXTURE2D(_FoamTex);
SAMPLER(sampler_FoamTex);

// Edge foam: scrolling texture masked to where the water is shallow.
float ComputeFoam(float3 worldPos, float thickness)
{
    float2 foamUV = worldPos.xz * _FoamScale;
    foamUV += float2(0.7, 0.3) * _Time.y * _FoamSpeed;

    half foam = SAMPLE_TEXTURE2D(_FoamTex, sampler_FoamTex, foamUV).r;

    float edgeMask = 1.0 - saturate(thickness / _FoamDepthThreshold);
    edgeMask = edgeMask * edgeMask; // sharpen falloff

    return foam * edgeMask * _FoamStrength;
}

#endif // WATER_V2_FOAM_INCLUDED
