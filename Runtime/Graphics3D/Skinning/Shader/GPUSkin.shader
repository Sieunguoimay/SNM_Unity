Shader "Custom/GpuSkin"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        LOD 200

        // =============================================
        // Forward Lit Pass
        // =============================================
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ GPU_SKINNING_ON BAKED_SKINNING_ON
            #pragma multi_compile _ BLEND_SHAPES_ON
            #pragma multi_compile _ BONE_OVERRIDE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "UnifiedSkinning.hlsl"
            #include "BlendShapes.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                SKINNING_VERTEX_INPUT
                BLEND_SHAPE_VERTEX_INPUT
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                Varyings o = (Varyings)0;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 posWS;
                float3 normWS;

                APPLY_BLEND_SHAPES(v);

                #if defined(GPU_SKINNING_ON)
                    float4 skinnedPos;
                    float3 skinnedNorm;
                    if (SkinLive(v.positionOS, v.normalOS, v.boneWeights, v.boneIndices, skinnedPos, skinnedNorm))
                    {
                        posWS = skinnedPos.xyz;
                        normWS = skinnedNorm;
                    }
                    else
                    {
                        posWS = TransformObjectToWorld(v.positionOS.xyz);
                        normWS = TransformObjectToWorldNormal(v.normalOS);
                    }
                #elif defined(BAKED_SKINNING_ON)
                    v.positionOS = SkinBaked(v.positionOS, v.normalOS, v.tangentOS.xyz, v.boneWeights, v.boneIndices);
                    posWS = TransformObjectToWorld(v.positionOS.xyz);
                    normWS = TransformObjectToWorldNormal(v.normalOS);
                #else
                    posWS = TransformObjectToWorld(v.positionOS.xyz);
                    normWS = TransformObjectToWorldNormal(v.normalOS);
                #endif

                o.positionCS = TransformWorldToHClip(posWS);
                o.positionWS = posWS;
                o.normalWS = normWS;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;

                float3 normalWS = normalize(i.normalWS);
                Light mainLight = GetMainLight();
                half ndl = saturate(dot(normalWS, mainLight.direction));
                half3 lighting = mainLight.color * ndl + half3(0.2, 0.2, 0.2);

                half4 color = half4(baseColor.rgb * lighting, baseColor.a);
                color.rgb = MixFog(color.rgb, i.fogFactor);
                return color;
            }
            ENDHLSL
        }

        // =============================================
        // Shadow Caster Pass
        // =============================================
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
            #pragma multi_compile _ GPU_SKINNING_ON BAKED_SKINNING_ON
            #pragma multi_compile _ BLEND_SHAPES_ON
            #pragma multi_compile _ BONE_OVERRIDE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "UnifiedSkinning.hlsl"
            #include "BlendShapes.hlsl"

            float3 _LightDirection;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                SKINNING_VERTEX_INPUT
                BLEND_SHAPE_VERTEX_INPUT
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            ShadowVaryings vertShadow(ShadowAttributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                ShadowVaryings o = (ShadowVaryings)0;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 posWS;
                float3 normWS;

                APPLY_BLEND_SHAPES(v);

                #if defined(GPU_SKINNING_ON)
                    float4 skinnedPos;
                    float3 skinnedNorm;
                    if (SkinLive(v.positionOS, v.normalOS, v.boneWeights, v.boneIndices, skinnedPos, skinnedNorm))
                    {
                        posWS = skinnedPos.xyz;
                        normWS = skinnedNorm;
                    }
                    else
                    {
                        posWS = TransformObjectToWorld(v.positionOS.xyz);
                        normWS = TransformObjectToWorldNormal(v.normalOS);
                    }
                #elif defined(BAKED_SKINNING_ON)
                    v.positionOS = SkinBakedShadow(v.positionOS, v.boneWeights, v.boneIndices);
                    posWS = TransformObjectToWorld(v.positionOS.xyz);
                    normWS = TransformObjectToWorldNormal(v.normalOS);
                #else
                    posWS = TransformObjectToWorld(v.positionOS.xyz);
                    normWS = TransformObjectToWorldNormal(v.normalOS);
                #endif

                o.positionCS = TransformWorldToHClip(ApplyShadowBias(posWS, normWS, _LightDirection));

                #if UNITY_REVERSED_Z
                    o.positionCS.z = min(o.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    o.positionCS.z = max(o.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return o;
            }

            half4 fragShadow(ShadowVaryings i) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // =============================================
        // Depth Only Pass
        // =============================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth
            #pragma multi_compile_instancing
            #pragma multi_compile _ GPU_SKINNING_ON BAKED_SKINNING_ON
            #pragma multi_compile _ BLEND_SHAPES_ON
            #pragma multi_compile _ BONE_OVERRIDE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "UnifiedSkinning.hlsl"
            #include "BlendShapes.hlsl"

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                SKINNING_VERTEX_INPUT
                BLEND_SHAPE_VERTEX_INPUT
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            DepthVaryings vertDepth(DepthAttributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                DepthVaryings o = (DepthVaryings)0;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 posWS;

                APPLY_BLEND_SHAPES(v);

                #if defined(GPU_SKINNING_ON)
                    float4 skinnedPos;
                    float3 skinnedNorm;
                    if (SkinLive(v.positionOS, v.normalOS, v.boneWeights, v.boneIndices, skinnedPos, skinnedNorm))
                        posWS = skinnedPos.xyz;
                    else
                        posWS = TransformObjectToWorld(v.positionOS.xyz);
                #elif defined(BAKED_SKINNING_ON)
                    v.positionOS = SkinBakedShadow(v.positionOS, v.boneWeights, v.boneIndices);
                    posWS = TransformObjectToWorld(v.positionOS.xyz);
                #else
                    posWS = TransformObjectToWorld(v.positionOS.xyz);
                #endif

                o.positionCS = TransformWorldToHClip(posWS);
                return o;
            }

            half4 fragDepth(DepthVaryings i) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
