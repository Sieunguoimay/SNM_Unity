#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public sealed class InspectorWindowController
    {
        private readonly EditorWindow inspectorWindow;
        private readonly InspectorAttachmentZones windowZonesBuilder = new("snm-inspector-window-ext");
        private readonly InspectorAttachmentZones editorZonesBuilder = new("snm-editor-ext");
        private readonly VisualElement editorList;
        private readonly VisualElement mainContainer;

        public InspectorWindowController(EditorWindow inspectorWindow)
        {
            this.inspectorWindow = inspectorWindow;
            InspectorWindowLayout.TryGetEditorsList(inspectorWindow, out editorList);
            InspectorWindowLayout.TryGetMainContainer(inspectorWindow, out mainContainer);
        }

        public void ApplyExtensions(IEnumerable<IInspectorExtension> extensions)
        {

            foreach (var editorElement in InspectorEditorElementAccess.EnumerateEditorElements(editorList))
            {
                if (!InspectorEditorElementAccess.TryGetEditor(editorElement, out var editor))
                    continue;

                var target = editor.target;
                if (target == null)
                    continue;
                editorZonesBuilder.RebuildZones(
                    InspectorEditorElementAccess.FindInspectorElement(editorElement),
                    out var left,
                    out var right,
                    out var top,
                    out var bottom);

                foreach (var extension in extensions)
                {
                    var support = extension.SupportedTypes.Any(t => t.IsInstanceOfType(target));
                    if (!support) continue;

                    var ctx = new InspectorExtensionContext(target, inspectorWindow);
                    var root = extension.VEBuilder.BuildVE(ctx);
                    var location = extension.Location switch
                    {
                        InspectorExtensionLocation.Left => left,
                        InspectorExtensionLocation.Right => right,
                        InspectorExtensionLocation.Top => top,
                        InspectorExtensionLocation.Bottom => bottom,
                        _ => throw new NotImplementedException(),
                    };
                    location.Add(root);
                }
            }
        }

        public void ClearExtensions()
        {
            if (editorList != null)
            {
                windowZonesBuilder.RemoveExistingZones(mainContainer.parent);
                editorZonesBuilder.RemoveExistingZones(editorList.parent);
            }
        }

        public void AssignWindowVEs(InspectorExtensionLocation location, IEnumerable<VisualElement> visualElements)
        {
            windowZonesBuilder.RebuildZones(
                mainContainer,
                out var left,
                out var right,
                out var top,
                out var bottom);

            var locationVE = location switch
            {
                InspectorExtensionLocation.Left => left,
                InspectorExtensionLocation.Right => right,
                InspectorExtensionLocation.Top => top,
                InspectorExtensionLocation.Bottom => bottom,
                _ => throw new NotImplementedException(),
            };

            foreach (var ve in visualElements) locationVE.Add(ve);
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

    internal class InspectorAttachmentZones
    {
        private readonly string ZoneClass = "snm-inspector-extension-zone";
        private const string LeftZoneName = "snm-inspector-ext-left";
        private const string RightZoneName = "snm-inspector-ext-right";
        private const string TopZoneName = "snm-inspector-ext-top";
        private const string BottomZoneName = "snm-inspector-ext-bottom";

        public InspectorAttachmentZones(string zoneClass)
        {
            ZoneClass = zoneClass;
        }

        public void RebuildZones(
            VisualElement element,
            out VisualElement left,
            out VisualElement right,
            out VisualElement top,
            out VisualElement bottom)
        {
            left = CreateZone(LeftZoneName);
            right = CreateZone(RightZoneName);
            top = CreateZone(TopZoneName);
            bottom = CreateZone(BottomZoneName);

            if (element == null) return;
            var parentElement = element.parent;
            if (parentElement == null) return;

            RemoveExistingZones(parentElement);

            parentElement.Add(left);
            parentElement.Add(right);
            parentElement.Add(top);
            parentElement.Add(bottom);

            left.PlaceBehind(element);
            top.PlaceBehind(element);
            bottom.PlaceInFront(element);
            right.PlaceInFront(element);
            // AddRightPanel(element, right);
        }

        public void RemoveExistingZones(VisualElement element)
        {
            var existing = element
                .Query<VisualElement>(className: ZoneClass)
                .Build()
                .ToArray();

            foreach (var e in existing)
                e.RemoveFromHierarchy();
        }

        private VisualElement CreateZone(string name)
        {
            var ve = new VisualElement { name = name };
            ve.AddToClassList(ZoneClass);
            return ve;
        }

        private static void AddRightPanel(VisualElement editorElement, VisualElement panel)
        {
            var parent = editorElement.parent;
            var index = parent.IndexOf(editorElement);

            // Remove original editor
            parent.RemoveAt(index);

            // Wrapper
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexGrow = 1;

            // Make editor take main space
            editorElement.style.flexGrow = 1;

            // Rebuild hierarchy
            row.Add(editorElement);
            row.Add(panel);

            parent.Insert(index, row);
        }

    }


    internal static class InspectorWindowLayout
    {
        public const string MainContainerClass = "unity-inspector-main-container";
        public const string EditorsListClass = "unity-inspector-editors-list";

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