#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework.Constraints;
using UnityEditor;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtensions
{
    public class InspectorWindowVE
    {
        public AttachmentZones AttachmentZones { get; }
        public EditorVE[] EditorVEs { get; }
        public EditorWindow InspectorWindow { get; }

        public InspectorWindowVE(
            AttachmentZones attachmentZones,
            EditorVE[] editorVEs,
            EditorWindow inspectorWindow)
        {
            AttachmentZones = attachmentZones;
            EditorVEs = editorVEs;
            InspectorWindow = inspectorWindow;
        }
    }

    public class InspectorExtensionVEApplier
    {
        private readonly InspectorWindowVE[] windowVEs;
        private readonly List<VisualElement> extensionVEs = new();

        public InspectorExtensionVEApplier(InspectorWindowVE[] windowVEs)
        {
            this.windowVEs = windowVEs;
        }

        public void ApplyExtensions(IInspectorExtension[] extensions)
        {
            foreach (var w in windowVEs)
            {
                foreach (var e in w.EditorVEs)
                {
                    foreach (var ext in extensions)
                    {
                        var supported = false;

                        foreach (var t in ext.SupportedTypes)
                        {
                            foreach (var to in e.TargetObjects)
                            {
                                if (t.IsInstanceOfType(to))
                                {
                                    supported = true;
                                    break;
                                }
                            }
                            if (supported) break;
                        }

                        if (supported)
                        {
                            var extVE = ext.VEBuilder.BuildVE(new(e.TargetObjects.FirstOrDefault(), w.InspectorWindow));
                            var parentVE = ext.Location switch
                            {
                                InspectorExtensionLocation.Top => e.AttachmentZones.Top,
                                InspectorExtensionLocation.Bottom => e.AttachmentZones.Bottom,
                                InspectorExtensionLocation.Left => e.AttachmentZones.Left,
                                InspectorExtensionLocation.Right => e.AttachmentZones.Right,
                                _ => throw new System.NotImplementedException(),
                            };
                            parentVE.Add(extVE);
                            extensionVEs.Add(extVE);
                        }
                    }
                }
            }
        }

        public void ClearExtensionVEs()
        {
            foreach (var eve in extensionVEs)
            {
                eve.RemoveFromHierarchy();
            }
            extensionVEs.Clear();
        }
    }

    public class EditorVE
    {
        public AttachmentZones AttachmentZones { get; }
        public UnityEngine.Object[] TargetObjects { get; }

        public EditorVE(
            AttachmentZones attachmentZones,
            UnityEngine.Object[] targetObjects)
        {
            AttachmentZones = attachmentZones;
            TargetObjects = targetObjects;
        }
    }

    public class AttachmentZones
    {
        public VisualElement Top { get; }
        public VisualElement Bottom { get; }
        public VisualElement Left { get; }
        public VisualElement Right { get; }

        public AttachmentZones(
            VisualElement top,
            VisualElement bottom,
            VisualElement left,
            VisualElement right)
        {
            Top = top;
            Bottom = bottom;
            Left = left;
            Right = right;
        }
    }
}
#endif