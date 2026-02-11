#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Snm.Tools.InspectorExtensions
{
    public class InspectorWindowTracker
    {
        private EditorWindow[] _inspectorWindows;

        public IReadOnlyList<EditorWindow> InspectorWindows => _inspectorWindows;
        public event Action<InspectorWindowTracker> OnInspectorWindowsChanged;

        public InspectorWindowTracker()
        {
            EditorApplication.delayCall += Editor_OnChanged;
            EditorApplication.playModeStateChanged += EditorApplication_OnPlayModeStateChanged;
            AssemblyReloadEvents.afterAssemblyReload += Editor_OnChanged;
            EditorApplication.projectChanged += Editor_OnChanged;
        }

        public void Dispose()
        {
            EditorApplication.delayCall -= Editor_OnChanged;
            EditorApplication.playModeStateChanged -= EditorApplication_OnPlayModeStateChanged;
            AssemblyReloadEvents.afterAssemblyReload -= Editor_OnChanged;
            EditorApplication.projectChanged -= Editor_OnChanged;
        }

        private void EditorApplication_OnPlayModeStateChanged(PlayModeStateChange change)
        {
            UpdateInspectorWindows();
        }

        private void Editor_OnChanged()
        {
            UpdateInspectorWindows();
        }

        private void UpdateInspectorWindows()
        {
            _inspectorWindows = GetInspectorWindows().ToArray();
            OnInspectorWindowsChanged?.Invoke(this);
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