#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public sealed class InspectorWindowVERegistry
    {
        private readonly Dictionary<EditorWindow, EditorWindowItem> windows = new();

        public InspectorWindowVE GetOrCreateWindowVE(EditorWindow inspectorWindow)
        {
            if (windows.TryGetValue(inspectorWindow, out var item))
            {
                return item.windowVE;
            }

            if (InspectorWindowLayout.TryGetMainContainer(inspectorWindow, out var mainContainer))
            {
                VisualElement top = new(), bottom = new(), left = new(), right = new();
                var zonesLifecycles = new List<AttachmentZonesLifecycle>
                {
                    new(mainContainer, top, bottom, left, right)
                };

                InspectorWindowLayout.TryGetEditorsList(inspectorWindow, out var editorsList);
                var editorVEs = InspectorEditorElementAccess.EnumerateEditorElements(editorsList)
                    .Select(editorVE =>
                    {
                        var centerVE = InspectorEditorElementAccess.FindInspectorElement(editorVE);
                        if (centerVE == null) return null;

                        VisualElement t = new(), b = new(), l = new(), r = new();
                        zonesLifecycles.Add(new(centerVE, t, b, l, r));

                        InspectorEditorElementAccess.TryGetEditor(editorVE, out var editor);
                        return new EditorVE(new(t, b, l, r), editor.targets);
                    })
                    .Where(e => e != null)
                    .ToArray();

                var ve = new InspectorWindowVE(new(top, bottom, left, right), editorVEs, inspectorWindow);
                windows.Add(inspectorWindow, new(zonesLifecycles.ToArray(), ve));

                return ve;
            }

            return null;
        }

        public void CleanupWindowVE(EditorWindow inspectorWindow)
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

        private class AttachmentZonesLifecycle
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
            public InspectorWindowVE windowVE;

            public EditorWindowItem(
                AttachmentZonesLifecycle[] attachments,
                InspectorWindowVE windowVE)
            {
                this.attachments = attachments;
                this.windowVE = windowVE;
            }
        }
    }
}
#endif