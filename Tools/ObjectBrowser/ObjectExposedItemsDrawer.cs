#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;


namespace Snm.Tools.ObjectBrowser
{
#if UNITY_EDITOR

    public class ObjectExposedItemsDrawer
    {
        public static void DrawExposedItems(
            IEnumerable<ObjectExposedItem> exposedItems,
            Action<ObjectExposedItem> clickEventHandler,
            bool allowExpose,
            bool hasObject,
            bool displayTypeHash)
        {
            foreach (var exposedItem in exposedItems)
            {
                EditorGUILayout.BeginHorizontal();
                if (exposedItem.MemberInfo is MethodInfo methodInfo)
                {
                    if (methodInfo.GetParameters().Length == 0 && (hasObject || methodInfo.IsStatic))
                    {
                        if (GUILayout.Button(new GUIContent($"()", "Invoke Method"), GUILayout.Width(25)))
                        {
                            clickEventHandler?.Invoke(exposedItem);
                        }
                    }
                    else
                    {
                        GUILayout.Space(25);
                    }
                }
                else
                {
                    var isExposable = allowExpose;// && exposedItem.Value != null;
                    if (isExposable)
                    {
                        if (GUILayout.Button(new GUIContent($"->", "Go into"), GUILayout.Width(25)))
                        {
                            clickEventHandler?.Invoke(exposedItem);
                        }
                    }
                    else
                    {
                        GUILayout.Space(25);
                    }
                }

                EditorGUILayout.LabelField(displayTypeHash ? exposedItem.MemberName : exposedItem.DisplayMemberName);
                if (exposedItem.Value is Object asset)
                {
                    EditorGUILayout.ObjectField(asset, typeof(Object), false);
                }
                else
                {
                    EditorGUILayout.LabelField($"{exposedItem.DisplayValue}");
                }

                EditorGUILayout.EndHorizontal();
            }

        }
    }
#endif
}