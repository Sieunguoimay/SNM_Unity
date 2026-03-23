Shader "Snm/InteractiveGrass"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [HDR] _TopColor ("Top Tint", Color) = (1, 1, 1, 1)
        [HDR] _BottomColor ("Bottom Tint", Color) = (0.2, 0.3, 0.1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite On

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);    SAMPLER(sampler_MainTex);
            TEXTURE2D(_TrampleMap); SAMPLER(sampler_TrampleMap);
            TEXTURE2D(_WindMap);    SAMPLER(sampler_WindMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _WorldCanvas;   // xy = origin, zw = size
                float4 _WindParams;    // x = strength, y = speed, zw = map scale
                float _Cutoff;
                half4 _TopColor;
                half4 _BottomColor;
            CBUFFER_END

            StructuredBuffer<float4x4> _LocalToWorldMatrices;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float heightFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // Rotate vector `v` from direction `from` toward direction `to` using quaternion math.
            float3 RotateFromTo(float3 v, float3 from, float3 to)
            {
                float3 axis = cross(from, to);
                float cosA = dot(from, to);
                float qw = 1.0 + cosA;

                // Near-opposite: pick arbitrary perpendicular axis
                if (qw < 1e-4)
                {
                    axis = abs(from.y) < 0.999 ? cross(from, float3(0,1,0)) : cross(from, float3(1,0,0));
                    qw = 0.0;
                }

                float3 t1 = cross(axis, v);
                float3 t2 = cross(axis, t1 + qw * v);
                return v + 2.0 * t2 / dot(float4(axis, qw), float4(axis, qw));
            }

            float ease_OutCubic(float x)
            {
                float inv = 1.0 - x;
                return 1.0 - inv * inv * inv;
            }

            Varyings vert(Attributes input, uint instanceID : SV_InstanceID)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float4x4 localToWorld = _LocalToWorldMatrices[instanceID];
                float3 worldOrigin = mul(localToWorld, float4(0, 0, 0, 1)).xyz;

                // Height-based bend factor: stronger at tip, zero at root
                float height01 = saturate(input.positionOS.y);
                float bendFactor = 1.0 - pow(1.0 - height01, 3.0); // ease_OutCubic

                // World UV for sampling trample/wind maps
                float2 worldUV = saturate((worldOrigin.xz - _WorldCanvas.xy) / _WorldCanvas.zw);

                // --- Trample ---
                // Channel layout: xy = push direction, z = hold buffer, w = trample value
                float4 trample = SAMPLE_TEXTURE2D_LOD(_TrampleMap, sampler_TrampleMap, worldUV, 0);
                float trampleStrength = ease_OutCubic(trample.w); // ease_OutSine
                // float3 trampleDir = normalize(float3(
                //     trample.x * trampleStrength,
                //     1.0 - trampleStrength,
                //     trample.y * trampleStrength));
                float2 trampleDir = trample.xy * trampleStrength;

                // float trampleStrength = trample.w; // remove sin()
                // float2 trampleOffset = trample.xy * trampleStrength;

                // --- Wind ---
                float windStrength = _WindParams.x;
                float windSpeed = _WindParams.y;
                float2 windScale = _WindParams.zw;

                float2 windUV = worldUV / windScale + _Time.y * windSpeed;
                float2 wind = SAMPLE_TEXTURE2D_LOD(_WindMap, sampler_WindMap, windUV, 0).xy * 2.0 - 1.0;// + 0.5;
                // float3 windDir = float3(wind.x * windStrength, 1.0, wind.y * windStrength);
                float2 windDir = wind * windStrength;
                // float2 windOffset = wind * windStrength;

                // --- Combine ---
                float3 combined = float3(0, 1, 0);
                combined.xz = windDir + trampleDir;
                combined.y = max(0.0, 1.0 - trampleStrength);
                // combined = normalize(combined);

                float3 grassDir = normalize(lerp(float3(0, 1, 0), combined, bendFactor));
                float3 localPos = RotateFromTo(input.positionOS.xyz, float3(0, 1, 0), grassDir);
                
                float3 worldPos = mul(localToWorld, float4(localPos, 1)).xyz;

                output.positionWS = worldPos;
                output.positionCS = TransformWorldToHClip(worldPos);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.normalWS = normalize(mul((float3x3)localToWorld, input.normalOS));
                output.heightFactor = height01;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                clip(albedo.a - _Cutoff);

                // Height-based color gradient
                half3 tint = lerp(_BottomColor.rgb, _TopColor.rgb, input.heightFactor);

                Light mainLight = GetMainLight();
                float ndl = max(0, dot(input.normalWS, mainLight.direction));
                half3 lit = albedo.rgb * tint * (0.2 + ndl * 0.8);

                return half4(lit, albedo.a);
            }
            ENDHLSL
        }

        // Shadow caster pass for correct shadow casting
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _WorldCanvas;
                float4 _WindParams;
                float _Cutoff;
                half4 _TopColor;
                half4 _BottomColor;
            CBUFFER_END

            StructuredBuffer<float4x4> _LocalToWorldMatrices;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vertShadow(Attributes input, uint instanceID : SV_InstanceID)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                float4x4 localToWorld = _LocalToWorldMatrices[instanceID];
                float3 worldPos = mul(localToWorld, float4(input.positionOS.xyz, 1)).xyz;

                output.positionCS = TransformWorldToHClip(worldPos);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 fragShadow(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                clip(albedo.a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
}
