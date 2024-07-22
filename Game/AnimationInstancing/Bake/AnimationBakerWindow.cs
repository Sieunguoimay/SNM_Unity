#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnimationInstancing_v2
{
    public class AnimationBakerWindow : EditorWindow
    {
        [SerializeField] private AnimationBakerToolSerializedData bakerToolSerializedData = new();

        [MenuItem("Tools/AnimInstancing/AnimationBaker")]
        public static void OpenWindow()
        {
            GetWindow<AnimationBakerWindow>().Show();
        }

        private void CreateGUI()
        {
            rootVisualElement.Add(new AnimationBakerTool(bakerToolSerializedData));
        }

        [Serializable]
        private class AnimationBakerToolSerializedData
        {
            public GameObject prefab;
            public List<string> selectedExtraBones = new();
            public List<string> selectedAnims = new();
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

            public AnimationBakerTool(AnimationBakerToolSerializedData serializedData)
            {
                this.serializedData = serializedData;
                SetupGUI();
            }

            private void SetupGUI()
            {
                var prefab = new ObjectField()
                {
                    label = "Prefab",
                    value = serializedData.prefab,
                    objectType = typeof(GameObject),
                };
                prefab.RegisterValueChangedCallback(OnPrefabChanged);

                _statusLabel = new Label() { text = "Status" };
                _bakeButton = new Button() { text = "Bake" };
                _bakeButton.RegisterCallback<ClickEvent>(OnBakeButtonClicked);
                _extraBoneTogglesHolder = new VisualElement();
                _clipTogglesHolder = new VisualElement();
                var refreshStatusButton = new Button() { text = "Refresh Status" };
                refreshStatusButton.RegisterCallback<ClickEvent>(evt => UpdateAll());

                Add(prefab);
                Add(new Label("Select extra bones:"));
                Add(_extraBoneTogglesHolder);
                Add(new Label("Select animation clips:"));
                Add(_clipTogglesHolder);
                Add(_statusLabel);
                Add(refreshStatusButton);
                Add(_bakeButton);

                UpdateAll();
            }

            private void OnPrefabChanged(ChangeEvent<UnityEngine.Object> evt)
            {
                serializedData.prefab = evt.newValue as GameObject;
                UpdateAll();
            }

            private void UpdateAll()
            {
                foreach (var bt in extraBoneToggles) _extraBoneTogglesHolder.Remove(bt);
                foreach (var bt in clipToggles) _clipTogglesHolder.Remove(bt);

                if (ValidatePrefab(serializedData.prefab))
                {
                    var extraBones = GetExtraBones(serializedData.prefab);
                    UpdateSerializedExtraBoneList(extraBones);
                    UpdateExtraBoneToggleList(extraBones);

                    var clips = GetClips(serializedData.prefab.GetComponentInChildren<Animator>());
                    UpdateSerializedClipList(clips);
                    UpdateClipToggleList(clips);
                }
                else
                {
                    UpdateExtraBoneToggleList(Array.Empty<Transform>());
                }

                foreach (var bt in extraBoneToggles) _extraBoneTogglesHolder.Add(bt);
                foreach (var bt in clipToggles) _clipTogglesHolder.Add(bt);
            }

            private void OnBakeButtonClicked(ClickEvent evt)
            {
                AnimationBaker.BakeWithAnimator(
                    serializedData.prefab,
                    serializedData.selectedExtraBones,
                    serializedData.selectedAnims,
                    out var animationData);

                var savePath = AssetDatabase.GetAssetPath(serializedData.prefab).Replace(".prefab", ".asset");

                BakedAnimationSaver.SaveAll(animationData, savePath);
            }

            private bool ValidatePrefab(GameObject prefab)
            {
                var status = new List<string>();

                if (prefab == null) status.Add("No prefab selected");
                else
                {
                    var animator = prefab.GetComponentInChildren<Animator>();
                    // var smrs = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();

                    if (animator == null) status.Add("Missing Animator");
                    if (animator != null && animator.runtimeAnimatorController == null) status.Add("Missing AnimatorController");
                    // if (smrs.Length == 0) status.Add("Missing SkinnedMeshRenderer");
                    // if (smrs.Any(smr => smr.sharedMesh == null)) status.Add("Missing Mesh");
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

            private Transform[] GetExtraBones(GameObject prefab)
            {
                if (prefab == null) return Array.Empty<Transform>();
                var smrBones = prefab
                    .GetComponentsInChildren<SkinnedMeshRenderer>()
                    .SelectMany(s => s.bones)
                    .ToArray();
                var extraBones = prefab
                    .GetComponentsInChildren<Transform>()
                    .Where(t => !smrBones.Contains(t))
                    .ToArray();
                return extraBones;
            }

            private void UpdateExtraBoneToggleList(Transform[] extraBones)
            {
                extraBoneToggles.Clear();

                foreach (var b in extraBones)
                {
                    var t = new Toggle() { label = b.name, value = serializedData.selectedExtraBones.Any(sb => sb == b.name) };
                    t.RegisterCallback<ChangeEvent<bool>>(evt =>
                    {
                        if (evt.newValue) serializedData.selectedExtraBones.Add(b.name);
                        else serializedData.selectedExtraBones.Remove(b.name);
                    });
                    extraBoneToggles.Add(t);
                }
            }

            private void UpdateSerializedExtraBoneList(Transform[] extraBones)
            {
                serializedData.selectedExtraBones = serializedData.selectedExtraBones.Where(b => extraBones.Any(bb => bb.name == b)).ToList();
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
                        label = c.name,
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
    }
}
#endif