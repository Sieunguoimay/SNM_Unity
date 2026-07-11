#ifndef WATER_V2_CAUSTICS_INCLUDED
#define WATER_V2_CAUSTICS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

TEXTURE2D(_CausticsTex);
SAMPLER(sampler_CausticsTex);
float4 _CausticsTex_ST;

half2 Panner(half2 uv, half speed, half tiling)
{
    return (half2(1, 0) * _Time.y * speed) + (uv * tiling);
}

half3 SampleCausticsSplit(half2 uv, half split)
{
    half2 uv1 = uv + half2(split, split);
    half2 uv2 = uv + half2(split, -split);
    half2 uv3 = uv + half2(-split, -split);

    half r = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, uv1).r;
    half g = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, uv2).r;
    half b = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, uv3).r;

    return half3(r, g, b);
}

// Light-projected caustics on the geometry behind the water. Two layers pan
// in opposite directions; min() of both keeps only where they overlap, which
// reads as the classic dancing pattern.
float3 ComputeCaustics(float3 bgPositionWS)
{
    Light mainLight = GetMainLight();
    float3 lightDirWS = normalize(-mainLight.direction);

    // Project position onto the plane perpendicular to the light.
    float distAlongLight = dot(bgPositionWS, lightDirWS);
    float3 projectedPos = bgPositionWS - (distAlongLight * lightDirWS);

    float2 causticsUV = projectedPos.xz * _CausticScale;

    float2 uv1 = Panner(causticsUV, _CausticSpeed, 1.0 / _CausticsTex_ST.x);
    float2 uv2 = Panner(causticsUV, _CausticSpeed, -1.0 / _CausticsTex_ST.x);

    #ifdef _CAUSTICS_CHROMATIC
    float3 c1 = SampleCausticsSplit(uv1, _CausticSplit);
    float3 c2 = SampleCausticsSplit(uv2, _CausticSplit);
    #else
    float3 c1 = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, uv1).rrr;
    float3 c2 = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, uv2).rrr;
    #endif

    return min(c1, c2) * _CausticStrength;
}

#endif // WATER_V2_CAUSTICS_INCLUDED
