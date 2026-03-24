Shader "Hidden/GrassTrample"
{
    SubShader
    {
        Tags { "Queue" = "Overlay" }
        ZWrite Off
        ZTest Always
        Blend Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #define MAX_BRUSHES 64
            #define EPSILON 1e-5

            sampler2D _MainTex;

            float4 _Brushes[MAX_BRUSHES]; // xy = world pos, z = direction angle, w = radius
            float _BrushCount;
            float _FadeAmount;
            float _HoldBuffer;
            float4 _WorldCanvas; // xy = origin, zw = size

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // Blit quad
            Varyings vert(appdata v)
            {
                Varyings o;
                o.positionCS = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float2 UVToWorld(float2 uv)
            {
                return uv * _WorldCanvas.zw + _WorldCanvas.xy;
            }

            float2 CombineVector(float2 a, float2 b)
            {
                float alignment = dot(a, b);
                float weight = clamp(0, 1, alignment);
                return a + b * weight;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float2 worldPos = UVToWorld(i.uv);

                // Previous frame
                float4 prev = tex2D(_MainTex, i.uv);

                // Accumulate brush influence
                int count = (int)min(_BrushCount, (float)MAX_BRUSHES);
                float2 inputDir = prev.xy;
                float maxTrample = 0;

                [loop]
                for (int b = 0; b < count; b++)
                {
                    float4 brush = _Brushes[b];
                    float2 diff = worldPos - brush.xy;
                    float distSqr = dot(diff, diff);

                    if (distSqr > brush.w * brush.w) continue;

                    float dist = sqrt(distSqr);
                    float trample = saturate(1.0 - dist / brush.w);
                    float2 dir = float2(cos(brush.z), sin(brush.z));

                    if(trample > maxTrample)
                    {
                        maxTrample = trample;
                        inputDir = dir;//diff / dist;//CombineVector(diff / dist, dir * 2.0);
                    }
                }

                // No brushes touching this pixel — fade toward zero
                if (maxTrample <= EPSILON)
                {
                    // Drain hold buffer first, then fade trample
                    float hold = max(0, prev.z - _FadeAmount);
                    float trample = hold > EPSILON ? prev.w : max(0, prev.w - _FadeAmount);
                    float visible = saturate(trample);
                    
                    // Channel layout: xy = push direction, z = hold buffer, w = trample value
                    return float4(prev.xy, hold, trample);
                }

                // Blend: keep previous if it's significantly stronger
                float2 outDir = lerp(inputDir, prev.xy, step(maxTrample, prev.w * 1.25));
                float outHold = max(prev.z, _HoldBuffer);
                float outTrample = max(prev.w, maxTrample);

                return float4(outDir, outHold, outTrample);
            }
            ENDHLSL
        }
    }
}
