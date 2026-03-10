#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
float4x4 _ReflectionVP;
TEXTURE2D(_ReflectionTex);
SAMPLER(sampler_ReflectionTex);

float4 SampleReflection(float3 worldPos)
{
    float4 clip = mul(_ReflectionVP, float4(worldPos, 1.0));
    
    // perspective divide
    float3 ndc = clip.xyz / clip.w;
    
    // NDC -> UV
    float2 uv = ndc.xy * 0.5 + 0.5;

    return SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, uv);
}
                