using System;
using System.Collections.Generic;
using Snm.Runtime.GPUSkinning;

namespace Snm.GPUSkinning.BoneWeightTool
{
    public class RuntimeBoneCollection
    {
        private RuntimeBone[] _bones;

        public IReadOnlyList<RuntimeBone> Bones => _bones;

        public event Action OnBonesChanged;

        public void SetBones(RuntimeBone[] bones)
        {
            _bones = bones;
            OnBonesChanged?.Invoke();
        }
    }
}