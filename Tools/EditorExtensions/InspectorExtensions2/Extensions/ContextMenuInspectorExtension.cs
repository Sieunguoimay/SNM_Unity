#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace Snm.Tools.InspectorExtensions
{
    public sealed class ContextMenuInspectorExtension : InspectorExtension
    {
        public override InspectorExtensionLocation Location => InspectorExtensionLocation.EditorBottom;

        public override bool SupportsObject(UnityEngine.Object target)
        {
            return true;
        }

        public override void Build(InspectorExtensionContext context)
        {
            Debug.Log("OK");
            foreach (var (method, menuAttr) in GetMethodInfos(context.Target))
            {
                var button = new Button
                {
                    text = menuAttr.menuItem
                };

                button.clicked += () =>
                {
                    try
                    {
                        method.Invoke(context.Target, null);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                };

                context.Root.Add(button);
            }
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
