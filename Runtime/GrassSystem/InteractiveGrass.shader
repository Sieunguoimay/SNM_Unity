Shader "Snm/InteractiveGrass"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _WindStrength ("Wind Strength", Float) = 1.5
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

            sampler2D _DuDvMap;
            float4 _DuDvMap_ST;
            float _WindStrength;

            // UNITY_INSTANCING_BUFFER_START(Grass)
            //     UNITY_DEFINE_INSTANCED_PROP(float4, _Random)
            // UNITY_INSTANCING_BUFFER_END(Grass)

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
                // float4 color: TEXCOORD2;
            };

            float ease_OutSine(float x) { return sin((x * 3.1415) / 2.0); }
            float ease_OutQuad(float x) { return 1 - (1 - x) * (1 - x); }
            float ease_OutQuart(float x) { return 1 - pow(1 - x, 3.0); }
            float ease_OutCircle(float x) { return sqrt(1 - pow(1 - x, 2.0)); }
            float ease_OutExpo(float x) { return x == 1 ? 1 : 1 - pow(2, -10 * x); }
            float ease_OutBack(float x) { 
                const float c1 = 1.70158;
                const float c3 = c1 + 1;

                return 1 + c3 * pow(x - 1, 3) + c1 * pow(x - 1, 2);
            }

            float ease_InSine(float x) { return 1.0 - cos(x * 3.1415 * .5); }
            float ease_InCubic(float x) { return x * x * x; }
            float ease_InCircle(float x) { return 1.0 - sqrt(1.0 - pow(x, 2.0)); }
            
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

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                // float4 rand = UNITY_ACCESS_INSTANCED_PROP(Grass, _Random);

                float3 worldOrigin = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                float height01 = saturate(v.vertex.y);
                float2 worldUV = saturate((worldOrigin.xz - _TrampleRect.xy) / _TrampleRect.zw);
                
                //Trample
                float4 trample = tex2Dlod(_TrampleRT, float4(worldUV, 0, 0));
                float2 trampleDir = trample.xy;

                float trampleImpact = ease_OutSine(trample.w);
                float3 grassDirByTrample = normalize(
                    float3(trampleDir.x, 0, trampleDir.y) * trampleImpact 
                    + float3(0, 1.0 - trampleImpact, 0));

                float4 dudvRaw = tex2Dlod(_DuDvMap, float4(worldUV * .1 + _Time.y *.01, 0, 0));
                float2 dudv = (dudvRaw.xy * 2 - 1.0) + 0.5;
                float3 grassDirByWind = float3(dudv.x * _WindStrength, 1, dudv.y * _WindStrength);

                float3 combined = grassDirByWind + grassDirByTrample;
                combined.y = max(0, min(grassDirByWind.y, grassDirByTrample.y));

                float3 grassDir = normalize(lerp(float3(0, 1, 0), normalize(combined), ease_OutExpo(height01)));

                float3 localPos = RotateFromTo(v.vertex.xyz, float3(0,1,0), grassDir);
                float3 worldPos = mul(unity_ObjectToWorld, float4(localPos, 1)).xyz;

                v2f o;
                o.pos = UnityWorldToClipPos(worldPos.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = UnityObjectToWorldNormal(v.normal);

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
