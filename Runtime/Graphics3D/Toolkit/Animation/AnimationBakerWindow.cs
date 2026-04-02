#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using Snm.Graphics3D.GPUSkinning;

namespace Snm.Graphics3D.Animation
{
    public class AnimationBakerWindow : EditorWindow
    {
        [SerializeField] private AnimationBakerToolSerializedData bakerToolSerializedData = new();
        private AnimationBakerTool _tool;

        [MenuItem("Tools/Snm/3D Toolkit/Animation/Animation Baker")]
        public static void OpenWindow()
        {
            GetWindow<AnimationBakerWindow>().Show();
        }

        public static void OpenWindow(IAnimationBakeData data)
        {
            var window = GetWindow<AnimationBakerWindow>();
            window.bakerToolSerializedData.Copy(data);
            window.Show();
            window._tool.Refresh();
        }

        private void CreateGUI()
        {
            rootVisualElement.Add(BuildContent());
        }

        internal VisualElement BuildContent()
        {
            return _tool = new AnimationBakerTool(bakerToolSerializedData);
        }

        [Serializable]
        private class AnimationBakerToolSerializedData : IAnimationBakeData
        {
            public GameObject prefab;
            public List<string> selectedExtraBones = new();
            public List<string> selectedAnims = new();
            public int fps = 30;

            public AnimationInstancingData asset;

            AnimationInstancingData IAnimationBakeData.Asset => asset;

            int IAnimationBakeData.Fps => fps;
            GameObject IAnimationBakeData.Prefab => prefab;
            List<string> IAnimationBakeData.SelectedExtraBones => selectedExtraBones;
            List<string> IAnimationBakeData.SelectedAnims => selectedAnims;

            public void Copy(IAnimationBakeData data)
            {
                prefab = data.Prefab;
                selectedExtraBones = data.SelectedExtraBones;
                selectedAnims = data.SelectedAnims;
                fps = data.Fps;
                asset = data.Asset;
            }
        }

        private class AnimationBakerTool : VisualElement
        {
            private readonly AnimationBakerToolSerializedData serializedData;

            private Label _statusLabel;
            private Button _bakeButton;
            private VisualElement _extraBoneTogglesHolder;
            private readonly List<Toggle> extraBoneToggles = new();
            private VisualElement _clipTogglesHolder;
            private readonly List<Toggle> clipToggles = new();

            private IntegerField _fps;
            private Foldout _selectExtraBoneLabel;
            private Foldout _selectionAnimationClips;
            private UnityEditor.UIElements.ObjectField _prefab;
            private UnityEditor.UIElements.ObjectField _asset;

            public AnimationBakerTool(AnimationBakerToolSerializedData serializedData)
            {
                this.serializedData = serializedData;
                SetupGUI();
            }

            private void SetupGUI()
            {
                _prefab = new UnityEditor.UIElements.ObjectField
                {
                    label = "Prefab",
                    value = serializedData.prefab,
                    objectType = typeof(GameObject),
                    allowSceneObjects = true
                };
                _prefab.RegisterValueChangedCallback(OnPrefabChanged);

                _asset = new UnityEditor.UIElements.ObjectField
                {
                    label = "Output Asset",
                    value = serializedData.asset,
                    objectType = typeof(AnimationInstancingData),
                    allowSceneObjects = true
                };
                _asset.RegisterValueChangedCallback(OnAssetChanged);

                _statusLabel = new Label() { text = "Status" };
                _bakeButton = new Button() { text = "Bake" };
                _bakeButton.RegisterCallback<ClickEvent>(OnBakeButtonClicked);
                _extraBoneTogglesHolder = new VisualElement();
                _clipTogglesHolder = new VisualElement();
                var refreshStatusButton = new Button() { text = "Refresh Status" };
                refreshStatusButton.RegisterCallback<ClickEvent>(evt => UpdateAll());

                (_fps = new IntegerField("fps") { value = serializedData.fps })
                    .RegisterCallback<ChangeEvent<int>>(evt => serializedData.fps = evt.newValue);
                _selectExtraBoneLabel = new Foldout() { text = "Select bones:" };
                _selectionAnimationClips = new Foldout() { text = "Select animation clips:" };
                Add(_prefab);
                Add(_asset);
                Add(_selectExtraBoneLabel);
                _selectExtraBoneLabel.Add(_extraBoneTogglesHolder);
                Add(_selectionAnimationClips);
                _selectionAnimationClips.Add(_clipTogglesHolder);
                Add(_statusLabel);
                Add(_fps);
                Add(refreshStatusButton);
                Add(_bakeButton);

                UpdateAll();
            }

            private void OnAssetChanged(ChangeEvent<UnityEngine.Object> evt)
            {
                serializedData.asset = evt.newValue as AnimationInstancingData;
            }

            private void OnPrefabChanged(ChangeEvent<UnityEngine.Object> evt)
            {
                serializedData.prefab = evt.newValue as GameObject;
                UpdateAll();
            }

            public void Refresh()
            {
                _prefab.value = serializedData.prefab;
                _asset.value = serializedData.asset;
                UpdateAll();
            }

            public void UpdateAll()
            {
                foreach (var bt in extraBoneToggles) _extraBoneTogglesHolder.Remove(bt);
                foreach (var bt in clipToggles) _clipTogglesHolder.Remove(bt);

                if (ValidatePrefab(serializedData.prefab))
                {
                    var allBonePaths = serializedData.prefab
                        .GetComponentsInChildren<Transform>(true)
                        .Select(t => RuntimeHelper.GetTransformPath(serializedData.prefab.transform, t))
                        .Where(p => !string.IsNullOrEmpty(p))
                        .ToArray();
                    UpdateSelectedBones(allBonePaths);

                    var skinnedMeshBones = serializedData.prefab
                        .GetComponentsInChildren<SkinnedMeshRenderer>()
                        .SelectMany(r => r.bones)
                        .Distinct().ToArray();

                    UpdateExtraBoneToggleList(serializedData.prefab.transform, skinnedMeshBones);

                    var clips = GetClips(serializedData.prefab.GetComponentInChildren<Animator>());
                    UpdateSerializedClipList(clips);
                    UpdateClipToggleList(clips);
                    _selectExtraBoneLabel.text = $"Select bones: ({serializedData.selectedExtraBones.Count + skinnedMeshBones.Length})";
                }
                else
                {
                    extraBoneToggles.Clear();
                }

                foreach (var bt in extraBoneToggles) _extraBoneTogglesHolder.Add(bt);
                foreach (var bt in clipToggles) _clipTogglesHolder.Add(bt);
            }

            private void OnBakeButtonClicked(ClickEvent evt)
            {
                var animationData = AnimationBaker.BakeWithAnimator(serializedData);

                var asset = (serializedData as IAnimationBakeData).Asset;
                if (asset != null)
                {
                    SaveToExistingAsset(animationData, asset);
                }
                else
                {
                    var savePath = "";
                    if (PrefabUtility.IsOutermostPrefabInstanceRoot(serializedData.prefab))
                    {
                        savePath = UnityEditor.AssetDatabase.GetAssetPath(serializedData.prefab)
                            .Replace(".prefab", ".asset");
                    }

                    if (string.IsNullOrEmpty(savePath))
                    {
                        savePath = EditorUtility.SaveFilePanel(
                            "Save file as",
                            "Assets/",
                            $"{serializedData.prefab?.name ?? "AnimationInstancingData"}.asset",
                            "asset");
                        if (savePath.StartsWith(Application.dataPath))
                        {
                            savePath = "Assets" + savePath[Application.dataPath.Length..];
                        }
                    }

                    if (string.IsNullOrEmpty(savePath))
                    {
                        Debug.LogError("Failed to save. No path selected");
                        return;
                    }
                    SaveToPath(animationData, savePath);

                    _asset.value = animationData;
                }
            }

            private static void SaveToPath(
                AnimationInstancingData animationData, string savePath)
            {
                var asset = animationData;
                AssetDatabase.CreateAsset(asset, savePath);
                foreach (var t in asset.animationTextureData.bakedBoneTextures)
                {
                    AssetDatabase.AddObjectToAsset(t, asset);
                }
                AssetDatabase.SaveAssets();
            }

            private bool ValidatePrefab(GameObject prefab)
            {
                var status = new List<string>();

                if (prefab == null) status.Add("No prefab selected");
                else
                {
                    var animator = prefab.GetComponentInChildren<Animator>();
                    // var smrs = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
                    var boneCounts = prefab.GetComponentsInChildren<Transform>(true)
                        .GroupBy(t => RuntimeHelper.GetTransformPath(prefab.transform, t))
                        .Select(g => (g.Key, g.Count()))
                        .ToArray();

                    if (animator == null) status.Add("Missing Animator");
                    if (animator != null && animator.runtimeAnimatorController == null) status.Add("Missing AnimatorController");
                    // if (smrs.Length == 0) status.Add("Missing SkinnedMeshRenderer");
                    // if (smrs.Any(smr => smr.sharedMesh == null)) status.Add("Missing Mesh");
                    if (boneCounts.Any(i => i.Item2 > 1)) status.Add($"Found ambiguous bones: {string.Join(",", boneCounts.Where(i => i.Item2 > 1).Select(i => i.Item1))}");
                }

                SetStatusLabel(status.Count > 0 ? string.Join(", ", status) : "OK");
                return status.Count <= 0;
            }

            private void SetStatusLabel(string status)
            {
                _statusLabel.text = "Status: " + status;

                OnStatusChanged();
            }

            private void OnStatusChanged()
            {
                UpdateBakeButton();
            }

            private void UpdateBakeButton()
            {
                _bakeButton.SetEnabled(_statusLabel.text.Contains("OK"));
            }

            private void UpdateExtraBoneToggleList(Transform root, Transform[] skinnedMeshBones)
            {
                extraBoneToggles.Clear();

                foreach (var b in TraverseTransformTree(root, 0))
                {
                    var name = b.transform.name;
                    var isSkinnedMeshBone = skinnedMeshBones.Contains(b.transform);
                    var path = RuntimeHelper.GetTransformPath(root, b.transform);
                    var t = new Toggle()
                    {
                        label = " " + string.Join("", new int[b.depth].Select(i => "-- ")) + b.transform.name,
                        value = isSkinnedMeshBone || serializedData.selectedExtraBones.Any(sb => sb == path),
                    };
                    t.SetEnabled(!isSkinnedMeshBone);
                    t.RegisterCallback<ChangeEvent<bool>>(evt =>
                    {
                        if (evt.newValue) serializedData.selectedExtraBones.Add(path);
                        else serializedData.selectedExtraBones.Remove(path);

                        _selectExtraBoneLabel.text = $"Select bones: ({serializedData.selectedExtraBones.Count + skinnedMeshBones.Length})";
                    });
                    extraBoneToggles.Add(t);
                }
            }

            private static IEnumerable<(Transform transform, int depth)> TraverseTransformTree(Transform current, int height)
            {
                yield return (current, height);

                foreach (Transform c in current)
                {
                    foreach (var p in TraverseTransformTree(c, height + 1))
                    {
                        yield return p;
                    }
                }
            }

            private void UpdateSelectedBones(IEnumerable<string> bonePaths)
            {
                if (serializedData.selectedExtraBones == null)
                    serializedData.selectedExtraBones = new List<string>();
                serializedData.selectedExtraBones = serializedData.selectedExtraBones
                    .Where(b => bonePaths.Any(bb => bb == b)).ToList();

                _selectExtraBoneLabel.text = $"Select extra bones: ({serializedData.selectedExtraBones.Count})";
            }

            private List<AnimationClip> GetClips(Animator animator)
            {
                var controller = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
                return GetClipsFromStatemachine(controller.layers[0].stateMachine);
            }

            private List<AnimationClip> GetClipsFromStatemachine(UnityEditor.Animations.AnimatorStateMachine stateMachine)
            {
                var list = new List<AnimationClip>();
                for (int i = 0; i != stateMachine.states.Length; ++i)
                {
                    UnityEditor.Animations.ChildAnimatorState state = stateMachine.states[i];
                    if (state.state.motion is UnityEditor.Animations.BlendTree)
                    {
                        var blendTree = state.state.motion as UnityEditor.Animations.BlendTree;
                        var childMotion = blendTree.children;
                        for (int j = 0; j != childMotion.Length; ++j)
                        {
                            list.Add(childMotion[j].motion as AnimationClip);
                        }
                    }
                    else if (state.state.motion != null)
                        list.Add(state.state.motion as AnimationClip);
                }

                for (int i = 0; i != stateMachine.stateMachines.Length; ++i)
                {
                    list.AddRange(GetClipsFromStatemachine(stateMachine.stateMachines[i].stateMachine));
                }

                return list.Where(q => q != null).Distinct().ToList();
            }


            private void UpdateSerializedClipList(List<AnimationClip> clips)
            {
                serializedData.selectedAnims = clips.Select(c => c.name).ToList();//serializedData.selectedAnims.Where(an => clips.Any(c => c.name == an)).ToList();
            }

            private void UpdateClipToggleList(List<AnimationClip> clips)
            {
                clipToggles.Clear();
                foreach (var c in clips)
                {
                    var t = new Toggle()
                    {
                        label = c.name + $" ({c.length})",
                        value = serializedData.selectedAnims.Any(a => a == c.name)
                    };
                    t.RegisterCallback<ChangeEvent<bool>>(evt =>
                    {
                        if (evt.newValue) serializedData.selectedAnims.Add(t.name);
                        else serializedData.selectedAnims.Remove(t.name);
                    });
                    clipToggles.Add(t);
                }
            }

        }

        public static void SaveToExistingAsset(AnimationInstancingData output, AnimationInstancingData asset)
        {
            foreach (var t in asset.animationTextureData.bakedBoneTextures)
            {
                UnityEditor.AssetDatabase.RemoveObjectFromAsset(t);
                DestroyImmediate(t);
            }

            asset.animInfoList = output.animInfoList;
            asset.boneData = output.boneData;
            asset.animationTextureData = output.animationTextureData;

            foreach (var t in asset.animationTextureData.bakedBoneTextures)
            {
                UnityEditor.AssetDatabase.AddObjectToAsset(t, asset);
            }

            UnityEditor.EditorUtility.SetDirty(asset);
            UnityEditor.AssetDatabase.SaveAssets();

            Debug.Log($"Baked to {UnityEditor.AssetDatabase.GetAssetPath(asset)}", asset);
        }

    }
}
#endif