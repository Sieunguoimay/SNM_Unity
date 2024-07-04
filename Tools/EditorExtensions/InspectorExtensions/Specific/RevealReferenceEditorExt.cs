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
        public ExtensionType ExtensionType => ExtensionType.Object;

        public Type TargetType => typeof(UnityEngine.Object);
        private readonly List<Editor> editors = new();

        public void ModifyExtensionElement(InspectorExtensionElement extensionElement)
        {
            var obj = extensionElement.Target as UnityEngine.Object;
            var serializedObject = new SerializedObject(obj);
            serializedObject.Update();
            var references = Iterate(serializedObject);
            var count = references.Count();
            if (count == 0) return;

            var dFoldout = new Foldout() { text = $"{obj.GetType().Name} References ({count})", value = false };
            dFoldout.style.color = Color.gray;
            dFoldout.style.borderTopWidth = 1;
            dFoldout.style.borderTopColor = new Color(.1f, .1f, .1f, 1f);
            extensionElement.Add(dFoldout);
            foreach (var r in references)
            {
                var foldout = new Foldout()
                {
                    text = $"{r.objectReferenceValue.name} ({r.objectReferenceValue.GetType().Name})",
                    value = false,
                    tooltip = $"{r.propertyPath}"
                };

                foldout.style.unityFontStyleAndWeight = FontStyle.Bold;
                dFoldout.Add(foldout);

                AddIconAndPingButtonToFoldout(r.objectReferenceValue, foldout);

                var e = Editor.CreateEditor(r.objectReferenceValue);
                foldout.Add(new IMGUIContainer()
                {
                    onGUIHandler = () =>
                    {
                        try { e.OnInspectorGUI(); }
                        catch (Exception) { }
                    }
                });

                editors.Add(e);
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

        public void CleanUp()
        {
            foreach (var e in editors)
            {
                UnityEngine.Object.DestroyImmediate(e);
            }
            editors.Clear();
        }
    }
}
#endif
