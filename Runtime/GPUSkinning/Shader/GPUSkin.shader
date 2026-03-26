Shader "Custom/GpuSkin"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_instancing
            #pragma multi_compile _ GPU_SKINNING_ON BAKED_SKINNING_ON

            #include "UnityCG.cginc"
            #include "UnifiedSkinning.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float3 tangent : TANGENT;
                float2 uv : TEXCOORD0;

                SKINNING_VERTEX_INPUT
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float4 worldPos;
                float3 worldNormal;

                #if defined(GPU_SKINNING_ON)
                if (!SKIN(v, worldPos, worldNormal))
                {
                    worldPos = mul(unity_ObjectToWorld, v.vertex);
                    worldNormal = UnityObjectToWorldNormal(v.normal);
                }
                #elif defined(BAKED_SKINNING_ON)
                v.vertex = SKIN_BAKED(v);
                worldPos = mul(unity_ObjectToWorld, v.vertex);
                worldNormal = UnityObjectToWorldNormal(v.normal);
                #else
                worldPos = mul(unity_ObjectToWorld, v.vertex);
                worldNormal = UnityObjectToWorldNormal(v.normal);
                #endif

                o.pos = UnityWorldToClipPos(worldPos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = worldNormal;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;

                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float ndl = max(0, dot(i.normal, lightDir));

                return col * (0.2 + ndl * 0.8);
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}
