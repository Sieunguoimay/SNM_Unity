Shader "Unlit/WaterSurface"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { 
            "Queue" = "Transparent"
            "RenderType"="Transparent" 
            "RenderPipeline"="UniversalPipeline"
        }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // make fog work
            // #pragma multi_compile_fog

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _ReflectionTex;
            float4x4 _ReflectionVP;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv: TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float2 uv: TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 clip = mul(_ReflectionVP, float4(i.worldPos, 1.0));

                // perspective divide
                float3 ndc = clip.xyz / clip.w;

                // NDC -> UV
                float2 uv = ndc.xy * 0.5 + 0.5;
                
                // uv.y = 1.0 - uv.y;
                
                // discard nếu ra ngoài
                // if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
                //     discard;

                float4 reflection = tex2D(_ReflectionTex, uv);
                
                return reflection;
            }

            ENDCG
        }
    }
}
