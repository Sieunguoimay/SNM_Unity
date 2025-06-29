using System;

namespace Snm.LifecycleStructureFramework
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class UnitReferenceAttribute : Attribute
    {


    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ExternalUnitReferenceAttribute : Attribute
    {
    }
}