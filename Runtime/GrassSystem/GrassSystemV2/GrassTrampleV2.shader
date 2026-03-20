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
                if (totalWeight <= EPSILON)
                {
                    float t = 1.0 - prev.a;
                    float fade = lerp(0.001, _FadeAmount, t * t * t * t * t); // ease_InQuint
                    prev.a = max(0.0, prev.a - fade);
                    return prev;
                }

                // Compute push direction directly from brush movement
                float2 avgDir = accumDir / totalWeight;
                float  avgLen = length(avgDir);
                float2 pushDir = avgLen > EPSILON ? avgDir / avgLen : prev.xy;

                float4 stamp = float4(pushDir, 0, maxMask);

                // Blend: keep previous if it's significantly stronger
                float usePrev = step(stamp.a, prev.a * 1.25);
                float3 outDir = lerp(stamp.xyz, prev.xyz, usePrev);
                float outA = max(prev.a, stamp.a);

                // Direction blend: when grass is already trampled, rotate gradually
                // instead of snapping to the new direction
                // float prevStrength = prev.a;
                // float dirBlend = maxMask * saturate(1.0 - prevStrength * 0.8);
                // float2 blendedDir2D = lerp(prev.xy, pushDir, dirBlend);
                // float  blendedLen   = length(blendedDir2D);
                // float3 outDir = blendedLen > EPSILON
                //     ? float3(blendedDir2D / blendedLen, 0)
                //     : stamp.xyz;
                // float  outA   = max(prev.a, stamp.a);

                return float4(outDir, outA);
            }
            ENDHLSL
        }
    }
}
