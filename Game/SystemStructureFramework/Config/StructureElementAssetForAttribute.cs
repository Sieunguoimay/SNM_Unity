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
        public Type ConfigType { get; }

        public StructureElementAssetForAttribute(Type type)
            : this(type, type)
        {
        }

        public StructureElementAssetForAttribute(Type configType, Type elementType)
        {
            ElementType = elementType;
            ConfigType = configType;
        }
    }
}