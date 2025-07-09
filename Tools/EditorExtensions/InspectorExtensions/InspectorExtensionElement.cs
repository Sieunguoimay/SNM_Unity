#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtra
{
    public class InspectorExtensionElement : VisualElement
    {
        private readonly EditorWindow inspectorWindow;
        private readonly IRefreshHandler refreshHandler;
        private readonly object target;
        private readonly Editor editor;
        private readonly VisualElement editorVE;
        private readonly System.Attribute attribute;
        private readonly IInspectorExtension extension;

        public EditorWindow InspectorWindow => inspectorWindow;
        public IRefreshHandler RefreshHandler => refreshHandler;
        public object Target => target;
        public Editor Editor => editor;
        public System.Attribute Attribute => attribute;
        public IInspectorExtension Extension => extension;
        public VisualElement EditorVE => editorVE;

        public InspectorExtensionElement(
            Editor editor,
            VisualElement editorVE,
            Attribute attribute,
            IInspectorExtension extension,
            EditorWindow inspectorWindow,
            IRefreshHandler refreshHandler)
            : this(editor, attribute, extension, inspectorWindow, refreshHandler)
        {
            this.editorVE = editorVE;
        }

        public InspectorExtensionElement(
            Editor editor,
            Attribute attribute,
            IInspectorExtension extension,
            EditorWindow inspectorWindow,
            IRefreshHandler refreshHandler)
            : this(editor.target, attribute, extension, inspectorWindow, refreshHandler)
        {
            this.editor = editor;
        }

        public InspectorExtensionElement(
            object target,
            Attribute attribute,
            IInspectorExtension extension,
            EditorWindow inspectorWindow,
            IRefreshHandler refreshHandler)
        {
            this.target = target;
            this.attribute = attribute;
            this.extension = extension;
            this.inspectorWindow = inspectorWindow;
            this.refreshHandler = refreshHandler;
        }
    }

    public class InspectorExtensionElement_MemberInfo : InspectorExtensionElement
    {
        private readonly MemberInfo memberInfo;
        public MemberInfo MemberInfo => memberInfo;

        public InspectorExtensionElement_MemberInfo(
            Editor editor,
            MemberInfo memberInfo,
            Attribute attribute,
            IInspectorExtension extension,
            EditorWindow inspectorWindow,
            IRefreshHandler refreshHandler)
            : this(editor.target, memberInfo, attribute, extension, inspectorWindow, refreshHandler)
        {
        }

        public InspectorExtensionElement_MemberInfo(
            object target,
            MemberInfo memberInfo,
            Attribute attribute,
            IInspectorExtension extension,
            EditorWindow inspectorWindow,
            IRefreshHandler refreshHandler)
            : base(target, attribute, extension, inspectorWindow, refreshHandler)
        {
            this.memberInfo = memberInfo;
        }
    }

    public enum ExtensionPosition
    {
        Top,
        Bottom
    }
}

#endif