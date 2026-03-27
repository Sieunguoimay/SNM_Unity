#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Snm.Runtime.GPUSkinning
{
    [CustomEditor(typeof(BakedAnimationRendererMB))]
    public class BakedAnimationRendererMBEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var sourcePrefab = serializedObject.FindProperty("sourcePrefab");
            var mesh = serializedObject.FindProperty("mesh");
            var material = serializedObject.FindProperty("material");
            var bakedData = serializedObject.FindProperty("bakedData");
            var defaultAnimation = serializedObject.FindProperty("defaultAnimation");
            var useInstancing = serializedObject.FindProperty("useInstancing");
            var shadowCasting = serializedObject.FindProperty("shadowCasting");
            var receiveShadows = serializedObject.FindProperty("receiveShadows");

            // Source
            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(sourcePrefab);

            var prefab = sourcePrefab.objectReferenceValue as GameObject;
            var hasAnimator = prefab != null && prefab.GetComponentInChildren<Animator>() != null;
            var hasSMR = prefab != null && prefab.GetComponentInChildren<SkinnedMeshRenderer>() != null;

            if (prefab != null && (!hasAnimator || !hasSMR))
            {
                var missing = new List<string>();
                if (!hasAnimator) missing.Add("Animator");
                if (!hasSMR) missing.Add("SkinnedMeshRenderer");
                EditorGUILayout.HelpBox($"Prefab is missing: {string.Join(", ", missing)}", MessageType.Warning);
            }

            // Bake & Setup button
            using (new EditorGUI.DisabledScope(!hasAnimator || !hasSMR))
            {
                var existing = bakedData.objectReferenceValue as AnimationInstancingData;
                var buttonLabel = existing != null ? "Rebake & Setup" : "Bake & Setup";

                if (GUILayout.Button(buttonLabel, GUILayout.Height(30)))
                {
                    BakeAndSetup(prefab, existing, mesh, material, bakedData);
                    serializedObject.ApplyModifiedProperties();
                    return;
                }
            }

            EditorGUILayout.Space();

            // Runtime fields (read-only look, but still editable for manual override)
            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(mesh);
            EditorGUILayout.PropertyField(material);
            EditorGUILayout.PropertyField(bakedData);
            EditorGUILayout.PropertyField(defaultAnimation);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rendering", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useInstancing);
            EditorGUILayout.PropertyField(shadowCasting);
            EditorGUILayout.PropertyField(receiveShadows);

            serializedObject.ApplyModifiedProperties();

            // Preview button
            var data = bakedData.objectReferenceValue as AnimationInstancingData;
            if (data != null && mesh.objectReferenceValue != null && material.objectReferenceValue != null)
            {
                EditorGUILayout.Space();
                if (GUILayout.Button("Open Animation Preview"))
                {
                    BakedAnimationPreviewWindow.OpenWindow((BakedAnimationRendererMB)target);
                }
            }

            // Playback info at runtime
            DrawPlaybackInfo();
        }

        private void BakeAndSetup(
            GameObject prefab,
            AnimationInstancingData existingAsset,
            SerializedProperty meshProp,
            SerializedProperty materialProp,
            SerializedProperty bakedDataProp)
        {
            // Extract clips from AnimatorController
            var animator = prefab.GetComponentInChildren<Animator>();
            var controller = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            if (controller == null || controller.layers.Length == 0)
            {
                Debug.LogError("BakeAndSetup: Prefab's Animator has no AnimatorController.");
                return;
            }

            var clipNames = ExtractClipNames(controller.layers[0].stateMachine);
            if (clipNames.Count == 0)
            {
                Debug.LogError("BakeAndSetup: No animation clips found in AnimatorController.");
                return;
            }

            // Extract extra bones (non-skinned transforms that have child renderers)
            var extraBones = new List<string>();

            // Bake
            var bakedOutput = AnimationBaker.BakeWithAnimator(prefab, extraBones, clipNames, 30);
            if (bakedOutput == null)
            {
                Debug.LogError("BakeAndSetup: Baking failed.");
                return;
            }

            // Save asset
            if (existingAsset != null)
            {
                AnimationBakerWindow.SaveToExistingAsset(bakedOutput, existingAsset);
            }
            else
            {
                var prefabPath = AssetDatabase.GetAssetPath(prefab);
                var savePath = string.IsNullOrEmpty(prefabPath)
                    ? $"Assets/{prefab.name}_Baked.asset"
                    : prefabPath.Replace(".prefab", "_Baked.asset");

                AssetDatabase.CreateAsset(bakedOutput, savePath);
                foreach (var t in bakedOutput.animationTextureData.bakedBoneTextures)
                    AssetDatabase.AddObjectToAsset(t, bakedOutput);
                AssetDatabase.SaveAssets();

                existingAsset = bakedOutput;
            }

            // Auto-fill fields from prefab
            var smr = prefab.GetComponentInChildren<SkinnedMeshRenderer>();
            meshProp.objectReferenceValue = smr.sharedMesh;
            materialProp.objectReferenceValue = smr.sharedMaterial;
            bakedDataProp.objectReferenceValue = existingAsset;

            Debug.Log($"Bake & Setup complete: {clipNames.Count} clips baked to {AssetDatabase.GetAssetPath(existingAsset)}", existingAsset);
        }

        private static List<string> ExtractClipNames(UnityEditor.Animations.AnimatorStateMachine stateMachine)
        {
            var names = new List<string>();
            foreach (var state in stateMachine.states)
            {
                if (state.state.motion is AnimationClip clip)
                    names.Add(clip.name);
                else if (state.state.motion is UnityEditor.Animations.BlendTree blendTree)
                {
                    foreach (var child in blendTree.children)
                    {
                        if (child.motion is AnimationClip blendClip)
                            names.Add(blendClip.name);
                    }
                }
            }
            foreach (var sub in stateMachine.stateMachines)
                names.AddRange(ExtractClipNames(sub.stateMachine));
            return names.Distinct().ToList();
        }

        private void DrawPlaybackInfo()
        {
            if (!Application.isPlaying) return;
            var renderer = (BakedAnimationRendererMB)target;
            if (renderer.Player == null) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Playback", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Playing", renderer.Player.IsPlaying.ToString());
            EditorGUILayout.LabelField("Frame", renderer.Player.FrameIndex.ToString("F1"));

            var info = renderer.Player.GetCurrentAnimationInfo();
            if (info != null)
                EditorGUILayout.LabelField("Clip", info.animationName);

            Repaint();
        }
    }
}
#endif
