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

            // ----------------------
            // Water parameters
            // ----------------------
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;

            float3 _ShallowColor;
            float3 _DeepColor;
            float  _Absorption;     // Controls how fast water darkens
            float  _AbsorptionPow;

            //Caustics
            float _CausticStrength;
            float _CausticScale;
            float _CausticSpeed;
            float _CausticFadeDepth;
            float _CausticSplit;
            float _CausticAbsorption;
            CBUFFER_END

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
            };

            float ComputeStylizedFresnel(float rawDepth, float waterDepth)
            {
                float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float depthDifference = sceneDepth - waterDepth;
                return ease_OutSine(saturate(depthDifference / 50.0));
            }

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.screenPos  = ComputeScreenPos(o.positionCS);
                o.worldPos  = TransformObjectToWorld(v.positionOS.xyz);
                return o;
            }

            float4 frag (Varyings i) : SV_Target
            {
                float3 normalWS = float3(0, 1, 0);
                float3 worldPos = i.worldPos;
            
                // ----------------------
                // Screen UV
                // ----------------------
                float2 screenUV = i.screenPos.xy / i.screenPos.w;

                float surfaceDepth = i.screenPos.z / i.screenPos.w;
                float rawDepth = SampleSceneDepth(screenUV);
                float3 bgPositionWS = ComputeWorldSpacePosition(screenUV, rawDepth, UNITY_MATRIX_I_VP);
                float thickness = ComputeThickness(bgPositionWS, worldPos, normalWS);
                float absorption = ComputeAbsorption(screenUV, thickness);
                
                float3 backgroundColor = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV).rgb;
                float3 caustics = ComputeCaustics(bgPositionWS);
                float4 reflectionColor = SampleReflection(worldPos);
                
                float fresnel = ComputeStylizedFresnel(rawDepth, surfaceDepth);

                float3 waterColor = lerp(backgroundColor * _ShallowColor, _DeepColor, absorption);
                float reflectionWeight = fresnel * reflectionColor.a;
                // float3 final = lerp(waterColor, reflectionColor.rgb, reflectionWeight) + caustics;
                float3 final = waterColor * (1.0 - reflectionWeight) + reflectionColor.rgb * reflectionWeight + caustics;

                // return fresnel;
                // return float4(reflectionColor + caustics, 1.0);
                return float4(final, 1.0);
            }

            ENDHLSL
        }
    }
}
