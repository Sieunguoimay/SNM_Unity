using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.GPUSkinning.BoneWeightTool
{
    public class BoneToolWindow : EditorWindow
    {
        [SerializeField] private Mesh mesh;
        [SerializeField] private Mesh outputMesh;

        private BoneTool _tool;
        private Action _exportCallback;
        private Action _cleanToolVECallback;

        [MenuItem("Tools/Open Bone Weight Tool")]
        public static void OpenTool()
        {
            GetWindow<BoneToolWindow>();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += SceneView_duringSceneGui;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= SceneView_duringSceneGui;
            if (_tool != null)
            {
                _tool.BoneSelectionTool.OnBoneSelectorsChanged -= Tool_OnBonesChanged;
                _tool.BindposesTool.Hide();
                _tool.HideVerticesSelector();
                _tool = null;
            }
            if (_cleanToolVECallback != null) _cleanToolVECallback();
        }

        private void CreateGUI()
        {
            LoadTool(this);
            RefreshVE();
        }

        private void SceneView_duringSceneGui(SceneView view)
        {
            if (_tool == null) return;

            if (_tool.VerticesSelector.IsActive)
            {
                DrawVerticesSelector(_tool.VerticesSelector);
            }
            else
            {
                var vertices = _tool.VerticesSelector.AllVertices;
                for (int i = 0; i < vertices.Count; i++)
                {
                    var vertexPos = vertices[i];
                    DrawHandleButton(vertexPos, Color.white);
                }
            }
        }

        private void DrawVerticesSelector(VerticesSelectionTool verticesSelector)
        {
            var vertices = verticesSelector.AllVertices;
            for (int i = 0; i < vertices.Count; i++)
            {
                var vertexPos = vertices[i];
                var color = verticesSelector.IsVertexSelected(i) ? Color.green : Color.white;
                if (DrawHandleButton(vertexPos, color))
                {
                    if (verticesSelector.IsVertexSelected(i))
                    {
                        verticesSelector.Unselect(i);
                    }
                    else
                    {
                        verticesSelector.Select(i);
                    }
                }
            }
        }

        private bool DrawHandleButton(Vector3 vertexPos, Color color)
        {
            var handleSize = HandleUtility.GetHandleSize(vertexPos) * 0.04f;
            Handles.color = color;
            return Handles.Button(vertexPos, Quaternion.identity, handleSize, handleSize, Handles.SphereHandleCap);
        }

        public void SetExportCallback(Action exportCallback)
        {
            _exportCallback = exportCallback;
            RefreshVE();
        }

        public void SetBoneTool(BoneTool tool)
        {
            if (_tool != null)
            {
                _tool.BoneSelectionTool.OnBoneSelectorsChanged -= Tool_OnBonesChanged;
                _tool.BindposesTool.Hide();
                _tool.HideVerticesSelector();
            }

            _tool = tool;

            if (_tool != null)
            {
                _tool.BoneSelectionTool.OnBoneSelectorsChanged += Tool_OnBonesChanged;
                _tool.BindposesTool.Show();
            }

            RefreshVE();
        }

        private void RefreshVE()
        {
            var layout_Config = new VisualElement();
            var serialized = new SerializedObject(this);
            layout_Config.Add(new PropertyField(serialized.FindProperty(nameof(mesh))) { bindingPath = nameof(mesh) });
            layout_Config.Add(new PropertyField(serialized.FindProperty(nameof(outputMesh))) { bindingPath = nameof(outputMesh) });
            layout_Config.Bind(serialized);

            var layout_ToolBar = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
            layout_ToolBar.Add(new Button() { text = "Load", clickable = new(() => LoadTool(this)) });

            rootVisualElement.Clear();
            rootVisualElement.Add(layout_Config);
            rootVisualElement.Add(layout_ToolBar);

            if (_exportCallback != null)
            {
                var button_Export = new Button { text = "Export", clickable = new(_exportCallback) };

                layout_ToolBar.Add(button_Export);
            }

            if (_cleanToolVECallback != null)
            {
                _cleanToolVECallback();
                _cleanToolVECallback = null;
            }
            if (_tool != null)
            {
                var toolVE = CreateBoneSelectorsVE(_tool, out _cleanToolVECallback);
                if (toolVE != null)
                {
                    rootVisualElement.Add(toolVE);
                }
            }
        }

        private static void LoadTool(BoneToolWindow window)
        {
            if (window.mesh == null) return;
            BoneToolCreator.CreateTool(window.mesh, out var tool, out var exportFunc);
            window.SetBoneTool(tool);
            window.SetExportCallback(() => BoneToolCreator.ExportSkinnedMesh(exportFunc(), window.mesh, ref window.outputMesh));
        }

        private void Tool_OnBonesChanged()
        {
            RefreshVE();
        }

        public VisualElement CreateBoneSelectorsVE(BoneTool tool, out Action cleanupAction)
        {
            var root = new VisualElement();
            var buttons = new VisualElement();
            var buttonDic = new Dictionary<object, VisualElement>();
            var boneSelectors = tool.BoneSelectionTool.BoneSelectors;
            for (int i = 0; i < boneSelectors.Count; i++)
            {
                var boneSelector = boneSelectors[i];
                var button = new Button() { text = "Bone " + i, clickable = new(boneSelector.Select) };

                button.SetEnabled(!boneSelector.IsSelected);
                boneSelector.OnIsSelectedChangedCallback += BoneSelector_OnIsSelectedChanged;

                buttonDic.Add(boneSelector, button);
                buttons.Add(button);
            }
            var button_NewBone = new Button() { text = "+", clickable = new(tool.AddNewBone) };
            root.Add(buttons);
            root.Add(button_NewBone);

            void BoneSelector_OnIsSelectedChanged(BoneSelector selector) => buttonDic[selector].SetEnabled(!selector.IsSelected);

            cleanupAction = () =>
            {
                for (int i = 0; i < boneSelectors.Count; i++)
                {
                    var boneSelector = boneSelectors[i];
                    boneSelector.OnIsSelectedChangedCallback -= BoneSelector_OnIsSelectedChanged;
                }
            };
            return root;
        }
    }
}