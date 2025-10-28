#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;


namespace Sieunguoimay.Tools
{
#if UNITY_EDITOR

    public class ObjectExposedItemsDrawer
    {
        private readonly Action<ObjectExposedItem> _clickEventHandler;
        private Vector2 _scrollPos;

        public ObjectExposedItemsDrawer(Action<ObjectExposedItem> clickEventHandler)
        {
            _clickEventHandler = clickEventHandler;
        }

        public void DrawExposedItems(IEnumerable<ObjectExposedItem> exposedItems, bool allowExpose)
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, EditorStyles.helpBox, GUILayout.ExpandWidth(true));
            EditorGUILayout.BeginVertical(GUI.skin.box);
            foreach (var exposedItem in exposedItems)
            {
                EditorGUILayout.BeginHorizontal();
                if (exposedItem.MemberInfo is MethodInfo methodInfo && methodInfo.GetParameters().Length == 0)
                {
                    if (GUILayout.Button(new GUIContent($"()", "Invoke Method"), GUILayout.Width(25)))
                    {
                        _clickEventHandler?.Invoke(exposedItem);
                    }
                }
                else
                {
                    var isExposable = allowExpose;// && exposedItem.Value != null;
                    if (isExposable)
                    {
                        if (GUILayout.Button(new GUIContent($"->", "Go into"), GUILayout.Width(25)))
                        {
                            _clickEventHandler?.Invoke(exposedItem);
                        }
                    }
                    else
                    {
                        GUILayout.Space(25);
                    }
                }

                EditorGUILayout.LabelField($"{exposedItem.DisplayMemberName}");
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

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }
    }
#endif
}