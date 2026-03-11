Shader "Custom/WaterSurface"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}

        _ShallowColor("Shallow Color", Color) = (1, 1, 1, 1)
        _DeepColor("Deep Color", Color) = (0, 0, 0, 0)
        _Absorption("Absorption", Float) = .4
        _AbsorptionPow("Absorption Pow", Float) = .5

        _CausticsTex("CausticsTex", 2D) = "white" {}
        _CausticStrength("CausticStrength", Float) = 1
        _CausticScale("CausticScale", Float) = .1
        _CausticSpeed("CausticSpeed", Float) = .05
        _CausticFadeDepth("CausticFadeDepth",Float) = 1
        _CausticSplit("CausticSplit", Float) = .003
        _CausticAbsorption("CausticAbsorption", Float) = 1

        _WaveNormalStrength("Wave Normal Strength", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ----------------------
            // Water parameters
            // ----------------------
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;

            float3 _ShallowColor;
            float3 _DeepColor;
            float  _Absorption;
            float  _AbsorptionPow;

            //Caustics
            float _CausticStrength;
            float _CausticScale;
            float _CausticSpeed;
            float _CausticFadeDepth;
            float _CausticSplit;
            float _CausticAbsorption;

            // Wave
            float _WaveNormalStrength;
            CBUFFER_END

            // Wave heightfield
            TEXTURE2D(_WaveTex);
            SAMPLER(sampler_WaveTex);
            float4 _WaveTex_TexelSize;

            // URP includes
            #include "WaterDepth.hlsl"
            #include "WaterCaustics.hlsl"
            #include "Reflection.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos  : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            float SampleWaveHeight(float2 uv)
            {
                return SAMPLE_TEXTURE2D_LOD(_WaveTex, sampler_WaveTex, uv, 0).r;
            }

            float3 ComputeWaveNormal(float2 uv)
            {
                float2 texel = _WaveTex_TexelSize.xy;

                float hL = SampleWaveHeight(uv + float2(-texel.x, 0));
                float hR = SampleWaveHeight(uv + float2( texel.x, 0));
                float hD = SampleWaveHeight(uv + float2(0, -texel.y));
                float hU = SampleWaveHeight(uv + float2(0,  texel.y));

                float3 normal = normalize(float3(
                    (hL - hR) * _WaveNormalStrength,
                    1.0,
                    (hD - hU) * _WaveNormalStrength));

                return normal;
            }

            float ComputeStylizedFresnel(float3 normalWS, float3 viewDir, float rawDepth, float waterDepth)
            {
                float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float depthDifference = sceneDepth - waterDepth;
                float depthFade = ease_OutSine(saturate(depthDifference / 50.0));

                float NdotV = saturate(dot(normalWS, viewDir));
                float fresnel = pow(1.0 - NdotV, 4.0);

                return max(depthFade, fresnel);
            }

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.screenPos  = ComputeScreenPos(o.positionCS);
                o.worldPos  = TransformObjectToWorld(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            float4 frag (Varyings i) : SV_Target
            {
                float3 worldPos = i.worldPos;
                float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);

                // ----------------------
                // Wave normal
                // ----------------------
                float3 waveNormal = ComputeWaveNormal(i.uv);
                float3 normalWS = normalize(
                    TransformObjectToWorldNormal(waveNormal));

                // ----------------------
                // Screen UV with refraction offset
                // ----------------------
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float2 refractionOffset = normalWS.xz * 0.02;
                float2 refractedUV = screenUV + refractionOffset;

                float surfaceDepth = i.screenPos.z / i.screenPos.w;
                float rawDepth = SampleSceneDepth(refractedUV);
                float3 bgPositionWS = ComputeWorldSpacePosition(refractedUV, rawDepth, UNITY_MATRIX_I_VP);
                float thickness = ComputeThickness(bgPositionWS, worldPos, normalWS);
                float absorption = ComputeAbsorption(refractedUV, thickness);

                float3 backgroundColor = SAMPLE_TEXTURE2D(
                    _CameraOpaqueTexture, sampler_CameraOpaqueTexture, refractedUV).rgb;
                float3 caustics = ComputeCaustics(bgPositionWS);

                // Perturb reflection sample by wave normal
                float3 reflWorldPos = worldPos + float3(normalWS.x, 0, normalWS.z) * 0.5;
                float4 reflectionColor = SampleReflection(reflWorldPos);

                float shadowAttenuation = 1.0;
                Light mainLight = GetMainLight();
                shadowAttenuation = mainLight.shadowAttenuation;

                // #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                // float4 shadowCoord = TransformWorldToShadowCoord(bgPositionWS);
                // shadowAttenuation = MainLightRealtimeShadow(shadowCoord);
                // #endif

                float fresnel = ComputeStylizedFresnel(normalWS, viewDir, rawDepth, surfaceDepth);

                float3 waterColor = lerp(backgroundColor * _ShallowColor, _DeepColor, absorption);
                float reflectionWeight = fresnel * reflectionColor.a;
                float3 final = waterColor * (1.0 - reflectionWeight)
                             + reflectionColor.rgb * reflectionWeight
                             + caustics;

                return float4(final, 1.0);
            }

            ENDHLSL
        }
    }
}
