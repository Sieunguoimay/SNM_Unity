#ifndef WATER_FOAM_INCLUDED
#define WATER_FOAM_INCLUDED

TEXTURE2D(_FoamTex);
SAMPLER(sampler_FoamTex);

float ComputeFoam(float3 worldPos, float thickness)
{
    // Scrolling foam UV
    float2 foamUV = worldPos.xz * _FoamScale;
    foamUV += float2(0.7, 0.3) * _Time.y * _FoamSpeed;

    half foam = SAMPLE_TEXTURE2D(_FoamTex, sampler_FoamTex, foamUV).r;

    // Edge mask: foam appears where water is shallow
    float edgeMask = 1.0 - saturate(thickness / _FoamDepthThreshold);
    edgeMask = edgeMask * edgeMask; // sharpen falloff

    return foam * edgeMask * _FoamStrength;
}

#endif // WATER_FOAM_INCLUDED
