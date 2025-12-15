using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Snm.Runtime.GPUSkinning
{
    public class BoneWeightAssignerMB : MonoBehaviour
    {
        [SerializeField] private Mesh sharedMesh;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
        [SerializeField] private BoneConfig[] boneConfigs;

        private Transform[] _boneTransforms;
        private Matrix4x4[] _boneMatrices;
        private Mesh _skinningMesh;

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
            if (!sharedMesh || !meshFilter)
                return;

            if (_skinningMesh != null)
                return;

            _boneTransforms = boneConfigs.Select(b => b.boneTransform).ToArray();
            _boneMatrices = new Matrix4x4[_boneTransforms.Length];

            _skinningMesh = SkinnedMeshCreator.CreateSkinnedMesh(sharedMesh,
                CreateBoneWeights(sharedMesh.vertexCount, boneConfigs),
                CreateBindPoses(_boneTransforms, meshFilter.transform));
            meshFilter.sharedMesh = _skinningMesh;

            skinnedMeshRenderer.sharedMesh = _skinningMesh;
            skinnedMeshRenderer.bones = _boneTransforms;
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
            if (_skinningMesh == null || _boneTransforms == null) return;
            if (!meshRenderer) return;

            UpdateBoneMatrices();

            var mat = meshRenderer.sharedMaterial;
            if (!mat) return;

            mat.SetInt("_BoneCount", _boneMatrices.Length);
            mat.SetMatrixArray("_Bones", _boneMatrices);
        }

        // ---------------------------------------------------------
        //  UPDATE BONE MATRICES FOR GPU SKINNING
        // ---------------------------------------------------------
        [ContextMenu("UpdateBoneMatrices")]
        private void UpdateBoneMatrices()
        {
            var bindposes = _skinningMesh.bindposes;

            for (int i = 0; i < _boneTransforms.Length; i++)
            {
                var bone = _boneTransforms[i];

                if (!bone || i >= bindposes.Length)
                {
                    _boneMatrices[i] = Matrix4x4.identity;
                    continue;
                }

                _boneMatrices[i] = bone.localToWorldMatrix * bindposes[i];
            }
        }

        // ---------------------------------------------------------
        //  BUILD BINDPOSES
        // ---------------------------------------------------------
        private static Matrix4x4[] CreateBindPoses(Transform[] boneTransforms, Transform meshTransform)
        {
            var bindposes = new Matrix4x4[boneTransforms.Length];
            var meshToWorld = meshTransform.localToWorldMatrix;

            for (int i = 0; i < boneTransforms.Length; i++)
            {
                var t = boneTransforms[i];
                if (!t)
                {
                    bindposes[i] = Matrix4x4.identity;
                    continue;
                }

                bindposes[i] = t.worldToLocalMatrix * meshToWorld;
            }

            return bindposes;
        }

        // ---------------------------------------------------------
        //  BUILD BONE WEIGHTS (multi-weight, normalized)
        // ---------------------------------------------------------
        private static BoneWeight[] CreateBoneWeights(int vertexCount, BoneConfig[] boneConfigs)
        {
            // Temporary accumulation: supports multiple bones per vertex
            var temp = new List<(int bone, float weight)>[vertexCount];
            for (int i = 0; i < vertexCount; i++)
                temp[i] = new List<(int, float)>();

            // Fill accumulators
            for (int boneIndex = 0; boneIndex < boneConfigs.Length; boneIndex++)
            {
                var cfg = boneConfigs[boneIndex];
                if (cfg.vertexBoneWeights == null) continue;

                foreach (var vbw in cfg.vertexBoneWeights)
                {
                    if (vbw.vertexIndex < 0 || vbw.vertexIndex >= vertexCount)
                        continue;

                    temp[vbw.vertexIndex].Add((boneIndex, vbw.weight));
                }
            }

            // Convert accumulators → BoneWeight array
            var final = new BoneWeight[vertexCount];

            for (int v = 0; v < vertexCount; v++)
            {
                var list = temp[v];

                if (list.Count == 0)
                {
                    final[v] = new BoneWeight(); // no weight
                    continue;
                }

                // Sort by weight desc
                list.Sort((a, b) => b.weight.CompareTo(a.weight));

                // Unity supports only 4 weights
                var count = Mathf.Min(list.Count, 4);

                var total = 0f;
                for (int i = 0; i < count; i++)
                    total += list[i].weight;
                if (total < 1e-6f) total = 1f; // avoid division by zero

                var bw = new BoneWeight();

                if (count > 0) { bw.boneIndex0 = list[0].bone; bw.weight0 = list[0].weight / total; }
                if (count > 1) { bw.boneIndex1 = list[1].bone; bw.weight1 = list[1].weight / total; }
                if (count > 2) { bw.boneIndex2 = list[2].bone; bw.weight2 = list[2].weight / total; }
                if (count > 3) { bw.boneIndex3 = list[3].bone; bw.weight3 = list[3].weight / total; }

                final[v] = bw;
            }

            return final;
        }

        // ---------------------------------------------------------
        [Serializable]
        private class BoneConfig
        {
            public Transform boneTransform;
            public VertexBoneWeight[] vertexBoneWeights;
        }

        [Serializable]
        private class VertexBoneWeight
        {
            public int vertexIndex;
            public float weight;
        }
    }
}