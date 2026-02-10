#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Snm.Tools.InspectorExtra;
using UnityEditor;
using UnityEngine;

namespace Snm.Tools.InspectorExtensions
{
    [InitializeOnLoad]
    public class InspectorExtensionSystemEntrypoint
    {
        private static readonly InspectorExtensionSystemControl _system;

        static InspectorExtensionSystemEntrypoint()
        {
            _system?.Destroyer.Destroy();

            var extensions = GetExtensionsToInstall().ToArray();

            _system = new InspectorExtensionSystemInstaller().Install(extensions);
        }

        private static IEnumerable<IInspectorExtension> GetExtensionsToInstall()
        {
            yield return new InspectorExtension(
                location: InspectorExtensionLocation.Bottom,
                buildVEFunc: context => ContextMenuListVEBuilder.BuildVE(context.Target),
                supportedTypes: new[] { typeof(MonoBehaviour), typeof(ScriptableObject) });

            yield return new InspectorExtension(
                location: InspectorExtensionLocation.Top,
                buildVEFunc: context => new EditorSecondHeaderVE(context.Target, context.InspectorWindow),
                supportedTypes: new[] { typeof(Object) });
        }
    }
}
#endif