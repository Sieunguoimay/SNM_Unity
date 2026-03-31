#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace Snm.Tools.InspectorExtensions
{
    public class ContextMenuHelper
    {
        public static IEnumerable<(MethodInfo, ContextMenu)> GetMethodInfos(UnityEngine.Object target)
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

        public static void InvokeMethod(MethodInfo method, UnityEngine.Object target)
        {
            try
            {
                method.Invoke(target, null);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
#endif
