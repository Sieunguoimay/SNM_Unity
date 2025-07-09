#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Collections;

namespace Snm.Tools.InspectorExtra
{
    public class InspectorExtensionInstaller
    {
        private static InspectorExtensionInstaller _instance;
        public static InspectorExtensionInstaller Instance => _instance ??= new();

        private readonly List<InspectorExtensionManager> inspectorExtensionManagers = new();
        private readonly List<IInspectorExtension> _inspectorExtensions = new();

        public bool DebugEnabled
        {
            get => EditorPrefs.GetBool("InspectorExtensionInstaller_DebugEnabled", false);
            set => EditorPrefs.SetBool("InspectorExtensionInstaller_DebugEnabled", value);
        }

        public IReadOnlyList<IInspectorExtension> InspectorExtensions => _inspectorExtensions;

        private InspectorExtensionInstaller()
        {
            if (DebugEnabled)
            {
                Debug.Log($"InspectorExtensionInstaller {GetHashCode()} created");
            }
        }

        ~InspectorExtensionInstaller()
        {
            TryTearDownExtensionManager();

            if (DebugEnabled)
            {
                Debug.Log($"InspectorExtensionInstaller {GetHashCode()} destroyed");
            }
        }

        public void InjectExtensions(params IInspectorExtension[] extensions)
        {
            _inspectorExtensions.Clear();
            _inspectorExtensions.AddRange(extensions);
        }

        public void Setup()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;

            EditorApplication.playModeStateChanged -= OnEditorPlaymodeChanged;
            EditorApplication.playModeStateChanged += OnEditorPlaymodeChanged;

            EditorApplication.delayCall -= OnOneFrameAfterLoad;
            EditorApplication.delayCall += OnOneFrameAfterLoad;

            if (DebugEnabled)
            {
                Debug.Log($"InspectorExtensionInstaller {GetHashCode()} Setup");
            }
        }

        public void Teardown()
        {
            TryTearDownExtensionManager();
            if (DebugEnabled)
            {
                Debug.Log($"InspectorExtensionInstaller {GetHashCode()} Teardown");
            }
        }

        private void OnOneFrameAfterLoad()
        {
            TryModify();
        }

        private void OnEditorPlaymodeChanged(PlayModeStateChange obj)
        {
            TryModify();
        }

        private void OnSelectionChanged()
        {
            TryModify();
        }

        private void TryModify()
        {
            TryTearDownExtensionManager();

            foreach (var w in GetInspectorWindows())
            {
                w.rootVisualElement.UnregisterCallback<DetachFromPanelEvent>(OnRootVisualElementDetached);
                w.rootVisualElement.RegisterCallback<DetachFromPanelEvent>(OnRootVisualElementDetached);

                var inspectorExtensionManager = new InspectorExtensionManager(w, _inspectorExtensions);
                inspectorExtensionManager.SetupExtensions();
                inspectorExtensionManagers.Add(inspectorExtensionManager);
            }
        }

        private void OnRootVisualElementDetached(DetachFromPanelEvent evt)
        {
            if (evt.target is VisualElement visualElement)
            {
                visualElement.UnregisterCallback<DetachFromPanelEvent>(OnRootVisualElementDetached);
                var found = inspectorExtensionManagers.FirstOrDefault(iem => iem.InspectorWindow.rootVisualElement == visualElement);

                if (found != null)
                {
                    found.TeardownExtensions();
                    inspectorExtensionManagers.Remove(found);
                }
            }
        }

        private IEnumerator WaitForNextFrame(Action callback)
        {
            yield return new WaitForEndOfFrame();
            callback?.Invoke();
        }

        private void TryTearDownExtensionManager()
        {
            foreach (var m in inspectorExtensionManagers)
            {
                m.TeardownExtensions();
            }
            inspectorExtensionManagers.Clear();
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