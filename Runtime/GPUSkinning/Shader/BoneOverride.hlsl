#ifndef BONE_OVERRIDE_INCLUDED
#define BONE_OVERRIDE_INCLUDED

// =============================================================================
// Bone Override — Sparse per-bone matrix override for baked animations
//
// Keyword: BONE_OVERRIDE_ON
//
// Allows overriding up to 8 bone matrices on top of the baked animation pose.
// Used for foot IK, look-at, or any per-bone procedural adjustment.
// Override matrices must be in root-local space (same space as baked matrices):
//   overrideMatrix = character.worldToLocalMatrix * desiredBoneWorldMatrix * bindpose
// =============================================================================

#if defined(BONE_OVERRIDE_ON) && defined(BAKED_SKINNING_ON)

#define MAX_BONE_OVERRIDES 8

int _BoneOverrideCount;
float _BoneOverrideIndices[MAX_BONE_OVERRIDES];
float4x4 _BoneOverrideMatrices[MAX_BONE_OVERRIDES];
float _BoneOverrideWeights[MAX_BONE_OVERRIDES];

half4x4 ApplyBoneOverride(half4x4 bakedMatrix, uint boneIndex)
{
    for (int i = 0; i < _BoneOverrideCount; i++)
    {
        if ((uint)_BoneOverrideIndices[i] == boneIndex)
        {
            float w = _BoneOverrideWeights[i];
            float4x4 overrideMat = _BoneOverrideMatrices[i];
            return (half4x4)lerp((float4x4)bakedMatrix, overrideMat, w);
        }
    }
    return bakedMatrix;
}

#define BONE_OVERRIDE(mat, boneIndex) ApplyBoneOverride(mat, boneIndex)

#else

#define BONE_OVERRIDE(mat, boneIndex) (mat)

#endif // BONE_OVERRIDE_ON && BAKED_SKINNING_ON

#endif // BONE_OVERRIDE_INCLUDED
