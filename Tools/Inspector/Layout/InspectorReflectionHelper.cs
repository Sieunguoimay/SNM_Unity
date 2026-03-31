#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    internal static class InspectorReflectionHelper
    {
        private const BindingFlags Flags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public const string EditorElementTypeName = "UnityEditor.UIElements.EditorElement";
        public const string InspectorElementTypeName = "UnityEditor.UIElements.InspectorElement";
        public const string MainContainerClass = "unity-inspector-main-container";
        public const string EditorsListClass = "unity-inspector-editors-list";

        public static IEnumerable<VisualElement> EnumerateEditorElements(
            VisualElement editorsList)
        {
            return editorsList.Children()
                .Where(e => e.GetType().FullName == EditorElementTypeName);
        }

        public static bool TryGetEditor(
            VisualElement editorElement,
            out Editor editor)
        {
            editor = editorElement
                .GetType()
                .GetProperty("editor", Flags)?
                .GetValue(editorElement) as Editor;

            return editor != null;
        }

        public static InspectorElement FindInspectorElement(
            VisualElement editorElement)
        {
            return editorElement.Q<InspectorElement>();
        }

        public static bool TryGetMainContainer(
            EditorWindow inspector,
            out VisualElement editorsList)
        {
            editorsList = inspector.rootVisualElement
                .Q<VisualElement>(className: MainContainerClass);

            return editorsList != null;
        }

        public static bool TryGetEditorsList(
            EditorWindow inspector,
            out VisualElement editorsList)
        {
            editorsList = inspector.rootVisualElement
                .Q<VisualElement>(className: EditorsListClass);

            return editorsList != null;
        }
    }
}
#endif