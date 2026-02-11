#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
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
            var window = GetWindow<EditorPopupWindow>();
            window.ShowPopup();
        }

        public void CreateGUI()
        {
            target = _target;
            if (target == null) return;
            editor = Editor.CreateEditor(target);
            if (editor == null) return;

            titleContent = new GUIContent(target.name);

            rootVisualElement.Add(CreateVE(target, editor));
        }

        private static VisualElement CreateVE(Object target, Editor editor)
        {
            var root = new VisualElement();

            if (target == null) return root;

            VisualElement horizontal;
            root.Add(horizontal = new());
            horizontal.style.flexDirection = FlexDirection.Row;

            ScrollView scrollView;
            root.Add(scrollView = new());
            scrollView.style.flexGrow = 1;

            scrollView.Add(SecondHeaderVECreator.Create(editor.serializedObject));

            VisualElement space;
            horizontal.Add(space = new());
            space.style.flexGrow = 1;

            horizontal.Add(new Button(() =>
            {
                Selection.activeObject = target;
            })
            { text = "Select" });

            IMGUIContainer editorVE;
            scrollView.Add(editorVE = new IMGUIContainer(() =>
            {
                if (editor == null || editor.serializedObject == null || editor.serializedObject.targetObject == null) return;
                editor.OnInspectorGUI();
            }));

            editorVE.style.marginLeft = 10f;
            editorVE.style.marginRight = 5f;
            return root;
        }
    }
}
#endif
