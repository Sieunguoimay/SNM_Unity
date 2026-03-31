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
            IInspectorTool[] tools,
            Action destroyCallback)
        {
            var extensionCoordinator = new InspectorExtensionCoordinator(extensions, tools);

            return new InspectorExtensionSystem(destroyCallback: () =>
            {
                extensionCoordinator.Dispose();
                destroyCallback?.Invoke();
            });
        }

        public static IEnumerable<IInspectorTool> GetDefaultToolsToInstall()
        {
            var historyTracker = new SelectionHistoryTracker();

            yield return new SimpleInspectorTool(
                location: InspectorExtensionLocation.Top,
                buildVEFunc: context => InspectorHeaderVECreator.BuildVE(context.InspectorWindow, historyTracker, context.Coordinator.RenderToLayout),
                disposeAction: historyTracker.Dispose);
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

            var tool = new SubAssetTool();
            yield return new SimpleInspectorExtension(
                location: InspectorExtensionLocation.Bottom,
                buildVEFunc: context => SubAssetExtensionVECreator.Create(tool, context.SerializedObject),
                supportedTypes: new[] { typeof(ScriptableObject) },
                unsupportedTypes: Array.Empty<Type>());

            yield return new SimpleInspectorExtension(
                location: InspectorExtensionLocation.Bottom,
                buildVEFunc: context => DependencyViewerVECreator.BuildVE(context.TargetObjects),
                supportedTypes: new[] { typeof(UnityEngine.Object) },
                unsupportedTypes: Array.Empty<Type>());
        }
    }
}
#endif