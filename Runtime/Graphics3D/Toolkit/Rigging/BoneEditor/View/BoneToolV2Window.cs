#if UNITY_EDITOR
using System.Collections.Generic;
using Snm.Graphics3D.GPUSkinning;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Graphics3D.Rigging
{
    public class BoneToolV2Window : EditorWindow
    {
        private RigDocument _doc;

        private Dictionary<RigDocument.ToolModeEnum, IToolMode> _modes;
        private IToolMode _activeMode;

        private ObjectField _meshField;
        private ObjectField _skeletonField;
        private Button _skeletonModeBtn;
        private Button _paintModeBtn;
        private Button _testModeBtn;
        private BoneTreeView _boneTreeView;
        private BrushSettingsPanel _brushPanel;
        private StatusBar _statusBar;
        private VisualElement _actionPanel;
        private VisualElement _weightActionsPanel;

        private BrushSettings _brushSettings;
        private Label _warningBanner;

        [MenuItem("Tools/Snm/3D Toolkit/Rigging/Bone Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<BoneToolV2Window>();
            window.titleContent = new GUIContent("Bone Tool V2");
            window.minSize = new Vector2(280, 400);
        }

        private void OnEnable()
        {
            _brushSettings = BrushSettings.LoadFromPrefs();

            _doc = ScriptableObject.CreateInstance<RigDocument>();
            _doc.hideFlags = HideFlags.DontSave;

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
            BrushSettings.SaveToPrefs(_brushSettings);

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
            rootVisualElement.Add(BuildContent());
        }

        internal VisualElement BuildContent()
        {
            var root = new VisualElement { style = { flexGrow = 1f } };
            root.style.flexGrow = 1f;

            // === Input fields ===
            var inputSection = new VisualElement { style = { paddingLeft = 4, paddingRight = 4, paddingTop = 4 } };

            // Import from prefab
            var importField = new ObjectField("Import Prefab") { objectType = typeof(GameObject) };
            importField.RegisterValueChangedCallback(evt =>
            {
                var go = evt.newValue as GameObject;
                if (go == null) return;
                ImportFromPrefab(go);
                importField.SetValueWithoutNotify(null); // reset field — it's a one-shot action
            });
            inputSection.Add(importField);

            inputSection.Add(MakeSeparator());

            _meshField = new ObjectField("Mesh") { objectType = typeof(Mesh) };
            _meshField.RegisterValueChangedCallback(evt =>
            {
                UndoHelper.Record(_doc, "Set Mesh");
                _doc.sourceMesh = evt.newValue as Mesh;
                _doc.EnsureVertexWeights();

                // If bones already exist, auto-weight the new mesh
                if (_doc.sourceMesh != null && _doc.bones.Count > 0)
                    AutoWeightService.AssignAutoWeights(_doc);

                RefreshAll();
            });
            inputSection.Add(_meshField);

            _skeletonField = new ObjectField("Rig Data") { objectType = typeof(SkeletonAsset) };
            _skeletonField.RegisterValueChangedCallback(evt =>
            {
                var asset = evt.newValue as SkeletonAsset;
                if (asset == null) return;

                _doc.sourceSkeletonAsset = asset;
                _doc.LoadFromSkeletonAsset(asset);

                if (_doc.sourceMesh == null)
                {
                    // No mesh yet — use the rig's mesh
                    if (asset.sourceMesh != null)
                    {
                        _doc.sourceMesh = asset.sourceMesh;
                        _meshField.SetValueWithoutNotify(asset.sourceMesh);
                        _doc.EnsureVertexWeights();
                    }
                }
                else if (asset.sourceMesh != null && asset.sourceMesh != _doc.sourceMesh)
                {
                    // Different mesh — ask user
                    int choice = EditorUtility.DisplayDialogComplex(
                        "Mesh Mismatch",
                        $"Current mesh: {_doc.sourceMesh.name}\n" +
                        $"Rig data mesh: {asset.sourceMesh.name}\n\n" +
                        "Which mesh do you want to use?",
                        $"Use rig mesh ({asset.sourceMesh.name})",
                        "Cancel",
                        $"Keep current ({_doc.sourceMesh.name})");

                    if (choice == 0)
                    {
                        // Use rig mesh
                        _doc.sourceMesh = asset.sourceMesh;
                        _meshField.SetValueWithoutNotify(asset.sourceMesh);
                        _doc.EnsureVertexWeights();
                    }
                    else if (choice == 1)
                    {
                        // Cancel — revert rig load
                        return;
                    }
                    // choice == 2: keep current mesh, auto-weight with loaded bones
                }

                // Auto-weight if bones and mesh both exist
                if (_doc.sourceMesh != null && _doc.bones.Count > 0)
                    AutoWeightService.AssignAutoWeights(_doc);

                RefreshAll();
            });
            inputSection.Add(_skeletonField);

            root.Add(inputSection);

            // === Warning banner (#5) ===
            _warningBanner = new Label();
            _warningBanner.style.paddingLeft = 8;
            _warningBanner.style.paddingRight = 8;
            _warningBanner.style.paddingTop = 4;
            _warningBanner.style.paddingBottom = 4;
            _warningBanner.style.marginLeft = 4;
            _warningBanner.style.marginRight = 4;
            _warningBanner.style.marginTop = 2;
            _warningBanner.style.marginBottom = 2;
            _warningBanner.style.backgroundColor = new Color(0.6f, 0.4f, 0.1f, 0.8f);
            _warningBanner.style.color = Color.white;
            _warningBanner.style.unityFontStyleAndWeight = FontStyle.Bold;
            _warningBanner.style.display = DisplayStyle.None;
            root.Add(_warningBanner);

            // === Mode tabs ===
            var modeRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    paddingLeft = 4, paddingRight = 4,
                    paddingTop = 2, paddingBottom = 2
                }
            };

            _skeletonModeBtn = new Button(() => SwitchMode(RigDocument.ToolModeEnum.Skeleton))
                { text = "Skeleton [1]", style = { flexGrow = 1 } };
            _paintModeBtn = new Button(() => SwitchMode(RigDocument.ToolModeEnum.Paint))
                { text = "Paint [2]", style = { flexGrow = 1 } };
            _testModeBtn = new Button(() => SwitchMode(RigDocument.ToolModeEnum.Test))
                { text = "Test [3]", style = { flexGrow = 1 } };

            modeRow.Add(_skeletonModeBtn);
            modeRow.Add(_paintModeBtn);
            modeRow.Add(_testModeBtn);
            root.Add(modeRow);

            // === Separator ===
            root.Add(MakeSeparator());

            // === Bone list (takes remaining space) ===
            _boneTreeView = new BoneTreeView();
            _boneTreeView.SetDocument(_doc);
            _boneTreeView.OnBoneSelected += OnBoneSelectedInTree;
            _boneTreeView.style.flexGrow = 1f;
            _boneTreeView.style.minHeight = 80;
            root.Add(_boneTreeView);

            // === Bottom section (never shrinks, never overlapped) ===
            var bottomSection = new VisualElement { style = { flexShrink = 0 } };

            // Add / Delete row
            var boneActions = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    paddingLeft = 4, paddingRight = 4,
                    paddingTop = 2, paddingBottom = 2
                }
            };
            boneActions.Add(new Button(OnAddBoneClicked) { text = "+ Add", style = { flexGrow = 1 } });
            boneActions.Add(new Button(OnDeleteBoneClicked) { text = "x Delete", style = { flexGrow = 1 } });
            bottomSection.Add(boneActions);

            bottomSection.Add(MakeSeparator());

            // Skeleton actions (Skeleton mode only)
            _actionPanel = new VisualElement { style = { paddingLeft = 4, paddingRight = 4, paddingBottom = 2 } };
            _actionPanel.Add(new Button(OnMirrorBonesClicked) { text = "Mirror Bones" });
            bottomSection.Add(_actionPanel);

            // Paint panel (Paint mode only): brush + weight actions
            _brushPanel = new BrushSettingsPanel();
            _brushPanel.Bind(_brushSettings);
            _brushPanel.style.display = DisplayStyle.None;
            bottomSection.Add(_brushPanel);

            _weightActionsPanel = new VisualElement { style = { paddingLeft = 4, paddingRight = 4, paddingBottom = 2, display = DisplayStyle.None } };
            _weightActionsPanel.Add(new Button(OnAutoWeightClicked) { text = "Auto Weights [A]" });
            _weightActionsPanel.Add(new Button(OnNormalizeClicked) { text = "Normalize All Weights" });
            _weightActionsPanel.Add(new Button(OnMirrorWeightsClicked) { text = "Mirror Weights" });
            bottomSection.Add(_weightActionsPanel);

            bottomSection.Add(MakeSeparator());

            // Export row
            var exportRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    paddingLeft = 4, paddingRight = 4,
                    paddingTop = 2, paddingBottom = 4
                }
            };
            exportRow.Add(new Button(SaveRigDocument) { text = "Save Rig", style = { flexGrow = 1 } });
            exportRow.Add(new Button(ApplyOrBuildPrefab) { text = "Apply / Build Prefab", style = { flexGrow = 1 } });
            bottomSection.Add(exportRow);

            // Status bar
            _statusBar = new StatusBar();
            bottomSection.Add(_statusBar);

            root.Add(bottomSection);

            SwitchMode(RigDocument.ToolModeEnum.Skeleton);
            return root;
        }

        private static VisualElement MakeSeparator()
        {
            return new VisualElement
            {
                style =
                {
                    height = 1,
                    backgroundColor = new Color(0.1f, 0.1f, 0.1f),
                    marginTop = 2,
                    marginBottom = 2
                }
            };
        }

        // =============================================
        // Mode switching
        // =============================================

        private void SwitchMode(RigDocument.ToolModeEnum mode)
        {
            if (_doc == null) return;

            _activeMode?.OnExit();
            _doc.activeMode = mode;

            if (_modes.TryGetValue(mode, out var newMode))
            {
                _activeMode = newMode;
                _activeMode.OnEnter(_doc);
            }

            UpdateModeButtons();
            bool isPaint = mode == RigDocument.ToolModeEnum.Paint;
            bool isSkeleton = mode == RigDocument.ToolModeEnum.Skeleton;
            _actionPanel.style.display = isSkeleton ? DisplayStyle.Flex : DisplayStyle.None;
            _brushPanel.style.display = isPaint ? DisplayStyle.Flex : DisplayStyle.None;
            _weightActionsPanel.style.display = isPaint ? DisplayStyle.Flex : DisplayStyle.None;

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
            btn.style.backgroundColor = active ? new Color(0.3f, 0.5f, 0.8f, 1f) : StyleKeyword.Null;
            btn.style.color = active ? Color.white : StyleKeyword.Null;
        }

        // =============================================
        // Scene + Editor update
        // =============================================

        private void OnSceneGUI(SceneView view)
        {
            if (_doc == null) return;

            var requestedMode = SceneInputHandler.HandleSceneInput(view, _activeMode, _doc);
            if (requestedMode.HasValue && requestedMode.Value != _doc.activeMode)
                SwitchMode(requestedMode.Value);
        }

        private void OnEditorUpdate()
        {
            RefreshStatusBar();
            if (_doc != null && _doc.activeMode == RigDocument.ToolModeEnum.Paint)
                _brushPanel?.RefreshFromBrush();
        }

        private void ImportFromPrefab(GameObject prefab)
        {
            var smr = prefab.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr == null || smr.sharedMesh == null)
            {
                EditorUtility.DisplayDialog("Import", "Prefab has no SkinnedMeshRenderer with a mesh.", "OK");
                return;
            }

            UndoHelper.Record(_doc, "Import from Prefab");

            // Track source prefab for "Apply to Prefab"
            _doc.sourcePrefab = prefab;

            // Mesh
            _doc.sourceMesh = smr.sharedMesh;
            _meshField.SetValueWithoutNotify(smr.sharedMesh);

            // Bones from SMR.bones
            var bones = smr.bones;
            var bindposes = smr.sharedMesh.bindposes;
            _doc.bones.Clear();

            for (int i = 0; i < bones.Length; i++)
            {
                // Find parent index
                int parentIndex = -1;
                if (bones[i] != null && bones[i].parent != null)
                {
                    for (int j = 0; j < bones.Length; j++)
                    {
                        if (j != i && bones[j] == bones[i].parent)
                        {
                            parentIndex = j;
                            break;
                        }
                    }
                }

                var bindpose = i < bindposes.Length ? bindposes[i] : Matrix4x4.identity;
                var boneName = bones[i] != null ? bones[i].name : "Bone_" + i;

                _doc.bones.Add(new BoneData
                {
                    name = boneName,
                    parentIndex = parentIndex,
                    bindpose = bindpose,
                    displayColor = new Color(
                        0.2f + (i * 0.13f) % 0.8f,
                        0.4f + (i * 0.07f) % 0.6f,
                        0.6f + (i * 0.11f) % 0.4f)
                });
            }

            // Weights
            var boneWeights = smr.sharedMesh.boneWeights;
            _doc.EnsureVertexWeights();
            if (boneWeights != null)
            {
                for (int v = 0; v < boneWeights.Length && v < _doc.vertexWeights.Length; v++)
                {
                    var bw = boneWeights[v];
                    _doc.vertexWeights[v] = new WeightData();
                    if (bw.weight0 > 0) _doc.vertexWeights[v].SetWeight(bw.boneIndex0, bw.weight0);
                    if (bw.weight1 > 0) _doc.vertexWeights[v].SetWeight(bw.boneIndex1, bw.weight1);
                    if (bw.weight2 > 0) _doc.vertexWeights[v].SetWeight(bw.boneIndex2, bw.weight2);
                    if (bw.weight3 > 0) _doc.vertexWeights[v].SetWeight(bw.boneIndex3, bw.weight3);
                }
            }

            RefreshAll();
            Debug.Log($"Imported from {prefab.name}: {_doc.bones.Count} bones, {smr.sharedMesh.vertexCount} vertices");
        }

        private void RefreshAll()
        {
            _boneTreeView?.SetDocument(_doc);
            _boneTreeView?.Rebuild();
            RefreshStatusBar();
            UpdateWarningBanner();
            SceneView.RepaintAll();
        }

        private void UpdateWarningBanner()
        {
            if (_warningBanner == null || _doc == null) return;

            bool noMesh = _doc.sourceMesh == null;
            bool noBones = _doc.bones == null || _doc.bones.Count == 0;

            if (noMesh)
            {
                _warningBanner.text = "Assign a mesh to begin";
                _warningBanner.style.backgroundColor = new Color(0.7f, 0.3f, 0.1f, 0.8f);
                _warningBanner.style.display = DisplayStyle.Flex;
            }
            else if (noBones)
            {
                _warningBanner.text = "Create bones in Skeleton mode or load a rig";
                _warningBanner.style.backgroundColor = new Color(0.6f, 0.4f, 0.1f, 0.8f);
                _warningBanner.style.display = DisplayStyle.Flex;
            }
            else
            {
                _warningBanner.style.display = DisplayStyle.None;
            }
        }

        private void RefreshStatusBar()
        {
            _statusBar?.UpdateFromDocument(_doc, _activeMode?.DisplayName);
        }

        // =============================================
        // Bone actions
        // =============================================

        private void OnBoneSelectedInTree(int boneIndex)
        {
            SceneView.RepaintAll();
            RefreshStatusBar();
        }

        private void OnAddBoneClicked()
        {
            if (_doc == null) return;
            int parentIdx = _doc.selectedBoneIndex;
            var pos = parentIdx >= 0 ? _doc.GetBoneWorldPosition(parentIdx) + Vector3.up * 0.2f : Vector3.zero;
            _doc.selectedBoneIndex = _doc.AddBone("Bone_" + _doc.bones.Count, parentIdx, pos);
            RefreshAll();
        }

        private void OnDeleteBoneClicked()
        {
            if (_doc == null || _doc.selectedBoneIndex < 0) return;
            _doc.RemoveBone(_doc.selectedBoneIndex);
            RefreshAll();
        }

        private void OnAutoWeightClicked()
        {
            if (_doc == null || _doc.sourceMesh == null || _doc.bones.Count == 0) return;
            if (HasAnyExistingWeights())
            {
                if (!EditorUtility.DisplayDialog("Auto Weights",
                    "This will overwrite existing weights. Continue?", "Yes", "Cancel"))
                    return;
            }
            AutoWeightService.AssignAutoWeights(_doc);
            RefreshAll();
        }

        private bool HasAnyExistingWeights()
        {
            if (_doc?.vertexWeights == null) return false;
            for (int i = 0; i < _doc.vertexWeights.Length; i++)
                if (_doc.vertexWeights[i].TotalWeight > 0.001f) return true;
            return false;
        }

        private void OnNormalizeClicked()
        {
            if (_doc == null || _doc.vertexWeights == null) return;
            ValidationService.NormalizeAllWeights(_doc);
            RefreshAll();
        }

        private void OnMirrorBonesClicked()
        {
            if (_doc == null || _doc.bones.Count == 0) return;
            WeightMirrorService.MirrorBoneNames(_doc);
            RefreshAll();
        }

        private void OnMirrorWeightsClicked()
        {
            if (_doc == null || _doc.selectedBoneIndex < 0) return;
            WeightMirrorService.MirrorWeights(_doc, _doc.selectedBoneIndex);
            RefreshAll();
        }

        // =============================================
        // Export
        // =============================================

        private void SaveRigDocument()
        {
            if (!ValidateExport()) return;

            var meshName = _doc.sourceMesh != null ? _doc.sourceMesh.name : "Rig";
            var path = EditorUtility.SaveFilePanelInProject("Save Rig", meshName + "_rig", "asset", "Save rig data for later editing.");
            if (string.IsNullOrEmpty(path)) return;

            var asset = ScriptableObject.CreateInstance<SkeletonAsset>();
            asset.sourceMesh = _doc.sourceMesh;
            asset.skeleton = new Skeleton { bones = new Bone[_doc.bones.Count] };

            for (int i = 0; i < _doc.bones.Count; i++)
            {
                asset.skeleton.bones[i] = new Bone
                {
                    name = _doc.bones[i].name,
                    parent = _doc.bones[i].parentIndex,
                    bindpose = _doc.bones[i].bindpose
                };
            }

            // Save weights onto a mesh clone stored as sub-asset
            if (_doc.sourceMesh != null && _doc.vertexWeights != null)
            {
                var weightedMesh = Object.Instantiate(_doc.sourceMesh);
                weightedMesh.name = _doc.sourceMesh.name + "_weights";

                var boneWeights = new BoneWeight[weightedMesh.vertexCount];
                for (int v = 0; v < weightedMesh.vertexCount && v < _doc.vertexWeights.Length; v++)
                    boneWeights[v] = _doc.vertexWeights[v].ToBoneWeight();
                weightedMesh.boneWeights = boneWeights;

                var bindposes = new Matrix4x4[_doc.bones.Count];
                for (int i = 0; i < _doc.bones.Count; i++)
                    bindposes[i] = _doc.bones[i].bindpose;
                weightedMesh.bindposes = bindposes;

                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.AddObjectToAsset(weightedMesh, asset);
            }
            else
            {
                AssetDatabase.CreateAsset(asset, path);
            }

            AssetDatabase.SaveAssets();
            _skeletonField.value = asset;
            Debug.Log($"Rig saved: {path}");
        }

        private void ApplyOrBuildPrefab()
        {
            if (!ValidateExport()) return;

            if (_doc.sourcePrefab != null)
                ApplyToPrefab();
            else
                BuildNewPrefab();
        }

        private void ApplyToPrefab()
        {
            var prefabPath = AssetDatabase.GetAssetPath(_doc.sourcePrefab);
            if (string.IsNullOrEmpty(prefabPath))
            {
                Debug.LogError("Source prefab has no asset path. Building new prefab instead.");
                BuildNewPrefab();
                return;
            }

            // Open prefab for editing
            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            var smr = prefabRoot.GetComponentInChildren<SkinnedMeshRenderer>();

            if (smr == null)
            {
                smr = prefabRoot.AddComponent<SkinnedMeshRenderer>();
            }

            // Rebuild bone hierarchy inside the prefab
            // Remove old bone transforms that no longer match
            var existingBones = smr.bones;

            var boneTransforms = new Transform[_doc.bones.Count];
            for (int i = 0; i < _doc.bones.Count; i++)
            {
                var boneData = _doc.bones[i];

                // Try to find existing bone by name
                Transform existing = null;
                if (existingBones != null)
                {
                    foreach (var b in existingBones)
                    {
                        if (b != null && b.name == boneData.name)
                        {
                            existing = b;
                            break;
                        }
                    }
                }

                if (existing == null)
                {
                    // Create new bone GameObject
                    existing = new GameObject(boneData.name).transform;
                }

                // Parent it
                if (boneData.parentIndex >= 0 && boneData.parentIndex < i)
                    existing.SetParent(boneTransforms[boneData.parentIndex], false);
                else
                    existing.SetParent(prefabRoot.transform, false);

                // Set position from bindpose
                var worldMatrix = boneData.bindpose.inverse;
                if (boneData.parentIndex >= 0 && boneData.parentIndex < i)
                {
                    var parentWorld = _doc.bones[boneData.parentIndex].bindpose.inverse;
                    var localMatrix = parentWorld.inverse * worldMatrix;
                    existing.localPosition = (Vector3)localMatrix.GetColumn(3);
                    existing.localRotation = localMatrix.rotation;
                    existing.localScale = localMatrix.lossyScale;
                }
                else
                {
                    existing.localPosition = (Vector3)worldMatrix.GetColumn(3);
                    existing.localRotation = worldMatrix.rotation;
                    existing.localScale = worldMatrix.lossyScale;
                }

                boneTransforms[i] = existing;
            }

            // Clone mesh with baked weights + bindposes
            var skinnedMesh = Object.Instantiate(_doc.sourceMesh);
            skinnedMesh.name = _doc.sourceMesh.name + "_Skinned";

            int vertexCount = skinnedMesh.vertexCount;
            var boneWeights = new BoneWeight[vertexCount];
            if (_doc.vertexWeights != null)
                for (int v = 0; v < vertexCount && v < _doc.vertexWeights.Length; v++)
                    boneWeights[v] = _doc.vertexWeights[v].ToBoneWeight();
            skinnedMesh.boneWeights = boneWeights;

            var bindposes = new Matrix4x4[_doc.bones.Count];
            for (int i = 0; i < _doc.bones.Count; i++)
                bindposes[i] = _doc.bones[i].bindpose;
            skinnedMesh.bindposes = bindposes;

            // Save mesh asset next to prefab
            var meshPath = prefabPath.Replace(".prefab", "_mesh.asset");
            var existingMeshAsset = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existingMeshAsset != null)
            {
                EditorUtility.CopySerialized(skinnedMesh, existingMeshAsset);
                DestroyImmediate(skinnedMesh);
                skinnedMesh = existingMeshAsset;
            }
            else
            {
                AssetDatabase.CreateAsset(skinnedMesh, meshPath);
            }

            // Wire up SMR
            smr.sharedMesh = skinnedMesh;
            smr.bones = boneTransforms;
            smr.rootBone = boneTransforms[0];
            smr.localBounds = skinnedMesh.bounds;

            // Keep existing material if any
            if (smr.sharedMaterial == null)
                smr.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");

            // Save back
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            AssetDatabase.SaveAssets();

            Debug.Log($"Applied rig to prefab: {prefabPath}");
        }

        private void BuildNewPrefab()
        {
            var defaultMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            var prefabRoot = PrefabBuilderService.BuildSkinnedPrefab(_doc, defaultMat);
            if (prefabRoot == null) return;

            var meshName = _doc.sourceMesh != null ? _doc.sourceMesh.name : "SkinnedMesh";
            var path = EditorUtility.SaveFilePanelInProject("Save Prefab", meshName, "prefab", "Save prefab.");
            if (string.IsNullOrEmpty(path))
            {
                DestroyImmediate(prefabRoot);
                return;
            }

            var smr = prefabRoot.GetComponent<SkinnedMeshRenderer>();
            if (smr != null && smr.sharedMesh != null)
            {
                var meshPath = path.Replace(".prefab", "_mesh.asset");
                AssetDatabase.CreateAsset(smr.sharedMesh, meshPath);
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
            DestroyImmediate(prefabRoot);
            AssetDatabase.SaveAssets();
            Debug.Log($"Prefab saved: {path}");
        }

        private bool ValidateExport()
        {
            if (_doc == null || _doc.bones == null || _doc.bones.Count == 0)
            {
                EditorUtility.DisplayDialog("Export", "No bones to export.", "OK");
                return false;
            }
            return true;
        }
    }
}
#endif
