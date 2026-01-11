// Upgrade NOTE: upgraded instancing buffer 'Grass' to new syntax.

Shader "Snm/InteractiveGrass"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            sampler2D _TrampleMap;
            float4 _TrampleMap_ST;
            float4 _WorldCanvas;

            sampler2D _WindMap;//Dudv map
            float4 _WindParams;//x - Strength, y - speed, zw - world size

            StructuredBuffer<float4x4> _LocalToWorldMatrices;
            float4x4 _LocalToWorldMatricesX[400];
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                uint instanceID : SV_InstanceID;
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
                float3 f = normalize(from);
                float3 t = normalize(to);

                float3 axis = cross(f, t);
                float  cosA = dot(f, t);

                // v' = v + 2 * cross(q.xyz, cross(q.xyz, v) + q.w * v)
                // where q = [axis, 1 + cosA]
                float3 q = axis;
                float  qw = 1.0 + cosA;

                // Handle opposite direction safely
                if (qw < 1e-4)
                {
                    // Pick any perpendicular axis
                    q = abs(f.y) < 0.999 ? cross(f, float3(0,1,0)) : cross(f, float3(1,0,0));
                    qw = 0.0;
                }

                float3 t1 = cross(q, v);
                float3 t2 = cross(q, t1 + qw * v);

                return v + 2.0 * t2 / dot(float4(q, qw), float4(q, qw));
            }

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v)
                
                float4x4 localToWorld = _LocalToWorldMatrices[v.instanceID];

                float3 worldOrigin = mul(localToWorld, float4(0, 0, 0, 1)).xyz;
                float height01 = ease_OutExpo(saturate(v.vertex.y));
                float2 worldUV = saturate((worldOrigin.xz - _WorldCanvas.xy) / _WorldCanvas.zw);
                
                //Trample
                float4 trample = tex2Dlod(_TrampleMap, float4(worldUV, 0, 0));
                float trampleFactor = ease_OutSine(trample.w);
                float3 trampleDir = normalize(float3(trample.x * trampleFactor, 1.0 - trampleFactor, trample.y * trampleFactor));

                float windStrength = _WindParams.x;
                float windSpeed = _WindParams.y;
                float2 windMapScale = _WindParams.zw;

                float2 windUV = worldUV / windMapScale + _Time.y * windSpeed;
                float4 windRaw = tex2Dlod(_WindMap, float4(windUV, 0, 0));
                float2 wind = (windRaw.xy * 2 - 1.0) + 0.5;
                float3 windDir = float3(wind.x * windStrength, 1, wind.y * windStrength);

                float3 combinedDir = windDir + trampleDir;
                combinedDir.y = max(0, min(windDir.y, trampleDir.y));
                combinedDir = normalize(combinedDir);

                float3 grassDir = normalize(lerp(float3(0, 1, 0), combinedDir, height01));

                float3 localPos = RotateFromTo(v.vertex.xyz, float3(0, 1, 0), grassDir);
                float3 worldPos = mul(localToWorld, float4(localPos, 1)).xyz;

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
