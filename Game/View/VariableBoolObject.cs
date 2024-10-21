using System;

namespace Views
{
    public class VariableBoolObject : IBoolWithSetter
    {
        private bool _value = false;

        public bool Value => _value;

        public event Action<IBool> OnValueChanged;
        
        public VariableBoolObject(bool value)
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
