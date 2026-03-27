#ifndef UNIFIED_SKINNING_INCLUDED
#define UNIFIED_SKINNING_INCLUDED

// =============================================================================
// Unified GPU Skinning (URP)
//
// Keywords:
//   GPU_SKINNING_ON    — live bone matrices uploaded per-frame from CPU
//   BAKED_SKINNING_ON  — bone matrices sampled from pre-baked texture
//
// Vertex data convention:
//   TEXCOORD1 = bone weights (xyzw, up to 4 bones)
//   TEXCOORD2 = bone indices (xyzw, up to 4 bones)
// =============================================================================

#define SKINNING_VERTEX_INPUT float4 boneWeights : TEXCOORD1; float4 boneIndices : TEXCOORD2;

// =============================================================================
// GPU_SKINNING_ON — Live bone matrix path
// =============================================================================
#if defined(GPU_SKINNING_ON)

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
    outVertex = float4(0, 0, 0, 0);
    outNormal = float3(0, 0, 0);

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

    outVertex = mul(m0, vertex) * weights.x
              + mul(m1, vertex) * weights.y
              + mul(m2, vertex) * weights.z
              + mul(m3, vertex) * weights.w;

    outNormal = mul((float3x3)m0, normal) * weights.x
              + mul((float3x3)m1, normal) * weights.y
              + mul((float3x3)m2, normal) * weights.z
              + mul((float3x3)m3, normal) * weights.w;
    outNormal = normalize(outNormal);

    return true;
}

#define SKIN(v, outVertex, outNormal) SkinLive((v).positionOS, (v).normalOS, (v).boneWeights, (v).boneIndices, outVertex, outNormal)

#endif // GPU_SKINNING_ON

// =============================================================================
// BAKED_SKINNING_ON — Texture-sampled bone matrix path
// =============================================================================
#if defined(BAKED_SKINNING_ON)

TEXTURE2D(_boneTexture);
SAMPLER(sampler_boneTexture);

int _boneTextureBlockWidth;
int _boneTextureBlockHeight;
int _boneTextureWidth;
int _boneTextureHeight;

#if defined(UNITY_INSTANCING_ENABLED) || defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
UNITY_INSTANCING_BUFFER_START(BakedSkinProps)
    UNITY_DEFINE_INSTANCED_PROP(float, frameIndex)
    UNITY_DEFINE_INSTANCED_PROP(float, preFrameIndex)
    UNITY_DEFINE_INSTANCED_PROP(float, transitionProgress)
UNITY_INSTANCING_BUFFER_END(BakedSkinProps)
#else
float frameIndex;
float preFrameIndex;
float transitionProgress;
#endif

half4x4 LoadBoneMatFromTexture(uint frame, uint boneIndex)
{
    uint blockCount = (uint)_boneTextureWidth / (uint)_boneTextureBlockWidth;
    int2 uv;
    uv.y = frame / blockCount * _boneTextureBlockHeight;
    uv.x = _boneTextureBlockWidth * (frame - _boneTextureWidth / _boneTextureBlockWidth * uv.y);

    int matCount = _boneTextureBlockWidth / 4;
    uv.x = uv.x + (boneIndex % (uint)matCount) * 4;
    uv.y = uv.y + boneIndex / (uint)matCount;

    float2 uvFrame;
    uvFrame.x = uv.x / (float)_boneTextureWidth;
    uvFrame.y = uv.y / (float)_boneTextureHeight;

    float offset = 1.0 / (float)_boneTextureWidth;

    half4 c1 = SAMPLE_TEXTURE2D_LOD(_boneTexture, sampler_boneTexture, uvFrame, 0);
    half4 c2 = SAMPLE_TEXTURE2D_LOD(_boneTexture, sampler_boneTexture, uvFrame + float2(offset, 0), 0);
    half4 c3 = SAMPLE_TEXTURE2D_LOD(_boneTexture, sampler_boneTexture, uvFrame + float2(offset * 2, 0), 0);
    half4 c4 = half4(0, 0, 0, 1);

    half4x4 m;
    m._11_21_31_41 = c1;
    m._12_22_32_42 = c2;
    m._13_23_33_43 = c3;
    m._14_24_34_44 = c4;
    return m;
}

#include "BoneOverride.hlsl"

// Wrapper: loads baked bone matrix then applies any override.
half4x4 GetBoneMatrix(uint frame, uint boneIndex)
{
    return BONE_OVERRIDE(LoadBoneMatFromTexture(frame, boneIndex), boneIndex);
}

float4 SkinBaked(inout float4 vertex, inout float3 normal, inout float3 tangent,
    float4 weights, float4 indices)
{
#if defined(UNITY_INSTANCING_ENABLED) || defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    float curFrame = UNITY_ACCESS_INSTANCED_PROP(BakedSkinProps, frameIndex);
    float preAniFrame = UNITY_ACCESS_INSTANCED_PROP(BakedSkinProps, preFrameIndex);
    float progress = UNITY_ACCESS_INSTANCED_PROP(BakedSkinProps, transitionProgress);
#else
    float curFrame = frameIndex;
    float preAniFrame = preFrameIndex;
    float progress = transitionProgress;
#endif

    half4 bone = indices;
    int preFrame = (int)curFrame;
    int nextFrame = preFrame + 1;

    half4x4 matPre = GetBoneMatrix(preFrame, (uint)bone.x) * weights.x;
    matPre += GetBoneMatrix(preFrame, (uint)bone.y) * max(0, weights.y);
    matPre += GetBoneMatrix(preFrame, (uint)bone.z) * max(0, weights.z);
    matPre += GetBoneMatrix(preFrame, (uint)bone.w) * max(0, weights.w);

    half4x4 matNext = GetBoneMatrix(nextFrame, (uint)bone.x) * weights.x;
    matNext += GetBoneMatrix(nextFrame, (uint)bone.y) * max(0, weights.y);
    matNext += GetBoneMatrix(nextFrame, (uint)bone.z) * max(0, weights.z);
    matNext += GetBoneMatrix(nextFrame, (uint)bone.w) * max(0, weights.w);

    float frameLerp = curFrame - preFrame;
    float4 localPosPre = mul(vertex, matPre);
    float4 localPosNext = mul(vertex, matNext);
    float4 localPos = lerp(localPosPre, localPosNext, frameLerp);

    half3 normPre = mul(normal, (float3x3)matPre);
    half3 normNext = mul(normal, (float3x3)matNext);
    normal = normalize(lerp(normPre, normNext, frameLerp));

    half3 tanPre = mul(tangent, (float3x3)matPre);
    half3 tanNext = mul(tangent, (float3x3)matNext);
    tangent = normalize(lerp(tanPre, tanNext, frameLerp));

    half4x4 matPreAni = GetBoneMatrix((uint)preAniFrame, (uint)bone.x);
    float4 localPosPreAni = mul(vertex, matPreAni);
    localPos = lerp(localPos, localPosPreAni, (1.0 - progress) * (preAniFrame > 0.0));

    return localPos;
}

float4 SkinBakedShadow(float4 vertex, float4 weights, float4 indices)
{
#if defined(UNITY_INSTANCING_ENABLED) || defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    float curFrame = UNITY_ACCESS_INSTANCED_PROP(BakedSkinProps, frameIndex);
    float preAniFrame = UNITY_ACCESS_INSTANCED_PROP(BakedSkinProps, preFrameIndex);
    float progress = UNITY_ACCESS_INSTANCED_PROP(BakedSkinProps, transitionProgress);
#else
    float curFrame = frameIndex;
    float preAniFrame = preFrameIndex;
    float progress = transitionProgress;
#endif

    half4 bone = indices;
    int preFrame = (int)curFrame;
    int nextFrame = preFrame + 1;

    half4x4 matPre = GetBoneMatrix(preFrame, (uint)bone.x);
    half4x4 matNext = GetBoneMatrix(nextFrame, (uint)bone.x);
    float4 localPos = lerp(mul(vertex, matPre), mul(vertex, matNext), curFrame - preFrame);

    half4x4 matPreAni = GetBoneMatrix((uint)preAniFrame, (uint)bone.x);
    float4 localPosPreAni = mul(vertex, matPreAni);
    localPos = lerp(localPos, localPosPreAni, (1.0 - progress) * (preAniFrame > 0.0));

    return localPos;
}

#define SKIN_BAKED(v) SkinBaked((v).positionOS, (v).normalOS, (v).tangentOS.xyz, (v).boneWeights, (v).boneIndices)
#define SKIN_BAKED_SHADOW(v) SkinBakedShadow((v).positionOS, (v).boneWeights, (v).boneIndices)

#endif // BAKED_SKINNING_ON

#endif // UNIFIED_SKINNING_INCLUDED
