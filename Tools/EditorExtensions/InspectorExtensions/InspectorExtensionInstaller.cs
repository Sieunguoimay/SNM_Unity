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
    public class InspectorExtensionInstaller : IExtensionElementProvider
    {
        private static InspectorExtensionInstaller _instance;
        public static InspectorExtensionInstaller Instance => _instance ??= new();

        private EditorWindow _inspectorWindow;
        public EditorWindow InspectorWindow
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
        private readonly List<InspectorExtensionElementObject> _inspectorExtensionElements = new();
        private InspectorExtensionHeaderVE _header;
        private EditorCoroutine _nextFrameCoroutine;

        private Action<IExtensionElementProvider> _onExtensionElementsChanged;
        event Action<IExtensionElementProvider> IExtensionElementProvider.OnExtensionElementsChanged
        {
            add { _onExtensionElementsChanged += value; }
            remove { _onExtensionElementsChanged -= value; }
        }

        public bool DebugEnabled
        {
            get => EditorPrefs.GetBool("InspectorExtensionInstaller_DebugEnabled", false);
            set => EditorPrefs.SetBool("InspectorExtensionInstaller_DebugEnabled", value);
        }

        private InspectorExtensionInstaller()
        {
            if (DebugEnabled)
            {
                Debug.Log("InspectorExtensionInstaller created");
            }
        }

        ~InspectorExtensionInstaller()
        {
            Teardown();

            if (DebugEnabled)
            {
                Debug.Log("InspectorExtensionInstaller destroyed");
            }
        }

        public void InjectExtensions(params IInspectorExtension[] extensions)
        {
            _inspectorExtensions.Clear();
            _inspectorExtensions.AddRange(extensions);
        }

        public void Setup()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;

            EditorApplication.playModeStateChanged -= OnEditorPlaymodeChanged;
            EditorApplication.playModeStateChanged += OnEditorPlaymodeChanged;

            if (_nextFrameCoroutine != null)
            {
                EditorCoroutineUtility.StopCoroutine(_nextFrameCoroutine);
            }
            _nextFrameCoroutine = EditorCoroutineUtility.StartCoroutine(WaitForNextFrame(TryModify), this);

            _inspectorWindow = null;
        }


        public void Teardown()
        {
            if (_header != null)
            {
                _header.Dispose();
            }

            foreach (var e in _inspectorExtensions)
            {
                e.CleanUp();
            }

            _inspectorExtensions.Clear();

            foreach (var e in _inspectorExtensionElements)
            {
                e.parent?.Remove(e.element);
            }

            _inspectorExtensionElements.Clear();
        }

        private void OnEditorPlaymodeChanged(PlayModeStateChange obj)
        {
            TryModify();
        }

        private void OnSelectionChanged()
        {
            TryModify();
        }

        public void Refresh()
        {
            TryModify(false);
        }

        public void TryModify() => TryModify(true);
        private void TryModify(bool newSession)
        {
            if (InspectorWindow != null)
            {
                if (newSession)
                {

                    foreach (var e in _inspectorExtensions)
                    {
                        e.CleanUp();
                    }

                }
                InsertHeader(InspectorWindow.rootVisualElement);
                InsertInspectorExtensions();
            }
        }

        private void InsertInspectorExtensions()
        {
            foreach (var e in _inspectorExtensionElements)
            {
                if (e.parent != null && e.parent.Contains(e.element))
                {
                    e.parent.Remove(e.element);
                }
            }

            _inspectorExtensionElements.Clear();

            _inspectorExtensionElements.AddRange(CreateExtensionsForAllInspectors(_inspectorExtensions));

            foreach (var e in _inspectorExtensionElements)
            {
                e.parent.Add(e.element);
            }

            _onExtensionElementsChanged?.Invoke(this);
        }

        private void InsertHeader(VisualElement rootVisualElement)
        {
            _header = new InspectorExtensionHeaderVE(this);
            var mainContainer = rootVisualElement.Query<VisualElement>(null, "unity-inspector-main-container").First();
            var found = mainContainer.Query<InspectorExtensionHeaderVE>();
            if (found != null && mainContainer.Contains(found))
            {
                mainContainer.Remove(found);
            }
            mainContainer.Insert(0, _header);
        }

        public VisualElement GetEditorContainerVE()
        {
            var rootVisualElement = InspectorWindow.rootVisualElement;
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

        private static IEnumerable<InspectorExtensionElement> CreateInspectorExtensionElementsForObject(UnityEditor.Editor editor, VisualElement editorVE, IEnumerable<IInspectorExtension> inspectorExts)
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
                var element = new InspectorExtensionElement_MemberInfo(editor, mae.memberInfo, mae.attribute, mae.extension)
                { name = mae.memberInfo.Name };
                mae.extension.ModifyExtensionElement(element);

                yield return element;
            }

            var exts = inspectorExts
                .Where(e => e.ExtensionType == ExtensionType.Object)
                .Where(e => e.IsSupportedFor(target));

            foreach (var ext in exts)
            {
                var element2 = new InspectorExtensionElement(editor, editorVE, null, ext) { name = editor.target.GetType().Name };
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

        private IEnumerator WaitForNextFrame(Action callback)
        {
            yield return new WaitForEndOfFrame();
            callback?.Invoke();
        }

        IEnumerable<InspectorExtensionElement> IExtensionElementProvider.GetExtensionElements()
        {
            return _inspectorExtensionElements.Select(e => e.element);
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