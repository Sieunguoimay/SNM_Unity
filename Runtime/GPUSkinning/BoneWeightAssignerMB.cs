using System;
using System.Linq;
using UnityEngine;

namespace Snm.Runtime.GPUSkinning
{
    public class TransformBoneManager
    {
        private readonly Matrix4x4[] bindposes;
        private readonly Transform[] boneTransforms;
        private readonly Matrix4x4[] boneMatrices;

        public TransformBoneManager(Transform[] boneTransforms, Matrix4x4 meshToWorld)
        {
            this.boneTransforms = boneTransforms;

            bindposes = CreateBindpose(meshToWorld);
            boneMatrices = new Matrix4x4[boneTransforms.Length];
        }

        public Matrix4x4[] CreateBoneMatrices()
        {
            for (int i = 0; i < boneTransforms.Length; i++)
            {
                var boneTransform = boneTransforms[i];
                var binepose = bindposes[i];

                boneMatrices[i] = boneTransform.localToWorldMatrix * binepose;
            }

            return boneMatrices;
        }

        private Matrix4x4[] CreateBindpose(Matrix4x4 meshToWorld)
        {
            var bindposes = new Matrix4x4[boneTransforms.Length];

            for (int i = 0; i < boneTransforms.Length; i++)
            {
                var boneTransform = boneTransforms[i];

                bindposes[i] = boneTransform.localToWorldMatrix * meshToWorld;
            }
            return bindposes;
        }
    }

    [ExecuteInEditMode]
    public class BoneWeightAssignerMB : MonoBehaviour
    {
        [SerializeField] private Mesh sharedMesh;
        [SerializeField] private Material material;
        // [SerializeField] private MeshFilter meshFilter;
        // [SerializeField] private MeshRenderer meshRenderer;
        // [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
        [SerializeField] private BoneConfig[] boneConfigs;

        private Mesh _skinningMesh;
        private TransformBoneManager _boneManager;

        private void Awake() => Setup();

        private void OnDestroy() => Cleanup();

        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            Cleanup();
            Setup();
        }

        private void Setup()
        {
            if (!sharedMesh)
                return;

            var boneTransforms = boneConfigs.Select(bc => bc.transform).ToArray();
            _boneManager = new TransformBoneManager(boneTransforms, meshToWorld: transform.localToWorldMatrix);

            var boneDatas = boneConfigs
                .Select(cfg => new BoneData
                {
                    vertices = cfg.vertices
                        .Select(v => new VertexData { index = v.index, boneWeight = v.boneWeight })
                        .ToList()
                })
                .ToArray();
            var boneWeights = BoneWeightExtractor.ExtractBoneWeights(boneDatas, sharedMesh.vertexCount);
            _skinningMesh = SkinnedMeshCreator.CreateSkinnedMesh(sharedMesh, boneWeights);
        }


        private void Cleanup()
        {
            if (_skinningMesh == null) return;

            if (Application.IsPlaying(this))
                Destroy(_skinningMesh);
            else
                DestroyImmediate(_skinningMesh);
        }

        [ContextMenu("LateUpdate")]
        private void LateUpdate()
        {
            if (_boneManager == null) return;
            if (!material) return;

            var boneMatrices = _boneManager.CreateBoneMatrices();
            material.SetInt("_BoneCount", boneMatrices.Length);
            material.SetMatrixArray("_Bones", boneMatrices);
            Graphics.DrawMesh(_skinningMesh, transform.localToWorldMatrix, material, 0);
        }

        // ---------------------------------------------------------
        [Serializable]
        private class BoneConfig
        {
            public Transform transform;
            public VertexConfig[] vertices;
        }

        [Serializable]
        private class VertexConfig
        {
            public int index;
            public float boneWeight;
        }
    }
}