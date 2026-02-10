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
    public interface IInspectorModeHelper : System.IDisposable
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

        void System.IDisposable.Dispose()
        {
            Cleanup();
        }

        private void Cleanup()
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

                    using IInspectorModeHelper helper = new InspectorModeHelper(_editor.serializedObject);
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

        void System.IDisposable.Dispose()
        {
        }
    }

    public class EditorSecondHeaderVE : VisualElement
    {
        private readonly Object target;
        private readonly EditorWindow inspectorWindow;
        private readonly IInspectorModeHelper inspectorModeHelper;

        // private MenuItemButton _copyComponentMenuItem;
        // private MenuItemButton _pasteComponentValuesMenuItem;

        public EditorSecondHeaderVE(
            Object target,
            EditorWindow inspectorWindow,
            IInspectorModeHelper inspectorModeHelper = null)
        {
            this.target = target;
            this.inspectorWindow = inspectorWindow;
            this.inspectorModeHelper = inspectorModeHelper;

            var layout_Buttons = this;

            style.flexDirection = FlexDirection.RowReverse;
            style.flexGrow = 1;
            style.marginBottom = 4;
            style.backgroundColor = new Color(0f, 0f, 0f, .2f);
            style.height = EditorGUIUtility.singleLineHeight + 2;

            var button_Browse = new Button { text = "Browse", clickable = new(OpenObjectBrowser) };

            if (target is MonoBehaviour || target is ScriptableObject)
            {
                layout_Buttons.Add(CreateEditScriptButton());
            }

            layout_Buttons.Add(new Button() { text = "Ping", clickable = new(() => EditorGUIUtility.PingObject(target)) });
            layout_Buttons.Add(button_Browse);
            layout_Buttons.Add(CreateOpenInWindowButton());

            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(target)) && (target is GameObject || target is Component))
            {
                layout_Buttons.Add(CreateFindReferencesInSceneButton());
            }

            if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(target)))
            {
                layout_Buttons.Add(CreateFindReferencesInProjectButton());
            }

            if (inspectorModeHelper != null)
            {
                layout_Buttons.Add(CreateDebugButton());
            }

            // IMenuItemObject copy = null;
            // IMenuItemObject paste = null;

            // if (target is ScriptableObject so)
            // {
            //     copy = new FakeMenuItemObject_ScriptableObject_Copy(so, refreshHandler);
            //     paste = new FakeMenuItemObject_ScriptableObject_Paste(so);
            // }
            // else if (target is Component && target is not Transform)
            // {
            //     copy = new MenuItemObject("CONTEXT/Component/Copy Component", target, refreshHandler);
            //     paste = new MenuItemObject("CONTEXT/Component/Paste Component Values", target, refreshHandler);
            // }

            // if (copy != null && paste != null)
            // {
            //     _copyComponentMenuItem = new MenuItemButton(copy, "Copy");
            //     _pasteComponentValuesMenuItem = new MenuItemButton(paste, "Paste");
            //     layout_Buttons.Add(_copyComponentMenuItem);
            //     layout_Buttons.Add(_pasteComponentValuesMenuItem);
            // }

            inspectorWindow.rootVisualElement.RegisterCallback<MouseEnterEvent>(OnRepaint);

            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OpenObjectBrowser()
        {
            EditorWindow.GetWindow<ObjectBrowserWindow>().Browse(target);
        }

        public void TriggerOnAttachToPanel(VisualElement parent)
        {
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            inspectorWindow.rootVisualElement.UnregisterCallback<MouseEnterEvent>(OnRepaint);

            if (inspectorModeHelper != null)
            {
                inspectorModeHelper.Dispose();
            }
        }

        private void OnInspectorModeToggleButtonClicked()
        {
            var mode = inspectorModeHelper.IsDebugMode() ? InspectorMode.Normal : InspectorMode.Debug;
            inspectorModeHelper.SetDebugMode(mode);
        }

        private void OnRepaint(MouseEnterEvent evt)
        {
            Refresh();
        }

        public void Refresh()
        {
            // _copyComponentMenuItem?.Refresh();
            // _pasteComponentValuesMenuItem?.Refresh();
        }

        private VisualElement CreateDebugButton()
        {
            return new ToggleButton2(
                "Normal", "Debug", Color.cyan * .8f,
                inspectorModeHelper.IsDebugMode,
                OnInspectorModeToggleButtonClicked,
                "InspectorExtensions_ToggleButton_InspectorMode",
                inspectorWindow);
        }

        private VisualElement CreateEditScriptButton()
        {
            return new Button()
            {
                text = "Edit Script",
                clickable = new(OnEditScriptButtonClicked),
            };
        }

        private VisualElement CreateOpenInWindowButton()
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

        private void OnEditScriptButtonClicked()
        {
            if (target != null)
            {
                var serialized = new SerializedObject(target);
                var scriptProperty = serialized.FindProperty("m_Script");
                AssetDatabase.OpenAsset(scriptProperty.objectReferenceValue);
            }
        }

        private VisualElement CreateFindReferencesInSceneButton()
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

        private void OnFindReferencesInSceneClicked()
        {
            EditorWindow.GetWindow<SceneReferencesFinderWindow>().Find(target);
        }

        private VisualElement CreateFindReferencesInProjectButton()
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

        private void OnFindReferencesInProjectClicked()
        {
            typeof(SearchableEditorWindow)
                .GetMethod("SearchForReferencesInProject", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { target });
        }

        private class MenuItemButton : Button
        {
            private readonly IMenuItemObject menuItemObject;

            public MenuItemButton(IMenuItemObject menuItemObject, string displayText)
            {
                this.menuItemObject = menuItemObject;

                text = displayText;
                clicked += OnButtonClicked;

                Refresh();
            }

            private void OnButtonClicked()
            {
                menuItemObject.Execute();
            }

            public void Refresh()
            {
                var value = menuItemObject.IsEnabled();
                style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private class ReferencesFoldout : Foldout
        {
            public ReferencesFoldout(UnityEngine.Object obj)
            {
                var serializedObject = new SerializedObject(obj);
                serializedObject.Update();
                var references = Iterate(serializedObject).Select(o => new { o.propertyPath, o.objectReferenceValue }).ToArray();
                var count = references.Length;

                text = $"References ({count})";
                value = false;
                style.color = Color.gray;
                style.borderTopWidth = 1;
                style.borderTopColor = new Color(.1f, .1f, .1f, 1f);

                if (InspectorExtensionInstaller.Instance.DebugEnabled)
                {
                    Debug.Log($"RevealReferenceEditorExt for {obj.name} ({obj.GetType().Name})");
                }


                foreach (var rObject in references)
                {
                    var foldout = new ObjectField()
                    {
                        label = $"{rObject.propertyPath}: ",
                        value = rObject.objectReferenceValue,
                        // tooltip = $"{r.propertyPath}"
                    };

                    Add(foldout);
                }
            }
        }

        private static IEnumerable<SerializedProperty> Iterate(SerializedObject obj)
        {
            var it = obj.GetIterator();
            while (it.Next(true))
            {
                if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue != null)
                {
                    yield return it;
                }
            }
        }

        public interface IMenuItemObject
        {
            bool IsEnabled();
            void Execute();
        }

        public class FakeMenuItemObject_ScriptableObject_Paste : IMenuItemObject
        {
            private readonly ScriptableObject target;

            public FakeMenuItemObject_ScriptableObject_Paste(ScriptableObject target)
            {
                this.target = target;
            }

            void IMenuItemObject.Execute()
            {
                EditorUtility.CopySerialized(ScriptableObjectInspectorExt.CopiedObject, target);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            bool IMenuItemObject.IsEnabled()
            {
                return ScriptableObjectInspectorExt.CopiedObject != null
                && target.GetType().IsAssignableFrom(ScriptableObjectInspectorExt.CopiedObject.GetType());
            }
        }

        public class FakeMenuItemObject_ScriptableObject_Copy : IMenuItemObject
        {
            private readonly ScriptableObject target;
            private readonly IRefreshHandler refreshHandler;

            public FakeMenuItemObject_ScriptableObject_Copy(ScriptableObject target, IRefreshHandler refreshHandler)
            {
                this.target = target;
                this.refreshHandler = refreshHandler;
            }

            void IMenuItemObject.Execute()
            {
                ScriptableObjectInspectorExt.CopiedObject = target;
                refreshHandler.Refresh();
            }

            bool IMenuItemObject.IsEnabled()
            {
                return true;
            }
        }

        public class MenuItemObject : IMenuItemObject
        {
            private readonly string menuItemPath;
            private readonly Object context;
            private readonly IRefreshHandler refreshHandler;

            public MenuItemObject(string menuItemPath, UnityEngine.Object context, IRefreshHandler refreshHandler)
            {
                this.menuItemPath = menuItemPath;
                this.context = context;
                this.refreshHandler = refreshHandler;
            }

            bool IMenuItemObject.IsEnabled()
            {
                return EditorApplicationHelper.GetEnabledWithContext(menuItemPath, context);
            }

            void IMenuItemObject.Execute()
            {
                EditorApplicationHelper.ExecuteMenuItem(menuItemPath, context);
                refreshHandler.Refresh();
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

                scrollView.Add(new EditorSecondHeaderVE(target, this, new InspectorModeHelper(editor.serializedObject)));

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