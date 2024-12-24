#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace InspectorExtensions
{
    public class ScriptableObjectInspectorExt : IInspectorExtension
    {
        public static UnityEngine.Object CopiedObject;
        private static UnityEngine.Object[] _copiedObjects;

        ExtensionType IInspectorExtension.ExtensionType => ExtensionType.Object;
        ExtensionPosition IInspectorExtension.Position => ExtensionPosition.Bottom;
        int IInspectorExtension.Priority => 0;
        bool IInspectorExtension.IsSupportedFor(object target) => target is ScriptableObject;

        private readonly Dictionary<UnityEngine.Object, ObjectData> objectStates = new();

        public class ObjectData
        {
            public bool foldout;
            public bool debug;
        }

        void IInspectorExtension.CleanUp()
        {
            objectStates.Clear();
        }

        void IInspectorExtension.ModifyExtensionElement(InspectorExtensionElement extensionElement)
        {
            if (extensionElement.Target is ScriptableObject so && AssetDatabase.IsMainAsset(so))
            {
                extensionElement.style.marginTop = 15;

                var toolbar = new Toolbar(so);
                extensionElement.Add(toolbar);

                var allAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(so)).Where(o => o != so).OrderBy(o => o?.name);
                foreach (var asset in allAssets)
                {
                    if (!objectStates.ContainsKey(asset))
                    {
                        objectStates.Add(asset, new ObjectData());
                    }
                    var e = new SubSOInspectorElement(asset, objectStates);
                    extensionElement.Add(e);
                    toolbar.AddTarget(e);
                }

                extensionElement.Add(new CreateObjectButton());
            }
        }

        private class CreateObjectButton : VisualElement
        {
            private readonly Button _button = new() { text = "Add ScriptableObject" };
            public CreateObjectButton()
            {
                Add(_button);

                _button.style.flexWrap = Wrap.Wrap;
                _button.style.alignSelf = Align.Center;
                _button.style.width = 200;
                _button.style.height = 25;
                _button.style.marginTop = 10;
                _button.style.marginBottom = 10;
                _button.focusable = false;

                style.display = DisplayStyle.Flex;
                style.alignItems = Align.Center;

                _button.RegisterCallback<ClickEvent>(OnButtonClicked);
            }

            private void OnButtonClicked(ClickEvent evt)
            {
                var window = EditorWindow.GetWindow<Tools.ScriptableObjectAssetCreator>();
                window.SetAssetCreatedCallback(() => { window.Close(); });
                window.ShowModalUtility();
                InspectorExtensionInstaller.Instance.Refresh();
            }
        }

        private class Toolbar : VisualElement
        {
            private readonly List<SubSOInspectorElement> _targets = new();
            private bool _foldoutState = false;
            private readonly Button _button;
            private readonly UnityEngine.Object _target;
            private readonly int allSubAssetsCount;

            public Toolbar(UnityEngine.Object target)
            {
                _target = target;

                allSubAssetsCount = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(_target))
                    .Where(a => !AssetDatabase.IsMainAsset(a))
                    .ToArray().Length;

                if (allSubAssetsCount == 0) return;

                style.display = DisplayStyle.Flex;
                style.flexDirection = FlexDirection.RowReverse;

                var moreButton = new Button { text = "..." };
                moreButton.style.width = 20;
                moreButton.style.marginLeft = 0;
                moreButton.RegisterCallback<ClickEvent>(OnMoreButtonClicked);
                Add(moreButton);

                if (allSubAssetsCount >= 1)
                {
                    _button = new Button() { text = "Reveal All" };
                    _button.style.width = 70;
                    _button.focusable = false;
                    _button.RegisterCallback<ClickEvent>(OnButtonClick);
                    Add(_button);
                }

                SetFouldoutState(false);
            }

            private void OnMoreButtonClicked(ClickEvent evt)
            {
                var menu = new GenericMenu();
                if (allSubAssetsCount >= 1)
                {
                    menu.AddItem(new GUIContent("Copy All"), false, () =>
                    {
                        _copiedObjects = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(_target)).Where(a => !AssetDatabase.IsMainAsset(a)).ToArray();
                    });
                }

                if (CopiedObject != null)
                {
                    menu.AddItem(new GUIContent("Paste Object as New"), false, () =>
                    {
                        AssetDatabase.AddObjectToAsset(UnityEngine.Object.Instantiate(CopiedObject), AssetDatabase.GetAssetPath(_target));
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();

                        InspectorExtensionInstaller.Instance.Refresh();
                    });
                }

                if (_copiedObjects != null)
                {
                    menu.AddItem(new GUIContent("Paste All Objects"), false, () =>
                    {
                        foreach (var o in _copiedObjects)
                        {
                            AssetDatabase.AddObjectToAsset(UnityEngine.Object.Instantiate(o), AssetDatabase.GetAssetPath(_target));
                        }
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();

                        InspectorExtensionInstaller.Instance.Refresh();
                    });
                }
                menu.ShowAsContext();
            }

            public void AddTarget(SubSOInspectorElement target)
            {
                _targets.Add(target);
            }

            private void OnButtonClick(ClickEvent evt)
            {
                _foldoutState = !_foldoutState;
                SetFouldoutState(_foldoutState);
            }

            private void SetFouldoutState(bool fouldout)
            {
                foreach (var t in _targets)
                {
                    _button.text = fouldout ? "Close all" : "Reveal all";
                    t.SetFoldout(fouldout);
                }
            }
        }

        private class SubSOInspectorElement : VisualElement
        {
            private readonly UnityEngine.Object asset;
            private readonly Dictionary<UnityEngine.Object, ObjectData> objectStates;
            private readonly Editor editor;
            private readonly IMGUIContainer imguiContainer;
            private readonly ObjectEditorHeader header;
            private readonly VisualElement body;

            public SubSOInspectorElement(UnityEngine.Object asset, Dictionary<UnityEngine.Object, ObjectData> objectStates)
            {
                this.asset = asset;
                this.objectStates = objectStates;
                if (asset == null) return;
                editor = Editor.CreateEditor(asset);
                imguiContainer = new IMGUIContainer()
                {
                    onGUIHandler = editor.OnInspectorGUI
                };
                body = new VisualElement();


                Add(header = new ObjectEditorHeader(asset, OnFoldoutChanged, objectStates.ContainsKey(asset) ? objectStates[asset].foldout : false));
                Add(body);
                var inspectorModeHelper = new InspectorModeHelper(editor.serializedObject);
                ((IInspectorModeHelper)inspectorModeHelper).SetDebugMode(objectStates.ContainsKey(asset) && objectStates[asset].debug ? InspectorMode.Debug : InspectorMode.Normal);
                ((IInspectorModeHelper)inspectorModeHelper).OnModeChanged += OnInspectorModeChanged;
                EditorSecondHeaderVE headerVE;
                body.Add(headerVE = new EditorSecondHeaderVE(asset, inspectorModeHelper));
                body.Add(ModifyEditor(imguiContainer));
                headerVE.TriggerOnAttachToPanel(this);
            }

            private void OnInspectorModeChanged(IInspectorModeHelper helper)
            {
                objectStates[asset].debug = helper.IsDebugMode();
            }

            private void OnFoldoutChanged(bool open)
            {
                SetFoldout(open, false);
            }

            public void SetFoldout(bool foldout, bool modifyHeaderFoldout = true)
            {
                if (body != null)
                {
                    body.style.display = foldout ? DisplayStyle.Flex : DisplayStyle.None;
                }
                if (modifyHeaderFoldout)
                {
                    header.Foldout.value = foldout;
                }

                objectStates[asset].foldout = foldout;
            }

            private VisualElement ModifyEditor(VisualElement visualElement)
            {
                visualElement.style.marginBottom = 8;
                visualElement.style.marginLeft = 20;
                visualElement.style.marginRight = 4;

                var wrapper = new VisualElement();

                wrapper.Add(visualElement);
                wrapper.style.flexDirection = FlexDirection.Column;
                wrapper.style.borderBottomWidth = 1;
                wrapper.style.borderBottomColor = new Color(.1f, .1f, .1f);
                return wrapper;
            }
        }

        private class ObjectEditorHeader : VisualElement
        {
            private readonly Action<bool> _foldoutCallback;
            public Foldout Foldout { get; private set; }

            public ObjectEditorHeader(UnityEngine.Object obj, Action<bool> foldoutCallback, bool defaultValue)
            {
                _foldoutCallback = foldoutCallback;

                style.display = DisplayStyle.Flex;
                style.flexDirection = FlexDirection.Row;
                style.paddingLeft = 0;
                style.marginLeft = 0;
                style.marginRight = 0;
                style.marginBottom = 2;
                style.alignItems = Align.Center;
                style.borderTopWidth = 1;
                style.borderTopColor = new Color(.1f, .1f, .1f);
                style.backgroundColor = new Color(0.2431373f, 0.2431373f, 0.2431373f, 1f);

                Foldout = new Foldout()
                {
                    value = defaultValue
                };
                _foldoutCallback?.Invoke(defaultValue);
                Foldout.RegisterCallback<ChangeEvent<bool>>(b =>
                {
                    _foldoutCallback?.Invoke(b.newValue);
                });

                Add(Foldout);

                var icon = new Image();
                icon.style.width = 17;
                icon.style.height = 17;
                icon.style.marginRight = 4;
                icon.image = EditorGUIUtility.ObjectContent(obj, obj.GetType()).image;
                Add(icon);

                var textEditor = new EditableLabel();
                textEditor.SetCallback(str =>
                {
                    obj.name = str;
                    textEditor.SetTextAndLabel(obj.name, obj.name + $" ({obj.GetType().Name})");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                });
                textEditor.SetTextAndLabel(obj.name, obj.name + $" ({obj.GetType().Name})");
                textEditor.style.flexGrow = 1;
                Add(textEditor);

                var moreButton = new Button() { text = "..." };
                moreButton.style.width = 20;
                Add(moreButton);

                moreButton.RegisterCallback<ClickEvent>(e =>
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Rename"), false, () =>
                    {
                        textEditor.ShowEditMode();
                    });
                    menu.AddItem(new GUIContent("Copy Object"), false, () =>
                    {
                        CopiedObject = obj;
                    });

                    if (CopiedObject != null)
                    {
                        if (obj.GetType().IsAssignableFrom(CopiedObject.GetType()))
                        {
                            menu.AddItem(new GUIContent("Paste Object Values"), false, () =>
                            {
                                EditorUtility.CopySerialized(CopiedObject, obj);
                                AssetDatabase.SaveAssets();
                                AssetDatabase.Refresh();
                            });
                        }

                        menu.AddItem(new GUIContent("Paste Object as New"), false, () =>
                        {
                            AssetDatabase.AddObjectToAsset(UnityEngine.Object.Instantiate(CopiedObject), AssetDatabase.GetAssetPath(obj));
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();

                            InspectorExtensionInstaller.Instance.Refresh();
                        });
                    }

                    menu.AddItem(new GUIContent("Delete"), false, () =>
                    {
                        AssetDatabase.RemoveObjectFromAsset(obj);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();

                        InspectorExtensionInstaller.Instance.Refresh();
                    });
                    menu.ShowAsContext();
                });


                textEditor.Label.RegisterCallback<ClickEvent>(e =>
                {
                    Foldout.value = !Foldout.value;
                    EditorGUIUtility.PingObject(obj);
                });

                icon.RegisterCallback<ClickEvent>(e =>
                {
                    EditorGUIUtility.PingObject(obj);
                });
            }
        }

        private class EditableLabel : VisualElement
        {
            public TextField TextField { get; private set; }
            public Label Label { get; private set; }
            public Button CloseButton { get; private set; }
            public Button OKButton { get; private set; }

            private readonly VisualElement _editMode;

            private Action<string> _callback;

            public EditableLabel()
            {
                style.display = DisplayStyle.Flex;
                style.flexDirection = FlexDirection.Row;

                _editMode = new VisualElement();
                _editMode.style.display = DisplayStyle.None;
                _editMode.style.flexDirection = FlexDirection.Row;
                Add(_editMode);

                Label = new Label();
                Label.style.flexGrow = 1;
                Label.style.unityFontStyleAndWeight = FontStyle.Bold;
                Add(Label);

                TextField = new TextField();
                TextField.style.flexGrow = 1;
                SetupTextFieldEvent();
                _editMode.Add(TextField);

                OKButton = new Button();
                OKButton.text = "Ok";
                OKButton.style.width = 20;
                OKButton.style.marginLeft = 0;
                OKButton.style.marginRight = 0;
                OKButton.RegisterCallback<ClickEvent>(e =>
                {
                    HideEditMode();
                    _callback?.Invoke(TextField.value);
                });
                _editMode.Add(OKButton);

                CloseButton = new Button();
                CloseButton.text = "x";
                CloseButton.style.width = 20;
                CloseButton.style.marginLeft = 0;
                CloseButton.style.marginRight = 0;
                CloseButton.RegisterCallback<ClickEvent>(e =>
                {
                    HideEditMode();
                });

                _editMode.Add(CloseButton);
            }

            public void SetCallback(Action<string> callback)
            {
                _callback = callback;
            }

            public void ShowEditMode()
            {
                Label.style.display = DisplayStyle.None;
                _editMode.style.display = DisplayStyle.Flex;
            }

            public void HideEditMode()
            {
                Label.style.display = DisplayStyle.Flex;
                _editMode.style.display = DisplayStyle.None;
            }
            public void SetTextAndLabel(string text, string label)
            {
                TextField.value = text;
                Label.text = label;
            }

            private void SetupTextFieldEvent()
            {
                TextField.RegisterCallback<KeyDownEvent>(k =>
                {
                    if (k.keyCode == KeyCode.Return)
                    {
                        HideEditMode();
                        _callback?.Invoke(TextField.value);
                    }
                    else if (k.keyCode == KeyCode.Escape)
                    {
                        HideEditMode();
                    }
                });
            }
        }
    }
}
#endif
