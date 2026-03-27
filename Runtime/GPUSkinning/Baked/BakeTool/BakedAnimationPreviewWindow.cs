#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Snm.Runtime.GPUSkinning
{
    public class BakedAnimationPreviewWindow : EditorWindow
    {
        private BakedAnimationRendererMB _target;
        private BakedAnimationPlayer _player;
        private AnimationInstancingData _bakedData;
        private AnimationTextureData _texData;
        private Mesh _mesh;
        private Material _previewMaterial;
        private MaterialPropertyBlock _propertyBlock;
        private Matrix4x4[] _bindposeInverses;
        private int[] _parentIndices;

        // State
        private bool _isPlaying;
        private bool _showBones = true;
        private float _speed = 1f;
        private readonly HashSet<int> _markedOverrideBones = new();
        private double _lastTime;

        // UI elements
        private DropdownField _clipDropdown;
        private Slider _frameSlider;
        private Label _frameLabel;
        private Label _clipInfoLabel;
        private Button _playPauseButton;
        private ListView _boneListView;

        // Shader IDs
        private static readonly int BoneTextureId = Shader.PropertyToID("_boneTexture");
        private static readonly int BoneTextureWidthId = Shader.PropertyToID("_boneTextureWidth");
        private static readonly int BoneTextureHeightId = Shader.PropertyToID("_boneTextureHeight");
        private static readonly int BoneTextureBlockWidthId = Shader.PropertyToID("_boneTextureBlockWidth");
        private static readonly int BoneTextureBlockHeightId = Shader.PropertyToID("_boneTextureBlockHeight");
        private static readonly int FrameIndexId = Shader.PropertyToID("frameIndex");
        private static readonly int PreFrameIndexId = Shader.PropertyToID("preFrameIndex");
        private static readonly int TransitionProgressId = Shader.PropertyToID("transitionProgress");

        [MenuItem("Tools/Snm/Game/Baked Animation Preview")]
        public static void OpenWindow()
        {
            GetWindow<BakedAnimationPreviewWindow>("Baked Anim Preview").Show();
        }

        public static void OpenWindow(BakedAnimationRendererMB target)
        {
            var window = GetWindow<BakedAnimationPreviewWindow>("Baked Anim Preview");
            window.SetTarget(target);
            window.Show();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.update -= OnEditorUpdate;
            CleanupPreview();
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingTop = 4;
            root.style.paddingLeft = 4;
            root.style.paddingRight = 4;

            // Target field
            var targetField = new ObjectField("Target") { objectType = typeof(BakedAnimationRendererMB) };
            targetField.value = _target;
            targetField.RegisterValueChangedCallback(evt =>
            {
                SetTarget(evt.newValue as BakedAnimationRendererMB);
            });
            root.Add(targetField);

            root.Add(new VisualElement { style = { height = 8 } });

            // Clip selector
            _clipDropdown = new DropdownField("Animation Clip");
            _clipDropdown.RegisterValueChangedCallback(evt => OnClipSelected());
            root.Add(_clipDropdown);

            // Clip info
            _clipInfoLabel = new Label { style = { color = Color.gray, fontSize = 11 } };
            root.Add(_clipInfoLabel);

            root.Add(new VisualElement { style = { height = 4 } });

            // Frame slider
            _frameLabel = new Label("Frame: 0 / 0");
            root.Add(_frameLabel);
            _frameSlider = new Slider(0, 1) { label = "Scrub" };
            _frameSlider.RegisterValueChangedCallback(evt =>
            {
                if (_player == null) return;
                _isPlaying = false;
                UpdatePlayPauseButton();
                _player.SetFrame(evt.newValue);
                SceneView.RepaintAll();
            });
            root.Add(_frameSlider);

            // Playback controls
            var playbackRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 4 } };
            _playPauseButton = new Button(TogglePlay) { text = "Play", style = { flexGrow = 1 } };
            playbackRow.Add(_playPauseButton);
            var stopButton = new Button(StopPlayback) { text = "Stop", style = { flexGrow = 1 } };
            playbackRow.Add(stopButton);
            root.Add(playbackRow);

            // Speed
            var speedSlider = new Slider("Speed", 0.1f, 3f) { value = _speed };
            speedSlider.RegisterValueChangedCallback(evt =>
            {
                _speed = evt.newValue;
                if (_player != null) _player.PlaySpeed = _speed;
            });
            root.Add(speedSlider);

            root.Add(new VisualElement { style = { height = 8 } });

            // Bone gizmo toggle
            var boneToggle = new Toggle("Show Bone Gizmos") { value = _showBones };
            boneToggle.RegisterValueChangedCallback(evt =>
            {
                _showBones = evt.newValue;
                SceneView.RepaintAll();
            });
            root.Add(boneToggle);

            // Bone list
            var boneFoldout = new Foldout { text = "Bones", value = false };

            _boneListView = new ListView
            {
                fixedItemHeight = 20,
                selectionType = SelectionType.Single,
                style = { maxHeight = 300 }
            };
            _boneListView.makeItem = () =>
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                row.Add(new Label { name = "bone-label", style = { flexGrow = 1 } });
                row.Add(new Toggle { name = "bone-override", tooltip = "Mark as override target" });
                return row;
            };
            _boneListView.bindItem = (element, index) =>
            {
                var label = element.Q<Label>("bone-label");
                label.text = GetBoneName(index);

                var toggle = element.Q<Toggle>("bone-override");
                toggle.SetValueWithoutNotify(_markedOverrideBones.Contains(index));
                toggle.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue) _markedOverrideBones.Add(index);
                    else _markedOverrideBones.Remove(index);
                    SceneView.RepaintAll();
                });

                var isMarked = _markedOverrideBones.Contains(index);
                label.style.color = isMarked ? new Color(1f, 0.3f, 0.3f) : Color.white;
            };

            boneFoldout.Add(_boneListView);
            root.Add(boneFoldout);

            // Override info
            var overrideInfo = new HelpBox(
                "Toggle bones in the list to mark them as override targets. " +
                "Marked bones show as red in the Scene view. " +
                "Use the bone index with SetBoneOverride() in your code.",
                HelpBoxMessageType.Info);
            root.Add(overrideInfo);

            RefreshUI();
        }

        // =============================================
        // Target management
        // =============================================

        private void SetTarget(BakedAnimationRendererMB target)
        {
            CleanupPreview();
            _target = target;
            if (_target != null)
                SetupPreview();
            RefreshUI();
        }

        private void SetupPreview()
        {
            var bakedDataField = GetSerializedField<AnimationInstancingData>("bakedData");
            var meshField = GetSerializedField<Mesh>("mesh");
            var materialField = GetSerializedField<Material>("material");

            if (bakedDataField == null || meshField == null || materialField == null) return;

            _bakedData = bakedDataField;
            _mesh = meshField;
            _texData = _bakedData.animationTextureData;

            _previewMaterial = Instantiate(materialField);
            _previewMaterial.EnableKeyword("BAKED_SKINNING_ON");
            _previewMaterial.DisableKeyword("GPU_SKINNING_ON");

            if (_texData?.bakedBoneTextures != null && _texData.bakedBoneTextures.Length > 0)
            {
                var tex = _texData.bakedBoneTextures[0];
                _previewMaterial.SetTexture(BoneTextureId, tex);
                _previewMaterial.SetInt(BoneTextureWidthId, tex.width);
                _previewMaterial.SetInt(BoneTextureHeightId, tex.height);
                _previewMaterial.SetInt(BoneTextureBlockWidthId, _texData.textureBlockWidth);
                _previewMaterial.SetInt(BoneTextureBlockHeightId, _texData.textureBlockHeight);
            }

            _propertyBlock = new MaterialPropertyBlock();
            _player = new BakedAnimationPlayer(_bakedData);
            _player.PlaySpeed = _speed;
            if (_player.AnimationCount > 0) _player.Play(0);

            // Cache bindpose inverses
            var bindposes = _mesh.bindposes;
            var extra = _bakedData.boneData?.extraBindPoses;
            var allBindposes = extra != null && extra.Length > 0
                ? bindposes.Concat(extra).ToArray()
                : bindposes;

            _bindposeInverses = new Matrix4x4[allBindposes.Length];
            for (int i = 0; i < allBindposes.Length; i++)
                _bindposeInverses[i] = allBindposes[i].inverse;

            // Build parent indices from source prefab
            BuildParentIndices();

            _lastTime = EditorApplication.timeSinceStartup;
        }

        private void BuildParentIndices()
        {
            _parentIndices = null;
            var prefabField = GetSerializedField<GameObject>("sourcePrefab");
            if (prefabField == null) return;

            var smr = prefabField.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr == null || smr.bones == null) return;

            var bones = smr.bones;
            _parentIndices = new int[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                _parentIndices[i] = -1;
                if (bones[i] == null || bones[i].parent == null) continue;
                for (int j = 0; j < bones.Length; j++)
                {
                    if (j != i && bones[j] == bones[i].parent)
                    {
                        _parentIndices[i] = j;
                        break;
                    }
                }
            }
        }

        private void CleanupPreview()
        {
            if (_previewMaterial != null) { DestroyImmediate(_previewMaterial); _previewMaterial = null; }
            _player = null;
            _propertyBlock = null;
            _bakedData = null;
            _texData = null;
            _mesh = null;
            _bindposeInverses = null;
            _parentIndices = null;
            _isPlaying = false;
        }

        private T GetSerializedField<T>(string fieldName) where T : Object
        {
            if (_target == null) return null;
            var so = new SerializedObject(_target);
            var prop = so.FindProperty(fieldName);
            return prop?.objectReferenceValue as T;
        }

        // =============================================
        // UI updates
        // =============================================

        private void RefreshUI()
        {
            if (_clipDropdown == null) return;

            if (_player == null || _bakedData == null)
            {
                _clipDropdown.choices = new List<string> { "(none)" };
                _clipDropdown.index = 0;
                _frameSlider.highValue = 1;
                _frameSlider.value = 0;
                _frameLabel.text = "Frame: - / -";
                _clipInfoLabel.text = "";
                _boneListView.itemsSource = System.Array.Empty<int>();
                return;
            }

            var names = new List<string>();
            for (int i = 0; i < _player.AnimationCount; i++)
            {
                var info = _player.GetAnimationInfo(i);
                names.Add(info != null ? $"{i}: {info.animationName}" : $"{i}: (unknown)");
            }
            _clipDropdown.choices = names;
            _clipDropdown.index = 0;

            UpdateFrameSliderRange();
            UpdateBoneList();
        }

        private void OnClipSelected()
        {
            if (_player == null || _clipDropdown == null) return;
            int idx = _clipDropdown.index;
            if (idx < 0 || idx >= _player.AnimationCount) return;
            _player.Play(idx);
            _player.SetFrame(0);
            UpdateFrameSliderRange();
            SceneView.RepaintAll();
        }

        private void UpdateFrameSliderRange()
        {
            if (_player == null || _clipDropdown == null) return;
            var info = _player.GetAnimationInfo(_clipDropdown.index);
            if (info == null) return;
            _frameSlider.highValue = Mathf.Max(info.totalFrame - 1, 1);
            _frameSlider.SetValueWithoutNotify(_player.CurrentFrame);
            _frameLabel.text = $"Frame: {_player.CurrentFrame:F1} / {info.totalFrame - 1}";
            _clipInfoLabel.text = $"FPS: {info.fps}  |  Frames: {info.totalFrame}  |  Wrap: {info.wrapMode}";
        }

        private void UpdateBoneList()
        {
            if (_bindposeInverses == null) { _boneListView.itemsSource = System.Array.Empty<int>(); return; }
            var indices = Enumerable.Range(0, _bindposeInverses.Length).ToList();
            _boneListView.itemsSource = indices;
            _boneListView.Rebuild();
        }

        private string GetBoneName(int index)
        {
            var prefab = GetSerializedField<GameObject>("sourcePrefab");
            if (prefab != null)
            {
                var smr = prefab.GetComponentInChildren<SkinnedMeshRenderer>();
                if (smr?.bones != null && index < smr.bones.Length && smr.bones[index] != null)
                    return $"[{index}] {smr.bones[index].name}";
            }
            return $"[{index}]";
        }

        // =============================================
        // Playback
        // =============================================

        private void TogglePlay()
        {
            _isPlaying = !_isPlaying;
            _lastTime = EditorApplication.timeSinceStartup;
            UpdatePlayPauseButton();
        }

        private void StopPlayback()
        {
            _isPlaying = false;
            if (_player != null) _player.SetFrame(0);
            UpdatePlayPauseButton();
            SceneView.RepaintAll();
        }

        private void UpdatePlayPauseButton()
        {
            if (_playPauseButton != null)
                _playPauseButton.text = _isPlaying ? "Pause" : "Play";
        }

        private void OnEditorUpdate()
        {
            if (!_isPlaying || _player == null) return;

            double now = EditorApplication.timeSinceStartup;
            float dt = (float)(now - _lastTime);
            _lastTime = now;

            _player.Update(dt);

            if (_frameSlider != null)
            {
                _frameSlider.SetValueWithoutNotify(_player.CurrentFrame);
                var info = _player.GetCurrentAnimationInfo();
                if (info != null)
                    _frameLabel.text = $"Frame: {_player.CurrentFrame:F1} / {info.totalFrame - 1}";
            }

            SceneView.RepaintAll();
        }

        // =============================================
        // Scene view rendering
        // =============================================

        private void OnSceneGUI(SceneView sceneView)
        {
            if (_target == null || _player == null || _previewMaterial == null || _mesh == null) return;

            // Render mesh
            UpdatePropertyBlock();
            Graphics.DrawMesh(_mesh, _target.transform.localToWorldMatrix, _previewMaterial,
                0, sceneView.camera, 0, _propertyBlock, ShadowCastingMode.Off, false);

            // Draw bone gizmos
            if (_showBones && _texData != null && _bindposeInverses != null)
                DrawBoneGizmos();
        }

        private void UpdatePropertyBlock()
        {
            int texIdx = _player.TextureIndex;
            if (_texData?.bakedBoneTextures != null && texIdx < _texData.bakedBoneTextures.Length)
            {
                var tex = _texData.bakedBoneTextures[texIdx];
                _propertyBlock.SetTexture(BoneTextureId, tex);
                _propertyBlock.SetInt(BoneTextureWidthId, tex.width);
                _propertyBlock.SetInt(BoneTextureHeightId, tex.height);
                _propertyBlock.SetInt(BoneTextureBlockWidthId, _texData.textureBlockWidth);
                _propertyBlock.SetInt(BoneTextureBlockHeightId, _texData.textureBlockHeight);
            }
            _propertyBlock.SetFloat(FrameIndexId, _player.FrameIndex);
            _propertyBlock.SetFloat(PreFrameIndexId, _player.PreFrameIndex);
            _propertyBlock.SetFloat(TransitionProgressId, _player.TransitionProgress);
        }

        private void DrawBoneGizmos()
        {
            int texIdx = _player.TextureIndex;
            int frame = (int)_player.FrameIndex;
            int boneCount = _bindposeInverses.Length;
            var l2w = _target.transform.localToWorldMatrix;
            var positions = new Vector3[boneCount];

            for (int i = 0; i < boneCount; i++)
            {
                var bakedMat = BakedBoneMatrixReader.ReadBoneMatrix(_texData, texIdx, frame, i);
                positions[i] = BakedBoneMatrixReader.BoneWorldPosition(l2w, bakedMat, _bindposeInverses[i]);

                bool isMarked = _markedOverrideBones.Contains(i);
                Handles.color = isMarked ? new Color(1f, 0.2f, 0.2f) : new Color(0f, 0.9f, 1f);

                float size = HandleUtility.GetHandleSize(positions[i]) * 0.03f;
                if (Handles.Button(positions[i], Quaternion.identity, size, size * 1.5f, Handles.DotHandleCap))
                {
                    if (_markedOverrideBones.Contains(i))
                        _markedOverrideBones.Remove(i);
                    else
                        _markedOverrideBones.Add(i);
                    _boneListView?.Rebuild();
                }
            }

            // Draw parent-child lines
            if (_parentIndices != null)
            {
                Handles.color = new Color(0f, 0.9f, 1f, 0.3f);
                for (int i = 0; i < Mathf.Min(boneCount, _parentIndices.Length); i++)
                {
                    int parent = _parentIndices[i];
                    if (parent >= 0 && parent < boneCount)
                    {
                        var lineColor = _markedOverrideBones.Contains(i)
                            ? new Color(1f, 0.2f, 0.2f, 0.5f)
                            : new Color(0f, 0.9f, 1f, 0.3f);
                        Handles.color = lineColor;
                        Handles.DrawLine(positions[parent], positions[i]);
                    }
                }
            }
        }
    }
}
#endif
