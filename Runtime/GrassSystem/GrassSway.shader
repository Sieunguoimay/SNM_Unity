Shader "Instanced/GrassSway"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _WindDir ("Wind Direction", Vector) = (1,0,0,0)
        _WindStrength ("Wind Strength", Float) = 0.3
        _WindFrequency ("Wind Frequency", Float) = 1.5
        _InteractorStrength ("Interactor Strength", Float) = 1
        _TestDirection ("Test Direction", Vector) = (0,0,1,0)
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
            // float4 _InteractorPosAndRadius;
            
            float3 _TestDirection;

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

            float ease_OutSine(float x) { return sin((x * 3.1415) / 2.0); }
            float ease_OutQuad(float x) { return 1 - (1 - x) * (1 - x); }
            float ease_OutQuart(float x) { return 1 - pow(1 - x, 3.0); }
            float ease_OutCircle(float x) { return sqrt(1 - pow(1 - x, 2.0)); }

            float ease_InCircle(float x) { return 1.0 - sqrt(1.0 - pow(x, 2.0)); }
            float ease_InSine(float x) { return 1.0 - cos(x * 3.1415 * .5); }
            float ease_InCubic(float x) { return x * x * x; }
            
            float3 RotateFromTo(float3 v, float3 from, float3 to)
            {
                from = normalize(from);
                to   = normalize(to);

                float3 axis = cross(from, to);
                float  cosA = dot(from, to);

                // Handle parallel vectors
                if (cosA > 0.9999)
                    return v;

                // Handle opposite vectors
                if (cosA < -0.9999)
                {
                    float3 ortho = abs(from.y) < 0.999
                        ? float3(0,1,0)
                        : float3(1,0,0);

                    axis = normalize(cross(from, ortho));
                    return v * -1;
                }

                float angle = acos(cosA);
                axis = normalize(axis);

                // Rodrigues
                return v * cos(angle)
                    + cross(axis, v) * sin(angle)
                    + axis * dot(axis, v) * (1 - cos(angle));
            }

            float3 FaceDirection(float3 vertexPos, float3 dir)
            {
                return RotateFromTo(vertexPos, float3(0,1,0), dir);
            }

            // float3 Push(float3 vertexPos, float3 pushDir)
            // {
            //     float heightFactor = saturate(vertexPos.y);
            //     float3 outPos = vertexPos + float3(pushDir.x, 0, pushDir.z) * ease_OutSine(heightFactor);
            //     outPos.y *= (1.0 - pushDir.y);
            //     return outPos;
            // }

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                float4 rand = UNITY_ACCESS_INSTANCED_PROP(Grass, _Random);

                // float heightFactor = saturate(v.vertex.y);

                // Wind
                float tx = _Time.y * _WindFrequency + rand.x * 10.0;
                float swayx = sin(tx) * _WindStrength;// * heightFactor * heightFactor;
                float ty = _Time.y * _WindFrequency + rand.y * 10.0;
                float swayy = sin(ty) * _WindStrength;// * heightFactor * heightFactor;
                float3 windOffset = float3(swayx, 0, swayy);

                float3 localOrigin = float3(0,0,0);
                float3 worldOrigin = 
                    mul(unity_ObjectToWorld, float4(localOrigin,1)).xyz;
                
                //Trample
                float2 worldUV = (worldOrigin.xz - _TrampleRect.xy) / _TrampleRect.zw;
                float4 trample = tex2Dlod(_TrampleRT, float4(saturate(worldUV), 0, 0));
                float3 tramplePos = mul(unity_WorldToObject, float4(trample.xyz, 1)).xyz;

                float trampleImpact = trample.w;
                float dist_ = distance(localOrigin, tramplePos);
                float3 pushDir_ = normalize(localOrigin - tramplePos);
                float3 originTrampleOffset = pushDir_ * trampleImpact;// * _InteractorStrength;
                float3 trampleOffset = float3(originTrampleOffset.x, 0, originTrampleOffset.z);

                float3 grassDir = normalize(windOffset + trample.xyz * trampleImpact + float3(0, 1.0 - trampleImpact,0));

                // Combine
                float3 localPos = FaceDirection(v.vertex.xyz, grassDir);
                float3 worldPos = mul(unity_ObjectToWorld, float4(localPos,1)).xyz;
                
                v2f o;
                o.pos = UnityWorldToClipPos(worldPos.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                // o.color = float4(Push(float3(0,0,1),_TestDirection),1);
                // o.color = float4(mul(LookRotation(_TestDirection),float3(0,0,1)),1);

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
