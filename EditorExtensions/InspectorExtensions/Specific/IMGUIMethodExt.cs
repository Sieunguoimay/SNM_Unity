#if UNITY_EDITOR

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace InspectorExtensions
{
    public class IMGUIMethodExt : IInspectorExtension
    {
        public ExtensionType ExtensionType => ExtensionType.Attribute;
        public Type TargetType => typeof(IMGUIMethodAttribute);

        public void CleanUp()
        {
        }

        public void ModifyExtensionElement(InspectorExtensionElement extensionElement)
        {
            var drawer = new IMGUIDrawer(extensionElement.MemberInfo as MethodInfo, extensionElement.Target, (extensionElement.Attribute as IMGUIMethodAttribute).ShowTitle);
            extensionElement.Add(drawer.Container);
        }

        private class IMGUIDrawer
        {
            private readonly MethodInfo _methodInfo;
            private readonly object _targetObj;
            private readonly bool _showTitle;
            public IMGUIContainer Container { get; private set; }

            public IMGUIDrawer(MethodInfo methodInfo, object targetObj, bool showTitle)
            {
                Container = new IMGUIContainer
                {
                    onGUIHandler = OnGUI
                };
                _methodInfo = methodInfo;
                _targetObj = targetObj;
                _showTitle = showTitle;
                Container.style.marginLeft = 3;
                Container.style.marginRight = 5;
            }

            private void OnGUI()
            {
                EditorGUI.indentLevel++;
                if (_showTitle)
                {
                    EditorGUILayout.LabelField(new GUIContent($"{_methodInfo.Name}"), EditorStyles.boldLabel);
                }
                _methodInfo.Invoke(_targetObj, new object[] { });
                EditorGUI.indentLevel--;
            }
        }
    }
}

#endif