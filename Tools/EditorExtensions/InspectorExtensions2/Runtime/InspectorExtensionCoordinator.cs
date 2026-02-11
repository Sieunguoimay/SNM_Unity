#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public class InspectorExtensionCoordinator : IDisposable
    {
        private readonly IInspectorExtension[] extensions;
        private readonly IInspectorTool[] inspectorTools;
        private EditorWindow[] _windows;
        private InspectorExtensionRenderer _extensionRenderer;
        private readonly InspectorLayoutInjector injector = new();
        private readonly InspectorWindowTracker windowTracker = new();

        public InspectorExtensionCoordinator(
            IInspectorExtension[] extensions,
            IInspectorTool[] inspectorTools)
        {
            this.extensions = extensions;
            this.inspectorTools = inspectorTools;

            windowTracker.OnInspectorWindowsChanged += WindowsTracker_OnInspectorWindowsChanged;
            Selection.selectionChanged += Selection_OnSelectionChanged;

            UpdateInspectorWindows();
            RenderToLayout();
        }

        public void Dispose()
        {
            _extensionRenderer?.ClearVEs();
            CleanupInspectorLayouts();

            Selection.selectionChanged -= Selection_OnSelectionChanged;
            windowTracker.OnInspectorWindowsChanged -= WindowsTracker_OnInspectorWindowsChanged;
        }

        private void Selection_OnSelectionChanged()
        {
            RenderToLayout();
        }

        private void WindowsTracker_OnInspectorWindowsChanged(InspectorWindowTracker tracker)
        {
            UpdateInspectorWindows();
            RenderToLayout();
        }

        public void UpdateInspectorWindows()
        {
            _windows = windowTracker.InspectorWindows.ToArray();
        }

        private void RenderToLayout()
        {
            _extensionRenderer?.ClearVEs();
            CleanupInspectorLayouts();

            var layouts = _windows.Select(injector.GetOrCreateLayout).ToArray();
            _extensionRenderer = new InspectorExtensionRenderer(layouts, new TypeBasedExtensionFilter());

            _extensionRenderer.ApplyExtensions(extensions);
            _extensionRenderer.ApplyTools(inspectorTools);
        }

        private void CleanupInspectorLayouts()
        {
            if (_windows != null)
            {
                foreach (var w in _windows)
                {
                    injector.CleanupInjectedLayout(w);
                }
            }
        }

    }
}
#endif