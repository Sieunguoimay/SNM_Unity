Shader "Hidden/WaveDisplay"
{
    Properties
    {
        _MainTex ("Wave Texture", 2D) = "white" {}
        _HeightfieldTex ("Heightfield", 2D) = "black" {}
        _HeightfieldStrength ("Heightfield Strength", Range(0, 2)) = 1.0
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
            #define DISPLAY_HEIGHTFIELD 2

            Texture2D _MainTex;
            SamplerState sampler_MainTex;
            float4 _MainTex_TexelSize;
            Texture2D _HeightfieldTex;
            SamplerState sampler_HeightfieldTex;
            float _HeightfieldStrength;
            float _DisplayMode;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float SampleCombinedHeight(float2 uv)
            {
                float waveHeight = _MainTex.SampleLevel(sampler_MainTex, uv, 0).r;
                float heightfield = _HeightfieldTex.SampleLevel(sampler_HeightfieldTex, uv, 0).r * _HeightfieldStrength;
                return waveHeight + heightfield;
            }

            float4 frag(v2f i) : SV_Target
            {
                // Heightfield only
                if (_DisplayMode == DISPLAY_HEIGHTFIELD)
                {
                    return _HeightfieldTex.SampleLevel(sampler_HeightfieldTex, i.uv, 0);
                }

                // Normal map
                if (_DisplayMode == DISPLAY_NORMAL)
                {
                    float2 texel = _MainTex_TexelSize.xy;

                    float hL = SampleCombinedHeight(i.uv + float2(-texel.x, 0));
                    float hR = SampleCombinedHeight(i.uv + float2( texel.x, 0));
                    float hD = SampleCombinedHeight(i.uv + float2(0, -texel.y));
                    float hU = SampleCombinedHeight(i.uv + float2(0,  texel.y));

                    float3 normal = normalize(float3(hL - hR, hD - hU, 2.0 * texel.x));
                    float3 color = normal * 0.5 + 0.5;
                    return float4(color, 1);
                }

                // Height
                float combinedHeight = SampleCombinedHeight(i.uv);
                float normalizedHeight = saturate(combinedHeight * 0.5 + 0.5);
                return float4(normalizedHeight, normalizedHeight, normalizedHeight, 1);
            }
            ENDCG
        }
    }
}
