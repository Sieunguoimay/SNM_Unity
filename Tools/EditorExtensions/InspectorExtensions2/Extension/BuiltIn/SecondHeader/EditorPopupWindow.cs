#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public class EditorPopupWindow : EditorWindow
    {
        [SerializeField] private Object target;
        [SerializeField] private Editor editor;

        private static Object _target;

        public static void Open(Object target)
        {
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

            editor = Editor.CreateEditor(target);

            if (editor == null) return;

            rootVisualElement.Add(CreateVE(target, editor, this));
        }

        private static VisualElement CreateVE(Object target, Editor editor, EditorWindow window)
        {
            var root = new VisualElement();

            if (target == null) return root;

            var horizontal = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
            var scrollView = new ScrollView() { style = { flexGrow = 1 } };
            var secondHeaderVE = SecondHeaderVECreator.Create(editor.serializedObject);
            var space = new VisualElement() { style = { flexGrow = 1 } };
            var button_Select = new Button(() => Selection.activeObject = target) { text = "Select" };
            var button_Close = new Button(window.Close) { text = "Close" };

            var editorVE = new IMGUIContainer()
            {
                style = { marginLeft = 10f, marginRight = 5f },
                onGUIHandler = () =>
                {
                    if (editor == null || editor.serializedObject == null || editor.serializedObject.targetObject == null) return;
                    editor.OnInspectorGUI();
                }
            };

            horizontal.Add(space);
            horizontal.Add(button_Select);
            horizontal.Add(button_Close);
            scrollView.Add(secondHeaderVE);
            scrollView.Add(editorVE);
            root.Add(horizontal);
            root.Add(scrollView);
            return root;
        }
    }
}
#endif
