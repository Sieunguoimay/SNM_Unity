#if UNITY_EDITOR
using System.Linq;
using UnityEditor;

namespace Snm.Tools.InspectorExtensions
{
    [InitializeOnLoad]
    public class InspectorExtensionSystemEntrypoint
    {
        private static readonly InspectorExtensionSystem _system;

        static InspectorExtensionSystemEntrypoint()
        {
            _system?.Dispose();

            var extensions = InspectorExtensionSystemInstaller.GetDefaultExtensionsToInstall().ToArray();
            var tools = InspectorExtensionSystemInstaller.GetDefaultToolsToInstall().ToArray();

            _system = new InspectorExtensionSystemInstaller().Install(extensions, tools);
        }

    }
}
#endif