Shader "Hidden/GrassTrample"
{
    SubShader
    {
        Tags { "Queue"="Overlay" }
        ZWrite Off
        ZTest Always
        Blend Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #define BRUSH_MAX_COUNT 64
            #define EPSILON 1e-5

            sampler2D _MainTex;

            float4 _Brush_PosDir[BRUSH_MAX_COUNT]; // xy = world pos, zw = direction
            float  _Brush_Radius[BRUSH_MAX_COUNT];

            int   _BrushCount;
            float _FadeAmount;
            float4 _WorldCanvas; // xy = origin, zw = size

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            // --------------------
            // Easing
            // --------------------
            float ease_InQuint(float x)
            {
                return x * x * x * x * x;
            }

            // --------------------
            // Space conversion
            // --------------------
            float2 UVToWorld(float2 uv)
            {
                uv.y = 1.0 - uv.y;
                return uv * _WorldCanvas.zw + _WorldCanvas.xy;
            }

            // --------------------
            // Fullscreen triangle
            // --------------------
            v2f vert(uint id : SV_VertexID)
            {
                v2f o;
                float2 uv = float2((id << 1) & 2, id & 2);
                o.uv = uv;
                o.pos = float4(uv * 2.0 - 1.0, 0, 1);
                return o;
            }

            // --------------------
            // Fragment
            // --------------------
            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 worldPos = UVToWorld(uv);

                float4 dst = tex2D(_MainTex, float2(uv.x, 1.0 - uv.y));

                float totalWeight = 0.0;
                float2 accumDir   = 0.0;
                float maxMask     = 0.0;

                // --------------------
                // Brush accumulation
                // --------------------
                [loop]
                for (int b = 0; b < _BrushCount; b++)
                {
                    float2 brushPos = _Brush_PosDir[b].xy;
                    float2 brushDir = normalize(_Brush_PosDir[b].zw + EPSILON);
                    float  radius   = _Brush_Radius[b];

                    float d = distance(worldPos, brushPos);
                    if (d > radius) continue;

                    float mask = saturate(1.0 - d / radius);

                    accumDir   += brushDir * mask;
                    totalWeight += mask;
                    maxMask     = max(maxMask, mask);
                }

                // --------------------
                // No brush → fade out
                // --------------------
                if (totalWeight <= EPSILON)
                {
                    float t = 1.0 - dst.a;
                    float fade = lerp(0.001, _FadeAmount, ease_InQuint(t));
                    dst.a = max(0.0, dst.a - fade);
                    return dst;
                }

                // --------------------
                // Brush push
                // --------------------
                float2 brushDir = normalize(accumDir / totalWeight);
                float2 fragDir  = normalize(worldPos - (_WorldCanvas.xy + _WorldCanvas.zw * 0.5));

                float2 pushDir = normalize(fragDir + brushDir * 10.0);

                float4 src;
                src.xyz = float3(pushDir, 0);
                src.a   = maxMask;

                float useDst = step(src.a, dst.a * 1.25);
                float3 outDir = lerp(src.xyz, dst.xyz, useDst);
                float outA = max(dst.a, src.a);

                return float4(outDir, outA);
            }
            ENDHLSL
        }
    }
}
