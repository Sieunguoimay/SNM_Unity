// Simplified Bumped Specular shader for URP.
// - no Main Color nor Specular Color
// - specular directions approximated per vertex
// - Normalmap uses Tiling/Offset of the Base texture
// - fully supports only 1 directional light

Shader "AnimationInstancing/Bumped Specular_instancing"
{
    Properties
    {
        _Shininess ("Shininess", Range (0.03, 1)) = 0.078125
        _BaseMap ("Base (RGB) Gloss (A)", 2D) = "white" {}
        [NoScaleOffset] _BumpMap ("Normalmap", 2D) = "bump" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        LOD 250

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ BAKED_SKINNING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "../../GPUSkinning/Shader/UnifiedSkinning.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half _Shininess;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                SKINNING_VERTEX_INPUT
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float3 tangentWS   : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                half3  halfDirWS   : TEXCOORD5;
                float  fogFactor   : TEXCOORD6;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                Varyings o = (Varyings)0;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                #if defined(BAKED_SKINNING_ON)
                    v.positionOS = SkinBaked(v.positionOS, v.normalOS, v.tangentOS.xyz, v.boneWeights, v.boneIndices);
                #endif

                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(posWS);
                o.positionWS = posWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.tangentWS = TransformObjectToWorldDir(v.tangentOS.xyz);
                o.bitangentWS = cross(o.normalWS, o.tangentWS) * v.tangentOS.w;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);

                // Per-vertex half-direction (mobile optimization)
                float3 viewDir = GetWorldSpaceNormalizeViewDir(posWS);
                Light mainLight = GetMainLight();
                o.halfDirWS = normalize(mainLight.direction + viewDir);

                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv));

                float3x3 tbn = float3x3(normalize(i.tangentWS), normalize(i.bitangentWS), normalize(i.normalWS));
                float3 normalWS = normalize(mul(normalTS, tbn));

                Light mainLight = GetMainLight();
                half ndl = saturate(dot(normalWS, mainLight.direction));
                half ndh = saturate(dot(normalWS, normalize(i.halfDirWS)));
                half spec = pow(ndh, _Shininess * 128.0) * tex.a;

                half4 color;
                color.rgb = (tex.rgb * mainLight.color * ndl + mainLight.color * spec);
                color.a = 1.0;
                color.rgb = MixFog(color.rgb, i.fogFactor);
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_instancing
            #pragma multi_compile _ BAKED_SKINNING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "../../GPUSkinning/Shader/UnifiedSkinning.hlsl"

            float3 _LightDirection;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                SKINNING_VERTEX_INPUT
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            ShadowVaryings vertShadow(ShadowAttributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                ShadowVaryings o;

                #if defined(BAKED_SKINNING_ON)
                    v.positionOS = SkinBakedShadow(v.positionOS, v.boneWeights, v.boneIndices);
                #endif

                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 normWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionCS = TransformWorldToHClip(ApplyShadowBias(posWS, normWS, _LightDirection));
                #if UNITY_REVERSED_Z
                    o.positionCS.z = min(o.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    o.positionCS.z = max(o.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return o;
            }

            half4 fragShadow(ShadowVaryings i) : SV_Target { return 0; }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
