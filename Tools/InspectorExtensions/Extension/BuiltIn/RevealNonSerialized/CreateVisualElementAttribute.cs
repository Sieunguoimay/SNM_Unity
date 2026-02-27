#if UNITY_EDITOR
using System;

namespace Snm.Tools.InspectorExtensions
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CreateVisualElementAttribute : Attribute { }
}
#endif