#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

TEXTURE2D(_CameraOpaqueTexture);
SAMPLER(sampler_CameraOpaqueTexture);

float ComputeThickness(float3 bgPositionWS, float3 surfacePositionWS, float3 surfaceNormalWS){

    // Vector from surface to background
    float3 surfaceToBackground = bgPositionWS - surfacePositionWS;

    // Project onto surface normal (upward direction)
    // Negative because we want depth BELOW surface
    float thickness = -dot(surfaceToBackground, surfaceNormalWS);

    // Clamp to positive (only count points below surface)
    thickness = max(0.0, thickness);

    return thickness;
}

float ComputeAbsorption(float2 screenUV, float thickness)
{
    // ----------------------
    // Beer–Lambert absorption
    // ----------------------
    float absorption = 1.0 - exp(-thickness * _Absorption);
    return absorption;
    // return pow(saturate(absorption), _AbsorptionPow);
}

float ComputeStylizedFresnel(float rawDepth, float waterDepth)
{
    float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
    float depthDifference = sceneDepth - waterDepth;
    return saturate(depthDifference / 100.0);
}