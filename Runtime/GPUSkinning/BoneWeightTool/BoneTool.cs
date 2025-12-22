using System;
using System.Collections.Generic;
using System.Linq;
using Snm.Runtime.GPUSkinning;
using UnityEngine;

namespace Snm.GPUSkinning.BoneWeightTool
{
    public enum BoneToolMode
    {
        BoneCreator,
        WeightPainter
    }

    public class BoneTool
    {
        private RuntimeBone[] _bones = new RuntimeBone[0];
        private readonly VerticesSelectionTool verticesSelector = new();
        private readonly BoneTransformsTool boneTransformsTool = new();
        private readonly BoneSelectionTool boneSelectionTool = new();

        public VerticesSelectionTool VerticesSelector => verticesSelector;
        public BoneTransformsTool BoneTransformsTool => boneTransformsTool;
        public BoneSelectionTool BoneSelectionTool => boneSelectionTool;

        public RuntimeBone[] Bones => _bones;

        public void SetRuntimeBones(RuntimeBone[] bones)
        {
            _bones = bones;

            boneSelectionTool.UpdateBoneSelectors(_bones.Length,
                onSelect: ShowVerticesSelectorForBone,
                onUnselect: bone => HideVerticesSelector());

            boneTransformsTool.SetBoneSelectors(boneSelectionTool.BoneSelectors);
            var transforms = boneTransformsTool.BoneTransforms.Select(bt => bt.transform).ToArray();
            BoneTransformsTool.ApplySkeletonPoses(transforms, _bones, Matrix4x4.identity);
        }

        public void SetBoneVertices(int boneIndex, IReadOnlyList<int> vertices)
        {
            var bone = _bones[boneIndex];
            bone.vertices = vertices
                .Select(v => new RuntimeVertex { index = v, boneWeight = 1 })
                .ToList();
        }

        public void AddNew()
        {
            SetRuntimeBones(_bones.Append(new RuntimeBone { bindpose = Matrix4x4.identity, parent = -1, vertices = new() }).ToArray());
        }

        public void DeleteBone(int boneIndex)
        {
            var bones = new List<RuntimeBone>();

            var parentDic = _bones.ToDictionary(b => b, b => b.parent < 0 ? null : _bones[b.parent]);

            for (int i = 0; i < _bones.Length; i++)
            {
                if (boneIndex == i) continue;
                RuntimeBone b = _bones[i];
                bones.Add(b);
            }

            foreach (var b in bones)
            {
                var parent = parentDic[b];
                b.parent = parent == null ? -1 : bones.IndexOf(parent);
            }

            SetRuntimeBones(bones.ToArray());
        }

        public void ClearBoneVertices(int boneIndex)
        {
            SetBoneVertices(boneIndex, Array.Empty<int>());

            ShowVerticesSelectorForBone(boneIndex);
        }

        private void ShowVerticesSelectorForBone(int boneIndex)
        {
            var bone = _bones[boneIndex];

            HideVerticesSelector();

            verticesSelector.SetIsActive(true);

            foreach (var v in bone.vertices) verticesSelector.MarkVertexAsSelected(v.index);

            verticesSelector.SetDirtyCallback(() => SetBoneVertices(boneIndex, verticesSelector.SelectedVertices));
        }

        public void HideVerticesSelector()
        {
            verticesSelector.SetDirtyCallback(null);
            verticesSelector.SetIsActive(false);
            verticesSelector.ClearMarks();
        }

        public void UpdateSkeletonWithBoneTransforms(Matrix4x4 meshToWorld)
        {
            var transforms = boneTransformsTool.BoneTransforms.Select(bt => bt.transform).ToArray();
            for (int i = 0; i < _bones.Length; i++)
            {
                RuntimeBone bone = _bones[i];
                var boneTransform = transforms[i];

                bone.bindpose = boneTransform.transform.worldToLocalMatrix * meshToWorld;
                bone.parent = Array.IndexOf(transforms, boneTransform.parent);
            }
        }
    }
}