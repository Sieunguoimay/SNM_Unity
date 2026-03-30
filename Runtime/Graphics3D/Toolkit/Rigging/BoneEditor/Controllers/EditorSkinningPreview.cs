#if UNITY_EDITOR
using Snm.Graphics3D.GPUSkinning;
using UnityEngine;

namespace Snm.Graphics3D.Rigging
{
    /// <summary>
    /// Editor-only skinning preview. Wraps GPUSkinUploader for live GPU-skinned deformation.
    /// </summary>
    public class EditorSkinningPreview
    {
        private GPUSkinUploader _uploader;
        private Material _material;

        public bool IsReady => _uploader != null;

        public void Create(Mesh mesh, Shader shader, int boneCount)
        {
            Cleanup();
            if (mesh == null || shader == null) return;

            _material = new Material(shader);
            _material.EnableKeyword("GPU_SKINNING_ON");
            _uploader = new GPUSkinUploader(mesh, _material, boneCount);
            _uploader.UploadMeshData();
        }

        public void Cleanup()
        {
            if (_material != null)
            {
                Object.DestroyImmediate(_material);
                _material = null;
            }
            _uploader = null;
        }

        public void SetSkinningMatrix(int boneIndex, Matrix4x4 matrix)
        {
            _uploader?.SetSkinningMatrix(boneIndex, matrix);
        }

        public void UploadAndRender(int boneCount, Matrix4x4 meshToWorld)
        {
            if (_uploader == null) return;
            _uploader.UploadBoneMatrices(boneCount);
            _uploader.Render(meshToWorld);
        }
    }
}
#endif
