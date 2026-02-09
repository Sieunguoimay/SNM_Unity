#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public enum InspectorExtensionLocation
    {
        EditorTop,
        EditorBottom
    }

    public sealed class InspectorExtensionContext
    {
        public VisualElement Root { get; }
        public UnityEngine.Object Target { get; }

        internal InspectorExtensionContext(
            VisualElement root,
            UnityEngine.Object target
        )
        {
            Root = root;
            Target = target;
        }
    }


    public abstract class InspectorExtension
    {
        /// <summary>
        /// Where this extension wants to be placed.
        /// </summary>
        public abstract InspectorExtensionLocation Location { get; }

        public abstract bool SupportsObject(UnityEngine.Object target);

        /// <summary>
        /// Create extension UI using provided context.
        /// </summary>
        public abstract void Build(InspectorExtensionContext context);

        /// <summary>
        /// Optional cleanup
        /// </summary>
        public virtual void OnRemoved() { }
    }

    public sealed class InspectorWindowController
    {
        private readonly EditorWindow inspector;

        public InspectorWindowController(EditorWindow inspectorWindow)
        {
            inspector = inspectorWindow;
        }

        public void ApplyExtensions(IEnumerable<InspectorExtension> extensions)
        {
            ClearExtensions();

            foreach (var editorElement in InspectorWindowScan.EnumerateEditorElements(inspector))
            {
                if (!InspectorEditorElementAccess.TryGetEditor(editorElement, out var editor))
                    continue;

                var target = editor.target;
                if (target == null)
                    continue;

                InspectorAttachmentZones.RebuildZones(editorElement, out var top, out var bottom);

                foreach (var ext in extensions)
                {
                    if (!ext.SupportsObject(target)) continue;
                    var location = ext.Location == InspectorExtensionLocation.EditorTop ? top : bottom;

                    var ctx = new InspectorExtensionContext(new VisualElement(), target);
                    location .Add(ctx.Root);
                    ext.Build(ctx);
                }
            }
        }


        public void ClearExtensions()
        {
        }
    }

    internal static class InspectorWindowLayout
    {
        public const string MainContainerClass = "unity-inspector-main-container";
        public const string EditorsListClass = "unity-inspector-editors-list";

        public static bool TryGetMainContainer(
            EditorWindow inspector,
            out VisualElement main)
        {
            main = inspector.rootVisualElement
                .Q<VisualElement>(className: MainContainerClass);

            return main != null;
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

    internal static class InspectorEditorElementAccess
    {
        private const BindingFlags Flags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public const string EditorElementTypeName = "UnityEditor.UIElements.EditorElement";
        public const string InspectorElementTypeName = "UnityEditor.UIElements.InspectorElement";

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

        public static VisualElement FindInspectorElement(
            VisualElement editorElement)
        {
            return editorElement.Children()
                .FirstOrDefault(e =>
                    e.GetType().FullName == InspectorElementTypeName);
        }
    }
    internal static class InspectorAttachmentZones
    {
        public const string ZoneClass = "snm-inspector-extension-zone";

        public const string TopZoneName = "snm-inspector-ext-top";
        public const string BottomZoneName = "snm-inspector-ext-bottom";

        public static void RebuildZones(
            VisualElement editorElement,
            out VisualElement top,
            out VisualElement bottom)
        {
            RemoveExistingZones(editorElement);

            top = CreateZone(TopZoneName);
            bottom = CreateZone(BottomZoneName);

            editorElement.Add(top);
            editorElement.Add(bottom);

            var inspector =
                InspectorEditorElementAccess.FindInspectorElement(editorElement);

            if (inspector != null)
            {
                top.PlaceBehind(inspector);
                bottom.PlaceInFront(inspector);
            }
        }

        private static void RemoveExistingZones(VisualElement editorElement)
        {
            var existing = editorElement
                .Query<VisualElement>(className: ZoneClass)
                .Build()
                .ToArray();

            foreach (var e in existing)
                e.RemoveFromHierarchy();
        }

        private static VisualElement CreateZone(string name)
        {
            var ve = new VisualElement { name = name };
            ve.AddToClassList(ZoneClass);
            return ve;
        }
    }

    internal static class InspectorWindowHeaderSlot
    {
        public const string RootClass = "snm-inspector-extension";

        public static bool InsertOrReplace(
            EditorWindow inspector,
            VisualElement element)
        {
            if (!InspectorWindowLayout.TryGetMainContainer(inspector, out var main))
                return false;

            // Find existing extension by USS class
            var existing = main.Q(
                className: RootClass);

            if (existing != null)
                existing.RemoveFromHierarchy();

            // Insert at top (Inspector header area)
            element.AddToClassList(RootClass);
            main.Insert(0, element);
            return true;
        }


        public static void Remove(VisualElement header)
        {
            header?.RemoveFromHierarchy();
        }
    }

    internal static class InspectorWindowScan
    {
        public static IEnumerable<VisualElement> EnumerateEditorElements(
            EditorWindow inspector)
        {
            if (!InspectorWindowLayout.TryGetEditorsList(inspector, out var list))
                yield break;

            foreach (var e in InspectorEditorElementAccess.EnumerateEditorElements(list))
                yield return e;
        }
    }
}
#endif