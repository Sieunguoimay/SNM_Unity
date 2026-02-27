#if UNITY_EDITOR
using System;

namespace Snm.Tools.InspectorExtensions
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class RevealNonSerializedAttribute : Attribute { }
}
#endif