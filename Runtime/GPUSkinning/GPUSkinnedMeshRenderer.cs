using UnityEngine;

namespace Snm.Runtime.GPUSkinning
{
    /// <summary>
    /// High-level GPU skinning renderer.
    /// Computes bone matrices from Transform array + bindposes and delegates to GPUSkinUploader.
    /// Uses dirty-flag caching: only recomputes when bone transforms actually change.
    /// </summary>
    public class GPUSkinRenderer : IGPUSkinRenderer
    {
        public SkinningMode Mode => SkinningMode.LiveBones;

        private readonly GPUSkinUploader _uploader;
        private readonly Matrix4x4[] _bindposes;
        private readonly Transform[] _boneTransforms;
        private readonly Transform _meshTransform;
        private readonly int _boneCount;

        public GPUSkinRenderer(
            Mesh mesh,
            Matrix4x4[] bindposes,
            Material material,
            Transform[] boneTransforms,
            Transform meshTransform)
        {
            _boneCount = boneTransforms.Length;
            _uploader = new GPUSkinUploader(mesh, material, _boneCount);
            _bindposes = bindposes;
            _boneTransforms = boneTransforms;
            _meshTransform = meshTransform;
        }

        public void SetupMesh()
        {
            _uploader.UploadMeshData();
        }

        /// <summary>
        /// Checks if any bone transform has changed, recomputes matrices if needed, and uploads to GPU.
        /// </summary>
        public void UpdateSkinning()
        {
            if (!HasAnyBoneChanged())
                return;

            ComputeBoneMatrices();
            _uploader.UploadBoneMatrices(_boneCount);
            ClearChangedFlags();
        }

        public void Render()
        {
            _uploader.Render(_meshTransform.localToWorldMatrix);
        }

        public void Dispose()
        {
            // No unmanaged resources; reserved for future use (NativeArray, compute buffers).
        }

        private bool HasAnyBoneChanged()
        {
            for (int i = 0; i < _boneCount; i++)
            {
                if (_boneTransforms[i].hasChanged)
                    return true;
            }
            return false;
        }

        private void ClearChangedFlags()
        {
            for (int i = 0; i < _boneCount; i++)
                _boneTransforms[i].hasChanged = false;
        }

        /// <summary>
        /// skinningMatrix[i] = boneTransform[i].localToWorldMatrix * bindpose[i]
        /// Transforms vertices from bind-pose space to current world space.
        /// </summary>
        private void ComputeBoneMatrices()
        {
            for (int i = 0; i < _boneCount; i++)
            {
                var skinningMatrix = _boneTransforms[i].localToWorldMatrix * _bindposes[i];
                _uploader.SetSkinningMatrix(i, skinningMatrix);
            }
        }
    }
}
