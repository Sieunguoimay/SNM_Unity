#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public sealed class InspectorLayoutInjector
    {
        private readonly Dictionary<EditorWindow, EditorWindowItem> windows = new();

        public InspectorWindowLayout GetOrCreateLayout(EditorWindow inspectorWindow)
        {
            if (windows.TryGetValue(inspectorWindow, out var item))
            {
                return item.windowVE;
            }

            if (InspectorReflectionHelper.TryGetMainContainer(inspectorWindow, out var mainContainer))
            {
                var top = new VisualElement() { style = { flexShrink = 0 } };
                var bottom = new VisualElement() { style = { flexShrink = 0 } };
                var left = new VisualElement() { style = { flexShrink = 0 } };
                var right = new VisualElement() { style = { flexShrink = 0 } };

                var zonesLifecycles = new List<AttachmentZonesLifecycle> { new(mainContainer, top, bottom, left, right) };

                InspectorReflectionHelper.TryGetEditorsList(inspectorWindow, out var editorsList);
                var editorVEs = InspectorReflectionHelper.EnumerateEditorElements(editorsList)
                    .Select(editorVE =>
                    {
                        if (!InspectorReflectionHelper.TryGetEditor(editorVE, out var editor)) return null;

                        var imguiContainer = editorVE.Query<IMGUIContainer>().AtIndex(1);
                        if (imguiContainer == null) return null;
                        imguiContainer.style.flexGrow = 1;

                        VisualElement t = new(), b = new(), l = new(), r = new();
                        zonesLifecycles.Add(new(imguiContainer, t, b, l, r));

                        var inspectorElement = editorVE.Q<InspectorElement>();
                        return new EditorLayout(
                            attachmentZones: new(t, b, l, r),
                            targetObjects: editor.targets, editor.serializedObject, inspectorElement);
                    })
                    .Where(e => e != null)
                    .ToArray();

                var ve = new InspectorWindowLayout(new(top, bottom, left, right), editorVEs, inspectorWindow);
                windows.Add(inspectorWindow, new(zonesLifecycles.ToArray(), ve));

                return ve;
            }

            return null;
        }

        public void CleanupInjectedLayout(EditorWindow inspectorWindow)
        {
            if (windows.TryGetValue(inspectorWindow, out var item))
            {
                foreach (var zonesAttachment in item.attachments)
                {
                    zonesAttachment.Cleanup();
                }

                windows.Remove(inspectorWindow);
            }
        }

        public class AttachmentZonesLifecycle
        {
            private readonly VisualElement center;
            private readonly VisualElement parent;
            private readonly int index;
            private readonly VisualElement vertical;

            public AttachmentZonesLifecycle(
                VisualElement center,
                VisualElement top,
                VisualElement bottom,
                VisualElement left,
                VisualElement right)
            {
                this.center = center;

                parent = center.parent;
                index = parent.IndexOf(center);

                parent.RemoveAt(index);

                vertical = new VisualElement() { name = "Vertical" };
                var horizontal = new VisualElement() { name = "Horizontal", style = { flexDirection = FlexDirection.Row } };

                vertical.Add(top);
                vertical.Add(horizontal);
                vertical.Add(bottom);

                horizontal.Add(left);
                horizontal.Add(center);
                horizontal.Add(right);

                parent.Insert(index, vertical);
            }

            public void Cleanup()
            {
                vertical.RemoveFromHierarchy();
                parent.Insert(index, center);
            }
        }

        private class EditorWindowItem
        {
            public AttachmentZonesLifecycle[] attachments;
            public InspectorWindowLayout windowVE;

            public EditorWindowItem(
                AttachmentZonesLifecycle[] attachments,
                InspectorWindowLayout windowVE)
            {
                this.attachments = attachments;
                this.windowVE = windowVE;
            }
        }
    }
}
#endif