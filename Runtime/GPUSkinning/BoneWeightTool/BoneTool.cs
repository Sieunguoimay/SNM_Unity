using System.Collections.Generic;
using System.Linq;
using Snm.Runtime.GPUSkinning;
using Snm.Runtime.GPUSkinning.Serialize;
using UnityEngine;

namespace Snm.GPUSkinning.BoneWeightTool
{
    public class BoneTool
    {
        private RuntimeBone[] _bones;
        private readonly VerticesSelectionTool verticesSelector;
        private readonly BoneHierarchyTool hierarchyTool;
        private readonly BindposeTransformsTool bindposeTransformsTool = new();
        private readonly BoneSelectionTool boneSelectionTool = new();

        public VerticesSelectionTool VerticesSelector => verticesSelector;
        public BindposeTransformsTool BindposeTransformsTool => bindposeTransformsTool;
        public BoneSelectionTool BoneSelectionTool => boneSelectionTool;

        public RuntimeBone[] Bones => _bones;

        public BoneTool(
            RuntimeBone[] bones,
            VerticesSelectionTool verticesSelector,
            BoneHierarchyTool hierarchyTool)
        {
            this.verticesSelector = verticesSelector;
            this.hierarchyTool = hierarchyTool;
            _bones = bones;

            UpdateBoneTransforms();
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

        public void AddNewBone()
        {
            ReadFromBoneTransforms();
            _bones = _bones
                .Append(new RuntimeBone()
                {
                    vertices = new(),
                    bindpose = Matrix4x4.identity,
                })
                .ToArray();
            hierarchyTool.AddNew();
            UpdateBoneTransforms();
        }

        public void ReadFromBoneTransforms()
        {
            var hierarchy = BoneHierarchyTool.ExtractHierarchy(bindposeTransformsTool.BindposeTransforms.Select(bt => bt.transform).ToArray());
            hierarchyTool.SetParents(hierarchy);

            var bindposes = bindposeTransformsTool.GetBindposes(Matrix4x4.identity);
            SetBindposes(bindposes);
        }

        private void UpdateBoneTransforms()
        {
            boneSelectionTool.UpdateBoneSelectors(_bones.Length,
                onSelect: ShowVerticesSelectorForBone,
                onUnselect: bone => HideVerticesSelector());

            bindposeTransformsTool.SetBones(
                _bones,
                Matrix4x4.identity,
                boneSelectionTool.BoneSelectors);

            BoneHierarchyTool.ApplyHierarchy(
                hierarchyTool.Parents,
                bindposeTransformsTool.BindposeTransforms.Select(bt => bt.transform).ToArray());
        }

        public void SetBoneVertices(int boneIndex, IReadOnlyList<int> vertices)
        {
            var bone = _bones[boneIndex];
            bone.vertices = vertices
                .Select(v => new RuntimeVertex { index = v, boneWeight = 1 })
                .ToList();
        }

        public void SetBindposes(Matrix4x4[] bindposes)
        {
            for (int i = 0; i < _bones.Length; i++)
            {
                var b = _bones[i];
                b.bindpose = bindposes[i];
            }
        }
    }
}