using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
namespace PropertyExt
{
    public class MoreButtonAttribute : PropertyAttribute
    {
        public MoreButtonAttribute(string callbackMethod)
        {
            SiblingName = callbackMethod;
        }

        public string SiblingName { get; }
#if UNITY_EDITOR
        public virtual void OnButtonClicked(SerializedProperty property)
        {
            InvokeCallback(SiblingName, property);
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

            FieldInfo fieldInfo = null;
            for (int i = 0; i < fieldNames.Length - 1; i++)
            {
                var fieldName = fieldNames[i];
                if (fieldName.EndsWith("]"))
                {
                    fieldInfo = targetObject.GetType().GetField(fieldName.Split('[')[0], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    int index = int.Parse(fieldName.Split('[', ']')[1]);
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
        public UnityEngine.Object Context { get; }
        public MoreButtonCallbackData(object immediateObject, UnityEngine.Object context)
        {
            ImmediateObject = immediateObject;
            Context = context;
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(MoreButtonAttribute))]
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
                (attribute as MoreButtonAttribute)?.OnButtonClicked(property);
            }
        }

    }

#endif
}
