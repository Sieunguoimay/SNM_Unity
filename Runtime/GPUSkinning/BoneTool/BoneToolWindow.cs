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
        [SerializeField] private SkeletonAsset skeleton;
        [SerializeField] private BoneToolMode toolMode;

        private readonly BoneTool tool = new();

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
            tool.BoneTransformsTool.Hide();
            tool.HideVerticesSelector();
            ClearToolVE();
        }

        private void CreateGUI()
        {
            LoadTool();
        }

        private void SceneView_duringSceneGui(SceneView view)
        {
            if (mesh == null) return;

            DrawMesh();
            DrawVertexSeletors();
        }

        private void DrawVertexSeletors()
        {
            if (toolMode != BoneToolMode.WeightPainter) return;

            if (tool.VerticesSelector.IsActive)
            {
                DrawVerticesSelector(tool.VerticesSelector);
            }
            else
            {
                var vertices = mesh.vertices;
                var boneWeights = mesh.boneWeights;

                for (int i = 0; i < vertices.Length; i++)
                {
                    var vertexPos = vertices[i];
                    if (i < boneWeights.Length)
                    {
                        var boneWeight = boneWeights[i];

                        vertexPos = Skin(vertexPos, boneWeight);
                    }
                    DrawHandleButton(vertexPos, Color.white);
                }
            }
        }

        private void DrawMesh()
        {
            if (_renderer != null)
            {
                if (toolMode == BoneToolMode.WeightPainter)
                {
                    for (int i = 0; i < tool.BoneTransformsTool.BoneTransforms.Length; i++)
                    {
                        var bt = tool.BoneTransformsTool.BoneTransforms[i];
                        _renderer.SetSkinningMatrix(i, bt.transform.localToWorldMatrix * tool.Bones[i].bindpose);
                    }
                    _renderer.UploadBoneMatricesViaMaterial(tool.Bones.Length);
                }
                else
                {
                    _renderer.UploadBoneMatricesViaMaterial(0);
                }
                _renderer.Render(Matrix4x4.identity);
            }
        }

        private void OnValidate()
        {
            TryCleanupRenderer();
            TryCreateRenderer();
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
            var vertices = mesh.vertices;
            var boneWeights = mesh.boneWeights;

            for (int i = 0; i < vertices.Length; i++)
            {
                var vertexPos = vertices[i];

                if (i < boneWeights.Length)
                {
                    var boneWeight = boneWeights[i];

                    vertexPos = Skin(vertexPos, boneWeight);
                }

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
                    RefreshVE();
                }
            }
        }

        private Vector3 Skin(Vector3 pos, BoneWeight boneWeight)
        {

            var w0 = boneWeight.weight0;
            var w1 = boneWeight.weight1;
            var w2 = boneWeight.weight2;
            var w3 = boneWeight.weight3;

            if (w0 + w1 + w2 + w3 < 0.01f) return pos;

            var boneIndex0 = boneWeight.boneIndex0;
            var boneIndex1 = boneWeight.boneIndex1;
            var boneIndex2 = boneWeight.boneIndex2;
            var boneIndex3 = boneWeight.boneIndex3;

            var boneTransforms = tool.BoneTransformsTool.BoneTransforms;
            var boneMatrix0 = boneIndex0 < boneTransforms.Length ? boneTransforms[boneIndex0].transform.localToWorldMatrix : Matrix4x4.identity;
            var boneMatrix1 = boneIndex1 < boneTransforms.Length ? boneTransforms[boneIndex1].transform.localToWorldMatrix : Matrix4x4.identity;
            var boneMatrix2 = boneIndex2 < boneTransforms.Length ? boneTransforms[boneIndex2].transform.localToWorldMatrix : Matrix4x4.identity;
            var boneMatrix3 = boneIndex3 < boneTransforms.Length ? boneTransforms[boneIndex3].transform.localToWorldMatrix : Matrix4x4.identity;

            var bindpose0 = boneIndex0 < mesh.bindposeCount ? mesh.bindposes[boneIndex0] : Matrix4x4.identity;
            var bindpose1 = boneIndex1 < mesh.bindposeCount ? mesh.bindposes[boneIndex1] : Matrix4x4.identity;
            var bindpose2 = boneIndex2 < mesh.bindposeCount ? mesh.bindposes[boneIndex2] : Matrix4x4.identity;
            var bindpose3 = boneIndex3 < mesh.bindposeCount ? mesh.bindposes[boneIndex3] : Matrix4x4.identity;

            if (skeleton != null)
            {
                bindpose0 = skeleton.skeleton.bones[boneIndex0].bindpose;
                bindpose1 = skeleton.skeleton.bones[boneIndex1].bindpose;
                bindpose2 = skeleton.skeleton.bones[boneIndex2].bindpose;
                bindpose3 = skeleton.skeleton.bones[boneIndex3].bindpose;
            }
            return (boneMatrix0 * bindpose0).MultiplyPoint3x4(pos) * w0
                + (boneMatrix1 * bindpose1).MultiplyPoint3x4(pos) * w1
                + (boneMatrix2 * bindpose2).MultiplyPoint3x4(pos) * w2
                + (boneMatrix3 * bindpose3).MultiplyPoint3x4(pos) * w3;
        }

        private static bool DrawHandleButton(Vector3 vertexPos, Color color)
        {
            var handleSize = HandleUtility.GetHandleSize(vertexPos) * 0.04f;
            var old = Handles.color;
            Handles.color = color;
            var clicked = Handles.Button(vertexPos, Quaternion.identity, handleSize, handleSize, Handles.SphereHandleCap);
            Handles.color = old;
            return clicked;
        }

        private void Export_ToSkeletonAsset()
        {
            tool.UpdateSkeletonWithBoneTransforms(Matrix4x4.identity);
            AssetExportTool.ExportToSkeletonAsset(tool.Bones.Select(b => new Bone { parent = b.parent, bindpose = b.bindpose }).ToArray(), ref skeleton);
        }

        private void Export_ToMesh(bool exportBindposes)
        {
            tool.UpdateSkeletonWithBoneTransforms(Matrix4x4.identity);

            var bindposes = tool.Bones.Select(b => b.bindpose).ToArray();
            var boneWeights = BoneWeightConverter.ExtractBoneWeights(RuntimeBoneImporter.Export(tool.Bones).Item1, mesh.vertexCount);

            AssetExportTool.ExportBoneWeightsToMesh(boneWeights, bindposes, mesh, ref mesh);
            TryCleanupRenderer();
            TryCreateRenderer();
        }

        private void RefreshVE()
        {
            ClearToolVE();
            rootVisualElement.Clear();
            rootVisualElement.Add(CreateConfigVE());
            rootVisualElement.Add(CreateToolVE());
        }

        private void ClearToolVE()
        {
            if (_cleanToolVECallback != null)
            {
                _cleanToolVECallback();
                _cleanToolVECallback = null;
            }
        }

        private VisualElement CreateToolVE()
        {
            var root = new VisualElement();

            var toolVE = CreateBoneVEs(tool, out _cleanToolVECallback);
            if (toolVE != null)
            {
                root.Add(toolVE);
            }

            return root;
        }

        private VisualElement CreateConfigVE()
        {
            var root = new VisualElement();
            var layout_Config = new VisualElement();

            var objectField_Mesh = new ObjectField { label = "Mesh", value = mesh, objectType = typeof(Mesh) };
            objectField_Mesh.RegisterValueChangedCallback(evt => { mesh = (Mesh)evt.newValue; });

            var objectField_Skeleton = new ObjectField { label = "Skeleton", value = skeleton, objectType = typeof(SkeletonAsset) };
            objectField_Skeleton.RegisterValueChangedCallback(evt => { skeleton = (SkeletonAsset)evt.newValue; });

            var enumField_ToolMode = new EnumField(toolMode);
            enumField_ToolMode.RegisterValueChangedCallback(evt =>
            {
                toolMode = (BoneToolMode)evt.newValue;
                tool.SnapBoneTransformsToBindposes();
                RefreshVE();
            });

            var layout_Horizontal = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
            var button_Load = new Button() { text = "Load Bones", clickable = new(LoadTool) };

            layout_Config.Add(objectField_Mesh);
            layout_Config.Add(objectField_Skeleton);
            root.Add(layout_Config);
            layout_Horizontal.Add(enumField_ToolMode);
            layout_Horizontal.Add(button_Load);
            root.Add(layout_Horizontal);

            if (toolMode == BoneToolMode.WeightPainter)
            {
                var button_ExportBoneWeights = new Button { text = "Save Mesh", clickable = new(() => Export_ToMesh(false)) };
                // var button_ExportBindposes = new Button { text = "Save Bindposes to Mesh", clickable = new(() => _exportCallback_ToMesh(true)) };

                layout_Horizontal.Add(button_ExportBoneWeights);
                // layout_Horizontal.Add(button_ExportBindposes);
            }
            else
            {
                var button_Export = new Button { text = "Save Skeleton", clickable = new(Export_ToSkeletonAsset) };

                layout_Horizontal.Add(button_Export);
            }

            return root;
        }

        private void LoadTool()
        {
            if (mesh != null)
            {
                var runtimeBones = skeleton != null
                    ? RuntimeBoneImporter.Import(BoneWeightConverter.ConvertToBoneDatas(mesh.boneWeights, skeleton.skeleton.bones.Length), skeleton.skeleton.bones.Select(b => b.bindpose).ToArray(), skeleton.skeleton.bones.Select(b => b.parent).ToArray())
                    : RuntimeBoneImporter.Import(BoneWeightConverter.ConvertToBoneDatas(mesh.boneWeights, mesh.bindposes.Length), mesh.bindposes, mesh.bindposes.Select(b => -1).ToArray());

                tool.SetRuntimeBones(runtimeBones);

                TryCleanupRenderer();
                TryCreateRenderer();
            }
            else
            {
                tool.SetRuntimeBones(Array.Empty<RuntimeBone>());
            }
            RefreshVE();
        }

        public VisualElement CreateBoneVEs(BoneTool tool, out Action cleanupAction)
        {
            var root = new VisualElement();
            var buttons = new VisualElement();
            var buttonDic = new Dictionary<object, VisualElement>();
            var boneSelectors = tool.BoneSelectionTool.BoneSelectors ?? Array.Empty<BoneSelector>();
            for (int i = 0; i < boneSelectors.Count; i++)
            {
                var boneSelector = boneSelectors[i];
                var boneIndex = i;

                boneSelector.OnIsSelectedChangedCallback += BoneSelector_OnIsSelectedChanged;

                if (toolMode == BoneToolMode.WeightPainter)
                {
                    var layout_Horizontal = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
                    var button_Select = new Button() { text = "Bone " + i + $" ({tool.Bones[i].vertices.Count})", clickable = new(boneSelector.Select), style = { flexGrow = 1 } };
                    var button_Clear = new Button() { text = "Clear Weight", clickable = new(() => tool.ClearBoneVertices(boneIndex)) };

                    button_Select.SetEnabled(!boneSelector.IsSelected);
                    buttonDic.Add(boneSelector, button_Select);

                    layout_Horizontal.Add(button_Select);
                    layout_Horizontal.Add(button_Clear);
                    buttons.Add(layout_Horizontal);
                }
                else
                {
                    var layout_Horizontal = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
                    var button_Select = new Button() { text = "Bone " + i, clickable = new(boneSelector.Select), style = { flexGrow = 1 } };
                    var button_Clear = new Button()
                    {
                        text = "X",
                        clickable = new(() =>
                    {
                        tool.UpdateSkeletonWithBoneTransforms(Matrix4x4.identity);
                        tool.DeleteBone(boneIndex);
                        RefreshVE();
                    })
                    };

                    layout_Horizontal.Add(button_Select);
                    layout_Horizontal.Add(button_Clear);

                    buttons.Add(layout_Horizontal);

                    button_Select.SetEnabled(!boneSelector.IsSelected);
                    buttonDic.Add(boneSelector, button_Select);
                }
            }
            root.Add(buttons);

            if (toolMode == BoneToolMode.BoneCreator)
            {
                var button_NewBone = new Button
                {
                    text = "Add New",
                    clickable = new(() =>
                {
                    tool.UpdateSkeletonWithBoneTransforms(Matrix4x4.identity);
                    tool.AddNew();
                    RefreshVE();
                })
                };

                buttons.Add(button_NewBone);
            }

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