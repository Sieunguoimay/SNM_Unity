#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CustomPropertyReferenceAttribute : Attribute
    {
        public static List<TBase> CreateAllWithAttribute<TAttribute, TBase>()
            where TAttribute : Attribute
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .Where(t =>
                    typeof(TBase).IsAssignableFrom(t) &&
                    t.IsClass &&
                    !t.IsAbstract &&
                    t.GetCustomAttribute<TAttribute>() != null)
                .Select(t => (TBase)Activator.CreateInstance(t))
                .ToList();
        }

        public static List<object> CreateAllWithAttribute<TAttribute>()
            where TAttribute : Attribute
        {
            var assembly = Assembly.GetExecutingAssembly();

            var types = assembly.GetTypes()
                .Where(t =>
                    t.IsClass &&
                    !t.IsAbstract &&
                    t.GetCustomAttribute<TAttribute>() != null);

            var instances = new List<object>();

            foreach (var type in types)
            {
                var instance = Activator.CreateInstance(type);
                instances.Add(instance);
            }

            return instances;
        }

        public static List<object> CreateAllWithAttributeFromAllAssemblies<TAttribute>()
            where TAttribute : Attribute
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var types = assemblies
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .Where(t =>
                    t.IsClass &&
                    !t.IsAbstract &&
                    t.GetCustomAttribute<TAttribute>() != null);

            var instances = new List<object>();

            foreach (var type in types)
            {
                var instance = Activator.CreateInstance(type);
                instances.Add(instance);
            }

            return instances;
        }
    }

    public interface ICustomPropertyReference
    {
        bool Supports(SerializedProperty property);
        void HandleClick(SerializedProperty property);
    }

    [CustomPropertyReference]
    public class CustomPropertyReference_Object : ICustomPropertyReference
    {
        public bool Supports(SerializedProperty property)
        {
            return property.propertyType == SerializedPropertyType.ObjectReference
                && property.objectReferenceValue != null;
        }

        public void HandleClick(SerializedProperty property)
        {
            EditorPopupWindow.Open(property.objectReferenceValue);
        }
    }

    public class CustomEditorVECreator
    {
        private static List<ICustomPropertyReference> _references;

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
                text = "Custom",
                clickable = new(() =>
                {
                    toggled = !toggled;

                    layout_Container.Clear();

                    if (toggled == true)
                    {
                        layout_Container.Add(CreateVE(serializedObject));
                    }
                    button_Toggle.text = toggled ? "Default" : "Custom";
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
            _references ??= CustomPropertyReferenceAttribute
                .CreateAllWithAttribute<CustomPropertyReferenceAttribute, ICustomPropertyReference>();

            var found = _references.FirstOrDefault(r => r.Supports(property));
            if (found != null)
            {
                root.Add(CreateObjectFieldWithButton(property.Copy(), found));
                return;
            }

            if (property.isArray && property.propertyType != SerializedPropertyType.String)
            {
                var foldout = new Foldout
                {
                    text = property.displayName
                };

                root.Add(foldout);

                // Draw array size
                var sizeProp = property.FindPropertyRelative("Array.size");
                if (sizeProp != null)
                {
                    foldout.Add(new PropertyField(sizeProp.Copy()));
                }

                // Draw elements
                for (int i = 0; i < property.arraySize; i++)
                {
                    var element = property.GetArrayElementAtIndex(i);
                    DrawPropertyRecursive(foldout, element);
                }

                return;
            }

            if (property.propertyType == SerializedPropertyType.Generic
                && property.hasVisibleChildren)
            {
                var foldout = new Foldout
                {
                    text = property.displayName
                };

                root.Add(foldout);

                var copy = property.Copy();
                var end = copy.GetEndProperty();

                copy.NextVisible(true);

                while (!SerializedProperty.EqualContents(copy, end))
                {
                    DrawPropertyRecursive(foldout, copy);
                    if (!copy.NextVisible(false))
                        break;
                }

                return;
            }

            root.Add(new PropertyField(property.Copy()));
        }

        private static VisualElement CreateObjectFieldWithButton(SerializedProperty property, ICustomPropertyReference reference)
        {
            var layout_Horizontal = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            var button_Open = new Button { text = "->", clickable = new(() => reference.HandleClick(property)), style = { alignSelf = Align.FlexEnd } };
            var propertyField = new PropertyField(property) { style = { flexGrow = 1 } };
            layout_Horizontal.Add(propertyField);
            layout_Horizontal.Add(button_Open);

            return layout_Horizontal;
        }

    }
}
#endif