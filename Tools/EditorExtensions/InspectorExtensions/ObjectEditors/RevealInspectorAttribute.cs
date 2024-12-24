#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.UIElements;
#endif
using UnityEngine;
using UnityEngine.UIElements;

namespace InspectorExtensions
{
    public class RevealInspectorAttribute : PropertyAttribute
    {
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(RevealInspectorAttribute))]
    public class ShowReferenceEditorPropDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return new ReferenceEditorVE(property);
        }

        private class ReferenceEditorVE : VisualElement
        {
            private Editor _editor;
            private readonly SerializedProperty property;

            public ReferenceEditorVE(SerializedProperty property)
            {
                _editor = Editor.CreateEditor(property.objectReferenceValue);

                var dropDown = new Foldout() { value = false };
                SetupBorders(dropDown);
                Add(dropDown);

                var imguiContainer = new IMGUIContainer(OnIMGUI);
                imguiContainer.style.marginTop = 2f;
                dropDown.Add(imguiContainer);

                var propField = new PropertyField(property);
                propField.style.position = Position.Absolute;
                propField.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
                Add(propField);
                this.property = property;
            }

            private void OnIMGUI()
            {
                UpdateEditor(property)?.OnInspectorGUI();
            }

            private Editor UpdateEditor(SerializedProperty property)
            {
                if (_editor == null && property.objectReferenceValue != null)
                {
                    _editor = Editor.CreateEditor(property.objectReferenceValue);
                }
                if (_editor != null && property.objectReferenceValue == null)
                {
                    _editor = null;
                }
                return _editor;
            }

            private static void SetupBorders(Foldout dropDown)
            {
                dropDown.style.marginTop = 2f;
                dropDown.style.marginBottom = 2f;

                dropDown.contentContainer.style.borderLeftColor = new Color(.1f, .1f, .1f);
                dropDown.contentContainer.style.borderLeftWidth = 1f;
                dropDown.contentContainer.style.paddingLeft = 2f;

                dropDown.contentContainer.style.borderRightColor = new Color(.1f, .1f, .1f);
                dropDown.contentContainer.style.borderRightWidth = 1f;
                dropDown.contentContainer.style.paddingRight = 2f;

                dropDown.contentContainer.style.borderTopColor = new Color(.1f, .1f, .1f);
                dropDown.contentContainer.style.borderTopWidth = 1f;
                dropDown.contentContainer.style.paddingTop = 2f;

                dropDown.contentContainer.style.borderBottomColor = new Color(.1f, .1f, .1f);
                dropDown.contentContainer.style.borderBottomWidth = 1f;
                dropDown.contentContainer.style.paddingBottom = 2f;

                dropDown.contentContainer.style.borderTopLeftRadius = 4f;
                dropDown.contentContainer.style.borderTopRightRadius = 4f;
                dropDown.contentContainer.style.borderBottomLeftRadius = 4f;
                dropDown.contentContainer.style.borderBottomRightRadius = 4f;
            }
        }
    }
#endif
}