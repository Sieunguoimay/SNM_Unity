// WaterSystemV2 surface shader. One forward URP transparent pass compositing
// depth absorption, refraction, caustics, waves, foam, shoreline, specular,
// sparkle and planar reflection. Every property name here must match
// WaterShaderIds.cs — that C# class is the single source of truth.
Shader "Snm/WaterSystemV2/WaterSurface"
{
    Properties
    {
        _ShallowColor("Shallow Color", Color) = (1, 1, 1, 1)
        _DeepColor("Deep Color", Color) = (0, 0, 0, 0)
        _Absorption("Absorption", Float) = 0.4
        _RefractionStrength("Refraction Strength", Float) = 0.02

        _CausticsTex("Caustics Texture", 2D) = "white" {}
        _CausticStrength("Caustic Strength", Float) = 1
        _CausticScale("Caustic Scale", Float) = 0.1
        _CausticSpeed("Caustic Speed", Float) = 0.05
        _CausticSplit("Caustic Split", Float) = 0.003

        _WaveTex("Wave Heightfield", 2D) = "black" {}
        _WaveNormalStrength("Wave Normal Strength", Float) = 1.0

        _FoamTex("Foam Texture", 2D) = "white" {}
        _FoamStrength("Foam Strength", Float) = 1
        _FoamDepthThreshold("Foam Depth Threshold", Float) = 0.5
        _FoamScale("Foam Scale", Float) = 0.5
        _FoamSpeed("Foam Speed", Float) = 0.05

        // Shoreline (driven by UV1 baked by WaterShoreBaker)
        _ShorelineWaveCount("Shoreline Wave Count", Int) = 3
        _ShorelineSpeed("Shoreline Speed", Float) = 0.5
        _ShorelineFoamStrength("Shoreline Foam Strength", Float) = 1
        _ShorelineFoamScale("Shoreline Foam Scale", Float) = 1

        _SparkleIntensity("Sparkle Intensity", Float) = 1
        _SparkleDensity("Sparkle Density", Float) = 30
        _SparkleSpeed("Sparkle Speed", Float) = 0.5

        _ScrollNormalMap("Scroll Normal Map", 2D) = "bump" {}
        _ScrollNormalStrength("Scroll Normal Strength", Float) = 0.5
        _ScrollNormalScale("Scroll Normal Scale", Float) = 0.2
        _ScrollNormalSpeed1("Scroll Normal Speed 1", Vector) = (0.03, 0.02, 0, 0)
        _ScrollNormalSpeed2("Scroll Normal Speed 2", Vector) = (-0.02, 0.03, 0, 0)

        // 0 off · 1 wave height · 2 normals · 3 shore distance
        _DebugView("Debug View", Float) = 0
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

            CBUFFER_START(UnityPerMaterial)
            float3 _ShallowColor;
            float3 _DeepColor;
            float  _Absorption;
            float  _RefractionStrength;

            float _CausticStrength;
            float _CausticScale;
            float _CausticSpeed;
            float _CausticSplit;

            float _WaveNormalStrength;

            float _FoamStrength;
            float _FoamDepthThreshold;
            float _FoamScale;
            float _FoamSpeed;

            int   _ShorelineWaveCount;
            float _ShorelineSpeed;
            float _ShorelineFoamStrength;
            float _ShorelineFoamScale;

            float _SparkleIntensity;
            float _SparkleDensity;
            float _SparkleSpeed;

            float _ScrollNormalStrength;
            float _ScrollNormalScale;
            float4 _ScrollNormalSpeed1; // xy used
            float4 _ScrollNormalSpeed2; // xy used

            float _DebugView;
            CBUFFER_END

            TEXTURE2D(_WaveTex);
            SAMPLER(sampler_WaveTex);
            float4 _WaveTex_TexelSize;

            #include "WaterDepth.hlsl"
            #include "WaterCaustics.hlsl"
            #include "WaterReflection.hlsl"
            #include "WaterFoam.hlsl"
            #include "WaterShoreline.hlsl"
            #include "WaterSparkle.hlsl"
            #include "WaterScrollNormal.hlsl"

            #define DEBUG_OFF 0
            #define DEBUG_WAVE_HEIGHT 1
            #define DEBUG_NORMALS 2
            #define DEBUG_SHORE_DISTANCE 3

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float2 uv1        : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos  : TEXCOORD0;
                float3 worldPos   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float2 uv1        : TEXCOORD3;
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

                return normalize(float3(
                    (hL - hR) * _WaveNormalStrength,
                    1.0,
                    (hD - hU) * _WaveNormalStrength));
            }

            // Stylized fresnel: view-angle fresnel, but never darker than the
            // depth fade so far-away water still reflects.
            float ComputeStylizedFresnel(float3 normalWS, float3 viewDir, float rawDepth, float waterDepth)
            {
                float depthDifference;

                if (unity_OrthoParams.w > 0.5)
                {
                    // Ortho: depth buffer is linear, not hyperbolic.
                    #if defined(UNITY_REVERSED_Z)
                        float sceneEye   = lerp(_ProjectionParams.z, _ProjectionParams.y, rawDepth);
                        float surfaceEye = lerp(_ProjectionParams.z, _ProjectionParams.y, waterDepth);
                    #else
                        float sceneEye   = lerp(_ProjectionParams.y, _ProjectionParams.z, rawDepth);
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
                o.worldPos   = TransformObjectToWorld(v.positionOS.xyz);
                o.uv = v.uv;
                o.uv1 = v.uv1;
                return o;
            }

            float4 frag (Varyings i) : SV_Target
            {
                float3 worldPos = i.worldPos;
                float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);

                // ── surface normal: waves + optional scrolling normal map ──
                float3 waveNormal = ComputeWaveNormal(i.uv);
                float3 normalWS = normalize(TransformObjectToWorldNormal(waveNormal));

                #ifdef _SCROLL_NORMAL_ON
                float3 scrollN = ComputeScrollNormal(worldPos);
                normalWS = normalize(float3(
                    normalWS.x + scrollN.x,
                    normalWS.y,
                    normalWS.z + scrollN.z));
                #endif

                // ── refraction: sample the scene slightly offset by normal ─
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float2 refractedUV = screenUV + normalWS.xz * _RefractionStrength;

                float surfaceDepth = i.screenPos.z / i.screenPos.w;
                float rawDepth = SampleSceneDepth(refractedUV);
                float3 bgPositionWS = ComputeWorldSpacePosition(refractedUV, rawDepth, UNITY_MATRIX_I_VP);
                float thickness = ComputeThickness(bgPositionWS, worldPos, normalWS);
                float absorption = ComputeAbsorption(thickness);

                float3 backgroundColor = SAMPLE_TEXTURE2D(
                    _CameraOpaqueTexture, sampler_CameraOpaqueTexture, refractedUV).rgb;

                #ifdef _CAUSTICS_ON
                float3 caustics = ComputeCaustics(bgPositionWS);
                #else
                float3 caustics = float3(0, 0, 0);
                #endif

                // Perturb the reflection sample by the wave normal.
                #ifdef _REFLECTION_ON
                float3 reflWorldPos = worldPos + float3(normalWS.x, 0, normalWS.z) * 0.5;
                float4 reflectionColor = SampleReflection(reflWorldPos);
                #else
                float4 reflectionColor = float4(0, 0, 0, 0);
                #endif

                // ── shadows ────────────────────────────────────────────────
                Light mainLight = GetMainLight();
                float shadowAttenuation = 1.0;
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                float4 shadowCoord = TransformWorldToShadowCoord(worldPos);
                shadowAttenuation = MainLightRealtimeShadow(shadowCoord);
                #endif

                float fresnel = ComputeStylizedFresnel(normalWS, viewDir, rawDepth, surfaceDepth);

                float3 waterColor = lerp(backgroundColor * _ShallowColor, _DeepColor, absorption);

                // Mask caustics by shadow so light patterns stay out of shade.
                float3 maskedCaustics = caustics * shadowAttenuation;

                #ifdef _FOAM_ON
                float foamAmount = ComputeFoam(worldPos, thickness);
                waterColor = lerp(waterColor, float3(1, 1, 1), foamAmount);
                #endif

                #ifdef _SHORELINE_ON
                float shorelineFoam = ComputeShoreline(i.uv1);
                waterColor = lerp(waterColor, float3(1, 1, 1), shorelineFoam);
                #endif

                #ifdef _SPECULAR_ON
                // GGX distribution, fixed low roughness.
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

                #ifdef _SPARKLE_ON
                float sparkle = ComputeSparkle(worldPos, normalWS, viewDir);
                final += mainLight.color * sparkle * shadowAttenuation;
                #endif

                // ── debug views (WaterBody inspector → Debug View) ─────────
                if (_DebugView >= DEBUG_WAVE_HEIGHT)
                {
                    if (_DebugView == DEBUG_WAVE_HEIGHT)
                    {
                        float h = saturate(SampleWaveHeight(i.uv) * 0.5 + 0.5);
                        return float4(h, h, h, 1.0);
                    }
                    if (_DebugView == DEBUG_NORMALS)
                        return float4(normalWS * 0.5 + 0.5, 1.0);
                    if (_DebugView == DEBUG_SHORE_DISTANCE)
                    {
                        // Red at the shore → blue in deep water. Solid red
                        // everywhere = UV1 missing (mesh not baked).
                        float d = saturate(i.uv1.x);
                        return float4(1.0 - d, 0.1, d, 1.0);
                    }
                }

                return float4(final, 1.0);
            }

            ENDHLSL
        }
    }
}
