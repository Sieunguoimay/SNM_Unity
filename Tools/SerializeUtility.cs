#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Snm.Tools
{
    public static class SerializeUtility
    {
        public static BindingFlags Flag => BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

        public static object GetObjectToWhichPropertyBelong(SerializedProperty property)
        {
            var pathComponents = property.propertyPath.Replace("Array.data[", "[").Split('.');
            var pathComponentsToDirectObject = pathComponents.Take(pathComponents.Length - 1);

            object currentObject = property.serializedObject.targetObject;

            var pattern = @"\[(\d+)\]";
            var regex = new Regex(pattern);
            foreach (var p in pathComponentsToDirectObject)
            {
                if (p.StartsWith('['))
                {
                    if (int.TryParse(regex.Match(p).Groups[1].Value, out int arrIndex))
                    {
                        currentObject = (currentObject as object[])[arrIndex];
                    }
                }
                else
                {
                    var t = currentObject.GetType();
                    while (t != null)
                    {
                        var fieldInfo = t.GetField(p, Flag);
                        if (fieldInfo != null)
                        {
                            currentObject = fieldInfo.GetValue(currentObject);
                            break;
                        }
                        else
                        {
                            t = t.BaseType;
                        }
                    }
                }
            }

            return currentObject;
        }
    }
}
#endif