Shader "Custom/GpuSkin"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color Tint", Color) = (1,1,1,1)
        _BoneCount("Bone Count", Float) = 0
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

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _Color;

            int _BoneCount;
            #define MAX_BONES 256
            float4x4 _Bones[MAX_BONES];

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;

                // custom bone data
                float4 boneWeights : TEXCOORD1;
                float4 boneIndices : TEXCOORD2;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4x4 GetBoneMatrix(int idx)
            {
                int safeIdx = clamp(idx, 0, _BoneCount - 1);
                return _Bones[safeIdx];
            }

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float4 skinnedPos = float4(0,0,0,0);
                float3 skinnedNormal = float3(0,0,0);

                float w0 = v.boneWeights.x;
                float w1 = v.boneWeights.y;
                float w2 = v.boneWeights.z;
                float w3 = v.boneWeights.w;

                // if any bone is used
                if (_BoneCount > 0 && (w0 + w1 + w2 + w3) > 0.0001f)
                {
                    int i0 = (int)v.boneIndices.x;
                    int i1 = (int)v.boneIndices.y;
                    int i2 = (int)v.boneIndices.z;
                    int i3 = (int)v.boneIndices.w;

                    float4x4 m0 = GetBoneMatrix(i0);
                    float4x4 m1 = GetBoneMatrix(i1);
                    float4x4 m2 = GetBoneMatrix(i2);
                    float4x4 m3 = GetBoneMatrix(i3);

                    skinnedPos =
                        mul(m0, v.vertex) * w0 +
                        mul(m1, v.vertex) * w1 +
                        mul(m2, v.vertex) * w2 +
                        mul(m3, v.vertex) * w3;

                    float3 n0 = mul((float3x3)m0, v.normal) * w0;
                    float3 n1 = mul((float3x3)m1, v.normal) * w1;
                    float3 n2 = mul((float3x3)m2, v.normal) * w2;
                    float3 n3 = mul((float3x3)m3, v.normal) * w3;

                    skinnedNormal = normalize(n0 + n1 + n2 + n3);
                }
                else
                {
                    // fallback: no skinning
                    skinnedPos = mul(unity_ObjectToWorld, v.vertex);
                    skinnedNormal = UnityObjectToWorldNormal(v.normal);
                }

                o.pos = UnityWorldToClipPos(skinnedPos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = skinnedNormal;
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
