#ifndef UNIFIED_SKINNING_INCLUDED
#define UNIFIED_SKINNING_INCLUDED

// =============================================================================
// Unified GPU Skinning Include
//
// Supports two skinning modes via shader keywords:
//   GPU_SKINNING_ON    — live bone matrices uploaded per-frame from CPU
//   BAKED_SKINNING_ON  — bone matrices sampled from pre-baked texture
//
// Both modes read vertex data from:
//   TEXCOORD1 = bone weights (xyzw, up to 4 bones)
//   TEXCOORD2 = bone indices (xyzw, up to 4 bones)
// =============================================================================

// ---- Shared vertex input macro ----
#define SKINNING_VERTEX_INPUT float4 boneWeights : TEXCOORD1; float4 boneIndices : TEXCOORD2;

// =============================================================================
// GPU_SKINNING_ON — Live bone matrix path
// =============================================================================
#ifdef GPU_SKINNING_ON

#define MAX_BONES 256

int _BoneCount;
float4x4 _Bones[MAX_BONES];

float4x4 GetBoneMatrixSafe(int idx)
{
    int safeIdx = clamp(idx, 0, _BoneCount - 1);
    return _Bones[safeIdx];
}

bool SkinLive(
    float4 vertex,
    float3 normal,
    float4 weights,
    float4 indices,
    out float4 outVertex,
    out float3 outNormal)
{
    float wSum = dot(weights, 1.0);
    if (_BoneCount <= 0 || wSum < 0.0001)
        return false;

    int i0 = (int)indices.x;
    int i1 = (int)indices.y;
    int i2 = (int)indices.z;
    int i3 = (int)indices.w;

    float4x4 m0 = GetBoneMatrixSafe(i0);
    float4x4 m1 = GetBoneMatrixSafe(i1);
    float4x4 m2 = GetBoneMatrixSafe(i2);
    float4x4 m3 = GetBoneMatrixSafe(i3);

    float4 p = mul(m0, vertex) * weights.x
             + mul(m1, vertex) * weights.y
             + mul(m2, vertex) * weights.z
             + mul(m3, vertex) * weights.w;

    float3 n = mul((float3x3)m0, normal) * weights.x
             + mul((float3x3)m1, normal) * weights.y
             + mul((float3x3)m2, normal) * weights.z
             + mul((float3x3)m3, normal) * weights.w;

    outVertex = p;
    outNormal = normalize(n);
    return true;
}

#endif // GPU_SKINNING_ON

// =============================================================================
// BAKED_SKINNING_ON — Texture-sampled bone matrix path
// =============================================================================
#ifdef BAKED_SKINNING_ON

sampler2D _boneTexture;
int _boneTextureBlockWidth;
int _boneTextureBlockHeight;
int _boneTextureWidth;
int _boneTextureHeight;

#if (SHADER_TARGET < 30 || SHADER_API_GLES)
uniform float frameIndex;
uniform float preFrameIndex;
uniform float transitionProgress;
#else
UNITY_INSTANCING_BUFFER_START(BakedSkinProps)
    UNITY_DEFINE_INSTANCED_PROP(float, frameIndex)
#define frameIndex_arr BakedSkinProps
    UNITY_DEFINE_INSTANCED_PROP(float, preFrameIndex)
#define preFrameIndex_arr BakedSkinProps
    UNITY_DEFINE_INSTANCED_PROP(float, transitionProgress)
#define transitionProgress_arr BakedSkinProps
UNITY_INSTANCING_BUFFER_END(BakedSkinProps)
#endif

half4x4 loadBoneMatFromTexture(uint frame, uint boneIndex)
{
    uint blockCount = _boneTextureWidth / _boneTextureBlockWidth;
    int2 uv;
    uv.y = frame / blockCount * _boneTextureBlockHeight;
    uv.x = _boneTextureBlockWidth * (frame - _boneTextureWidth / _boneTextureBlockWidth * uv.y);

    int matCount = _boneTextureBlockWidth / 4;
    uv.x = uv.x + (boneIndex % matCount) * 4;
    uv.y = uv.y + boneIndex / matCount;

    float2 uvFrame;
    uvFrame.x = uv.x / (float)_boneTextureWidth;
    uvFrame.y = uv.y / (float)_boneTextureHeight;
    half4 uvf = half4(uvFrame, 0, 0);

    float offset = 1.0f / (float)_boneTextureWidth;
    half4 c1 = tex2Dlod(_boneTexture, uvf);
    uvf.x = uvf.x + offset;
    half4 c2 = tex2Dlod(_boneTexture, uvf);
    uvf.x = uvf.x + offset;
    half4 c3 = tex2Dlod(_boneTexture, uvf);
    half4 c4 = half4(0, 0, 0, 1);

    half4x4 m;
    m._11_21_31_41 = c1;
    m._12_22_32_42 = c2;
    m._13_23_33_43 = c3;
    m._14_24_34_44 = c4;
    return m;
}

half4 SkinBaked(inout float4 vertex, inout float3 normal, inout float3 tangent,
    float4 weights, float4 indices)
{
#if (SHADER_TARGET < 30 || SHADER_API_GLES)
    float curFrame = frameIndex;
    float preAniFrame = preFrameIndex;
    float progress = transitionProgress;
#else
    float curFrame = UNITY_ACCESS_INSTANCED_PROP(frameIndex_arr, frameIndex);
    float preAniFrame = UNITY_ACCESS_INSTANCED_PROP(preFrameIndex_arr, preFrameIndex);
    float progress = UNITY_ACCESS_INSTANCED_PROP(transitionProgress_arr, transitionProgress);
#endif

    half4 bone = indices;
    int preFrame = curFrame;
    int nextFrame = curFrame + 1.0f;

    // Current frame skinning
    half4x4 matPre = loadBoneMatFromTexture(preFrame, bone.x) * weights.x;
    matPre += loadBoneMatFromTexture(preFrame, bone.y) * max(0, weights.y);
    matPre += loadBoneMatFromTexture(preFrame, bone.z) * max(0, weights.z);
    matPre += loadBoneMatFromTexture(preFrame, bone.w) * max(0, weights.w);

    // Next frame skinning (for interpolation)
    half4x4 matNext = loadBoneMatFromTexture(nextFrame, bone.x) * weights.x;
    matNext += loadBoneMatFromTexture(nextFrame, bone.y) * max(0, weights.y);
    matNext += loadBoneMatFromTexture(nextFrame, bone.z) * max(0, weights.z);
    matNext += loadBoneMatFromTexture(nextFrame, bone.w) * max(0, weights.w);

    float frameLerp = curFrame - preFrame;
    half4 localPosPre = mul(vertex, matPre);
    half4 localPosNext = mul(vertex, matNext);
    half4 localPos = lerp(localPosPre, localPosNext, frameLerp);

    // Normals
    half3 localNormPre = mul(normal, (float3x3)matPre);
    half3 localNormNext = mul(normal, (float3x3)matNext);
    normal = normalize(lerp(localNormPre, localNormNext, frameLerp));

    // Tangents
    half3 localTanPre = mul(tangent, (float3x3)matPre);
    half3 localTanNext = mul(tangent, (float3x3)matNext);
    tangent = normalize(lerp(localTanPre, localTanNext, frameLerp));

    // Animation transition blend
    half4x4 matPreAni = loadBoneMatFromTexture(preAniFrame, bone.x);
    half4 localPosPreAni = mul(vertex, matPreAni);
    localPos = lerp(localPos, localPosPreAni, (1.0f - progress) * (preAniFrame > 0.0f));

    return localPos;
}

half4 SkinBakedShadow(float4 vertex, float4 weights, float4 indices)
{
#if (SHADER_TARGET < 30 || SHADER_API_GLES)
    float curFrame = frameIndex;
    float preAniFrame = preFrameIndex;
    float progress = transitionProgress;
#else
    float curFrame = UNITY_ACCESS_INSTANCED_PROP(frameIndex_arr, frameIndex);
    float preAniFrame = UNITY_ACCESS_INSTANCED_PROP(preFrameIndex_arr, preFrameIndex);
    float progress = UNITY_ACCESS_INSTANCED_PROP(transitionProgress_arr, transitionProgress);
#endif

    half4 bone = indices;
    int preFrame = curFrame;
    int nextFrame = curFrame + 1.0f;

    half4x4 matPre = loadBoneMatFromTexture(preFrame, bone.x);
    half4x4 matNext = loadBoneMatFromTexture(nextFrame, bone.x);
    half4 localPos = lerp(mul(vertex, matPre), mul(vertex, matNext), curFrame - preFrame);

    half4x4 matPreAni = loadBoneMatFromTexture(preAniFrame, bone.x);
    half4 localPosPreAni = mul(vertex, matPreAni);
    localPos = lerp(localPos, localPosPreAni, (1.0f - progress) * (preAniFrame > 0.0f));

    return localPos;
}

#endif // BAKED_SKINNING_ON

// =============================================================================
// Unified SKIN macro — works with both modes
// For GPU_SKINNING_ON: outputs world-space position and normal
// For BAKED_SKINNING_ON: outputs local-space position (instance matrix applied by GPU)
// =============================================================================
#ifndef SKIN
    #ifdef GPU_SKINNING_ON
        #define SKIN(v, outVertex, outNormal) SkinLive((v).vertex, (v).normal, (v).boneWeights, (v).boneIndices, outVertex, outNormal)
    #elif defined(BAKED_SKINNING_ON)
        // Baked path modifies vertex/normal in-place and returns local pos
        #define SKIN_BAKED(v) SkinBaked((v).vertex, (v).normal, (v).tangent, (v).boneWeights, (v).boneIndices)
        #define SKIN_BAKED_SHADOW(v) SkinBakedShadow((v).vertex, (v).boneWeights, (v).boneIndices)
    #endif
#endif

#endif // UNIFIED_SKINNING_INCLUDED
