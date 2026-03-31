#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static Snm.Tools.InspectorExtensions.InspectorLayoutInjector;

namespace Snm.Tools.InspectorExtensions
{
    public class EditorPopupWindow : EditorWindow
    {
        [SerializeField] private Object target;

        public static void Open(Object target)
        {
            if (target == null) return;

            var foundWindow = Resources.FindObjectsOfTypeAll<EditorPopupWindow>()
                .FirstOrDefault(w => w.target == target);

            if (foundWindow != null)
            {
                foundWindow.Focus();
            }
            else
            {
                var window = CreateWindow<EditorPopupWindow>(typeof(EditorPopupWindow));
                window.titleContent = new GUIContent($"{target.name} ({target.GetType().Name})");
                window.target = target;
                window.UpdateVE();
                window.Show();
            }
        }

        public void CreateGUI()
        {
            if (target == null) return;

            UpdateVE();
        }

        private void UpdateVE()
        {
            rootVisualElement.Clear();
            rootVisualElement.Add(CreateVE(target, this));
        }

        private static VisualElement CreateVE(Object target, EditorWindow window)
        {
            var root = new VisualElement();

            if (target == null) return root;

            var horizontal = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
            var scrollView = new ScrollView() { style = { flexGrow = 1 } };
            var space = new VisualElement() { style = { flexGrow = 1 } };
            var button_Select = new Button(() => Selection.activeObject = target) { text = "Select" };
            var button_Close = new Button(window.Close) { text = "Close" };

            var editor = Editor.CreateEditor(new[] { target });
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
            return root;
        }
    }
}
#endif
