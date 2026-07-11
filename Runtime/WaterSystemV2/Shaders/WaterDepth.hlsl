#ifndef WATER_V2_DEPTH_INCLUDED
#define WATER_V2_DEPTH_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

TEXTURE2D(_CameraOpaqueTexture);
SAMPLER(sampler_CameraOpaqueTexture);

float ease_OutSine(float x) { return sin((x * PI) / 2.0); }

// Water thickness along the view ray: distance from the surface down to the
// scene geometry behind it, measured along the surface normal.
float ComputeThickness(float3 bgPositionWS, float3 surfacePositionWS, float3 surfaceNormalWS)
{
    float3 surfaceToBackground = bgPositionWS - surfacePositionWS;
    return max(0.0, -dot(surfaceToBackground, surfaceNormalWS));
}

// Beer–Lambert absorption.
float ComputeAbsorption(float thickness)
{
    return 1.0 - exp(-thickness * _Absorption);
}

#endif // WATER_V2_DEPTH_INCLUDED
