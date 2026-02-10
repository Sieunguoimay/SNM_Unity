#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace Snm.Tools.InspectorExtensions
{
    public sealed class ContextMenuListVEBuilder
    {
        public static VisualElement BuildVE(UnityEngine.Object target)
        {
            var root = new VisualElement();
            foreach (var (method, menuAttr) in GetMethodInfos(target))
            {
                var button = new Button
                {
                    text = menuAttr.menuItem
                };

                button.clicked += () =>
                {
                    try
                    {
                        method.Invoke(target, null);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                };

                root.Add(button);
            }
            return root;
        }

        private static IEnumerable<(MethodInfo, ContextMenu)> GetMethodInfos(UnityEngine.Object target)
        {
            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            var type = target.GetType();

            while (type != null)
            {
                foreach (var member in type.GetMethods(flags))
                {
                    foreach (var attr in member.GetCustomAttributes())
                    {
                        if (attr is ContextMenu cm) yield return (member, cm);
                    }
                }

                type = type.BaseType;
            }
        }
    }
}
#endif
