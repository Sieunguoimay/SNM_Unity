#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Snm.Tools.InspectorExtensions
{
    public class InspectorExtensionSystemInstaller
    {
        public InspectorExtensionSystem Install(
            IInspectorExtension[] extensions,
            IInspectorTool[] tools)
        {
            var extensionCoordinator = new InspectorExtensionCoordinator(extensions, tools);

            return new InspectorExtensionSystem(destroyCallback: () =>
            {
                extensionCoordinator.Dispose();
            });
        }

        public static IEnumerable<IInspectorTool> GetDefaultToolsToInstall()
        {
            yield return new SimpleInspectorTool(
                location: InspectorExtensionLocation.Top,
                buildVEFunc: context => InspectorHeaderVECreator.BuildVE(context.InspectorWindow, context.Coordinator.RenderToLayout));
        }

        public static IEnumerable<IInspectorExtension> GetDefaultExtensionsToInstall()
        {
            yield return new SimpleInspectorExtension(
                location: InspectorExtensionLocation.Bottom,
                buildVEFunc: context => ContextMenuListVEBuilder.BuildVE(context.TargetObjects.FirstOrDefault()),
                supportedTypes: new[] { typeof(MonoBehaviour), typeof(ScriptableObject) },
                unsupportedTypes: Array.Empty<Type>());

            yield return new SimpleInspectorExtension(
                location: InspectorExtensionLocation.Top,
                buildVEFunc: context => SecondHeaderVECreator.Create(context.SerializedObject, context.InspectorElement),
                supportedTypes: new[] { typeof(UnityEngine.Object) },
                unsupportedTypes: Array.Empty<Type>());
        }
    }
}
#endif