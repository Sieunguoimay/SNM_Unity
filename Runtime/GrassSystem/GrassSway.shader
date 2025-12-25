Shader "Instanced/GrassSway"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _WindDir ("Wind Direction", Vector) = (1,0,0,0)
        _WindStrength ("Wind Strength", Float) = 0.3
        _WindFrequency ("Wind Frequency", Float) = 1.5
        _InteractorStrength ("Interactor Strength", Float) = 1
    }

    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Cull Off
        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            //Trample
            sampler2D _TrampleRT;
            float4 _TrampleRT_ST;
            float4 _TrampleRect;

            float3 _WindDir;
            float _WindStrength;
            float _WindFrequency;
            float _InteractorStrength;
            float4 _InteractorPosAndRadius;

            UNITY_INSTANCING_BUFFER_START(Grass)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Random)
            UNITY_INSTANCING_BUFFER_END(Grass)

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float4 color: TEXCOORD2;
            };

            float ease_OutQuart(float x) { return 1 - pow(1 - x, 3.0); }
            float ease_OutCircle(float x) { return sqrt(1 - pow(1 - x, 2.0)); }
            float ease_OutQuad(float x) { return 1 - (1 - x) * (1 - x); }
            float ease_OutSin(float x) { return sin((x * 3.1415) / 2.0); }

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                float4 rand = UNITY_ACCESS_INSTANCED_PROP(Grass, _Random);

                float heightFactor = saturate(v.vertex.y);

                // Wind
                float tx = _Time.y * _WindFrequency + rand.x * 10.0;
                float swayx = sin(tx) * _WindStrength * heightFactor * heightFactor;
                float ty = _Time.y * _WindFrequency + rand.y * 10.0;
                float swayy = sin(ty) * _WindStrength * heightFactor * heightFactor;
                float3 windOffset = float3(swayx, 0, swayy);

                // Interaction
                float3 worldOrigin =
                    mul(unity_ObjectToWorld, float4(0,0,0,1)).xyz;

                float3 interactorPos = _InteractorPosAndRadius.xyz;
                float interactorRadius = _InteractorPosAndRadius.w;

                float dist = distance(worldOrigin, interactorPos);
                float influence = saturate(1.0 - dist / interactorRadius);

                float3 pushDir = normalize(worldOrigin - interactorPos);
                float3 interactionOffset = pushDir * influence * _InteractorStrength * ease_OutSin(heightFactor);
                interactionOffset.y *= .15;

                //Trample
                float2 worldUV = (worldOrigin.xz - _TrampleRect.xy) / _TrampleRect.zw;// * .5 + .5;
                float4 tramplePos = tex2Dlod(_TrampleRT, float4(saturate(worldUV), 0, 0));
                float3 tramplePos3 = tramplePos.xyz;

                float dist_ = distance(worldOrigin, tramplePos3);
                float influence_ = tramplePos.w;// * saturate(1.0 - dist_ / interactorRadius);
                float3 pushDir_ = normalize(worldOrigin - tramplePos3);
                float3 interactionOffset_ = pushDir_ * influence_ * _InteractorStrength * ease_OutSin(heightFactor);
                interactionOffset_.y *= .15;

                float useTrample = step(influence, influence_); 
                float3 offset = lerp(interactionOffset, interactionOffset_, useTrample);

                // Combine
                float3 localPos = v.vertex.xyz + windOffset + offset;
                float3 worldPos = mul(unity_ObjectToWorld, float4(localPos,1)).xyz;

                v2f o;
                o.pos = UnityWorldToClipPos(worldPos.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.color = tramplePos.w;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);// * _Color;

                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float ndl = max(0, dot(i.normal, lightDir));
                return col * (0.2 + ndl * 0.8);
            }
            ENDCG
        }
    }
}
