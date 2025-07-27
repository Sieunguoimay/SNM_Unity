using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Sieunguoimay.Attribute
{
    public class ComponentSelectorAttribute : ObjectSelectorAttribute
    {
        public ComponentSelectorAttribute() : base("", false)
        {
        }
    }
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ComponentSelectorAttribute))]
    public class ComponentSelectorPropertyDrawer : ObjectSelectorDrawer
    {
        // public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        // {
        //     var fullWidth = position.width;
        //     position.width = fullWidth - 27;
        //     EditorGUI.PropertyField(position, property, label);
        //
        //     var btnRect = position;
        //     btnRect.x = position.x + fullWidth - 25;
        //     btnRect.width = 25;
        //     TypeConstraintPropertyDrawer.Menu(btnRect, property, go => go.GetComponents<Component>().Concat(new[] {go as Object}).ToArray(), false);
        // }

        protected override IEnumerable<Object> GetObjects(SerializedProperty property, StringSelectorAttribute objectSelector)
        {
            var value = property.objectReferenceValue
                ? property.objectReferenceValue
                : property.serializedObject.targetObject;

            var go = value as GameObject ?? (value as Component)?.gameObject;
            return go.GetComponents<Component>().Concat(new[] {(Object) go});
        }

        protected override string GetName(Object obj)
        {
            return obj?.GetType().Name;
        }
    }
#endif
}