using System;

namespace Snm.Framework.System
{
    /// <summary>
    /// Optional attribute to specify the LifecycleUnit type that this LifecycleUnitAsset is for.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class StructureElementAssetForAttribute : Attribute
    {
        public Type ElementType { get; }
        public Type ReferenceType { get; }

        public StructureElementAssetForAttribute(Type configType)
            :this(configType, configType)
        {
            
        }

        public StructureElementAssetForAttribute(Type configType, Type referenceType)
        {
            ElementType = configType;
            ReferenceType = referenceType;
        }
    }
}