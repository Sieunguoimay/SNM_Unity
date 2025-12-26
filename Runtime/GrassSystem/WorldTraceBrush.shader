Shader "Hidden/WorldTraceBrush"
{
    SubShader
    {
        Tags { "Queue"="Overlay" }
        ZWrite Off
        ZTest Always
        Blend Off
        // Blend One One   // Additive
        // BlendOp Add, Max
        // Blend OneMinusDstAlpha One, SrcAlpha DstAlpha
        // Blend SrcAlpha OneMinusSrcAlpha
        // Blend OneMinusDstAlpha One
        // Blend SrcAlpha DstAlpha
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float4 _BrushParams; // x,y = UV center, z = radius, w = strength
            float4 _BrushColor;

            sampler2D _MainTex;
            // float4 _MainTex_ST;
            
            // struct appdata
            // {
            //     uint vertexID : SV_VertexID;
            //     float2 uv : TEXCOORD0;
            // };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                // float2 uv2  : TEXCOORD1;
            };

            // v2f vert(appdata v)
            // {
            //     float2 quad[6] = {
            //         float2(-1,-1), float2(1,-1), float2(-1, 1), 
            //         float2(-1, 1), float2(1,-1), float2(1, 1)
            //     };

            //     uint id = v.vertexID;
                
            //     v2f o;
            //     o.pos = float4(quad[id], 0, 1);
            //     o.uv = quad[id] * 0.5 + 0.5;
            //     o.uv2 = TRANSFORM_TEX(v.uv, _MainTex);
            //     return o;
            // }
            float ease_InCircle(float x) { return 1.0 - sqrt(1.0 - pow(x, 2.0)); }
            float ease_InSine(float x) { return 1.0 - cos(x * 3.1415 * .5); }

            v2f vert(uint id : SV_VertexID)
            {
                v2f o;

                float2 uv = float2(
                    (id << 1) & 2,
                    id & 2
                );

                o.uv  = uv;
                o.pos = float4(uv * 2 - 1, 0, 1);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float d = distance(uv, _BrushParams.xy);

                // clip(_BrushParams.z - d);
                float brushRadius = _BrushParams.z;
                float mask = saturate(1 - d / brushRadius);
                // return mask * _BrushParams.w;
                // return float4(_BrushColor.xyz, mask);

                float4 dst = tex2D(_MainTex, float2(uv.x, 1.0 - uv.y));
                float4 src = float4(_BrushColor.xyz, mask);
                float useSrc = step(dst.a, src.a); 
                // float w = saturate(src.a - dst.a);

                float4 outColor = lerp(dst, src, useSrc);

                if(brushRadius < d)
                {
                    // discard;
                    return float4(dst.xyz, dst.w * .995);
                }

                return outColor;
            }
            ENDHLSL
        }
    }
}
