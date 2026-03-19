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

            sampler2D _MainTex;

            float4 _Brushes[MAX_BRUSHES]; // xy = world pos, z = direction angle, w = radius
            float _BrushCount;
            float _FadeAmount;
            float4 _WorldCanvas; // xy = origin, zw = size

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // Fullscreen triangle
            Varyings vert(uint id : SV_VertexID)
            {
                Varyings o;
                o.uv = float2((id << 1) & 2, id & 2);
                o.positionCS = float4(o.uv * 2.0 - 1.0, 0, 1);
                return o;
            }

            float2 UVToWorld(float2 uv)
            {
                uv.y = 1.0 - uv.y;
                return uv * _WorldCanvas.zw + _WorldCanvas.xy;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float2 worldPos = UVToWorld(i.uv);

                // Previous frame
                float4 prev = tex2D(_MainTex, float2(i.uv.x, 1.0 - i.uv.y));

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
                    float2 dir = float2(cos(brush.z), sin(brush.z));

                    accumDir += dir * mask;
                    totalWeight += mask;
                    maxMask = max(maxMask, mask);
                }

                // No brushes touching this pixel — fade toward zero
                if (totalWeight <= EPSILON)
                {
                    float t = 1.0 - prev.a;
                    float fade = lerp(0.001, _FadeAmount, t * t * t * t * t); // ease_InQuint
                    prev.a = max(0.0, prev.a - fade);
                    return prev;
                }

                // Compute push direction: outward from canvas center + brush direction
                float2 brushDir = normalize(accumDir / totalWeight);
                float2 canvasCenter = _WorldCanvas.xy + _WorldCanvas.zw * 0.5;
                float2 fragDir = normalize(worldPos - canvasCenter);
                float2 pushDir = normalize(fragDir + brushDir * 10.0);

                float4 stamp = float4(pushDir, 0, maxMask);

                // Blend: keep previous if it's significantly stronger
                float usePrev = step(stamp.a, prev.a * 1.25);
                float3 outDir = lerp(stamp.xyz, prev.xyz, usePrev);
                float outA = max(prev.a, stamp.a);

                return float4(outDir, outA);
            }
            ENDHLSL
        }
    }
}
