using System;
using System.Linq;
using System.Reflection;

namespace Snm.Tools
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class VisualElementToolForAttribute : Attribute
    {
        public Type TargetType { get; }

        public VisualElementToolForAttribute(Type targetType)
        {
            TargetType = targetType;
        }

        public static Type TryGetToolVETypeFor(Type type)
        {
            return typeof(VisualElementToolForAttribute).Assembly.GetTypes()
                .FirstOrDefault(t =>
                    t.IsDefined(typeof(VisualElementToolForAttribute), false) &&
                    t.GetCustomAttribute<VisualElementToolForAttribute>().TargetType.IsAssignableFrom(type));
        }
    }
}
