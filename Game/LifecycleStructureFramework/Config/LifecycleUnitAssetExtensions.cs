using System;
using System.Reflection;

namespace Snm.LifecycleStructureFramework
{
    public static class LifecycleUnitAssetExtensions
    {
        public static bool IsAssetFor(this LifecycleUnitAsset asset, Type targetType)
        {
            var att = asset.GetType().GetCustomAttribute<LifecycleUnitAssetForAttribute>();
            return att != null && att.LifecycleUnitType == targetType;
        }
    }
}