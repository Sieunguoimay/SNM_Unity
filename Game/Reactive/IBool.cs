using System;

namespace Snm.Framework.Reactive
{
    public interface IBool
    {
        bool Value { get; }
        event Action<IBool> OnValueChanged;
    }
}
