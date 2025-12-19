using System.Collections.Generic;
using System.Linq;
using Snm.Runtime.GPUSkinning;
using UnityEngine;

namespace Snm.GPUSkinning.BoneWeightTool
{
    public class BoneTool
    {
        private RuntimeBone[] _bones;
        private readonly VerticesSelectionTool verticesSelector;
        private readonly BoneTransformsTool boneTransformsTool = new();
        private readonly BoneSelectionTool boneSelectionTool = new();

        public VerticesSelectionTool VerticesSelector => verticesSelector;
        public BoneTransformsTool BoneTransformsTool => boneTransformsTool;
        public BoneSelectionTool BoneSelectionTool => boneSelectionTool;

        public RuntimeBone[] Bones => _bones;

        public BoneTool(
            RuntimeBone[] bones,
            VerticesSelectionTool verticesSelector)
        {
            this.verticesSelector = verticesSelector;
            _bones = bones;

            UpdateBoneSelectors();
        }

        private void UpdateBoneSelectors()
        {
            boneSelectionTool.UpdateBoneSelectors(_bones.Length,
                onSelect: ShowVerticesSelectorForBone,
                onUnselect: bone => HideVerticesSelector());

            boneTransformsTool.SetBones(_bones, Matrix4x4.identity, boneSelectionTool.BoneSelectors);
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
            UpdateBindposes();
            var nbones = _bones
                .Append(new RuntimeBone()
                {
                    vertices = new(),
                    bindpose = Matrix4x4.identity,
                })
                .ToArray();
            _bones = nbones;
            UpdateBoneSelectors();
        }

        public void UpdateBindposes()
        {
            var bindposes = boneTransformsTool.GetBindposes(Matrix4x4.identity);
            SetBindposes(bindposes);
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