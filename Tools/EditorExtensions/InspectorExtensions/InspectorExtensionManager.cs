#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System;

namespace Snm.Tools.InspectorExtra
{

    public interface IRefreshHandler
    {
        void Refresh();
    }

    public class InspectorExtensionManager : IExtensionElementProvider, IRefreshHandler
    {
        private readonly EditorWindow inspectorWindow;
        private readonly List<IInspectorExtension> _inspectorExtensions = new();
        private readonly List<InspectorExtensionElementObject> _inspectorExtensionElements = new();
        private InspectorExtensionHeaderVE _header;
        private Action<IExtensionElementProvider> _onExtensionElementsChanged;

        public EditorWindow InspectorWindow => inspectorWindow;

        event Action<IExtensionElementProvider> IExtensionElementProvider.OnExtensionElementsChanged
        {
            add { _onExtensionElementsChanged += value; }
            remove { _onExtensionElementsChanged -= value; }
        }

        public InspectorExtensionManager(EditorWindow inspectorWindow, IEnumerable<IInspectorExtension> inspectorExtensions)
        {
            this.inspectorWindow = inspectorWindow;
            _inspectorExtensions.AddRange(inspectorExtensions);
        }

        public void SetupExtensions()
        {
            InsertHeader();
            InsertInspectorExtensions();
            
            if (InspectorExtensionInstaller.Instance.DebugEnabled)
            {
                Debug.Log($"InspectorExtensionManager {GetHashCode()} Setup");
            }
        }

        public void TeardownExtensions(bool clearStaticData = true)
        {
            if (_header != null)
            {
                _header.RemoveFromHierarchy();
                _header = null;
            }

            if (clearStaticData)
            {
                foreach (var e in _inspectorExtensions)
                {
                    e.CleanUpStaticData();
                }
            }

            foreach (var e in _inspectorExtensionElements)
            {
                e.element.RemoveFromHierarchy();
            }

            _inspectorExtensionElements.Clear();

            if (InspectorExtensionInstaller.Instance.DebugEnabled)
            {
                Debug.Log($"InspectorExtensionManager {GetHashCode()} Teardown");
            }
        }
        private void InsertInspectorExtensions()
        {
            _inspectorExtensionElements.AddRange(CreateExtensionsForAllInspectors(_inspectorExtensions));

            foreach (var e in _inspectorExtensionElements)
            {
                e.parent.Add(e.element);
            }

            _onExtensionElementsChanged?.Invoke(this);
        }

        private void InsertHeader()
        {
            _header = new InspectorExtensionHeaderVE(this, inspectorWindow, this);
            var mainContainer = inspectorWindow.rootVisualElement.Query<VisualElement>(null, "unity-inspector-main-container").First();
            var found = mainContainer.Query<InspectorExtensionHeaderVE>();
            if (found != null && mainContainer.Contains(found))
            {
                mainContainer.Remove(found);
            }
            mainContainer.Insert(0, _header);
        }

        public VisualElement GetEditorContainerVE()
        {
            var rootVisualElement = inspectorWindow.rootVisualElement;
            var veContainer = rootVisualElement.Query<VisualElement>(null, "unity-inspector-editors-list").First();
            return veContainer;
        }

        public IEnumerable<InspectorExtensionElementObject> CreateExtensionsForAllInspectors(IEnumerable<IInspectorExtension> inspectorExts)
        {
            const BindingFlags BindingFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

            var veContainer = GetEditorContainerVE();

            if (veContainer != null)
            {
                var editorElements = veContainer.Children().Where(ve => ve.GetType().FullName == "UnityEditor.UIElements.EditorElement");

                foreach (var editorElement in editorElements)
                {
                    SetupContainerElement(editorElement, out var topVE, out var bottomVE);

                    var targetEditor = editorElement.GetType().GetProperty("editor", BindingFlags).GetValue(editorElement) as Editor;

                    var extensionElements = CreateInspectorExtensionElementsForObject(targetEditor, editorElement, inspectorExts)
                        .OrderBy(e => e.Extension.Priority);

                    foreach (var element in extensionElements)
                    {
                        var parent = element.Extension.Position == ExtensionPosition.Bottom ? bottomVE : topVE;

                        yield return new()
                        {
                            element = element,
                            parent = parent
                        };
                    }
                }
            }
        }

        private IEnumerable<InspectorExtensionElement> CreateInspectorExtensionElementsForObject(Editor editor, VisualElement editorVE, IEnumerable<IInspectorExtension> inspectorExts)
        {
            var target = editor.target;
            var memberInfos = IterateMembers(target.GetType());

            var attributeExts = inspectorExts.Where(e => e.ExtensionType == ExtensionType.Attribute).ToArray();

            var maes = memberInfos.OrderBy(mi => mi is FieldInfo ? 0 : (mi is PropertyInfo ? 1 : 2))
                .SelectMany(mi => mi.GetCustomAttributes()
                    .Select(a => new { extension = attributeExts.FirstOrDefault(e => e.IsSupportedFor(a)), attribute = a })
                    .Where(ea => ea.extension != null)
                    .Select(a => new { memberInfo = mi, a.attribute, a.extension })).ToArray();

            foreach (var mae in maes)
            {
                var element = new InspectorExtensionElement_MemberInfo(editor, mae.memberInfo, mae.attribute, mae.extension, inspectorWindow, this)
                { name = mae.memberInfo.Name };
                mae.extension.ModifyExtensionElement(element);

                yield return element;
            }

            var exts = inspectorExts
                .Where(e => e.ExtensionType == ExtensionType.Object)
                .Where(e => e.IsSupportedFor(target));

            foreach (var ext in exts)
            {
                var element2 = new InspectorExtensionElement(editor, editorVE, null, ext, inspectorWindow, this) { name = editor.target.GetType().Name };
                ext.ModifyExtensionElement(element2);
                yield return element2;
            }
        }

        private static IEnumerable<MemberInfo> IterateMembers(Type type)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.NonPublic;
            var current = type;
            while (current != null)
            {
                foreach (var mi in current.GetMembers(flags))
                {
                    yield return mi;
                }
                current = current.BaseType;
            }
        }

        private static void SetupContainerElement(VisualElement editorElement, out VisualElement topVE, out VisualElement bottomVE)
        {
            var existings = editorElement.Query<InspectorExtsContainer>(className: "inspector-extensions").Build();

            foreach (var existing in existings)
            {
                editorElement.Remove(existing);
            }

            topVE = new InspectorExtsContainer() { name = "inspector-extensions-top" };
            bottomVE = new InspectorExtsContainer() { name = "inspector-extensions-bottom" };

            topVE.AddToClassList("inspector-extensions");
            bottomVE.AddToClassList("inspector-extensions");

            editorElement.Add(topVE);
            editorElement.Add(bottomVE);

            var inspectorElement = editorElement.Children().FirstOrDefault(e => e.GetType().FullName == "UnityEditor.UIElements.InspectorElement");
            if (inspectorElement != null)
            {
                topVE.PlaceBehind(inspectorElement);
                bottomVE.PlaceInFront(inspectorElement);
            }
        }

        IEnumerable<InspectorExtensionElement> IExtensionElementProvider.GetExtensionElements()
        {
            return _inspectorExtensionElements.Select(e => e.element);
        }

        void IRefreshHandler.Refresh()
        {
            TeardownExtensions(false);
            SetupExtensions();
        }

        private class InspectorExtsContainer : VisualElement
        {
        }

        public class InspectorExtensionElementObject
        {
            public InspectorExtensionElement element;
            public VisualElement parent;
        }
    }
}

#endif