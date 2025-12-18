using System;
using System.Collections.Generic;
using System.Linq;
using Snm.Runtime.GPUSkinning;
using UnityEditor;
using UnityEngine;

namespace Snm.GPUSkinning.BoneWeightTool
{
    public class BoneTool
    {
        private readonly RuntimeBoneCollection boneCollection;
        private readonly VerticesSelector verticesSelector;

        private BoneSelector[] _boneSelectors;
        private BoneTransformMB[] _boneTransforms;

        public IReadOnlyList<BoneSelector> BoneSelectors => _boneSelectors;
        public VerticesSelector VerticesSelector => verticesSelector;

        public event Action OnBoneSelectorsChanged;

        public BoneTool(RuntimeBoneCollection boneCollection, VerticesSelector verticesSelector)
        {
            this.boneCollection = boneCollection;
            this.verticesSelector = verticesSelector;

            UpdateBoneSelectors();
        }

        public void Cleanup()
        {
            if (_boneTransforms != null)
            {
                BoneTransformTool.DestroyBoneTransforms(_boneTransforms);
                _boneTransforms = null;
            }
            _boneSelectors = null;
        }

        private void UpdateBoneSelectors()
        {
            Cleanup();
            var bindposes = boneCollection.Bones.Select(b => b.bindpose).ToArray();

            _boneSelectors = GenerateBoneSelectors(boneCollection.Bones);
            _boneTransforms = BoneTransformTool.CreateBoneTransforms(bindposes, Matrix4x4.identity);

            for (int i = 0; i < _boneSelectors.Length; i++)
            {
                var bt = _boneTransforms[i];
                var boneSelector = _boneSelectors[i];
                bt.SetBoneSelector(boneSelector);
            }

            _boneSelectors.FirstOrDefault()?.Select();
            OnBoneSelectorsChanged?.Invoke();
        }

        public BoneSelector[] GenerateBoneSelectors(IReadOnlyList<RuntimeBone> bones)
        {
            return bones.Select((bone, index) => new BoneSelector(onSelected: () =>
            {
                var i = index;
                ClearSelection(i);
                verticesSelector.SetIsActive(true);
                verticesSelector.SetBoneModifier(new BoneModifier(bone));
            }, onUnselected: () =>
            {
                verticesSelector.SetIsActive(false);
                verticesSelector.SetBoneModifier(null);
            })).ToArray();
        }

        private void ClearSelection(int except)
        {
            if (_boneSelectors == null) return;
            for (int i = 0; i < _boneSelectors.Length; i++)
            {
                if (except == i) continue;
                var boneSelector = _boneSelectors[i];
                boneSelector.SetIsSelected(false);
            }
        }

        public void AssignBoneToVerticesSelector(RuntimeBone bone)
        {
        }

        public void AddNewBone()
        {
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

        public void UpdateBindposes(Matrix4x4 meshToWorld)
        {
            for (int i = 0; i < boneCollection.Bones.Count; i++)
            {
                var b = boneCollection.Bones[i];
                b.bindpose = _boneTransforms[i].GetWorldToLocalMatrix() * meshToWorld;
            }
        }
    }
}