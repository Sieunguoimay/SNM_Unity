#if UNITY_EDITOR
using System.Linq;

namespace Snm.Tools.InspectorExtensions
{
    public class TypeBasedExtensionFilter : IInspectorExtensionFilter
    {
        public bool IsMatch(
            IInspectorExtension extension,
            InspectorExtensionContext context)
        {
            var supported = false;

            foreach (var to in context.TargetObjects)
            {
                var isUnsupported = false;
                foreach (var t in extension.UnsupportedTypes)
                {
                    if (t.IsInstanceOfType(to))
                    {
                        isUnsupported = true;
                        break;
                    }
                }
                if (isUnsupported) break;

                supported = IsExtensionSupportedForTarget(extension, to);

                if (supported) break;
            }

            return supported;
        }

        private static bool IsExtensionSupportedForTarget(IInspectorExtension ext, object target)
        {
            if (ext.UnsupportedTypes.Any(t => t.IsInstanceOfType(target)))
                return false;

            return ext.SupportedTypes.Any(t => t.IsInstanceOfType(target));
        }
    }
}
#endif