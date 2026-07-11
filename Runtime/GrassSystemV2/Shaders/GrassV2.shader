// GrassSystemV2 blade shader. One shader serves both render tiers:
// the GPU-driven tier binds a compacted visible buffer (_GrassBaseIndex = 0),
// the Simple tier binds a chunk buffer with a range offset.
//
// Fully self-contained: procedural wind (no wind texture), bend + effects
// sampled from the global interaction canvas RTs set by GrassWorld.
// No ShadowCaster pass by design — shadow maps are too coarse for blades
// (blocky flicker); root AO substitutes for grounding.
Shader "Snm/GrassV2"
{
    Properties
    {
        _MainTex ("Albedo (optional)", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _TopColor ("Top Tint", Color) = (0.55, 0.85, 0.35, 1)
        _BottomColor ("Bottom Tint", Color) = (0.15, 0.35, 0.1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off // blades visible from both sides
        ZWrite On

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "GrassV2Common.hlsl"

            TEXTURE2D(_MainTex);         SAMPLER(sampler_MainTex);
            TEXTURE2D(_GrassBendMap);    SAMPLER(sampler_GrassBendMap);   // global, set by GrassWorld
            TEXTURE2D(_GrassEffectMap);  SAMPLER(sampler_GrassEffectMap); // global, set by GrassWorld

            // --- Globals (world-level, set once per frame by GrassWorld) ---
            float4 _GrassCanvasRect;    // xy = world min, zw = world size of interaction canvas
            float4 _GrassWindGlobal;    // xy = direction, z = speed, w = noise scale
            float4 _GrassWindGlobal2;   // x = lean, y = coherence
            half4 _GrassTintColor;

            // --- Per-material (type-level, SRP-batcher friendly) ---
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _TopColor;
                half4 _BottomColor;
                half4 _GrassColorA;
                half4 _GrassColorB;
                float _GrassAoStrength;
                float _GrassAoPower;
                float _GrassSwayAmount;
                float _GrassSwayFrequency;
                float _GrassWindStiffness;
                float _GrassBladeHeight;
                float4 _GrassSpringParams; // x = frequency, y = damping, z = amplitude
                float _Cutoff;
            CBUFFER_END

            StructuredBuffer<GrassInstanceData> _GrassInstances;
            int _GrassBaseIndex;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float heightFactor : TEXCOORD2;   // 0 root, 1 tip
                float seed01 : TEXCOORD3;
                float trampleStrength : TEXCOORD4;
                float3 effects : TEXCOORD5;       // x = burn, y = freeze, z = tint
            };

            Varyings vert(Attributes input, uint instanceID : SV_InstanceID)
            {
                GrassInstanceData instance = _GrassInstances[_GrassBaseIndex + (int)instanceID];

                float yaw = GrassUnpackYaw(instance);
                float scale = GrassUnpackScale(instance);
                float seed01 = GrassUnpackSeed01(instance);

                // Cut blades collapse to a point — zero-area triangles cost nothing.
                // The GPU tier already filters them in compute; this covers the Simple tier.
                if (GrassUnpackFlags(instance) & GRASS_FLAG_CUT) scale = 0.0;

                float3 root = instance.position;
                float height01 = saturate(input.positionOS.y / max(_GrassBladeHeight, 0.0001));
                float bendFactor = GrassEaseInCubic(height01);              // trample bend distribution (unchanged)
                float windBendFactor = pow(height01, _GrassWindStiffness);  // wind bend: stiffer = bend concentrated near the tip

                // Yaw + uniform scale, applied by hand (no matrix needed).
                float sinYaw, cosYaw;
                sincos(yaw, sinYaw, cosYaw);
                float3 local = input.positionOS.xyz * scale;
                float3 offsetWS = float3(
                    local.x * cosYaw - local.z * sinYaw,
                    local.y,
                    local.x * sinYaw + local.z * cosYaw);

                // --- Interaction canvas sample (bend + effects) ---
                float2 canvasUV = (root.xz - _GrassCanvasRect.xy) / max(_GrassCanvasRect.zw, 0.0001);
                float inCanvas = all(canvasUV == saturate(canvasUV)) ? 1.0 : 0.0;

                // Bend map: xy = push direction, z = hold, w = fading energy.
                // Used linearly so the canvas-side soft falloff (_StampSoftness)
                // survives — an ease-out here would re-inflate the soft rim.
                float4 bend = SAMPLE_TEXTURE2D_LOD(_GrassBendMap, sampler_GrassBendMap, canvasUV, 0) * inCanvas;
                float trampleStrength = saturate(bend.w);

                // Recovery spring: hold gone but energy still fading -> damped overshoot.
                float isRecovering = step(bend.z, 0.001) * step(0.01, bend.w);
                float recoveryProgress = 1.0 - bend.w;
                float springOsc = exp(-recoveryProgress * _GrassSpringParams.y)
                                * sin(recoveryProgress * _GrassSpringParams.x * 6.2831853);
                trampleStrength += springOsc * _GrassSpringParams.z * isRecovering;
                trampleStrength = clamp(trampleStrength, -0.1, 1.0);

                // Effects map: r = burn, g = freeze, b = tint amount.
                float4 effects = SAMPLE_TEXTURE2D_LOD(_GrassEffectMap, sampler_GrassEffectMap, canvasUV, 0) * inCanvas;

                // --- Procedural wind ---
                float2 windDirection = normalize(_GrassWindGlobal.xy + 1e-5);
                // Coherence fades out the per-blade phase offset: 1 = blades sway
                // in phase (gusts read as travelling waves), 0 = each blade has its
                // own random phase (busier, no clear wave).
                float phaseOffset = seed01 * 6.2831853 * (1.0 - _GrassWindGlobal2.y);
                float windTime = _Time.y * _GrassWindGlobal.z * _GrassSwayFrequency + phaseOffset;
                float2 windVec = GrassWindVector(root.xz, windTime, windDirection, _GrassWindGlobal.w);
                // Lean: a steady push along the wind so blades lean over and sway
                // around that pose instead of just rocking about upright.
                windVec += windDirection * _GrassWindGlobal2.x;
                float amplitude = lerp(0.8, 1.2, GrassHash21(root.xz * 7.31)); // per-blade amplitude variation
                windVec *= _GrassSwayAmount * amplitude * (1.0 - effects.g); // frozen grass stops swaying

                float3 windMaxBend = float3(windVec.x, 0, windVec.y);
                float3 up = float3(0, 1, 0);
                float3 windBendDir = normalize(lerp(up, windMaxBend, saturate(windBendFactor * length(windVec))));

                // --- Trample bend: heavy trample flattens the whole blade ---
                float3 trampleDir = up;
                trampleDir.xz = bend.xy * trampleStrength;
                trampleDir.y = max(0.0, 1.0 - abs(trampleStrength));
                float3 trampleBendDir = normalize(lerp(trampleDir, up, lerp(bendFactor, 0.0, saturate(trampleStrength))));

                // Trample overrides wind proportionally — a pinned blade cannot sway.
                float3 finalBendDir = normalize(lerp(windBendDir, trampleBendDir, saturate(abs(trampleStrength))));

                offsetWS = GrassRotateFromTo(offsetWS, up, finalBendDir);

                // Burnt grass shrivels: tip sinks toward the ground.
                offsetWS.y *= 1.0 - effects.r * 0.5;

                float3 positionWS = root + offsetWS;

                Varyings output;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.heightFactor = height01;
                output.seed01 = seed01;
                output.trampleStrength = saturate(trampleStrength);
                output.effects = effects.rgb;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                clip(albedo.a - _Cutoff);

                // Root-to-tip gradient x per-blade variation (stable via seed).
                half3 tint = lerp(_BottomColor.rgb, _TopColor.rgb, input.heightFactor);
                tint *= lerp(_GrassColorA.rgb, _GrassColorB.rgb, input.seed01);

                half3 color = albedo.rgb * tint;

                // Root AO, deepened while trampled (a flat blade hugs the ground).
                float aoExtra = _GrassAoStrength * input.trampleStrength * 0.5;
                float ao = lerp(1.0 - _GrassAoStrength - aoExtra, 1.0, pow(input.heightFactor, _GrassAoPower));
                color *= ao;

                // Effects: burn chars, freeze frosts, tint dyes.
                color = lerp(color, half3(0.08, 0.06, 0.04), input.effects.x);
                color = lerp(color, color * half3(0.7, 0.85, 1.1) + half3(0.05, 0.1, 0.2), input.effects.y * 0.8);
                color = lerp(color, _GrassTintColor.rgb, input.effects.z);

                // Unlit base + main light color, shadow clamped so grass never goes black.
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                #else
                Light mainLight = GetMainLight();
                #endif
                half shadow = lerp(0.5, 1.0, saturate(mainLight.distanceAttenuation * mainLight.shadowAttenuation));
                color *= mainLight.color * shadow;

                return half4(color, albedo.a);
            }
            ENDHLSL
        }
    }
}
