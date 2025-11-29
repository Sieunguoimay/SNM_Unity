using System;

namespace Reflection
{
    [AttributeUsage(AttributeTargets.Field)]
    public class InjectFieldAttribute : Attribute { }
}
