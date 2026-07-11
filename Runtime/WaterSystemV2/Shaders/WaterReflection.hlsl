#ifndef WATER_V2_REFLECTION_INCLUDED
#define WATER_V2_REFLECTION_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

float4x4 _ReflectionVP;
TEXTURE2D(_ReflectionTex);
SAMPLER(sampler_ReflectionTex);

// Projects a world position through the reflection camera's VP matrix and
// samples the planar reflection texture rendered by PlanarReflection.cs.
float4 SampleReflection(float3 worldPos)
{
    float4 clip = mul(_ReflectionVP, float4(worldPos, 1.0));
    float3 ndc = clip.xyz / clip.w;
    float2 uv = ndc.xy * 0.5 + 0.5;
    return SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, uv);
}

#endif // WATER_V2_REFLECTION_INCLUDED
