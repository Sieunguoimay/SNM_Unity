#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Snm.Tools.ObjectBrowser;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Tools.InspectorExtra
{
    public interface IInspectorModeHelper
    {
        Object Target { get; }
        event System.Action<IInspectorModeHelper> OnModeChanged;
        void SetDebugMode(InspectorMode mode);
        bool IsDebugMode();
    }

    public class InspectorModeHelper_DebugEditor : IInspectorModeHelper
    {
        private readonly InspectorExtensionElement extensionVE;
        private IMGUIContainer _editorVE;
        private InspectorElement _inspectorElement;
        private readonly int index;
        private Editor _editor;
        private InspectorMode _mode;
        Object IInspectorModeHelper.Target => extensionVE.Target as Object;
        private System.Action<IInspectorModeHelper> _onModeChanged;

        event System.Action<IInspectorModeHelper> IInspectorModeHelper.OnModeChanged
        {
            add => _onModeChanged += value;
            remove => _onModeChanged -= value;
        }

        public InspectorModeHelper_DebugEditor(InspectorExtensionElement extensionVE)
        {
            this.extensionVE = extensionVE;
            _inspectorElement = extensionVE.EditorVE.Q<InspectorElement>();
            index = extensionVE.EditorVE.IndexOf(_inspectorElement);
        }

        public void Cleanup()
        {
            if (_inspectorElement != null)
            {
                AddToEditorVEAtInitialIndex(_inspectorElement);
            }
            if (_editor != null)
            {
                _editor = null;
                Object.DestroyImmediate(_editor);
            }
            if (_editorVE != null)
            {
                _editorVE.RemoveFromHierarchy();
                _editorVE = null;
            }
        }

        bool IInspectorModeHelper.IsDebugMode()
        {
            return _mode == InspectorMode.Debug;
        }

        void IInspectorModeHelper.SetDebugMode(InspectorMode mode)
        {
            if (extensionVE != null && extensionVE.EditorVE != null)
            {
                if (mode == InspectorMode.Debug)
                {
                    _editor = Editor.CreateEditor(extensionVE.Target as Object);

                    IInspectorModeHelper helper = new InspectorModeHelper(_editor.serializedObject);
                    helper.SetDebugMode(InspectorMode.Debug);

                    _editorVE = new IMGUIContainer(OnIMGUI);
                    AddToEditorVEAtInitialIndex(_editorVE);

                    _editorVE.style.marginLeft = 20;
                    _editorVE.style.marginRight = 5;
                    _inspectorElement?.RemoveFromHierarchy();
                }
                else
                {
                    Cleanup();
                }
            }

            _mode = mode;
            _onModeChanged?.Invoke(this);
        }

        private void AddToEditorVEAtInitialIndex(VisualElement ve)
        {
            if (extensionVE == null || extensionVE.EditorVE == null || ve == null
                || extensionVE.parent == null || extensionVE.EditorVE.parent == null)
            {
                return;
            }

            if (index >= 0 && index < extensionVE.EditorVE.childCount)
            {
                try
                {
                    extensionVE.EditorVE.Insert(index, ve);
                }
                catch (System.Exception ex)
                {
                    Debug.Log($"Error adding to EditorVE {ex.Message}");
                }
            }
            else
            {
                try
                {
                    extensionVE.EditorVE.Add(ve);
                }
                catch (System.Exception ex)
                {
                    Debug.Log($"Error adding to EditorVE {ex.Message}");
                }
            }

        }

        private void OnIMGUI()
        {
            if (_editor != null)
            {
                try
                {
                    _editor.OnInspectorGUI();
                }
                catch (System.Exception)
                {
                }
            }
        }
    }

    public class InspectorModeHelper : IInspectorModeHelper
    {
        private readonly PropertyInfo inspectorMode;
        private readonly SerializedObject serializedObject;

        Object IInspectorModeHelper.Target => serializedObject.targetObject;

        private System.Action<IInspectorModeHelper> _onModeChanged;
        event System.Action<IInspectorModeHelper> IInspectorModeHelper.OnModeChanged
        {
            add => _onModeChanged += value;
            remove => _onModeChanged -= value;
        }

        public InspectorModeHelper(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            inspectorMode = typeof(SerializedObject)
                .GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        void IInspectorModeHelper.SetDebugMode(InspectorMode mode)
        {
            inspectorMode.SetValue(serializedObject, mode);
            _onModeChanged?.Invoke(this);
        }

        bool IInspectorModeHelper.IsDebugMode()
        {
            return ((InspectorMode)inspectorMode.GetValue(serializedObject)) == InspectorMode.Debug;
        }
    }

    public class EditorSecondHeaderVECreator
    {
        public static VisualElement Create(
            Object target,
            EditorWindow inspectorWindow,
            IInspectorModeHelper inspectorModeHelper)
        {
            var secondHeader = new EditorSecondHeader(target, inspectorWindow, inspectorModeHelper);
            var layout_Buttons = new VisualElement()
            {
                style = {
                    flexDirection = FlexDirection.RowReverse,
                    flexGrow = 1,
                    marginBottom = 4,
                    backgroundColor = new Color(0f, 0f, 0f, .2f),
                    height = EditorGUIUtility.singleLineHeight + 2,
                }
            };

            var button_Browse = new Button { text = "Browse", clickable = new(secondHeader.OpenObjectBrowser) };

            if (target is MonoBehaviour || target is ScriptableObject)
            {
                layout_Buttons.Add(secondHeader.CreateEditScriptButton());
            }

            layout_Buttons.Add(new Button() { text = "Ping", clickable = new(() => EditorGUIUtility.PingObject(target)) });
            layout_Buttons.Add(button_Browse);
            layout_Buttons.Add(secondHeader.CreateOpenInWindowButton());

            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(target)) && (target is GameObject || target is Component))
                layout_Buttons.Add(secondHeader.CreateFindReferencesInSceneButton());

            if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(target)))
                layout_Buttons.Add(secondHeader.CreateFindReferencesInProjectButton());

            if (inspectorModeHelper != null)
                layout_Buttons.Add(secondHeader.CreateDebugButton());

            return layout_Buttons;
        }

        private class EditorSecondHeader
        {
            private readonly Object target;
            private readonly EditorWindow inspectorWindow;
            private readonly IInspectorModeHelper inspectorModeHelper;

            public EditorSecondHeader(
                Object target,
                EditorWindow inspectorWindow,
                IInspectorModeHelper inspectorModeHelper
            )
            {
                this.target = target;
                this.inspectorWindow = inspectorWindow;
                this.inspectorModeHelper = inspectorModeHelper;
            }

            public void OpenObjectBrowser()
            {
                EditorWindow.GetWindow<ObjectBrowserWindow>().Browse(target);
            }

            public void OnInspectorModeToggleButtonClicked()
            {
                var mode = inspectorModeHelper.IsDebugMode() ? InspectorMode.Normal : InspectorMode.Debug;
                inspectorModeHelper.SetDebugMode(mode);
            }

            public VisualElement CreateDebugButton()
            {
                return new ToggleButton2(
                    "Normal", "Debug", Color.cyan * .8f,
                    inspectorModeHelper.IsDebugMode,
                    OnInspectorModeToggleButtonClicked,
                    "InspectorExtensions_ToggleButton_InspectorMode",
                    inspectorWindow);
            }

            public VisualElement CreateEditScriptButton()
            {
                return new Button()
                {
                    text = "Edit Script",
                    clickable = new(OnEditScriptButtonClicked),
                };
            }

            public VisualElement CreateOpenInWindowButton()
            {
                return new Button()
                {
                    tooltip = "Open in Window",
                    text = "Window",
                    clickable = new(() => EditorPopupWindow.Open(target)),
#if UNITY_2023_2_OR_NEWER
                iconImage = Background.FromTexture2D((Texture2D)EditorGUIUtility.IconContent("d_ScaleTool").image)
#endif
                };
            }

            void OnEditScriptButtonClicked()
            {
                if (target != null)
                {
                    var serialized = new SerializedObject(target);
                    var scriptProperty = serialized.FindProperty("m_Script");
                    AssetDatabase.OpenAsset(scriptProperty.objectReferenceValue);
                }
            }

            public VisualElement CreateFindReferencesInSceneButton()
            {
                var button = new Button()
                {
                    tooltip = "Find References in Scene",
                    text = "Scene",
                    clickable = new(OnFindReferencesInSceneClicked),
#if UNITY_2023_2_OR_NEWER
                iconImage = Background.FromTexture2D((Texture2D)EditorGUIUtility.IconContent("d_Search Icon").image)
#endif
                };
#if UNITY_2023_2_OR_NEWER
            var image = button.Q<Image>();
            image.style.height = 17;
            image.style.width = 17;
#endif
                return button;
            }

            public void OnFindReferencesInSceneClicked()
            {
                EditorWindow.GetWindow<SceneReferencesFinderWindow>().Find(target);
            }

            public VisualElement CreateFindReferencesInProjectButton()
            {
                Background background = Background.FromTexture2D((Texture2D)EditorGUIUtility.IconContent("d_Search Icon").image);
                var button = new Button(OnFindReferencesInProjectClicked)
                {
                    tooltip = "Find References in Project",
                    text = "Project",
#if UNITY_2023_2_OR_NEWER
                iconImage = background
#endif
                };
#if UNITY_2023_2_OR_NEWER
            var image = button.Q<Image>();
            image.style.height = 17;
            image.style.width = 17;
#endif
                return button;
            }

            public void OnFindReferencesInProjectClicked()
            {
                typeof(SearchableEditorWindow)
                    .GetMethod("SearchForReferencesInProject", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, new object[] { target });
            }
        }

        private class EditorPopupWindow : EditorWindow, IRefreshHandler
        {
            [SerializeField] private Object target;
            [SerializeField] private Editor editor;

            private static Object _target;

            public static void Open(Object target)
            {
                _target = target;
                var window = GetWindow<EditorPopupWindow>();
                window.ShowPopup();
            }

            public void CreateGUI()
            {
                target = _target;
                if (target == null) return;

                editor = Editor.CreateEditor(target);
                if (editor == null) return;

                titleContent = new GUIContent(target.name);

                VisualElement horizontal;
                rootVisualElement.Add(horizontal = new());
                horizontal.style.flexDirection = FlexDirection.Row;

                ScrollView scrollView;
                rootVisualElement.Add(scrollView = new());
                scrollView.style.flexGrow = 1;

                scrollView.Add(EditorSecondHeaderVECreator.Create(target, this, new InspectorModeHelper(editor.serializedObject)));

                VisualElement space;
                horizontal.Add(space = new());
                space.style.flexGrow = 1;

                horizontal.Add(new Button(() =>
                {
                    EditorGUIUtility.PingObject(target);
                })
                { text = "Ping" });

                horizontal.Add(new Button(() =>
                {
                    Selection.activeObject = target;
                })
                { text = "Select" });

                IMGUIContainer editorVE;
                scrollView.Add(editorVE = new IMGUIContainer(() =>
                {
                    if (editor == null || editor.serializedObject == null || editor.serializedObject.targetObject == null) return;
                    editor.OnInspectorGUI();
                }));

                editorVE.style.marginLeft = 10f;
                editorVE.style.marginRight = 5f;
            }

            void IRefreshHandler.Refresh()
            {
            }
        }
    }
}

#endif