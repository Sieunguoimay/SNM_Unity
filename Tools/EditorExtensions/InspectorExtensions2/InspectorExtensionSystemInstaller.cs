#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Snm.Tools.InspectorExtensions
{
    public class InspectorExtensionSystemDestroyer
    {
        private readonly Action destroyCallback;

        public InspectorExtensionSystemDestroyer(Action destroyCallback)
        {
            this.destroyCallback = destroyCallback;
        }

        public void Destroy() { destroyCallback?.Invoke(); }
    }

    public class InspectorExtensionSystemControl
    {
        public InspectorExtensionSystemDestroyer Destroyer { get; }

        public InspectorExtensionSystemControl(
            InspectorExtensionSystemDestroyer destroyer)
        {
            Destroyer = destroyer;
        }
    }

    public class InspectorExtensionSystemInstaller
    {
        public InspectorExtensionSystemControl Install(IReadOnlyList<InspectorExtension> extensions)
        {
            var inspectorWindowTracker = new InspectorWindowTracker();
            var controllerManager = new InspectorWindowControllerManager(inspectorWindowTracker, extensions);

            inspectorWindowTracker.Setup();

            var destroyer = new InspectorExtensionSystemDestroyer(destroyCallback: () =>
            {
                controllerManager.Cleanup();
                inspectorWindowTracker.Teardown();
            });

            return new(destroyer);
        }
    }

    public class InspectorWindowControllerManager
    {
        private readonly InspectorWindowTracker tracker;
        private readonly IReadOnlyList<InspectorExtension> extensions;
        private InspectorWindowController[] _windowControllers;

        public InspectorWindowControllerManager(
            InspectorWindowTracker tracker,
            IReadOnlyList<InspectorExtension> extensions)
        {
            this.tracker = tracker;
            this.extensions = extensions;

            tracker.OnInspectorsChanged += InspectorWindowTracker_OnInspectorChanged;
            Selection.selectionChanged += Selection_OnSelectionChanged;
            UpdateControllerList();
        }

        public void Cleanup()
        {
            Selection.selectionChanged -= Selection_OnSelectionChanged;
            tracker.OnInspectorsChanged -= InspectorWindowTracker_OnInspectorChanged;
            TeardownControllers();
        }

        private void Selection_OnSelectionChanged()
        {
            UpdateControllerList();
        }

        private void InspectorWindowTracker_OnInspectorChanged()
        {
            UpdateControllerList();
        }

        private void UpdateControllerList()
        {
            TeardownControllers();
            _windowControllers = CreateInspectorWindowControllers(tracker.GetInspectors()).ToArray();
            SetupControllers();
        }

        private void SetupControllers()
        {
            if (_windowControllers != null)
            {
                foreach (var wc in _windowControllers)
                {
                    wc.ApplyExtensions(extensions);
                }
            }
        }

        private void TeardownControllers()
        {
            if (_windowControllers != null)
            {
                foreach (var wc in _windowControllers)
                {
                    wc.ClearExtensions();
                }
            }
        }

        private static IEnumerable<InspectorWindowController> CreateInspectorWindowControllers(IEnumerable<EditorWindow> inspectorWindows)
        {
            return inspectorWindows.Select(w => new InspectorWindowController(w));
        }
    }
}
#endif