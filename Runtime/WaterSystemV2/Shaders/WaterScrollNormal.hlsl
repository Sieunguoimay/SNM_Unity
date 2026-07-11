#ifndef WATER_V2_SCROLL_NORMAL_INCLUDED
#define WATER_V2_SCROLL_NORMAL_INCLUDED

TEXTURE2D(_ScrollNormalMap);
SAMPLER(sampler_ScrollNormalMap);

// Two normal-map layers scrolling in different directions, blended.
float3 ComputeScrollNormal(float3 worldPos)
{
    float2 baseUV = worldPos.xz * _ScrollNormalScale;

    float2 uv1 = baseUV + _ScrollNormalSpeed1.xy * _Time.y;
    float2 uv2 = baseUV + _ScrollNormalSpeed2.xy * _Time.y;

    float3 n1 = SAMPLE_TEXTURE2D(_ScrollNormalMap, sampler_ScrollNormalMap, uv1).rgb * 2.0 - 1.0;
    float3 n2 = SAMPLE_TEXTURE2D(_ScrollNormalMap, sampler_ScrollNormalMap, uv2).rgb * 2.0 - 1.0;

    float3 blended;
    blended.xz = (n1.xz + n2.xz) * _ScrollNormalStrength;
    blended.y = 1.0;
    return normalize(blended);
}

#endif // WATER_V2_SCROLL_NORMAL_INCLUDED
