using System;

namespace Snm.LifecycleStructureFramework
{
    /// <summary>
    /// Optional attribute to specify the LifecycleUnit type that this LifecycleUnitAsset is for.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class LifecycleUnitAssetForAttribute : Attribute
    {
        public Type LifecycleUnitType { get; }

        public LifecycleUnitAssetForAttribute(Type unitType)
        {
            LifecycleUnitType = unitType;
        }
    }
}