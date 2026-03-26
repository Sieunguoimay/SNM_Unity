Shader "Hidden/Snm/GrassMapOverlay"
{
    Properties
    {
        _MainTex ("Map", 2D) = "black" {}
        _Opacity ("Opacity", Float) = 0.5
        _ChannelMask ("Channel Mask", Vector) = (1,1,1,0)
        _ScrollOffset ("Scroll Offset", Vector) = (0,0,0,0)
        _MapScale ("Map Scale", Vector) = (1,1,0,0)
        _UseScroll ("Use Scroll", Float) = 0
        _ShowAllChannels ("Show All Channels", Float) = 1
    }

    SubShader
    {
        Tags { "Queue" = "Overlay" "RenderType" = "Transparent" }
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Opacity;
            float4 _ChannelMask;
            float4 _ScrollOffset;
            float4 _MapScale;
            float _UseScroll;
            float _ShowAllChannels;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                if (_UseScroll > 0.5)
                    uv = uv / _MapScale.xy + _ScrollOffset.xy;

                float4 raw = tex2D(_MainTex, uv);

                if (_ShowAllChannels > 0.5)
                    return float4(raw.rgb, _Opacity);

                float val = dot(raw, _ChannelMask);
                return float4(val, val, val, _Opacity);
            }
            ENDCG
        }
    }
}
