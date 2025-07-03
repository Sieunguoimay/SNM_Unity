using System;

namespace Snm.SystemStructureFramework
{
    /// <summary>
    /// Optional attribute to specify the LifecycleUnit type that this LifecycleUnitAsset is for.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class StructureElementAssetForAttribute : Attribute
    {
        public Type ElementType { get; }

        public StructureElementAssetForAttribute(Type unitType)
        {
            ElementType = unitType;
        }
    }
}