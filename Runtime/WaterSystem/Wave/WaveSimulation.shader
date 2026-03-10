Shader "Hidden/WaveSimulation"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Damping ("Damping", Range(0.9, 1.0)) = 0.99
        _WaveSpeed ("Wave Speed", Range(0.1, 0.5)) = 0.5
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
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            #define MAX_DISTURBANCES 32

            Texture2D _MainTex;
            SamplerState sampler_MainTex;
            float4 _MainTex_TexelSize;

            float _Damping;
            float _WaveSpeed;

            // Disturbances: xy = UV position, z = radius, w = strength
            float4 _Disturbances[MAX_DISTURBANCES];
            
            // Changed to float for reliability with SetFloat
            float _DisturbanceCount;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
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

                // === 4. Apply Disturbances ===
                float totalDisturbance = 0.0;
                int count = (int)min(_DisturbanceCount, (float)MAX_DISTURBANCES);

                UNITY_LOOP
                for (int idx = 0; idx < count; idx++)
                {
                    float4 d = _Disturbances[idx];
                    float dist = distance(i.uv, d.xy);

                    if (dist < d.z)
                    {
                        float influence = 1.0 - (dist / d.z);
                        influence = influence * influence * (3.0 - 2.0 * influence);
                        totalDisturbance += d.w * influence;
                    }
                }

                h_new += totalDisturbance;

                // === 5. Output ===
                return float4(h_new, h_curr, 0.0, 1.0);
            }
            ENDCG
        }
    }
}