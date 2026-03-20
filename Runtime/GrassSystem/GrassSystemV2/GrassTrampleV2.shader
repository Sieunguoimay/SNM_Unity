Shader "Hidden/GrassTrampleV2"
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
            #define PRESENCE_SENTINEL 500.0

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

            float4 frag(Varyings i) : SV_Target
            {
                float2 worldPos = UVToWorld(i.uv);

                // Previous frame
                float4 prev = tex2D(_MainTex, i.uv);

                // Accumulate brush influence
                int count = (int)min(_BrushCount, (float)MAX_BRUSHES);
                float2 accumDir = 0;
                float totalWeight = 0;
                float maxMask = 0;

                [loop]
                for (int b = 0; b < count; b++)
                {
                    float4 brush = _Brushes[b];
                    float dist = distance(worldPos, brush.xy);
                    if (dist > brush.w) continue;

                    float mask = saturate(1.0 - dist / brush.w);

                    // Presence stamp: push radially away from brush center
                    // Movement stamp: use the encoded direction angle
                    float2 dir;
                    if (brush.z > PRESENCE_SENTINEL)
                    {
                        float2 offset = worldPos - brush.xy;
                        float  offLen = length(offset);
                        dir = offLen > EPSILON ? offset / offLen : float2(0, 0);
                    }
                    else
                    {
                        dir = float2(cos(brush.z), sin(brush.z));
                    }

                    accumDir += dir * mask;
                    totalWeight += mask;
                    maxMask = max(maxMask, mask);
                }

                // No brushes touching this pixel — fade toward zero
                // Channel layout: xy = push direction, z = hold buffer, w = trample value
                if (totalWeight <= EPSILON)
                {
                    // Drain hold buffer first, then fade trample
                    float hold = max(0, prev.z - _FadeAmount);
                    float trample = hold > EPSILON ? prev.w : max(0, prev.w - _FadeAmount);
                    float visible = saturate(trample);

                    return float4(prev.xy * visible, hold, trample);
                }

                // Compute push direction directly from brush movement
                float2 avgDir = accumDir / totalWeight;
                float  avgLen = length(avgDir);
                float2 pushDir = avgLen > EPSILON ? avgDir / avgLen : prev.xy;

                // Blend: keep previous if it's significantly stronger
                float2 outDir = lerp(pushDir, prev.xy, step(maxMask, prev.w * 1.25));
                float outTrample = max(prev.w, maxMask);
                float outHold = max(prev.z, _HoldBuffer * maxMask);

                return float4(outDir, outHold, outTrample);
            }
            ENDHLSL
        }
    }
}
