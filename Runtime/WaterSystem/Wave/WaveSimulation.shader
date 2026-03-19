Shader "Hidden/WaveSimulation"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Damping ("Damping", Range(0.9, 1.0)) = 0.99
        _WaveSpeed ("Wave Speed", Range(0.1, 0.5)) = 0.45
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            Texture2D _MainTex;
            SamplerState sampler_MainTex;
            float4 _MainTex_TexelSize;

            float _Damping;
            float _WaveSpeed;

            // Stamp data texture (Nx1, RGBAFloat, point-filtered).
            // Each pixel: (uv.x, uv.y, radius, strength)
            Texture2D _StampTex;
            SamplerState sampler_point_clamp_StampTex;
            float4 _StampTex_TexelSize;
            float _StampCount;

            // Procedural rain
            float _RainEnabled;
            float _RainIntensity;
            float _RainDensity;
            float _RainFrame;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 LoadStamp(int index)
            {
                float u = ((float)index + 0.5) * _StampTex_TexelSize.x;
                return _StampTex.SampleLevel(sampler_point_clamp_StampTex, float2(u, 0.5), 0);
            }

            // PCG hash — good distribution, no precision issues across GPUs.
            uint pcg_hash(uint input)
            {
                uint state = input * 747796405u + 2891336453u;
                uint word = ((state >> ((state >> 28u) + 4u)) ^ state) * 277803737u;
                return (word >> 22u) ^ word;
            }

            float hash_to_float(uint h)
            {
                // Use top 24 bits to match float mantissa precision,
                // allowing per-pixel probabilities as low as ~6e-8.
                return float(h >> 8u) * (1.0 / 16777215.0);
            }

            float4 frag(v2f i) : SV_Target
            {
                // === 1. Sample Current State ===
                float4 center = _MainTex.Sample(sampler_MainTex, i.uv);
                float h_curr = center.r;
                float h_prev = center.g;

                // === 2. Sample Neighbors ===
                float2 texel = _MainTex_TexelSize.xy;

                float h_left   = _MainTex.Sample(sampler_MainTex, i.uv + float2(-texel.x, 0.0)).r;
                float h_right  = _MainTex.Sample(sampler_MainTex, i.uv + float2( texel.x, 0.0)).r;
                float h_top    = _MainTex.Sample(sampler_MainTex, i.uv + float2(0.0,  texel.y)).r;
                float h_bottom = _MainTex.Sample(sampler_MainTex, i.uv + float2(0.0, -texel.y)).r;

                // === 3. Wave Equation ===
                float laplacian = h_left + h_right + h_top + h_bottom - 4.0 * h_curr;
                float h_new = (2.0 * h_curr - h_prev + _WaveSpeed * laplacian) * _Damping;

                // === 4. Apply Stamp Disturbances ===
                float totalDisturbance = 0.0;
                int count = (int)_StampCount;

                UNITY_LOOP
                for (int idx = 0; idx < count; idx++)
                {
                    float4 s = LoadStamp(idx);
                    float dist = distance(i.uv, s.xy);
                    float radius = s.z;
                    float strength = s.w;

                    if (dist < radius)
                    {
                        float t = 1.0 - dist / radius;
                        float influence = t * t * (3.0 - 2.0 * t); // smoothstep falloff
                        totalDisturbance += strength * influence;
                    }
                }

                // === 5. Procedural Rain ===
                // Single-texel point impulse. The wave equation naturally
                // expands it into a circular ring.
                if (_RainEnabled > 0.5)
                {
                    uint2 pixel = uint2(i.uv * _MainTex_TexelSize.zw);
                    uint frame = (uint)_RainFrame;
                    uint seed = pcg_hash(pixel.x + pcg_hash(pixel.y + pcg_hash(frame)));

                    if (hash_to_float(seed) < _RainDensity)
                    {
                        totalDisturbance += _RainIntensity;
                    }
                }

                h_new += totalDisturbance;
                h_new = clamp(h_new, -1.0, 1.0);

                // === 6. Output ===
                return float4(h_new, h_curr, 0.0, 1.0);
            }
            ENDCG
        }
    }
}
