#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public class InspectorWindowControllerManager
    {
        private readonly IInspectorExtension[] extensions;
        private readonly Func<EditorWindow, VisualElement> createHeaderVEFunc;
        private (InspectorWindowController, EditorWindow)[] _windowControllers;
        private EditorWindow[] _windows;
        private InspectorExtensionVEApplier _extensionVEApplier;
        private readonly InspectorWindowVERegistry registry = new();

        public InspectorWindowControllerManager(
            IInspectorExtension[] extensions,
            Func<EditorWindow, VisualElement> createHeaderVEFunc)
        {
            this.extensions = extensions;
            this.createHeaderVEFunc = createHeaderVEFunc;

            EditorApplication.delayCall += Refresh;
            EditorApplication.playModeStateChanged += EditorApplication_OnPlayModeStateChanged;
            AssemblyReloadEvents.afterAssemblyReload += Refresh;
            EditorApplication.projectChanged += Refresh;
            Selection.selectionChanged += Refresh;

            // UpdateControllerList();
            UpdateInspectorWindowVEs();
        }

        public void Cleanup()
        {
            // TeardownControllers();
            CleanupWindowVEs();

            EditorApplication.delayCall -= Refresh;
            EditorApplication.playModeStateChanged -= EditorApplication_OnPlayModeStateChanged;
            AssemblyReloadEvents.afterAssemblyReload -= Refresh;
            EditorApplication.projectChanged -= Refresh;
            Selection.selectionChanged -= Refresh;
        }

        private void EditorApplication_OnPlayModeStateChanged(PlayModeStateChange change)
        {
            Refresh();
        }

        public void Refresh()
        {
            // UpdateControllerList();
            UpdateInspectorWindowVEs();
        }

        public void UpdateInspectorWindowVEs()
        {
            CleanupWindowVEs();

            _windows = GetInspectorWindows().ToArray();

            var windowVEs = _windows.Select(registry.GetOrCreateWindowVE).ToArray();

            _extensionVEApplier = new InspectorExtensionVEApplier(windowVEs);
            _extensionVEApplier.ApplyExtensions(extensions);

            for (int i = 0; i < windowVEs.Length; i++)
            {
                InspectorWindowVE wVE = windowVEs[i];
                wVE.AttachmentZones.Top.Add(new Label($"{i} WTop"));
                wVE.AttachmentZones.Bottom.Add(new Label($"{i} WBottom"));
                wVE.AttachmentZones.Left.Add(new Label($"{i} WLeft"));
                wVE.AttachmentZones.Right.Add(new Label($"{i} WRight"));

                // foreach (var eVE in wVE.EditorVEs)
                // {
                //     eVE.AttachmentZones.Top.Add(new Label("ETop"));
                //     eVE.AttachmentZones.Bottom.Add(new Label("EBottom"));
                //     eVE.AttachmentZones.Left.Add(new Label("ELeft"));
                //     eVE.AttachmentZones.Right.Add(new Label("ERight"));
                // }
            }
        }

        private void CleanupWindowVEs()
        {
            _extensionVEApplier?.ClearExtensionVEs();

            if (_windows != null)
            {
                foreach (var w in _windows)
                {
                    registry.CleanupWindowVE(w);
                }
            }
        }

        private void UpdateControllerList()
        {
            TeardownControllers();

            var windows = GetInspectorWindows();
            _windowControllers = windows.Select(w => (new InspectorWindowController(w), w)).ToArray();

            SetupControllers();
        }

        private void SetupControllers()
        {
            if (_windowControllers != null)
            {
                foreach (var (wc, w) in _windowControllers)
                {
                    wc.ApplyExtensions(extensions);
                    wc.AssignWindowVEs(InspectorExtensionLocation.Right, new[] { createHeaderVEFunc(w) });
                }
            }
        }

        private void TeardownControllers()
        {
            if (_windowControllers != null)
            {
                foreach (var (wc, _) in _windowControllers)
                {
                    wc.ClearExtensions();
                }
            }
        }

        private static IEnumerable<EditorWindow> GetInspectorWindows()
        {
            const string UNITYEDITOR_INSPECTORWINDOW = "UnityEditor.InspectorWindow";

            return Resources.FindObjectsOfTypeAll(typeof(EditorWindow))
                .OfType<EditorWindow>()
                .Where(w => w.GetType().FullName == UNITYEDITOR_INSPECTORWINDOW);
        }
    }
}
#endif