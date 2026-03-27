#if UNITY_EDITOR
using System;
using System.Linq;
using Snm.GPUSkinning.BoneWeightTool;
using UnityEditor;
using UnityEngine;

namespace Snm.Runtime.GPUSkinning
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

            boneTransforms = BoneTransformsTool.CreateBoneHierarchy(
                mesh.bindposes,
                transform.localToWorldMatrix,
                hierarchy);

            EditorUtility.SetDirty(this);
            OnValidate();
        }
    }
}
#endif
