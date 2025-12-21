using System;
using System.Collections.Generic;
using System.Linq;
using Snm.Runtime.GPUSkinning;
using Snm.Runtime.GPUSkinning.Serialize;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.GPUSkinning.BoneWeightTool
{
    public class BoneToolWindow : EditorWindow
    {
        [SerializeField] private Mesh mesh;
        [SerializeField] private BoneHierarchyAsset boneHierarchy;

        private BoneTool _tool;
        private Action _exportCallback_ToAsset;
        private Action<bool> _exportCallback_ToMesh;
        private Action _cleanToolVECallback;
        private Material _material;
        private GPUSkinnedMeshRendererCore _renderer;

        [MenuItem("Tools/Open Bone Weight Tool")]
        public static void OpenTool()
        {
            GetWindow<BoneToolWindow>();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += SceneView_duringSceneGui;
            TryCleanupRenderer();
            TryCreateRenderer();
        }

        private void OnDisable()
        {
            TryCleanupRenderer();
            SceneView.duringSceneGui -= SceneView_duringSceneGui;
            if (_tool != null)
            {
                _tool.BoneSelectionTool.OnBoneSelectorsChanged -= Tool_OnBonesChanged;
                _tool.BindposeTransformsTool.Hide();
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

            DrawMesh();

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

        private void OnValidate()
        {
            TryCleanupRenderer();
            TryCreateRenderer();
        }

        private void DrawMesh()
        {
            if (_renderer != null && _tool != null)
            {
                for (int i = 0; i < _tool.BindposeTransformsTool.BindposeTransforms.Length; i++)
                {
                    var bt = _tool.BindposeTransformsTool.BindposeTransforms[i];
                    _renderer.SetBoneMatrix(i, bt.transform.localToWorldMatrix);
                }
                _renderer.UploadBoneMatricesViaMaterial();
                _renderer.Render(Matrix4x4.identity);
            }
        }

        private void TryCreateRenderer()
        {
            if (mesh == null) return;

            _material = new Material(
                AssetDatabase.LoadAssetAtPath<Shader>("Assets/SNM_Unity/Runtime/GPUSkinning/GPUSkin.shader"));
            _renderer = new GPUSkinnedMeshRendererCore(mesh, _material);
            _renderer.UploadMeshDataViaMesh();
        }

        private void TryCleanupRenderer()
        {
            if (_material != null)
            {
                DestroyImmediate(_material);
                _material = null;
            }
            _renderer = null;
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
            var old = Handles.color;
            Handles.color = color;
            var clicked = Handles.Button(vertexPos, Quaternion.identity, handleSize, handleSize, Handles.SphereHandleCap);
            Handles.color = old;
            return clicked;
        }

        public void SetExportCallback(Action exportCallback_ToAsset, Action<bool> exportCallback_ToMesh)
        {
            _exportCallback_ToAsset = exportCallback_ToAsset;
            _exportCallback_ToMesh = exportCallback_ToMesh;
            RefreshVE();
        }

        public void SetBoneTool(BoneTool tool)
        {
            if (_tool != null)
            {
                _tool.BoneSelectionTool.OnBoneSelectorsChanged -= Tool_OnBonesChanged;
                _tool.BindposeTransformsTool.Hide();
                _tool.HideVerticesSelector();
            }

            _tool = tool;

            if (_tool != null)
            {
                _tool.BoneSelectionTool.OnBoneSelectorsChanged += Tool_OnBonesChanged;
                _tool.BindposeTransformsTool.Show();
            }

            RefreshVE();
        }

        private void RefreshVE()
        {
            var root = new VisualElement();
            var layout_Config = new VisualElement();
            var serialized = new SerializedObject(this);
            layout_Config.Add(new PropertyField(serialized.FindProperty(nameof(mesh))) { bindingPath = nameof(mesh) });
            layout_Config.Add(new PropertyField(serialized.FindProperty(nameof(boneHierarchy))) { bindingPath = nameof(boneHierarchy) });
            layout_Config.Bind(serialized);
            var button_Load = new Button() { text = "Load", clickable = new(() => LoadTool(this)) };

            var layout_BindposesAndBoneWeights = new Foldout() { text = "Bindposes & Boneweights", value = true };
            if (_exportCallback_ToMesh != null)
            {
                var layout_Horizontal = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
                var button_Export = new Button { text = "Export to Mesh", clickable = new(() => _exportCallback_ToMesh(false)) };
                var button_ExportAs = new Button { text = "Export As New Mesh", clickable = new(() => _exportCallback_ToMesh(true)) };

                layout_Horizontal.Add(button_Export);
                layout_Horizontal.Add(button_ExportAs);
                layout_BindposesAndBoneWeights.Add(layout_Horizontal);
            }

            var layout_BoneHierarchy = new Foldout() { text = "Bone Hierarchy", value = true };
            if (_exportCallback_ToAsset != null)
            {
                var button_Export = new Button { text = "Export to Asset", clickable = new(_exportCallback_ToAsset) };

                layout_BoneHierarchy.Add(button_Export);
            }

            root.Add(layout_Config);
            root.Add(button_Load);
            root.Add(layout_BindposesAndBoneWeights);
            root.Add(layout_BoneHierarchy);

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
                    root.Add(toolVE);
                }
            }

            rootVisualElement.Clear();
            rootVisualElement.Add(root);
        }

        private static void LoadTool(BoneToolWindow window)
        {
            if (window.mesh == null) return;

            var boneHierarchy = window.boneHierarchy != null
                ? window.boneHierarchy.boneHierarchy.parents
                : Enumerable.Range(0, window.mesh.bindposeCount).Select(i => -1).ToArray();

            BoneToolCreator.CreateTool(window.mesh, boneHierarchy, out var tool, out var exportFunc);

            window.SetBoneTool(tool);
            window.SetExportCallback(
                exportCallback_ToAsset: () =>
                {
                    var (bones, hierarchy) = exportFunc();
                    AssetExportTool.ExportBoneHierarchy(hierarchy, (string)window.mesh.name, ref window.boneHierarchy);
                },
                exportCallback_ToMesh: (toNewMesh) =>
                {
                    var (bones, hierarchy) = exportFunc();
                    if (toNewMesh)
                    {
                        Mesh newMesh = null;
                        AssetExportTool.ExportBoneDataAsSkinnedMesh(RuntimeBoneImporter.Export(bones), window.mesh, ref newMesh);
                    }
                    else
                    {
                        AssetExportTool.ExportBoneDataAsSkinnedMesh(RuntimeBoneImporter.Export(bones), window.mesh, ref window.mesh);
                    }
                });
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