#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;

namespace Snm.Tools.InspectorExtensions
{
    public class InspectorExtensionCoordinator : IDisposable
    {
        private readonly IInspectorExtension[] extensions;
        private readonly IInspectorTool[] inspectorTools;
        private EditorWindow[] _windows;
        private InspectorExtensionRenderer _extensionRenderer;
        private InspectorToolRenderer _toolRenderer;
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
            _toolRenderer?.ClearVEs();
            CleanupInspectorLayouts();

            Selection.selectionChanged -= Selection_OnSelectionChanged;
            windowTracker.OnInspectorWindowsChanged -= WindowsTracker_OnInspectorWindowsChanged;
            windowTracker.Dispose();
        }

        private void Selection_OnSelectionChanged()
        {
            EditorApplication.delayCall += () =>
                EditorApplication.delayCall += RenderToLayout;
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

        public void RenderToLayout()
        {
            _extensionRenderer?.ClearVEs();
            _toolRenderer?.ClearVEs();

            CleanupInspectorLayouts();
            try
            {
                var layouts = _windows.Select(injector.GetOrCreateLayout).ToArray();
                var editorLayouts = layouts.SelectMany(l => l.EditorLayouts).ToArray();

                _extensionRenderer = new InspectorExtensionRenderer(editorLayouts, new TypeBasedExtensionFilter());
                _toolRenderer = new InspectorToolRenderer(layouts);

                _extensionRenderer.ApplyExtensions(extensions);
                _toolRenderer.ApplyTools(inspectorTools, this);
            }
            catch { }
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