Shader "Hidden/WorldTraceBrush"
{
    SubShader
    {
        Tags { "Queue"="Overlay" }
        ZWrite Off
        ZTest Always
        Blend One One   // Additive

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 _BrushParams; // x,y = UV center, z = radius, w = strength

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(uint id : SV_VertexID)
            {
                float2 quad[6] = {
                    float2(-1,-1), float2(1,-1), float2(-1, 1), 
                    float2(-1, 1), float2(1,-1), float2(1, 1)
                };

                v2f o;
                o.pos = float4(quad[id], 0, 1);
                o.uv = quad[id] * 0.5 + 0.5;
                return o;
            }

            float frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float d = distance(uv, _BrushParams.xy);
                float mask = saturate(1 - d / _BrushParams.z);
                return mask * _BrushParams.w;
            }
            ENDHLSL
        }
    }
}
