Shader "Snm/InteractiveGrass"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Cull Off
        ZWrite On

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            // URP keywords for lighting
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // Textures and Samplers
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            TEXTURE2D(_TrampleMap);
            SAMPLER(sampler_TrampleMap);
            
            TEXTURE2D(_WindMap);
            SAMPLER(sampler_WindMap);

            // Properties
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _TrampleMap_ST;
                float4 _WorldCanvas;
                float4 _WindParams; // x - Strength, y - speed, zw - world size
                float _Cutoff;
            CBUFFER_END

            // Instancing
            StructuredBuffer<float4x4> _LocalToWorldMatrices;
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // Easing functions
            float ease_OutSine(float x) { return sin((x * PI) / 2.0); }
            float ease_OutQuad(float x) { return 1 - (1 - x) * (1 - x); }
            float ease_OutQuart(float x) { return 1 - pow(1 - x, 3.0); }
            float ease_OutCircle(float x) { return sqrt(1 - pow(1 - x, 2.0)); }
            float ease_OutExpo(float x) { return x == 1 ? 1 : 1 - pow(2, -10 * x); }
            float ease_OutBack(float x) 
            { 
                const float c1 = 1.70158;
                const float c3 = c1 + 1;
                return 1 + c3 * pow(x - 1, 3) + c1 * pow(x - 1, 2);
            }

            float ease_InSine(float x) { return 1.0 - cos(x * PI * 0.5); }
            float ease_InCubic(float x) { return x * x * x; }
            float ease_InCircle(float x) { return 1.0 - sqrt(1.0 - pow(x, 2.0)); }
            
            float3 RotateFromTo(float3 v, float3 from, float3 to)
            {
                float3 f = normalize(from);
                float3 t = normalize(to);

                float3 axis = cross(f, t);
                float cosA = dot(f, t);

                // v' = v + 2 * cross(q.xyz, cross(q.xyz, v) + q.w * v)
                // where q = [axis, 1 + cosA]
                float3 q = axis;
                float qw = 1.0 + cosA;

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

            Varyings vert(Attributes input, uint instanceID : SV_InstanceID)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                float4x4 localToWorld = _LocalToWorldMatrices[instanceID];

                float3 worldOrigin = mul(localToWorld, float4(0, 0, 0, 1)).xyz;
                float height01 = ease_OutExpo(saturate(input.positionOS.y));
                float2 worldUV = saturate((worldOrigin.xz - _WorldCanvas.xy) / _WorldCanvas.zw);
                
                // Trample
                float4 trample = SAMPLE_TEXTURE2D_LOD(_TrampleMap, sampler_TrampleMap, worldUV, 0);
                float trampleFactor = ease_OutSine(trample.w);
                float3 trampleDir = normalize(float3(trample.x * trampleFactor, 1.0 - trampleFactor, trample.y * trampleFactor));

                float windStrength = _WindParams.x;
                float windSpeed = _WindParams.y;
                float2 windMapScale = _WindParams.zw;

                float2 windUV = worldUV / windMapScale + _Time.y * windSpeed;
                float4 windRaw = SAMPLE_TEXTURE2D_LOD(_WindMap, sampler_WindMap, windUV, 0);
                float2 wind = (windRaw.xy * 2 - 1.0) + 0.5;
                float3 windDir = float3(wind.x * windStrength, 1, wind.y * windStrength);

                float3 combinedDir = windDir + trampleDir;
                combinedDir.y = max(0, min(windDir.y, trampleDir.y));
                combinedDir = normalize(combinedDir);

                float3 grassDir = normalize(lerp(float3(0, 1, 0), combinedDir, height01));

                float3 localPos = RotateFromTo(input.positionOS.xyz, float3(0, 1, 0), grassDir);
                float3 worldPos = mul(localToWorld, float4(localPos, 1)).xyz;

                output.positionWS = worldPos;
                output.positionCS = TransformWorldToHClip(worldPos);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                // Transform normal to world space
                float3 normalLS = normalize(mul((float3x3)localToWorld, input.normalOS));
                output.normalWS = normalLS;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // Alpha cutout
                clip(albedo.a - _Cutoff);

                // Get main light
                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;
                
                // Simple N.L lighting (matching original shader)
                float ndl = max(0, dot(input.normalWS, lightDir));
                
                half3 finalColor = albedo.rgb * (0.2 + ndl * 0.8);
                
                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }
    }
}
