using System;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Snm.Tools
{

    public class TypeSelectorAttribute : PropertyAttribute
    {
        public TypeSelectorAttribute(Type typeConstraint = null, bool shouldDrawLabel = true)
        {
            TypeConstraint = typeConstraint;
            ShouldDrawLabel = shouldDrawLabel;
        }

        public Type TypeConstraint { get; }
        public bool ShouldDrawLabel { get; }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(TypeSelectorAttribute))]
    public class ObjectSelectorDrawer : PropertyDrawer
    {
        private TypeSelectorAttribute _att;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _att ??= attribute as TypeSelectorAttribute;
            EditorGUI.BeginProperty(position, label, property);
            position.width -= 20;

            using var changed = new EditorGUI.ChangeCheckScope();
            var oldValue = property.objectReferenceValue;

            if (_att.ShouldDrawLabel)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
            else
            {
                EditorGUI.PropertyField(position, property, GUIContent.none, true);
            }

            if (changed.changed)
            {
                var newValue = property.objectReferenceValue;
                if (oldValue == null && newValue != null)
                {
                    var autoSelected = GetAllAssociatedObjects(newValue).FirstOrDefault(o => _att.TypeConstraint.IsInstanceOfType(o));

                    property.serializedObject.Update();
                    property.objectReferenceValue = autoSelected;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }

            position.x += position.width;
            position.width = 20;
            if (GUI.Button(position, new GUIContent("..", property.objectReferenceValue?.GetType()?.Name)))
            {
                ShowGenericMenu(property, obj =>
                {
                    property.serializedObject.Update();
                    property.objectReferenceValue = obj;
                    property.serializedObject.ApplyModifiedProperties();
                });
            }
            EditorGUI.EndProperty();
        }

        private void ShowGenericMenu(SerializedProperty property, System.Action<UnityEngine.Object> onNewObject)
        {
            var menu = new GenericMenu();
            var att = attribute as TypeSelectorAttribute;
            var allObjects = GetAllAssociatedObjects(property.objectReferenceValue)
                .Where(o => att.TypeConstraint?.IsAssignableFrom(o.GetType()) ?? true);
            foreach (var obj in allObjects)
            {
                menu.AddItem(new GUIContent(obj.GetType().Name), property.objectReferenceValue == obj, () =>
                {
                    onNewObject?.Invoke(obj);
                });
            }
            menu.ShowAsContext();
        }

        private static IEnumerable<UnityEngine.Object> GetAllAssociatedObjects(UnityEngine.Object obj)
        {
            if (obj is GameObject go)
            {
                yield return go;
                foreach (var c in go.GetComponents<Component>()) yield return c;
            }
            else if (obj is Component c)
            {
                yield return c.gameObject;
                foreach (var c1 in c.gameObject.GetComponents<Component>()) yield return c1;
            }
            else if (obj is ScriptableObject so)
            {
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(so))) yield return o;
            }
            else
            {
                yield return obj;
            }
        }
    }
#endif

}