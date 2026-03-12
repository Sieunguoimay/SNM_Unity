#ifndef WATER_SCROLL_NORMAL_INCLUDED
#define WATER_SCROLL_NORMAL_INCLUDED

TEXTURE2D(_ScrollNormalMap);
SAMPLER(sampler_ScrollNormalMap);

float4 _ScrollNormalSpeed1; // xy used
float4 _ScrollNormalSpeed2; // xy used

float3 ComputeScrollNormal(float3 worldPos)
{
    float2 baseUV = worldPos.xz * _ScrollNormalScale;

    // Two layers scrolling in different directions
    float2 uv1 = baseUV + _ScrollNormalSpeed1.xy * _Time.y;
    float2 uv2 = baseUV + _ScrollNormalSpeed2.xy * _Time.y;

    // Sample normal map (assumed to be in tangent-space DXT5nm or standard RGB)
    float3 n1 = SAMPLE_TEXTURE2D(_ScrollNormalMap, sampler_ScrollNormalMap, uv1).rgb * 2.0 - 1.0;
    float3 n2 = SAMPLE_TEXTURE2D(_ScrollNormalMap, sampler_ScrollNormalMap, uv2).rgb * 2.0 - 1.0;

    // Blend normals using Reoriented Normal Mapping (RNM) lite
    float3 blended;
    blended.xz = (n1.xz + n2.xz) * _ScrollNormalStrength;
    blended.y = 1.0;
    blended = normalize(blended);

    return blended;
}

#endif // WATER_SCROLL_NORMAL_INCLUDED
