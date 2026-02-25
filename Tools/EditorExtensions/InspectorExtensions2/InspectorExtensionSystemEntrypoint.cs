#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Snm.Tools.InspectorExtensions
{
    [InitializeOnLoad]
    public class InspectorExtensionSystemEntrypoint
    {
        private static InspectorExtensionSystem _system;

        private static bool SystemEnabled
        {
            get => PlayerPrefs.GetInt("Snm.Tools.InspectorExtensions.Enabled", 0) == 1;
            set => PlayerPrefs.SetInt("Snm.Tools.InspectorExtensions.Enabled", value ? 1 : 0);
        }

        static InspectorExtensionSystemEntrypoint()
        {
            TryInstall();
        }

        private static void TryInstall()
        {
            _system?.Dispose();

            if (SystemEnabled)
            {
                var extensions = InspectorExtensionSystemInstaller.GetDefaultExtensionsToInstall().ToArray();
                var tools = InspectorExtensionSystemInstaller.GetDefaultToolsToInstall().ToArray();

                _system = new InspectorExtensionSystemInstaller().Install(extensions, tools);
            }

            UpdateCheck();
        }

        [MenuItem("Tools/Snm/Toggle Inspector Extension")]
        private static void ToggleSystem()
        {
            SystemEnabled = !SystemEnabled;
            TryInstall();
        }

        private static void UpdateCheck()
        {
            Menu.SetChecked("Tools/Snm/Toggle Inspector Extension", SystemEnabled);
        }
    }
}
#endif