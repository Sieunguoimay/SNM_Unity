#if UNITY_EDITOR
using UnityEngine;

namespace Snm.Runtime.GPUSkinning
{
    /// <summary>
    /// Reads bone matrices from baked animation textures on CPU.
    /// Mirrors the shader's LoadBoneMatFromTexture logic using Texture2D.GetPixel.
    /// Editor-only — used for bone gizmo visualization.
    /// </summary>
    public static class BakedBoneMatrixReader
    {
        /// <summary>
        /// Read a single bone matrix from the baked texture.
        /// Returns the matrix in root-local space (same as shader output).
        /// </summary>
        public static Matrix4x4 ReadBoneMatrix(
            AnimationTextureData texData, int textureIndex, int frame, int boneIndex)
        {
            if (texData?.bakedBoneTextures == null || textureIndex >= texData.bakedBoneTextures.Length)
                return Matrix4x4.identity;

            var tex = texData.bakedBoneTextures[textureIndex];
            int blockWidth = texData.textureBlockWidth;
            int blockHeight = texData.textureBlockHeight;
            int texWidth = tex.width;
            int texHeight = tex.height;

            int blockCount = texWidth / blockWidth;

            int uvY = frame / blockCount * blockHeight;
            int uvX = blockWidth * (frame - texWidth / blockWidth * uvY);

            int matCount = blockWidth / 4;
            uvX += (int)((uint)boneIndex % (uint)matCount) * 4;
            uvY += (int)((uint)boneIndex / (uint)matCount);

            if (uvX < 0 || uvY < 0 || uvX + 2 >= texWidth || uvY >= texHeight)
                return Matrix4x4.identity;

            Color c1 = tex.GetPixel(uvX, uvY);
            Color c2 = tex.GetPixel(uvX + 1, uvY);
            Color c3 = tex.GetPixel(uvX + 2, uvY);

            var m = Matrix4x4.identity;
            m.m00 = c1.r; m.m10 = c1.g; m.m20 = c1.b; m.m30 = c1.a;
            m.m01 = c2.r; m.m11 = c2.g; m.m21 = c2.b; m.m31 = c2.a;
            m.m02 = c3.r; m.m12 = c3.g; m.m22 = c3.b; m.m32 = c3.a;
            m.m03 = 0;    m.m13 = 0;    m.m23 = 0;    m.m33 = 1;
            return m;
        }

        /// <summary>
        /// Read all bone matrices for a given frame.
        /// </summary>
        public static Matrix4x4[] ReadAllBoneMatrices(
            AnimationTextureData texData, int textureIndex, int frame, int boneCount)
        {
            var matrices = new Matrix4x4[boneCount];
            for (int i = 0; i < boneCount; i++)
                matrices[i] = ReadBoneMatrix(texData, textureIndex, frame, i);
            return matrices;
        }

        /// <summary>
        /// Convert a baked bone matrix to a world-space position.
        /// bakedMatrix is in root-local space, bindpose transforms from bind to local.
        /// </summary>
        public static Vector3 BoneWorldPosition(
            Matrix4x4 characterLocalToWorld, Matrix4x4 bakedBoneMatrix, Matrix4x4 bindposeInverse)
        {
            var worldMatrix = characterLocalToWorld * bakedBoneMatrix * bindposeInverse;
            return (Vector3)worldMatrix.GetColumn(3);
        }
    }
}
#endif
