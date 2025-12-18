using UnityEditor;
using UnityEngine;

namespace Snm.GPUSkinning.BoneWeightTool
{
    public class BoneTransformMB : MonoBehaviour
    {
        private BoneSelector _boneSelector;

        private void OnEnable()
        {
            if (_boneSelector != null)
            {
                _boneSelector.OnIsSelectedChangedCallback += BonSelector_OnIsSelectedChangedCallback;
            }
        }

        private void OnDisable()
        {
            if (_boneSelector != null)
            {
                _boneSelector.OnIsSelectedChangedCallback -= BonSelector_OnIsSelectedChangedCallback;
            }
            var selected = Selection.activeObject;
            if (selected != gameObject)
            {
                Selection.activeObject = null;
            }
        }

        public Matrix4x4 GetLocalToWorldMatrix()
        {
            return transform.localToWorldMatrix;
        }

        public Matrix4x4 GetWorldToLocalMatrix()
        {
            return transform.worldToLocalMatrix;
        }

        public void SetBoneSelector(BoneSelector boneSelector)
        {
            if (_boneSelector != null)
            {
                _boneSelector.OnIsSelectedChangedCallback -= BonSelector_OnIsSelectedChangedCallback;
            }

            _boneSelector = boneSelector;

            if (_boneSelector != null)
            {
                _boneSelector.OnIsSelectedChangedCallback += BonSelector_OnIsSelectedChangedCallback;
                UpdateSelection();
            }
        }

        private void BonSelector_OnIsSelectedChangedCallback(BoneSelector selector)
        {
            UpdateSelection();
        }

        private void UpdateSelection()
        {
            if (_boneSelector.IsSelected)
            {
                var selected = Selection.activeObject;
                if (selected != gameObject)
                {
                    Selection.activeObject = gameObject;
                }
            }
        }

        public void Select()
        {
            _boneSelector?.Select();
        }

        public void Unselect()
        {
            _boneSelector?.Unselect();
        }

#if UNITY_EDITOR
        [UnityEditor.CustomEditor(typeof(BoneTransformMB))]
        private class _Editor : UnityEditor.Editor
        {
            private void OnEnable() => (target as BoneTransformMB).Select();
            private void OnDisable() => (target as BoneTransformMB).Unselect();
        }
#endif
    }
}