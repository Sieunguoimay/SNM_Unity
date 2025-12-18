using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Snm.GPUSkinning.BoneWeightTool
{
    public class BoneTransformTool
    {
        public static BoneTransformMB[] CreateBoneTransforms(Matrix4x4[] bindposes, Matrix4x4 meshToWorld)
        {
            return bindposes
                .Select((p, index) => CreateBoneTransform(p, index, meshToWorld))
                .ToArray();
        }

        public static void DestroyBoneTransforms(BoneTransformMB[] boneTransforms)
        {
            for (int i = 0; i < boneTransforms.Length; i++)
            {
                var bt = boneTransforms[i];
                UnityEngine.Object.DestroyImmediate(bt.gameObject);
            }
        }

        private static BoneTransformMB CreateBoneTransform(Matrix4x4 bindpose, int index, Matrix4x4 meshToWorld)
        {
            var transform = new GameObject($"Bone_{index}").AddComponent<BoneTransformMB>();
            var boneMatrix = bindpose.inverse * meshToWorld;

            UpdateTransform(transform.transform, boneMatrix);

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