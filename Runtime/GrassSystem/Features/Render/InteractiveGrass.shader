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
                float4 _WindParams2;   // x = sway variation, y = amplitude variation
                half4 _ColorVariationA;
                half4 _ColorVariationB;
                float _AOStrength;
                float _AOPower;
                float _SpringFrequency;
                float _SpringDamping;
                float _SpringAmplitude;
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
                float instanceRand : TEXCOORD4; // per-instance random [0,1]
                float trampleStrength : TEXCOORD5;
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

                // --- Recovery Spring ---
                // When hold buffer (z) is depleted but strength (w) is still fading,
                // the blade is recovering — add damped sinusoidal overshoot.
                float isRecovering = step(trample.z, 0.001) * step(0.01, trample.w);
                float recoveryProgress = 1.0 - trample.w;
                float springOsc = exp(-recoveryProgress * _SpringDamping)
                                * sin(recoveryProgress * _SpringFrequency * 6.283);
                trampleStrength += springOsc * _SpringAmplitude * isRecovering;
                trampleStrength = clamp(trampleStrength, -0.1, 1.0);

                float2 trampleDir = trample.xy;

                // --- Wind ---
                float phaseOffset = frac(worldOrigin.x * 12.9898 + worldOrigin.z * 78.233);
                float2 windUV = worldUV / _WindParams.zw + _Time.y * _WindParams.y + phaseOffset * _WindParams2.x;
                float3 windDir = SAMPLE_TEXTURE2D_LOD(_WindMap, sampler_WindMap, windUV, 0).xyz * 2.0 - 1.0;
                float ampVar = lerp(1.0, 0.7 + frac(worldOrigin.x * 45.164 + worldOrigin.z * 37.912) * 0.6, _WindParams2.y);
                windDir *= ampVar;
                float windStrength = _WindParams.x;
                float3 windMaxBendDir = float3(windDir.x, 0, windDir.y); // Wind only bends in xz plane
                float3 windBendDirWS = normalize(lerp(float3(0, 1, 0), windMaxBendDir, lerp(0.0, bendFactor, windStrength)));

                // --- Combine bend directions in world space ---
                // The blade direction starts upright (0,1,0). Wind and trample push it sideways (xz).
                // Under heavy trample, y collapses toward 0 so the blade flattens to the ground.
                float3 trampleDirWS = float3(0, 1, 0);
                trampleDirWS.xz = trampleDir * trampleStrength;
                trampleDirWS.y = max(0.0, 1.0 - trampleStrength);
                float3 trampleBendDirWS = normalize(lerp(trampleDirWS, float3(0, 1, 0), lerp(bendFactor, 0.0, trampleStrength)));

                // Convert combined bend direction from world space into blade-local space
                float3x3 worldToLocal = (float3x3)transpose((float3x3)localToWorld);
                float3 trampleBendDirLS = mul(worldToLocal, trampleBendDirWS);
                float3 windBendDirLS = mul(worldToLocal, windBendDirWS);

                float3 finalBendDirLS = normalize(lerp(windBendDirLS, trampleBendDirLS, trampleStrength));

                // Blend between the combined bend and straight-up based on how high up the blade we are.
                // When trampled, the entire blade bends (bendFactor is overridden toward 0).
                float3 localPos = RotateFromTo(input.positionOS.xyz, float3(0, 1, 0), finalBendDirLS);

                float3 worldPos = mul(localToWorld, float4(localPos, 1)).xyz;

                output.positionWS = worldPos;
                output.positionCS = TransformWorldToHClip(worldPos);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.normalWS = normalize(mul((float3x3)localToWorld, input.normalOS));
                output.heightFactor = height01;
                output.trampleStrength = trampleStrength;
                uint hash = instanceID * 2654435761u;
                output.instanceRand = frac(float(hash) * (1.0 / 4294967296.0));

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

                // Per-instance color variation
                half3 variation = lerp(_ColorVariationA.rgb, _ColorVariationB.rgb, input.instanceRand);
                tint *= variation;

                half3 unlit = albedo.rgb * tint ;

                // Ambient occlusion — darker near root, intensified when trampled
                float aoExtra = _AOStrength * input.trampleStrength * 0.5;
                float ao = lerp(1.0 - _AOStrength - aoExtra, 1.0, pow(input.heightFactor, _AOPower));
                unlit *= ao;

                // ----------------------
                // Shadows
                // ----------------------
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                unlit *= clamp(mainLight.shadowAttenuation, 0.5, 1.0);
                #endif

                return half4(unlit, albedo.a);
            }
            ENDHLSL
        }

    }
}
