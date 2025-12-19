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
            int boneCount,
            Action<int> onSelect,
            Action<int> onUnselect)
        {
            _boneSelectors = Enumerable.Range(0, boneCount)
                .Select(boneIndex => new BoneSelector(onSelected: () =>
                    {
                        ClearSelection(boneIndex);
                        onSelect(boneIndex);
                    }, onUnselected: () =>
                    {
                        onUnselect(boneIndex);
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