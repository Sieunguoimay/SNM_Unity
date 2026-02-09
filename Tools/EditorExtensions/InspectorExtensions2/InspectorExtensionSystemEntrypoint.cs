#if UNITY_EDITOR
using UnityEditor;

namespace Snm.Tools.InspectorExtensions
{
    [InitializeOnLoad]
    public class InspectorExtensionSystemEntrypoint
    {
        static InspectorExtensionSystemEntrypoint()
        {
            var extensions = new InspectorExtension[] { new ContextMenuInspectorExtension()};

            var system = new InspectorExtensionSystemInstaller().Install(extensions);

            //when should i destroy the system??
        }
    }
}
#endif