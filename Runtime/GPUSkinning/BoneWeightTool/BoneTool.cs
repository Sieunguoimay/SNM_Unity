using System.Linq;
using Snm.Runtime.GPUSkinning;
using UnityEngine;

namespace Snm.GPUSkinning.BoneWeightTool
{
    public class BoneTool
    {
        private readonly RuntimeBoneCollection boneCollection;
        private readonly VerticesSelectionTool verticesSelector;
        private readonly BoneTransformsTool bindposesTool = new();
        private readonly BoneSelectionTool boneSelectionTool = new();

        public VerticesSelectionTool VerticesSelector => verticesSelector;
        public BoneTransformsTool BindposesTool => bindposesTool;
        public BoneSelectionTool BoneSelectionTool => boneSelectionTool;

        public BoneTool(
            RuntimeBoneCollection boneCollection,
            VerticesSelectionTool verticesSelector)
        {
            this.boneCollection = boneCollection;
            this.verticesSelector = verticesSelector;

            UpdateBoneSelectors();
        }

        private void UpdateBoneSelectors()
        {
            var bones = boneCollection.Bones;
            boneSelectionTool.UpdateBoneSelectors(
                bones,
                onSelect: ShowVerticesSelectorForBone,
                onUnselect: bone => HideVerticesSelector());

            var bindposes = bones.Select(b => b.bindpose).ToArray();
            bindposesTool.SetBindposes(bindposes, Matrix4x4.identity, boneSelectionTool.BoneSelectors);
        }

        private void ShowVerticesSelectorForBone(RuntimeBone bone)
        {
            HideVerticesSelector();

            verticesSelector.SetIsActive(true);
            foreach (var v in bone.vertices) verticesSelector.MarkVertexAsSelected(v.index);
            verticesSelector.SetDirtyCallback(() =>
            {
                bone.vertices = verticesSelector.SelectedVertices
                    .Select(v => new RuntimeVertex { index = v, boneWeight = 1 })
                    .ToList();
            });
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
            var bones = boneCollection.Bones
                .Append(new RuntimeBone()
                {
                    vertices = new(),
                    bindpose = Matrix4x4.identity
                })
                .ToArray();
            boneCollection.SetBones(bones);
            UpdateBoneSelectors();
        }

        public void UpdateBindposes()
        {
            var bindposes = bindposesTool.GetBindposes(Matrix4x4.identity);
            for (int i = 0; i < boneCollection.Bones.Count; i++)
            {
                var b = boneCollection.Bones[i];
                b.bindpose = bindposes[i];
            }
        }
    }
}