// Deprecated: Use ../Shaders/UnifiedSkinning.cginc with GPU_SKINNING_ON keyword instead.
#ifndef GPU_SKINNING_INCLUDED
#define GPU_SKINNING_INCLUDED

// ================= CONFIG =================
#define MAX_BONES 256

// ================= UNIFORMS =================
int _BoneCount;
float4x4 _Bones[MAX_BONES];

// ================= HELPERS =================
float4x4 GetBoneMatrixSafe(int idx)
{
    int safeIdx = clamp(idx, 0, _BoneCount - 1);
    return _Bones[safeIdx];
}

// ================= SKINNING =================
bool Skin(
    float4 vertex,
    float3 normal,
    float4 boneWeights,
    float4 boneIndices,
    out float4 outVertex,
    out float3 outNormal
)
{
    float wSum = dot(boneWeights, 1.0);

    if (_BoneCount <= 0 || wSum < 0.0001)
        return false;

    int i0 = (int)boneIndices.x;
    int i1 = (int)boneIndices.y;
    int i2 = (int)boneIndices.z;
    int i3 = (int)boneIndices.w;

    float w0 = boneWeights.x; 
    float w1 = boneWeights.y; 
    float w2 = boneWeights.z; 
    float w3 = boneWeights.w;

    float4x4 m0 = GetBoneMatrixSafe(i0);
    float4x4 m1 = GetBoneMatrixSafe(i1);
    float4x4 m2 = GetBoneMatrixSafe(i2);
    float4x4 m3 = GetBoneMatrixSafe(i3);

    float3 n0 = mul((float3x3)m0, normal) * w0; 
    float3 n1 = mul((float3x3)m1, normal) * w1; 
    float3 n2 = mul((float3x3)m2, normal) * w2; 
    float3 n3 = mul((float3x3)m3, normal) * w3; 

    float4 p0 = mul(m0, vertex) * boneWeights.x;
    float4 p1 = mul(m1, vertex) * boneWeights.y;
    float4 p2 = mul(m2, vertex) * boneWeights.z;
    float4 p3 = mul(m3, vertex) * boneWeights.w;

    outVertex = p0 + p1 + p2 + p3; 
    outNormal = normalize(n0 + n1 + n2 + n3);
    return true;
}

#ifndef GPU_SKINNING_VERTEX_INPUT
    #define GPU_SKINNING_VERTEX_INPUT float4 boneWeights : TEXCOORD1; float4 boneIndices : TEXCOORD2;
#endif

#ifndef SKIN
    #define SKIN(v, outVertex, outNormal) Skin((v).vertex, (v).normal, (v).boneWeights, (v).boneIndices, outVertex, outNormal)
#endif

#endif // GPU_SKINNING_INCLUDED
