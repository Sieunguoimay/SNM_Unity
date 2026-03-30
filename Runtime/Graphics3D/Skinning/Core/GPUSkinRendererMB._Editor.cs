#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Snm.Graphics3D.GPUSkinning
{
    public partial class GPUSkinRendererMB
    {
        [ContextMenu("Create Bone Transforms")]
        private void CreateBoneTransforms()
        {
            foreach (var bt in boneTransforms) bt.name += "_OBSOLETE";
            var hierarchy = skeleton != null
                ? skeleton.skeleton.bones.Select(b => b.parent).ToArray()
                : Array.Empty<int>();

            boneTransforms = CreateBoneHierarchy(
                mesh.bindposes,
                transform.localToWorldMatrix,
                hierarchy);

            EditorUtility.SetDirty(this);
            OnValidate();
        }

        private static Transform[] CreateBoneHierarchy(Matrix4x4[] bindposes, Matrix4x4 meshToWorld, int[] parents)
        {
            var transforms = new Transform[bindposes.Length];
            for (int i = 0; i < bindposes.Length; i++)
            {
                var mat = bindposes[i].inverse * meshToWorld;
                var tr = new GameObject($"bone_{i}").transform;
                tr.SetPositionAndRotation(
                    (Vector3)mat.GetColumn(3),
                    mat.rotation);
                tr.localScale = mat.lossyScale;
                transforms[i] = tr;
            }

            for (int i = 0; i < transforms.Length; i++)
            {
                var parent = i < parents.Length ? parents[i] : -1;
                if (parent >= 0)
                    transforms[i].SetParent(transforms[parent]);
            }
            return transforms;
        }
    }
}
#endif
