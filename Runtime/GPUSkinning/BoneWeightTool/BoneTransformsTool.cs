using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Snm.GPUSkinning.BoneWeightTool
{
    public class BoneTransformsTool
    {
        private Matrix4x4[] _transformMatrices;
        private IReadOnlyList<BoneSelector> _boneSelectors;
        private BoneTransformMB[] _boneTransforms;

        public void Show()
        {
            TryCreateBoneTransforms();
        }

        public void Hide()
        {
            TryDestroyBoneTransforms();
        }

        public void SetBindposes(
            Matrix4x4[] bindposes,
            Matrix4x4 meshToWorld,
            IReadOnlyList<BoneSelector> boneSelectors)
        {
            _transformMatrices = bindposes.Select(b => b.inverse * meshToWorld).ToArray();
            _boneSelectors = boneSelectors;

            TryDestroyBoneTransforms();
            TryCreateBoneTransforms();
        }

        private void TryCreateBoneTransforms()
        {
            if (_transformMatrices == null) return;

            _boneTransforms = CreateBoneTransforms(_transformMatrices);

            for (int i = 0; i < _boneTransforms.Length; i++)
            {
                _boneTransforms[i].SetBoneSelector(_boneSelectors[i]);
            }
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
        }

        public Matrix4x4[] GetBindposes(Matrix4x4 meshToWorld)
        {
            return _boneTransforms
                .Select(bt => bt.GetWorldToLocalMatrix() * meshToWorld)
                .ToArray();
        }

        public static BoneTransformMB[] CreateBoneTransforms(Matrix4x4[] transformMatrices)
        {
            return transformMatrices
                .Select((p, index) => CreateBoneTransform(p, index))
                .ToArray();
        }

        public static void DestroyBoneTransforms(BoneTransformMB[] boneTransforms)
        {
            for (int i = 0; i < boneTransforms.Length; i++)
            {
                var bt = boneTransforms[i];
                Object.DestroyImmediate(bt.gameObject);
            }
        }

        private static BoneTransformMB CreateBoneTransform(Matrix4x4 transformMatrix, int index)
        {
            var transform = new GameObject($"Bone_{index}").AddComponent<BoneTransformMB>();

            UpdateTransform(transform.transform, transformMatrix);

            return transform;
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