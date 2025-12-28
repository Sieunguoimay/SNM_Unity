Shader "Hidden/WorldTraceBrush"
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

            float4 _BrushParams; // x,y = UV center, z = radius, w = strength
            float4 _WorldCanvas;
            float4 _BrushDir;

            sampler2D _MainTex;
  
            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            float ease_InCircle(float x) { return 1.0 - sqrt(1.0 - pow(x, 2.0)); }
            float ease_InSine(float x) { return 1.0 - cos(x * 3.1415 * .5); }

            float2 WorldToUV(float2 worldPos)
            {
                float2 uv = (worldPos.xy - _WorldCanvas.xy) / _WorldCanvas.zw;
                return float2(uv.x, 1.0 - uv.y);
            }

            float2 UVToWorld(float2 uv){
                uv.y = 1.0 - uv.y;

                float2 worldPos = uv * _WorldCanvas.zw + _WorldCanvas.xy;
                return worldPos;
            }

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
                float brushRadius = _BrushParams.w;
                float2 worldBrushPos = _BrushParams.xz;
                float2 uv = i.uv;
                float2 worldFrag = UVToWorld(uv);

                float d = distance(worldFrag, worldBrushPos);
                float4 dst = tex2D(_MainTex, float2(uv.x, 1.0 - uv.y));

                if(d > brushRadius)
                {
                    // discard;
                    float deltaTime = _BrushDir.w;
                    
                    dst.w = max(0, dst.w - deltaTime * .1);

                    return float4(dst.xyz, dst.w);
                }
                else
                {
                    float2 brushDir = normalize(_BrushDir.xz);
                    float2 fragDir = normalize(worldFrag - worldBrushPos);
                    
                    float mask = saturate(1 - d / brushRadius);
                    float2 pushDir = normalize(fragDir + brushDir  * 10.0);
                    float4 src = float4(pushDir.x, pushDir.y, 0, mask);

                    float useDst = step(src.a, dst.a * 1.25);
                    float3 outDir = lerp(src.xyz, dst.xyz, useDst); 

                    float outFactor = max(dst.a, src.a);

                    float4 outColor = float4(outDir, outFactor);

                    return outColor;
                }
            }
            ENDHLSL
        }
    }
}
