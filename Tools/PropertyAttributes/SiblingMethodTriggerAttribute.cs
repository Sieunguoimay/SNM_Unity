using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Snm.Tools
{
    public class SiblingMethodTriggerAttribute : PropertyAttribute
    {
        public SiblingMethodTriggerAttribute(string callbackMethod)
        {
            SiblingMethod = callbackMethod;
        }

        public string SiblingMethod { get; }

#if UNITY_EDITOR
        public virtual void OnButtonClicked(SerializedProperty property)
        {
            InvokeCallback(SiblingMethod, property);
        }

        public static void InvokeCallback(string callbackMethod, SerializedProperty property)
        {
            var obj = GetParentObjectOfProperty(property);
            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;
            var method = obj.GetType().GetMethod(callbackMethod, flags);
            if (method.GetParameters().Length > 0)
            {
                method.Invoke(obj, new object[] { new MoreButtonCallbackData(obj, property.serializedObject.targetObject) });
            }
            else
            {
                method.Invoke(obj, new object[] { });
            }
        }

        public static object GetParentObjectOfProperty(SerializedProperty property)
        {
            var targetObject = property.serializedObject.targetObject as object;
            var fieldNames = property.propertyPath.Split('.');
            for (var i = 0; i < fieldNames.Length - 1; i++)
            {
                var fieldName = fieldNames[i];

                FieldInfo fieldInfo;
                if (fieldName.EndsWith("]"))
                {
                    fieldInfo = targetObject.GetType().GetField(fieldName.Split('[')[0], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    var index = int.Parse(fieldName.Split('[', ']')[1]);
                    var array = fieldInfo.GetValue(targetObject) as System.Array;
                    targetObject = array.GetValue(index);
                }
                else
                {
                    fieldInfo = targetObject.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    targetObject = fieldInfo.GetValue(targetObject);
                }
            }

            return targetObject;
        }
#endif
    }

    public class MoreButtonCallbackData
    {
        public object ImmediateObject { get; }
        public Object Context { get; }

        public MoreButtonCallbackData(object immediateObject, Object context)
        {
            ImmediateObject = immediateObject;
            Context = context;
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(SiblingMethodTriggerAttribute))]
    public class MoreButtonDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.width -= 20;
            EditorGUI.PropertyField(position, property, label);
            position.x += position.width;
            position.width = 20;
            if (GUI.Button(position, ".."))
            {
                (attribute as SiblingMethodTriggerAttribute)?.OnButtonClicked(property);
            }
        }
    }
#endif
}
