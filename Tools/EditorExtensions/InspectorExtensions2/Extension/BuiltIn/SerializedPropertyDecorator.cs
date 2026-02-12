using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public class SerializedPropertyDecorator
    {
        private static readonly Dictionary<object, Dictionary<string, Rect>> objectDic = new();

        public static void Register(SerializedProperty property, Rect rect)
        {
            if (!objectDic.TryGetValue(property.serializedObject, out var rectDic))
            {
                objectDic.Add(property.serializedObject, rectDic = new());
            }

            if (!rectDic.TryGetValue(property.propertyPath, out var r))
            {
                rectDic.Add(property.propertyPath, rect);
            }

            rectDic[property.propertyPath] = rect;
        }

        public static VisualElement BuildVE(SerializedObject serializedObject)
        {
            var root = new VisualElement();
            var layout_Container = new VisualElement() { style = { marginLeft = 24 } };
            var button_Toggle = new Button() { text = "Toggle", clickable = new(() => { layout_Container.Clear(); layout_Container.Add(CreateVE(serializedObject)); }) };

            root.Add(button_Toggle);
            root.Add(layout_Container);
            return root;

            // var root = new VisualElement() { style = { width = 30 } };

            // // if (objectDic.TryGetValue(serializedObject, out var rectDic))
            // // {
            // var it = serializedObject.GetIterator();
            // it.Next(true);
            // var accY = 0f;
            // while (it.NextVisible(false))
            // {
            //     // if (rectDic.TryGetValue(it.propertyPath, out var rect) && rect != null)
            //     // {
            //     var height = EditorGUI.GetPropertyHeight(it, true);

            //     if (it.propertyType == SerializedPropertyType.ObjectReference)
            //     {
            //         var obj = it.objectReferenceValue;
            //         if (obj != null)
            //         {
            //             var button_ToWindow = new Button() { text = "->", tooltip = obj.name, clickable = new(() => EditorPopupWindow.Open(obj)), style = { position = Position.Absolute, top = accY } };
            //             root.Add(button_ToWindow);
            //         }
            //     }
            //     // }
            //     accY += height;
            // }
            // // }
            // return root;
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
            if (property.propertyType == SerializedPropertyType.ObjectReference)
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
            // Horizontal container
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;

            // Property field
            var propertyField = new PropertyField(property);
            propertyField.style.flexGrow = 1;

            // Button
            var button = new Button
            {
                clickable = new(() => { Debug.Log($"Clicked button for: {property.propertyPath}"); }),
                text = "->",
            };

            container.Add(propertyField);
            container.Add(button);

            return container;
        }

    }

    [CustomPropertyDrawer(typeof(UnityEngine.Object), true)]
    public class PropertyDrawer_Object : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PropertyField(position, property, label, true);
            EditorGUI.EndProperty();
            SerializedPropertyDecorator.Register(property, position);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, true);
        }
    }

    public static class IMGUIContainerInterceptor
    {
        public static void PatchIMGUIContainer(IMGUIContainer container, Editor editor)
        {
            var original = container.onGUIHandler;
            var rectPropertyMap = new Dictionary<Rect, SerializedProperty>();

            container.onGUIHandler = () =>
            {
                if (Event.current.type == EventType.Layout)
                {
                    // Rebuild rect->property map during layout pass
                    rectPropertyMap.Clear();
                    BuildRectPropertyMap(editor.serializedObject, rectPropertyMap);
                }

                original?.Invoke();

                if (Event.current.type != EventType.Repaint) return;

                var mousePos = Event.current.mousePosition;
                foreach (var kvp in rectPropertyMap)
                {
                    if (kvp.Key.Contains(mousePos))
                    {
                        Debug.Log($"Hovered property: {kvp.Value.propertyPath}");
                        break;
                    }
                }
            };
        }

        static void BuildRectPropertyMap(SerializedObject so, Dictionary<Rect, SerializedProperty> map)
        {
            var iterator = so.GetIterator();
            iterator.NextVisible(true); // skip script field

            while (iterator.NextVisible(false))
            {
                // GetPropertyHeight + a fake rect lets us ask Unity where it would draw
                float height = EditorGUI.GetPropertyHeight(iterator, true);
                // But we don't know the X/Y without actually drawing...
                // This approach breaks down here without a full layout pass
            }
        }
    }
}