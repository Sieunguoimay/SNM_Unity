// Debug preview of the wave simulation texture (height or normal view).
// Only blitted on demand by WaveSimulation.RenderDebugPreview — never part
// of the per-frame game loop.
Shader "Hidden/Snm/WaterSystemV2/WaveDisplay"
{
    Properties
    {
        _MainTex ("Wave Texture", 2D) = "black" {}
        _DisplayMode ("Display Mode", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always

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

            #define DISPLAY_HEIGHT 0
            #define DISPLAY_NORMAL 1

            Texture2D _MainTex;
            SamplerState sampler_MainTex;
            float4 _MainTex_TexelSize;
            float _DisplayMode;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float SampleHeight(float2 uv)
            {
                return _MainTex.SampleLevel(sampler_MainTex, uv, 0).r;
            }

            float4 frag(v2f i) : SV_Target
            {
                if (_DisplayMode == DISPLAY_NORMAL)
                {
                    float2 texel = _MainTex_TexelSize.xy;

                    float hL = SampleHeight(i.uv + float2(-texel.x, 0));
                    float hR = SampleHeight(i.uv + float2( texel.x, 0));
                    float hD = SampleHeight(i.uv + float2(0, -texel.y));
                    float hU = SampleHeight(i.uv + float2(0,  texel.y));

                    float3 normal = normalize(float3(hL - hR, hD - hU, 2.0 * texel.x));
                    return float4(normal * 0.5 + 0.5, 1);
                }

                float h = saturate(SampleHeight(i.uv) * 0.5 + 0.5);
                return float4(h, h, h, 1);
            }
            ENDCG
        }
    }
}
