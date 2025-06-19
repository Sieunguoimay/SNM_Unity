using System;

namespace Snm.Framework.Reactive
{
    public class VariableBool : IBoolWithSet
    {
        private bool _value = false;

        public bool Value => _value;

        public event Action<IBool> OnValueChanged;
        
        public VariableBool(bool value)
        {
            _value = value;
        }

        public void SetValue(bool value)
        {
            _value = value;
            OnValueChanged?.Invoke(this);
        }
    }
}
