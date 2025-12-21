using System;
using System.Collections.Generic;
using System.Linq;
using Snm.Runtime.GPUSkinning;
using UnityEditor;
using UnityEngine;

namespace Snm.GPUSkinning.BoneWeightTool
{
    public class BindposeTransformsTool
    {
        private IReadOnlyList<RuntimeBone> _bones;
        private IReadOnlyList<BoneSelector> _boneSelectors;
        private Matrix4x4 _meshToWorld;
        private BindposeTransformMB[] _bindposeTransforms;

        public BindposeTransformMB[] BindposeTransforms => _bindposeTransforms;

        public void Show()
        {
            TryCreateBindposeTransforms();
        }

        public void Hide()
        {
            TryDestroyBindposeTransforms();
        }

        public void SetBones(
            IReadOnlyList<RuntimeBone> bones,
            Matrix4x4 meshToWorld,
            IReadOnlyList<BoneSelector> boneSelectors)
        {
            _bones = bones;
            _boneSelectors = boneSelectors;
            _meshToWorld = meshToWorld;

            TryDestroyBindposeTransforms();
            TryCreateBindposeTransforms();
        }

        private void TryCreateBindposeTransforms()
        {
            if (_bones == null || _bindposeTransforms != null) return;

            var transforms = CreateBoneHierarchy(
                _bones.Select(b => b.bindpose).ToArray(),
                _meshToWorld,
                Array.Empty<int>());

            _bindposeTransforms = new BindposeTransformMB[_bones.Count];

            for (int i = 0; i < transforms.Length; i++)
            {
                _bindposeTransforms[i] = transforms[i].gameObject.AddComponent<BindposeTransformMB>();
                _bindposeTransforms[i].SetBoneSelector(_boneSelectors[i]);
            }
        }

        public static Transform[] CreateBoneHierarchy(Matrix4x4[] bindposes, Matrix4x4 meshToWorld, int[] parents)
        {
            var transforms = new Transform[bindposes.Length];
            for (int i = 0; i < bindposes.Length; i++)
            {
                var bp = bindposes[i];
                var tr = new GameObject($"bone_{i}").transform;
                UpdateTransform(tr, bp.inverse * meshToWorld);
                transforms[i] = tr;
            }

            for (int i = 0; i < transforms.Length; i++)
            {
                var tr = transforms[i];
                var parent = i < parents.Length ? parents[i] : -1;
                if (parent < 0) continue;

                tr.SetParent(transforms[parent]);
            }
            return transforms;
        }

        private void TryDestroyBindposeTransforms()
        {
            if (_bindposeTransforms != null)
            {
                foreach (BindposeTransformMB v in _bindposeTransforms)
                {
                    v.SetBoneSelector(null);
                }
                DestroyBoneTransforms(_bindposeTransforms);
                _bindposeTransforms = null;
            }
        }

        public Matrix4x4[] GetBindposes(Matrix4x4 meshToWorld)
        {
            return _bindposeTransforms
                .Select(bt => bt.GetWorldToLocalMatrix() * meshToWorld)
                .ToArray();
        }

        public static void DestroyBoneTransforms(BindposeTransformMB[] boneTransforms)
        {
            for (int i = 0; i < boneTransforms.Length; i++)
            {
                var bt = boneTransforms[i];
                if (bt && bt.gameObject) UnityEngine.Object.DestroyImmediate(bt.gameObject);
            }
        }

        public static void DecomposeMatrix(
            Matrix4x4 matrix,
            out Vector3 position,
            out Quaternion rotation,
            out Vector3 scale)
        {
            // Extract position (column 3)
            position = matrix.GetColumn(3);

            // Extract scale
            Vector3 col0 = matrix.GetColumn(0);
            Vector3 col1 = matrix.GetColumn(1);
            Vector3 col2 = matrix.GetColumn(2);

            scale = new Vector3(
                col0.magnitude,
                col1.magnitude,
                col2.magnitude
            );

            // Prevent division by zero
            if (scale.x == 0 || scale.y == 0 || scale.z == 0)
            {
                rotation = Quaternion.identity;
                return;
            }

            // Remove scale from the matrix
            Matrix4x4 rotationMatrix = Matrix4x4.identity;
            rotationMatrix.SetColumn(0, col0 / scale.x);
            rotationMatrix.SetColumn(1, col1 / scale.y);
            rotationMatrix.SetColumn(2, col2 / scale.z);

            // Handle negative scale (mirroring)
            if (Matrix4x4.Determinant(rotationMatrix) < 0)
            {
                scale.x = -scale.x;
                rotationMatrix.SetColumn(0, rotationMatrix.GetColumn(0) * -1f);
            }

            // Extract rotation
            rotation = Quaternion.LookRotation(
                rotationMatrix.GetColumn(2),
                rotationMatrix.GetColumn(1)
            );
        }

        public static void UpdateTransform(Transform tr, Matrix4x4 newMatrix)
        {
            DecomposeMatrix(newMatrix, out Vector3 position, out Quaternion rotation, out Vector3 scale);

            tr.SetPositionAndRotation(position, rotation);
            tr.localScale = scale;
        }
    }
}