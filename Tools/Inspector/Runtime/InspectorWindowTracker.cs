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
        private EditorWindow[] _inspectorWindows = Array.Empty<EditorWindow>();

        public IReadOnlyList<EditorWindow> InspectorWindows => _inspectorWindows;
        public event Action<InspectorWindowTracker> OnInspectorWindowsChanged;

        public InspectorWindowTracker()
        {
            EditorApplication.delayCall += Editor_OnChanged;
            EditorApplication.playModeStateChanged += EditorApplication_OnPlayModeStateChanged;
            AssemblyReloadEvents.afterAssemblyReload += Editor_OnChanged;
            UpdateInspectorWindows();
        }

        public void Dispose()
        {
            EditorApplication.delayCall -= Editor_OnChanged;
            EditorApplication.playModeStateChanged -= EditorApplication_OnPlayModeStateChanged;
            AssemblyReloadEvents.afterAssemblyReload -= Editor_OnChanged;
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
            var next = GetInspectorWindows().ToArray();

            if (SameWindows(_inspectorWindows, next))
                return;

            _inspectorWindows = next;
            OnInspectorWindowsChanged?.Invoke(this);
        }

        private static bool SameWindows(EditorWindow[] a, EditorWindow[] b)
        {
            if (a == null) return b == null || b.Length == 0;
            if (b == null) return a.Length == 0;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
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