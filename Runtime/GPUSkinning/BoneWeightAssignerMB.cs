using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
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
        private Mesh _clonedMesh;

        public Mesh ClonedMesh => _clonedMesh;

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

            if (_clonedMesh != null)
                return;

            _boneTransforms = boneConfigs.Select(b => b.boneTransform).ToArray();

            _clonedMesh = Instantiate(sharedMesh);

            var boneWeights = BuildBoneWeights(sharedMesh.vertexCount);
            _clonedMesh.bindposes = BuildBindPoses();

            var boneWeights4 = new List<Vector4>(boneWeights.Length);
            var boneIndices4 = new List<Vector4>(boneWeights.Length);

            for (int i = 0; i < boneWeights.Length; i++)
            {
                BoneWeight bw = boneWeights[i];

                // ---- weights ----
                Vector4 w = new Vector4(
                    bw.weight0,
                    bw.weight1,
                    bw.weight2,
                    bw.weight3
                );
                boneWeights4.Add(w);

                // ---- indices ----
                Vector4 idx = new Vector4(
                    bw.boneIndex0,
                    bw.boneIndex1,
                    bw.boneIndex2,
                    bw.boneIndex3
                );
                boneIndices4.Add(idx);
            }
            _clonedMesh.SetUVs(1, boneWeights4);   // List<Vector4>
            _clonedMesh.SetUVs(2, boneIndices4);   // List<Vector4>

            _boneMatrices = new Matrix4x4[_boneTransforms.Length];

            meshFilter.sharedMesh = _clonedMesh;

            skinnedMeshRenderer.sharedMesh = _clonedMesh;
            skinnedMeshRenderer.bones = _boneTransforms;
        }

        private void Cleanup()
        {
            if (_clonedMesh == null) return;

            if (Application.IsPlaying(this))
                Destroy(_clonedMesh);
            else
                DestroyImmediate(_clonedMesh);
        }

        private void LateUpdate()
        {
            if (_clonedMesh == null || _boneTransforms == null) return;
            if (!meshRenderer) return;

            UpdateBoneMatrices();

            var mat = meshRenderer.material;
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
            var bindposes = _clonedMesh.bindposes;

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
        private Matrix4x4[] BuildBindPoses()
        {
            var bindposes = new Matrix4x4[_boneTransforms.Length];
            var meshToWorld = meshFilter.transform.localToWorldMatrix;

            for (int i = 0; i < _boneTransforms.Length; i++)
            {
                var t = _boneTransforms[i];
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
        private BoneWeight[] BuildBoneWeights(int vertexCount)
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
                int count = Mathf.Min(list.Count, 4);

                float total = 0f;
                for (int i = 0; i < count; i++)
                    total += list[i].weight;
                if (total < 1e-6f) total = 1f; // avoid division by zero

                BoneWeight bw = new BoneWeight();

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

    [CustomEditor(typeof(BoneWeightAssignerMB))]
    public class BoneWeightAssignerMBEditor : Editor
    {
        private void OnSceneGUI()
        {
            var mb = (BoneWeightAssignerMB)target;
            if (mb.ClonedMesh == null) return;
            var vertices = mb.ClonedMesh.vertices;

            Handles.matrix = mb.transform.localToWorldMatrix;

            // Handles.Label(Vector3.up * 2f, $"Bone Weights Count: {mb.ClonedMesh.boneWeights.Length}");
            for (int i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i];
                Handles.Label(v, $"{i}");
            }
        }
    }
}