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

        // Foam
        _FoamTex("Foam Texture", 2D) = "white" {}
        _FoamStrength("Foam Strength", Float) = 1
        _FoamDepthThreshold("Foam Depth Threshold", Float) = 1
        _FoamScale("Foam Scale", Float) = 0.5
        _FoamSpeed("Foam Speed", Float) = 0.05

        // Shoreline
        _ShorelineWaveCount("Shoreline Wave Count", Int) = 3
        _ShorelineSpeed("Shoreline Speed", Float) = 0.5
        _ShorelineFoamStrength("Shoreline Foam Strength", Float) = 1
        _ShorelineFoamScale("Shoreline Foam Scale", Float) = 1
        _ShorelineMaxDepth("Shoreline Max Depth", Float) = 3

        // Sparkle
        _SparkleIntensity("Sparkle Intensity", Float) = 1
        _SparkleDensity("Sparkle Density", Float) = 30
        _SparkleSpeed("Sparkle Speed", Float) = 0.5

        // Scroll Normal
        _ScrollNormalMap("Scroll Normal Map", 2D) = "bump" {}
        _ScrollNormalStrength("Scroll Normal Strength", Float) = 0.5
        _ScrollNormalScale("Scroll Normal Scale", Float) = 0.2


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

            #pragma multi_compile_local _ _CAUSTICS_ON
            #pragma multi_compile_local _ _CAUSTICS_CHROMATIC
            #pragma multi_compile_local _ _REFLECTION_ON
            #pragma multi_compile_local _ _SPECULAR_ON
            #pragma multi_compile_local _ _FOAM_ON
            #pragma multi_compile_local _ _SHORELINE_ON
            #pragma multi_compile_local _ _SPARKLE_ON
            #pragma multi_compile_local _ _SCROLL_NORMAL_ON


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

            // Foam
            float _FoamStrength;
            float _FoamDepthThreshold;
            float _FoamScale;
            float _FoamSpeed;

            // Shoreline
            int   _ShorelineWaveCount;
            float _ShorelineSpeed;
            float _ShorelineFoamStrength;
            float _ShorelineFoamScale;
            float _ShorelineMaxDepth;

            // Sparkle
            float _SparkleIntensity;
            float _SparkleDensity;
            float _SparkleSpeed;

            // Scroll Normal
            float _ScrollNormalStrength;
            float _ScrollNormalScale;


            CBUFFER_END

            // Wave heightfield
            TEXTURE2D(_WaveTex);
            SAMPLER(sampler_WaveTex);
            float4 _WaveTex_TexelSize;

            // URP includes
            #include "WaterDepth.hlsl"
            #include "WaterCaustics.hlsl"
            #include "Reflection.hlsl"
            #include "WaterFoam.hlsl"
            #include "WaterShoreline.hlsl"
            #include "WaterSparkle.hlsl"
            #include "WaterScrollNormal.hlsl"


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
                float depthDifference;

                if (unity_OrthoParams.w > 0.5)
                {
                    // Ortho: depth buffer is linear, not hyperbolic.
                    #if defined(UNITY_REVERSED_Z)
                        float sceneEye  = lerp(_ProjectionParams.z, _ProjectionParams.y, rawDepth);
                        float surfaceEye = lerp(_ProjectionParams.z, _ProjectionParams.y, waterDepth);
                    #else
                        float sceneEye  = lerp(_ProjectionParams.y, _ProjectionParams.z, rawDepth);
                        float surfaceEye = lerp(_ProjectionParams.y, _ProjectionParams.z, waterDepth);
                    #endif
                    depthDifference = sceneEye - surfaceEye;
                }
                else
                {
                    float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                    depthDifference = sceneDepth - waterDepth;
                }

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
                // Scrolling normal map (blends into surface normal)
                // ----------------------
                #ifdef _SCROLL_NORMAL_ON
                float3 scrollN = ComputeScrollNormal(worldPos);
                // Blend scrolling normal with wave normal
                normalWS = normalize(float3(
                    normalWS.x + scrollN.x,
                    normalWS.y,
                    normalWS.z + scrollN.z));
                #endif

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

                #ifdef _CAUSTICS_ON
                float3 caustics = ComputeCaustics(bgPositionWS);
                #else
                float3 caustics = float3(0, 0, 0);
                #endif

                // Perturb reflection sample by wave normal
                #ifdef _REFLECTION_ON
                float3 reflWorldPos = worldPos + float3(normalWS.x, 0, normalWS.z) * 0.5;
                float4 reflectionColor = SampleReflection(reflWorldPos);
                #else
                float4 reflectionColor = float4(0, 0, 0, 0);
                #endif

                // ----------------------
                // Shadows
                // ----------------------
                Light mainLight = GetMainLight();
                float shadowAttenuation = 1.0;
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                float4 shadowCoord = TransformWorldToShadowCoord(worldPos);
                shadowAttenuation = MainLightRealtimeShadow(shadowCoord);
                #endif

                float fresnel = ComputeStylizedFresnel(normalWS, viewDir, rawDepth, surfaceDepth);

                float3 waterColor = lerp(backgroundColor * _ShallowColor, _DeepColor, absorption);

                // Mask caustics by shadow so light patterns don't appear in shadow
                float3 maskedCaustics = caustics * shadowAttenuation;

                // ----------------------
                // Foam (edge foam where water meets geometry)
                // ----------------------
                #ifdef _FOAM_ON
                float foamAmount = ComputeFoam(worldPos, thickness);
                waterColor = lerp(waterColor, float3(1, 1, 1), foamAmount);
                #endif

                // ----------------------
                // Shoreline waves (animated foam bands rolling to shore)
                // ----------------------
                #ifdef _SHORELINE_ON
                float shorelineFoam = ComputeShoreline(thickness);
                waterColor = lerp(waterColor, float3(1, 1, 1), shorelineFoam);
                #endif

                // ----------------------
                // Specular (GGX)
                // ----------------------
                #ifdef _SPECULAR_ON
                float3 halfDir = normalize(mainLight.direction + viewDir);
                float NdotH = saturate(dot(normalWS, halfDir));
                float roughness = 0.1;
                float a2 = roughness * roughness;
                float denom = NdotH * NdotH * (a2 - 1.0) + 1.0;
                float D = a2 / (PI * denom * denom);
                float3 specular = mainLight.color * D * shadowAttenuation * 0.25;
                #else
                float3 specular = float3(0, 0, 0);
                #endif

                float reflectionWeight = fresnel * reflectionColor.a;
                float3 final = waterColor * (1.0 - reflectionWeight)
                             + reflectionColor.rgb * reflectionWeight
                             + maskedCaustics
                             + specular;

                // ----------------------
                // Surface sparkle (additive glints)
                // ----------------------
                #ifdef _SPARKLE_ON
                float sparkle = ComputeSparkle(worldPos, normalWS, viewDir);
                final += mainLight.color * sparkle * shadowAttenuation;
                #endif

                return float4(final, 1.0);
            }

            ENDHLSL
        }
    }
}
