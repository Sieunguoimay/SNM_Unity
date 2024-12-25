#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace InspectorExtensions
{
    public class AnimationClipInspectorExt : IInspectorExtension
    {
        ExtensionType IInspectorExtension.ExtensionType => ExtensionType.Object;
        ExtensionPosition IInspectorExtension.Position => ExtensionPosition.Bottom;
        int IInspectorExtension.Priority => 0;
        bool IInspectorExtension.IsSupportedFor(object target) => target is AnimationClip;

        void IInspectorExtension.CleanUpStaticData()
        {
        }

        void IInspectorExtension.ModifyExtensionElement(InspectorExtensionElement extensionElement)
        {
            var clip = extensionElement.Target as AnimationClip;
            extensionElement.Add(new EditorContainer(clip));
        }

        private class EditorContainer : VisualElement
        {
            private readonly Button applyButton;
            private readonly List<ClipLineVE> clipLineVEs = new();
            private readonly AnimationClip clip;

            public EditorContainer(AnimationClip clip)
            {
                var foldout = new Foldout() { value = false, text = "Curves" };
                Add(foldout);

                var lines = AnimationUtility.GetCurveBindings(clip)
                    .Select(b => new ClipLine { binding = b, curve = AnimationUtility.GetEditorCurve(clip, b) }).ToArray();
                for (int i = 0; i < lines.Length; i++)
                {
                    var l = lines[i];
                    var line = new ClipLineVE(l, i);
                    line.RegisterCallback<ChangeEvent<bool>>(OnLineChanged);
                    foldout.Add(line);
                    clipLineVEs.Add(line);
                }

                applyButton = new Button() { text = "Apply" };
                applyButton.RegisterCallback<ClickEvent>(OnApplyClicked);
                applyButton.SetEnabled(false);
                foldout.Add(applyButton);

                this.clip = clip;
            }

            private void OnApplyClicked(ClickEvent evt)
            {
                Debug.Log($"Applied Modifications to AnimationClip {clip.name}", clip);

                foreach (var l in clipLineVEs)
                {
                    if (l.MarkedForDelete)
                    {
                        AnimationUtility.SetEditorCurve(clip, l.Line.binding, null);
                    }
                    else
                    {
                        if (l.PathChanged)
                        {
                            AnimationUtility.SetEditorCurve(clip, l.Line.binding, null);
                            l.Line.binding.path = l.NewPath;
                            AnimationUtility.SetEditorCurve(clip, l.Line.binding, l.Line.curve);
                        }
                    }
                }
                EditorUtility.SetDirty(clip);
                AssetDatabase.SaveAssetIfDirty(clip);
            }

            private void OnLineChanged(ChangeEvent<bool> evt)
            {
                applyButton.SetEnabled(clipLineVEs.Any(l => l.PathChanged || l.MarkedForDelete));
            }
        }

        private class ClipLine
        {
            public EditorCurveBinding binding;
            public AnimationCurve curve;
        }

        private class ClipLineVE : VisualElement
        {
            private readonly ClipLine line;
            public readonly TextField path;
            public readonly CurveField curve;
            private readonly Button delete;
            private readonly Color bgColor;

            public bool MarkedForDelete { get; private set; }
            public bool PathChanged => NewPath != line.binding.path;
            public ClipLine Line => line;
            public string NewPath => path.value;

            public ClipLineVE(ClipLine line, int index)
            {
                this.line = line;
                style.flexDirection = FlexDirection.Row;
                Add(new Label($"{index}"));

                path = new TextField() { value = line.binding.path };
                path.RegisterCallback<ChangeEvent<string>>(OnPathChanged);
                path.style.flexGrow = 1;
                Add(path);

                curve = new CurveField() { value = line.curve };
                curve.RegisterCallback<ChangeEvent<AnimationCurve>>(OnCurveChanged);
                curve.style.width = 60;
                Add(curve);

                delete = new Button() { text = "X" };
                delete.RegisterCallback<ClickEvent>(OnDeleteClicked);
                delete.style.display = DisplayStyle.None;
                Add(delete);

                RegisterCallback<FocusOutEvent>(OnLineFocusOut);
                RegisterCallback<FocusInEvent>(OnLineFocusIn);

                bgColor = style.backgroundColor.value;
            }

            private void OnLineFocusOut(FocusOutEvent evt)
            {
                delete.style.display = MarkedForDelete ? DisplayStyle.Flex : DisplayStyle.None;
            }

            private void OnLineFocusIn(FocusInEvent evt)
            {
                delete.style.display = DisplayStyle.Flex;
            }

            private void OnDeleteClicked(ClickEvent evt)
            {
                MarkForDelete(!MarkedForDelete);
                UpdateChangeStatus();
                delete.style.display = MarkedForDelete ? DisplayStyle.Flex : DisplayStyle.None;
            }

            private void OnCurveChanged(ChangeEvent<AnimationCurve> evt)
            {
            }

            private void OnPathChanged(ChangeEvent<string> evt)
            {
                UpdateChangeStatus();
            }

            public void MarkForDelete(bool d)
            {
                MarkedForDelete = d;
                style.backgroundColor = d ? Color.red : bgColor;
            }

            private void UpdateChangeStatus()
            {
                var evt = ChangeEvent<bool>.GetPooled(false, true);
                evt.target = this;
                SendEvent(evt);
            }
        }
    }
}
#endif