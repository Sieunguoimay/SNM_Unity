#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Reflection;

namespace InspectorExtensions
{
    public class ContextMenuInspectorExt : IInspectorExtension
    {
        Type IInspectorExtension.TargetType => typeof(ContextMenu);

        ExtensionType IInspectorExtension.ExtensionType => ExtensionType.Attribute;

        void IInspectorExtension.CleanUp()
        {
        }

        void IInspectorExtension.ModifyExtensionElement(InspectorExtensionElement extensionElement)
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