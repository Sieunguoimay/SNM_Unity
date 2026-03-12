#ifndef WATER_CAUSTICS_INCLUDED
#define WATER_CAUSTICS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

TEXTURE2D(_CausticsTex);
SAMPLER(sampler_CausticsTex);
float4 _CausticsTex_ST;

half2 Panner(half2 uv, half speed, half tiling)
{
    return (half2(1, 0) * _Time.y * speed) + (uv * tiling);
}

half3 SampleCaustics(half2 uv, half split)
{
    half2 uv1 = uv + half2(split, split);
    half2 uv2 = uv + half2(split, -split);
    half2 uv3 = uv + half2(-split, -split);

    half r = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, uv1).r;
    half g = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, uv2).r;
    half b = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, uv3).r;

    return half3(r, g, b);
}

float3x3 BuildLightViewMatrix(float3 lightDirWS)
{
    // lightDirWS: normalized, pointing FROM surface TO light
    float3 forward = normalize(lightDirWS);

    // pick a safe up vector
    float3 up = abs(forward.y) > 0.99
        ? float3(0, 0, 1)
        : float3(0, 1, 0);

    float3 right = normalize(cross(up, forward));
    float3 newUp = cross(forward, right);

    // rows = basis vectors (view space)
    return float3x3(
        right,
        newUp,
        forward
    );
}

float3 ComputeCaustics(float3 bgPositionWS)
{
    // Instead of light-space transform, try direct projection:
    Light mainLight = GetMainLight();
    
    float3 lightDirWS = normalize(-mainLight.direction); // No negation
    
    // Project position onto plane perpendicular to light
    float3 planeNormal = lightDirWS;
    float distAlongLight = dot(bgPositionWS, planeNormal);
    float3 projectedPos = bgPositionWS - (distAlongLight * planeNormal);
    
    float2 causticsUV = projectedPos.xz * _CausticScale;

    float2 uv1 = Panner(causticsUV, _CausticSpeed, 1.0 / _CausticsTex_ST.x);
    float2 uv2 = Panner(causticsUV, _CausticSpeed, - 1.0 / _CausticsTex_ST.x);

    #ifdef _CAUSTICS_CHROMATIC
    float3 c1 = SampleCaustics(uv1, _CausticSplit);
    float3 c2 = SampleCaustics(uv2, _CausticSplit);
    #else
    float3 c1 = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, uv1).rrr;
    float3 c2 = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, uv2).rrr;
    #endif

    float3 caustic = min(c1, c2);

    return caustic * _CausticStrength;
}

#endif // WATER_CAUSTICS_INCLUDED