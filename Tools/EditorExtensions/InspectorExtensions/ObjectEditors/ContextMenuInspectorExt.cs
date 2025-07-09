#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Reflection;

namespace Snm.Tools.InspectorExtra
{
    public class ContextMenuInspectorExt : IInspectorExtension
    {
        ExtensionType IInspectorExtension.ExtensionType => ExtensionType.Attribute;
        public ExtensionPosition Position => ExtensionPosition.Bottom;
        public int Priority => 0;
        bool IInspectorExtension.IsSupportedFor(object target) => target is ContextMenu;

        void IInspectorExtension.ModifyExtensionElement(InspectorExtensionElement extensionElement)
        {
            var button = new Button { text = (extensionElement.Attribute as ContextMenu).menuItem };
            button.clicked += () =>
            {
                if (extensionElement is InspectorExtensionElement_MemberInfo e)
                {
                    var method = e.MemberInfo as MethodInfo;
                    method.Invoke(extensionElement.Target, new object[] { });
                }
            };
            extensionElement.Add(button);
        }

        void IInspectorExtension.CleanUpStaticData()
        {
        }
    }
}

#endif