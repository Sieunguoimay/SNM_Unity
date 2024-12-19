#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace InspectorExtensions
{
    public class RevealReferenceEditorExt : IInspectorExtension
    {
        ExtensionType IInspectorExtension.ExtensionType => ExtensionType.Object;

        Type IInspectorExtension.TargetType => typeof(UnityEngine.Object);

        void IInspectorExtension.ModifyExtensionElement(InspectorExtensionElement extensionElement)
        {
            var obj = extensionElement.Target as UnityEngine.Object;
            var serializedObject = new SerializedObject(obj);
            serializedObject.Update();
            var references = Iterate(serializedObject).Select(o => o.objectReferenceValue).ToArray();
            var count = references.Length;
            if (count == 0) return;

            var dFoldout = new Foldout()
            {
                text = $"{obj.GetType().Name} References ({count})",
                value = false
            };
            dFoldout.style.color = Color.gray;
            dFoldout.style.borderTopWidth = 1;
            dFoldout.style.borderTopColor = new Color(.1f, .1f, .1f, 1f);
            extensionElement.Add(dFoldout);

            foreach (var rObject in references)
            {
                var foldout = new Foldout()
                {
                    text = $"{rObject.name} ({rObject.GetType().Name})",
                    value = false,
                    // tooltip = $"{r.propertyPath}"
                };

                foldout.style.unityFontStyleAndWeight = FontStyle.Bold;
                dFoldout.Add(foldout);

                AddIconAndPingButtonToFoldout(rObject, foldout);

                foldout.Add(new CustomIMGUIContainer(rObject));
            }
        }

        private class CustomIMGUIContainer : IMGUIContainer
        {
            private readonly UnityEngine.Object obj;
            private readonly Editor editor;

            public CustomIMGUIContainer(UnityEngine.Object obj)
            {
                this.obj = obj;
                onGUIHandler = OnGUI;
                editor = Editor.CreateEditor(obj);
            }

            private void OnGUI()
            {
                if (obj != null)
                {
                    if (editor != null)
                    {
                        editor.OnInspectorGUI();
                        return;
                    }
                }

                Cleanup();
            }

            private void Cleanup()
            {
                RemoveFromHierarchy();
                onGUIHandler = null;
                if (editor != null)
                {
                    UnityEngine.Object.DestroyImmediate(editor);
                }
            }
        }

        private static void AddIconAndPingButtonToFoldout(UnityEngine.Object o, Foldout foldout)
        {
            var label = foldout.Q<Label>();

            var icon = new VisualElement() { };
            icon.style.width = 14;
            icon.style.height = 14;
            icon.style.marginRight = 4;
            icon.style.backgroundImage = EditorGUIUtility.ObjectContent(o, o.GetType()).image as Texture2D;
            icon.style.alignSelf = Align.Center;
            label.parent.Insert(1, icon);

            label.style.flexGrow = 1;
            var pingButton = new Button() { text = "Ping" };
            pingButton.style.height = 16;
            pingButton.style.width = 40;
            pingButton.style.paddingRight = 0;
            pingButton.style.unityFontStyleAndWeight = FontStyle.Normal;
            pingButton.RegisterCallback<ClickEvent>((evt) => EditorGUIUtility.PingObject(o));
            label.parent.Insert(3, pingButton);
        }

        private IEnumerable<SerializedProperty> Iterate(SerializedObject obj)
        {
            var it = obj.GetIterator();
            while (it.Next(true))
            {
                if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue != null)
                {
                    yield return it;
                }
            }
        }

        void IInspectorExtension.CleanUp()
        {
        }
    }
}
#endif
