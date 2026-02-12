#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public sealed class SecondHeaderVECreator
    {
        public static VisualElement Create(SerializedObject serializedObject)
        {
            var target = serializedObject.targetObject;
            var assetPath = AssetDatabase.GetAssetPath(target);
            var debugMode = new InspectorMode(serializedObject);

            var root = new VisualElement();
            var layout_Buttons = new VisualElement()
            {
                style = {
                    flexDirection = FlexDirection.RowReverse,
                    flexGrow = 1,
                    marginBottom = 4,
                    backgroundColor = new Color(0f, 0f, 0f, .2f),
                    height = EditorGUIUtility.singleLineHeight + 2,
                }
            };

            var button_Browse = new Button { text = "Browse", clickable = new(() => SecondHeaderTools.OpenObjectBrowser(target)) };
            var button_EditScript = new Button() { text = "Edit Script", clickable = new(() => SecondHeaderTools.OpenScript(target)), };
            var button_Ping = new Button() { text = "Ping", clickable = new(() => EditorGUIUtility.PingObject(target)) };
            var button_Open = new Button() { text = "-> Window", clickable = new(() => EditorPopupWindow.Open(target)), tooltip = "Open in new Window", };
            var layout_Refs = new VisualElement();
            var button_ShowRefs = new Button() { text = "Show Refs", clickable = new(() => ShowRefs(serializedObject, layout_Refs)) };
            var button_Find = CreateFindReferences(target);
            var shouldShow_EditScript = target is MonoBehaviour || target is ScriptableObject;

            if (shouldShow_EditScript) layout_Buttons.Add(button_EditScript);
            layout_Buttons.Add(button_Ping);
            layout_Buttons.Add(button_Browse);
            layout_Buttons.Add(button_Open);
            layout_Buttons.Add(button_Find);
            layout_Buttons.Add(button_ShowRefs);

            root.Add(layout_Buttons);
            root.Add(layout_Refs);

            return root;
        }

        private static void ShowRefs(SerializedObject serializedObject, VisualElement root)
        {
            if (root.childCount > 0)
            {
                root.Clear();
                return;
            }

            foreach (var it in SerializeUtility.Iterate(serializedObject))
            {
                if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue != null)
                {
                    var obj = it.objectReferenceValue;
                    var layout_Horizontal = new VisualElement() { style = { flexDirection = FlexDirection.Row } };

                    layout_Horizontal.Add(new ObjectField() { value = obj, label = it.displayName, style = { flexGrow = 1 } });
                    layout_Horizontal.Add(new Button() { text = "-> Window", tooltip = "Open in new Window", clickable = new(() => EditorPopupWindow.Open(obj)) });
                    root.Add(layout_Horizontal);
                }
            }
        }

        private static VisualElement CreateFindReferences(Object target)
        {
            var button = new Button()
            {
                text = "Find",
                clickable = new(() =>
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Find in Scene"), false, () => SecondHeaderTools.OpenFindReferencesInScene(target));
                    menu.AddItem(new GUIContent("Find in Project"), false, () => SecondHeaderTools.FindRefrencesInProject(target));
                    menu.ShowAsContext();
                }),
#if UNITY_2023_2_OR_NEWER
                iconImage = Background.FromTexture2D((Texture2D)EditorGUIUtility.IconContent("d_Search Icon").image)
#endif
            };
#if UNITY_2023_2_OR_NEWER
            var image = button.Q<Image>();
            image.style.height = 17;
            image.style.width = 17;
#endif
            return button;
        }
    }
}
#endif
