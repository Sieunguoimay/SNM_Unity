using System;
using System.Collections.Generic;
using System.Linq;
using Snm.Runtime.GPUSkinning;
using UnityEditor;

namespace Snm.GPUSkinning.BoneWeightTool
{
    public class BoneSelectionTool
    {
        private BoneSelector[] _boneSelectors;

        public IReadOnlyList<BoneSelector> BoneSelectors => _boneSelectors;
        public event Action OnBoneSelectorsChanged;

        public void UpdateBoneSelectors(
            IReadOnlyList<RuntimeBone> bones,
            Action<RuntimeBone> onSelect,
            Action<RuntimeBone> onUnselect)
        {
            _boneSelectors = bones
                .Select((bone, index) => new BoneSelector(onSelected: () =>
                    {
                        ClearSelection(index);
                        onSelect(bone);
                    }, onUnselected: () =>
                    {
                        onUnselect(bone);
                    }))
                .ToArray();
            _boneSelectors.FirstOrDefault()?.Select();
            OnBoneSelectorsChanged?.Invoke();
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
    }
}