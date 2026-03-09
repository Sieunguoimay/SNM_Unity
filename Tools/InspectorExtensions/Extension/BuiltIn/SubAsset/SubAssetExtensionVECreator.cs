#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static Snm.Tools.InspectorExtensions.InspectorLayoutInjector;

namespace Snm.Tools.InspectorExtensions
{
    public class SubAssetExtensionVECreator
    {
        public static VisualElement Create(SubAssetTool tool, SerializedObject serializedObject)
        {
            var ve = new VisualElement();
            var target = serializedObject.targetObject;
            if (AssetDatabase.IsMainAsset(target) && serializedObject.targetObjects.Length == 1)
            {
                var button_Add = CreateAddButton(null);
                var list = CreateSubAssetList(target, tool);
                ve.Add(list);
                ve.Add(button_Add);
            }
            return ve;
        }

        private static VisualElement CreateSubAssetList(UnityEngine.Object target, SubAssetTool tool)
        {
            var root = new VisualElement();

            foreach (var subAsset in SubAssetTool.GetSubAssets(target))
            {
                var subAssetVE = CreateComponentLikeInspector(subAsset, tool);
                root.Add(subAssetVE);
            }

            return root;
        }

        private static VisualElement CreateAddButton(Action refreshHandler)
        {
            var root = new VisualElement() { style = { display = DisplayStyle.Flex, alignItems = Align.Center } };

            var button_Add = new Button()
            {
                text = "Add Sub Asset",
                style = { flexWrap = Wrap.Wrap, alignSelf = Align.Center, width = 200, height = 25, marginTop = 10, marginBottom = 10 },
                focusable = false,
                clickable = new(OnButtonClicked)
            };
            root.Add(button_Add);

            void OnButtonClicked()
            {
                var window = EditorWindow.GetWindow<ScriptableObjectAssetCreator>();
                window.SetAssetCreatedCallback(() => { window.Close(); });
                window.ShowModalUtility();
                refreshHandler?.Invoke();
            }

            return root;
        }

        private static VisualElement CreateComponentLikeInspector(UnityEngine.Object target, SubAssetTool tool)
        {
            var root = new VisualElement();
            var header = new VisualElement
            {
                style = {
                    flexGrow = 1, display = DisplayStyle.Flex, flexDirection = FlexDirection.Row,
                    paddingLeft = 0, marginLeft = 0, marginRight = 0, marginBottom = 2,
                    alignItems = Align.Center, borderTopWidth = 1,
                    borderTopColor = new Color(.1f, .1f, .1f),
                    backgroundColor = new Color(0.2431373f, 0.2431373f, 0.2431373f, 1f),
                }
            };
            var foldout = new Foldout { value = false, text = $"{target.name} ({target.GetType().Name})", style = { flexGrow = 1, marginLeft = 14, marginRight = 0 } };
            var button_SelectActions = new Button() { text = "..", style = { flexShrink = 1 }, clickable = new(ShowActions) };
            var body = new VisualElement() { style = { display = DisplayStyle.None } };
            var inspector = new InspectorElement(target);
            var icon = new Image { image = EditorGUIUtility.ObjectContent(target, target?.GetType()).image, scaleMode = ScaleMode.ScaleToFit, style = { flexShrink = 0, width = 16, height = 16, marginRight = 4 } };

            foldout.Q<Label>().parent.Insert(1, icon);
            foldout.RegisterValueChangedCallback(evt => body.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None);

            header.Add(foldout);
            header.Add(button_SelectActions);
            body.Add(inspector);
            root.Add(header);
            root.Add(body);

            VisualElement top = new(), bottom = new(), left = new(), right = new();
            var zonesLifecycles = new List<AttachmentZonesLifecycle> { new(inspector, top, bottom, left, right) };
            var editorLayouts = new[] { new EditorLayout(new(top, bottom, left, right), new[] { target }, new SerializedObject(target), inspector) };

            var extensions = InspectorExtensionSystemInstaller.GetDefaultExtensionsToInstall().ToArray();
            var extensionRenderer = new InspectorExtensionRenderer(editorLayouts, new TypeBasedExtensionFilter());
            extensionRenderer.ApplyExtensions(extensions);

            return root;

            void ShowActions()
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Remove Sub Asset"), false, () => tool.RemoveSubAsset(target));
                menu.AddItem(new GUIContent("Rename"), false, () => RenameToolWindow.Open(target));
                menu.ShowAsContext();
            }
        }
    }
}

#endif