#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using static Snm.Tools.InspectorExtensions.InspectorLayoutInjector;

namespace Snm.Tools.InspectorExtensions
{
    public class EditorPopupWindow : EditorWindow
    {
        [SerializeField] private Object target;

        private static Object _target;

        public static void OpenFor(Object target)
        {
            if (target == null) return;

            var type = System.Type.GetType("UnityEditor.PropertyEditor, UnityEditor");
            var window = ScriptableObject.CreateInstance(type) as EditorWindow;
            window.Show();
            EditorApplication.delayCall += () =>
            {
                // Get tracker
                var trackerField = type.GetField("m_Tracker", BindingFlags.NonPublic | BindingFlags.Instance);
                var tracker = trackerField?.GetValue(window);

                if (tracker == null) return;

                // Call internal SetObjects via reflection
                var setObjectsMethod = tracker.GetType().GetMethod(
                    "SetObjects",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );

                setObjectsMethod?.Invoke(tracker, new object[] { new Object[] { target } });

                // Force rebuild
                var rebuildMethod = tracker.GetType().GetMethod(
                    "ForceRebuild",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                rebuildMethod?.Invoke(tracker, null);
            };
        }

        public static void Open(Object target)
        {
            OpenFor(target);
            return;
            _target = target;

            var foundWindow = Resources.FindObjectsOfTypeAll<EditorPopupWindow>()
                .FirstOrDefault(w => w.target == target);

            if (foundWindow != null)
            {
                foundWindow.Focus();
            }
            else
            {
                var window = CreateWindow<EditorPopupWindow>(typeof(EditorPopupWindow));
                window.titleContent = new GUIContent(target.name);
                window.Show();
            }
        }

        public void CreateGUI()
        {
            target = _target;

            if (target == null) return;

            var editor = Editor.CreateEditor(new[] { target });
            rootVisualElement.Add(CreateVE(target, editor, this));
        }

        private static VisualElement CreateVE(Object target, Editor editor, EditorWindow window)
        {
            var root = new VisualElement();

            if (target == null) return root;

            var horizontal = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
            var scrollView = new ScrollView() { style = { flexGrow = 1 } };
            var space = new VisualElement() { style = { flexGrow = 1 } };
            var button_Select = new Button(() => Selection.activeObject = target) { text = "Select" };
            var button_Close = new Button(window.Close) { text = "Close" };

            var editorVE = new InspectorElement(editor) { name = $"editor-{target.name}", style = { marginLeft = 10f, marginRight = 5f, flexGrow = 1 }, };

            horizontal.Add(space);
            horizontal.Add(button_Select);
            horizontal.Add(button_Close);
            scrollView.Add(editorVE);
            root.Add(horizontal);
            root.Add(scrollView);

            VisualElement top = new(), bottom = new(), left = new(), right = new();
            var zonesLifecycles = new List<AttachmentZonesLifecycle> { new(editorVE, top, bottom, left, right) };
            var editorLayouts = new[] { new EditorLayout(new(top, bottom, left, right), editor.targets, editor.serializedObject, editorVE) };

            var extensions = InspectorExtensionSystemInstaller.GetDefaultExtensionsToInstall().ToArray();
            var extensionRenderer = new InspectorExtensionRenderer(editorLayouts, new TypeBasedExtensionFilter());
            extensionRenderer.ApplyExtensions(extensions);

            root.RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                extensionRenderer.ClearVEs();
                foreach (var zonesAttachment in zonesLifecycles)
                    zonesAttachment.Cleanup();
            });
            return root;
        }
    }
}
#endif
