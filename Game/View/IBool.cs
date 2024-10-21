using System;

namespace Views
{
    public interface IBool
    {
        bool Value { get; }
        event Action<IBool> OnValueChanged;
    }
}
