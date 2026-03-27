#if UNITY_EDITOR
using System.Collections.Generic;
using Snm.Runtime.GPUSkinning.Serialize;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.GPUSkinning.BoneToolV2
{
    /// <summary>
    /// Main EditorWindow for BoneToolV2. Builds the full UI layout using UIToolkit:
    /// toolbar (mesh/skeleton fields, mode buttons, export), left sidebar (BoneTreeView),
    /// bottom panel (BrushSettingsPanel), and status bar.
    /// Subscribes to SceneView.duringSceneGui and routes events to the active IToolMode.
    /// </summary>
    public class BoneToolV2Window : EditorWindow
    {
        private RigDocument _doc;

        // Modes
        private Dictionary<RigDocument.ToolModeEnum, IToolMode> _modes;
        private IToolMode _activeMode;

        // UI elements
        private ObjectField _meshField;
        private ObjectField _skeletonField;
        private Button _skeletonModeBtn;
        private Button _paintModeBtn;
        private Button _testModeBtn;
        private BoneTreeView _boneTreeView;
        private BrushSettingsPanel _brushPanel;
        private StatusBar _statusBar;

        // Brush settings shared between paint mode and UI
        private BrushSettings _brushSettings;

        [MenuItem("Tools/Snm/Game/Bone Tool V2")]
        public static void ShowWindow()
        {
            var window = GetWindow<BoneToolV2Window>();
            window.titleContent = new GUIContent("Bone Tool V2");
            window.minSize = new Vector2(400, 300);
        }

        private void OnEnable()
        {
            _brushSettings = new BrushSettings();

            // Create the in-memory document
            _doc = ScriptableObject.CreateInstance<RigDocument>();
            _doc.hideFlags = HideFlags.DontSave;

            // Create modes
            _modes = new Dictionary<RigDocument.ToolModeEnum, IToolMode>
            {
                { RigDocument.ToolModeEnum.Skeleton, new SkeletonEditMode() },
                { RigDocument.ToolModeEnum.Paint, new WeightPaintMode(_brushSettings) },
                { RigDocument.ToolModeEnum.Test, new TestPoseMode() },
            };

            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.update -= OnEditorUpdate;

            _activeMode?.OnExit();
            _activeMode = null;

            if (_doc != null)
            {
                DestroyImmediate(_doc);
                _doc = null;
            }
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.flexGrow = 1f;

            // --- Toolbar ---
            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.alignItems = Align.Center;
            toolbar.style.paddingLeft = 4;
            toolbar.style.paddingRight = 4;
            toolbar.style.paddingTop = 4;
            toolbar.style.paddingBottom = 4;
            toolbar.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            toolbar.style.borderBottomWidth = 1;
            toolbar.style.borderBottomColor = new Color(0.1f, 0.1f, 0.1f);

            // Mesh field
            _meshField = new ObjectField("Mesh");
            _meshField.objectType = typeof(Mesh);
            _meshField.style.width = 200;
            _meshField.RegisterValueChangedCallback(evt =>
            {
                UndoHelper.Record(_doc, "Set Mesh");
                _doc.sourceMesh = evt.newValue as Mesh;
                _doc.EnsureVertexWeights();
                RefreshAll();
            });
            toolbar.Add(_meshField);

            // Skeleton asset field
            _skeletonField = new ObjectField("Skeleton");
            _skeletonField.objectType = typeof(SkeletonAsset);
            _skeletonField.style.width = 200;
            _skeletonField.style.marginLeft = 8;
            _skeletonField.RegisterValueChangedCallback(evt =>
            {
                var asset = evt.newValue as SkeletonAsset;
                _doc.sourceSkeletonAsset = asset;
                if (asset != null)
                    _doc.LoadFromSkeletonAsset(asset);
                RefreshAll();
            });
            toolbar.Add(_skeletonField);

            // Spacer
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1f;
            toolbar.Add(spacer);

            // Mode buttons
            var modeGroup = new VisualElement();
            modeGroup.style.flexDirection = FlexDirection.Row;
            modeGroup.style.marginLeft = 12;

            _skeletonModeBtn = new Button(() => SwitchMode(RigDocument.ToolModeEnum.Skeleton)) { text = "Skeleton [1]" };
            _paintModeBtn = new Button(() => SwitchMode(RigDocument.ToolModeEnum.Paint)) { text = "Paint [2]" };
            _testModeBtn = new Button(() => SwitchMode(RigDocument.ToolModeEnum.Test)) { text = "Test [3]" };

            modeGroup.Add(_skeletonModeBtn);
            modeGroup.Add(_paintModeBtn);
            modeGroup.Add(_testModeBtn);
            toolbar.Add(modeGroup);

            // Export button
            var exportBtn = new Button(OnExportClicked) { text = "Export" };
            exportBtn.style.marginLeft = 12;
            toolbar.Add(exportBtn);

            root.Add(toolbar);

            // --- Main content area ---
            var mainArea = new VisualElement();
            mainArea.style.flexDirection = FlexDirection.Row;
            mainArea.style.flexGrow = 1f;

            // Left sidebar: Bone tree view
            var sidebar = new VisualElement();
            sidebar.style.width = 250;
            sidebar.style.borderRightWidth = 1;
            sidebar.style.borderRightColor = new Color(0.1f, 0.1f, 0.1f);
            sidebar.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f, 1f);

            var sidebarHeader = new Label("Bone Hierarchy");
            sidebarHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            sidebarHeader.style.paddingLeft = 8;
            sidebarHeader.style.paddingTop = 6;
            sidebarHeader.style.paddingBottom = 4;
            sidebar.Add(sidebarHeader);

            _boneTreeView = new BoneTreeView();
            _boneTreeView.SetDocument(_doc);
            _boneTreeView.OnBoneSelected += OnBoneSelectedInTree;
            sidebar.Add(_boneTreeView);

            // Sidebar bottom buttons
            var sidebarButtons = new VisualElement();
            sidebarButtons.style.flexDirection = FlexDirection.Row;
            sidebarButtons.style.paddingLeft = 8;
            sidebarButtons.style.paddingBottom = 4;
            sidebarButtons.style.paddingTop = 4;

            var addBoneBtn = new Button(OnAddBoneClicked) { text = "+ Add" };
            var delBoneBtn = new Button(OnDeleteBoneClicked) { text = "x Delete" };
            sidebarButtons.Add(addBoneBtn);
            sidebarButtons.Add(delBoneBtn);
            sidebar.Add(sidebarButtons);

            mainArea.Add(sidebar);

            // Right side: info / instructions panel
            var infoPanel = new VisualElement();
            infoPanel.style.flexGrow = 1f;
            infoPanel.style.alignItems = Align.Center;
            infoPanel.style.justifyContent = Justify.Center;

            var infoLabel = new Label("Scene View is the main editing area.\nSwitch modes with toolbar buttons or keys 1/2/3.");
            infoLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
            infoLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            infoLabel.style.whiteSpace = WhiteSpace.Normal;
            infoPanel.Add(infoLabel);

            mainArea.Add(infoPanel);
            root.Add(mainArea);

            // --- Bottom panel: brush settings (only visible in Paint mode) ---
            _brushPanel = new BrushSettingsPanel();
            _brushPanel.Bind(_brushSettings);
            _brushPanel.style.display = DisplayStyle.None; // hidden by default
            root.Add(_brushPanel);

            // --- Status bar ---
            _statusBar = new StatusBar();
            root.Add(_statusBar);

            // Initialize to Skeleton mode
            SwitchMode(RigDocument.ToolModeEnum.Skeleton);
        }

        private void SwitchMode(RigDocument.ToolModeEnum mode)
        {
            if (_doc == null) return;

            // Exit current mode
            _activeMode?.OnExit();

            _doc.activeMode = mode;

            // Enter new mode
            if (_modes.TryGetValue(mode, out var newMode))
            {
                _activeMode = newMode;
                _activeMode.OnEnter(_doc);
            }

            // Update UI
            UpdateModeButtons();
            _brushPanel.style.display = (mode == RigDocument.ToolModeEnum.Paint)
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            RefreshStatusBar();
            SceneView.RepaintAll();
        }

        private void UpdateModeButtons()
        {
            SetModeButtonStyle(_skeletonModeBtn, _doc.activeMode == RigDocument.ToolModeEnum.Skeleton);
            SetModeButtonStyle(_paintModeBtn, _doc.activeMode == RigDocument.ToolModeEnum.Paint);
            SetModeButtonStyle(_testModeBtn, _doc.activeMode == RigDocument.ToolModeEnum.Test);
        }

        private void SetModeButtonStyle(Button btn, bool active)
        {
            if (active)
            {
                btn.style.backgroundColor = new Color(0.3f, 0.5f, 0.8f, 1f);
                btn.style.color = Color.white;
            }
            else
            {
                btn.style.backgroundColor = StyleKeyword.Null;
                btn.style.color = StyleKeyword.Null;
            }
        }

        private void OnSceneGUI(SceneView view)
        {
            if (_doc == null) return;

            // Route input through SceneInputHandler
            var requestedMode = SceneInputHandler.HandleSceneInput(view, _activeMode, _doc);
            if (requestedMode.HasValue && requestedMode.Value != _doc.activeMode)
            {
                SwitchMode(requestedMode.Value);
            }
        }

        private void OnEditorUpdate()
        {
            // Periodic refresh of UI state
            RefreshStatusBar();

            // Refresh brush panel if in paint mode (settings may change via keyboard)
            if (_doc != null && _doc.activeMode == RigDocument.ToolModeEnum.Paint)
            {
                _brushPanel?.RefreshFromBrush();
            }
        }

        private void RefreshAll()
        {
            _boneTreeView?.SetDocument(_doc);
            _boneTreeView?.Rebuild();
            RefreshStatusBar();
            SceneView.RepaintAll();
        }

        private void RefreshStatusBar()
        {
            _statusBar?.UpdateFromDocument(_doc, _activeMode?.DisplayName);
        }

        private void OnBoneSelectedInTree(int boneIndex)
        {
            SceneView.RepaintAll();
            RefreshStatusBar();
        }

        private void OnAddBoneClicked()
        {
            if (_doc == null) return;

            int parentIdx = _doc.selectedBoneIndex;
            Vector3 pos = Vector3.zero;

            if (parentIdx >= 0)
                pos = _doc.GetBoneWorldPosition(parentIdx) + Vector3.up * 0.2f;

            int newIdx = _doc.AddBone("Bone_" + _doc.bones.Count, parentIdx, pos);
            _doc.selectedBoneIndex = newIdx;
            RefreshAll();
        }

        private void OnDeleteBoneClicked()
        {
            if (_doc == null || _doc.selectedBoneIndex < 0) return;
            _doc.RemoveBone(_doc.selectedBoneIndex);
            RefreshAll();
        }

        private void OnExportClicked()
        {
            if (_doc == null || _doc.bones == null || _doc.bones.Count == 0)
            {
                EditorUtility.DisplayDialog("Export", "No bones to export. Create a skeleton first.", "OK");
                return;
            }

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Export SkeletonAsset"), false, ExportSkeletonAsset);
            menu.AddItem(new GUIContent("Export Mesh (with weights)"), false, ExportMeshWithWeights);
            menu.AddItem(new GUIContent("Build Skinned Prefab"), false, BuildSkinnedPrefab);
            menu.ShowAsContext();
        }

        private void ExportSkeletonAsset()
        {
            var path = EditorUtility.SaveFilePanelInProject("Save SkeletonAsset", "NewSkeleton", "asset",
                "Choose a location to save the skeleton asset.");
            if (string.IsNullOrEmpty(path)) return;

            var asset = ScriptableObject.CreateInstance<SkeletonAsset>();
            asset.skeleton = new Skeleton();
            asset.skeleton.bones = new Bone[_doc.bones.Count];

            for (int i = 0; i < _doc.bones.Count; i++)
            {
                asset.skeleton.bones[i] = new Bone
                {
                    parent = _doc.bones[i].parentIndex,
                    bindpose = _doc.bones[i].bindpose
                };
            }

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Export", "SkeletonAsset saved to:\n" + path, "OK");
        }

        private void ExportMeshWithWeights()
        {
            if (_doc.sourceMesh == null)
            {
                EditorUtility.DisplayDialog("Export", "No source mesh assigned.", "OK");
                return;
            }

            var path = EditorUtility.SaveFilePanelInProject("Save Mesh", _doc.sourceMesh.name + "_skinned", "asset",
                "Choose a location to save the mesh with weights.");
            if (string.IsNullOrEmpty(path)) return;

            var mesh = Object.Instantiate(_doc.sourceMesh);
            mesh.name = _doc.sourceMesh.name + "_skinned";

            // Bake weights
            int vertCount = mesh.vertexCount;
            var boneWeights = new BoneWeight[vertCount];
            if (_doc.vertexWeights != null)
            {
                for (int v = 0; v < vertCount && v < _doc.vertexWeights.Length; v++)
                    boneWeights[v] = _doc.vertexWeights[v].ToBoneWeight();
            }
            mesh.boneWeights = boneWeights;

            // Bake bindposes
            var bindposes = new Matrix4x4[_doc.bones.Count];
            for (int i = 0; i < _doc.bones.Count; i++)
                bindposes[i] = _doc.bones[i].bindpose;
            mesh.bindposes = bindposes;

            AssetDatabase.CreateAsset(mesh, path);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Export", "Mesh saved to:\n" + path, "OK");
        }

        private void BuildSkinnedPrefab()
        {
            var prefabRoot = PrefabBuilderService.BuildSkinnedPrefab(_doc, null);
            if (prefabRoot == null) return;

            var path = EditorUtility.SaveFilePanelInProject("Save Prefab", "SkinnedMesh", "prefab",
                "Choose a location to save the skinned prefab.");
            if (string.IsNullOrEmpty(path))
            {
                DestroyImmediate(prefabRoot);
                return;
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
            DestroyImmediate(prefabRoot);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Export", "Prefab saved to:\n" + path, "OK");
        }
    }
}
#endif
