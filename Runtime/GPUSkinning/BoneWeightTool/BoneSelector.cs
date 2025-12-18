using System;

namespace Snm.GPUSkinning.BoneWeightTool
{
    public class BoneSelector
    {
        private readonly Action onSelected;
        private readonly Action onUnselected;
        private bool _isSelected;
        private Action<BoneSelector> _onIsSelectedChangedCallback;

        public bool IsSelected => _isSelected;
        public event Action<BoneSelector> OnIsSelectedChangedCallback
        {
            add => _onIsSelectedChangedCallback += value;
            remove => _onIsSelectedChangedCallback -= value;
        }

        public BoneSelector(
            Action onSelected,
            Action onUnselected)
        {
            this.onSelected = onSelected;
            this.onUnselected = onUnselected;
        }

        public void Select()
        {
            SetIsSelected(true);
            onSelected?.Invoke();
        }

        public void Unselect()
        {
            SetIsSelected(false);
            onUnselected?.Invoke();
        }

        public void SetIsSelected(bool isSelected)
        {
            if (_isSelected != isSelected)
            {
                _isSelected = isSelected;
                _onIsSelectedChangedCallback?.Invoke(this);
            }
        }
    }
}