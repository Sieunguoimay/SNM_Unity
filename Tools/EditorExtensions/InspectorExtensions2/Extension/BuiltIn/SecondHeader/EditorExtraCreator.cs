using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public class EditorExtraCreator
    {
        public static VisualElement BuildVE(
            SerializedObject serializedObject, 
            VisualElement imguiContainer, 
            VisualElement layoutContainer = null)
        {
            var root = new VisualElement();
            var layout_Container = new VisualElement() { style = { marginLeft = 24 } };
            var toggled = false;
            Button button_Toggle = null;
            button_Toggle = new Button()
            {
                text = "Extra",
                clickable = new(() =>
                {
                    toggled = !toggled;

                    layout_Container.Clear();

                    if (toggled == true)
                    {
                        layout_Container.Add(CreateVE(serializedObject));
                    }
                    button_Toggle.text = toggled ? "Default" : "Extra";
                    imguiContainer.style.display = toggled ? DisplayStyle.None : DisplayStyle.Flex;
                })
            };

            root.Add(button_Toggle);
            (layoutContainer ?? root).Add(layout_Container);
            return root;
        }

        public static VisualElement CreateVE(SerializedObject serializedObject)
        {
            var root = new VisualElement();

            DrawSerializedObject(root, serializedObject);

            return root;
        }

        private static void DrawSerializedObject(VisualElement root, SerializedObject so)
        {
            so.Update();

            var iterator = so.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.propertyPath == "m_Script")
                {
                    var scriptField = new PropertyField(iterator.Copy());
                    scriptField.SetEnabled(false);
                    root.Add(scriptField);
                    continue;
                }

                DrawPropertyRecursive(root, iterator.Copy());
            }

            root.Bind(so);
        }

        private static void DrawPropertyRecursive(
            VisualElement root,
            SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference
            && property.objectReferenceValue != null)
            {
                root.Add(CreateObjectFieldWithButton(property.Copy()));
                return;
            }

            // If array (but not string)
            if (property.isArray && property.propertyType != SerializedPropertyType.String)
            {
                var foldout = new Foldout { text = property.displayName };

                root.Add(foldout);

                var sizeProp = property.FindPropertyRelative("Array.size");
                foldout.Add(new PropertyField(sizeProp));

                for (int i = 0; i < property.arraySize; i++)
                {
                    var element = property.GetArrayElementAtIndex(i);
                    DrawPropertyRecursive(foldout, element);
                }

                return;
            }

            // Normal property
            root.Add(new PropertyField(property.Copy()));
        }

        private static VisualElement CreateObjectFieldWithButton(SerializedProperty property)
        {
            var layout_Horizontal = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            var button_Open = new Button { text = "->", clickable = new(() => EditorPopupWindow.Open(property.objectReferenceValue)), style = { alignSelf = Align.FlexEnd} };
            var propertyField = new PropertyField(property) { style = { flexGrow = 1 } };
            layout_Horizontal.Add(propertyField);
            layout_Horizontal.Add(button_Open);

            return layout_Horizontal;
        }

    }
}