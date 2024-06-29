#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System;
using Unity.EditorCoroutines.Editor;
using System.Collections;

namespace InspectorExtensions
{
    public class InspectorExtensionInstaller
    {
        private static InspectorExtensionInstaller _instance;
        public static InspectorExtensionInstaller Instance => _instance ??= new();

        private static BindingFlags BindingFlags => BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

        private EditorWindow _inspectorWindow;
        private EditorWindow InspectorWindow
        {
            get
            {
                if (_inspectorWindow == null)
                {
                    var windows = Resources.FindObjectsOfTypeAll(typeof(EditorWindow)).OfType<EditorWindow>();
                    _inspectorWindow = windows.FirstOrDefault(w => w.GetType().FullName == "UnityEditor.InspectorWindow");
                }
                return _inspectorWindow;
            }
        }

        private readonly List<IInspectorExtension> _inspectorExtensions = new();
        private InspectorExtensionHeader _header;

        public IReadOnlyList<IInspectorExtension> InspectorExtensions => _inspectorExtensions;

        public void Init()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;

            EditorApplication.playModeStateChanged -= OnEditorPlaymodeChanged;
            EditorApplication.playModeStateChanged += OnEditorPlaymodeChanged;

            EditorCoroutineUtility.StartCoroutine(WaitForNextFrame(TryModify), this);

            _inspectorWindow = null;
        }

        private void OnEditorPlaymodeChanged(PlayModeStateChange obj)
        {
            TryModify();
        }

        private void OnSelectionChanged()
        {
            TryModify();
        }

        public void AddExtension(IInspectorExtension extension)
        {
            _inspectorExtensions.Add(extension);
        }

        public void TryModify()
        {
            if (InspectorWindow != null)
            {
                InsertInspectorExtensions(InspectorWindow.rootVisualElement);
            }
        }

        private void InsertInspectorExtensions(VisualElement rootVisualElement)
        {
            if (_header == null)
            {
                _header = new InspectorExtensionHeader();

                var mainContainer = rootVisualElement.Query<VisualElement>(null, "unity-inspector-main-container").First();
                mainContainer.Insert(0, _header);
            }
            else
            {
                _header.ClearExtensions();
            }

            foreach (var e in _inspectorExtensions)
            {
                e.CleanUp();
            }

            var veContainer = rootVisualElement.Query<VisualElement>(null, "unity-inspector-editors-list").First();
            TryApplyExtensionsToInspector(_header, veContainer, _inspectorExtensions);

            _header.ApplyToggleButton();
        }

        public void TryApplyExtensionsToInspector(InspectorExtensionHeader header, VisualElement veContainer, IEnumerable<IInspectorExtension> inspectorExts)
        {
            if (veContainer != null)
            {
                var editorElements = veContainer.Children().Where(ve => ve.GetType().FullName == "UnityEditor.UIElements.EditorElement");

                foreach (var editorElement in editorElements)
                {
                    var targetEditor = editorElement.GetType().GetProperty("editor", BindingFlags).GetValue(editorElement) as Editor;
                    var extensionContainer = AddContainerElement(editorElement);
                    var extensionElements = CreateExtensionElements(targetEditor.target, inspectorExts);

                    foreach (var element in extensionElements)
                    {
                        extensionContainer.Add(element);
                        header.AddExtension(element);
                    }
                }
            }
        }

        private static IEnumerable<InspectorExtensionElement> CreateExtensionElements(UnityEngine.Object target, IEnumerable<IInspectorExtension> inspectorExts)
        {
            var memberInfos = IterateMembers(target.GetType());

            var attributeExts = inspectorExts.Where(e => e.ExtensionType == ExtensionType.Attribute).ToArray();

            var maes = memberInfos.OrderBy(mi => mi is FieldInfo ? 0 : (mi is PropertyInfo ? 1 : 2))
                .SelectMany(mi => mi.GetCustomAttributes()
                    .Select(a => new { extension = attributeExts.FirstOrDefault(e => e.TargetType.IsInstanceOfType(a)), attribute = a })
                    .Where(ea => ea.extension != null)
                    .Select(a => new { memberInfo = mi, a.attribute, a.extension })).ToArray();

            foreach (var mae in maes)
            {
                var element = new InspectorExtensionElement(target, mae.memberInfo, mae.attribute)
                { name = mae.memberInfo.Name };
                mae.extension.ModifyExtensionElement(element);

                yield return element;
            }

            var exts = inspectorExts
                .Where(e => e.ExtensionType == ExtensionType.Object)
                .Where(e => e.TargetType.IsInstanceOfType(target));

            foreach (var ext in exts)
            {
                var element2 = new InspectorExtensionElement(target, null, null) { name = target.GetType().Name };
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

        private static VisualElement AddContainerElement(VisualElement editorElement)
        {
            var existing = editorElement.Q<InspectorExtsContainer>("inspector-extensions");
            if (existing != null)
            {
                editorElement.Remove(existing);
            }
            var extensionContainer = new InspectorExtsContainer() { name = "inspector-extensions" };
            editorElement.Add(extensionContainer);

            var inspectorElement = editorElement.Children().FirstOrDefault(e => e.GetType().FullName == "UnityEditor.UIElements.InspectorElement");
            if (inspectorElement != null)
            {
                extensionContainer.PlaceInFront(inspectorElement);
            }

            return extensionContainer;
        }

        private IEnumerator WaitForNextFrame(Action callback)
        {
            yield return new WaitForEndOfFrame();
            callback?.Invoke();
        }

        private class InspectorExtsContainer : VisualElement
        {
        }
    }
}

#endif