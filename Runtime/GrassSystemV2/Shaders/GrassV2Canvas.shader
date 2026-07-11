// Interaction canvas update for GrassSystemV2. Two fullscreen passes over
// small sliding RTs (default 512x512), ping-ponged each frame:
//
//   Pass 0 "Bend":    RG = push direction, B = hold, A = fading energy.
//                     Stamps refresh hold+energy; energy only fades once hold
//                     is gone (drives the recovery spring in GrassV2.shader).
//   Pass 1 "Effects": R = burn (permanent), G = freeze (thaws over time),
//                     B = tint (fades slowly).
//
// Both passes re-sample the previous frame with a UV shift so the canvas can
// slide with the camera without losing content.
Shader "Hidden/Snm/GrassV2Canvas"
{
    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        #define MAX_STAMPS 64

        TEXTURE2D(_PrevTex); SAMPLER(sampler_PrevTex);

        float2 _ShiftUV;        // UV offset applied when the canvas re-centers
        float4 _CanvasRect;     // xy = world min, zw = world size
        float _DeltaTime;
        float _FadeSpeed;       // bend energy fade (1/s)
        float _HoldDecay;       // bend hold decay (1/s)
        float4 _FreezeTintDecay; // x = freeze decay (1/s), y = tint decay (1/s)

        int _StampCount;
        float4 _Stamps[MAX_STAMPS];      // xy = world pos, z = radius, w = strength
        float4 _StampParams[MAX_STAMPS]; // bend: xy = direction | effects: x = channel (0/1/2)

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        Varyings vert(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            output.uv = input.uv;
            return output;
        }

        // Previous frame value at this texel, accounting for canvas sliding.
        // Texels that slid in from outside the old canvas start at zero.
        float4 SamplePrev(float2 uv)
        {
            float2 prevUV = uv + _ShiftUV;
            float inside = all(prevUV == saturate(prevUV)) ? 1.0 : 0.0;
            return SAMPLE_TEXTURE2D(_PrevTex, sampler_PrevTex, prevUV) * inside;
        }

        float StampInfluence(float2 worldXZ, float4 stamp)
        {
            float dist = distance(worldXZ, stamp.xy);
            float falloff = 1.0 - saturate(dist / max(stamp.z, 0.0001));
            return falloff * falloff * stamp.w; // smooth quadratic edge
        }
        ENDHLSL

        Pass
        {
            Name "Bend"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragBend

            float4 fragBend(Varyings input) : SV_Target
            {
                float2 worldXZ = _CanvasRect.xy + input.uv * _CanvasRect.zw;
                float4 prev = SamplePrev(input.uv);

                // Accumulate this frame's stamps.
                float stampStrength = 0.0;
                float2 stampDir = 0.0;
                for (int i = 0; i < _StampCount; i++)
                {
                    float influence = StampInfluence(worldXZ, _Stamps[i]);
                    stampStrength = max(stampStrength, influence);
                    stampDir += _StampParams[i].xy * influence;
                }
                if (stampStrength > 0.001) stampDir = normalize(stampDir + 1e-5);

                // Hold: refreshed by stamps, decays quickly when they stop.
                float hold = max(prev.b - _DeltaTime * _HoldDecay, stampStrength);

                // Energy: pinned up while hold is active, fades once it is gone.
                float fading = step(hold, 0.001);
                float energy = max(prev.a - _DeltaTime * _FadeSpeed * fading, stampStrength);

                // Direction: latch toward the newest push, keep the old one while recovering.
                float2 direction = lerp(prev.xy, stampDir, saturate(stampStrength * 2.0));

                return float4(direction, hold, energy);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Effects"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragEffects

            float4 fragEffects(Varyings input) : SV_Target
            {
                float2 worldXZ = _CanvasRect.xy + input.uv * _CanvasRect.zw;
                float4 prev = SamplePrev(input.uv);

                float3 value = prev.rgb;
                value.g = max(value.g - _DeltaTime * _FreezeTintDecay.x, 0.0); // thaw
                value.b = max(value.b - _DeltaTime * _FreezeTintDecay.y, 0.0); // tint fade
                // value.r (burn) never decays — scorched earth stays scorched.

                for (int i = 0; i < _StampCount; i++)
                {
                    float influence = StampInfluence(worldXZ, _Stamps[i]);
                    int channel = (int)_StampParams[i].x;
                    if (channel == 0) value.r = max(value.r, influence);
                    else if (channel == 1) value.g = max(value.g, influence);
                    else value.b = max(value.b, influence);
                }

                return float4(value, 0.0);
            }
            ENDHLSL
        }
    }
}
