#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Reflection;

namespace InspectorExtensions
{
    public class ContextMenuInspectorExt : IInspectorExtension
    {
        public Type TargetType => typeof(ContextMenu);

        public ExtensionType ExtensionType => ExtensionType.Attribute;

        public void CleanUp()
        {
        }

        public void ModifyExtensionElement(InspectorExtensionElement extensionElement)
        {
            var button = new Button { text = (extensionElement.Attribute as ContextMenu).menuItem };
            button.clicked += () =>
            {
                var method = extensionElement.MemberInfo as MethodInfo;
                method.Invoke(extensionElement.Target, new object[] { });
            };
            extensionElement.Add(button);
        }
    }
}

#endif