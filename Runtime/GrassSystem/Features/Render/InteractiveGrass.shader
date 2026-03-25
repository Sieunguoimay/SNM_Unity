// GPU-instanced grass shader with wind animation and trample interaction.
// Each blade is positioned via a StructuredBuffer of per-instance matrices,
// then bent by wind (scrolling noise map) and trample (persistent RT written by interactors).
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

        Cull Off   // Grass blades are visible from both sides
        ZWrite On

        // =====================================================================
        // Forward Lit Pass — renders grass with lighting, wind, and trample
        // =====================================================================
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
            TEXTURE2D(_TrampleMap); SAMPLER(sampler_TrampleMap); // RT written by EnvironmentInteractionSystem
            TEXTURE2D(_WindMap);    SAMPLER(sampler_WindMap);     // Scrolling noise texture for wind

            float _BladeHeight; // Set globally from C#; used to normalize vertex height

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _WorldCanvas;   // xy = world-space origin, zw = world-space size of the grass area
                float4 _WindParams;    // x = strength, y = scroll speed, zw = UV tiling scale
                float _Cutoff;
                half4 _TopColor;
                half4 _BottomColor;
            CBUFFER_END

            // Per-instance transform matrices uploaded from C# (one per grass blade)
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
                float heightFactor : TEXCOORD3; // 0 at root, 1 at tip
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // Rotates vector `v` so that direction `from` maps to direction `to`.
            // Uses a quaternion constructed from the half-angle between the two directions.
            float3 RotateFromTo(float3 v, float3 from, float3 to)
            {
                float3 axis = cross(from, to);
                float cosA = dot(from, to);
                float qw = 1.0 + cosA;

                // Near-opposite directions: pick an arbitrary perpendicular axis
                if (qw < 1e-4)
                {
                    axis = abs(from.y) < 0.999 ? cross(from, float3(0,1,0)) : cross(from, float3(1,0,0));
                    qw = 0.0;
                }

                // Apply quaternion rotation: v + 2 * cross(q.xyz, cross(q.xyz, v) + q.w * v) / |q|^2
                float3 t1 = cross(axis, v);
                float3 t2 = cross(axis, t1 + qw * v);
                return v + 2.0 * t2 / dot(float4(axis, qw), float4(axis, qw));
            }

            // Easing: fast start, decelerating end — used for trample strength falloff
            float ease_OutCubic(float x)
            {
                float inv = 1.0 - x;
                return 1.0 - inv * inv * inv;
            }

            // Easing: slow start, accelerating — used for height-based bend factor
            float ease_InCubic(float x)
            {
                return x * x * x;
            }

            Varyings vert(Attributes input, uint instanceID : SV_InstanceID)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float4x4 localToWorld = _LocalToWorldMatrices[instanceID];
                float3 worldOrigin = mul(localToWorld, float4(0, 0, 0, 1)).xyz;

                // Normalized height along the blade (0 = root, 1 = tip).
                // bendFactor uses cubic easing so the base stays stiff and the tip bends most.
                float height01 = saturate(input.positionOS.y / _BladeHeight);
                float bendFactor = ease_InCubic(height01);

                // Map blade world position to [0,1] UV within the grass canvas
                float2 worldUV = saturate((worldOrigin.xz - _WorldCanvas.xy) / _WorldCanvas.zw);

                // --- Trample ---
                // _TrampleMap channel layout: xy = push direction, z = hold buffer, w = trample intensity
                float4 trample = SAMPLE_TEXTURE2D_LOD(_TrampleMap, sampler_TrampleMap, worldUV, 0);
                float trampleStrength = ease_OutCubic(trample.w);
                float2 trampleDir = trample.xy;

                // --- Wind ---
                float2 windUV = worldUV / _WindParams.zw + _Time.y * _WindParams.y;
                float2 wind = SAMPLE_TEXTURE2D_LOD(_WindMap, sampler_WindMap, windUV, 0).xy * 2.0 - 1.0;
                float2 windDir = wind * _WindParams.x;

                // --- Combine bend directions in world space ---
                // The blade direction starts upright (0,1,0). Wind and trample push it sideways (xz).
                // Under heavy trample, y collapses toward 0 so the blade flattens to the ground.
                float3 combinedWS = float3(0, 1, 0);
                combinedWS.xz = windDir + trampleDir * trampleStrength;
                combinedWS.y = max(0.0, 1.0 - trampleStrength);

                // Convert combined bend direction from world space into blade-local space
                float3x3 worldToLocal = (float3x3)transpose((float3x3)localToWorld);
                float3 combinedLS = mul(worldToLocal, combinedWS);

                // Blend between the combined bend and straight-up based on how high up the blade we are.
                // When trampled, the entire blade bends (bendFactor is overridden toward 0).
                float3 grassDir = normalize(lerp(combinedLS, float3(0, 1, 0), lerp(bendFactor, 0.0, trampleStrength)));
                float3 localPos = RotateFromTo(input.positionOS.xyz, float3(0, 1, 0), grassDir);

                float3 worldPos = mul(localToWorld, float4(localPos, 1)).xyz;

                output.positionWS = worldPos;
                output.positionCS = TransformWorldToHClip(worldPos);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.normalWS = normalize(mul((float3x3)localToWorld, input.normalOS));
                output.heightFactor = height01;

                return output;
            }

            half4 frag(Varyings input, bool facing : SV_IsFrontFace) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                clip(albedo.a - _Cutoff); // Alpha test cutout

                // Flip normal for back faces so both sides receive correct lighting
                float3 normal = facing ? input.normalWS : -input.normalWS;

                // Gradient tint from root (_BottomColor) to tip (_TopColor)
                half3 tint = lerp(_BottomColor.rgb, _TopColor.rgb, input.heightFactor);

                // Simple N dot L lighting with 0.2 ambient floor
                Light mainLight = GetMainLight();
                float ndl = max(0, dot(normal, mainLight.direction));
                half3 lit = albedo.rgb * tint * (0.2 + ndl * 0.8);

                return half4(lit, albedo.a);
            }
            ENDHLSL
        }

        // =====================================================================
        // Shadow Caster Pass — writes depth only, no bending applied
        // (shadows stay at blade origin for perf; visually acceptable for small grass)
        // =====================================================================
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
