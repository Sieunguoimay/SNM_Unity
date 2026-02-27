#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace Snm.Tools.InspectorExtensions
{
    public interface IInspectorExtension
    {
        InspectorExtensionLocation Location { get; }
        IEnumerable<Type> SupportedTypes { get; }
        IEnumerable<Type> UnsupportedTypes { get; }
        IInspectorExtensionVEBuilder VEBuilder { get; }
    }
}
#endif