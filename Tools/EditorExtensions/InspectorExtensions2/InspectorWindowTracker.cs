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
        private readonly Type inspectorWindowType;
        private readonly HashSet<EditorWindow> inspectors = new();

        public event Action OnInspectorsChanged;

        public InspectorWindowTracker()
        {
            inspectorWindowType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
        }

        public void Setup()
        {

            Refresh();

            EditorApplication.delayCall += Refresh;
            EditorApplication.playModeStateChanged += _ => Refresh();
            AssemblyReloadEvents.afterAssemblyReload += Refresh;
            EditorApplication.projectChanged += Refresh;
        }

        public IReadOnlyCollection<EditorWindow> GetInspectors() => inspectors;

        private void Refresh()
        {
            var current = Resources
                .FindObjectsOfTypeAll(inspectorWindowType)
                .Cast<EditorWindow>()
                .Where(w => w != null);

            var currentSet = new HashSet<EditorWindow>(current);

            // Opened
            foreach (var w in currentSet)
            {
                inspectors.Add(w);
            }

            // Closed
            foreach (var w in inspectors.ToArray())
            {
                if (!currentSet.Contains(w))
                {
                    inspectors.Remove(w);
                }
            }

            OnInspectorsChanged?.Invoke();
        }

        internal void Teardown()
        {
            throw new NotImplementedException();
        }
    }
}
#endif