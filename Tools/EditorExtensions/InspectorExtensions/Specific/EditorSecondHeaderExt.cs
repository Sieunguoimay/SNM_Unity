#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace InspectorExtensions
{
    public class EditorSecondHeaderExt : IInspectorExtension
    {
        ExtensionType IInspectorExtension.ExtensionType => ExtensionType.Object;
        ExtensionPosition IInspectorExtension.Position => ExtensionPosition.Top;
        int IInspectorExtension.Priority => 0;
        bool IInspectorExtension.IsSupportedFor(object target)
        {
            if (target is MonoBehaviour) return true;
            if (target is ScriptableObject) return true;
            return false;
        }

        void IInspectorExtension.ModifyExtensionElement(InspectorExtensionElement extensionElement)
        {
            if (extensionElement.Target is UnityEngine.Object target)
            {
                extensionElement.Add(new SecondHeaderVE(target));
            }
        }

        void IInspectorExtension.CleanUp()
        {
        }

        private class SecondHeaderVE : VisualElement
        {
            private readonly UnityEngine.Object target;

            public SecondHeaderVE(UnityEngine.Object target)
            {
                this.target = target;

                style.flexDirection = FlexDirection.RowReverse;

                if (target is ScriptableObject)
                {
                    style.paddingTop = 2;
                    style.paddingBottom = 2;
                    style.height = 22;
                }
                if (target is MonoBehaviour)
                {
                    style.marginBottom = -3;
                    style.height = 18;
                }

                Add(CreateEditScriptButton());
            }

            private VisualElement CreateEditScriptButton()
            {
                var button = new Button(() =>
                {
                    var serialized = new SerializedObject(target);
                    var scriptProperty = serialized.FindProperty("m_Script");
                    AssetDatabase.OpenAsset(scriptProperty.objectReferenceValue);
                })
                {
                    text = "Edit Script"
                };
                button.style.marginTop = 0;
                button.style.marginBottom = 0;
                return button;
            }
            private VisualElement CreateCopyComponentButton()
            {
                var button = new Button(() =>
                {
                })
                {
                    text = "Copy Component"
                };
                return button;
            }
        }
    }
}

#endif