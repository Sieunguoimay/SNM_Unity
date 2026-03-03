#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Snm.Runtime.GPUSkinning;
using Snm.Runtime.Unity;
using UnityEditor;
using UnityEngine;

namespace Snm.GPUSkinning.BoneWeightTool
{
    public class BoneTransformsTool
    {
        private IReadOnlyList<BoneSelector> _boneSelectors;
        private BoneTransformMB[] _boneTransforms;
        private BoneVisualizerMB _visualizer;

        public BoneTransformMB[] BoneTransforms => _boneTransforms;

        public void Show()
        {
            TryCreateBoneTransforms();
        }

        public void Hide()
        {
            TryDestroyBoneTransforms();
        }

        public void SetBoneSelectors(
            IReadOnlyList<BoneSelector> boneSelectors)
        {
            _boneSelectors = boneSelectors;

            TryDestroyBoneTransforms();
            TryCreateBoneTransforms();
        }

        private void TryCreateBoneTransforms()
        {
            if (_boneSelectors == null || _boneTransforms != null) return;

            var transforms = Enumerable.Range(0, _boneSelectors.Count)
                .Select(i => new GameObject($"bone_{i}").transform)
                .ToArray();

            _boneTransforms = new BoneTransformMB[_boneSelectors.Count];

            for (int i = 0; i < transforms.Length; i++)
            {
                _boneTransforms[i] = transforms[i].gameObject.AddComponent<BoneTransformMB>();
                _boneTransforms[i].SetBoneSelector(_boneSelectors[i]);
            }

            _visualizer = UnityEngineUtility.CreateGameObjectWithComponent<BoneVisualizerMB>();
            _visualizer.SetTransforms(transforms);
        }

        private void TryDestroyBoneTransforms()
        {
            if (_boneTransforms != null)
            {
                foreach (BoneTransformMB v in _boneTransforms)
                {
                    v.SetBoneSelector(null);
                }
                DestroyBoneTransforms(_boneTransforms);
                _boneTransforms = null;
            }

            if (_visualizer != null)
            {
                UnityEngine.Object.DestroyImmediate(_visualizer.gameObject);
            }
        }

        public static void ApplySkeletonPoses(Transform[] transforms, RuntimeBone[] bones, Matrix4x4 meshToWorld)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                var bone = bones[i];
                var parent = bone.parent;
                var tr = transforms[i];

                UpdateTransform(tr, bone.bindpose.inverse * meshToWorld);
                tr.SetParent(parent < 0 ? null : transforms[parent]);
            }
        }

        public static void DestroyBoneTransforms(BoneTransformMB[] boneTransforms)
        {
            for (int i = 0; i < boneTransforms.Length; i++)
            {
                var bt = boneTransforms[i];
                if (bt && bt.gameObject) UnityEngine.Object.DestroyImmediate(bt.gameObject);
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

            // Handle negative scale (reflection)
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
#endif