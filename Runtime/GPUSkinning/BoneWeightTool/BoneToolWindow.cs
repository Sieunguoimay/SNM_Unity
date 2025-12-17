using System;
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

        private BoneToolUI _toolUI;
        private Action _exportCallback;
        private VisualElement _toolVE;

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
            if (_toolUI != null)
            {
                _toolUI.OnBoneSelectorsChanged -= ToolUI_OnBoneSelectorsChanged;
                _toolUI = null;
            }
        }

        private void CreateGUI()
        {
            LoadTool(this);
            RefreshVE();
        }

        private void SceneView_duringSceneGui(SceneView view)
        {
            if (_toolUI == null) return;

            if (_toolUI.VerticesSelector != null)
            {
                DrawVerticesSelectorUI(_toolUI.VerticesSelector);
            }
            else
            {
                var vertices = _toolUI.AllVertices;
                for (int i = 0; i < vertices.Count; i++)
                {
                    var vertexPos = vertices[i];
                    DrawHandleButton(vertexPos, Color.white);
                }
            }

            foreach (var boneUI in _toolUI.BoneSelectors)
            {
                DrawBoneSelectorUI(boneUI);
            }
        }

        private void DrawBoneSelectorUI(BoneSelectorUI boneUI)
        {
        }

        private void DrawVerticesSelectorUI(VerticesSelectorUI verticesUI)
        {
            if (verticesUI == null) return;

            var vertices = verticesUI.AllVertices;
            for (int i = 0; i < vertices.Count; i++)
            {
                var vertexPos = vertices[i];
                var color = verticesUI.IsVertexSelected(i) ? Color.green : Color.white;
                if (DrawHandleButton(vertexPos, color))
                {
                    if (verticesUI.IsVertexSelected(i))
                    {
                        verticesUI.Unselect(i);
                    }
                    else
                    {
                        verticesUI.Select(i);
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

        public void SetBoneToolUI(BoneToolUI toolUI)
        {
            if (_toolUI != null)
            {
                _toolUI.OnBoneSelectorsChanged -= ToolUI_OnBoneSelectorsChanged;
            }

            _toolUI = toolUI;
            _toolVE = null;

            if (_toolUI != null)
            {
                _toolUI.OnBoneSelectorsChanged += ToolUI_OnBoneSelectorsChanged;
                _toolVE = CreateToolVE(_toolUI);
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

            if (_toolVE != null)
            {
                rootVisualElement.Add(_toolVE);
            }
        }

        private static void LoadTool(BoneToolWindow window)
        {
            if (window.mesh == null) return;
            var toolUI = BoneToolUICreator.CreateToolUI(window.mesh, out var exportFunc);
            window.SetBoneToolUI(toolUI);
            window.SetExportCallback(() => BoneToolUICreator.ExportSkinnedMesh(exportFunc(), window.mesh, ref window.outputMesh));
        }

        private void ToolUI_OnBoneSelectorsChanged()
        {
            _toolVE = CreateToolVE(_toolUI);
            RefreshVE();
        }

        public VisualElement CreateToolVE(BoneToolUI toolUI)
        {
            var boneSelectors = toolUI.BoneSelectors;
            var root = new VisualElement();
            var buttons = new VisualElement();
            for (int i = 0; i < boneSelectors.Count; i++)
            {
                var boneSelector = boneSelectors[i];
                var button = new Button() { text = "Bone " + i, clickable = new(ButtonClick) };

                void ButtonClick()
                {
                    ClearSelection();
                    boneSelector.Select();
                }
                button.SetEnabled(!boneSelector.IsSelected);
                boneSelector.SetIsSelectedChangeCallback(_ => button.SetEnabled(!_.IsSelected));
                buttons.Add(button);
            }
            var button_NewBone = new Button() { text = "+", clickable = new(() => { toolUI.AddNewBone(); }) };
            root.Add(buttons);
            root.Add(button_NewBone);

            void ClearSelection()
            {
                foreach (var boneSelector in boneSelectors)
                {
                    boneSelector.Unselect();
                }
            }
            return root;
        }
    }
}